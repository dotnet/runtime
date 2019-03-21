// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    internal sealed class PipeWriterStream : Stream
    {
        private readonly PipeWriter _pipeWriter;

        public PipeWriterStream(PipeWriter pipeWriter)
        {
            Debug.Assert(pipeWriter != null);
            _pipeWriter = pipeWriter;
        }

        protected override void Dispose(bool disposing)
        {
            _pipeWriter.Complete();
            base.Dispose(disposing);
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
            FlushAsync().GetAwaiter().GetResult();
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public sealed override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
            TaskToApm.Begin(WriteAsync(buffer, offset, count, default), callback, state);

        public sealed override void EndWrite(IAsyncResult asyncResult) =>
            TaskToApm.End(asyncResult);

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValueTask<FlushResult> valueTask = _pipeWriter.WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);

            return GetFlushResultAsTask(valueTask);
        }

#if !netstandard
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ValueTask<FlushResult> valueTask = _pipeWriter.WriteAsync(buffer, cancellationToken);

            return new ValueTask(GetFlushResultAsTask(valueTask));
        }
#endif

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            ValueTask<FlushResult> valueTask = _pipeWriter.FlushAsync(cancellationToken);

            return GetFlushResultAsTask(valueTask);
        }

        private static Task GetFlushResultAsTask(ValueTask<FlushResult> valueTask)
        {
            if (valueTask.IsCompletedSuccessfully)
            {
                FlushResult result = valueTask.Result;
                if (result.IsCanceled)
                {
                    ThrowHelper.ThrowOperationCanceledException_FlushCanceled();
                }

                return Task.CompletedTask;
            }

            static async Task AwaitTask(ValueTask<FlushResult> valueTask)
            {
                FlushResult result = await valueTask.ConfigureAwait(false);

                if (result.IsCanceled)
                {
                    ThrowHelper.ThrowOperationCanceledException_FlushCanceled();
                }
            }

            return AwaitTask(valueTask);
        }
    }
}

