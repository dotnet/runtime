// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace System.Net.Http
{
    internal sealed class CurlResponseMessage : HttpResponseMessage
    {
        private readonly CurlResponseStream _responseStream = new CurlResponseStream();

        #region Properties

        internal CurlResponseStream ContentStream
        {
            get { return _responseStream; }
        }

        #endregion

        internal CurlResponseMessage(HttpRequestMessage request)
        {
            RequestMessage = request;
            Content = new StreamContent(_responseStream);
        }
    }

    internal sealed class CurlResponseStream : Stream
    {
        private volatile bool _disposed = false;
        private Stream _innerStream;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEventSlim _readerRequestingDataEvent = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim _writerHasDataEvent = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim _readerReadAllDataEvent = new ManualResetEventSlim(false);

        private object LockObject
        {
            get { return _cancellationTokenSource; }
        }

        internal void SignalComplete()
        {
            if (!_disposed)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        internal unsafe void WaitAndSignalReaders(byte* pointer, long length)
        {
            CheckDisposed();
            _readerRequestingDataEvent.Wait(Timeout.InfiniteTimeSpan, _cancellationTokenSource.Token);
            lock (LockObject)
            {
                _innerStream = new UnmanagedMemoryStream(pointer, length);
                _readerReadAllDataEvent.Reset();
                _readerRequestingDataEvent.Reset();
                _writerHasDataEvent.Set();
            }
            _readerReadAllDataEvent.Wait(Timeout.InfiniteTimeSpan, _cancellationTokenSource.Token);;
        }

        public override bool CanRead
        {
            get
            {
                return !_disposed;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                CheckDisposed();
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                CheckDisposed();
                throw new NotSupportedException();
            }

            set
            {
                CheckDisposed();
                throw new NotSupportedException();
            }
        }

        public override void Flush()
        {
            // Nothing to do.
        }

        //public override Task<int> ReadAsync(byte[] buffer, int offset, int count)
        //{
        //    // TODO: Consider proper implementation here using AsyncTaskMethodBuilder
        //    return base.ReadAsync(buffer, offset, count);
        //}

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if ((long) offset + (long) count > (long) buffer.Length)
            {
                throw new ArgumentException("buffer");
            }

            CheckDisposed();

            do
            {
                lock (LockObject)
                {
                    // Wait till data is available
                    if (_innerStream == null)
                    {
                        SignalWriter();
                        continue;
                    }

                    // Since this is under lock, make sure the Read is non-blocking
                    Debug.Assert((_innerStream is MemoryStream) || (_innerStream is UnmanagedMemoryStream));

                    int bytesRead = _innerStream.Read(buffer, offset, (int)Math.Min((long)count, _innerStream.Length));
                    if (_innerStream.Position == _innerStream.Length)
                    {
                        _innerStream.Dispose();
                        _innerStream = null;
                        _readerReadAllDataEvent.Set();
                    }
                    return bytesRead;
                }
            }
            while (WaitForWriter());
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            CheckDisposed();
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                // Cancel any pending operations
                SignalComplete();

                _disposed = true;
                lock (LockObject)
                {
                    if (_innerStream != null)
                    {
                        _innerStream.Dispose();
                        _innerStream = null;
                    }
                }
                _readerRequestingDataEvent.Dispose();
                _writerHasDataEvent.Dispose();
                _readerReadAllDataEvent.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Private

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        private bool WaitForWriter()
        {
            try
            {
                _writerHasDataEvent.Wait(Timeout.InfiniteTimeSpan, _cancellationTokenSource.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                if (_writerHasDataEvent.Wait(TimeSpan.Zero))
                {
                    // If both are set, unblock the reader based on data availability
                    lock (LockObject)
                    {
                        return (_innerStream != null);
                    }
                }
                return false;
            }
        }

        private void SignalWriter()
        {
            Debug.Assert(Monitor.IsEntered(LockObject));
            _writerHasDataEvent.Reset();
            _readerRequestingDataEvent.Set();
        }

        #endregion
    }
}
