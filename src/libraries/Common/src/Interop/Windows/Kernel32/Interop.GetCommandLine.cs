// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static unsafe partial class Interop
{
    internal static partial class Kernel32
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
#if NET
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
        [LibraryImport(Libraries.Kernel32, EntryPoint = "GetCommandLineW")]
        internal static partial char* GetCommandLine();
    }
}
