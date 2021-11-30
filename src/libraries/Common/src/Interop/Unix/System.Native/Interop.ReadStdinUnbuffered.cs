// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_ReadStdin", SetLastError = true)]
        internal static unsafe partial int ReadStdin(byte* buffer, int bufferSize);

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_InitializeConsoleBeforeRead")]
        internal static partial void InitializeConsoleBeforeRead(byte minChars = 1, byte decisecondsTimeout = 0);

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_UninitializeConsoleAfterRead")]
        internal static partial void UninitializeConsoleAfterRead();
    }
}
