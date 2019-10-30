// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Compression;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class zlib
    {
        [DllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_DeflateInit2_")]
        internal static extern ZLibNative.ErrorCode DeflateInit2_(
            ref ZLibNative.ZStream stream,
            ZLibNative.CompressionLevel level,
            ZLibNative.CompressionMethod method,
            int windowBits,
            int memLevel,
            ZLibNative.CompressionStrategy strategy);

        [DllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_Deflate")]
        internal static extern ZLibNative.ErrorCode Deflate(ref ZLibNative.ZStream stream, ZLibNative.FlushCode flush);

        [DllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_DeflateEnd")]
        internal static extern ZLibNative.ErrorCode DeflateEnd(ref ZLibNative.ZStream stream);

        [DllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_InflateInit2_")]
        internal static extern ZLibNative.ErrorCode InflateInit2_(ref ZLibNative.ZStream stream, int windowBits);

        [DllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_Inflate")]
        internal static extern ZLibNative.ErrorCode Inflate(ref ZLibNative.ZStream stream, ZLibNative.FlushCode flush);

        [DllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_InflateEnd")]
        internal static extern ZLibNative.ErrorCode InflateEnd(ref ZLibNative.ZStream stream);

        [DllImport(Libraries.CompressionNative, EntryPoint = "CompressionNative_Crc32")]
        internal static extern unsafe uint crc32(uint crc, byte* buffer, int len);
    }
}
