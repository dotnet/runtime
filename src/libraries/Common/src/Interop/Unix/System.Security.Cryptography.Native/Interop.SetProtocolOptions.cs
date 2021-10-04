// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Ssl
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetProtocolOptions")]
        internal static extern void SslCtxSetProtocolOptions(IntPtr ctx, SslProtocols protocols);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetProtocolOptions")]
        internal static extern void SslCtxSetProtocolOptions(SafeSslContextHandle ctx, SslProtocols protocols);
    }
}
