// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        // This was computed by sizeof(EVP_CIPHER_CTX) on a 64-bit Ubuntu
        // machine running OpenSSL 1.0.1f.  If we end up making a native
        // interop boundary library for OpenSSL then a very good candidate
        // method would be EVP_CIPHER_CTX_new, so the size can be computed
        // to match the platform.
        internal const int EVP_CIPHER_CTX_SIZE = 168;

        [DllImport(Libraries.LibCrypto)]
        internal static extern void EVP_CIPHER_CTX_init(SafeEvpCipherCtxHandle ctx);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EVP_CipherInit_ex(
            SafeEvpCipherCtxHandle ctx,
            IntPtr cipher,
            IntPtr engineNull,
            byte[] key,
            byte[] iv,
            int enc);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EVP_CIPHER_CTX_set_padding(SafeEvpCipherCtxHandle x, int padding);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe extern bool EVP_CipherUpdate(
            SafeEvpCipherCtxHandle ctx,
            byte* @out,
            out int outl,
            byte* @in,
            int inl);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool EVP_CipherFinal_ex(
            SafeEvpCipherCtxHandle ctx,
            byte* outm,
            out int outl);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EVP_CIPHER_CTX_cleanup(IntPtr ctx);

        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr EVP_aes_128_ecb();

        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr EVP_aes_128_cbc();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_128_cfb1();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_128_cfb8();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_128_cfb128();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_128_ofb();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_128_ctr();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_128_ccm();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_128_gcm();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_128_xts();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_128_wrap();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_128_wrap_pad();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_128_ocb();

        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr EVP_aes_192_ecb();

        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr EVP_aes_192_cbc();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_192_cfb1();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_192_cfb8();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_192_cfb128();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_192_ofb();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_192_ctr();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_192_ccm();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_192_gcm();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_192_wrap();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_192_wrap_pad();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_192_ocb();

        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr EVP_aes_256_ecb();

        [DllImport(Libraries.LibCrypto)]
        internal static extern IntPtr EVP_aes_256_cbc();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_256_cfb1();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_256_cfb8();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_256_cfb128();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_256_ofb();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_256_ctr();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_256_ccm();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_256_gcm();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_256_xts();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_256_wrap();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_256_wrap_pad();

        //[DllImport(Libraries.LibCrypto)]
        //internal static extern IntPtr EVP_aes_256_ocb();
    }
}
