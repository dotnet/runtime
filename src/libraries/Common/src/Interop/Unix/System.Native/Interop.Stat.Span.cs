// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Sys
    {
        // Unix max paths are typically 1K or 4K UTF-8 bytes, 256 should handle the majority of paths
        // without putting too much pressure on the stack.
        private const int StackBufferSize = 256;

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Stat", SetLastError = true)]
        internal static extern int Stat(ref byte path, out FileStatus output);

        internal static int Stat(ReadOnlySpan<char> path, out FileStatus output)
        {
            var converter = new ValueUtf8Converter(stackalloc byte[StackBufferSize]);
            int result = Stat(ref MemoryMarshal.GetReference(converter.ConvertAndTerminateString(path)), out output);
            converter.Dispose();
            return result;
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_LStat", SetLastError = true)]
        internal static extern int LStat(ref byte path, out FileStatus output);

        internal static int LStat(ReadOnlySpan<char> path, out FileStatus output)
        {
            var converter = new ValueUtf8Converter(stackalloc byte[StackBufferSize]);
            int result = LStat(ref MemoryMarshal.GetReference(converter.ConvertAndTerminateString(path)), out output);
            converter.Dispose();
            return result;
        }
    }
}
