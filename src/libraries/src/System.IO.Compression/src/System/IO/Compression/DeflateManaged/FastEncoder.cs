// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.IO.Compression
{
    internal sealed class FastEncoder
    {
        private readonly FastEncoderWindow _inputWindow; // input history window
        private readonly Match _currentMatch;            // current match in history window
        private double _lastCompressionRatio;

        public FastEncoder()
        {
            _inputWindow = new FastEncoderWindow();
            _currentMatch = new Match();
        }

        internal int BytesInHistory => _inputWindow.BytesAvailable;

        internal DeflateInput UnprocessedInput => _inputWindow.UnprocessedInput;

        internal void FlushInput() => _inputWindow.FlushWindow();

        internal double LastCompressionRatio => _lastCompressionRatio;

        // Copy the compressed bytes to output buffer as a block. maxBytesToCopy limits the number of
        // bytes we can copy from input. Set to any value < 1 if no limit
        internal void GetBlock(DeflateInput input, OutputBuffer output, int maxBytesToCopy)
        {
            Debug.Assert(InputAvailable(input), "call SetInput before trying to compress!");

            WriteDeflatePreamble(output);
            GetCompressedOutput(input, output, maxBytesToCopy);
            WriteEndOfBlock(output);
        }

        // Compress data but don't format as block (doesn't have header and footer)
        internal void GetCompressedData(DeflateInput input, OutputBuffer output) =>
            GetCompressedOutput(input, output, maxBytesToCopy:- 1);

        internal void GetBlockHeader(OutputBuffer output) => WriteDeflatePreamble(output);

        internal void GetBlockFooter(OutputBuffer output) => WriteEndOfBlock(output);

        // maxBytesToCopy limits the number of bytes we can copy from input. Set to any value < 1 if no limit
        private void GetCompressedOutput(DeflateInput input, OutputBuffer output, int maxBytesToCopy)
        {
            // snapshot for compression ratio stats
            int bytesWrittenPre = output.BytesWritten;
            int bytesConsumedFromInput = 0;
            int inputBytesPre = BytesInHistory + input.Count;

            do
            {
                // read more input data into the window if there is space available
                int bytesToCopy = (input.Count < _inputWindow.FreeWindowSpace) ?
                                         input.Count : _inputWindow.FreeWindowSpace;
                if (maxBytesToCopy >= 1)
                {
                    bytesToCopy = Math.Min(bytesToCopy, maxBytesToCopy - bytesConsumedFromInput);
                }
                if (bytesToCopy > 0)
                {
                    // copy data into history window
                    _inputWindow.CopyBytes(input.Buffer, input.StartIndex, bytesToCopy);
                    input.ConsumeBytes(bytesToCopy);
                    bytesConsumedFromInput += bytesToCopy;
                }

                GetCompressedOutput(output);
            } while (SafeToWriteTo(output) && InputAvailable(input) && (maxBytesToCopy < 1 || bytesConsumedFromInput < maxBytesToCopy));

            // determine compression ratio, save
            int bytesWrittenPost = output.BytesWritten;
            int bytesWritten = bytesWrittenPost - bytesWrittenPre;
            int inputBytesPost = BytesInHistory + input.Count;
            int totalBytesConsumed = inputBytesPre - inputBytesPost;
            if (bytesWritten != 0)
            {
                _lastCompressionRatio = (double)bytesWritten / (double)totalBytesConsumed;
            }
        }

        // compress the bytes in input history window
        private void GetCompressedOutput(OutputBuffer output)
        {
            while (_inputWindow.BytesAvailable > 0 && SafeToWriteTo(output))
            {
                // Find next match. A match can be a symbol,
                // a distance/length pair, a symbol followed by a distance/Length pair
                _inputWindow.GetNextSymbolOrMatch(_currentMatch);

                if (_currentMatch.State == MatchState.HasSymbol)
                {
                    WriteChar(_currentMatch.Symbol, output);
                }
                else if (_currentMatch.State == MatchState.HasMatch)
                {
                    WriteMatch(_currentMatch.Length, _currentMatch.Position, output);
                }
                else
                {
                    WriteChar(_currentMatch.Symbol, output);
                    WriteMatch(_currentMatch.Length, _currentMatch.Position, output);
                }
            }
        }

        private bool InputAvailable(DeflateInput input) => input.Count > 0 || BytesInHistory > 0;

        // Can we safely continue writing to output buffer
        private bool SafeToWriteTo(OutputBuffer output) => output.FreeBytes > FastEncoderStatics.MaxCodeLen;

        private void WriteEndOfBlock(OutputBuffer output)
        {
            // The fast encoder outputs one long block, so it just needs to terminate this block
            const int EndOfBlockCode = 256;
            uint code_info = FastEncoderStatics.FastEncoderLiteralCodeInfo[EndOfBlockCode];
            int code_len = (int)(code_info & 31);
            output.WriteBits(code_len, code_info >> 5);
        }

        internal static void WriteMatch(int matchLen, int matchPos, OutputBuffer output)
        {
            Debug.Assert(matchLen >= FastEncoderWindow.MinMatch && matchLen <= FastEncoderWindow.MaxMatch, "Illegal currentMatch length!");

            // Get the code information for a match code
            uint codeInfo = FastEncoderStatics.FastEncoderLiteralCodeInfo[(FastEncoderStatics.NumChars + 1 - FastEncoderWindow.MinMatch) + matchLen];
            int codeLen = (int)codeInfo & 31;
            Debug.Assert(codeLen != 0, "Invalid Match Length!");
            if (codeLen <= 16)
            {
                output.WriteBits(codeLen, codeInfo >> 5);
            }
            else
            {
                output.WriteBits(16, (codeInfo >> 5) & 65535);
                output.WriteBits(codeLen - 16, codeInfo >> (5 + 16));
            }

            // Get the code information for a distance code
            codeInfo = FastEncoderStatics.FastEncoderDistanceCodeInfo[FastEncoderStatics.GetSlot(matchPos)];
            output.WriteBits((int)(codeInfo & 15), codeInfo >> 8);
            int extraBits = (int)(codeInfo >> 4) & 15;
            if (extraBits != 0)
            {
                output.WriteBits(extraBits, (uint)matchPos & FastEncoderStatics.BitMask[extraBits]);
            }
        }

        internal static void WriteChar(byte b, OutputBuffer output)
        {
            uint code = FastEncoderStatics.FastEncoderLiteralCodeInfo[b];
            output.WriteBits((int)code & 31, code >> 5);
        }

        // Output the block type and tree structure for our hard-coded trees.
        // Contains following data:
        //  "final" block flag 1 bit
        //  BLOCKTYPE_DYNAMIC 2 bits
        //  FastEncoderLiteralTreeLength
        //  FastEncoderDistanceTreeLength
        //
        internal static void WriteDeflatePreamble(OutputBuffer output)
        {
            //Debug.Assert( bitCount == 0, "bitCount must be zero before writing tree bit!");

            output.WriteBytes(FastEncoderStatics.FastEncoderTreeStructureData, 0, FastEncoderStatics.FastEncoderTreeStructureData.Length);
            output.WriteBits(FastEncoderStatics.FastEncoderPostTreeBitCount, FastEncoderStatics.FastEncoderPostTreeBitBuf);
        }
    }
}
