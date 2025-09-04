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
    }
}
