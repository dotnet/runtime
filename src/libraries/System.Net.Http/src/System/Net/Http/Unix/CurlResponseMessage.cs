// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed class CurlResponseMessage : HttpResponseMessage
    {
        private readonly SingleBlockingWriterSingleReaderStream _responseStream = new SingleBlockingWriterSingleReaderStream();

        internal CurlResponseMessage(HttpRequestMessage request)
        {
            RequestMessage = request;
            Content = new StreamContent(_responseStream);
        }

        internal SingleBlockingWriterSingleReaderStream ContentStream
        {
            get { return _responseStream; }
        }
    }

    /// <summary>
    /// Provides a rendezvous stream that allows a writer to handoff data to a reader.
    /// A call from a writer provides data and blocks until all of the provided data
    /// has been consumed by a reader, which may make multiple calls to consume the data
    /// and which may wait for data either synchronously or asynchronously. Only one
    /// writer and one reader are supported concurrently.
    /// </summary>
    internal sealed class SingleBlockingWriterSingleReaderStream : Stream
    {
        /// <summary>Stores whether Dispose has been called on the stream.</summary>
        private volatile bool _disposed = false;

        /// <summary>
        /// CancellationTokenSource that has cancellation requested when the Stream is being shutdown,
        /// either due to the writer not having any more data or due to a failure in the request/response processing.
        /// </summary>
        private readonly CancellationTokenSource _completionSignal = new CancellationTokenSource();

        /// <summary>
        /// A semaphore used as an auto-reset event to communicate when the writer has published data
        /// to be consumed by the reader.  0 count available means unset / no data, and 1 count available means set / data.
        /// SemaphoreSlim is used instead of ManualResetEventSlim because SemaphoreSlim supports both
        /// synchronous and asynchronous waiting.
        /// </summary>
        private readonly SemaphoreSlim _writerPublishedDataEvent = new SemaphoreSlim(0, 1);

        /// <summary>
        /// A manual reset event used to communicate when all of the data published by the writer has
        /// been consumed by the reader.  The writer will block and not return to the libcurl callback
        /// until this event is set by the reader.
        /// </summary>
        private readonly ManualResetEventSlim _readerConsumedAllDataEvent = new ManualResetEventSlim();

        /// <summary>A pointer to the data published by the writer, or IntPtr.Zero if no data is available.</summary>
        private IntPtr _availableData;

        /// <summary>The length of the remaining data available via <see cref="_availableData"/>.  May be 0 if no data is available.</summary>
        private long _availableDataLength;

        /// <summary>If not null, represents an exception object that should be propagated to any reader.</summary>
        private ExceptionDispatchInfo _storedException;

        /// <summary>Gets the lock object used to protect the available data.</summary>
        private object LockObject
        {
            get { return _completionSignal; }
        }

        /// <summary>Notifies the stream that no more data will be written.</summary>
        internal void SignalComplete()
        {
            _completionSignal.Cancel();
        }

        /// <summary>Notifies the stream that a failure has occurred and that no more data will be written.</summary>
        /// <param name="error">An exception for the failure and that should be propagated to any readers.</param>
        internal void SignalFailure(ExceptionDispatchInfo error)
        {
            if (_storedException == null)
            {
                _storedException = error;
            }
            SignalComplete();
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

        public override void Flush()
        {
            // Nothing to do.
        }

        /// <summary>
        /// Writes the <paramref name="length"/> bytes starting at <paramref name="pointer"/>
        /// to the stream, and waits for a reader to consume all of the data before returning.
        /// </summary>
        internal void WriteDataAndWaitForConsumption(IntPtr pointer, long length)
        {
            Debug.Assert(pointer != IntPtr.Zero, "Expected a non-null pointer");
            Debug.Assert(length >= 0, "Expected a non-negative length");
            Debug.Assert(_availableData == IntPtr.Zero, "Expected no existing data in the stream");
            Debug.Assert(_availableDataLength == 0, "Expected length of existing data in the stream to be 0");
            Debug.Assert(_writerPublishedDataEvent.CurrentCount == 0, "Expected semaphore to reflect no published data");

            CheckDisposed();

            if (length == 0)
            {
                // No data to write; bail.
                return;
            }

            lock (LockObject)
            {
                // libcurl's data write callback is synchronous, such that it expects
                // all of the data it provides to have been consumed by the time
                // the callback returns. As such, we'll need to block until all of the data 
                // has been read by a reader.  To enable that, reset the event that'll be set 
                // by the reader when the data has been entirely consumed.
                _readerConsumedAllDataEvent.Reset();

                // Store the supplied data, to make it available to the reader.  The reader will 
                // consume this data, potentially in multiple calls to Read/ReadAsync.
                _availableData = pointer;
                _availableDataLength = length;

                // With the data now published, let a current or future reader know about the data.
                _writerPublishedDataEvent.Release();
            }

            try
            {
                // Wait for all of the published data to be consumed.
                _readerConsumedAllDataEvent.Wait(_completionSignal.Token);
            }
            finally
            {
                // At this point, either all of the data has been consumed, or cancellation
                // was requested due to the stream shutting down.  Null out the data, but 
                // do so synchronizing with any potential concurrent reader to ensure we don't let 
                // libcurl deallocate the data while it's still being used by a reader.
                lock (LockObject)
                {
                    ClearPublishedData();
                }
            }
        }

        /// <summary>Clears the published data and associated state.</summary>
        private void ClearPublishedData()
        {
            Debug.Assert(Monitor.IsEntered(LockObject), "Published data must only be accessed while holding the lock");
            _availableData = IntPtr.Zero;
            _availableDataLength = 0;
            _writerPublishedDataEvent.Wait(0); // poll the semaphore to ensure it has no count (effectively Reset)
            Debug.Assert(_writerPublishedDataEvent.CurrentCount == 0, "Reset semaphore should not have any count");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateReadArguments(buffer, offset, count);

            // If there's any data left over from a previous Read call, grab as much
            // of it as remains (or as we're asking for) and return it.
            lock (LockObject)
            {
                if (_availableDataLength > 0)
                {
                    return ConsumePublishedData(buffer, offset, count);
                }
            }

            // There wasn't any data, so wait for the writer to publish more.
            try
            {
                _writerPublishedDataEvent.Wait(_completionSignal.Token);
            }
            catch (OperationCanceledException) { }

            // Now either cancellation should have been requested or
            // we should have data to read and return.
            return ReadDataAfterWaitingForWriter(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateReadArguments(buffer, offset, count);
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            // If there's any data left over from a previous call, grab as much
            // of it as remains (or as we're asking for) and return it.
            lock (LockObject)
            {
                if (_availableDataLength > 0)
                {
                    return Task.FromResult(ConsumePublishedData(buffer, offset, count));
                }
            }

            // There wasn't any data, so wait for the writer to publish more
            // and then read it.  Unlike in Read, this is separated out in a separated
            // async method to allow for ValidateReadArguments to propagate any arg exceptions
            // synchronously and for a fast-path read in the case of data already being available.
            return ReadAsyncCore(buffer, offset, count, cancellationToken);
        }

        private async Task<int> ReadAsyncCore(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // There wasn't any data, so wait for the writer to publish more.
            try
            {
                await _writerPublishedDataEvent.WaitAsync(_completionSignal.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }

            // Now either cancellation should have been requested or
            // we should have data to read and return.
            return ReadDataAfterWaitingForWriter(buffer, offset, count);
        }

        /// <summary>
        /// Common routine used by both Read and ReadAsync to read available data after waiting
        /// for the writer to publish.
        /// </summary>
        private int ReadDataAfterWaitingForWriter(byte[] buffer, int offset, int count)
        {
            // We should now have data to read, as long as cancellation hasn't been requested.
            if (!_completionSignal.IsCancellationRequested)
            {
                Debug.Assert(_writerPublishedDataEvent.CurrentCount == 0, "Since cancellation wasn't requested, expected semaphore to have 0 count");
                lock (LockObject)
                {
                    Debug.Assert(_completionSignal.IsCancellationRequested || _availableData != IntPtr.Zero, "Expected writer to publish non-null pointer");
                    Debug.Assert(_completionSignal.IsCancellationRequested || _availableDataLength > 0, "Expected writer to publish positive amount of data");
                    if (_availableDataLength > 0)
                    {
                        return ConsumePublishedData(buffer, offset, count);
                    }
                }
            }

            // There's no more data to read due to the stream shutting down.  If we shut down due to a failure,
            // propagate the exception.  Otherwise, return 0 to indicate that the stream is complete.
            Debug.Assert(_completionSignal.IsCancellationRequested, "Cancellation must have been requested to get here");
            ExceptionDispatchInfo edi = _storedException;
            if (edi != null)
            {
                edi.Throw();
            }
            return 0;
        }

        /// <summary>
        /// Copies at most <paramref name="count"/> bytes from the published data into the 
        /// supplied buffer at the specified offset.  The published data is then updated
        /// accordingly to reflect the consumed data.
        /// </summary>
        /// <returns>The number of bytes copied.</returns>
        private int ConsumePublishedData(byte[] buffer, int offset, int count)
        {
            Debug.Assert(Monitor.IsEntered(LockObject), "Published data must only be accessed while holding the lock");

            // Copy the data into the read buffer
            int bytesToRead = (int)Math.Min(_availableDataLength, count);
            Marshal.Copy(_availableData, buffer, offset, bytesToRead);

            // Update how much remains
            _availableData = _availableData + bytesToRead;
            _availableDataLength -= bytesToRead;

            // If none is left, clear out the ptr and let
            // the writer know it's all be consumed.
            if (_availableDataLength == 0)
            {
                ClearPublishedData();
                _readerConsumedAllDataEvent.Set();
            }

            // Finally, return how much was read.
            return bytesToRead;
        }

        /// <summary>Performs validation for calls to Read and ReadAsync.</summary>
        private void ValidateReadArguments(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (offset < 0) throw new ArgumentOutOfRangeException("offset");
            if (count < 0) throw new ArgumentOutOfRangeException("count");
            if (offset > buffer.Length - count) throw new ArgumentException("buffer");
            CheckDisposed();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                SignalComplete(); // Wake-up any blocked waits

                // Don't Dispose of the events as they may still be in use by Waits. Since 
                // we're not accessing their WaitHandle's, Dispose would be a nop, anyway.
            }

            base.Dispose(disposing);
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

    }
}
