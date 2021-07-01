// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class WinHttp
    {
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeWinHttpHandle WinHttpOpen(
            IntPtr userAgent,
            uint accessType,
            string? proxyName,
            string? proxyBypass, int flags);

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

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeWinHttpHandle WinHttpOpenRequest(
            SafeWinHttpHandle connectHandle,
            string verb,
            string objectName,
            string? version,
            string referrer,
            string acceptTypes,
            uint flags);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpAddRequestHeaders(
            SafeWinHttpHandle requestHandle,
#pragma warning disable CA1838 // Uses pooled StringBuilder
            [In] StringBuilder headers,
#pragma warning restore CA1838
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
            IntPtr headers,
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
            IntPtr parameterIgnoredAndShouldBeNullForAsync);

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
            IntPtr optionData,
            uint optionLength);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpSetCredentials(
            SafeWinHttpHandle requestHandle,
            uint authTargets,
            uint authScheme,
            string? userName,
            string? password,
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
            SafeWinHttpHandle? sessionHandle, string url,
            ref WINHTTP_AUTOPROXY_OPTIONS autoProxyOptions,
            out WINHTTP_PROXY_INFO proxyInfo);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr WinHttpSetStatusCallback(
            SafeWinHttpHandle handle,
            WINHTTP_STATUS_CALLBACK callback,
            uint notificationFlags,
            IntPtr reserved);
    }
}
