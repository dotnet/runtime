// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class WinHttp
    {
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial SafeWinHttpHandle WinHttpOpen(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeWinHttpHandle WinHttpOpen(
#endif
            IntPtr userAgent,
            uint accessType,
            string? proxyName,
            string? proxyBypass, int flags);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpCloseHandle(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpCloseHandle(
#endif
            IntPtr handle);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial SafeWinHttpHandle WinHttpConnect(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeWinHttpHandle WinHttpConnect(
#endif
            SafeWinHttpHandle sessionHandle,
            string serverName,
            ushort serverPort,
            uint reserved);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial SafeWinHttpHandle WinHttpOpenRequest(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeWinHttpHandle WinHttpOpenRequest(
#endif
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

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpAddRequestHeaders(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpAddRequestHeaders(
#endif
            SafeWinHttpHandle requestHandle,
            string headers,
            uint headersLength,
            uint modifiers);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpSendRequest(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpSendRequest(
#endif
            SafeWinHttpHandle requestHandle,
            IntPtr headers,
            uint headersLength,
            IntPtr optional,
            uint optionalLength,
            uint totalLength,
            IntPtr context);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpReceiveResponse(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpReceiveResponse(
#endif
            SafeWinHttpHandle requestHandle,
            IntPtr reserved);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryDataAvailable(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpQueryDataAvailable(
#endif
            SafeWinHttpHandle requestHandle,
            IntPtr parameterIgnoredAndShouldBeNullForAsync);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpReadData(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpReadData(
#endif
            SafeWinHttpHandle requestHandle,
            IntPtr buffer,
            uint bufferSize,
            IntPtr parameterIgnoredAndShouldBeNullForAsync);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryHeaders(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpQueryHeaders(
#endif
            SafeWinHttpHandle requestHandle,
            uint infoLevel,
            string name,
            IntPtr buffer,
            ref uint bufferLength,
            ref uint index);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryHeaders(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpQueryHeaders(
#endif
            SafeWinHttpHandle requestHandle,
            uint infoLevel,
            string name,
            ref uint number,
            ref uint bufferLength,
            IntPtr index);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryOption(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpQueryOption(
#endif
            SafeWinHttpHandle handle,
            uint option,
            ref IntPtr buffer,
            ref uint bufferSize);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryOption(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpQueryOption(
#endif
            SafeWinHttpHandle handle,
            uint option,
            IntPtr buffer,
            ref uint bufferSize);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryOption(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpQueryOption(
#endif
            SafeWinHttpHandle handle,
            uint option,
            ref uint buffer,
            ref uint bufferSize);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpWriteData(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpWriteData(
#endif
            SafeWinHttpHandle requestHandle,
            IntPtr buffer,
            uint bufferSize,
            IntPtr parameterIgnoredAndShouldBeNullForAsync);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpSetOption(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpSetOption(
#endif
            SafeWinHttpHandle handle,
            uint option,
            ref uint optionData,
            uint optionLength = sizeof(uint));

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpSetOption(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpSetOption(
#endif
            SafeWinHttpHandle handle,
            uint option,
            IntPtr optionData,
            uint optionLength);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpSetCredentials(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpSetCredentials(
#endif
            SafeWinHttpHandle requestHandle,
            uint authTargets,
            uint authScheme,
            string? userName,
            string? password,
            IntPtr reserved);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryAuthSchemes(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpQueryAuthSchemes(
#endif
            SafeWinHttpHandle requestHandle,
            out uint supportedSchemes,
            out uint firstScheme,
            out uint authTarget);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpSetTimeouts(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpSetTimeouts(
#endif
            SafeWinHttpHandle handle,
            int resolveTimeout,
            int connectTimeout,
            int sendTimeout,
            int receiveTimeout);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpGetIEProxyConfigForCurrentUser(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpGetIEProxyConfigForCurrentUser(
#endif
            out WINHTTP_CURRENT_USER_IE_PROXY_CONFIG proxyConfig);

        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WinHttpGetProxyForUrl(
            SafeWinHttpHandle? sessionHandle, string url,
            ref WINHTTP_AUTOPROXY_OPTIONS autoProxyOptions,
            out WINHTTP_PROXY_INFO proxyInfo);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial IntPtr WinHttpSetStatusCallback(
#else
        [DllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr WinHttpSetStatusCallback(
#endif
            SafeWinHttpHandle handle,
            WINHTTP_STATUS_CALLBACK callback,
            uint notificationFlags,
            IntPtr reserved);
    }
}
