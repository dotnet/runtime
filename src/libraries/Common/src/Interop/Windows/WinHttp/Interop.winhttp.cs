// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class WinHttp
    {
        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial SafeWinHttpHandle WinHttpOpen(
            IntPtr userAgent,
            uint accessType,
            string? proxyName,
            string? proxyBypass, int flags);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpCloseHandle(
            IntPtr handle);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial SafeWinHttpHandle WinHttpConnect(
            SafeWinHttpHandle sessionHandle,
            string serverName,
            ushort serverPort,
            uint reserved);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial SafeWinHttpHandle WinHttpOpenRequest(
            SafeWinHttpHandle connectHandle,
            string verb,
            string objectName,
            string? version,
            string referrer,
            string acceptTypes,
            uint flags);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpAddRequestHeaders(
            SafeWinHttpHandle requestHandle,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(SimpleStringBufferMarshaller))] StringBuilder headers,
#else
#pragma warning disable CA1838 // Uses pooled StringBuilder
            [In] StringBuilder headers,
#pragma warning restore CA1838 // Uses pooled StringBuilder
#endif
            uint headersLength,
            uint modifiers);

#if NET7_0_OR_GREATER
        private unsafe struct SimpleStringBufferMarshaller
        {
            public SimpleStringBufferMarshaller(StringBuilder builder)
            {
                int length = builder.Length + 1;
                Value = NativeMemory.Alloc(sizeof(char) * (nuint)length);
                Span<char> buffer = new(Value, length);
                buffer.Clear();
                builder.CopyTo(0, buffer, length - 1);
            }

            public void* Value { get; }

            public void FreeNative()
            {
                NativeMemory.Free(Value);
            }
        }
#endif

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpAddRequestHeaders(
            SafeWinHttpHandle requestHandle,
            string headers,
            uint headersLength,
            uint modifiers);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpSendRequest(
            SafeWinHttpHandle requestHandle,
            IntPtr headers,
            uint headersLength,
            IntPtr optional,
            uint optionalLength,
            uint totalLength,
            IntPtr context);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpReceiveResponse(
            SafeWinHttpHandle requestHandle,
            IntPtr reserved);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryDataAvailable(
            SafeWinHttpHandle requestHandle,
            IntPtr parameterIgnoredAndShouldBeNullForAsync);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpReadData(
            SafeWinHttpHandle requestHandle,
            IntPtr buffer,
            uint bufferSize,
            IntPtr parameterIgnoredAndShouldBeNullForAsync);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryHeaders(
            SafeWinHttpHandle requestHandle,
            uint infoLevel,
            string name,
            IntPtr buffer,
            ref uint bufferLength,
            ref uint index);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryHeaders(
            SafeWinHttpHandle requestHandle,
            uint infoLevel,
            string name,
            ref uint number,
            ref uint bufferLength,
            IntPtr index);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryOption(
            SafeWinHttpHandle handle,
            uint option,
            ref IntPtr buffer,
            ref uint bufferSize);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryOption(
            SafeWinHttpHandle handle,
            uint option,
            IntPtr buffer,
            ref uint bufferSize);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryOption(
            SafeWinHttpHandle handle,
            uint option,
            ref uint buffer,
            ref uint bufferSize);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpWriteData(
            SafeWinHttpHandle requestHandle,
            IntPtr buffer,
            uint bufferSize,
            IntPtr parameterIgnoredAndShouldBeNullForAsync);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpSetOption(
            SafeWinHttpHandle handle,
            uint option,
            ref uint optionData,
            uint optionLength = sizeof(uint));

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpSetOption(
            SafeWinHttpHandle handle,
            uint option,
            IntPtr optionData,
            uint optionLength);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpSetCredentials(
            SafeWinHttpHandle requestHandle,
            uint authTargets,
            uint authScheme,
            string? userName,
            string? password,
            IntPtr reserved);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryAuthSchemes(
            SafeWinHttpHandle requestHandle,
            out uint supportedSchemes,
            out uint firstScheme,
            out uint authTarget);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpSetTimeouts(
            SafeWinHttpHandle handle,
            int resolveTimeout,
            int connectTimeout,
            int sendTimeout,
            int receiveTimeout);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpGetIEProxyConfigForCurrentUser(
            out WINHTTP_CURRENT_USER_IE_PROXY_CONFIG proxyConfig);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]public static partial bool WinHttpGetProxyForUrl(
            SafeWinHttpHandle? sessionHandle,
            string url,
            ref WINHTTP_AUTOPROXY_OPTIONS autoProxyOptions,
            out WINHTTP_PROXY_INFO proxyInfo);

        [GeneratedDllImport(Interop.Libraries.WinHttp, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial IntPtr WinHttpSetStatusCallback(
            SafeWinHttpHandle handle,
            WINHTTP_STATUS_CALLBACK callback,
            uint notificationFlags,
            IntPtr reserved);
    }
}
