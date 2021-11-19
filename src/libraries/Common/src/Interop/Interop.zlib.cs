// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class zlib
    {
        [GeneratedDllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_DeflateInit2_")]
        internal static unsafe partial ZLibNative.ErrorCode DeflateInit2_(
            ZLibNative.ZStream* stream,
            ZLibNative.CompressionLevel level,
            ZLibNative.CompressionMethod method,
            int windowBits,
            int memLevel,
            ZLibNative.CompressionStrategy strategy);

        [GeneratedDllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_Deflate")]
        internal static unsafe partial ZLibNative.ErrorCode Deflate(ZLibNative.ZStream* stream, ZLibNative.FlushCode flush);

        [GeneratedDllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_DeflateReset")]
        internal static unsafe partial ZLibNative.ErrorCode DeflateReset(ZLibNative.ZStream* stream);

        [GeneratedDllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_DeflateEnd")]
        internal static unsafe partial ZLibNative.ErrorCode DeflateEnd(ZLibNative.ZStream* stream);

        [GeneratedDllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_InflateInit2_")]
        internal static unsafe partial ZLibNative.ErrorCode InflateInit2_(ZLibNative.ZStream* stream, int windowBits);

        [GeneratedDllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_Inflate")]
        internal static unsafe partial ZLibNative.ErrorCode Inflate(ZLibNative.ZStream* stream, ZLibNative.FlushCode flush);

        [GeneratedDllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_InflateReset")]
        internal static unsafe partial ZLibNative.ErrorCode InflateReset(ZLibNative.ZStream* stream);

        [GeneratedDllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_InflateEnd")]
        internal static unsafe partial ZLibNative.ErrorCode InflateEnd(ZLibNative.ZStream* stream);

        [GeneratedDllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_Crc32")]
        internal static unsafe partial uint crc32(uint crc, byte* buffer, int len);
    }
}
