// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;

namespace System.Net.Http
{
    // libcurl has a pull-based model and needs to be able to read data on-demand
    // from the request content stream.  HttpContent provides a ReadAsStreamAsync method
    // that would theoretically enable this, however it's actually not intended for such
    // scenarios and has some deficiencies in this regard.  For example, in certain scenarios,
    // we may need to retry an HTTP request, which means sending the data again.  In such
    // situations, libcurl will ask us to rewind the stream.  We can do this with ReadAsStreamAsync,
    // but only if the returned stream supports seeking.  If it doesn't support seeking, we
    // could ask for a new stream again via calling ReadAsStreamAsync again, but it doesn't
    // do content consumption detection like you get with HttpContent.CopyToAsync.  CopyToAsync
    // is the preferred, supported mechanism for a handler getting data from the HttpContent,
    // and is what WinHttpHandler uses, but it also provides a push-based model.  In order to
    // be able to pull from a push-based model, we need an adapter stream that straddles
    // the divide.  This implementation provides that.

    internal partial class CurlHandler : HttpMessageHandler
    {
        /// <summary>
        /// Provides a stream that lets CurlHandler read the data from an
        /// HttpContent via HttpContent's CopyToAsync.
        /// </summary>
        private sealed class HttpContentAsyncStream : Stream
        {
            /// <summary>Cached task that contains the Int32 value 0.</int></summary>
            private static readonly Task<int> s_zeroTask = Task.FromResult(0);

            /// <summary>Object used to synchronize all activity on the stream.</summary>
            private readonly object _syncObj = new object();
            /// <summary>The associated EasyRequest.</summary>
            private readonly EasyRequest _easy;
            /// <summary>The transportContext of CurlHandler </summary>
            private readonly CurlTransportContext _transportContext = new CurlTransportContext();

            /// <summary>true if the stream has been disposed; otherwise, false.</summary>
            private bool _disposed;
            /// <summary>Task representing the transfer of data from the HttpContent to this stream.</summary>
            private Task _copyTask;

            /// <summary>TaskCompletionSource for the currently outstanding read or write operation.</summary>
            private TaskCompletionSource<int> _asyncOp;
            /// <summary>Cancellation registration associated with the currently pending async operation.</summary>
            private CancellationTokenRegistration _cancellationRegistration;

            /// <summary>Whether the currently outstanding async operation is a reader.</summary>
            private bool _waiterIsReader;
            /// <summary>The current outstanding async operation's buffer.</summary>
            private byte[] _buffer;
            /// <summary>The current outstanding async operations offset.</summary>
            private int _offset;
            /// <summary>The current outstanding async operations count.</summary>
            private int _count;

            /// <summary>Initializes the stream.</summary>
            /// <param name="easy">The associated easy request containing the content to transfer to this stream.</param>
            internal HttpContentAsyncStream(EasyRequest easy)
            {
                Debug.Assert(easy != null, "Expected non-null easy");
                Debug.Assert(easy._requestMessage?.Content != null, "Expected non-null content");
                _easy = easy;
            }

            /// <summary>Gets whether the stream is writable. It is.</summary>
            public override bool CanWrite { get { return true; } }

            /// <summary>Gets whether the stream is readable.  It's not (at least not through the public Read* methods).</summary>
            public override bool CanRead { get { return false; } }
            
            /// <summary>Gets whether the stream is seekable. It's not.</summary>
            public override bool CanSeek { get { return false; } }

