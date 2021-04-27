// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections.Tests
{
    internal class MockPipeReader : PipeReader
    {
        public Action<SequencePosition, SequencePosition> OnAdvanceTo { get; set; }
        public Action OnCancelPendingRead { get; set; }
        public Action<Exception> OnComplete { get; set; }
        public Func<Exception,ValueTask> OnCompleteAsync { get; set; }
        public Func<CancellationToken, ValueTask<ReadResult>> OnReadAsync { get; set; }
        public Func<ReadResult?> OnTryRead { get; set; }

        public override void AdvanceTo(SequencePosition consumed)
            => OnAdvanceTo(consumed, consumed);

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
            => OnAdvanceTo(consumed, examined);

        public override void CancelPendingRead()
            => OnCancelPendingRead();

        public override void Complete(Exception exception = null)
            => OnComplete(exception);

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
            => OnReadAsync(cancellationToken);

        public override bool TryRead(out ReadResult result)
        {
            ReadResult? r = OnTryRead();
            result = r.GetValueOrDefault();
            return r.HasValue;
        }

        public override ValueTask CompleteAsync(Exception exception = null)
            => OnCompleteAsync(exception);
    }
}
