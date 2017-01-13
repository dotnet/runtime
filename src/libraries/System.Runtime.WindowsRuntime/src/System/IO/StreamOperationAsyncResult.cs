// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace System.IO
{
    #region class StreamOperationAsyncResult

    internal abstract partial class StreamOperationAsyncResult : IAsyncResult
    {
        private AsyncCallback _userCompletionCallback = null;
        private Object _userAsyncStateInfo = null;

        private IAsyncInfo _asyncStreamOperation = null;

        private volatile bool _completed = false;
        private volatile bool _callbackInvoked = false;
        private volatile ManualResetEvent _waitHandle = null;

        private Int64 _bytesCompleted = 0;

        private ExceptionDispatchInfo _errorInfo = null;

        private readonly bool _processCompletedOperationInCallback;
        private IAsyncInfo _completedOperation = null;


        protected internal StreamOperationAsyncResult(IAsyncInfo asyncStreamOperation,
                                                      AsyncCallback userCompletionCallback, Object userAsyncStateInfo,
                                                      bool processCompletedOperationInCallback)
        {
            if (asyncStreamOperation == null)
                throw new ArgumentNullException("asyncReadOperation");

            _userCompletionCallback = userCompletionCallback;
            _userAsyncStateInfo = userAsyncStateInfo;

            _asyncStreamOperation = asyncStreamOperation;

            _completed = false;
            _callbackInvoked = false;

            _bytesCompleted = 0;

            _errorInfo = null;

            _processCompletedOperationInCallback = processCompletedOperationInCallback;
        }


        public object AsyncState
        {
            get { return _userAsyncStateInfo; }
        }


        internal bool ProcessCompletedOperationInCallback
        {
            get { return _processCompletedOperationInCallback; }
        }


#pragma warning disable 420  // "a reference to a volatile field will not be treated as volatile"

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                ManualResetEvent wh = _waitHandle;
                if (wh != null)
                    return wh;

                // What if someone calls this public property and decides to wait on it?
                // > Use 'completed' in the ctor - this way the handle wait will return as appropriate.
                wh = new ManualResetEvent(_completed);

                ManualResetEvent otherHandle = Interlocked.CompareExchange(ref _waitHandle, wh, null);

                // We lost the race. Dispose OUR handle and return OTHER handle:
                if (otherHandle != null)
                {
                    wh.Dispose();
                    return otherHandle;
                }

                // We won the race. Return OUR new handle:
                return wh;
            }
        }