            /// <summary>
            /// Reads asynchronously from the data written to the stream.  Since this stream is exposed out
            /// as an argument to HttpContent.CopyToAsync, and since we don't want that code attempting to read
            /// from this stream (which will mess with consumption by libcurl's callbacks), we mark the stream
            /// as CanRead==false and throw NotSupportedExceptions from the public Read* methods, with internal
            /// usage using this ReadAsyncInternal method instead.
            /// </summary>
            internal Task<int> ReadAsyncInternal(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                EventSourceTrace("Buffer={0}, Offset={1}, Count={2}", buffer.Length, offset, count);

                // If no data was requested, give back 0 bytes.
                if (count == 0)
                {
                    return s_zeroTask;
                }

                // If cancellation was requested, bail.
                if (cancellationToken.IsCancellationRequested)
                {
                    EventSourceTrace("CancellationToken canceled");
                    return Task.FromCanceled<int>(cancellationToken);
                }

                lock (_syncObj)
                {
                    // If we've been disposed, return that no data is available.
                    if (_disposed)
                    {
                        EventSourceTrace("Content stream already disposed");
                        return s_zeroTask;
                    }

                    // If we've already completed, return 0 bytes or fail with the same error that the CopyToAsync failed with.
                    if (_copyTask.IsCompleted && (_buffer == null || _count == 0))
                    {
                        EventSourceTrace("Copy already completed: {0}", _copyTask.Status);
                        return _copyTask.Status == TaskStatus.RanToCompletion ?
                            s_zeroTask :
                            PropagateError<int>(_copyTask);
                    }

                    Debug.Assert(!_waiterIsReader, "Must not have two readers concurrently");

                    // If there's no writer waiting for us, register our buffer/offset/count and return.
                    if (_buffer == null)
                    {
                        EventSourceTrace("No waiting writer. Queueing");
                        Update(true, buffer, offset, count, NewTcs());
                        RegisterCancellation(cancellationToken);
                        return _asyncOp.Task;
                    }

                    // There's writer data available to use.  Read it to satisfy this request.
                    int bytesToCopy = Math.Min(_count, count);
                    Debug.Assert(bytesToCopy > 0, "Expected to be copying at least 1 byte");
                    Buffer.BlockCopy(_buffer, _offset, buffer, offset, bytesToCopy);

                    if (bytesToCopy == _count)
                    {
                        // We exhausted all of the writer's data, so complete the writer
                        // and clear out the state.
                        TaskCompletionSource<int> tcs = _asyncOp;
                        Update(false, null, 0, 0, null);
                        _cancellationRegistration.Dispose();
                        if (tcs != null) // could be null if the writer was synchronous
                        {
                            EventSourceTrace("Completing waiting writer");
                            bool writerCompleted = tcs.TrySetResult(default(int)); // value doesn't matter, as the Task is a writer
                            Debug.Assert(writerCompleted, "No one else should have completed the writer");
                        }
                    }
                    else
                    {
                        // The writer still has more data to provide to a future read.
                        // Update based on this read.
                        _offset += bytesToCopy;
                        _count -= bytesToCopy;
                    }

                    // Return the number of bytes read.
                    EventSourceTrace("Read {0} bytes, {1} remain buffered", bytesToCopy, _count, 0);
                    return Task.FromResult(bytesToCopy);
                }
            }

            /// <summary>
            /// Copies the data from the buffer to the pending write buffer.  If there's currently
            /// a buffer, this appends to it, and if there isn't, it replaces it.
            /// </summary>
            private void AppendSynchronousWriteBuffer(byte[] buffer, int offset, int count)
            {
                if (_buffer == null)
                {
                    // There isn't currently a buffer, so copy the data into a new one
                    byte[] tmp = new byte[count];
                    Buffer.BlockCopy(buffer, offset, tmp, 0, count);
                    Update(false, tmp, 0, count, null);
                }
                else
                {
                    // There is currently a buffer.
                    long newCount = _count + (long)count;
                    if (_buffer.Length >= newCount) 
                    {
                        // Enough space remains in the buffer to copy into it.
                        if (_offset != 0 && _count != 0)
                        {
                            Buffer.BlockCopy(_buffer, _offset, _buffer, 0, _count); // shift the existing data down to the beginning
                        }
                        Buffer.BlockCopy(buffer, offset, _buffer, _count, count); // append the new data
                        Update(false, _buffer, 0, (int)newCount, null);
                    }
                    else
                    {
                        // Not enough space remains in the buffer... we need a new one.
                        byte[] tmp = new byte[Math.Max(_buffer.Length * 2L, newCount)]; // grow to at least 2x the current size
                        if (_count != 0)
                        {
                            Buffer.BlockCopy(_buffer, _offset, tmp, 0, _count);
                        }
                        Buffer.BlockCopy(buffer, offset, tmp, _count, count);
                        Update(false, tmp, 0, (int)newCount, null);
                    }
                }
                EventSourceTrace("No waiting reader. Stored {0} additional bytes, {1} total", count, _count, 0);
            }

