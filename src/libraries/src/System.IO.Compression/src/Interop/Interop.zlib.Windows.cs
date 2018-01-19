// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Compression;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class zlib
    {
        private static readonly byte[] ZLibVersion = { (byte)'1', (byte)'.', (byte)'2', (byte)'.', (byte)'3', 0 };

        [DllImport(Libraries.CompressionNative)]
        private extern static unsafe int deflateInit2_(byte* stream, int level, int method, int windowBits, int memLevel, int strategy,
                                                byte* version, int stream_size);

        [DllImport(Libraries.CompressionNative)]
        private extern static unsafe int deflate(byte* stream, int flush);

        [DllImport(Libraries.CompressionNative)]
        private extern static unsafe int deflateEnd(byte* strm);

        [DllImport(Libraries.CompressionNative)]
        internal extern static unsafe uint crc32(uint crc, byte* buffer, int len);

        [DllImport(Libraries.CompressionNative)]
        private extern static unsafe int inflateInit2_(byte* stream, int windowBits, byte* version, int stream_size);

        [DllImport(Libraries.CompressionNative)]
        private extern static unsafe int inflate(byte* stream, int flush);

        [DllImport(Libraries.CompressionNative)]
        private extern static unsafe int inflateEnd(byte* stream);

        internal static unsafe ZLibNative.ErrorCode DeflateInit2_(
                                            ref ZLibNative.ZStream stream,
                                            ZLibNative.CompressionLevel level,
                                            ZLibNative.CompressionMethod method,
                                            int windowBits,
                                            int memLevel,
                                            ZLibNative.CompressionStrategy strategy)
        {
            fixed (byte* versionString = &ZLibVersion[0])
            fixed (ZLibNative.ZStream* streamBytes = &stream)
            {
                byte* pBytes = (byte*)streamBytes;
                return (ZLibNative.ErrorCode)deflateInit2_(pBytes, (int)level, (int)method, (int)windowBits, (int)memLevel, (int)strategy, versionString, sizeof(ZLibNative.ZStream));
            }
        }

        internal static unsafe ZLibNative.ErrorCode Deflate(ref ZLibNative.ZStream stream, ZLibNative.FlushCode flush)
        {
            fixed (ZLibNative.ZStream* streamBytes = &stream)
            {
                byte* pBytes = (byte*)streamBytes;
                return (ZLibNative.ErrorCode)deflate(pBytes, (int)flush);
            }
        }

        internal static unsafe ZLibNative.ErrorCode DeflateEnd(ref ZLibNative.ZStream stream)
        {
            fixed (ZLibNative.ZStream* streamBytes = &stream)
            {
                byte* pBytes = (byte*)streamBytes;
                return (ZLibNative.ErrorCode)deflateEnd(pBytes);
            }
        }

        internal static unsafe ZLibNative.ErrorCode InflateInit2_(
                                            ref ZLibNative.ZStream stream,
                                            int windowBits)
        {
            fixed (byte* versionString = &ZLibVersion[0])
            fixed (ZLibNative.ZStream* streamBytes = &stream)
            {
                byte* pBytes = (byte*)streamBytes;
                return (ZLibNative.ErrorCode)inflateInit2_(pBytes, (int)windowBits, versionString, sizeof(ZLibNative.ZStream));
            }
        }

        internal static unsafe ZLibNative.ErrorCode Inflate(ref ZLibNative.ZStream stream, ZLibNative.FlushCode flush)
        {
            fixed (ZLibNative.ZStream* streamBytes = &stream)
            {
                byte* pBytes = (byte*)streamBytes;
                return (ZLibNative.ErrorCode)inflate(pBytes, (int)flush);
            }
        }

        internal static unsafe ZLibNative.ErrorCode InflateEnd(ref ZLibNative.ZStream stream)
        {
            fixed (ZLibNative.ZStream* streamBytes = &stream)
            {
                byte* pBytes = (byte*)streamBytes;
                return (ZLibNative.ErrorCode)inflateEnd(pBytes);
            }
        }
    }
}
