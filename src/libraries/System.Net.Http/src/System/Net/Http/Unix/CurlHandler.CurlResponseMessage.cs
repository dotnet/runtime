// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using size_t = System.UInt64; // TODO: IntPtr

namespace System.Net.Http
{
    internal partial class CurlHandler : HttpMessageHandler
    {
        private sealed class CurlResponseMessage : HttpResponseMessage
        {
            internal readonly EasyRequest _easy;
            private readonly CurlResponseStream _responseStream;

            internal CurlResponseMessage(EasyRequest easy)
            {
                Debug.Assert(easy != null, "Expected non-null EasyRequest");
                _easy = easy;
                _responseStream = new CurlResponseStream(easy);
                RequestMessage = easy._requestMessage;
                Content = new StreamContent(_responseStream);
            }

            internal CurlResponseStream ResponseStream
            {
                get { return _responseStream; }
            }
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
            /// <summary>A cached task storing the Int32 value 0.</summary>
            private static readonly Task<int> s_zeroTask = Task.FromResult(0);

            /// <summary>
            /// A sentinel object used in the <see cref="_completed"/> field to indicate that
            /// the stream completed successfully.
            /// </summary>
            private static readonly Exception s_completionSentinel = new Exception("s_completionSentinel");

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

            public override bool CanRead { get { return !_disposed; } }
            public override bool CanWrite { get { return false; } }
            public override bool CanSeek { get { return false; } }

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
            /// <see cref="Interop.libcurl.CURL_WRITEFUNC_PAUSE"/> if the data wasn't copied and the connection
            /// should be paused until a reader is available.
            /// </returns>
            internal size_t TransferDataToStream(IntPtr pointer, long length)
            {
                Debug.Assert(pointer != IntPtr.Zero, "Expected a non-null pointer");
                Debug.Assert(length >= 0, "Expected a non-negative length");
                VerboseTrace("length: " + length);

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
                    if (_remainingDataCount > 0 || _pendingReadRequest == null)
                    {
                        VerboseTrace("Pausing due to _remainingDataCount: " + _remainingDataCount + ", _pendingReadRequest: " + (_pendingReadRequest != null));
                        _easy._paused = EasyRequest.PauseState.Paused;
                        return Interop.libcurl.CURL_WRITEFUNC_PAUSE;
                    }

                    // There's no data in the buffer and there is a pending read request.  
                    // Transfer as much data as we can to the read request, completing it.
                    int numBytesForTask = (int)Math.Min(length, _pendingReadRequest._count);
                    Debug.Assert(numBytesForTask > 0, "We must be copying a positive amount.");
                    Marshal.Copy(pointer, _pendingReadRequest._buffer, _pendingReadRequest._offset, numBytesForTask);
                    _pendingReadRequest.SetResult(numBytesForTask);
                    ClearPendingReadRequest();
                    VerboseTrace("Copied to task: " + numBytesForTask);

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
                        VerboseTrace("Allocated new remainingData array of length: " + _remainingData.Length);

                        // Copy the remaining data to the buffer
                        Marshal.Copy(remainingPointer, _remainingData, 0, _remainingDataCount);
                        VerboseTrace("Copied to buffer: " + _remainingDataCount);
                    }

                    // All of the data from libcurl was consumed.
                    return (ulong)length;
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (buffer == null) throw new ArgumentNullException("buffer");
                if (offset < 0) throw new ArgumentOutOfRangeException("offset");
                if (count < 0) throw new ArgumentOutOfRangeException("count");
                if (offset > buffer.Length - count) throw new ArgumentException("buffer");
                CheckDisposed();

                VerboseTrace("buffer: " + buffer.Length + ", offset: " + offset + ", count: " + count);

                // Check for cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    VerboseTrace("Canceled");
                    return Task.FromCanceled<int>(cancellationToken);
                }

