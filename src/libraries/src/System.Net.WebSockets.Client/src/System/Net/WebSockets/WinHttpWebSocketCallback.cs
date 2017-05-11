// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace System.Net.WebSockets
{
    /// <summary>
    /// Static class containing the WinHttp global callback and associated routines.
    /// </summary>
    internal static class WinHttpWebSocketCallback
    {
        public static Interop.WinHttp.WINHTTP_STATUS_CALLBACK s_StaticCallbackDelegate =
            new Interop.WinHttp.WINHTTP_STATUS_CALLBACK(WinHttpCallback);

        public static void WinHttpCallback(
            IntPtr handle,
            IntPtr context,
            uint internetStatus,
            IntPtr statusInformation,
            uint statusInformationLength)
        {
            if (Environment.HasShutdownStarted)
            {
                return;
            }

            if (context == IntPtr.Zero)
            {
                Debug.Assert(internetStatus != Interop.WinHttp.WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING);
                return;
            }

            try
            {
                WinHttpWebSocketState state = WinHttpWebSocketState.FromIntPtr(context);
                Debug.Assert(state != null, "WinHttpWebSocketCallback: state should not be null");

                if ((state.RequestHandle != null) &&
                    (state.RequestHandle.DangerousGetHandle() == handle))
                {
                    RequestCallback(handle, state, internetStatus, statusInformation, statusInformationLength);
                    return;
                }

                if ((state.WebSocketHandle != null) &&
                    (state.WebSocketHandle.DangerousGetHandle() == handle))
                {
                    WebSocketCallback(handle, state, internetStatus, statusInformation, statusInformationLength);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.Fail("Unhandled exception in WinHTTP callback: " + ex);
            }
        }

        #region RequestCallback

        private static void RequestCallback(
            IntPtr handle,
            WinHttpWebSocketState state,
            uint internetStatus,
            IntPtr statusInformation,
            uint statusInformationLength)
        {
            switch (internetStatus)
            {
                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_SENDREQUEST_COMPLETE:
                    OnRequestSendRequestComplete(state);
                    return;

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_HEADERS_AVAILABLE:
                    OnRequestHeadersAvailable(state);
                    return;

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING:
                    OnRequestHandleClosing(state);
                    return;

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_REQUEST_ERROR:
                    Debug.Assert(
                        statusInformationLength == Marshal.SizeOf<Interop.WinHttp.WINHTTP_ASYNC_RESULT>(),
                        "RequestCallback: statusInformationLength=" + statusInformationLength +
                        " must be sizeof(WINHTTP_ASYNC_RESULT)=" + Marshal.SizeOf<Interop.WinHttp.WINHTTP_ASYNC_RESULT>());

                    var asyncResult = Marshal.PtrToStructure<Interop.WinHttp.WINHTTP_ASYNC_RESULT>(statusInformation);
                    OnRequestError(state, asyncResult);
                    return;

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_SECURE_FAILURE:
                    Debug.Assert(
                        statusInformationLength == sizeof(uint),
                        "RequestCallback: statusInformationLength must be sizeof(uint).");

                    // statusInformation contains a flag: WINHTTP_CALLBACK_STATUS_FLAG_*
                    uint flags = 0;
                    unchecked
                    {
                        flags = (uint)Marshal.ReadInt32(statusInformation);
                    }
                    OnRequestSecureFailure(state, flags);

                    return;
            }
        }

        private static void OnRequestSendRequestComplete(WinHttpWebSocketState state)
        {
            Debug.Assert(state != null, "OnRequestSendRequestComplete: state is null");
            Debug.Assert(state.TcsUpgrade != null, "OnRequestSendRequestComplete: task completion source is null");
            state.TcsUpgrade.TrySetResult(true);
        }

        private static void OnRequestHeadersAvailable(WinHttpWebSocketState state)
        {
            Debug.Assert(state != null, "OnRequestHeadersAvailable: state is null");
            Debug.Assert(state.TcsUpgrade != null, "OnRequestHeadersAvailable: task completion source is null");
            state.TcsUpgrade.TrySetResult(true);
        }

        private static void OnRequestHandleClosing(WinHttpWebSocketState state)
        {
            Debug.Assert(state != null, "OnRequestError: state is null");
            Debug.Assert(state.RequestHandle != null, "OnRequestError: RequestHandle is null");
            Debug.Assert(!state.RequestHandle.IsInvalid, "OnRequestError: RequestHandle is invalid");

            state.RequestHandle.DetachCallback();
            state.RequestHandle = null;
            
            // Unpin the state object if there are no more open handles that are wired to the callback.
            if (state.DecrementHandlesOpenWithCallback() == 0)
            {
                state.Unpin();
            }
        }

        private static void OnRequestError(
            WinHttpWebSocketState state,
            Interop.WinHttp.WINHTTP_ASYNC_RESULT asyncResult)
        {
            Debug.Assert(state != null, "OnRequestError: state is null");

            var innerException = WinHttpException.CreateExceptionUsingError(unchecked((int)asyncResult.dwError));

            switch (unchecked((uint)asyncResult.dwResult.ToInt32()))
            {
                case Interop.WinHttp.API_SEND_REQUEST:
                case Interop.WinHttp.API_RECEIVE_RESPONSE:
                    {
                        var exception = new WebSocketException(SR.net_webstatus_ConnectFailure, innerException);
                        state.UpdateState(WebSocketState.Closed);
                        state.TcsUpgrade.TrySetException(exception);
                    }
                    break;

                default:
                    {
                        Debug.Fail(
                            "OnRequestError: Result (" + asyncResult.dwResult + ") is not expected.",
                            "Error code: " + asyncResult.dwError + " (" + innerException.Message + ")");
                    }
                    break;
            }
        }

        private static void OnRequestSecureFailure(WinHttpWebSocketState state, uint flags)
        {
            Debug.Assert(state != null, "OnRequestSecureFailure: state is null");

            var innerException = WinHttpException.CreateExceptionUsingError(unchecked((int)Interop.WinHttp.ERROR_WINHTTP_SECURE_FAILURE));

            var exception = new WebSocketException(
                WebSocketError.Success,
                SR.net_webstatus_ConnectFailure,
                innerException);

            // TODO (#2509): handle SSL related exceptions.
            state.UpdateState(WebSocketState.Closed);

            // TODO (#2509): Create exception from WINHTTP_CALLBACK_STATUS_SECURE_FAILURE flags.
            state.TcsUpgrade.TrySetException(exception);
        }
        #endregion

        #region WebSocketCallback
        private static void WebSocketCallback(
            IntPtr handle,
            WinHttpWebSocketState state,
            uint internetStatus,
            IntPtr statusInformation,
            uint statusInformationLength)
        {
            switch (internetStatus)
            {
                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_WRITE_COMPLETE:
                    OnWebSocketWriteComplete(state);
                    return;

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_READ_COMPLETE:
                    Debug.Assert(
                        statusInformationLength == Marshal.SizeOf<Interop.WinHttp.WINHTTP_WEB_SOCKET_STATUS>(),
                        "WebSocketCallback: statusInformationLength must be sizeof(WINHTTP_WEB_SOCKET_STATUS).");

                    var info = Marshal.PtrToStructure<Interop.WinHttp.WINHTTP_WEB_SOCKET_STATUS>(statusInformation);
                    OnWebSocketReadComplete(state, info);
                    return;

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_CLOSE_COMPLETE:
                    OnWebSocketCloseComplete(state);
                    return;

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_SHUTDOWN_COMPLETE:
                    OnWebSocketShutdownComplete(state);
                    return;

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING:
                    OnWebSocketHandleClosing(state);
                    return;

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_REQUEST_ERROR:
                    Debug.Assert(
                        statusInformationLength == Marshal.SizeOf<Interop.WinHttp.WINHTTP_WEB_SOCKET_ASYNC_RESULT>(),
                        "WebSocketCallback: statusInformationLength must be sizeof(WINHTTP_WEB_SOCKET_ASYNC_RESULT).");

                    var asyncResult = Marshal.PtrToStructure<Interop.WinHttp.WINHTTP_WEB_SOCKET_ASYNC_RESULT>(statusInformation);
                    OnWebSocketError(state, asyncResult);
                    return;

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_SECURE_FAILURE:
                    Debug.Assert(
                        statusInformationLength == sizeof(uint),
                        "WebSocketCallback: statusInformationLength must be sizeof(uint).");

                    // statusInformation contains a flag: WINHTTP_CALLBACK_STATUS_FLAG_*
                    uint flags = unchecked((uint)statusInformation);
                    OnRequestSecureFailure(state, flags);
                    return;
            }
        }

        private static void OnWebSocketWriteComplete(WinHttpWebSocketState state)
        {
            Debug.Assert(state != null, "OnWebSocketWriteComplete: state is null");

            state.PendingWriteOperation = false;
            state.TcsSend.TrySetResult(true);
        }

        private static void OnWebSocketReadComplete(
            WinHttpWebSocketState state,
            Interop.WinHttp.WINHTTP_WEB_SOCKET_STATUS info)
        {
            Debug.Assert(state != null, "OnWebSocketReadComplete: state is null");

            if (info.eBufferType == Interop.WinHttp.WINHTTP_WEB_SOCKET_BUFFER_TYPE.WINHTTP_WEB_SOCKET_CLOSE_BUFFER_TYPE)
            {
                state.UpdateState(WebSocketState.CloseReceived);
            }

            state.BufferType = info.eBufferType;
            state.BytesTransferred = info.dwBytesTransferred;
            state.PendingReadOperation = false;

            state.TcsReceive.TrySetResult(true);
        }

        private static void OnWebSocketCloseComplete(WinHttpWebSocketState state)
        {
            Debug.Assert(state != null, "OnWebSocketCloseComplete: state is null");

            state.UpdateState(WebSocketState.Closed);
            state.TcsClose.TrySetResult(true);
        }

        private static void OnWebSocketShutdownComplete(WinHttpWebSocketState state)
        {
            Debug.Assert(state != null, "OnWebSocketShutdownComplete: state is null");

            state.UpdateState(WebSocketState.CloseSent);
            state.TcsCloseOutput.TrySetResult(true);
        }

        private static void OnWebSocketHandleClosing(WinHttpWebSocketState state)
        {
            Debug.Assert(state != null, "OnWebSocketHandleClosing: state is null");
            Debug.Assert(state.WebSocketHandle != null, "OnWebSocketHandleClosing: WebSocketHandle is null");
            Debug.Assert(!state.WebSocketHandle.IsInvalid, "OnWebSocketHandleClosing: WebSocketHandle is invalid");

            state.WebSocketHandle.DetachCallback();
            state.WebSocketHandle = null;

            // Unpin the state object if there are no more open handles that are wired to the callback.
            if (state.DecrementHandlesOpenWithCallback() == 0)
            {
                state.Unpin();
            }
        }

        private static void OnWebSocketError(
            WinHttpWebSocketState state,
            Interop.WinHttp.WINHTTP_WEB_SOCKET_ASYNC_RESULT asyncResult)
        {
            Debug.Assert(state != null, "OnWebSocketError: state is null");

            var innerException = WinHttpException.CreateExceptionUsingError(unchecked((int)(asyncResult.AsyncResult.dwError)));

            if (asyncResult.AsyncResult.dwError == Interop.WinHttp.ERROR_WINHTTP_OPERATION_CANCELLED)
            {
                state.UpdateState(WebSocketState.Aborted);

                if (state.TcsReceive != null)
                {
                    state.TcsReceive.TrySetCanceled();
                }

                if (state.TcsSend != null)
                {
                    state.TcsSend.TrySetCanceled();
                }

                return;
            }

            switch (asyncResult.Operation)
            {
                case Interop.WinHttp.WINHTTP_WEB_SOCKET_OPERATION.WINHTTP_WEB_SOCKET_SEND_OPERATION:
                    state.PendingWriteOperation = false;
                    state.TcsSend.TrySetException(innerException);
                    break;

                case Interop.WinHttp.WINHTTP_WEB_SOCKET_OPERATION.WINHTTP_WEB_SOCKET_RECEIVE_OPERATION:
                    state.PendingReadOperation = false;
                    state.TcsReceive.TrySetException(innerException);
                    break;

                case Interop.WinHttp.WINHTTP_WEB_SOCKET_OPERATION.WINHTTP_WEB_SOCKET_CLOSE_OPERATION:
                    state.TcsClose.TrySetException(innerException);
                    break;

                case Interop.WinHttp.WINHTTP_WEB_SOCKET_OPERATION.WINHTTP_WEB_SOCKET_SHUTDOWN_OPERATION:
                    state.TcsCloseOutput.TrySetException(innerException);
                    break;

                default:
                    Debug.Fail(
                        "OnWebSocketError: Operation (" + asyncResult.Operation + ") is not expected.",
                        "Error code: " + asyncResult.AsyncResult.dwError + " (" + innerException.Message + ")");
                    break;
            }
        }

        private static void OnWebSocketSecureFailure(WinHttpWebSocketState state, uint flags)
        {
            Debug.Assert(state != null, "OnWebSocketSecureFailure: state is null");

            var innerException = WinHttpException.CreateExceptionUsingError(unchecked((int)Interop.WinHttp.ERROR_WINHTTP_SECURE_FAILURE));
            var exception = new WebSocketException(WebSocketError.ConnectionClosedPrematurely, innerException);

            // TODO (Issue 2509): handle SSL related exceptions.
            state.UpdateState(WebSocketState.Aborted);

            // TODO (Issue 2509): Create exception from WINHTTP_CALLBACK_STATUS_SECURE_FAILURE flags.
            state.TcsUpgrade.TrySetException(exception);
        }
        #endregion
    }
}
