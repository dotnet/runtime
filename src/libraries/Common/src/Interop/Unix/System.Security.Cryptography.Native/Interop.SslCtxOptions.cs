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
        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxUseCertificate")]
        internal static partial int SslCtxUseCertificate(SafeSslContextHandle ctx, SafeX509Handle certPtr);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxUsePrivateKey")]
        internal static partial int SslCtxUsePrivateKey(SafeSslContextHandle ctx, SafeEvpPKeyHandle keyPtr);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxCheckPrivateKey")]
        internal static partial int SslCtxCheckPrivateKey(SafeSslContextHandle ctx);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetQuietShutdown")]
        internal static partial void SslCtxSetQuietShutdown(SafeSslContextHandle ctx);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetVerify")]
        internal static unsafe partial void SslCtxSetVerify(SafeSslContextHandle ctx, delegate* unmanaged<int, IntPtr, int> callback);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SetCiphers")]
        internal static unsafe partial bool SetCiphers(SafeSslContextHandle ctx, byte* cipherList, byte* cipherSuites);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SetEncryptionPolicy")]
        internal static partial bool SetEncryptionPolicy(SafeSslContextHandle ctx, EncryptionPolicy policy);
    }
}
