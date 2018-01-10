// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using size_t = System.IntPtr;

internal static partial class Interop
{
    internal static partial class Brotli
    {
        [DllImport(Libraries.CompressionNative)]
        internal static extern SafeBrotliDecoderHandle BrotliDecoderCreateInstance(IntPtr allocFunc, IntPtr freeFunc, IntPtr opaque);

        [DllImport(Libraries.CompressionNative)]
        internal static extern unsafe int BrotliDecoderDecompressStream(
            SafeBrotliDecoderHandle state, ref size_t availableIn, byte** nextIn,
            ref size_t availableOut, byte** nextOut, out size_t totalOut);

        [DllImport(Libraries.CompressionNative)]
        internal static extern unsafe bool BrotliDecoderDecompress(size_t availableInput, byte* inBytes, ref size_t availableOutput, byte* outBytes);

        [DllImport(Libraries.CompressionNative)]
        internal static extern void BrotliDecoderDestroyInstance(IntPtr state);

        [DllImport(Libraries.CompressionNative)]
        internal static extern bool BrotliDecoderIsFinished(SafeBrotliDecoderHandle state);
    }
}

