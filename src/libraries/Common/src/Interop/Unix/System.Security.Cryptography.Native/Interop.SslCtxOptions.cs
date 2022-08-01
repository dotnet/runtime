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
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxAddExtraChainCert")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SslCtxAddExtraChainCert(SafeSslContextHandle ctx, SafeX509Handle x509);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxUseCertificate")]
        internal static partial int SslCtxUseCertificate(SafeSslContextHandle ctx, SafeX509Handle certPtr);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxUsePrivateKey")]
        internal static partial int SslCtxUsePrivateKey(SafeSslContextHandle ctx, SafeEvpPKeyHandle keyPtr);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxCheckPrivateKey")]
        internal static partial int SslCtxCheckPrivateKey(SafeSslContextHandle ctx);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetQuietShutdown")]
        internal static partial void SslCtxSetQuietShutdown(SafeSslContextHandle ctx);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetCiphers")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool SslCtxSetCiphers(SafeSslContextHandle ctx, byte* cipherList, byte* cipherSuites);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetEncryptionPolicy")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetEncryptionPolicy(SafeSslContextHandle ctx, EncryptionPolicy policy);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetDefaultOcspCallback")]
        internal static partial void SslCtxSetDefaultOcspCallback(SafeSslContextHandle ctx);
    }
}
