// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Threading;
using System.Runtime.InteropServices;

namespace System.Net
{
    internal unsafe class ListenerAsyncResult : LazyAsyncResult
    {
        private static readonly IOCompletionCallback s_IOCallback = new IOCompletionCallback(WaitCallback);

        internal static IOCompletionCallback IOCallback
        {
            get
            {
                return s_IOCallback;
            }
        }

        private AsyncRequestContext _requestContext;

        internal ListenerAsyncResult(HttpListener listener, object userState, AsyncCallback callback) :
            base(listener, userState, callback)
        {
            _requestContext = new AsyncRequestContext(listener.RequestQueueBoundHandle, this);
        }

        private static void IOCompleted(ListenerAsyncResult asyncResult, uint errorCode, uint numBytes)
        {
            object result = null;
            try
            {
                //GlobalLog.Print("ListenerAsyncResult#" + LoggingHash.HashString(asyncResult) + "::WaitCallback() errorCode:[" + errorCode.ToString() + "] numBytes:[" + numBytes.ToString() + "]");

                if (errorCode != Interop.HttpApi.ERROR_SUCCESS &&
                    errorCode != Interop.HttpApi.ERROR_MORE_DATA)
                {
                    asyncResult.ErrorCode = (int)errorCode;
                    result = new HttpListenerException((int)errorCode);
                }
                else
                {
                    HttpListener httpWebListener = asyncResult.AsyncObject as HttpListener;
                    if (errorCode == Interop.HttpApi.ERROR_SUCCESS)
                    {
                        // at this point we have received an unmanaged HTTP_REQUEST and memoryBlob
                        // points to it we need to hook up our authentication handling code here.
                        bool stoleBlob = false;
                        try
                        {
                            if (httpWebListener.ValidateRequest(asyncResult._requestContext))
                            {
                                result = httpWebListener.HandleAuthentication(asyncResult._requestContext, out stoleBlob);
                            }
                        }
                        finally
                        {
                            if (stoleBlob)
                            {
                                // The request has been handed to the user, which means this code can't reuse the blob.  Reset it here.
                                asyncResult._requestContext = result == null ? new AsyncRequestContext(httpWebListener.RequestQueueBoundHandle, asyncResult) : null;
                            }
                            else
                            {
                                asyncResult._requestContext.Reset(httpWebListener.RequestQueueBoundHandle, 0, 0);
                            }
                        }
                    }
                    else
                    {
                        asyncResult._requestContext.Reset(httpWebListener.RequestQueueBoundHandle, asyncResult._requestContext.RequestBlob->RequestId, numBytes);
                    }

                    // We need to issue a new request, either because auth failed, or because our buffer was too small the first time.
                    if (result == null)
                    {
                        uint statusCode = asyncResult.QueueBeginGetContext();
                        if (statusCode != Interop.HttpApi.ERROR_SUCCESS &&
                            statusCode != Interop.HttpApi.ERROR_IO_PENDING)
                        {
                            // someother bad error, possible(?) return values are:
                            // ERROR_INVALID_HANDLE, ERROR_INSUFFICIENT_BUFFER, ERROR_OPERATION_ABORTED
                            result = new HttpListenerException((int)statusCode);
                        }
                    }
                    if (result == null)
                    {
                        return;
                    }
                }

                // complete the async IO and invoke the callback
                //GlobalLog.Print("ListenerAsyncResult#" + LoggingHash.HashString(asyncResult) + "::WaitCallback() calling Complete()");
            }
            catch (Exception exception) when (!ExceptionCheck.IsFatal(exception))
            {
                //GlobalLog.Print("ListenerAsyncResult#" + LoggingHash.HashString(asyncResult) + "::WaitCallback() Caught exception:" + exception.ToString());
                result = exception;
            }
            asyncResult.InvokeCallback(result);
        }

        private static unsafe void WaitCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            ListenerAsyncResult asyncResult = (ListenerAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
            IOCompleted(asyncResult, errorCode, numBytes);
        }

        internal uint QueueBeginGetContext()
        {
            uint statusCode = Interop.HttpApi.ERROR_SUCCESS;
            while (true)
            {
                //GlobalLog.Print("ListenerAsyncResult#" + LoggingHash.HashString(this) + "::QueueBeginGetContext() calling Interop.HttpApi.HttpReceiveHttpRequest RequestId:" + m_RequestContext.RequestBlob->RequestId + " Buffer:0x" + ((IntPtr)m_RequestContext.RequestBlob).ToString("x") + " Size:" + m_RequestContext.Size.ToString());
                uint bytesTransferred = 0;
                HttpListener listener = (HttpListener)AsyncObject;
                statusCode = Interop.HttpApi.HttpReceiveHttpRequest(
                    listener.RequestQueueHandle,
                    _requestContext.RequestBlob->RequestId,
                    (uint)Interop.HttpApi.HTTP_FLAGS.HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY,
                    _requestContext.RequestBlob,
                    _requestContext.Size,
                    &bytesTransferred,
                    _requestContext.NativeOverlapped);

                //GlobalLog.Print("ListenerAsyncResult#" + LoggingHash.HashString(this) + "::QueueBeginGetContext() call to Interop.HttpApi.HttpReceiveHttpRequest returned:" + statusCode);
                if (statusCode == Interop.HttpApi.ERROR_INVALID_PARAMETER && _requestContext.RequestBlob->RequestId != 0)
                {
                    // we might get this if somebody stole our RequestId,
                    // set RequestId to 0 and start all over again with the buffer we just allocated
                    // BUGBUG: how can someone steal our request ID?  seems really bad and in need of fix.
                    _requestContext.RequestBlob->RequestId = 0;
                    continue;
                }
                else if (statusCode == Interop.HttpApi.ERROR_MORE_DATA)
                {
                    // the buffer was not big enough to fit the headers, we need
                    // to read the RequestId returned, allocate a new buffer of the required size
                    _requestContext.Reset(listener.RequestQueueBoundHandle, _requestContext.RequestBlob->RequestId, bytesTransferred);
                    continue;
                }
                else if (statusCode == Interop.HttpApi.ERROR_SUCCESS &&
                    HttpListener.SkipIOCPCallbackOnSuccess)
                {
                    // IO operation completed synchronously - callback won't be called to signal completion.
                    IOCompleted(this, statusCode, bytesTransferred);
                }
                break;
            }
            return statusCode;
        }

        // Will be called from the base class upon InvokeCallback()
        protected override void Cleanup()
        {
            if (_requestContext != null)
            {
                _requestContext.ReleasePins();
                _requestContext.Close();
            }
            base.Cleanup();
        }
    }
}
