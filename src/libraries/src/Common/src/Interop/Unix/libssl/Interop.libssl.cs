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
        internal static extern long SSL_CTX_ctrl(IntPtr ctx, int cmd, long larg, IntPtr parg);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern IntPtr SSL_new(IntPtr ctx);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern ulong ERR_get_error();

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern IntPtr ERR_reason_error_string(ulong error);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int SSL_get_error(IntPtr ssl, int ret);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern void SSL_free(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern void SSL_CTX_free(IntPtr ctx);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern void SSL_set_connect_state(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern void SSL_set_accept_state(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int SSL_write(IntPtr ssl, IntPtr buf, int num);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int SSL_read(IntPtr ssl, IntPtr buf, int num);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int SSL_shutdown(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern IntPtr BIO_new(IntPtr type);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern void SSL_set_bio(IntPtr ssl, IntPtr rbio, IntPtr wbio);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int SSL_do_handshake(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int SSL_state(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int BIO_read(IntPtr bio, IntPtr buf, int num);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int BIO_write(IntPtr bio, IntPtr buf, int num);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern IntPtr SSL_get_peer_certificate(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern IntPtr SSL_get_current_cipher(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int SSL_CTX_use_certificate(IntPtr ssl, SafeX509Handle certPtr);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int SSL_CTX_use_PrivateKey(IntPtr ssl, SafeEvpPKeyHandle keyPtr);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int SSL_CTX_check_private_key(IntPtr ssl);

        [DllImport(Interop.Libraries.LibSsl)]
        internal static extern int BIO_ctrl_pending(IntPtr bio);

        [DllImport(Interop.Libraries.LibSsl, EntryPoint = "SSL_CTX_set_quiet_shutdown")]
        internal static extern void SSL_CTX_set_quiet_shutdown(IntPtr ssl, int mode);
    }
}