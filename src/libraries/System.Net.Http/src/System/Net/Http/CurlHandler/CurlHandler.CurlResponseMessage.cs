// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal partial class CurlHandler : HttpMessageHandler
    {
        private sealed class CurlResponseMessage : HttpResponseMessage
        {
            internal uint _headerBytesReceived;

            internal CurlResponseMessage(EasyRequest easy)
            {
                Debug.Assert(easy != null, "Expected non-null EasyRequest");
                RequestMessage = easy._requestMessage;
                ResponseStream = new CurlResponseStream(easy);
                Content = new NoWriteNoSeekStreamContent(ResponseStream);

                // On Windows, we pass the equivalent of the easy._cancellationToken
                // in to StreamContent's ctor.  This in turn passes that token through
                // to ReadAsync operations on the stream response stream when HttpClient
                // reads from the response stream to buffer it with ResponseContentRead.
                // We don't need to do that here in the Unix implementation as, until the
                // SendAsync task completes, the handler will have registered with the
                // CancellationToken with an action that will cancel all work related to
                // the easy handle if cancellation occurs, and that includes canceling any
                // pending reads on the response stream.  It wouldn't hurt anything functionally
                // to still pass easy._cancellationToken here, but it will increase costs
                // a bit, as each read will then need to allocate a larger state object as
                // well as register with and unregister from the cancellation token.
            }

            internal CurlResponseStream ResponseStream { get; }
        }

        /// <summary>
        /// Provides a response stream that allows libcurl to transfer data asynchronously to a reader.
        /// When writing data to the response stream, either all or none of the data will be transferred,
        /// and if none, libcurl will pause the connection until a reader is waiting to consume the data.
        /// Readers consume the data via ReadAsync, which registers a read state with the stream, to be
        /// filled in by a pending write.  Read is just a thin wrapper around ReadAsync, since the underlying
        /// mechanism must be asynchronous to prevent blocking libcurl's processing.
        /// </summary>
        private sealed class CurlResponseStream : Stream
        {
            /// <summary>
            /// A sentinel object used in the <see cref="_completed"/> field to indicate that
            /// the stream completed successfully.
            /// </summary>
            private static readonly Exception s_completionSentinel = new Exception(nameof(s_completionSentinel));

            /// <summary>A object used to synchronize all access to state on this response stream.</summary>
            private readonly object _lockObject = new object();

            /// <summary>The associated EasyRequest.</summary>
            private readonly EasyRequest _easy;

            /// <summary>Stores whether Dispose has been called on the stream.</summary>
            private bool _disposed = false;

            /// <summary>
            /// Null if the Stream has not been completed, non-null if it has been completed.
            /// If non-null, it'll either be the <see cref="s_completionSentinel"/> object, meaning the stream completed
            /// successfully, or it'll be an Exception object representing the error that caused completion.
            /// That error will be transferred to any subsequent read requests.
            /// </summary>
            private Exception _completed;

            /// <summary>
            /// The state associated with a pending read request. When a reader requests data, it puts
            /// its state here for the writer to fill in when data is available.
            /// </summary>
            private ReadState _pendingReadRequest;

            /// <summary>
            /// When data is provided by libcurl, it must be consumed all or nothing: either all of the data is consumed, or
            /// we must pause the connection.  Since a read could need to be satisfied with only some of the data provided,
            /// we store the rest here until all reads can consume it.  If a subsequent write callback comes in to provide
            /// more data, the connection will then be paused until this buffer is entirely consumed.
            /// </summary>
            private byte[] _remainingData;

            /// <summary>
            /// The offset into <see cref="_remainingData"/> from which the next read should occur.
            /// </summary>
            private int _remainingDataOffset;

            /// <summary>
            /// The remaining number of bytes in <see cref="_remainingData"/> available to be read.
            /// </summary>
            private int _remainingDataCount;

            internal CurlResponseStream(EasyRequest easy)
            {
                Debug.Assert(easy != null, "Expected non-null associated EasyRequest");
                _easy = easy;
            }

            public override bool CanRead => !_disposed;
            public override bool CanWrite => false;
            public override bool CanSeek => false;

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
            /// Writes the <paramref name="length"/> bytes starting at <paramref name="pointer"/> to the stream.
            /// </summary>
            /// <returns>
            /// <paramref name="length"/> if all of the data was written, or 
            /// <see cref="Interop.Http.CURL_WRITEFUNC_PAUSE"/> if the data wasn't copied and the connection
            /// should be paused until a reader is available.
            /// </returns>
            internal ulong TransferDataToResponseStream(IntPtr pointer, long length)
            {
                Debug.Assert(pointer != IntPtr.Zero, "Expected a non-null pointer");
                Debug.Assert(length >= 0, "Expected a non-negative length");
                EventSourceTrace("Length: {0}", length);

                CheckDisposed();

                // If there's no data to write, consider everything transferred.
                if (length == 0)
                {
                    return 0;
                }

                lock (_lockObject)
                {
                    VerifyInvariants();

                    // If there's existing data in the remaining data buffer, or if there's no pending read request, 
                    // we need to pause until the existing data is consumed or until there's a waiting read.
                    if (_remainingDataCount > 0)
                    {
                        EventSourceTrace("Pausing transfer to response stream. Remaining bytes: {0}", _remainingDataCount);
                        return Interop.Http.CURL_WRITEFUNC_PAUSE;
                    }
                    else if (_pendingReadRequest == null)
                    {
                        EventSourceTrace("Pausing transfer to response stream. No pending read request");
                        return Interop.Http.CURL_WRITEFUNC_PAUSE;
                    }

                    // There's no data in the buffer and there is a pending read request.  
                    // Transfer as much data as we can to the read request, completing it.
                    int numBytesForTask = (int)Math.Min(length, _pendingReadRequest._buffer.Length);
                    Debug.Assert(numBytesForTask > 0, "We must be copying a positive amount.");
                    unsafe { new Span<byte>((byte*)pointer, numBytesForTask).CopyTo(_pendingReadRequest._buffer.Span); }
                    EventSourceTrace("Completing pending read with {0} bytes", numBytesForTask);
                    _pendingReadRequest.SetResult(numBytesForTask);
                    ClearPendingReadRequest();

                    // If there's any data left, transfer it to our remaining buffer. libcurl does not support
                    // partial transfers of data, so since we just took some of it to satisfy the read request
                    // we must take the rest of it. (If libcurl then comes back to us subsequently with more data
                    // before this buffered data has been consumed, at that point we won't consume any of the
                    // subsequent offering and will ask libcurl to pause.)
                    if (numBytesForTask < length)
                    {
                        IntPtr remainingPointer = pointer + numBytesForTask;
                        _remainingDataCount = checked((int)(length - numBytesForTask));
                        _remainingDataOffset = 0;

                        // Make sure our remaining data buffer exists and is big enough to hold the data
                        if (_remainingData == null)
                        {
                            _remainingData = new byte[_remainingDataCount];
                        }
                        else if (_remainingData.Length < _remainingDataCount)
                        {
                            _remainingData = new byte[Math.Max(_remainingData.Length * 2, _remainingDataCount)];
                        }

                        // Copy the remaining data to the buffer
                        EventSourceTrace("Storing {0} bytes for later", _remainingDataCount);
                        Marshal.Copy(remainingPointer, _remainingData, 0, _remainingDataCount);
                    }

                    // All of the data from libcurl was consumed.
                    return (ulong)length;
                }
            }

            public override int Read(byte[] buffer, int offset, int count) =>
                ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
                TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);

            public override int EndRead(IAsyncResult asyncResult) =>
                TaskToApm.End<int>(asyncResult);

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (buffer == null) throw new ArgumentNullException(nameof(buffer));
                if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
                if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
                if (offset > buffer.Length - count) throw new ArgumentException(SR.net_http_buffer_insufficient_length, nameof(buffer));

                return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                CheckDisposed();

                EventSourceTrace("Buffer: {0}", buffer.Length);

                // Check for cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    EventSourceTrace("Canceled");
                    return new ValueTask<int>(Task.FromCanceled<int>(cancellationToken));
                }

                lock (_lockObject)
                {
                    VerifyInvariants();

                    // If there's currently a pending read, fail this read, as we don't support concurrent reads.
                    if (_pendingReadRequest != null)
                    {
                        EventSourceTrace("Failing due to existing pending read; concurrent reads not supported.");
                        return new ValueTask<int>(Task.FromException<int>(new InvalidOperationException(SR.net_http_content_no_concurrent_reads)));
                    }

                    // If the stream was already completed with failure, complete the read as a failure.
                    if (_completed != null && _completed != s_completionSentinel)
                    {
                        EventSourceTrace("Failing read with error: {0}", _completed);

                        OperationCanceledException oce = _completed as OperationCanceledException;
                        return new ValueTask<int>((oce != null && oce.CancellationToken.IsCancellationRequested) ?
                            Task.FromCanceled<int>(oce.CancellationToken) :
                            Task.FromException<int>(MapToReadWriteIOException(_completed, isRead: true)));
                    }

                    // Quick check for if no data was actually requested.  We do this after the check
                    // for errors so that we can still fail the read and transfer the exception if we should.
                    if (buffer.Length == 0)
                    {
                        return new ValueTask<int>(0);
                    }

                    // If there's any data left over from a previous call, grab as much as we can.
                    if (_remainingDataCount > 0)
                    {
                        int bytesToCopy = Math.Min(buffer.Length, _remainingDataCount);
                        new Span<byte>(_remainingData, _remainingDataOffset, bytesToCopy).CopyTo(buffer.Span);

                        _remainingDataOffset += bytesToCopy;
                        _remainingDataCount -= bytesToCopy;
                        Debug.Assert(_remainingDataCount >= 0, "The remaining count should never go negative");
                        Debug.Assert(_remainingDataOffset <= _remainingData.Length, "The remaining offset should never exceed the buffer size");

                        EventSourceTrace("Read {0} bytes", bytesToCopy);
                        return new ValueTask<int>(bytesToCopy);
                    }

                    // If the stream has already been completed, complete the read immediately.
                    if (_completed == s_completionSentinel)
                    {
                        EventSourceTrace("Stream already completed");
                        return new ValueTask<int>(0);
                    }

                    // Finally, the stream is still alive, and we want to read some data, but there's no data 
                    // in the buffer so we need to register ourself to get the next write.
                    if (cancellationToken.CanBeCanceled)
                    {
                        // If the cancellation token is cancelable, then we need to register for cancellation.
                        // We create a special CancelableReadState that carries with it additional info:
                        // the cancellation token and the registration with that token.  When cancellation
                        // is requested, we schedule a work item that tries to remove the read state
                        // from being pending, canceling it in the process.  This needs to happen under the
                        // lock, which is why we schedule the operation to run asynchronously: if it ran
                        // synchronously, it could deadlock due to code on another thread holding the lock
                        // and calling Dispose on the registration concurrently with the call to Cancel
                        // the cancellation token.  Dispose on the registration won't return until the action
                        // associated with the registration has completed, but if that action is currently
                        // executing and is blocked on the lock that's held while calling Dispose... deadlock.
                        var crs = new CancelableReadState(buffer, this, cancellationToken);
                        crs._registration = cancellationToken.Register(s1 =>
                        {
                            ((CancelableReadState)s1)._stream.EventSourceTrace("Cancellation invoked. Queueing work item to cancel read state");
                            Task.Factory.StartNew(s2 =>
                            {
                                var crsRef = (CancelableReadState)s2;
                                lock (crsRef._stream._lockObject)
                                {
                                    Debug.Assert(crsRef._token.IsCancellationRequested, "We should only be here if cancellation was requested.");
                                    if (crsRef._stream._pendingReadRequest == crsRef)
                                    {
                                        crsRef._stream.EventSourceTrace("Canceling");
                                        crsRef.TrySetCanceled(crsRef._token);
                                        crsRef._stream.ClearPendingReadRequest();
                                    }
                                }
                            }, s1, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
                        }, crs);
                        _pendingReadRequest = crs;
                    }
                    else
                    {
                        // The token isn't cancelable.  Just create a normal read state.
                        _pendingReadRequest = new ReadState(buffer);
                    }

                    _easy._associatedMultiAgent.RequestUnpause(_easy);
                    _easy._selfStrongToWeakReference.MakeStrong(); // convert from a weak to a strong ref to keep the easy alive during the read
                    return new ValueTask<int>(_pendingReadRequest.Task);
                }
            }

            /// <summary>Notifies the stream that no more data will be written.</summary>
            internal void SignalComplete(Exception error = null, bool forceCancel = false)
            {
                lock (_lockObject)
                {
                    VerifyInvariants();

                    // If we already completed, nothing more to do
                    if (_completed != null)
                    {
                        EventSourceTrace("Already completed");
                        return;
                    }

                    // Mark ourselves as being completed
                    EventSourceTrace("Marking as complete");
                    _completed = error ?? s_completionSentinel;

                    // If the request wasn't already completed, and if requested, send a cancellation
                    // request to ensure that the connection gets cleaned up.  This is only necessary
                    // to do if this method is being called for a reason other than the request/response
                    // completing naturally, e.g. if the response stream is being disposed of before
                    // all of the response has been downloaded.
                    if (forceCancel)
                    {
                        EventSourceTrace("Completing the response stream prematurely.");
                        _easy._associatedMultiAgent.RequestCancel(_easy);
                    }

                    // If there's a pending read request, complete it, either with 0 bytes for success
                    // or with the exception/CancellationToken for failure.
                    ReadState pendingRead = _pendingReadRequest;
                    if (pendingRead != null)
                    {
                        if (_completed == s_completionSentinel)
                        {
                            EventSourceTrace("Completing pending read with 0 bytes");
                            pendingRead.TrySetResult(0);
                        }
                        else
                        {
                            EventSourceTrace("Failing pending read task with error: {0}", _completed);
                            OperationCanceledException oce = _completed as OperationCanceledException;
                            if (oce != null)
                            {
                                pendingRead.TrySetCanceled(oce.CancellationToken);
                            }
                            else
                            {
                                pendingRead.TrySetException(MapToReadWriteIOException(_completed, isRead: true));
                            }
                        }

                        ClearPendingReadRequest();
                    }
                }
            }

            /// <summary>
            /// Clears a pending read request, making sure any cancellation registration is unregistered and
            /// ensuring that the EasyRequest has dropped its strong reference to itself, which should only
            /// exist while an active async operation is going.
            /// </summary>
            private void ClearPendingReadRequest()
            {
                Debug.Assert(Monitor.IsEntered(_lockObject), "Lock object must be held to manipulate _pendingReadRequest");
                Debug.Assert(_pendingReadRequest != null, "Should only be clearing the pending read request if there is one");
                Debug.Assert(_pendingReadRequest.Task.IsCompleted, "The pending request should have been completed");

                (_pendingReadRequest as CancelableReadState)?._registration.Dispose();
                _pendingReadRequest = null;

                // The async operation has completed.  We no longer want to be holding a strong reference.
                _easy._selfStrongToWeakReference.MakeWeak();
            }

            ~CurlResponseStream()
            {
                Dispose(disposing: false);
            }

            protected override void Dispose(bool disposing)
            {
                // disposing is ignored.  We need to SignalComplete whether this is due to Dispose
                // or due to finalization, so that we don't leave Tasks uncompleted, don't leave
                // connections paused, etc.

                if (!_disposed)
                {
                    EventSourceTrace("Disposing response stream");
                    _disposed = true;
                    SignalComplete(forceCancel: true);
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

            private void EventSourceTrace<TArg0>(string formatMessage, TArg0 arg0, [CallerMemberName] string memberName = null)
            {
                CurlHandler.EventSourceTrace(formatMessage, arg0, agent: null, easy: _easy, memberName: memberName);
            }

            private void EventSourceTrace<TArg0, TArg1, TArg2>(string formatMessage, TArg0 arg0, TArg1 arg1, TArg2 arg2, [CallerMemberName] string memberName = null)
            {
                CurlHandler.EventSourceTrace(formatMessage, arg0, arg1, arg2, agent: null, easy: _easy, memberName: memberName);
            }

            private void EventSourceTrace(string message, [CallerMemberName] string memberName = null)
            {
                CurlHandler.EventSourceTrace(message, agent: null, easy: _easy, memberName: memberName);
            }

            /// <summary>Verifies various invariants that must be true about our state.</summary>
            [Conditional("DEBUG")]
            private void VerifyInvariants()
            {
                Debug.Assert(Monitor.IsEntered(_lockObject), "Can only verify invariants while holding the lock");

                Debug.Assert(_remainingDataCount >= 0, "Remaining data count should never be negative.");
                Debug.Assert(_remainingDataCount == 0 || _remainingData != null, "If there's remaining data, there must be a buffer to store it.");
                Debug.Assert(_remainingData == null || _remainingDataCount <= _remainingData.Length, "The buffer must be large enough for the data length.");
                Debug.Assert(_remainingData == null || _remainingDataOffset <= _remainingData.Length, "The offset must not walk off the buffer.");

                Debug.Assert(!((_remainingDataCount > 0) && (_pendingReadRequest != null)), "We can't have both remaining data and a pending request.");
                Debug.Assert(!((_completed != null) && (_pendingReadRequest != null)), "We can't both be completed and have a pending request.");
                Debug.Assert(_pendingReadRequest == null || !_pendingReadRequest.Task.IsCompleted, "A pending read request must not have been completed yet.");
            }

            /// <summary>State associated with a pending read request.</summary>
            private class ReadState : TaskCompletionSource<int>
            {
                internal readonly Memory<byte> _buffer;

                internal ReadState(Memory<byte> buffer) :  base(TaskCreationOptions.RunContinuationsAsynchronously)
                {
                    _buffer = buffer;
                }
            }

            /// <summary>State associated with a pending read request that's cancelable.</summary>
            private sealed class CancelableReadState : ReadState
            {
                internal readonly CurlResponseStream _stream;
                internal readonly CancellationToken _token;
                internal CancellationTokenRegistration _registration;

                internal CancelableReadState(Memory<byte> buffer, CurlResponseStream responseStream, CancellationToken cancellationToken) : base(buffer)
                {
                    _stream = responseStream;
                    _token = cancellationToken;
                }
            }
        }
    }
}
