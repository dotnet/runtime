// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_Log")]
        internal static unsafe partial void Log(byte* buffer, int count);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LogError")]
        internal static unsafe partial void LogError(byte* buffer, int count);
    }
}
