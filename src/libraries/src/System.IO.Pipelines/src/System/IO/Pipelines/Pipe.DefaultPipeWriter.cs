// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.IO.Pipelines
{
    /// <summary>
    /// Default <see cref="PipeWriter"/> and <see cref="PipeReader"/> implementation.
    /// </summary>
    public sealed partial class Pipe
    {
        private sealed class DefaultPipeWriter : PipeWriter, IPipeAwaiter<FlushResult>
        {
            private readonly Pipe _pipe;

            public DefaultPipeWriter(Pipe pipe)
            {
                _pipe = pipe;
            }

            public override void Complete(Exception exception = null) => _pipe.CompleteWriter(exception);

            public override void CancelPendingFlush() => _pipe.CancelPendingFlush();

            public override void OnReaderCompleted(Action<Exception, object> callback, object state) => _pipe.OnReaderCompleted(callback, state);

            public override PipeAwaiter<FlushResult> FlushAsync(CancellationToken cancellationToken = default) => _pipe.FlushAsync(cancellationToken);

            public override void Commit() => _pipe.Commit();

            public override void Advance(int bytes) => _pipe.Advance(bytes);

            public override Memory<byte> GetMemory(int minimumLength = 0) => _pipe.GetMemory(minimumLength);

            public override Span<byte> GetSpan(int minimumLength = 0) => _pipe.GetMemory(minimumLength).Span;

            public bool IsCompleted => _pipe.IsFlushAsyncCompleted;

            public FlushResult GetResult() => _pipe.GetFlushAsyncResult();

            public void OnCompleted(Action continuation) => _pipe.OnFlushAsyncCompleted(continuation);
        }
    }
}
