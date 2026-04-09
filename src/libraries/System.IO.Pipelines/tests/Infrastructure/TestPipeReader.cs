// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    public class TestPipeReader : PipeReader
    {
        private readonly PipeReader _inner;

        public TestPipeReader(PipeReader inner)
        {
            _inner = inner;
        }

        public ReadResult LastReadResult { get; private set; }
        public SequencePosition LastConsumed { get; private set; }
        public SequencePosition LastExamined { get; private set; }

        public override void AdvanceTo(SequencePosition consumed)
        {
            LastConsumed = consumed;
            LastExamined = consumed;
            _inner.AdvanceTo(consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            LastConsumed = consumed;
            LastExamined = examined;
            _inner.AdvanceTo(consumed);
        }

        public override void CancelPendingRead()
        {
            _inner.CancelPendingRead();
        }

        public override void Complete(Exception exception = null)
        {
            _inner.Complete(exception);
        }

        public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            LastReadResult = await _inner.ReadAsync(cancellationToken);
            return LastReadResult;
        }

        public override bool TryRead(out ReadResult result)
        {
            if (_inner.TryRead(out result))
            {
                LastReadResult = result;
                return true;
            }

            return false;
        }
    }
}
