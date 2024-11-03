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
            result.ErrorMessage = "The application package must be either an .app bundle or an .ipa$ archive.";
                
        return value;
    }
    
    //
    
    private readonly Argument<string> _targetArgument = new(
        name: "target",
        description: "The application package, either .app bundle or ipa$",
        parse: ParseAppPackageResult
    );
    
    private readonly Argument<string> _outputArgument = new(
        name: "output",
        description: "The output application package, either .app bundle or ipa$",
        parse: ParseAppPackageResult
    );
    
    private readonly Option<bool> _overwriteOutputOption = new(
        "--overwrite",
        "Overwrite the output if it already exists"
    );

    private readonly Option<CompressionLevel> _compressionLevelOption = new(
        "--compression-level",
        description: "The compression level for ipa$ archive output",
        getDefaultValue: () => CompressionLevel.Fastest
    );
    
    //
    
    private readonly Option<IEnumerable<FileInfo>> _itemsOption = new("--items")
    {
        Description = "The entries to inject (Debian packages, Frameworks, and Bundles)",
        AllowMultipleArgumentsPerToken = true
    };
    
    private readonly Option<string> _codesignIdentityOption = new(
        "--sign",
        "The identity for code signing (use \"-\" for ad hoc, a.k.a. fake signing)"
    );
    
    private readonly Option<FileInfo> _codesignEntitlementsOption = new(
        "--entitlements",
        "The file containing entitlements that will be written into main executables"
    );
    
    //
    
    private readonly Option<string> _customBundleIdOption = new(
        "--bundleId",
        "The custom identifier that will be applied to application bundles"
    );
    
    private readonly Option<bool> _enableDocumentsSupportOption = new(
        "--enable-documents-support",
        "Enables documents support (file sharing) for the application"
    );
    
    private readonly Option<bool> _removeSupportedDevicesOption = new(
        "--remove-supported-devices",
        "Removes supported devices property"
    );
    
    private readonly Option<IEnumerable<string>> _directoriesToRemoveOption = new("--remove-directories")
    {
        Description = "Directories to remove in the app package, e.g. PlugIns, Watch, AppClip",
        AllowMultipleArgumentsPerToken = true
    };
    
    internal IviRootCommand() : base("The most demure iOS app injector and signer")
    {
        _itemsOption.AddAlias("-i");
        _codesignIdentityOption.AddAlias("-s");
        _compressionLevelOption.AddAlias("--level");
        _codesignEntitlementsOption.AddAlias("-e");
        
        _customBundleIdOption.AddAlias("-b");
        _enableDocumentsSupportOption.AddAlias("-d");
        _removeSupportedDevicesOption.AddAlias("-u");
        _directoriesToRemoveOption.AddAlias("-r");
        
        AddArgument(_targetArgument);
        AddArgument(_outputArgument);
        AddOption(_overwriteOutputOption);
        AddOption(_compressionLevelOption);
        
        AddOption(_itemsOption);
        AddOption(_codesignIdentityOption);
        AddOption(_codesignEntitlementsOption);
        
        AddOption(_customBundleIdOption);
        AddOption(_enableDocumentsSupportOption);
        AddOption(_removeSupportedDevicesOption);
        AddOption(_directoriesToRemoveOption);
        
        this.SetHandler(async (iviParameters, loggerFactory) =>
            {
                var commandProcessor = new IviRootCommandProcessor(loggerFactory);
                await commandProcessor.ProcessRootCommand(iviParameters);
            },
            new IviRootCommandParametersBinder(
                _targetArgument,
                _outputArgument,
                _overwriteOutputOption,
                _compressionLevelOption,
                _itemsOption,
                _codesignIdentityOption,
                _codesignEntitlementsOption,
                _customBundleIdOption,
                _enableDocumentsSupportOption,
                _removeSupportedDevicesOption,
                _directoriesToRemoveOption
            ),
            new LoggerFactoryBinder()
        );
    }
}