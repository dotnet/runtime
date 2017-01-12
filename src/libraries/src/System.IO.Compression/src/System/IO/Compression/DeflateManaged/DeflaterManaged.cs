// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


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

using System.Diagnostics;

namespace System.IO.Compression
{
    internal sealed class DeflaterManaged : IDisposable
    {
        private const int MinBlockSize = 256;
        private const int MaxHeaderFooterGoo = 120;
        private const int CleanCopySize = DeflateManagedStream.DefaultBufferSize - MaxHeaderFooterGoo;
        private const double BadCompressionThreshold = 1.0;

        private readonly FastEncoder _deflateEncoder;
        private readonly CopyEncoder _copyEncoder;

        private readonly DeflateInput _input;
        private readonly OutputBuffer _output;
        private DeflaterState _processingState;
        private DeflateInput _inputFromHistory;

        internal DeflaterManaged()
        {
            _deflateEncoder = new FastEncoder();
            _copyEncoder = new CopyEncoder();
            _input = new DeflateInput();
            _output = new OutputBuffer();

            _processingState = DeflaterState.NotStarted;
        }

        internal bool NeedsInput() => _input.Count == 0 && _deflateEncoder.BytesInHistory == 0;

        /// <summary>
        /// Sets the input to compress. The only buffer copy occurs when the input is copied
        /// to the FastEncoderWindow.
        /// </summary>
        internal void SetInput(byte[] inputBuffer, int startIndex, int count)
        {
            Debug.Assert(_input.Count == 0, "We have something left in previous input!");

            _input.Buffer = inputBuffer;
            _input.Count = count;
            _input.StartIndex = startIndex;

            if (count > 0 && count < MinBlockSize)
            {
                // user is writing small buffers. If buffer size is below MinBlockSize, we
                // need to switch to a small data mode, to avoid block headers and footers
                // dominating the output.
                switch (_processingState)
                {
                    case DeflaterState.NotStarted:
                    case DeflaterState.CheckingForIncompressible:
                        // clean states, needs a block header first
                        _processingState = DeflaterState.StartingSmallData;
                        break;

                    case DeflaterState.CompressThenCheck:
                        // already has correct block header
                        _processingState = DeflaterState.HandlingSmallData;
                        break;
                }
            }
        }