#pragma warning restore 420  // "a reference to a volatile field will not be treated as volatile"


        public bool CompletedSynchronously
        {
            get { return false; }
        }


        public bool IsCompleted
        {
            get { return _completed; }
        }


        internal void Wait()
        {
            if (_completed)
                return;

            WaitHandle wh = AsyncWaitHandle;

            while (_completed == false)
                wh.WaitOne();
        }


        internal Int64 BytesCompleted
        {
            get { return _bytesCompleted; }
        }


        internal bool HasError
        {
            get { return _errorInfo != null; }
        }


        internal void ThrowCachedError()
        {
            if (_errorInfo == null)
                return;

            _errorInfo.Throw();
        }


        internal bool CancelStreamOperation()
        {
            if (_callbackInvoked)
                return false;

            if (_asyncStreamOperation != null)
            {
                _asyncStreamOperation.Cancel();
                _asyncStreamOperation = null;
            }

            return true;
        }

        internal void CloseStreamOperation()
        {
            try
            {
                if (_asyncStreamOperation != null)
                    _asyncStreamOperation.Close();
            }
            catch { }
            _asyncStreamOperation = null;
        }


        ~StreamOperationAsyncResult()
        {
            // This finalisation is not critical, but we can still make an effort to notify the underlying WinRT stream
            // that we are not any longer interested in the results:
            CancelStreamOperation();
        }


        internal abstract void ProcessConcreteCompletedOperation(IAsyncInfo completedOperation, out Int64 bytesCompleted);


        private static void ProcessCompletedOperation_InvalidOperationThrowHelper(ExceptionDispatchInfo errInfo, String errMsg)
        {
            Exception errInfoSrc = (errInfo == null) ? null : errInfo.SourceException;

            if (errInfoSrc == null)
                throw new InvalidOperationException(errMsg);
            else
                throw new InvalidOperationException(errMsg, errInfoSrc);
        }


        internal void ProcessCompletedOperation()
        {
            // The error handling is slightly tricky here:
            // Before processing the IO results, we are verifying some basic assumptions and if they do not hold, we are
            // throwing InvalidOperation. However, by the time this method is called, we might have already stored something
            // into errorInfo, e.g. if an error occurred in StreamOperationCompletedCallback. If that is the case, then that
            // previous exception might include some important info relevant for detecting the problem. So, we take that
            // previous exception and attach it as the inner exception to the InvalidOperationException being thrown.
            // In cases where we have a good understanding of the previously saved errorInfo, and we know for sure that it
            // the immediate reason for the state validation to fail, we can avoid throwing InvalidOperation altogether
            // and only rethrow the errorInfo.

            if (!_callbackInvoked)
                ProcessCompletedOperation_InvalidOperationThrowHelper(_errorInfo, SR.InvalidOperation_CannotCallThisMethodInCurrentState);

            if (!_processCompletedOperationInCallback && !_completed)
                ProcessCompletedOperation_InvalidOperationThrowHelper(_errorInfo, SR.InvalidOperation_CannotCallThisMethodInCurrentState);

            if (_completedOperation == null)
            {
                ExceptionDispatchInfo errInfo = _errorInfo;
                Exception errInfoSrc = (errInfo == null) ? null : errInfo.SourceException;

                // See if errorInfo is set because we observed completedOperation == null previously (being slow is Ok on error path):
                if (errInfoSrc != null && errInfoSrc is NullReferenceException
                        && SR.NullReference_IOCompletionCallbackCannotProcessNullAsyncInfo.Equals(errInfoSrc.Message))
                {
                    errInfo.Throw();
                }
                else
                {
                    throw new InvalidOperationException(SR.InvalidOperation_CannotCallThisMethodInCurrentState);
                }
            }

            if (_completedOperation.Id != _asyncStreamOperation.Id)
                ProcessCompletedOperation_InvalidOperationThrowHelper(_errorInfo, SR.InvalidOperation_UnexpectedAsyncOperationID);

            if (_completedOperation.Status == AsyncStatus.Error)
            {
                _bytesCompleted = 0;
                ThrowWithIOExceptionDispatchInfo(_completedOperation.ErrorCode);
            }

            ProcessConcreteCompletedOperation(_completedOperation, out _bytesCompleted);
        }


        internal void StreamOperationCompletedCallback(IAsyncInfo completedOperation, AsyncStatus unusedCompletionStatus)
        {
            try
            {
                if (_callbackInvoked)
                    throw new InvalidOperationException(SR.InvalidOperation_MultipleIOCompletionCallbackInvocation);

                _callbackInvoked = true;

                // This happens in rare stress cases in Console mode and the WinRT folks said they are unlikely to fix this in Dev11.
                // Moreover, this can happen if the underlying WinRT stream has a faulty user implementation.
                // If we did not do this check, we would either get the same exception without the explaining message when dereferencing
                // completedOperation later, or we will get an InvalidOperation when processing the Op. With the check, they will be
                // aggregated and the user will know what went wrong.
                if (completedOperation == null)
                    throw new NullReferenceException(SR.NullReference_IOCompletionCallbackCannotProcessNullAsyncInfo);

                _completedOperation = completedOperation;

                // processCompletedOperationInCallback == false indicates that the stream is doing a blocking wait on the waitHandle of this IAsyncResult.
                // In that case calls on completedOperation may deadlock if completedOperation is not free threaded.
                // By setting processCompletedOperationInCallback to false the stream that created this IAsyncResult indicated that it
                // will call ProcessCompletedOperation after the waitHandle is signalled to fetch the results.

                if (_processCompletedOperationInCallback)
                    ProcessCompletedOperation();
            }
            catch (Exception ex)
            {
                _bytesCompleted = 0;
                _errorInfo = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                _completed = true;
                Interlocked.MemoryBarrier();
                // From this point on, AsyncWaitHandle would create a handle that is readily set,
                // so we do not need to check if it is being produced asynchronously.
                if (_waitHandle != null)
                    _waitHandle.Set();
            }

            if (_userCompletionCallback != null)
                _userCompletionCallback(this);
        }
    }  // class StreamOperationAsyncResult

    #endregion class StreamOperationAsyncResult


    #region class StreamReadAsyncResult

    internal class StreamReadAsyncResult : StreamOperationAsyncResult
    {
        private IBuffer _userBuffer = null;

        internal StreamReadAsyncResult(IAsyncOperationWithProgress<IBuffer, UInt32> asyncStreamReadOperation, IBuffer buffer,
                                       AsyncCallback userCompletionCallback, Object userAsyncStateInfo,
                                       bool processCompletedOperationInCallback)

            : base(asyncStreamReadOperation, userCompletionCallback, userAsyncStateInfo, processCompletedOperationInCallback)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            _userBuffer = buffer;
            asyncStreamReadOperation.Completed = this.StreamOperationCompletedCallback;
        }


        internal override void ProcessConcreteCompletedOperation(IAsyncInfo completedOperation, out Int64 bytesCompleted)
        {
            ProcessConcreteCompletedOperation((IAsyncOperationWithProgress<IBuffer, UInt32>)completedOperation, out bytesCompleted);
        }


        private void ProcessConcreteCompletedOperation(IAsyncOperationWithProgress<IBuffer, UInt32> completedOperation, out Int64 bytesCompleted)
        {
            IBuffer resultBuffer = completedOperation.GetResults();
            Debug.Assert(resultBuffer != null);

            WinRtIOHelper.EnsureResultsInUserBuffer(_userBuffer, resultBuffer);
            bytesCompleted = _userBuffer.Length;
        }
    }  // class StreamReadAsyncResult

    #endregion class StreamReadAsyncResult


    #region class StreamWriteAsyncResult

    internal class StreamWriteAsyncResult : StreamOperationAsyncResult
    {
        internal StreamWriteAsyncResult(IAsyncOperationWithProgress<UInt32, UInt32> asyncStreamWriteOperation,
                                        AsyncCallback userCompletionCallback, Object userAsyncStateInfo,
                                        bool processCompletedOperationInCallback)

            : base(asyncStreamWriteOperation, userCompletionCallback, userAsyncStateInfo, processCompletedOperationInCallback)
        {
            asyncStreamWriteOperation.Completed = this.StreamOperationCompletedCallback;
        }


        internal override void ProcessConcreteCompletedOperation(IAsyncInfo completedOperation, out Int64 bytesCompleted)
        {
            ProcessConcreteCompletedOperation((IAsyncOperationWithProgress<UInt32, UInt32>)completedOperation, out bytesCompleted);
        }


        private void ProcessConcreteCompletedOperation(IAsyncOperationWithProgress<UInt32, UInt32> completedOperation, out Int64 bytesCompleted)
        {
            UInt32 bytesWritten = completedOperation.GetResults();
            bytesCompleted = bytesWritten;
        }
    }  // class StreamWriteAsyncResult

    #endregion class StreamWriteAsyncResult


    #region class StreamFlushAsyncResult

    internal class StreamFlushAsyncResult : StreamOperationAsyncResult
    {
        internal StreamFlushAsyncResult(IAsyncOperation<Boolean> asyncStreamFlushOperation, bool processCompletedOperationInCallback)

            : base(asyncStreamFlushOperation, null, null, processCompletedOperationInCallback)
        {
            asyncStreamFlushOperation.Completed = this.StreamOperationCompletedCallback;
        }


        internal override void ProcessConcreteCompletedOperation(IAsyncInfo completedOperation, out Int64 bytesCompleted)
        {
            ProcessConcreteCompletedOperation((IAsyncOperation<Boolean>)completedOperation, out bytesCompleted);
        }


        private void ProcessConcreteCompletedOperation(IAsyncOperation<Boolean> completedOperation, out Int64 bytesCompleted)
        {
            Boolean success = completedOperation.GetResults();
            bytesCompleted = (success ? 0 : -1);
        }
    }  // class StreamFlushAsyncResult
    #endregion class StreamFlushAsyncResult
}  // namespace

// StreamOperationAsyncResult.cs
