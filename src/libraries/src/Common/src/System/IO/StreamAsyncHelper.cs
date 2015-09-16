// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>Provides support for implementing asynchronous operations on Streams.</summary>
    internal sealed class StreamAsyncHelper
    {
        private SemaphoreSlim _asyncActiveSemaphore;
        private Task _activeReadWriteTask;
        private readonly Stream _stream;

        internal StreamAsyncHelper(Stream stream)
        {
            Debug.Assert(stream != null);
            _stream = stream;
        }

        private SemaphoreSlim EnsureAsyncActiveSemaphoreInitialized()
        {
            // Lazily-initialize _asyncActiveSemaphore.  As we're never accessing the SemaphoreSlim's
            // WaitHandle, we don't need to worry about Disposing it.
            return LazyInitializer.EnsureInitialized(ref _asyncActiveSemaphore, () => new SemaphoreSlim(1, 1));
        }

        internal IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (!_stream.CanRead)
            {
                throw __Error.GetReadNotSupported();
            }

            return BeginReadWrite(true, buffer, offset, count, callback, state);
        }

        internal IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
        {
            if (!_stream.CanWrite)
            {
                throw __Error.GetWriteNotSupported();
            }

            return BeginReadWrite(false, buffer, offset, count, callback, state);
        }

        private IAsyncResult BeginReadWrite(bool isRead, byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            // To avoid a race with a stream's position pointer & generating race 
            // conditions with internal buffer indexes in our own streams that 
            // don't natively support async IO operations when there are multiple 
            // async requests outstanding, we block the calling thread until 
            // the active one completes.
            SemaphoreSlim sem = EnsureAsyncActiveSemaphoreInitialized();
            sem.Wait();
            Debug.Assert(_activeReadWriteTask == null);

            // Create the task to asynchronously run the read or write.  Even though Task implements IAsyncResult,
            // we wrap it in a special IAsyncResult object that stores all of the state for the operation
            // and that we can pass around as a state parameter to all of our delegates.  Even though this
            // is an allocation, this allows us to avoid any closures or non-statically cached delegates
            // for both the Task and its continuation, saving more allocations.
            var asyncResult = new StreamReadWriteAsyncResult(_stream, buffer, offset, count, callback, state);
            Task t;
            if (isRead)
            {
                t = new Task<int>(obj =>
                {
                    var ar = (StreamReadWriteAsyncResult)obj;
                    return ar._stream.Read(ar._buffer, ar._offset, ar._count);
                }, asyncResult, CancellationToken.None, TaskCreationOptions.DenyChildAttach);
            }
            else
            {
                t = new Task(obj =>
                {
                    var ar = (StreamReadWriteAsyncResult)obj;
                    ar._stream.Write(ar._buffer, ar._offset, ar._count);
                }, asyncResult, CancellationToken.None, TaskCreationOptions.DenyChildAttach);
            }

            asyncResult._task = t; // this doesn't happen in the async result's ctor because the Task needs to reference the AR, and vice versa

            if (callback != null)
            {
                t.ContinueWith((_, obj) =>
                {
                    var ar = (StreamReadWriteAsyncResult)obj;
                    ar._callback(ar);
                }, asyncResult, CancellationToken.None, TaskContinuationOptions.DenyChildAttach | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            _activeReadWriteTask = t;
            t.Start(TaskScheduler.Default);

            return asyncResult;
        }

        internal int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }

            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.EndContractBlock();

            var ar = asyncResult as StreamReadWriteAsyncResult;
            var task = _activeReadWriteTask;

            if (task == null || ar == null)
            {
                throw new ArgumentException(SR.InvalidOperation_WrongAsyncResultOrEndReadCalledMultiple);
            }
            else if (task != ar._task)
            {
                throw new InvalidOperationException(SR.InvalidOperation_WrongAsyncResultOrEndReadCalledMultiple);
            }

            Task<int> readTask = task as Task<int>;
            if (readTask == null)
            {
                throw new ArgumentException(SR.InvalidOperation_WrongAsyncResultOrEndReadCalledMultiple);
            }

            try
            {
                return readTask.GetAwaiter().GetResult(); // block until completion, then get result / propagate any exception
            }
            finally
            {
                _activeReadWriteTask = null;
                Debug.Assert(_asyncActiveSemaphore != null, "Must have been initialized in order to get here.");
                _asyncActiveSemaphore.Release();
            }
        }

        internal void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }

            Contract.EndContractBlock();

            var ar = asyncResult as StreamReadWriteAsyncResult;
            var writeTask = _activeReadWriteTask;

            if (writeTask == null || ar == null)
            {
                throw new ArgumentException(SR.InvalidOperation_WrongAsyncResultOrEndWriteCalledMultiple);
            }
            else if (writeTask != ar._task)
            {
                throw new InvalidOperationException(SR.InvalidOperation_WrongAsyncResultOrEndWriteCalledMultiple);
            }
            else if (writeTask is Task<int>)
            {
                throw new ArgumentException(SR.InvalidOperation_WrongAsyncResultOrEndWriteCalledMultiple);
            }

            try
            {
                writeTask.GetAwaiter().GetResult(); // block until completion, then propagate any exceptions
            }
            finally
            {
                _activeReadWriteTask = null;
                Debug.Assert(_asyncActiveSemaphore != null, "Must have been initialized in order to get here.");
                _asyncActiveSemaphore.Release();
            }
        }

        private sealed class StreamReadWriteAsyncResult : IAsyncResult
        {
            internal readonly Stream _stream;
            internal readonly byte[] _buffer;
            internal readonly int _offset;
            internal readonly int _count;
            internal readonly AsyncCallback _callback;
            internal readonly object _state;

            internal Task _task;

            internal StreamReadWriteAsyncResult(Stream stream, byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                _stream = stream;
                _buffer = buffer;
                _offset = offset;
                _count = count;
                _callback = callback;
                _state = state;
            }

            object IAsyncResult.AsyncState { get { return _state; } } // return caller-provided state, not that from the task
            bool IAsyncResult.CompletedSynchronously { get { return false; } } // we always complete asynchronously

            bool IAsyncResult.IsCompleted { get { return _task.IsCompleted; } }
            WaitHandle IAsyncResult.AsyncWaitHandle { get { return ((IAsyncResult)_task).AsyncWaitHandle; } }
        }
    }
}