// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.IO.Compression
{
    internal sealed class CopyEncoder
    {
        /// <summary>
        /// padding for copy encoder formatting
        ///  - 1 byte for header
        ///  - 4 bytes for len, nlen
        /// </summary>
        private const int PaddingSize = 5;

        private const int MaxUncompressedBlockSize = 65536;

        /// <summary>
        /// null input means write an empty payload with formatting info. This is needed for the final block.
        /// </summary>
        public void GetBlock(DeflateInput input, OutputBuffer output, bool isFinal)
        {
            Debug.Assert(output != null);
            Debug.Assert(output.FreeBytes >= PaddingSize);

            // determine number of bytes to write
            int count = 0;
            if (input != null)
            {
                // allow space for padding and bits not yet flushed to buffer
                count = Math.Min(input.Count, output.FreeBytes - PaddingSize - output.BitsInBuffer);

                // we don't expect the output buffer to ever be this big (currently 4K), but we'll check this
                // just in case that changes.
                if (count > MaxUncompressedBlockSize - PaddingSize)
                {
                    count = MaxUncompressedBlockSize - PaddingSize;
                }
            }

            // write header and flush bits
            if (isFinal)
            {
                output.WriteBits(
                    FastEncoderStatics.BFinalNoCompressionHeaderBitCount,
                    FastEncoderStatics.BFinalNoCompressionHeader);
            }
            else
            {
                output.WriteBits(
                    FastEncoderStatics.NoCompressionHeaderBitCount,
                    FastEncoderStatics.NoCompressionHeader);
            }

            // now we're aligned
            output.FlushBits();

            // write len, nlen
            WriteLenNLen((ushort)count, output);

            // write uncompressed bytes
            if (input != null && count > 0)
            {
                output.WriteBytes(input.Buffer, input.StartIndex, count);
                input.ConsumeBytes(count);
            }
        }

        private void WriteLenNLen(ushort len, OutputBuffer output)
        {
            // len
            output.WriteUInt16(len);

            // nlen
            ushort onesComp = (ushort)(~(ushort)len);
            output.WriteUInt16(onesComp);
        }
    }
}
