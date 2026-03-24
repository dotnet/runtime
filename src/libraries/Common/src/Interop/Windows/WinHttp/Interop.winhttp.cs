// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
#if NET
using System.Runtime.InteropServices.Marshalling;
#endif
using System.Text;

internal static partial class Interop
{
    internal static partial class WinHttp
    {
        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        public static partial SafeWinHttpHandle WinHttpOpen(
            IntPtr userAgent,
            uint accessType,
            string? proxyName,
            string? proxyBypass, int flags);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpCloseHandle(
            IntPtr handle);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        public static partial SafeWinHttpHandle WinHttpConnect(
            SafeWinHttpHandle sessionHandle,
            string serverName,
            ushort serverPort,
            uint reserved);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        public static partial SafeWinHttpHandle WinHttpOpenRequest(
            SafeWinHttpHandle connectHandle,
            string verb,
            string objectName,
            string? version,
            string referrer,
            string acceptTypes,
            uint flags);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpAddRequestHeaders(
            SafeWinHttpHandle requestHandle,
#if NET
            [MarshalUsing(typeof(SimpleStringBufferMarshaller))] StringBuilder headers,
#else
#pragma warning disable CA1838 // Uses pooled StringBuilder
            [In] StringBuilder headers,
#pragma warning restore CA1838 // Uses pooled StringBuilder
#endif
            uint headersLength,
            uint modifiers);

#if NET
        [CustomMarshaller(typeof(StringBuilder), MarshalMode.ManagedToUnmanagedIn, typeof(SimpleStringBufferMarshaller))]
        private static unsafe class SimpleStringBufferMarshaller
        {
            public static void* ConvertToUnmanaged(StringBuilder builder)
            {
                int length = builder.Length + 1;
                void* value = NativeMemory.Alloc(sizeof(char) * (nuint)length);
                Span<char> buffer = new(value, length);
                buffer.Clear();
                builder.CopyTo(0, buffer, length - 1);
                return value;
            }

            public static void Free(void* value) => NativeMemory.Free(value);
        }
#endif

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpAddRequestHeaders(
            SafeWinHttpHandle requestHandle,
            string headers,
            uint headersLength,
            uint modifiers);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpSendRequest(
            SafeWinHttpHandle requestHandle,
            IntPtr headers,
            uint headersLength,
            IntPtr optional,
            uint optionalLength,
            uint totalLength,
            IntPtr context);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpReceiveResponse(
            SafeWinHttpHandle requestHandle,
            IntPtr reserved);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryDataAvailable(
            SafeWinHttpHandle requestHandle,
            IntPtr parameterIgnoredAndShouldBeNullForAsync);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpReadData(
            SafeWinHttpHandle requestHandle,
            IntPtr buffer,
            uint bufferSize,
            IntPtr parameterIgnoredAndShouldBeNullForAsync);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryHeaders(
            SafeWinHttpHandle requestHandle,
            uint infoLevel,
            string name,
            IntPtr buffer,
            ref uint bufferLength,
            ref uint index);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryHeaders(
            SafeWinHttpHandle requestHandle,
            uint infoLevel,
            string name,
            ref uint number,
            ref uint bufferLength,
            IntPtr index);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryOption(
            SafeWinHttpHandle handle,
            uint option,
            ref IntPtr buffer,
            ref uint bufferSize);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryOption(
            SafeWinHttpHandle handle,
            uint option,
            IntPtr buffer,
            ref uint bufferSize);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryOption(
            SafeWinHttpHandle handle,
            uint option,
            ref uint buffer,
            ref uint bufferSize);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpWriteData(
            SafeWinHttpHandle requestHandle,
            IntPtr buffer,
            uint bufferSize,
            IntPtr parameterIgnoredAndShouldBeNullForAsync);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpSetOption(
            SafeWinHttpHandle handle,
            uint option,
            ref uint optionData,
            uint optionLength = sizeof(uint));

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpSetOption(
            SafeWinHttpHandle handle,
            uint option,
            IntPtr optionData,
            uint optionLength);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpSetCredentials(
            SafeWinHttpHandle requestHandle,
            uint authTargets,
            uint authScheme,
            string? userName,
            string? password,
            IntPtr reserved);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpQueryAuthSchemes(
            SafeWinHttpHandle requestHandle,
            out uint supportedSchemes,
            out uint firstScheme,
            out uint authTarget);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpSetTimeouts(
            SafeWinHttpHandle handle,
            int resolveTimeout,
            int connectTimeout,
            int sendTimeout,
            int receiveTimeout);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpGetIEProxyConfigForCurrentUser(
            out WINHTTP_CURRENT_USER_IE_PROXY_CONFIG proxyConfig);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WinHttpGetProxyForUrl(
            SafeWinHttpHandle? sessionHandle,
            string url,
            ref WINHTTP_AUTOPROXY_OPTIONS autoProxyOptions,
            out WINHTTP_PROXY_INFO proxyInfo);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.WinHttp, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        public static partial IntPtr WinHttpSetStatusCallback(
            SafeWinHttpHandle handle,
            WINHTTP_STATUS_CALLBACK callback,
            uint notificationFlags,
            IntPtr reserved);
    }
}
