// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging
{
    public static partial class ConsoleLoggerExtensions
    {
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddConsole(this Microsoft.Extensions.Logging.ILoggingBuilder builder) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddConsole(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Action<Microsoft.Extensions.Logging.Console.ConsoleLoggerOptions> configure) { throw null; }
    }
}
namespace Microsoft.Extensions.Logging.Console
{
    public partial class ConsoleLoggerOptions
    {
        public ConsoleLoggerOptions() { }
        public bool DisableColors { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool IncludeScopes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.Extensions.Logging.LogLevel LogToStandardErrorThreshold { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string TimestampFormat { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    [Microsoft.Extensions.Logging.ProviderAliasAttribute("Console")]
    public partial class ConsoleLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider, Microsoft.Extensions.Logging.ISupportExternalScope, System.IDisposable
    {
        public ConsoleLoggerProvider(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.Extensions.Logging.Console.ConsoleLoggerOptions> options) { }
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string name) { throw null; }
        public void Dispose() { }
        public void SetScopeProvider(Microsoft.Extensions.Logging.IExternalScopeProvider scopeProvider) { }
    }
}
