// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_MkDir", SetLastError = true)]
        private static partial int MkDir(ref byte path, int mode);

        internal static int MkDir(ReadOnlySpan<char> path, int mode)
        {
            using ValueUtf8Converter converter = new(stackalloc byte[DefaultPathBufferSize]);
            int result = MkDir(ref MemoryMarshal.GetReference(converter.ConvertAndTerminateString(path)), mode);
            return result;
        }
    }
}
