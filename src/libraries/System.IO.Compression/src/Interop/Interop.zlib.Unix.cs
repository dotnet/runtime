// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class zlib
    {
        [DllImport(Libraries.CompressionNative)]
        internal static extern ZLibNative.ErrorCode DeflateInit2_(
            ref ZLibNative.ZStream stream,
            ZLibNative.CompressionLevel level,
            ZLibNative.CompressionMethod method,
            int windowBits,
            int memLevel,
            ZLibNative.CompressionStrategy strategy);

        [DllImport(Libraries.CompressionNative)]
        internal static extern ZLibNative.ErrorCode Deflate(ref ZLibNative.ZStream stream, ZLibNative.FlushCode flush);

        [DllImport(Libraries.CompressionNative)]
        internal static extern ZLibNative.ErrorCode DeflateEnd(ref ZLibNative.ZStream stream);

        [DllImport(Libraries.CompressionNative)]
        internal static extern ZLibNative.ErrorCode InflateInit2_(ref ZLibNative.ZStream stream, int windowBits);

        [DllImport(Libraries.CompressionNative)]
        internal static extern ZLibNative.ErrorCode Inflate(ref ZLibNative.ZStream stream, ZLibNative.FlushCode flush);

        [DllImport(Libraries.CompressionNative)]
        internal static extern ZLibNative.ErrorCode InflateEnd(ref ZLibNative.ZStream stream);

        internal static unsafe uint crc32(uint crc, byte[] buffer, int offset, int len)
        {
            fixed (byte* buf = &buffer[offset])
                return Crc32(crc, buf, len);
        }

        [DllImport(Libraries.CompressionNative)]
        private static unsafe extern uint Crc32(uint crc, byte* buffer, int len);
    }
}
