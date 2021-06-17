// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines.Tests
{
    // This is a PipeReader implementation that does not override any of the virtual methods.
    // The intent is to test the base implementation without having to rewrite the base functionality
    // of the PipeReader.
    public class BasePipeReader : PipeReader
    {
        private readonly PipeReader _reader;

        public BasePipeReader(PipeReader reader)
        {
            _reader = reader;
        }

        public override void AdvanceTo(SequencePosition consumed) => _reader.AdvanceTo(consumed);
        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) => _reader.AdvanceTo(consumed, examined);
        public override void CancelPendingRead() => _reader.CancelPendingRead();
        public override void Complete(Exception? exception = null) => _reader.Complete(exception);
        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default) => _reader.ReadAsync(cancellationToken);
        public override bool TryRead(out ReadResult result) => _reader.TryRead(out result);
    }
}
