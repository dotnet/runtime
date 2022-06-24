// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static unsafe partial class Interop
{
    internal static partial class Shell32
    {
        [LibraryImport(Libraries.Shell32, EntryPoint = "CommandLineToArgvW")]
        internal static partial char** CommandLineToArgv(char* lpCommandLine, int* pNumArgs);
    }
}
