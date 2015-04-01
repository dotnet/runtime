// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//

//
//  zlib.h -- interface of the 'zlib' general purpose compression library
//  version 1.2.1, November 17th, 2003
//
//  Copyright (C) 1995-2003 Jean-loup Gailly and Mark Adler
//
//  This software is provided 'as-is', without any express or implied
//  warranty.  In no event will the authors be held liable for any damages
//  arising from the use of this software.
//
//  Permission is granted to anyone to use this software for any purpose,
//  including commercial applications, and to alter it and redistribute it
//  freely, subject to the following restrictions:
//
//  1. The origin of this software must not be misrepresented; you must not
//     claim that you wrote the original software. If you use this software
//     in a product, an acknowledgment in the product documentation would be
//     appreciated but is not required.
//  2. Altered source versions must be plainly marked as such, and must not be
//     misrepresented as being the original software.
//  3. This notice may not be removed or altered from any source distribution.
//
//

// Compression engine

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace System.IO.Compression
{
    internal class DeflaterManaged : IDeflater
    {
        private const int MinBlockSize = 256;
        private const int MaxHeaderFooterGoo = 120;
        private const int CleanCopySize = DeflateStream.DefaultBufferSize - MaxHeaderFooterGoo;
        private const double BadCompressionThreshold = 1.0;

        private FastEncoder deflateEncoder;
        private CopyEncoder copyEncoder;

        private DeflateInput input;
        private OutputBuffer output;
        private DeflaterState processingState;
        private DeflateInput inputFromHistory;

        internal DeflaterManaged()
        {
            deflateEncoder = new FastEncoder();
            copyEncoder = new CopyEncoder();
            input = new DeflateInput();
            output = new OutputBuffer();

            processingState = DeflaterState.NotStarted;
        }

        private bool NeedsInput()
        {
            // Convenience method to call NeedsInput privately without a cast.
            return ((IDeflater)this).NeedsInput();
        }

        bool IDeflater.NeedsInput()
        {
            return input.Count == 0 && deflateEncoder.BytesInHistory == 0;
        }

        // Sets the input to compress. The only buffer copy occurs when the input is copied
        // to the FastEncoderWindow
        void IDeflater.SetInput(byte[] inputBuffer, int startIndex, int count)
        {
            Debug.Assert(input.Count == 0, "We have something left in previous input!");

            input.Buffer = inputBuffer;
            input.Count = count;
            input.StartIndex = startIndex;

            if (count > 0 && count < MinBlockSize)
            {
                // user is writing small buffers. If buffer size is below MinBlockSize, we
                // need to switch to a small data mode, to avoid block headers and footers 
                // dominating the output.
                switch (processingState)
                {
                    case DeflaterState.NotStarted:
                    case DeflaterState.CheckingForIncompressible:
                        // clean states, needs a block header first
                        processingState = DeflaterState.StartingSmallData;
                        break;
                    case DeflaterState.CompressThenCheck:
                        // already has correct block header
                        processingState = DeflaterState.HandlingSmallData;
                        break;
                }
            }
        }

        int IDeflater.GetDeflateOutput(byte[] outputBuffer)
        {
            Debug.Assert(outputBuffer != null, "Can't pass in a null output buffer!");
            Debug.Assert(!NeedsInput(), "GetDeflateOutput should only be called after providing input");

            output.UpdateBuffer(outputBuffer);

            switch (processingState)
            {
                case DeflaterState.NotStarted:
                    {
                        // first call. Try to compress but if we get bad compression ratio, switch to uncompressed blocks. 
                        Debug.Assert(deflateEncoder.BytesInHistory == 0, "have leftover bytes in window");

                        // save these in case we need to switch to uncompressed format
                        DeflateInput.InputState initialInputState = input.DumpState();
                        OutputBuffer.BufferState initialOutputState = output.DumpState();

                        deflateEncoder.GetBlockHeader(output);
                        deflateEncoder.GetCompressedData(input, output);

                        if (!UseCompressed(deflateEncoder.LastCompressionRatio))
                        {
                            // we're expanding; restore state and switch to uncompressed
                            input.RestoreState(initialInputState);
                            output.RestoreState(initialOutputState);
                            copyEncoder.GetBlock(input, output, false);
                            FlushInputWindows();
                            processingState = DeflaterState.CheckingForIncompressible;
                        }
                        else
                        {
                            processingState = DeflaterState.CompressThenCheck;
                        }

                        break;
                    }
                case DeflaterState.CompressThenCheck:
                    {
                        // continue assuming data is compressible. If we reach data that indicates otherwise
                        // finish off remaining data in history and decide whether to compress on a 
                        // block-by-block basis
                        deflateEncoder.GetCompressedData(input, output);

                        if (!UseCompressed(deflateEncoder.LastCompressionRatio))
                        {
                            processingState = DeflaterState.SlowDownForIncompressible1;
                            inputFromHistory = deflateEncoder.UnprocessedInput;
                        }
                        break;
                    }
                case DeflaterState.SlowDownForIncompressible1:
                    {
                        // finish off previous compressed block
                        deflateEncoder.GetBlockFooter(output);

                        processingState = DeflaterState.SlowDownForIncompressible2;
                        goto case DeflaterState.SlowDownForIncompressible2; // yeah I know, but there's no fallthrough
                    }

                case DeflaterState.SlowDownForIncompressible2:
                    {
                        // clear out data from history, but add them as uncompressed blocks
                        if (inputFromHistory.Count > 0)
                        {
                            copyEncoder.GetBlock(inputFromHistory, output, false);
                        }

                        if (inputFromHistory.Count == 0)
                        {
                            // now we're clean
                            deflateEncoder.FlushInput();
                            processingState = DeflaterState.CheckingForIncompressible;
                        }
                        break;
                    }

                case DeflaterState.CheckingForIncompressible:
                    {
                        // decide whether to compress on a block-by-block basis
                        Debug.Assert(deflateEncoder.BytesInHistory == 0, "have leftover bytes in window");

                        // save these in case we need to store as uncompressed
                        DeflateInput.InputState initialInputState = input.DumpState();
                        OutputBuffer.BufferState initialOutputState = output.DumpState();

                        // enforce max so we can ensure state between calls
                        deflateEncoder.GetBlock(input, output, CleanCopySize);

                        if (!UseCompressed(deflateEncoder.LastCompressionRatio))
                        {
                            // we're expanding; restore state and switch to uncompressed
                            input.RestoreState(initialInputState);
                            output.RestoreState(initialOutputState);
                            copyEncoder.GetBlock(input, output, false);
                            FlushInputWindows();
                        }

                        break;
                    }

                case DeflaterState.StartingSmallData:
                    {
                        // add compressed header and data, but not footer. Subsequent calls will keep 
                        // adding compressed data (no header and no footer). We're doing this to 
                        // avoid overhead of header and footer size relative to compressed payload.
                        deflateEncoder.GetBlockHeader(output);

                        processingState = DeflaterState.HandlingSmallData;
                        goto case DeflaterState.HandlingSmallData; // yeah I know, but there's no fallthrough
                    }

                case DeflaterState.HandlingSmallData:
                    {
                        // continue adding compressed data
                        deflateEncoder.GetCompressedData(input, output);
                        break;
                    }
            }

            return output.BytesWritten;
        }

        bool IDeflater.Finish(byte[] outputBuffer, out int bytesRead)
        {
            Debug.Assert(outputBuffer != null, "Can't pass in a null output buffer!");
            Debug.Assert(processingState == DeflaterState.NotStarted ||
                            processingState == DeflaterState.CheckingForIncompressible ||
                            processingState == DeflaterState.HandlingSmallData ||
                            processingState == DeflaterState.CompressThenCheck ||
                            processingState == DeflaterState.SlowDownForIncompressible1,
                            "got unexpected processing state = " + processingState);

            Debug.Assert(NeedsInput());

            // no need to add end of block info if we didn't write anything
            if (processingState == DeflaterState.NotStarted)
            {
                bytesRead = 0;
                return true;
            }

            output.UpdateBuffer(outputBuffer);

            if (processingState == DeflaterState.CompressThenCheck ||
                        processingState == DeflaterState.HandlingSmallData ||
                        processingState == DeflaterState.SlowDownForIncompressible1)
            {
                // need to finish off block
                deflateEncoder.GetBlockFooter(output);
            }

            // write final block
            WriteFinal();
            bytesRead = output.BytesWritten;
            return true;
        }

        void IDisposable.Dispose() { }
        protected void Dispose(bool disposing) { }

        // Is compression ratio under threshold?
        private bool UseCompressed(double ratio)
        {
            return (ratio <= BadCompressionThreshold);
        }

        private void FlushInputWindows()
        {
            deflateEncoder.FlushInput();
        }

        private void WriteFinal()
        {
            copyEncoder.GetBlock(null, output, true);
        }

        // These states allow us to assume that data is compressible and keep compression ratios at least
        // as good as historical values, but switch to different handling if that approach may increase the
        // data. If we detect we're getting a bad compression ratio, we switch to CheckingForIncompressible
        // state and decide to compress on a block by block basis. 
        //
        // If we're getting small data buffers, we want to avoid overhead of excessive header and footer 
        // info, so we add one header and keep adding blocks as compressed. This means that if the user uses
        // small buffers, they won't get the "don't increase size" improvements.
        //
        // An earlier iteration of this fix handled that data separately by buffering this data until it 
        // reached a reasonable size, but given that Flush is not implemented on DeflateStream, this meant
        // data could be flushed only on Dispose. In the future, it would be reasonable to revisit this, in
        // case this isn't breaking.
        //
        // NotStarted                 -> CheckingForIncompressible, CompressThenCheck, StartingSmallData
        // CompressThenCheck          -> SlowDownForIncompressible1
        // SlowDownForIncompressible1 -> SlowDownForIncompressible2
        // SlowDownForIncompressible2 -> CheckingForIncompressible
        // StartingSmallData          -> HandlingSmallData
        private enum DeflaterState
        {
            // no bytes to write yet
            NotStarted,

            // transient states
            SlowDownForIncompressible1,
            SlowDownForIncompressible2,
            StartingSmallData,

            // stable state: may transition to CheckingForIncompressible (via transient states) if it 
            // appears we're expanding data
            CompressThenCheck,

            // sink states
            CheckingForIncompressible,
            HandlingSmallData
        }
    }  // internal class DeflaterManaged
}  // namespace System.IO.Compression
