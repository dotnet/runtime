// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Ssl
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SetProtocolOptions")]
        internal static extern void SetProtocolOptions(SafeSslContextHandle ctx, SslProtocols protocols);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxUseCertificate")]
        internal static extern int SslCtxUseCertificate(SafeSslContextHandle ctx, SafeX509Handle certPtr);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxUsePrivateKey")]
        internal static extern int SslCtxUsePrivateKey(SafeSslContextHandle ctx, SafeEvpPKeyHandle keyPtr);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxCheckPrivateKey")]
        internal static extern int SslCtxCheckPrivateKey(SafeSslContextHandle ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetQuietShutdown")]
        internal static extern void SslCtxSetQuietShutdown(SafeSslContextHandle ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetVerify")]
        internal static extern void SslCtxSetVerify(SafeSslContextHandle ctx, SslCtxSetVerifyCallback callback);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SetEncryptionPolicy")]
        internal static extern void SetEncryptionPolicy(SafeSslContextHandle ctx, EncryptionPolicy policy);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetClientCAList")]
        internal static extern void SslCtxSetClientCAList(SafeSslContextHandle ctx, SafeX509NameStackHandle x509NameStackPtr);
    }
}
