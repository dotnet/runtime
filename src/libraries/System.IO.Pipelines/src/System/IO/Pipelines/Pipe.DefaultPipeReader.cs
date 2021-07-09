// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.IO.Pipelines
{
    /// <summary>The default <see cref="System.IO.Pipelines.PipeWriter" /> and <see cref="System.IO.Pipelines.PipeReader" /> implementation.</summary>
    public sealed partial class Pipe
    {
        private sealed class DefaultPipeReader : PipeReader, IValueTaskSource<ReadResult>
        {
            private readonly Pipe _pipe;

            public DefaultPipeReader(Pipe pipe)
            {
                _pipe = pipe;
            }

            public override bool TryRead(out ReadResult result) => _pipe.TryRead(out result);

            public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default) => _pipe.ReadAsync(cancellationToken);

            protected override ValueTask<ReadResult> ReadAtLeastAsyncCore(int minimumBytes, CancellationToken cancellationToken) => _pipe.ReadAtLeastAsync(minimumBytes, cancellationToken);

            public override void AdvanceTo(SequencePosition consumed) => _pipe.AdvanceReader(consumed);

            public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) => _pipe.AdvanceReader(consumed, examined);

            public override void CancelPendingRead() => _pipe.CancelPendingRead();

            public override void Complete(Exception? exception = null) => _pipe.CompleteReader(exception);

#pragma warning disable CS0672 // Member overrides obsolete member
            public override void OnWriterCompleted(Action<Exception?, object?> callback, object? state) => _pipe.OnWriterCompleted(callback, state);
#pragma warning restore CS0672 // Member overrides obsolete member

            public ValueTaskSourceStatus GetStatus(short token) => _pipe.GetReadAsyncStatus();

            public ReadResult GetResult(short token) => _pipe.GetReadAsyncResult();

            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _pipe.OnReadAsyncCompleted(continuation, state, flags);
        }
    }
}