                lock (_lockObject)
                {
                    VerifyInvariants();

                    // If there's currently a pending read, fail this read, as we don't support concurrent reads.
                    if (_pendingReadRequest != null)
                    {
                        VerboseTrace("Existing pending read");
                        return Task.FromException<int>(new InvalidOperationException(SR.net_http_content_no_concurrent_reads));
                    }

                    // If the stream was already completed with failure, complete the read as a failure.
                    if (_completed != null && _completed != s_completionSentinel)
                    {
                        VerboseTrace("Failing read with " + _completed);
                        OperationCanceledException oce = _completed as OperationCanceledException;
                        return (oce != null && oce.CancellationToken.IsCancellationRequested) ?
                            Task.FromCanceled<int>(oce.CancellationToken) :
                            Task.FromException<int>(_completed);
                    }

                    // Quick check for if no data was actually requested.  We do this after the check
                    // for errors so that we can still fail the read and transfer the exception if we should.
                    if (count == 0)
                    {
                        VerboseTrace("Zero count");
                        return s_zeroTask;
                    }

                    // If there's any data left over from a previous call, grab as much as we can.
                    if (_remainingDataCount > 0)
                    {
                        int bytesToCopy = Math.Min(count, _remainingDataCount);
                        Array.Copy(_remainingData, _remainingDataOffset, buffer, offset, bytesToCopy);

                        _remainingDataOffset += bytesToCopy;
                        _remainingDataCount -= bytesToCopy;
                        Debug.Assert(_remainingDataCount >= 0, "The remaining count should never go negative");
                        Debug.Assert(_remainingDataOffset <= _remainingData.Length, "The remaining offset should never exceed the buffer size");

                        VerboseTrace("Copied to task: " + bytesToCopy);
                        return Task.FromResult(bytesToCopy);
                    }

                    // If the stream has already been completed, complete the read immediately.
                    if (_completed == s_completionSentinel)
                    {
                        VerboseTrace("Completed successfully after stream completion");
                        return s_zeroTask;
                    }

                    // Finally, the stream is still alive, and we want to read some data, but there's no data 
                    // in the buffer so we need to register ourself to get the next write.
                    if (cancellationToken.CanBeCanceled)
                    {
                        // If the cancellation token is cancelable, then we need to register for cancellation.
                        // We creat a special CancelableReadState that carries with it additional info:
                        // the cancellation token and the registration with that token.  When cancellation
                        // is requested, we schedule a work item that tries to remove the read state
                        // from being pending, canceling it in the process.  This needs to happen under the
                        // lock, which is why we schedule the operation to run asynchronously: if it ran
                        // synchronously, it could deadlock due to code on another thread holding the lock
                        // and calling Dispose on the registration concurrently with the call to Cancel
                        // the cancellation token.  Dispose on the registration won't return until the action
                        // associated with the registration has completed, but if that action is currently
                        // executing and is blocked on the lock that's held while calling Dispose... deadlock.
                        var crs = new CancelableReadState(buffer, offset, count, this, cancellationToken);
                        crs._registration = cancellationToken.Register(s1 =>
                        {
                            ((CancelableReadState)s1)._stream.VerboseTrace("Cancellation invoked. Queueing work item to cancel read state.");
                            Task.Factory.StartNew(s2 =>
                            {
                                var crsRef = (CancelableReadState)s2;
                                Debug.Assert(crsRef._token.IsCancellationRequested, "We should only be here if cancellation was requested.");
                                lock (crsRef._stream._lockObject)
                                {
                                    if (crsRef._stream._pendingReadRequest == crsRef)
                                    {
                                        crsRef.TrySetCanceled(crsRef._token);
                                        crsRef._stream.ClearPendingReadRequest();
                                    }
                                }
                            }, s1, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
                        }, crs);
                        _pendingReadRequest = crs;
                        VerboseTrace("Created pending cancelable read");
                    }
                    else
                    {
                        // The token isn't cancelable.  Just create a normal read state.
                        _pendingReadRequest = new ReadState(buffer, offset, count);
                        VerboseTrace("Created pending read");
                    }

                    RequestUnpause();
                    return _pendingReadRequest.Task;
                }
            }

