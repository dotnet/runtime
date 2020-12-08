// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections.Tests
{
    internal class MockPipeWriter : PipeWriter
    {
        public Action<int> OnAdvance { get; set; }
        public Action OnCancelPendingFlush { get; set; }
        public Action<Exception> OnComplete { get; set; }
        public Func<Exception,ValueTask> OnCompleteAsync { get; set; }
        public Func<CancellationToken,ValueTask<FlushResult>> OnFlushAsync { get; set; }
        public Func<int, Memory<byte>> OnGetMemory { get; set; }

        public override void Advance(int bytes)
            => OnAdvance(bytes);

        public override void CancelPendingFlush()
            => OnCancelPendingFlush();

        public override void Complete(Exception exception = null)
            => OnComplete(exception);

        public override ValueTask CompleteAsync(Exception exception = null)
            => OnCompleteAsync(exception);

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
            => OnFlushAsync(cancellationToken);

        public override Memory<byte> GetMemory(int sizeHint = 0)
            => OnGetMemory(sizeHint);

        public override Span<byte> GetSpan(int sizeHint = 0)
            => GetMemory(sizeHint).Span;
    }
}
