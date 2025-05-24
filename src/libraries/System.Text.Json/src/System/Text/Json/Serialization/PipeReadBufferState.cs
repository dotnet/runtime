// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization
{
    [StructLayout(LayoutKind.Auto)]
    internal struct PipeReadBufferState : IReadBufferState
    {
        private readonly PipeReader _utf8Json;

        private ReadOnlySequence<byte> _sequence;
        private bool _isFinalBlock;
        private bool _isFirstBlock = true;

        public PipeReadBufferState(PipeReader utf8Json)
        {
            _utf8Json = utf8Json;
        }

        public bool IsFinalBlock => _isFinalBlock;

        public ReadOnlySequence<byte> Bytes => _sequence;

        public void Advance(int bytesConsumed)
        {
            _utf8Json.AdvanceTo(_sequence.Slice(bytesConsumed).Start);
            _sequence = default;
        }

        /// <summary>
        /// Read from the PipeReader until either our buffer limit is filled or we hit EOF.
        /// Calling ReadCore is relatively expensive, so we minimize the number of times
        /// we need to call it.
        /// </summary>
        public async ValueTask<IReadBufferState> ReadAsync(CancellationToken cancellationToken, bool fillBuffer = true)
        {
            // Since mutable structs don't work well with async state machines,
            // make all updates on a copy which is returned once complete.
            PipeReadBufferState bufferState = this;

            int minBufferSize = JsonConstants.Utf8Bom.Length;
            ReadResult readResult = await _utf8Json.ReadAtLeastAsync(minBufferSize, cancellationToken).ConfigureAwait(false);

            bufferState._sequence = readResult.Buffer;
            bufferState._isFinalBlock = readResult.IsCompleted; // || readResult.IsCanceled;?
            bufferState.ProcessReadBytes();

            if (readResult.IsCanceled)
            {
                // TODO: Handle cancellation logic here
            }

            return bufferState;
        }

        public void Read() => throw new NotImplementedException();

        private void ProcessReadBytes()
        {
            if (_isFirstBlock)
            {
                _isFirstBlock = false;

                // Handle the UTF-8 BOM if present
                if (_sequence.Length > 0)
                {
                    //Debug.Assert(_sequence.Length >= JsonConstants.Utf8Bom.Length);
                    if (_sequence.First.Length >= JsonConstants.Utf8Bom.Length)
                    {
                        if (_sequence.First.Span.StartsWith(JsonConstants.Utf8Bom))
                        {
                            _sequence = _sequence.Slice((byte)JsonConstants.Utf8Bom.Length);
                        }
                    }
                    else
                    {
                        // TODO
                        //_sequence = _sequence.Slice(JsonConstants.Utf8Bom.Length);
                        //_sequence.PositionOf(JsonConstants.Utf8Bom[0]);
                        //if (_sequence.StartsWith(JsonConstants.Utf8Bom))
                        //{
                        //    _offset = (byte)JsonConstants.Utf8Bom.Length;
                        //}
                    }
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
