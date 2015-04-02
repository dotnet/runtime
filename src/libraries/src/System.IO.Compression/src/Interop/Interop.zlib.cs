// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.IO.Compression;

internal static partial class Interop
{
    internal static partial class zlib
    {
        [DllImport(Libraries.Zlib)]
        private extern unsafe static int deflateInit2_(byte* stream, int level, int method, int windowBits, int memLevel, int strategy,
                                                byte* version, int stream_size);

        [DllImport(Libraries.Zlib)]
        private extern unsafe static int deflate(byte* stream, int flush);

        [DllImport(Libraries.Zlib)]
        private extern unsafe static int deflateEnd(byte* strm);

        [DllImport(Libraries.Zlib)]
        private extern unsafe static int inflateInit2_(byte* stream, int windowBits, byte* version, int stream_size);

        [DllImport(Libraries.Zlib)]
        private extern unsafe static int inflate(byte* stream, int flush);

        [DllImport(Libraries.Zlib)]
        private extern unsafe static int inflateEnd(byte* stream);

        [DllImport(Libraries.Zlib)]
        internal extern static int zlibCompileFlags();

        internal static unsafe ZLibNative.ErrorCode DeflateInit2_(
                                            ref ZLibNative.ZStream stream,
                                            ZLibNative.CompressionLevel level,
                                            ZLibNative.CompressionMethod method,
                                            int windowBits,
                                            int memLevel,
                                            ZLibNative.CompressionStrategy strategy,
                                            byte[] version)
        {
            fixed (byte* versionString = version)
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
                                            int windowBits,
                                            byte[] version)
        {
            fixed (byte* versionString = version)
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
