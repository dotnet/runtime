// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class WinHttp
    {
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeWinHttpHandle WinHttpOpen(
            IntPtr userAgent,
            uint accessType,
            string proxyName,
            string proxyBypass, int flags);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpCloseHandle(
            IntPtr handle);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeWinHttpHandle WinHttpConnect(
            SafeWinHttpHandle sessionHandle,
            string serverName,
            ushort serverPort,
            uint reserved);

        // NOTE: except for the return type, this refers to the same function as WinHttpConnect.
        [DllImport(Interop.Libraries.WinHttp, EntryPoint = "WinHttpConnect", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeWinHttpHandleWithCallback WinHttpConnectWithCallback(
            SafeWinHttpHandle sessionHandle,
            string serverName,
            ushort serverPort,
            uint reserved);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeWinHttpHandle WinHttpOpenRequest(
            SafeWinHttpHandle connectHandle,
            string verb,
            string objectName,
            string version,
            string referrer,
            string acceptTypes,
            uint flags);

        // NOTE: except for the return type, this refers to the same function as WinHttpOpenRequest.
        [DllImport(Interop.Libraries.WinHttp, EntryPoint = "WinHttpOpenRequest", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeWinHttpHandleWithCallback WinHttpOpenRequestWithCallback(
            SafeWinHttpHandle connectHandle,
            string verb,
            string objectName,
            string version,
            string referrer,
            string acceptTypes,
            uint flags);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpAddRequestHeaders(
            SafeWinHttpHandle requestHandle,
            [In] StringBuilder headers,
            uint headersLength,
            uint modifiers);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpAddRequestHeaders(
            SafeWinHttpHandle requestHandle,
            string headers,
            uint headersLength,
            uint modifiers);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpSendRequest(
            SafeWinHttpHandle requestHandle,
            [In] StringBuilder headers,
            uint headersLength,
            IntPtr optional,
            uint optionalLength,
            uint totalLength,
            IntPtr context);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpReceiveResponse(
            SafeWinHttpHandle requestHandle,
            IntPtr reserved);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpQueryDataAvailable(
            SafeWinHttpHandle requestHandle,
            out uint bytesAvailable);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpQueryDataAvailable(
            SafeWinHttpHandle requestHandle,
            IntPtr parameterIgnoredAndShouldBeNullForAsync);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpReadData(
            SafeWinHttpHandle requestHandle,
            IntPtr buffer,
            uint bufferSize,
            out uint bytesRead);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpReadData(
            SafeWinHttpHandle requestHandle,
            IntPtr buffer,
            uint bufferSize,
            IntPtr parameterIgnoredAndShouldBeNullForAsync);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpQueryHeaders(
            SafeWinHttpHandle requestHandle,
            uint infoLevel,
            string name,
            IntPtr buffer,
            ref uint bufferLength,
            ref uint index);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpQueryHeaders(
            SafeWinHttpHandle requestHandle,
            uint infoLevel,
            string name,
            ref uint number,
            ref uint bufferLength,
            IntPtr index);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpQueryOption(
            SafeWinHttpHandle handle,
            uint option,
            [Out] StringBuilder buffer,
            ref uint bufferSize);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpQueryOption(
            SafeWinHttpHandle handle,
            uint option,
            ref IntPtr buffer,
            ref uint bufferSize);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpQueryOption(
            SafeWinHttpHandle handle,
            uint option,
            IntPtr buffer,
            ref uint bufferSize);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpQueryOption(
            SafeWinHttpHandle handle,
            uint option,
            ref uint buffer,
            ref uint bufferSize);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpWriteData(
            SafeWinHttpHandle requestHandle,
            IntPtr buffer,
            uint bufferSize,
            out uint bytesWritten);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpWriteData(
            SafeWinHttpHandle requestHandle,
            IntPtr buffer,
            uint bufferSize,
            IntPtr parameterIgnoredAndShouldBeNullForAsync);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpSetOption(
            SafeWinHttpHandle handle,
            uint option,
            ref uint optionData,
            uint optionLength = sizeof(uint));

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpSetOption(
            SafeWinHttpHandle handle,
            uint option,
            string optionData,
            uint optionLength);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpSetOption(
            SafeWinHttpHandle handle,
            uint option,
            IntPtr optionData,
            uint optionLength);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpSetCredentials(
            SafeWinHttpHandle requestHandle,
            uint authTargets,
            uint authScheme,
            string userName,
            string password,
            IntPtr reserved);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpQueryAuthSchemes(
            SafeWinHttpHandle requestHandle,
            out uint supportedSchemes,
            out uint firstScheme,
            out uint authTarget);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpSetTimeouts(
            SafeWinHttpHandle handle,
            int resolveTimeout,
            int connectTimeout,
            int sendTimeout,
            int receiveTimeout);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpGetIEProxyConfigForCurrentUser(
            out WINHTTP_CURRENT_USER_IE_PROXY_CONFIG proxyConfig);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpGetProxyForUrl(
            SafeWinHttpHandle sessionHandle, string url,
            ref WINHTTP_AUTOPROXY_OPTIONS autoProxyOptions,
            out WINHTTP_PROXY_INFO proxyInfo);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr WinHttpSetStatusCallback(
            SafeWinHttpHandle handle,
            WINHTTP_STATUS_CALLBACK callback,
            uint notificationFlags,
            IntPtr reserved);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeWinHttpHandleWithCallback WinHttpWebSocketCompleteUpgrade(
            SafeWinHttpHandle requestHandle,
            IntPtr context);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern uint WinHttpWebSocketSend(
            SafeWinHttpHandle webSocketHandle,
            WINHTTP_WEB_SOCKET_BUFFER_TYPE bufferType,
            IntPtr buffer,
            uint bufferLength);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern uint WinHttpWebSocketReceive(
            SafeWinHttpHandle webSocketHandle,
            IntPtr buffer,
            uint bufferLength,
            out uint bytesRead,
            out WINHTTP_WEB_SOCKET_BUFFER_TYPE bufferType);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern uint WinHttpWebSocketShutdown(
            SafeWinHttpHandle webSocketHandle,
            ushort status,
            byte[] reason,
            uint reasonLength);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern uint WinHttpWebSocketShutdown(
            SafeWinHttpHandle webSocketHandle,
            ushort status,
            IntPtr reason,
            uint reasonLength);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern uint WinHttpWebSocketClose(
            SafeWinHttpHandle webSocketHandle,
            ushort status,
            byte[] reason,
            uint reasonLength);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern uint WinHttpWebSocketClose(
            SafeWinHttpHandle webSocketHandle,
            ushort status,
            IntPtr reason,
            uint reasonLength);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern uint WinHttpWebSocketQueryCloseStatus(
            SafeWinHttpHandle webSocketHandle,
            out ushort status,
            byte[] reason,
            uint reasonLength,
            out uint reasonLengthConsumed);
    }
}
