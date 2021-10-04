// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Ssl
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxAddExtraChainCert")]
        internal static extern bool SslCtxAddExtraChainCert(SafeSslContextHandle ctx, SafeX509Handle x509);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxUseCertificate")]
        internal static extern int SslCtxUseCertificate(SafeSslContextHandle ctx, SafeX509Handle certPtr);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxUsePrivateKey")]
        internal static extern int SslCtxUsePrivateKey(SafeSslContextHandle ctx, SafeEvpPKeyHandle keyPtr);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxCheckPrivateKey")]
        internal static extern int SslCtxCheckPrivateKey(SafeSslContextHandle ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetQuietShutdown")]
        internal static extern void SslCtxSetQuietShutdown(SafeSslContextHandle ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetVerify")]
        internal static extern unsafe void SslCtxSetVerify(SafeSslContextHandle ctx, delegate* unmanaged<int, IntPtr, int> callback);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetCiphers")]
        internal static extern unsafe bool SslCtxSetCiphers(SafeSslContextHandle ctx, byte* cipherList, byte* cipherSuites);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetEncryptionPolicy")]
        internal static extern bool SetEncryptionPolicy(SafeSslContextHandle ctx, EncryptionPolicy policy);
    }
}
