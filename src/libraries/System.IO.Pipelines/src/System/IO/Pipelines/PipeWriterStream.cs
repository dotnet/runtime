// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    internal sealed class PipeWriterStream : Stream
    {
        private readonly PipeWriter _pipeWriter;

        public PipeWriterStream(PipeWriter pipeWriter, bool leaveOpen)
        {
            Debug.Assert(pipeWriter != null);
            _pipeWriter = pipeWriter;
            LeaveOpen = leaveOpen;
        }

        protected override void Dispose(bool disposing)
        {
            if (!LeaveOpen)
            {
                _pipeWriter.Complete();
            }
        }

#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
        public override ValueTask DisposeAsync()
        {
            if (!LeaveOpen)
            {
                return _pipeWriter.CompleteAsync();
            }
            return default;
        }
#endif

        internal bool LeaveOpen { get; set; }

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

        public sealed override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(WriteAsync(buffer, offset, count, default), callback, state);

        public sealed override void EndWrite(IAsyncResult asyncResult) =>
            TaskToApm.End(asyncResult);

        public override void Write(byte[] buffer, int offset, int count) =>
            WriteAsync(buffer, offset, count).GetAwaiter().GetResult();

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.buffer);
            }

            ValueTask<FlushResult> valueTask = _pipeWriter.WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);

            return GetFlushResultAsTask(valueTask);
        }

#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
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
