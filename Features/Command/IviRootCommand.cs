using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO.Compression;
using ivinject.Common.Models;

namespace ivinject.Features.Command;

internal class IviRootCommand : RootCommand
{
    private static string ParseAppPackageResult(ArgumentResult result)
    {
        var value = result.Tokens[0].Value;

        if (!RegularExpressions.ApplicationPackage().IsMatch(value))
            result.AddError("The application package must be either an .app bundle or an .ipa$ archive.");
                
        return value;
    }
    
    //

    private readonly Argument<string> _targetArgument = new("target")
    {
        Description = "The application package, either .app bundle or ipa$",
        CustomParser = ParseAppPackageResult
    };

    private readonly Argument<string> _outputArgument = new("output")
    {
        Description = "The output application package, either .app bundle or ipa$",
        CustomParser = ParseAppPackageResult
    };

    private readonly Option<bool> _overwriteOutputOption = new("--overwrite")
    {
        Description = "Overwrite the output if it already exists"
    };

    private readonly Option<CompressionLevel> _compressionLevelOption = new("--compression-level")
    {
        Aliases = { "--level" },
        Description = "The compression level for ipa$ archive output",
        DefaultValueFactory = _ => CompressionLevel.Fastest
    };
    
    //
    
    private readonly Option<IEnumerable<FileInfo>> _itemsOption = new("--items")
    {
        Description = "The entries to inject (Debian packages, Frameworks, and Bundles)",
        Aliases = { "-i" },
        AllowMultipleArgumentsPerToken = true
    };
    
    private readonly Option<FileInfo> _provisioningProfileOption = new("--profile")
    {
        Aliases = { "-p" },
        Description = "Provisioning profile to extract entitlements, signing identity, and bundle ID"
    };

    private readonly Option<bool> _profileBundleIdOption = new("--profile-bundle-id")
    {
        Description = "Replace the bundle ID with the one in the provisioning profile"
    };

    private readonly Option<string> _codesignIdentityOption = new("--sign")
    {
        Aliases = { "-s" },
        Description = "The identity for code signing (use \"-\" for ad hoc, a.k.a. fake signing)"
    };
    
    private readonly Option<FileInfo> _codesignEntitlementsOption = new("--entitlements")
    {
        Aliases = { "-e" },
        Description = "The file containing entitlements that will be written into main executables"
    };
    
    //

    private readonly Option<string> _customBundleIdOption = new("--bundle-id")
    {
        Aliases = { "-b" },
        Description = "The custom identifier that will be applied to application bundles"
    };

    private readonly Option<bool> _enableDocumentsSupportOption = new("--enable-documents-support")
    {
        Aliases = { "-d" },
        Description = "Enables documents support (file sharing) for the application"
    };

    private readonly Option<bool> _removeSupportedDevicesOption = new("--remove-supported-devices")
    {
        Aliases = { "-u" },
        Description = "Removes supported devices property"
    };
    
    private readonly Option<IEnumerable<string>> _directoriesToRemoveOption = new("--remove-directories")
    {
        Aliases = { "-r" },
        Description = "Directories to remove in the app package, e.g. PlugIns, Watch, AppClip",
        AllowMultipleArgumentsPerToken = true
    };
    
    internal IviRootCommand() : base("The most demure iOS app injector and signer")
    {
        Arguments.Add(_targetArgument);
        Arguments.Add(_outputArgument);
        Options.Add(_overwriteOutputOption);
        Options.Add(_compressionLevelOption);
        
        Options.Add(_itemsOption);
        Options.Add(_provisioningProfileOption);
        Options.Add(_profileBundleIdOption);
        Options.Add(_codesignIdentityOption);
        Options.Add(_codesignEntitlementsOption);
        
        Options.Add(_customBundleIdOption);
        Options.Add(_enableDocumentsSupportOption);
        Options.Add(_removeSupportedDevicesOption);
        Options.Add(_directoriesToRemoveOption);
        
        SetAction(async parseResult =>
            {
                var binder = new IviRootCommandParametersBinder(
                    _targetArgument,
                    _outputArgument,
                    _overwriteOutputOption,
                    _compressionLevelOption,
                    _itemsOption,
                    _provisioningProfileOption,
                    _profileBundleIdOption,
                    _codesignIdentityOption,
                    _codesignEntitlementsOption,
                    _customBundleIdOption,
                    _enableDocumentsSupportOption,
                    _removeSupportedDevicesOption,
                    _directoriesToRemoveOption
                );
                var iviParameters = binder.GetBoundValue(parseResult);
                var commandProcessor = new IviRootCommandProcessor();
                await commandProcessor.ProcessRootCommand(iviParameters);
            }
        );
    }
}