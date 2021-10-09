// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    internal sealed class SequencePipeReader : PipeReader
    {
        private ReadOnlySequence<byte> _sequence;
        private bool _isReaderCompleted;

        private int _cancelNext;

        public SequencePipeReader(ReadOnlySequence<byte> sequence)
        {
            _sequence = sequence;
        }

        /// <inheritdoc />
        public override void AdvanceTo(SequencePosition consumed)
        {
            AdvanceTo(consumed, consumed);
        }

        /// <inheritdoc />
        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            ThrowIfCompleted();

            // Fast path: did we consume everything?
            if (consumed.Equals(_sequence.End))
            {
                _sequence = ReadOnlySequence<byte>.Empty;
                return;
            }

            _sequence = _sequence.Slice(consumed);
        }

        /// <inheritdoc />
        public override void CancelPendingRead()
        {
            Interlocked.Exchange(ref _cancelNext, 1);
        }

        /// <inheritdoc />
        public override void Complete(Exception? exception = null)
        {
            if (_isReaderCompleted)
            {
                return;
            }

            _isReaderCompleted = true;
            _sequence = ReadOnlySequence<byte>.Empty;
        }

        /// <inheritdoc />
        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (TryRead(out ReadResult result))
            {
                return new ValueTask<ReadResult>(result);
            }

            result = new ReadResult(ReadOnlySequence<byte>.Empty, isCanceled: false, isCompleted: true);
            return new ValueTask<ReadResult>(result);
        }

        /// <inheritdoc />
        public override bool TryRead(out ReadResult result)
        {
            ThrowIfCompleted();

            bool isCancellationRequested = Interlocked.Exchange(ref _cancelNext, 0) == 1;
            if (isCancellationRequested || _sequence.Length > 0)
            {
                result = new ReadResult(_sequence, isCancellationRequested, isCompleted: true);
                return true;
            }

            result = default;
            return false;
        }

        private void ThrowIfCompleted()
        {
            if (_isReaderCompleted)
            {
                ThrowHelper.ThrowInvalidOperationException_NoReadingAllowed();
            }
        }
    }
}