            /// <summary>Validate arguments to Write and WriteAsync.</summary>
            private void ValidateWriteArgs(byte[] buffer, int offset, int count)
            {
                if (buffer == null) throw new ArgumentNullException(nameof(buffer));
                if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
                if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
                if (buffer.Length - offset < count) throw new ArgumentOutOfRangeException(nameof(buffer));
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                ValidateWriteArgs(buffer, offset, count);
                EventSourceTrace("Buffer={0}, Offset={1}, Count={2}", buffer.Length, offset, count);

                // This stream is used by HttpContent.CopyToAsync, and we expect the writes to all be asynchronous via WriteAsync.
                // However, it's possible for an override of CopyToAsync to use Write, and if it does, and if we just delegated
                // from here to WriteAsync(...).GetAwaiter().GetResult(), we would likely deadlock, as the CopyToAsync call is
                // invoked from the single thread responsible for processing libcurl interactions, including supplying the reader
                // that would unblock the writer.  To handle this, we have two options: schedule the CopyToAsync to be invoked
                // asynchronously just in case it does a synchronous write, or implement the write to buffer the data.  The former
                // adds cost to the expected / common case, but it can also lead to concurrency problems and spurious disposals,
                // as if the request is faulted/canceled between the time that a task to invoke CopyToAsync is scheduled and the
                // time that it actually executes, we could be invoking CopyToAsync on a disposed HttpContent; worse, we could
                // end up invoking CopyToAsync while it's being disposed, which could lead to arbitrary state corruption.  In
                // comparison, buffering has the downside of taking up more and arbitrary amounts of memory to store all of the
                // inputs.  But, we expect synchronous Writes to be rare, and code shouldn't be doing it anyway, so we opt
                // for buffering in order to ensure correctness and to optimize for the expected case.

                // If no data was provided, we're done.
                if (count == 0)
                {
                    return;
                }

                lock (_syncObj)
                {
                    // If we've been disposed, throw an exception so as to end the CopyToAsync operation.
                    if (_disposed)
                    {
                        EventSourceTrace("Stream already disposed");
                        throw new ObjectDisposedException(GetType().FullName);
                    }

                    if (!_waiterIsReader)
                    {
                        // There's no waiting reader.  Store everything for later.
                        Debug.Assert(_asyncOp == null, "No one else should be waiting");
                        AppendSynchronousWriteBuffer(buffer, offset, count);
                        return;
                    }

                    // There's a waiting reader.  We'll give them our data and store for later anything they can't use.
                    Debug.Assert(_count > 0, "Expected reader to need at least 1 byte");
                    Debug.Assert(_asyncOp != null, "Reader should have created a task to complete");

                    // Complete the waiting reader with however much we can give them
                    int bytesToCopy = Math.Min(count, _count);
                    Buffer.BlockCopy(buffer, offset, _buffer, _offset, bytesToCopy);
                    _cancellationRegistration.Dispose();
                    EventSourceTrace("Completing reader with {0} bytes", bytesToCopy);
                    bool completed = _asyncOp.TrySetResult(bytesToCopy);
                    Debug.Assert(completed, "No one else should have completed the reader");

                    Update(false, null, 0, 0, null); // set buffer to null so that if we append we don't add to reader's buffer
                    if (bytesToCopy < count)
                    {
                        // If we have more bytes from this write, copy for use by a reader later.
                        AppendSynchronousWriteBuffer(buffer, offset + bytesToCopy, count - bytesToCopy);
                    }
                }
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ValidateWriteArgs(buffer, offset, count);
                EventSourceTrace("Buffer={0}, Offset={1}, Count={2}", buffer.Length, offset, count);

                // If no data was provided, we're done.
                if (count == 0)
                {
                    return Task.CompletedTask;
                }

                // If cancellation was requested, bail.
                if (cancellationToken.IsCancellationRequested)
                {
                    EventSourceTrace("CancellationToken canceled");
                    return Task.FromCanceled<int>(cancellationToken);
                }

                lock (_syncObj)
                {
                    // If we've been disposed, throw an exception so as to end the CopyToAsync operation.
                    if (_disposed)
                    {
                        EventSourceTrace("Stream already disposed");
                        throw new ObjectDisposedException(GetType().FullName);
                    }

                    // If there's no waiting reader, store everything for later and return.
                    if (!_waiterIsReader)
                    {
                        Debug.Assert(_asyncOp == null, "No one else should be waiting");
                        if (_buffer == null)
                        {
                            // The common-case: there's no waiting reader, and writes are being done
                            // asynchronously, so there's no buffer currently stored.  Store our
                            // buffer and register as the waiting reader.
                            EventSourceTrace("No waiting reader. Queueing");
                            Update(false, buffer, offset, count, NewTcs());
                            RegisterCancellation(cancellationToken);
                            return _asyncOp.Task;
                        }
                        else
                        {
                            // The existing buffer is non-null.  This can only mean that a previous
                            // synchronous Write call was made.  Our only good option at this point
                            // is to treat this as a synchronous Write and copy our data in.
                            Debug.Assert(_offset != 0 || _count != 0, $"Expected existing write, got _offset={_offset}, _count={_count}");
                            AppendSynchronousWriteBuffer(buffer, offset, count);
                            return Task.CompletedTask;
                        }
                    }

                    // There's a waiting reader.  We'll give them our data and store
                    // for later anything they can't use.
                    Debug.Assert(_buffer != null, "Expected non-null buffer from waiting reader");
                    Debug.Assert(_count > 0, "Expected reader to need at least 1 byte");
                    Debug.Assert(_asyncOp != null, "Reader should have created a task to complete");

                    // Complete the waiting reader with however much we can give them
                    int bytesToCopy = Math.Min(count, _count);
                    Buffer.BlockCopy(buffer, offset, _buffer, _offset, bytesToCopy);
                    _cancellationRegistration.Dispose();
                    EventSourceTrace("Copying {0} bytes to reader", bytesToCopy);
                    bool completed = _asyncOp.TrySetResult(bytesToCopy);
                    Debug.Assert(completed, "No one else should have completed the reader");

                    if (bytesToCopy < count)
                    {
                        // If we have more bytes from this write, store the arguments
                        // for use later and return a task to be completed by a reader.
                        EventSourceTrace("Storing {0} bytes for later", count - bytesToCopy);
                        Update(false, buffer, offset + bytesToCopy, count - bytesToCopy, NewTcs());
                        RegisterCancellation(cancellationToken);
                        return _asyncOp.Task;
                    }
                    else
                    {
                        // We used up all of the data in this write, so complete
                        // the caller synchronously.
                        Update(false, null, 0, 0, null);
                        return Task.CompletedTask;
                    }
                }
            }

