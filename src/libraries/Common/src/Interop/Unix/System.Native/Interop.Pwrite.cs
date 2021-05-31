// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Pwrite", SetLastError = true)]
        internal static extern unsafe int Pwrite(SafeHandle fd, byte* buffer, int bufferSize, long fileOffset);
    }
}
