// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    internal class SequencePipeReader : PipeReader
    {
        private ReadOnlySequence<byte> _sequence;
        private bool _isReaderCompleted;

        private CancellationTokenSource? _internalTokenSource;
        private readonly object _lock = new object();

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

        private CancellationTokenSource InternalTokenSource
        {
            get
            {
                lock (_lock)
                {
                    if (_internalTokenSource == null)
                    {
                        _internalTokenSource = new CancellationTokenSource();
                    }
                    return _internalTokenSource;
                }
            }
        }

        /// <inheritdoc />
        public override void CancelPendingRead()
        {
            InternalTokenSource.Cancel();
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
            ThrowIfCompleted();

            if (TryReadInternal(InternalTokenSource, out var result))
            {
                return new ValueTask<ReadResult>(result);
            }

            result = new ReadResult(_sequence, isCanceled: false, isCompleted: true);
            return new ValueTask<ReadResult>(result);
        }

        /// <inheritdoc />
        public override bool TryRead(out ReadResult result)
        {
            ThrowIfCompleted();

            return TryReadInternal(InternalTokenSource, out result);
        }

        private bool TryReadInternal(CancellationTokenSource source, out ReadResult result)
        {
            bool isCancellationRequested = source.IsCancellationRequested;
            if (isCancellationRequested || _sequence.Length > 0)
            {
                if (isCancellationRequested)
                {
                    ClearCancellationToken();
                }

                result = new ReadResult(_sequence, isCancellationRequested, isCompleted: true);
                return true;
            }

            result = default;
            return false;
        }

        private void ClearCancellationToken()
        {
            lock (_lock)
            {
                _internalTokenSource = null;
            }
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
