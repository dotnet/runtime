// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections
{
    internal sealed class DuplexPipeStream : Stream
    {
        private readonly PipeReader _reader;
        private readonly PipeWriter _writer;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public DuplexPipeStream(IDuplexPipe pipe)
        {
            _reader = pipe.Input;
            _writer = pipe.Output;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _reader.Complete();
                _writer.Complete();
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _reader.CompleteAsync().ConfigureAwait(false);
            await _writer.CompleteAsync().ConfigureAwait(false);
        }

        public override void Flush()
        {
            FlushAsync().GetAwaiter().GetResult();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            FlushResult r = await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            if (r.IsCanceled) throw new OperationCanceledException(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            ValueTask<int> t = ReadAsync(buffer.AsMemory(offset, count));
            return
                t.IsCompleted ? t.GetAwaiter().GetResult() :
                t.AsTask().GetAwaiter().GetResult();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null) return Task.FromException<int>(ExceptionDispatchInfo.SetCurrentStackTrace(new ArgumentNullException(nameof(buffer))));
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ReadResult result = await _reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            if (result.IsCanceled)
            {
                throw new OperationCanceledException();
            }

            ReadOnlySequence<byte> sequence = result.Buffer;
            long bufferLength = sequence.Length;
            SequencePosition consumed = sequence.Start;

            try
            {
                if (bufferLength != 0)
                {
                    int actual = (int)Math.Min(bufferLength, buffer.Length);

                    ReadOnlySequence<byte> slice = actual == bufferLength ? sequence : sequence.Slice(0, actual);
                    consumed = slice.End;
                    slice.CopyTo(buffer.Span);

                    return actual;
                }

                if (result.IsCompleted)
                {
                    return 0;
                }
            }
            finally
            {
                _reader.AdvanceTo(consumed);
            }

            // This is a buggy PipeReader implementation that returns 0 byte reads even though the PipeReader
            // isn't completed or canceled.
            throw new InvalidOperationException(SR.net_connections_zero_byte_pipe_read);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return TaskToApm.End<int>(asyncResult);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            FlushResult r = await _writer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (r.IsCanceled) throw new OperationCanceledException(cancellationToken);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return TaskToApm.Begin(WriteAsync(buffer, offset, count), callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            TaskToApm.End(asyncResult);
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return _reader.CopyToAsync(destination, cancellationToken);
        }
    }
}
