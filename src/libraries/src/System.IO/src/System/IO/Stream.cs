// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    public abstract class Stream : IDisposable
    {
        public static readonly Stream Null = new NullStream();

        //We pick a value that is the largest multiple of 4096 that is still smaller than the large object heap threshold (85K).
        // The CopyTo/CopyToAsync buffer is short-lived and is likely to be collected at Gen0, and it offers a significant
        // improvement in Copy performance.
        private const int DefaultCopyBufferSize = 81920;

        // To implement Async IO operations on streams that don't support async IO

        private SemaphoreSlim _asyncActiveSemaphore;

        internal SemaphoreSlim EnsureAsyncActiveSemaphoreInitialized()
        {
            // Lazily-initialize _asyncActiveSemaphore.  As we're never accessing the SemaphoreSlim's
            // WaitHandle, we don't need to worry about Disposing it.
            return LazyInitializer.EnsureInitialized(ref _asyncActiveSemaphore, () => new SemaphoreSlim(1, 1));
        }

        public abstract bool CanRead
        {
            [Pure]
            get;
        }

        // If CanSeek is false, Position, Seek, Length, and SetLength should throw.
        public abstract bool CanSeek
        {
            [Pure]
            get;
        }

        public virtual bool CanTimeout
        {
            [Pure]
            get
            {
                return false;
            }
        }

        public abstract bool CanWrite
        {
            [Pure]
            get;
        }

        public abstract long Length
        {
            get;
        }

        public abstract long Position
        {
            get;
            set;
        }

        public virtual int ReadTimeout
        {
            get
            {
                throw new InvalidOperationException(SR.InvalidOperation_TimeoutsNotSupported);
            }
            set
            {
                throw new InvalidOperationException(SR.InvalidOperation_TimeoutsNotSupported);
            }
        }

        public virtual int WriteTimeout
        {
            get
            {
                throw new InvalidOperationException(SR.InvalidOperation_TimeoutsNotSupported);
            }
            set
            {
                throw new InvalidOperationException(SR.InvalidOperation_TimeoutsNotSupported);
            }
        }

        public Task CopyToAsync(Stream destination)
        {
            return CopyToAsync(destination, DefaultCopyBufferSize);
        }

        public Task CopyToAsync(Stream destination, int bufferSize)
        {
            return CopyToAsync(destination, bufferSize, CancellationToken.None);
        }

        public virtual Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedPosNum);
            }
            if (!CanRead && !CanWrite)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
            }
            if (!destination.CanRead && !destination.CanWrite)
            {
                throw new ObjectDisposedException(nameof(destination), SR.ObjectDisposed_StreamClosed);
            }
            if (!CanRead)
            {
                throw new NotSupportedException(SR.NotSupported_UnreadableStream);
            }
            if (!destination.CanWrite)
            {
                throw new NotSupportedException(SR.NotSupported_UnwritableStream);
            }

            return CopyToAsyncInternal(destination, bufferSize, cancellationToken);
        }

        private async Task CopyToAsyncInternal(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            Debug.Assert(destination != null);
            Debug.Assert(bufferSize > 0);
            Debug.Assert(CanRead);
            Debug.Assert(destination.CanWrite);

            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            while ((bytesRead = await ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            }
        }

        // Reads the bytes from the current stream and writes the bytes to
        // the destination stream until all bytes are read, starting at
        // the current position.
        public void CopyTo(Stream destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }
            if (!CanRead && !CanWrite)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
            }
            if (!destination.CanRead && !destination.CanWrite)
            {
                throw new ObjectDisposedException(nameof(destination), SR.ObjectDisposed_StreamClosed);
            }
            if (!CanRead)
            {
                throw new NotSupportedException(SR.NotSupported_UnreadableStream);
            }
            if (!destination.CanWrite)
            {
                throw new NotSupportedException(SR.NotSupported_UnwritableStream);
            }

            InternalCopyTo(destination, DefaultCopyBufferSize);
        }

        public void CopyTo(Stream destination, int bufferSize)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedPosNum);
            }
            if (!CanRead && !CanWrite)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
            }
            if (!destination.CanRead && !destination.CanWrite)
            {
                throw new ObjectDisposedException(nameof(destination), SR.ObjectDisposed_StreamClosed);
            }
            if (!CanRead)
            {
                throw new NotSupportedException(SR.NotSupported_UnreadableStream);
            }
            if (!destination.CanWrite)
            {
                throw new NotSupportedException(SR.NotSupported_UnwritableStream);
            }

            InternalCopyTo(destination, bufferSize);
        }

        private void InternalCopyTo(Stream destination, int bufferSize)
        {
            Debug.Assert(destination != null);
            Debug.Assert(CanRead);
            Debug.Assert(destination.CanWrite);
            Debug.Assert(bufferSize > 0);

            byte[] buffer = new byte[bufferSize];
            int read;
            while ((read = Read(buffer, 0, buffer.Length)) != 0)
            {
                destination.Write(buffer, 0, read);
            }
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            // Note: Never change this to call other virtual methods on Stream
            // like Write, since the state on subclasses has already been 
            // torn down.  This is the last code to run on cleanup for a stream.
        }

        public abstract void Flush();

        public Task FlushAsync()
        {
            return FlushAsync(CancellationToken.None);
        }

        public virtual Task FlushAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new Task(() => { }, cancellationToken);
            }

            return Task.Factory.StartNew(state => ((Stream)state).Flush(), this,
                cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public Task<int> ReadAsync(Byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count, CancellationToken.None);
        }

        public virtual Task<int> ReadAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!CanRead)
            {
                throw new NotSupportedException(SR.NotSupported_UnreadableStream);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return new Task<int>(() => 0, cancellationToken);
            }

            // To avoid a race with a stream's position pointer & generating race 
            // conditions with internal buffer indexes in our own streams that 
            // don't natively support async IO operations when there are multiple 
            // async requests outstanding, we will serialize the requests.
            return EnsureAsyncActiveSemaphoreInitialized().WaitAsync().ContinueWith((completedWait, s) =>
            {
                Debug.Assert(completedWait.Status == TaskStatus.RanToCompletion);
                var state = (Tuple<Stream, byte[], int, int>)s;
                try
                {
                    return state.Item1.Read(state.Item2, state.Item3, state.Item4); // this.Read(buffer, offset, count);
                }
                finally
                {
                    state.Item1._asyncActiveSemaphore.Release();
                }
            }, Tuple.Create(this, buffer, offset, count), CancellationToken.None, TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public Task WriteAsync(Byte[] buffer, int offset, int count)
        {
            return WriteAsync(buffer, offset, count, CancellationToken.None);
        }

        public virtual Task WriteAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!CanWrite)
            {
                throw new NotSupportedException(SR.NotSupported_UnwritableStream);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return new Task(() => { }, cancellationToken);
            }

            // To avoid a race with a stream's position pointer & generating race 
            // conditions with internal buffer indexes in our own streams that 
            // don't natively support async IO operations when there are multiple 
            // async requests outstanding, we will serialize the requests.
            return EnsureAsyncActiveSemaphoreInitialized().WaitAsync().ContinueWith((completedWait, s) =>
            {
                Debug.Assert(completedWait.Status == TaskStatus.RanToCompletion);
                var state = (Tuple<Stream, byte[], int, int>)s;
                try
                {
                    state.Item1.Write(state.Item2, state.Item3, state.Item4); // this.Write(buffer, offset, count);
                }
                finally
                {
                    state.Item1._asyncActiveSemaphore.Release();
                }
            }, Tuple.Create(this, buffer, offset, count), CancellationToken.None, TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public abstract long Seek(long offset, SeekOrigin origin);

        public abstract void SetLength(long value);

        public abstract int Read(byte[] buffer, int offset, int count);

        // Reads one byte from the stream by calling Read(byte[], int, int). 
        // Will return an unsigned byte cast to an int or -1 on end of stream.
        // This implementation does not perform well because it allocates a new
        // byte[] each time you call it, and should be overridden by any 
        // subclass that maintains an internal buffer.  Then, it can help perf
        // significantly for people who are reading one byte at a time.
        public virtual int ReadByte()
        {
            byte[] oneByteArray = new byte[1];
            int r = Read(oneByteArray, 0, 1);
            if (r == 0)
            {
                return -1;
            }
            return oneByteArray[0];
        }

        public abstract void Write(byte[] buffer, int offset, int count);

        // Writes one byte from the stream by calling Write(byte[], int, int).
        // This implementation does not perform well because it allocates a new
        // byte[] each time you call it, and should be overridden by any 
        // subclass that maintains an internal buffer.  Then, it can help perf
        // significantly for people who are writing one byte at a time.
        public virtual void WriteByte(byte value)
        {
            byte[] oneByteArray = new byte[1];
            oneByteArray[0] = value;
            Write(oneByteArray, 0, 1);
        }

        private sealed class NullStream : Stream
        {
            internal NullStream() { }

            public override bool CanRead
            {
                [Pure]
                get
                { return true; }
            }

            public override bool CanWrite
            {
                [Pure]
                get
                { return true; }
            }

            public override bool CanSeek
            {
                [Pure]
                get
                { return true; }
            }

            public override long Length
            {
                get { return 0; }
            }

            public override long Position
            {
                get { return 0; }
                set { }
            }

            protected override void Dispose(bool disposing)
            {
                // Do nothing - we don't want NullStream singleton (static) to be closable
            }

            public override void Flush()
            {
            }

#pragma warning disable 1998 // async method with no await
            public override async Task FlushAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
#pragma warning restore 1998

            public override int Read(byte[] buffer, int offset, int count)
            {
                return 0;
            }

#pragma warning disable 1998 // async method with no await
            public override async Task<int> ReadAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return 0;
            }
#pragma warning restore 1998

            public override int ReadByte()
            {
                return -1;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
            }

#pragma warning disable 1998 // async method with no await
            public override async Task WriteAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
#pragma warning restore 1998

            public override void WriteByte(byte value)
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return 0;
            }

            public override void SetLength(long length)
            {
            }
        }
    }
}
