// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class OpenSsl
    {
        // Targeting OpenSSL 3
        private const string LibSsl = "libssl.so.3";
        private const string LibCrypto = "libcrypto.so.3";

        internal const int SSL_VERIFY_NONE = 0x00;
        internal const int SSL_VERIFY_PEER = 0x01;

        // OpenSSL: int (*callback)(int preverify_ok, X509_STORE_CTX *x509_ctx);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int VerifyCallback(int preverify_ok, IntPtr x509StoreCtx);

        [LibraryImport(LibSsl, EntryPoint = "SSL_set_verify")]
        internal static partial void SSL_set_verify(
            IntPtr ssl,
            int mode,
            VerifyCallback callback);

        [LibraryImport(LibSsl, EntryPoint = "SSL_CTX_set_verify")]
        internal static partial void SSL_CTX_set_verify(
            IntPtr ctx,
            int mode,
            VerifyCallback callback);

        [LibraryImport(LibSsl, EntryPoint = "X509_STORE_CTX_get_current_cert")]
        internal static partial IntPtr X509_STORE_CTX_get_current_cert(IntPtr ctx);

        [LibraryImport(LibSsl, EntryPoint = "X509_STORE_CTX_get_error_depth")]
        internal static partial int X509_STORE_CTX_get_error_depth(IntPtr ctx);

        [LibraryImport(LibSsl, EntryPoint = "X509_STORE_CTX_get_error")]
        internal static partial int X509_STORE_CTX_get_error(IntPtr ctx);

        // int i2d_X509(X509 *a, unsigned char **out);
        [LibraryImport(LibSsl, EntryPoint = "i2d_X509")]
        internal static partial int i2d_X509(IntPtr x509, ref IntPtr pp);

        // void CRYPTO_free(void *ptr, const char *file, int line);
        [LibraryImport(LibCrypto, EntryPoint = "CRYPTO_free")]
        internal static partial void CRYPTO_free(IntPtr ptr, IntPtr file, int line);
    }
}
