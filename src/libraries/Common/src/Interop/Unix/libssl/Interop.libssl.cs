// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libssl
    {

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "ERR_load_BIO_strings")]
        internal static extern void ERR_load_BIO_strings();

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSLv3_server_method")]
        internal static extern IntPtr SSLv3_server_method();

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSLv3_client_method")]
        internal static extern IntPtr SSLv3_client_method();

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSLv23_server_method")]
        internal static extern IntPtr SSLv23_server_method();

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSLv23_client_method")]
        internal static extern IntPtr SSLv23_client_method();

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "TLS_server_method")]
        internal static extern IntPtr TLS_server_method();

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "TLS_client_method")]
        internal static extern IntPtr TLS_client_method();

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "TLSv1_server_method")]
        internal static extern IntPtr TLSv1_server_method();

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "TLSv1_client_method")]
        internal static extern IntPtr TLSv1_client_method();

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "TLSv1_1_server_method")]
        internal static extern IntPtr TLSv1_1_server_method();

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "TLSv1_1_client_method")]
        internal static extern IntPtr TLSv1_1_client_method();

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "TLSv1_2_server_method")]
        internal static extern IntPtr TLSv1_2_server_method();

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "TLSv1_2_client_method")]
        internal static extern IntPtr TLSv1_2_client_method();

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_CTX_new")]
        internal static extern IntPtr SSL_CTX_new(IntPtr meth);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_CTX_ctrl")]
        internal static extern long SSL_CTX_ctrl(IntPtr ctx, int cmd, long larg, IntPtr parg);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_new")]
        internal static extern IntPtr SSL_new(IntPtr ctx);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "ERR_get_error")]
        internal static extern ulong ERR_get_error();

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "ERR_reason_error_string")]
        internal static extern IntPtr ERR_reason_error_string(ulong error);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_get_error")]
        internal static extern int SSL_get_error(IntPtr ssl, int ret);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_free")]
        internal static extern void SSL_free(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_CTX_free")]
        internal static extern void SSL_CTX_free(IntPtr ctx);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_set_connect_state")]
        internal static extern void SSL_set_connect_state(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_set_accept_state")]
        internal static extern void SSL_set_accept_state(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_write")]
        internal static extern int SSL_write(IntPtr ssl, IntPtr buf, int num);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_read")]
        internal static extern int SSL_read(IntPtr ssl, IntPtr buf, int num);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_shutdown")]
        internal static extern int SSL_shutdown(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "BIO_new")]
        internal static extern IntPtr BIO_new(IntPtr type);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_set_bio")]
        internal static extern void SSL_set_bio(IntPtr ssl, IntPtr rbio, IntPtr wbio);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_do_handshake")]
        internal static extern int SSL_do_handshake(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_state")]
        internal static extern int SSL_state(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "BIO_read")]
        internal static extern int BIO_read(IntPtr bio, IntPtr buf, int num);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "BIO_write")]
        internal static extern int BIO_write(IntPtr bio, IntPtr buf, int num);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_get_peer_certificate")]
        internal static extern IntPtr SSL_get_peer_certificate(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_get_current_cipher")]
        internal static extern IntPtr SSL_get_current_cipher(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "X509_free")]
        internal static extern void X509_free(IntPtr certPtr);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "i2d_X509")]
        internal static extern unsafe int i2d_X509(IntPtr certPtr, ref byte* bufPtr);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "d2i_X509")]
        internal static extern unsafe IntPtr d2i_X509(IntPtr certPtr, ref byte* bufPtr, int len);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_CTX_use_certificate")]
        internal static extern int SSL_CTX_use_certificate(IntPtr ssl, IntPtr certPtr);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_CTX_use_PrivateKey")]
        internal static extern int SSL_CTX_use_PrivateKey(IntPtr ssl, IntPtr certPtr);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_CTX_check_private_key")]
        internal static extern int SSL_CTX_check_private_key(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "BIO_ctrl_pending")]
        internal static extern int BIO_ctrl_pending(IntPtr bio);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_CTX_set_quiet_shutdown")]
        internal static extern void SSL_CTX_set_quiet_shutdown(IntPtr ssl, int mode);
    }
}