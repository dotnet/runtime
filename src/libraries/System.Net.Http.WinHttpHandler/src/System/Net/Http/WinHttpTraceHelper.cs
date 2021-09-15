// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Net.Http
{
    internal static class WinHttpTraceHelper
    {
        public static void TraceCallbackStatus(object? thisOrContextObject, IntPtr handle, IntPtr context, uint status, [CallerMemberName] string? memberName = null)
        {
            Debug.Assert(NetEventSource.Log.IsEnabled());

            NetEventSource.Info(
                thisOrContextObject,
                $"handle=0x{handle:X}, context=0x{context:X}, {GetStringFromInternetStatus(status)}",
                memberName);
        }

        public static void TraceAsyncError(object thisOrContextObject, Interop.WinHttp.WINHTTP_ASYNC_RESULT asyncResult, [CallerMemberName] string? memberName = null)
        {
            Debug.Assert(NetEventSource.Log.IsEnabled());

            uint apiIndex = (uint)asyncResult.dwResult.ToInt32();
            uint error = asyncResult.dwError;
            string apiName = GetNameFromApiIndex(apiIndex);
            NetEventSource.Error(
                thisOrContextObject,
                $"api={apiName}, error={GetNameFromError(error)}({error}) \"{WinHttpException.GetErrorMessage((int)error, apiName)}\"",
                memberName);
        }

        private static string GetNameFromApiIndex(uint index) =>
            index switch
            {
                Interop.WinHttp.API_RECEIVE_RESPONSE => "API_RECEIVE_RESPONSE",
                Interop.WinHttp.API_QUERY_DATA_AVAILABLE => "API_QUERY_DATA_AVAILABLE",
                Interop.WinHttp.API_READ_DATA => "API_READ_DATA",
                Interop.WinHttp.API_WRITE_DATA => "API_WRITE_DATA",
                Interop.WinHttp.API_SEND_REQUEST => "API_SEND_REQUEST",
                _ => index.ToString(),
            };

        private static string GetNameFromError(uint error) =>
            error switch
            {
                Interop.WinHttp.ERROR_FILE_NOT_FOUND => "ERROR_FILE_NOT_FOUND",
                Interop.WinHttp.ERROR_INVALID_HANDLE => "ERROR_INVALID_HANDLE",
                Interop.WinHttp.ERROR_INVALID_PARAMETER => "ERROR_INVALID_PARAMETER",
                Interop.WinHttp.ERROR_INSUFFICIENT_BUFFER => "ERROR_INSUFFICIENT_BUFFER",
                Interop.WinHttp.ERROR_NOT_FOUND => "ERROR_NOT_FOUND",
                Interop.WinHttp.ERROR_WINHTTP_INVALID_OPTION => "WINHTTP_INVALID_OPTION",
                Interop.WinHttp.ERROR_WINHTTP_LOGIN_FAILURE => "WINHTTP_LOGIN_FAILURE",
                Interop.WinHttp.ERROR_WINHTTP_OPERATION_CANCELLED => "WINHTTP_OPERATION_CANCELLED",
                Interop.WinHttp.ERROR_WINHTTP_INCORRECT_HANDLE_STATE => "WINHTTP_INCORRECT_HANDLE_STATE",
                Interop.WinHttp.ERROR_WINHTTP_CONNECTION_ERROR => "WINHTTP_CONNECTION_ERROR",
                Interop.WinHttp.ERROR_WINHTTP_RESEND_REQUEST => "WINHTTP_RESEND_REQUEST",
                Interop.WinHttp.ERROR_WINHTTP_CLIENT_AUTH_CERT_NEEDED => "WINHTTP_CLIENT_AUTH_CERT_NEEDED",
                Interop.WinHttp.ERROR_WINHTTP_HEADER_NOT_FOUND => "WINHTTP_HEADER_NOT_FOUND",
                Interop.WinHttp.ERROR_WINHTTP_SECURE_FAILURE => "WINHTTP_SECURE_FAILURE",
                Interop.WinHttp.ERROR_WINHTTP_AUTODETECTION_FAILED => "WINHTTP_AUTODETECTION_FAILED",
                _ => error.ToString(),
            };

        private static string GetStringFromInternetStatus(uint status) =>
            status switch
            {
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_RESOLVING_NAME => "STATUS_RESOLVING_NAME",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_NAME_RESOLVED => "STATUS_NAME_RESOLVED",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_CONNECTING_TO_SERVER => "STATUS_CONNECTING_TO_SERVER",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_CONNECTED_TO_SERVER => "STATUS_CONNECTED_TO_SERVER",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_SENDING_REQUEST => "STATUS_SENDING_REQUEST",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_REQUEST_SENT => "STATUS_REQUEST_SENT",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_RECEIVING_RESPONSE => "STATUS_RECEIVING_RESPONSE",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_RESPONSE_RECEIVED => "STATUS_RESPONSE_RECEIVED",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_CLOSING_CONNECTION => "STATUS_CLOSING_CONNECTION",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_CONNECTION_CLOSED => "STATUS_CONNECTION_CLOSED",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_HANDLE_CREATED => "STATUS_HANDLE_CREATED",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING => "STATUS_HANDLE_CLOSING",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_DETECTING_PROXY => "STATUS_DETECTING_PROXY",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_REDIRECT => "STATUS_REDIRECT",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_INTERMEDIATE_RESPONSE => "STATUS_INTERMEDIATE_RESPONSE",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_SECURE_FAILURE => "STATUS_SECURE_FAILURE",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_HEADERS_AVAILABLE => "STATUS_HEADERS_AVAILABLE",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_DATA_AVAILABLE => "STATUS_DATA_AVAILABLE",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_READ_COMPLETE => "STATUS_READ_COMPLETE",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_WRITE_COMPLETE => "STATUS_WRITE_COMPLETE",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_REQUEST_ERROR => "STATUS_REQUEST_ERROR",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_SENDREQUEST_COMPLETE => "STATUS_SENDREQUEST_COMPLETE",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_GETPROXYFORURL_COMPLETE => "STATUS_GETPROXYFORURL_COMPLETE",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_CLOSE_COMPLETE => "STATUS_CLOSE_COMPLETE",
                Interop.WinHttp.WINHTTP_CALLBACK_STATUS_SHUTDOWN_COMPLETE => "STATUS_SHUTDOWN_COMPLETE",
                _ => $"0x{status:X}",
            };
    }
}
