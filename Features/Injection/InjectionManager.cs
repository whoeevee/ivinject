using Claunia.PropertyList;
using ivinject.Common.Models;
using ivinject.Features.Injection.Models;
using ivinject.Features.Packaging;
using ivinject.Features.Packaging.Models;
using Microsoft.Extensions.Logging;
using static ivinject.Common.DirectoryExtensions;

namespace ivinject.Features.Injection;

internal class InjectionManager(ILogger logger)
{
    private readonly List<IviCopiedBinary> _copiedBinaries = [];
    private IviPackageInfo _packageInfo = null!;
    
    private static readonly IviInjectionEntry[] KnownFrameworkEntries = Directory.GetDirectories(
            Path.Combine(HomeDirectoryPath(), ".ivinject"),
            "*.framework"
        )
        .Select(framework => new IviInjectionEntry(framework))
        .ToArray();

    private static readonly IviInjectionEntry OrionFramework = KnownFrameworkEntries
        .Single(framework => framework.Name == "Orion.framework");
        
    private static readonly IviInjectionEntry SubstrateFramework = KnownFrameworkEntries
        .Single(framework => framework.Name == "CydiaSubstrate.framework");
    
    internal void UpdateWithPackage(IviPackageInfo packageInfo) =>
        _packageInfo = packageInfo;

    private async Task<IviInjectionEntry[]> PrepareEntriesAsync(
        IviInjectionEntry[] entries,
        List<IDisposable> debManagers
    )
    {
        var finalEntries = entries.Where(entry =>
            entry.Type is not IviInjectionEntryType.DebianPackage
        ).ToList();

        var debFiles = entries.Where(entry =>
            entry.Type is IviInjectionEntryType.DebianPackage
        );

        foreach (var debFile in debFiles)
        {
            var name = debFile.Name;

            var debManager = new DebManager(debFile);
            
            var debEntries = await debManager.ExtractDebEntries();
            logger.LogInformation("{} entries within {} will be injected", debEntries.Length, name);
            
            finalEntries.AddRange(debEntries);
            debManagers.Add(debManager);
        }
        
        return finalEntries.ToArray();
    }

    private void CopyEntries(IEnumerable<IviInjectionEntry> entries)
    {
        foreach (var entry in entries)
        {
            var isReplaced = false;
            var pathInBundle = entry.GetPathInBundle(_packageInfo.DirectoriesInfo);
            
            var isDynamicLibrary = entry.Type is IviInjectionEntryType.DynamicLibrary;
            
            if (isDynamicLibrary || entry.Type is IviInjectionEntryType.Unknown)
            {
                if (File.Exists(pathInBundle))
                {
                    File.Delete(pathInBundle);
                    isReplaced = true;
                }
                
                File.Copy(entry.FullName, pathInBundle);
                
                if (isDynamicLibrary)
                    _copiedBinaries.Add(
                        new IviCopiedBinary
                        {
                            Binary = new IviMachOBinary(pathInBundle),
                            Type = entry.Type
                        }
                    );
            }
            else
            {
                if (Directory.Exists(pathInBundle))
                {
                    Directory.Delete(pathInBundle, true);
                    isReplaced = true;
                }
                
                CopyDirectory(entry.FullName, pathInBundle, true);

                if (entry.Type is not IviInjectionEntryType.Bundle) {
                    var infoDictionaryPath = Path.Combine(pathInBundle, "Info.plist");
                    var infoDictionary = (NSDictionary)PropertyListParser.Parse(infoDictionaryPath);
                    
                    _copiedBinaries.Add(
                        new IviCopiedBinary
                        {
                            Binary = new IviMachOBinary(
                                Path.Combine(pathInBundle, infoDictionary.BundleExecutable())
                            ),
                            Type = entry.Type
                        }
                    );
                }
            }
            
            logger.LogInformation("{} {}", isReplaced ? "Replaced" : "Copied", entry.Name);
        }
    }
    
    internal async Task AddEntriesAsync(IEnumerable<IviInjectionEntry> files)
    {
        var debManagers = new List<IDisposable>();
        
        var entries = await PrepareEntriesAsync(files.ToArray(), debManagers);
        CopyEntries(entries);
        
        debManagers.ForEach(manager => manager.Dispose());
    }
    
    internal async Task<bool> ThinCopiedBinariesAsync()
    {
        var fatBinaries = 
            _copiedBinaries.Select(binary => binary.Binary).Where(binary => binary.IsFatFile);
        
        foreach (var fatBinary in fatBinaries)
        {
            var previousSize = fatBinary.FileSize;

            if (!await fatBinary.Thin())
                return false;
            
            logger.LogInformation(
                "Thinned {} ({} -> {})",
                fatBinary.Name,
                previousSize,
                fatBinary.FileSize
            );
        }
        
        return true;
    }
    
    internal async Task CopyKnownFrameworksAsync()
    {
        var allDependencies = await _copiedBinaries.AllDependencies();
        
        var entries = KnownFrameworkEntries.Where(framework =>
            allDependencies.Any(dependency => dependency.Contains(framework.Name))
        ).ToList();

        if (entries.Contains(OrionFramework) && !entries.Contains(SubstrateFramework))
            entries.Add(SubstrateFramework);
        
        CopyEntries(entries);
    }

    internal async Task FixCopiedDependenciesAsync()
    {
        var copiedNames = _copiedBinaries.Select(binary => binary.Name);
        
        foreach (var binary in _copiedBinaries.Select(binary => binary.Binary))
        {
            var dependencies = await binary.GetSharedLibraries();
            
            var brokenDependencies = dependencies.Where(dependency => 
                !dependency.StartsWith('@') && copiedNames.Any(dependency.Contains)
            );
            
            foreach (var dependency in brokenDependencies)
            {
                var copiedBinary = _copiedBinaries.Single(copiedBinary =>
                    dependency.Contains(copiedBinary.Name)
                );

                var newPath = copiedBinary.GetRunPath(_packageInfo.DirectoriesInfo);
                await binary.ChangeDependency(dependency, newPath);

                logger.LogInformation(
                    "Fixed dependency path in {} ({} -> {})",
                    binary.Name,
                    dependency,
                    newPath
                );
            }
        }
    }

    internal async Task InsertLoadCommandsAsync()
    {
        var mainBinary = _packageInfo.MainBinary;

        var copiedDependencies = (await _copiedBinaries.AllDependencies())
            .Where(dependency => dependency.StartsWith('@'));

        var mainBinaryDependencies = await mainBinary.GetSharedLibraries();

        // if (mainBinaryDependencies.All(dependency => !dependency.StartsWith("@rpath")))
        // {
        //     await mainBinary.AddRunPath("@executable_path/Frameworks");
        //     logger.LogInformation("Added Frameworks to {}'s run path", mainBinary.Name);
        // }

        var dependenciesToInsert = _copiedBinaries
            .Where(binary => 
                binary.Type is IviInjectionEntryType.DynamicLibrary or IviInjectionEntryType.Framework
            )
            .Where(binary => 
                copiedDependencies.All(dependency => !dependency.Contains(binary.Name))
            );
        
        foreach (var dependency in dependenciesToInsert)
        {
            if (mainBinaryDependencies.Contains(dependency.Name))
                continue;
            
            var runPath = dependency.GetRunPath(_packageInfo.DirectoriesInfo);

            await mainBinary.InsertDependency(runPath);
            logger.LogInformation("Inserted load command {} into {}", runPath, mainBinary.Name);
        }
    }
}