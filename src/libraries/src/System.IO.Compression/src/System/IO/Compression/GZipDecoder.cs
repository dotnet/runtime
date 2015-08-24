// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.IO.Compression
{
    // This class decodes GZip header and footer information.
    // See RFC 1952 for details about the format.
    internal class GZipDecoder : IFileFormatReader
    {
        private GzipHeaderState _gzipHeaderSubstate;
        private GzipHeaderState _gzipFooterSubstate;

        private int _gzip_header_flag;
        private int _gzip_header_xlen;
        private uint _expectedCrc32;
        private uint _expectedOutputStreamSizeModulo;
        private int _loopCounter;
        private uint _actualCrc32;
        private long _actualStreamSizeModulo;

        public int ZLibWindowSize
        {
            get
            {
                // Since we don't necessarily know what WindowSize the stream was compressed with,
                // we use the upper bound of (32..47) as the WindowSize for decompression. This enables
                // detection for Zlib and GZip headers
                return 47;
            }
        }

        public GZipDecoder()
        {
            Reset();
        }

        public void Reset()
        {
            _gzipHeaderSubstate = GzipHeaderState.ReadingID1;
            _gzipFooterSubstate = GzipHeaderState.ReadingCRC;
            _expectedCrc32 = 0;
            _expectedOutputStreamSizeModulo = 0;
        }

        public bool ReadHeader(InputBuffer input)
        {
            while (true)
            {
                int bits;
                switch (_gzipHeaderSubstate)
                {
                    case GzipHeaderState.ReadingID1:
                        bits = input.GetBits(8);
                        if (bits < 0)
                        {
                            return false;
                        }

                        if (bits != GZipConstants.ID1)
                        {
                            throw new InvalidDataException(SR.CorruptedGZipHeader);
                        }
                        _gzipHeaderSubstate = GzipHeaderState.ReadingID2;
                        goto case GzipHeaderState.ReadingID2;

                    case GzipHeaderState.ReadingID2:
                        bits = input.GetBits(8);
                        if (bits < 0)
                        {
                            return false;
                        }

                        if (bits != GZipConstants.ID2)
                        {
                            throw new InvalidDataException(SR.CorruptedGZipHeader);
                        }

                        _gzipHeaderSubstate = GzipHeaderState.ReadingCM;
                        goto case GzipHeaderState.ReadingCM;

                    case GzipHeaderState.ReadingCM:
                        bits = input.GetBits(8);
                        if (bits < 0)
                        {
                            return false;
                        }

                        if (bits != GZipConstants.Deflate)
                        {         // compression mode must be 8 (deflate)
                            throw new InvalidDataException(SR.UnknownCompressionMode);
                        }

                        _gzipHeaderSubstate = GzipHeaderState.ReadingFLG; ;
                        goto case GzipHeaderState.ReadingFLG;

                    case GzipHeaderState.ReadingFLG:
                        bits = input.GetBits(8);
                        if (bits < 0)
                        {
                            return false;
                        }

                        _gzip_header_flag = bits;
                        _gzipHeaderSubstate = GzipHeaderState.ReadingMMTime;
                        _loopCounter = 0; // 4 MMTIME bytes
                        goto case GzipHeaderState.ReadingMMTime;

                    case GzipHeaderState.ReadingMMTime:
                        bits = 0;
                        while (_loopCounter < 4)
                        {
                            bits = input.GetBits(8);
                            if (bits < 0)
                            {
                                return false;
                            }

                            _loopCounter++;
                        }

                        _gzipHeaderSubstate = GzipHeaderState.ReadingXFL;
                        _loopCounter = 0;
                        goto case GzipHeaderState.ReadingXFL;

                    case GzipHeaderState.ReadingXFL:      // ignore XFL
                        bits = input.GetBits(8);
                        if (bits < 0)
                        {
                            return false;
                        }

                        _gzipHeaderSubstate = GzipHeaderState.ReadingOS;
                        goto case GzipHeaderState.ReadingOS;

                    case GzipHeaderState.ReadingOS:      // ignore OS
                        bits = input.GetBits(8);
                        if (bits < 0)
                        {
                            return false;
                        }

                        _gzipHeaderSubstate = GzipHeaderState.ReadingXLen1;
                        goto case GzipHeaderState.ReadingXLen1;

                    case GzipHeaderState.ReadingXLen1:
                        if ((_gzip_header_flag & (int)GZipOptionalHeaderFlags.ExtraFieldsFlag) == 0)
                        {
                            goto case GzipHeaderState.ReadingFileName;
                        }

                        bits = input.GetBits(8);
                        if (bits < 0)
                        {
                            return false;
                        }

                        _gzip_header_xlen = bits;
                        _gzipHeaderSubstate = GzipHeaderState.ReadingXLen2;
                        goto case GzipHeaderState.ReadingXLen2;

                    case GzipHeaderState.ReadingXLen2:
                        bits = input.GetBits(8);
                        if (bits < 0)
                        {
                            return false;
                        }

                        _gzip_header_xlen |= (bits << 8);
                        _gzipHeaderSubstate = GzipHeaderState.ReadingXLenData;
                        _loopCounter = 0; // 0 bytes of XLEN data read so far
                        goto case GzipHeaderState.ReadingXLenData;

                    case GzipHeaderState.ReadingXLenData:
                        bits = 0;
                        while (_loopCounter < _gzip_header_xlen)
                        {
                            bits = input.GetBits(8);
                            if (bits < 0)
                            {
                                return false;
                            }

                            _loopCounter++;
                        }
                        _gzipHeaderSubstate = GzipHeaderState.ReadingFileName;
                        _loopCounter = 0;
                        goto case GzipHeaderState.ReadingFileName;

                    case GzipHeaderState.ReadingFileName:
                        if ((_gzip_header_flag & (int)GZipOptionalHeaderFlags.FileNameFlag) == 0)
                        {
                            _gzipHeaderSubstate = GzipHeaderState.ReadingComment;
                            goto case GzipHeaderState.ReadingComment;
                        }

                        do
                        {
                            bits = input.GetBits(8);
                            if (bits < 0)
                            {
                                return false;
                            }

                            if (bits == 0)
                            {   // see '\0' in the file name string
                                break;
                            }
                        } while (true);

                        _gzipHeaderSubstate = GzipHeaderState.ReadingComment;
                        goto case GzipHeaderState.ReadingComment;

                    case GzipHeaderState.ReadingComment:
                        if ((_gzip_header_flag & (int)GZipOptionalHeaderFlags.CommentFlag) == 0)
                        {
                            _gzipHeaderSubstate = GzipHeaderState.ReadingCRC16Part1;
                            goto case GzipHeaderState.ReadingCRC16Part1;
                        }

                        do
                        {
                            bits = input.GetBits(8);
                            if (bits < 0)
                            {
                                return false;
                            }

                            if (bits == 0)
                            {   // see '\0' in the file name string
                                break;
                            }
                        } while (true);

                        _gzipHeaderSubstate = GzipHeaderState.ReadingCRC16Part1;
                        goto case GzipHeaderState.ReadingCRC16Part1;

                    case GzipHeaderState.ReadingCRC16Part1:
                        if ((_gzip_header_flag & (int)GZipOptionalHeaderFlags.CRCFlag) == 0)
                        {
                            _gzipHeaderSubstate = GzipHeaderState.Done;
                            goto case GzipHeaderState.Done;
                        }

                        bits = input.GetBits(8);     // ignore crc
                        if (bits < 0)
                        {
                            return false;
                        }

                        _gzipHeaderSubstate = GzipHeaderState.ReadingCRC16Part2;
                        goto case GzipHeaderState.ReadingCRC16Part2;

                    case GzipHeaderState.ReadingCRC16Part2:
                        bits = input.GetBits(8);     // ignore crc
                        if (bits < 0)
                        {
                            return false;
                        }

                        _gzipHeaderSubstate = GzipHeaderState.Done;
                        goto case GzipHeaderState.Done;

                    case GzipHeaderState.Done:
                        return true;
                    default:
                        Debug.Assert(false, "We should not reach unknown state!");
                        throw new InvalidDataException(SR.UnknownState);
                }
            }
        }

        public bool ReadFooter(InputBuffer input)
        {
            input.SkipToByteBoundary();
            if (_gzipFooterSubstate == GzipHeaderState.ReadingCRC)
            {
                while (_loopCounter < 4)
                {
                    int bits = input.GetBits(8);
                    if (bits < 0)
                    {
                        return false;
                    }

                    _expectedCrc32 |= ((uint)bits << (8 * _loopCounter));
                    _loopCounter++;
                }
                _gzipFooterSubstate = GzipHeaderState.ReadingFileSize;
                _loopCounter = 0;
            }

            if (_gzipFooterSubstate == GzipHeaderState.ReadingFileSize)
            {
                if (_loopCounter == 0)
                    _expectedOutputStreamSizeModulo = 0;

                while (_loopCounter < 4)
                {
                    int bits = input.GetBits(8);
                    if (bits < 0)
                    {
                        return false;
                    }

                    _expectedOutputStreamSizeModulo |= ((uint)bits << (8 * _loopCounter));
                    _loopCounter++;
                }
            }

            return true;
        }

        public void UpdateWithBytesRead(byte[] buffer, int offset, int copied)
        {
            _actualCrc32 = Crc32Helper.UpdateCrc32(_actualCrc32, buffer, offset, copied);

            long n = _actualStreamSizeModulo + (uint)copied;
            if (n >= GZipConstants.FileLengthModulo)
            {
                n %= GZipConstants.FileLengthModulo;
            }
            _actualStreamSizeModulo = n;
        }

        public void Validate()
        {
            if (_expectedCrc32 != _actualCrc32)
            {
                throw new InvalidDataException(SR.InvalidCRC);
            }

            if (_actualStreamSizeModulo != _expectedOutputStreamSizeModulo)
            {
                throw new InvalidDataException(SR.InvalidStreamSize);
            }
        }

        internal enum GzipHeaderState
        {
            // GZIP header
            ReadingID1,
            ReadingID2,
            ReadingCM,
            ReadingFLG,
            ReadingMMTime, // iterates 4 times
            ReadingXFL,
            ReadingOS,
            ReadingXLen1,
            ReadingXLen2,
            ReadingXLenData,
            ReadingFileName,
            ReadingComment,
            ReadingCRC16Part1,
            ReadingCRC16Part2,
            Done, // done reading GZIP header

            // GZIP footer
            ReadingCRC, // iterates 4 times
            ReadingFileSize // iterates 4 times
        }

        [Flags]
        internal enum GZipOptionalHeaderFlags
        {
            CRCFlag = 2,
            ExtraFieldsFlag = 4,
            FileNameFlag = 8,
            CommentFlag = 16
        }
    }
}
