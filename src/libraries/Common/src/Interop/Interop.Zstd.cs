// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Zstd
    {
        // Compression context management
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial SafeZstdCompressHandle ZSTD_createCCtx();

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_freeCCtx(IntPtr cctx);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial SafeZstdDecompressHandle ZSTD_createDCtx();

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_freeDCtx(IntPtr dctx);

        // Dictionary management
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial SafeZstdCDictHandle ZSTD_createCDict(IntPtr dictBuffer, nuint dictSize, int compressionLevel);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_freeCDict(IntPtr cdict);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial SafeZstdDDictHandle ZSTD_createDDict(IntPtr dictBuffer, nuint dictSize);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_freeDDict(IntPtr ddict);

        // Compression functions
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_compressBound(nuint srcSize);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_compress(IntPtr dst, nuint dstCapacity, IntPtr src, nuint srcSize, int compressionLevel);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_compressCCtx(SafeZstdCompressHandle cctx, IntPtr dst, nuint dstCapacity, IntPtr src, nuint srcSize, int compressionLevel);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_compress_usingCDict(SafeZstdCompressHandle cctx, IntPtr dst, nuint dstCapacity, IntPtr src, nuint srcSize, SafeZstdCDictHandle cdict);

        // Decompression functions
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_decompress(IntPtr dst, nuint dstCapacity, IntPtr src, nuint srcSize);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_decompressDCtx(SafeZstdDecompressHandle dctx, IntPtr dst, nuint dstCapacity, IntPtr src, nuint srcSize);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_decompress_usingDDict(SafeZstdDecompressHandle dctx, IntPtr dst, nuint dstCapacity, IntPtr src, nuint srcSize, SafeZstdDDictHandle ddict);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial ulong ZSTD_getFrameContentSize(IntPtr src, nuint srcSize);

        // Streaming decompression
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_decompressStream(SafeZstdDecompressHandle dctx, ref ZstdOutBuffer output, ref ZstdInBuffer input);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_DCtx_reset(SafeZstdDecompressHandle dctx, ZstdResetDirective reset);

        // Streaming compression
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_compressStream2(SafeZstdCompressHandle cctx, ref ZstdOutBuffer output, ref ZstdInBuffer input, ZstdEndDirective endOp);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_CCtx_setParameter(SafeZstdCompressHandle cctx, ZstdCParameter param, int value);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_CCtx_reset(SafeZstdCompressHandle cctx, ZstdResetDirective reset);

        // Compression level functions
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial int ZSTD_minCLevel();

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial int ZSTD_maxCLevel();

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial int ZSTD_defaultCLevel();

        // Error checking
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial uint ZSTD_isError(nuint result);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial IntPtr ZSTD_getErrorName(nuint result);

        // Dictionary context functions
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_DCtx_refDDict(SafeZstdDecompressHandle dctx, SafeZstdDDictHandle ddict);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_CCtx_refCDict(SafeZstdCompressHandle cctx, SafeZstdCDictHandle cdict);

        // Enums and structures for streaming
        internal enum ZstdEndDirective
        {
            ZSTD_e_continue = 0,
            ZSTD_e_flush = 1,
            ZSTD_e_end = 2
        }

        internal enum ZstdCParameter
        {
            ZSTD_c_compressionLevel = 100,
            ZSTD_c_windowLog = 101,
            ZSTD_c_hashLog = 102,
            ZSTD_c_chainLog = 103,
            ZSTD_c_searchLog = 104,
            ZSTD_c_minMatch = 105,
            ZSTD_c_targetLength = 106,
            ZSTD_c_strategy = 107
        }

        internal enum ZstdResetDirective
        {
            ZSTD_reset_session_only = 1,
            ZSTD_reset_parameters = 2,
            ZSTD_reset_session_and_parameters = 3
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ZstdInBuffer
        {
            internal IntPtr src;
            internal nuint size;
            internal nuint pos;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ZstdOutBuffer
        {
            internal IntPtr dst;
            internal nuint size;
            internal nuint pos;
        }

        internal sealed class ZstdNativeException : Exception
        {
            public ZstdNativeException(string message) : base(message) { }

            public static void ThrowIfError(nuint result, string message)
            {
                if (ZstandardUtils.IsError(result))
                {
                    throw new ZstdNativeException(SR.Format(message, ZstandardUtils.GetErrorMessage(result)));
                }
            }
        }
    }
}
