// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class libssl
    {
        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern IntPtr SSLv23_method();

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern IntPtr SSLv3_method();

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern IntPtr TLSv1_method();

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern IntPtr TLSv1_1_method();

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern IntPtr TLSv1_2_method();

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern IntPtr SSL_CTX_new(IntPtr meth);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern long SSL_CTX_ctrl(SafeSslContextHandle ctx, int cmd, long larg, IntPtr parg);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern SafeSslHandle SSL_new(SafeSslContextHandle ctx);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern ulong ERR_get_error();

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern IntPtr ERR_reason_error_string(ulong error);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern SslErrorCode SSL_get_error(SafeSslHandle ssl, int ret);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern void SSL_free(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern void SSL_CTX_free(IntPtr ctx);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern void SSL_set_connect_state(SafeSslHandle ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern void SSL_set_accept_state(SafeSslHandle ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int SSL_write(SafeSslHandle ssl, IntPtr buf, int num);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int SSL_read(SafeSslHandle ssl, IntPtr buf, int num);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int SSL_renegotiate_pending(SafeSslHandle ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int SSL_shutdown(SafeSslHandle ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern void SSL_set_bio(SafeSslHandle ssl, SafeBioHandle rbio, SafeBioHandle wbio);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int SSL_do_handshake(SafeSslHandle ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int SSL_state(SafeSslHandle ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int BIO_read(SafeBioHandle bio, IntPtr buf, int num);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int BIO_write(SafeBioHandle bio, IntPtr buf, int num);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern SafeX509Handle SSL_get_peer_certificate(SafeSslHandle ssl);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_get_client_CA_list")]
        private static extern SafeSharedX509NameStackHandle SSL_get_client_CA_list_private(SafeSslHandle ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern SafeSharedX509StackHandle SSL_get_peer_cert_chain(SafeSslHandle ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern IntPtr SSL_get_current_cipher(SafeSslHandle ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int SSL_CTX_use_certificate(SafeSslContextHandle ctx, SafeX509Handle certPtr);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int SSL_CTX_use_PrivateKey(SafeSslContextHandle ctx, SafeEvpPKeyHandle keyPtr);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int SSL_CTX_check_private_key(SafeSslContextHandle ctx);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int BIO_ctrl_pending(SafeBioHandle bio);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern void SSL_CTX_set_quiet_shutdown(SafeSslContextHandle ctx, int mode);

        internal static SafeSharedX509NameStackHandle SSL_get_client_CA_list(SafeSslHandle ssl)
        {
            Interop.Crypto.CheckValidOpenSslHandle(ssl);

            SafeSharedX509NameStackHandle handle = SSL_get_client_CA_list_private(ssl);

            if (!handle.IsInvalid)
            {
                handle.SetParent(ssl);
            }

            return handle;
        }

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern IntPtr SSL_get_version(SafeSslHandle ssl);
    }
}