            public override void Flush() { }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return cancellationToken.IsCancellationRequested ?
                    Task.FromCanceled(cancellationToken) :
                    Task.CompletedTask;
            }

            protected override void Dispose(bool disposing)
            {
                lock (_syncObj)
                {
                    if (!_disposed)
                    {
                        // Mark ourselves as disposed, and make sure to wake
                        // up any writer or reader who's been waiting.
                        EventSourceTrace("Disposing request stream");
                        _disposed = true;
                        if (_asyncOp != null)
                        {
                            _cancellationRegistration.Dispose();
                            bool canceled = _asyncOp.TrySetCanceled();
                            Debug.Assert(canceled, "No one else should have completed the operation.");
                        }
                        Update(false, null, 0, 0, null);
                    }
                }

                base.Dispose(disposing);
            }

            /// <summary>Registers with the specified cancellation token to cancel the pending operation.</summary>
            private void RegisterCancellation(CancellationToken cancellationToken)
            {
                if (!cancellationToken.CanBeCanceled)
                    return;

                _cancellationRegistration = cancellationToken.Register(s =>
                {
                    var thisRef = (HttpContentAsyncStream)s;
                    thisRef.EventSourceTrace("Cancellation requested");
                    lock (thisRef._syncObj)
                    {
                        if (thisRef._asyncOp != null)
                        {
                            bool canceled = thisRef._asyncOp.TrySetCanceled();
                            Debug.Assert(canceled, "Expected to be able to cancel pending async op");
                            thisRef.Update(false, null, 0, 0, null);
                        }
                    }
                }, this);
            }

            /// <summary>
            /// Try to rewind the stream to the beginning.  If successful, the next call to ReadAsync
            /// will initiate a new CopyToAsync operation.  Reset is only successful when there's
            /// no CopyToAsync in progress.
            /// </summary>
            /// <returns></returns>
            internal bool TryReset()
            {
                lock (_syncObj)
                {
                    if (_copyTask == null || _copyTask.IsCompleted)
                    {
                        EventSourceTrace("TryReset successful");
                        _copyTask = null;
                        return true;
                    }
                }

                EventSourceTrace("TryReset failed due to in-progress copy.");
                return false;
            }

