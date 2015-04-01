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
        private GzipHeaderState gzipHeaderSubstate;
        private GzipHeaderState gzipFooterSubstate;

        private int gzip_header_flag;
        private int gzip_header_xlen;
        private uint expectedCrc32;
        private uint expectedOutputStreamSizeModulo;
        private int loopCounter;
        private uint actualCrc32;
        private long actualStreamSizeModulo;

        public GZipDecoder()
        {
            Reset();
        }

        public void Reset()
        {
            gzipHeaderSubstate = GzipHeaderState.ReadingID1;
            gzipFooterSubstate = GzipHeaderState.ReadingCRC;
            expectedCrc32 = 0;
            expectedOutputStreamSizeModulo = 0;
        }

        public bool ReadHeader(InputBuffer input)
        {
            while (true)
            {
                int bits;
                switch (gzipHeaderSubstate)
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
                        gzipHeaderSubstate = GzipHeaderState.ReadingID2;
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

                        gzipHeaderSubstate = GzipHeaderState.ReadingCM;
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

                        gzipHeaderSubstate = GzipHeaderState.ReadingFLG; ;
                        goto case GzipHeaderState.ReadingFLG;

                    case GzipHeaderState.ReadingFLG:
                        bits = input.GetBits(8);
                        if (bits < 0)
                        {
                            return false;
                        }

                        gzip_header_flag = bits;
                        gzipHeaderSubstate = GzipHeaderState.ReadingMMTime;
                        loopCounter = 0; // 4 MMTIME bytes
                        goto case GzipHeaderState.ReadingMMTime;

                    case GzipHeaderState.ReadingMMTime:
                        bits = 0;
                        while (loopCounter < 4)
                        {
                            bits = input.GetBits(8);
                            if (bits < 0)
                            {
                                return false;
                            }

                            loopCounter++;
                        }

                        gzipHeaderSubstate = GzipHeaderState.ReadingXFL;
                        loopCounter = 0;
                        goto case GzipHeaderState.ReadingXFL;

                    case GzipHeaderState.ReadingXFL:      // ignore XFL
                        bits = input.GetBits(8);
                        if (bits < 0)
                        {
                            return false;
                        }

                        gzipHeaderSubstate = GzipHeaderState.ReadingOS;
                        goto case GzipHeaderState.ReadingOS;

                    case GzipHeaderState.ReadingOS:      // ignore OS
                        bits = input.GetBits(8);
                        if (bits < 0)
                        {
                            return false;
                        }

                        gzipHeaderSubstate = GzipHeaderState.ReadingXLen1;
                        goto case GzipHeaderState.ReadingXLen1;

                    case GzipHeaderState.ReadingXLen1:
                        if ((gzip_header_flag & (int)GZipOptionalHeaderFlags.ExtraFieldsFlag) == 0)
                        {
                            goto case GzipHeaderState.ReadingFileName;
                        }

                        bits = input.GetBits(8);
                        if (bits < 0)
                        {
                            return false;
                        }

                        gzip_header_xlen = bits;
                        gzipHeaderSubstate = GzipHeaderState.ReadingXLen2;
                        goto case GzipHeaderState.ReadingXLen2;

                    case GzipHeaderState.ReadingXLen2:
                        bits = input.GetBits(8);
                        if (bits < 0)
                        {
                            return false;
                        }

                        gzip_header_xlen |= (bits << 8);
                        gzipHeaderSubstate = GzipHeaderState.ReadingXLenData;
                        loopCounter = 0; // 0 bytes of XLEN data read so far
                        goto case GzipHeaderState.ReadingXLenData;

                    case GzipHeaderState.ReadingXLenData:
                        bits = 0;
                        while (loopCounter < gzip_header_xlen)
                        {
                            bits = input.GetBits(8);
                            if (bits < 0)
                            {
                                return false;
                            }

                            loopCounter++;
                        }
                        gzipHeaderSubstate = GzipHeaderState.ReadingFileName;
                        loopCounter = 0;
                        goto case GzipHeaderState.ReadingFileName;

                    case GzipHeaderState.ReadingFileName:
                        if ((gzip_header_flag & (int)GZipOptionalHeaderFlags.FileNameFlag) == 0)
                        {
                            gzipHeaderSubstate = GzipHeaderState.ReadingComment;
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

                        gzipHeaderSubstate = GzipHeaderState.ReadingComment;
                        goto case GzipHeaderState.ReadingComment;

                    case GzipHeaderState.ReadingComment:
                        if ((gzip_header_flag & (int)GZipOptionalHeaderFlags.CommentFlag) == 0)
                        {
                            gzipHeaderSubstate = GzipHeaderState.ReadingCRC16Part1;
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

                        gzipHeaderSubstate = GzipHeaderState.ReadingCRC16Part1;
                        goto case GzipHeaderState.ReadingCRC16Part1;

                    case GzipHeaderState.ReadingCRC16Part1:
                        if ((gzip_header_flag & (int)GZipOptionalHeaderFlags.CRCFlag) == 0)
                        {
                            gzipHeaderSubstate = GzipHeaderState.Done;
                            goto case GzipHeaderState.Done;
                        }

                        bits = input.GetBits(8);     // ignore crc
                        if (bits < 0)
                        {
                            return false;
                        }

                        gzipHeaderSubstate = GzipHeaderState.ReadingCRC16Part2;
                        goto case GzipHeaderState.ReadingCRC16Part2;

                    case GzipHeaderState.ReadingCRC16Part2:
                        bits = input.GetBits(8);     // ignore crc
                        if (bits < 0)
                        {
                            return false;
                        }

                        gzipHeaderSubstate = GzipHeaderState.Done;
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
            if (gzipFooterSubstate == GzipHeaderState.ReadingCRC)
            {
                while (loopCounter < 4)
                {
                    int bits = input.GetBits(8);
                    if (bits < 0)
                    {
                        return false;
                    }

                    expectedCrc32 |= ((uint)bits << (8 * loopCounter));
                    loopCounter++;
                }
                gzipFooterSubstate = GzipHeaderState.ReadingFileSize;
                loopCounter = 0;
            }

            if (gzipFooterSubstate == GzipHeaderState.ReadingFileSize)
            {
                if (loopCounter == 0)
                    expectedOutputStreamSizeModulo = 0;

                while (loopCounter < 4)
                {
                    int bits = input.GetBits(8);
                    if (bits < 0)
                    {
                        return false;
                    }

                    expectedOutputStreamSizeModulo |= ((uint)bits << (8 * loopCounter));
                    loopCounter++;
                }
            }

            return true;
        }

        public void UpdateWithBytesRead(byte[] buffer, int offset, int copied)
        {
            actualCrc32 = Crc32Helper.UpdateCrc32(actualCrc32, buffer, offset, copied);

            long n = actualStreamSizeModulo + (uint)copied;
            if (n >= GZipConstants.FileLengthModulo)
            {
                n %= GZipConstants.FileLengthModulo;
            }
            actualStreamSizeModulo = n;
        }

        public void Validate()
        {
            if (expectedCrc32 != actualCrc32)
            {
                throw new InvalidDataException(SR.InvalidCRC);
            }

            if (actualStreamSizeModulo != expectedOutputStreamSizeModulo)
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