            /// <summary>Requests that libcurl unpause the connection associated with this request.</summary>
            private void RequestUnpause()
            {
                if (_easy._paused == EasyRequest.PauseState.Paused)
                {
                    VerboseTrace("Issuing unpause request");
                    _easy._paused = EasyRequest.PauseState.UnpauseRequestIssued;
                    _easy._associatedMultiAgent.Queue(
                        new MultiAgent.IncomingRequest { Easy = _easy, Type = MultiAgent.IncomingRequestType.Unpause });
                }
                else
                {
                    VerboseTrace("Not issuing unpause request with state " + _easy._paused);
                }
            }

            /// <summary>Notifies the stream that no more data will be written.</summary>
            internal void SignalComplete(Exception error = null)
            {
                lock (_lockObject)
                {
                    VerifyInvariants();

                    // If we already completed, nothing more to do
                    if (_completed != null)
                    {
                        return;
                    }

                    // Mark ourselves as being completed
                    _completed = error != null ?
                        error :
                        s_completionSentinel;

                    // If there's a pending read request, complete it, either with 0 bytes for success
                    // or with the exception/CancellationToken for failure.
                    if (_pendingReadRequest != null)
                    {
                        if (_completed == s_completionSentinel)
                        {
                            VerboseTrace("Completed pending read task with 0 bytes.");
                            _pendingReadRequest.TrySetResult(0);
                        }
                        else
                        {
                            VerboseTrace("Completed pending read task with " + _completed);
                            OperationCanceledException oce = _completed as OperationCanceledException;
                            if (oce != null)
                            {
                                _pendingReadRequest.TrySetCanceled(oce.CancellationToken);
                            }
                            else
                            {
                                _pendingReadRequest.TrySetException(_completed);
                            }
                        }

                        ClearPendingReadRequest();
                    }
                }
            }

            /// <summary>Clears a pending read request, making sure any cancellation registration is unregistered.</summary>
            private void ClearPendingReadRequest()
            {
                Debug.Assert(Monitor.IsEntered(_lockObject), "Lock object must be held to manipulate _pendingReadRequest");
                Debug.Assert(_pendingReadRequest != null, "Should only be clearing the pending read request if there is one");

                var crs = _pendingReadRequest as CancelableReadState;
                if (crs != null)
                {
                    crs._registration.Dispose();
                }

                _pendingReadRequest = null;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing && !_disposed)
                {
                    _disposed = true;
                    SignalComplete();
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

            [Conditional(VerboseDebuggingConditional)]
            private void VerboseTrace(string text = null, [CallerMemberName] string memberName = null)
            {
                CurlHandler.VerboseTrace(text, memberName, _easy);
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

                Debug.Assert(!_disposed || _completed != null, "If disposed, the stream must also be completed.");
            }

            /// <summary>State associated with a pending read request.</summary>
            private class ReadState : TaskCompletionSource<int>
            {
                internal readonly byte[] _buffer;
                internal readonly int _offset;
                internal readonly int _count;

                internal ReadState(byte[] buffer, int offset, int count) : 
                    base(TaskCreationOptions.RunContinuationsAsynchronously)
                {
                    Debug.Assert(buffer != null, "Need non-null buffer");
                    Debug.Assert(offset >= 0, "Need non-negative offset");
                    Debug.Assert(count > 0, "Need positive count");
                    _buffer = buffer;
                    _offset = offset;
                    _count = count;
                }
            }

            /// <summary>State associated with a pending read request that's cancelable.</summary>
            private sealed class CancelableReadState : ReadState
            {
                internal readonly CurlResponseStream _stream;
                internal readonly CancellationToken _token;
                internal CancellationTokenRegistration _registration;

                internal CancelableReadState(byte[] buffer, int offset, int count, 
                    CurlResponseStream responseStream, CancellationToken cancellationToken) :
                    base(buffer, offset, count)
                {
                    _stream = responseStream;
                    _token = cancellationToken;
                }
            }

        }
    }
}