            /// <summary>Initiate a new copy from the HttpContent to this stream. This is not thread-safe.</summary>
            internal void Run()
            {
                Debug.Assert(!Monitor.IsEntered(_syncObj), "Should not be invoked while holding the lock");
                Debug.Assert(_copyTask == null, "Should only be invoked after construction or a reset");

                // Start the copy and store the task to represent it.
                _copyTask = _easy._requestMessage.Content.CopyToAsync(this, _transportContext);

                // Fix up the instance when it's done
                _copyTask.ContinueWith((t, s) => ((HttpContentAsyncStream)s).EndCopyToAsync(t), this,
                    CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            /// <summary>  passes the channel binding token to the transport context </summary>
            internal void SetChannelBindingToken(X509Certificate2 certificate)
            {
                _transportContext.CurlChannelBinding.SetToken(certificate);
            }

            /// <summary>Completes a CopyToAsync initiated in Run.</summary>
            private void EndCopyToAsync(Task completedCopy)
            {
                Debug.Assert(!Monitor.IsEntered(_syncObj), "Should not be invoked while holding the lock");
                lock (_syncObj)
                {
                    if (completedCopy.IsFaulted)
                    {
                        EventSourceTrace("Copy failed: {0}", completedCopy.Exception.InnerException);
                    }
                    else
                    {
                        EventSourceTrace("Copy completed: {0}", completedCopy.Status);
                    }

                    // We're done transferring, but a reader doesn't know that until they successfully
                    // read 0 bytes, which won't happen until either a) we complete an already waiting
                    // reader or b) a new reader comes along and see _copyTask completed.  We also need
                    // to make sure that, once we do signal completion by giving a reader 0 bytes, we're
                    // immediately ready for a TryReset, which means the _copyTask needs to be completed.
                    // As such, while we're holding the lock, we need to both give 0 bytes to any waiting
                    // reader and force _copyTask to complete, which we do by replacing this Task's
                    // reference with one known to already be complete.

                    if (_asyncOp != null)
                    {
                        _cancellationRegistration.Dispose();
                        Debug.Assert(_waiterIsReader, "We're done writing, so a waiter must be a reader");
                        if (completedCopy.IsFaulted)
                        {
                            _asyncOp.TrySetException(completedCopy.Exception.InnerException);
                            _copyTask = completedCopy;
                        }
                        else if (completedCopy.IsCanceled)
                        {
                            _asyncOp.TrySetCanceled();
                            _copyTask = completedCopy;
                        }
                        else
                        {
                            _asyncOp.TrySetResult(0);
                            _copyTask = s_zeroTask;
                        }
                        Update(false, null, 0, 0, null);
                    }
                    else
                    {
                        _copyTask = completedCopy.Status == TaskStatus.RanToCompletion ? s_zeroTask : completedCopy;
                    }
                }
            }

            /// <summary>Returns a task that's faulted or canceled based on the specified task's state.</summary>
            private static async Task<T> PropagateError<T>(Task failedTask)
            {
                Debug.Assert(failedTask.IsFaulted || failedTask.IsCanceled, "Task must have already faulted or been canceled");
                await failedTask;
                return default(T); // should never get here
            }

            /// <summary>Updates the state on the stream to the specified values.</summary>
            private void Update(bool isReader, byte[] buffer, int offset, int count, TaskCompletionSource<int> asyncOp)
            {
                _waiterIsReader = isReader;
                _buffer = buffer;
                _offset = offset;
                _count = count;
                _asyncOp = asyncOp;
            }

            /// <summary>Creates a new TaskCompletionSource to use to represent a pending read or write.</summary>
            private static TaskCompletionSource<int> NewTcs()
            {
                return new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public override long Length
            {
                get { throw new NotSupportedException(); }
            }

            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                // Reading should only be performed by CurlHandler, and it should always do it with 
                // ReadAsyncInternal, not Read or ReadAsync.
                throw new NotSupportedException();
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                // Reading should only be performed by CurlHandler, and it should always do it with 
                // ReadAsyncInternal, not Read or ReadAsync.
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
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

        }
    }
}
