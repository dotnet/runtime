// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging.Console
{

    [System.Obsolete("TODO")]
    public partial class ConfigurationConsoleLoggerSettings : IConsoleLoggerSettings
    {
        public ConfigurationConsoleLoggerSettings(Extensions.Configuration.IConfiguration configuration) { throw new NotImplementedException(); }

        public Extensions.Primitives.IChangeToken ChangeToken { get { throw new NotImplementedException(); } }

        public bool IncludeScopes { get { throw new NotImplementedException(); } }

        public IConsoleLoggerSettings Reload() { throw new NotImplementedException(); }

        public bool TryGetSwitch(string name, out Logging.LogLevel level) { throw new NotImplementedException(); }
    }

    [System.Obsolete("TODO")]
    public partial class ConsoleLoggerSettings : IConsoleLoggerSettings
    {
        public ConsoleLoggerSettings() { throw new NotImplementedException(); }

        public Extensions.Primitives.IChangeToken ChangeToken { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        public bool DisableColors { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        public bool IncludeScopes { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        public System.Collections.Generic.IDictionary<string, Logging.LogLevel> Switches { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        public IConsoleLoggerSettings Reload() { throw new NotImplementedException(); }

        public bool TryGetSwitch(string name, out Logging.LogLevel level) { throw new NotImplementedException(); }
    }

    [System.Obsolete("TODO")]
    public partial interface IConsoleLoggerSettings
    {
        Extensions.Primitives.IChangeToken ChangeToken { get; }

        bool IncludeScopes { get; }

        IConsoleLoggerSettings Reload();
        bool TryGetSwitch(string name, out Logging.LogLevel level);
    }
}
