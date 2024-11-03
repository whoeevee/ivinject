using System.CommandLine.Binding;
using Microsoft.Extensions.Logging;

namespace ivinject.Features.Command;

internal class LoggerFactoryBinder : BinderBase<ILoggerFactory>
{
    protected override ILoggerFactory GetBoundValue(BindingContext bindingContext) 
        => GetLoggerFactory();
    private static ILoggerFactory GetLoggerFactory()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole());
        
        return loggerFactory;
    }
}