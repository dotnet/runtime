// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal abstract class BaseAsyncStream : Stream
    {
        public sealed override bool CanSeek => false;

        public sealed override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count, default), callback, state);

        public sealed override int EndRead(IAsyncResult asyncResult) =>
            TaskToApm.End<int>(asyncResult);

        public sealed override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
            TaskToApm.Begin(WriteAsync(buffer, offset, count, default), callback, state);

        public sealed override void EndWrite(IAsyncResult asyncResult) =>
            TaskToApm.End(asyncResult);

        public sealed override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public sealed override void SetLength(long value) => throw new NotSupportedException();

        public sealed override long Length => throw new NotSupportedException();

        public sealed override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        protected static void ValidateBufferArgs(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if ((uint)offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if ((uint)count > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
        }

        /// <summary>
        /// Validate the arguments to CopyTo, as would Stream.CopyTo, but with knowledge that
        /// the source stream is always readable and so only checking the destination.
        /// </summary>
        protected static void ValidateCopyToArgs(Stream source, Stream destination, int bufferSize)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, SR.ArgumentOutOfRange_NeedPosNum);
            }

            if (!destination.CanWrite)
            {
                throw destination.CanRead ?
                    new NotSupportedException(SR.NotSupported_UnwritableStream) :
                    (Exception)new ObjectDisposedException(nameof(destination), SR.ObjectDisposed_StreamClosed);
            }
        }

        public sealed override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArgs(buffer, offset, count);
            return ReadAsync(new Memory<byte>(buffer, offset, count), CancellationToken.None).GetAwaiter().GetResult();
        }

        public sealed override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArgs(buffer, offset, count);
            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public sealed override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArgs(buffer, offset, count);
            WriteAsync(new Memory<byte>(buffer, offset, count), CancellationToken.None).GetAwaiter().GetResult();
        }

        public sealed override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArgs(buffer, offset, count);
            return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public sealed override void Flush() => FlushAsync().GetAwaiter().GetResult();

        public sealed override void CopyTo(Stream destination, int bufferSize) =>
            CopyToAsync(destination, bufferSize, CancellationToken.None).GetAwaiter().GetResult();


        //
        // Methods which must be implemented by derived classes
        //

        public abstract override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken);
        public abstract override ValueTask WriteAsync(ReadOnlyMemory<byte> destination, CancellationToken cancellationToken);
        public abstract override Task FlushAsync(CancellationToken cancellationToken);
    }
}
