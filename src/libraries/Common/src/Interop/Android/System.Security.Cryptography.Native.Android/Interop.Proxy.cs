// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        internal enum AndroidProxyType
        {
            Direct = 0,
            Http = 1,
            Socks = 2,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AndroidProxyInfo
        {
            public int Type;     // AndroidProxyType
            public IntPtr Host;  // NUL-terminated UTF-16; read via Marshal.PtrToStringUni; freed by FreeProxyResult
            public int Port;
        }

        [LibraryImport(Libraries.AndroidCryptoNative,
            EntryPoint = "AndroidCryptoNative_GetProxyForUrl",
            StringMarshalling = StringMarshalling.Utf8)]
        internal static unsafe partial int GetProxyForUrl(
            string url,
            out int count,
            out AndroidProxyInfo* proxies);

        [LibraryImport(Libraries.AndroidCryptoNative,
            EntryPoint = "AndroidCryptoNative_FreeProxyResult")]
        internal static unsafe partial void FreeProxyResult(
            AndroidProxyInfo* proxies, int count);
    }
}
