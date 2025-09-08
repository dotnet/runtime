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
        internal static partial SafeZStdCompressHandle ZSTD_createCCtx();

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_freeCCtx(IntPtr cctx);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial SafeZStdDecompressHandle ZSTD_createDCtx();

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_freeDCtx(IntPtr dctx);

        // Dictionary management
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial SafeZStdCDictHandle ZSTD_createCDict(IntPtr dictBuffer, nuint dictSize, int compressionLevel);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_freeCDict(IntPtr cdict);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial SafeZStdDDictHandle ZSTD_createDDict(IntPtr dictBuffer, nuint dictSize);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_freeDDict(IntPtr ddict);

        // Compression functions
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_compressBound(nuint srcSize);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_compress(IntPtr dst, nuint dstCapacity, IntPtr src, nuint srcSize, int compressionLevel);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_compressCCtx(SafeZStdCompressHandle cctx, IntPtr dst, nuint dstCapacity, IntPtr src, nuint srcSize, int compressionLevel);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_compress_usingCDict(SafeZStdCompressHandle cctx, IntPtr dst, nuint dstCapacity, IntPtr src, nuint srcSize, SafeZStdCDictHandle cdict);

        // Streaming compression
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_compressStream2(SafeZStdCompressHandle cctx, ref ZStdOutBuffer output, ref ZStdInBuffer input, ZStdEndDirective endOp);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_CCtx_setParameter(SafeZStdCompressHandle cctx, ZStdCParameter param, int value);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_CCtx_reset(SafeZStdCompressHandle cctx, ZStdResetDirective reset);

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
    }

    // Enums and structures for streaming
    internal enum ZStdEndDirective
    {
        ZSTD_e_continue = 0,
        ZSTD_e_flush = 1,
        ZSTD_e_end = 2
    }

    internal enum ZStdCParameter
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

    internal enum ZStdResetDirective
    {
        ZSTD_reset_session_only = 1,
        ZSTD_reset_parameters = 2,
        ZSTD_reset_session_and_parameters = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ZStdInBuffer
    {
        internal IntPtr src;
        internal nuint size;
        internal nuint pos;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ZStdOutBuffer
    {
        internal IntPtr dst;
        internal nuint size;
        internal nuint pos;
    }
}
