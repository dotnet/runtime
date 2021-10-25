// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class zlib
    {
        [DllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_DeflateInit2_")]
        internal static extern unsafe ZLibNative.ErrorCode DeflateInit2_(
            ZLibNative.ZStream* stream,
            ZLibNative.CompressionLevel level,
            ZLibNative.CompressionMethod method,
            int windowBits,
            int memLevel,
            ZLibNative.CompressionStrategy strategy);

        [DllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_Deflate")]
        internal static extern unsafe ZLibNative.ErrorCode Deflate(ZLibNative.ZStream* stream, ZLibNative.FlushCode flush);

        [DllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_DeflateReset")]
        internal static extern unsafe ZLibNative.ErrorCode DeflateReset(ZLibNative.ZStream* stream);

        [DllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_DeflateEnd")]
        internal static extern unsafe ZLibNative.ErrorCode DeflateEnd(ZLibNative.ZStream* stream);

        [DllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_InflateInit2_")]
        internal static extern unsafe ZLibNative.ErrorCode InflateInit2_(ZLibNative.ZStream* stream, int windowBits);

        [DllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_Inflate")]
        internal static extern unsafe ZLibNative.ErrorCode Inflate(ZLibNative.ZStream* stream, ZLibNative.FlushCode flush);

        [DllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_InflateReset")]
        internal static extern unsafe ZLibNative.ErrorCode InflateReset(ZLibNative.ZStream* stream);

        [DllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_InflateEnd")]
        internal static extern unsafe ZLibNative.ErrorCode InflateEnd(ZLibNative.ZStream* stream);

        [DllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_Crc32")]
        internal static extern unsafe uint crc32(uint crc, byte* buffer, int len);
    }
}
