// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public static partial class AppContext
    {
        public static string BaseDirectory { get { throw null; } }
        public static string? TargetFrameworkName { get { throw null; } }
        public static event System.EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs>? FirstChanceException { add { } remove { } }
        public static event System.EventHandler? ProcessExit { add { } remove { } }
        public static event System.UnhandledExceptionEventHandler? UnhandledException { add { } remove { } }
        public static object? GetData(string name) { throw null; }
        public static void SetData(string name, object? data) { }
        public static void SetSwitch(string switchName, bool isEnabled) { }
        public static bool TryGetSwitch(string switchName, out bool isEnabled) { throw null; }
    }
}
