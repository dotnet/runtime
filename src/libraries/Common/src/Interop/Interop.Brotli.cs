// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.IO.Compression;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static class Brotli
    {
        [DllImport(Libraries.CompressionNative)]
        internal static extern SafeBrotliDecoderHandle BrotliDecoderCreateInstance(IntPtr allocFunc, IntPtr freeFunc, IntPtr opaque);

        [DllImport(Libraries.CompressionNative)]
        internal static extern unsafe int BrotliDecoderDecompressStream(
            SafeBrotliDecoderHandle state, ref nuint availableIn, byte** nextIn,
            ref nuint availableOut, byte** nextOut, out nuint totalOut);

        [DllImport(Libraries.CompressionNative)]
        internal static extern unsafe bool BrotliDecoderDecompress(nuint availableInput, byte* inBytes, ref nuint availableOutput, byte* outBytes);

        [DllImport(Libraries.CompressionNative)]
        internal static extern void BrotliDecoderDestroyInstance(IntPtr state);

        [DllImport(Libraries.CompressionNative)]
        internal static extern bool BrotliDecoderIsFinished(SafeBrotliDecoderHandle state);

        [DllImport(Libraries.CompressionNative)]
        internal static extern SafeBrotliEncoderHandle BrotliEncoderCreateInstance(IntPtr allocFunc, IntPtr freeFunc, IntPtr opaque);

        [DllImport(Libraries.CompressionNative)]
        internal static extern bool BrotliEncoderSetParameter(SafeBrotliEncoderHandle state, BrotliEncoderParameter parameter, uint value);

        [DllImport(Libraries.CompressionNative)]
        internal static extern unsafe bool BrotliEncoderCompressStream(
            SafeBrotliEncoderHandle state, BrotliEncoderOperation op, ref nuint availableIn,
            byte** nextIn, ref nuint availableOut, byte** nextOut, out nuint totalOut);

        [DllImport(Libraries.CompressionNative)]
        internal static extern bool BrotliEncoderHasMoreOutput(SafeBrotliEncoderHandle state);

        [DllImport(Libraries.CompressionNative)]
        internal static extern void BrotliEncoderDestroyInstance(IntPtr state);

        [DllImport(Libraries.CompressionNative)]
        internal static extern unsafe bool BrotliEncoderCompress(int quality, int window, int v, nuint availableInput, byte* inBytes, ref nuint availableOutput, byte* outBytes);
    }
}
