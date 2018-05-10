// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Net.Http
{
    internal static class WinHttpTraceHelper
    {
        public static void TraceCallbackStatus(object thisOrContextObject, IntPtr handle, IntPtr context, uint status, [CallerMemberName] string memberName = null)
        {
            Debug.Assert(NetEventSource.IsEnabled);

            NetEventSource.Info(
                thisOrContextObject,
                $"handle=0x{handle.ToString("X")}, context=0x{context.ToString("X")}, {GetStringFromInternetStatus(status)}",
                memberName);
        }

        public static void TraceAsyncError(object thisOrContextObject, Interop.WinHttp.WINHTTP_ASYNC_RESULT asyncResult, [CallerMemberName] string memberName = null)
        {
            Debug.Assert(NetEventSource.IsEnabled);

            uint apiIndex = (uint)asyncResult.dwResult.ToInt32();
            uint error = asyncResult.dwError;
            string apiName = GetNameFromApiIndex(apiIndex);
            NetEventSource.Error(
                thisOrContextObject,
                $"api={apiName}, error={GetNameFromError(error)}({error}) \"{WinHttpException.GetErrorMessage((int)error, apiName)}\"",
                memberName);
        }

        private static string GetNameFromApiIndex(uint index)
        {
            switch (index)
            {
                case Interop.WinHttp.API_RECEIVE_RESPONSE:
                    return "API_RECEIVE_RESPONSE";

                case Interop.WinHttp.API_QUERY_DATA_AVAILABLE:
                    return "API_QUERY_DATA_AVAILABLE";

                case Interop.WinHttp.API_READ_DATA:
                    return "API_READ_DATA";

                case Interop.WinHttp.API_WRITE_DATA:
                    return "API_WRITE_DATA";

                case Interop.WinHttp.API_SEND_REQUEST:
                    return "API_SEND_REQUEST";

                default:
                    return index.ToString();
            }
        }

        private static string GetNameFromError(uint error)
        {
            switch (error)
            {
                case Interop.WinHttp.ERROR_FILE_NOT_FOUND:
                    return "ERROR_FILE_NOT_FOUND";

                case Interop.WinHttp.ERROR_INVALID_HANDLE:
                    return "ERROR_INVALID_HANDLE";

                case Interop.WinHttp.ERROR_INVALID_PARAMETER:
                    return "ERROR_INVALID_PARAMETER";

                case Interop.WinHttp.ERROR_INSUFFICIENT_BUFFER:
                    return "ERROR_INSUFFICIENT_BUFFER";

                case Interop.WinHttp.ERROR_NOT_FOUND:
                    return "ERROR_NOT_FOUND";

                case Interop.WinHttp.ERROR_WINHTTP_INVALID_OPTION:
                    return "WINHTTP_INVALID_OPTION";

                case Interop.WinHttp.ERROR_WINHTTP_LOGIN_FAILURE:
                    return "WINHTTP_LOGIN_FAILURE";

                case Interop.WinHttp.ERROR_WINHTTP_OPERATION_CANCELLED:
                    return "WINHTTP_OPERATION_CANCELLED";

                case Interop.WinHttp.ERROR_WINHTTP_INCORRECT_HANDLE_STATE:
                    return "WINHTTP_INCORRECT_HANDLE_STATE";

                case Interop.WinHttp.ERROR_WINHTTP_CONNECTION_ERROR:
                    return "WINHTTP_CONNECTION_ERROR";

                case Interop.WinHttp.ERROR_WINHTTP_RESEND_REQUEST:
                    return "WINHTTP_RESEND_REQUEST";

                case Interop.WinHttp.ERROR_WINHTTP_CLIENT_AUTH_CERT_NEEDED:
                    return "WINHTTP_CLIENT_AUTH_CERT_NEEDED";

                case Interop.WinHttp.ERROR_WINHTTP_HEADER_NOT_FOUND:
                    return "WINHTTP_HEADER_NOT_FOUND";

                case Interop.WinHttp.ERROR_WINHTTP_SECURE_FAILURE:
                    return "WINHTTP_SECURE_FAILURE";

                case Interop.WinHttp.ERROR_WINHTTP_AUTODETECTION_FAILED:
                    return "WINHTTP_AUTODETECTION_FAILED";

                 default:
                    return error.ToString();
            }
        }

        private static string GetStringFromInternetStatus(uint status)
        {
            switch (status)
            {
                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_RESOLVING_NAME:
                    return "STATUS_RESOLVING_NAME";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_NAME_RESOLVED:
                    return "STATUS_NAME_RESOLVED";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_CONNECTING_TO_SERVER:
                    return "STATUS_CONNECTING_TO_SERVER";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_CONNECTED_TO_SERVER:
                    return "STATUS_CONNECTED_TO_SERVER";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_SENDING_REQUEST:
                    return "STATUS_SENDING_REQUEST";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_REQUEST_SENT:
                    return "STATUS_REQUEST_SENT";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_RECEIVING_RESPONSE:
                    return "STATUS_RECEIVING_RESPONSE"; 

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_RESPONSE_RECEIVED:
                    return "STATUS_RESPONSE_RECEIVED";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_CLOSING_CONNECTION:
                    return "STATUS_CLOSING_CONNECTION";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_CONNECTION_CLOSED:
                    return "STATUS_CONNECTION_CLOSED";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_HANDLE_CREATED:
                    return "STATUS_HANDLE_CREATED";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING:
                    return "STATUS_HANDLE_CLOSING";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_DETECTING_PROXY:
                    return "STATUS_DETECTING_PROXY";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_REDIRECT:
                    return "STATUS_REDIRECT";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_INTERMEDIATE_RESPONSE:
                    return "STATUS_INTERMEDIATE_RESPONSE";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_SECURE_FAILURE:
                    return "STATUS_SECURE_FAILURE";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_HEADERS_AVAILABLE:
                    return "STATUS_HEADERS_AVAILABLE";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_DATA_AVAILABLE:
                    return "STATUS_DATA_AVAILABLE";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_READ_COMPLETE:
                    return "STATUS_READ_COMPLETE";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_WRITE_COMPLETE:
                    return "STATUS_WRITE_COMPLETE";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_REQUEST_ERROR:
                    return "STATUS_REQUEST_ERROR";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_SENDREQUEST_COMPLETE:
                    return "STATUS_SENDREQUEST_COMPLETE";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_GETPROXYFORURL_COMPLETE:
                    return "STATUS_GETPROXYFORURL_COMPLETE";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_CLOSE_COMPLETE:
                    return "STATUS_CLOSE_COMPLETE";

                case Interop.WinHttp.WINHTTP_CALLBACK_STATUS_SHUTDOWN_COMPLETE:
                    return "STATUS_SHUTDOWN_COMPLETE";

                default:
                    return string.Format("0x{0:X}", status);
            }
        }
    }
}
