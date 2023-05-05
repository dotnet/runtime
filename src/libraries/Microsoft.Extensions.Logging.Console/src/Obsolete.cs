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
    public partial class ConsoleLogScope
    {
        internal ConsoleLogScope() { throw new NotImplementedException(); }

        public static ConsoleLogScope Current { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        public ConsoleLogScope Parent { get { throw new NotImplementedException(); } }

        public static System.IDisposable Push(string name, object state) { throw new NotImplementedException(); }

        public override string ToString() { throw new NotImplementedException(); }
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

namespace Microsoft.Extensions.Logging.Console.Internal
{
    [System.Obsolete("TODO")]
    public partial class AnsiLogConsole : IConsole
    {
        public AnsiLogConsole(IAnsiSystemConsole systemConsole) { throw new NotImplementedException(); }

        public void Flush() { throw new NotImplementedException(); }

        public void Write(string message, System.Nullable<System.ConsoleColor> background, System.Nullable<System.ConsoleColor> foreground) { throw new NotImplementedException(); }

        public void WriteLine(string message, System.Nullable<System.ConsoleColor> background, System.Nullable<System.ConsoleColor> foreground) { throw new NotImplementedException(); }
    }

    [System.Obsolete("TODO")]
    public partial class ConsoleLoggerProcessor
    {
        public IConsole Console;
        public ConsoleLoggerProcessor() { throw new NotImplementedException(); }

        public void Dispose() { throw new NotImplementedException(); }

        public virtual void EnqueueMessage(LogMessageEntry message) { throw new NotImplementedException(); }
    }

    [System.Obsolete("TODO")]
    public partial interface IAnsiSystemConsole
    {
        void Write(string message);
        void WriteLine(string message);
    }

    [System.Obsolete("TODO")]
    public partial interface IConsole
    {
        void Flush();
        void Write(string message, System.Nullable<System.ConsoleColor> background, System.Nullable<System.ConsoleColor> foreground);
        void WriteLine(string message, System.Nullable<System.ConsoleColor> background, System.Nullable<System.ConsoleColor> foreground);
    }

    [System.Obsolete("TODO")]
    public sealed partial class LogMessageEntry
    {
        internal LogMessageEntry() { throw new NotImplementedException(); }

        public System.Nullable<System.ConsoleColor> LevelBackground;
        public System.Nullable<System.ConsoleColor> LevelForeground;
        public string LevelString;
        public string Message;
        public System.Nullable<System.ConsoleColor> MessageColor;
    }

    [System.Obsolete("TODO")]
    public partial class WindowsLogConsole : IConsole
    {
        public WindowsLogConsole() { throw new NotImplementedException(); }

        public void Flush() { throw new NotImplementedException(); }

        public void Write(string message, System.Nullable<System.ConsoleColor> background, System.Nullable<System.ConsoleColor> foreground) { throw new NotImplementedException(); }

        public void WriteLine(string message, System.Nullable<System.ConsoleColor> background, System.Nullable<System.ConsoleColor> foreground) { throw new NotImplementedException(); }
    }
}
