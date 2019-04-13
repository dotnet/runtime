// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SetCiphers")]
        internal static unsafe extern bool SetCiphers(SafeSslContextHandle ctx, byte* cipherList, byte* cipherSuites);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SetEncryptionPolicy")]
        internal static extern bool SetEncryptionPolicy(SafeSslContextHandle ctx, EncryptionPolicy policy);
    }
}
