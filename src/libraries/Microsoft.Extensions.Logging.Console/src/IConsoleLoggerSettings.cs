using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Logging.Console
{
    [Obsolete("This type is obsolete and will be removed in a future version. The recommended alternative is ConsoleLoggerOptions.")]
    public interface IConsoleLoggerSettings
    {
        bool IncludeScopes { get; }

        IChangeToken ChangeToken { get; }

        bool TryGetSwitch(string name, out LogLevel level);

        IConsoleLoggerSettings Reload();
    }
}
