// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.IO.Compression
{
    internal static class GZipConstants
    {
        internal const int CompressionLevel_3 = 3;
        internal const int CompressionLevel_10 = 10;

        internal const long FileLengthModulo = 4294967296;

        internal const byte ID1 = 0x1F;
        internal const byte ID2 = 0x8B;
        internal const byte Deflate = 0x8;

        internal const int Xfl_HeaderPos = 8;
        internal const byte Xfl_FastestAlgorithm = 4;
        internal const byte Xfl_MaxCompressionSlowestAlgorithm = 2;
    }

    internal class GZipFormatter : IFileFormatWriter
    {
        private byte[] _headerBytes = new byte[] {
                GZipConstants.ID1,      // ID1
                GZipConstants.ID2,      // ID2
                GZipConstants.Deflate,  // CM = deflate
                0,                      // FLG, no text, no crc, no extra, no name, no comment

            // MTIME (Modification Time) - no time available
            0,
                0,
                0,
                0,

            // XFL
            // 2 = compressor used max compression, slowest algorithm
            // 4 = compressor used fastest algorithm
            GZipConstants.Xfl_FastestAlgorithm,

            // OS: 0 = FAT filesystem (MS-DOS, OS/2, NT/Win32)
            0
            };

        private uint _crc32;
        private long _inputStreamSizeModulo;

        internal GZipFormatter() : this(GZipConstants.CompressionLevel_3) { }

        internal GZipFormatter(int compressionLevel)
        {
            if (compressionLevel == GZipConstants.CompressionLevel_10)
            {
                _headerBytes[GZipConstants.Xfl_HeaderPos] = GZipConstants.Xfl_MaxCompressionSlowestAlgorithm;
            }
        }

        public byte[] GetHeader()
        {
            return _headerBytes;
        }

        public void UpdateWithBytesRead(byte[] buffer, int offset, int bytesToCopy)
        {
            _crc32 = Crc32Helper.UpdateCrc32(_crc32, buffer, offset, bytesToCopy);

            long n = _inputStreamSizeModulo + (uint)bytesToCopy;
            if (n >= GZipConstants.FileLengthModulo)
            {
                n %= GZipConstants.FileLengthModulo;
            }
            _inputStreamSizeModulo = n;
        }

        public byte[] GetFooter()
        {
            byte[] b = new byte[8];

            WriteUInt32(b, _crc32, 0);
            WriteUInt32(b, (uint)_inputStreamSizeModulo, 4);

            return b;
        }

        internal void WriteUInt32(byte[] b, uint value, int startIndex)
        {
            b[startIndex] = (byte)value;
            b[startIndex + 1] = (byte)(value >> 8);
            b[startIndex + 2] = (byte)(value >> 16);
            b[startIndex + 3] = (byte)(value >> 24);
        }
    }
}

