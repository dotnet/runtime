// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

using size_t  = System.IntPtr;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        [DllImport(Libraries.LibCrypto)]
        internal extern static SafeEvpMdCtxHandle EVP_MD_CTX_create();

        [DllImport(Libraries.LibCrypto)]
        internal extern static int EVP_DigestInit_ex(SafeEvpMdCtxHandle ctx, IntPtr type, IntPtr impl);

        [DllImport(Libraries.LibCrypto)]
        internal extern static unsafe int EVP_DigestUpdate(SafeEvpMdCtxHandle ctx, byte* d, size_t cnt);

        [DllImport(Libraries.LibCrypto)]
        internal extern static unsafe int EVP_DigestFinal_ex(SafeEvpMdCtxHandle ctx, byte* md, ref uint s);

        [DllImport(Libraries.LibCrypto)]
        internal extern static unsafe void EVP_MD_CTX_destroy(IntPtr ctx);

        [DllImport(Libraries.LibCrypto)]
        internal extern static unsafe int EVP_MD_size(IntPtr md);


        [DllImport(Libraries.LibCrypto)]
        internal extern static IntPtr EVP_md5();

        [DllImport(Libraries.LibCrypto)]
        internal extern static IntPtr EVP_sha1();

        [DllImport(Libraries.LibCrypto)]
        internal extern static IntPtr EVP_sha256();

        [DllImport(Libraries.LibCrypto)]
        internal extern static IntPtr EVP_sha384();

        [DllImport(Libraries.LibCrypto)]
        internal extern static IntPtr EVP_sha512();


        internal const int EVP_MAX_MD_SIZE = 64;
    }
}
