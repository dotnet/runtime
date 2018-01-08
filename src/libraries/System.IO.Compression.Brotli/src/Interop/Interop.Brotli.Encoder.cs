// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.IO.Compression;
using Microsoft.Win32.SafeHandles;
using size_t = System.IntPtr;

internal static partial class Interop
{
    internal static partial class Brotli
    {
        internal const string LibNameEncoder = Library.BrotliEncoder;

        [DllImport(LibNameEncoder, CallingConvention = CallingConvention.Cdecl)]
        internal static extern SafeBrotliEncoderHandle BrotliEncoderCreateInstance(IntPtr allocFunc, IntPtr freeFunc, IntPtr opaque);

        [DllImport(LibNameEncoder, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool BrotliEncoderSetParameter(SafeBrotliEncoderHandle state, BrotliEncoderParameter parameter, UInt32 value);

        [DllImport(LibNameEncoder, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool BrotliEncoderCompressStream(
            SafeBrotliEncoderHandle state, BrotliEncoderOperation op, ref size_t availableIn,
            ref IntPtr nextIn, ref size_t availableOut, ref IntPtr nextOut, out size_t totalOut);

        [DllImport(LibNameEncoder, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool BrotliEncoderHasMoreOutput(SafeBrotliEncoderHandle state);

        [DllImport(LibNameEncoder, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void BrotliEncoderDestroyInstance(IntPtr state);

        [DllImport(LibNameEncoder, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool BrotliEncoderCompress(int quality, int window, int v, size_t availableInput, IntPtr inBytes, ref size_t availableOutput, IntPtr outBytes);
    }
}