        internal int GetDeflateOutput(byte[] outputBuffer)
        {
            Debug.Assert(outputBuffer != null, "Can't pass in a null output buffer!");
            Debug.Assert(!NeedsInput(), "GetDeflateOutput should only be called after providing input");

            _output.UpdateBuffer(outputBuffer);

            switch (_processingState)
            {
                case DeflaterState.NotStarted:
                    {
                        // first call. Try to compress but if we get bad compression ratio, switch to uncompressed blocks.
                        Debug.Assert(_deflateEncoder.BytesInHistory == 0, "have leftover bytes in window");

                        // save these in case we need to switch to uncompressed format
                        DeflateInput.InputState initialInputState = _input.DumpState();
                        OutputBuffer.BufferState initialOutputState = _output.DumpState();

                        _deflateEncoder.GetBlockHeader(_output);
                        _deflateEncoder.GetCompressedData(_input, _output);

                        if (!UseCompressed(_deflateEncoder.LastCompressionRatio))
                        {
                            // we're expanding; restore state and switch to uncompressed
                            _input.RestoreState(initialInputState);
                            _output.RestoreState(initialOutputState);
                            _copyEncoder.GetBlock(_input, _output, isFinal: false);
                            FlushInputWindows();
                            _processingState = DeflaterState.CheckingForIncompressible;
                        }
                        else
                        {
                            _processingState = DeflaterState.CompressThenCheck;
                        }

                        break;
                    }
                case DeflaterState.CompressThenCheck:
                    {
                        // continue assuming data is compressible. If we reach data that indicates otherwise
                        // finish off remaining data in history and decide whether to compress on a
                        // block-by-block basis
                        _deflateEncoder.GetCompressedData(_input, _output);

                        if (!UseCompressed(_deflateEncoder.LastCompressionRatio))
                        {
                            _processingState = DeflaterState.SlowDownForIncompressible1;
                            _inputFromHistory = _deflateEncoder.UnprocessedInput;
                        }
                        break;
                    }
                case DeflaterState.SlowDownForIncompressible1:
                    {
                        // finish off previous compressed block
                        _deflateEncoder.GetBlockFooter(_output);

                        _processingState = DeflaterState.SlowDownForIncompressible2;
                        goto case DeflaterState.SlowDownForIncompressible2; // yeah I know, but there's no fallthrough
                    }

                case DeflaterState.SlowDownForIncompressible2:
                    {
                        // clear out data from history, but add them as uncompressed blocks
                        if (_inputFromHistory.Count > 0)
                        {
                            _copyEncoder.GetBlock(_inputFromHistory, _output, isFinal: false);
                        }

                        if (_inputFromHistory.Count == 0)
                        {
                            // now we're clean
                            _deflateEncoder.FlushInput();
                            _processingState = DeflaterState.CheckingForIncompressible;
                        }
                        break;
                    }

                case DeflaterState.CheckingForIncompressible:
                    {
                        // decide whether to compress on a block-by-block basis
                        Debug.Assert(_deflateEncoder.BytesInHistory == 0, "have leftover bytes in window");

                        // save these in case we need to store as uncompressed
                        DeflateInput.InputState initialInputState = _input.DumpState();
                        OutputBuffer.BufferState initialOutputState = _output.DumpState();

                        // enforce max so we can ensure state between calls
                        _deflateEncoder.GetBlock(_input, _output, CleanCopySize);

                        if (!UseCompressed(_deflateEncoder.LastCompressionRatio))
                        {
                            // we're expanding; restore state and switch to uncompressed
                            _input.RestoreState(initialInputState);
                            _output.RestoreState(initialOutputState);
                            _copyEncoder.GetBlock(_input, _output, isFinal: false);
                            FlushInputWindows();
                        }

                        break;
                    }

                case DeflaterState.StartingSmallData:
                    {
                        // add compressed header and data, but not footer. Subsequent calls will keep
                        // adding compressed data (no header and no footer). We're doing this to
                        // avoid overhead of header and footer size relative to compressed payload.
                        _deflateEncoder.GetBlockHeader(_output);

                        _processingState = DeflaterState.HandlingSmallData;
                        goto case DeflaterState.HandlingSmallData; // yeah I know, but there's no fallthrough
                    }

                case DeflaterState.HandlingSmallData:
                    {
                        // continue adding compressed data
                        _deflateEncoder.GetCompressedData(_input, _output);
                        break;
                    }
            }

            return _output.BytesWritten;
        }

        internal bool Finish(byte[] outputBuffer, out int bytesRead)
        {
            Debug.Assert(outputBuffer != null, "Can't pass in a null output buffer!");
            Debug.Assert(
                _processingState == DeflaterState.NotStarted ||
                _processingState == DeflaterState.CheckingForIncompressible ||
                _processingState == DeflaterState.HandlingSmallData ||
                _processingState == DeflaterState.CompressThenCheck ||
                _processingState == DeflaterState.SlowDownForIncompressible1,
                $"Got unexpected processing state = {_processingState}");

            Debug.Assert(NeedsInput());

            // no need to add end of block info if we didn't write anything
            if (_processingState == DeflaterState.NotStarted)
            {
                bytesRead = 0;
                return true;
            }

            _output.UpdateBuffer(outputBuffer);

            if (_processingState == DeflaterState.CompressThenCheck ||
                _processingState == DeflaterState.HandlingSmallData ||
                _processingState == DeflaterState.SlowDownForIncompressible1)
            {
                // need to finish off block
                _deflateEncoder.GetBlockFooter(_output);
            }

            // write final block
            WriteFinal();
            bytesRead = _output.BytesWritten;
            return true;
        }

        private bool UseCompressed(double ratio) => ratio <= BadCompressionThreshold;

        private void FlushInputWindows() => _deflateEncoder.FlushInput();

        private void WriteFinal() => _copyEncoder.GetBlock(input: null, output: _output, isFinal: true);

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
        // reached a reasonable size, but given that Flush is not implemented on DeflateManagedStream, this meant
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

        public void Dispose() { }
    }
}
