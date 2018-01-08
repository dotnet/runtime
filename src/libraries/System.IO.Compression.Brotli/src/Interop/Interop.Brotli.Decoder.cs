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
        internal const string LibNameDecoder = Library.BrotliDecoder;

        [DllImport(LibNameDecoder, CallingConvention = CallingConvention.Cdecl)]
        internal static extern SafeBrotliDecoderHandle BrotliDecoderCreateInstance(IntPtr allocFunc, IntPtr freeFunc, IntPtr opaque);

        [DllImport(LibNameDecoder, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int BrotliDecoderDecompressStream(
            SafeBrotliDecoderHandle state, ref size_t availableIn, ref IntPtr nextIn,
            ref size_t availableOut, ref IntPtr nextOut, out size_t totalOut);

        [DllImport(LibNameDecoder, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool BrotliDecoderDecompress(size_t availableInput, IntPtr inBytes, ref size_t availableOutput, IntPtr outBytes);

        [DllImport(LibNameDecoder, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void BrotliDecoderDestroyInstance(IntPtr state);

        [DllImport(LibNameDecoder, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool BrotliDecoderIsFinished(SafeBrotliDecoderHandle state);
    }
}

