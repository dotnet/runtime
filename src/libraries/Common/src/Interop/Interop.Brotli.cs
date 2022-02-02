// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.IO.Compression;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Brotli
    {
        [GeneratedDllImport(Libraries.CompressionNative)]
        internal static partial SafeBrotliDecoderHandle BrotliDecoderCreateInstance(IntPtr allocFunc, IntPtr freeFunc, IntPtr opaque);

        [GeneratedDllImport(Libraries.CompressionNative)]
        internal static unsafe partial int BrotliDecoderDecompressStream(
            SafeBrotliDecoderHandle state, ref nuint availableIn, byte** nextIn,
            ref nuint availableOut, byte** nextOut, out nuint totalOut);

        [GeneratedDllImport(Libraries.CompressionNative)]
        internal static unsafe partial BOOL BrotliDecoderDecompress(nuint availableInput, byte* inBytes, nuint* availableOutput, byte* outBytes);

        [GeneratedDllImport(Libraries.CompressionNative)]
        internal static partial void BrotliDecoderDestroyInstance(IntPtr state);

        [GeneratedDllImport(Libraries.CompressionNative)]
        internal static partial BOOL BrotliDecoderIsFinished(SafeBrotliDecoderHandle state);

        [GeneratedDllImport(Libraries.CompressionNative)]
        internal static partial SafeBrotliEncoderHandle BrotliEncoderCreateInstance(IntPtr allocFunc, IntPtr freeFunc, IntPtr opaque);

        [GeneratedDllImport(Libraries.CompressionNative)]
        internal static partial BOOL BrotliEncoderSetParameter(SafeBrotliEncoderHandle state, BrotliEncoderParameter parameter, uint value);

        [GeneratedDllImport(Libraries.CompressionNative)]
        internal static unsafe partial BOOL BrotliEncoderCompressStream(
            SafeBrotliEncoderHandle state, BrotliEncoderOperation op, ref nuint availableIn,
            byte** nextIn, ref nuint availableOut, byte** nextOut, out nuint totalOut);

        [GeneratedDllImport(Libraries.CompressionNative)]
        internal static partial BOOL BrotliEncoderHasMoreOutput(SafeBrotliEncoderHandle state);

        [GeneratedDllImport(Libraries.CompressionNative)]
        internal static partial void BrotliEncoderDestroyInstance(IntPtr state);

        [GeneratedDllImport(Libraries.CompressionNative)]
        internal static unsafe partial BOOL BrotliEncoderCompress(int quality, int window, int v, nuint availableInput, byte* inBytes, nuint* availableOutput, byte* outBytes);
    }
}
