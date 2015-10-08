// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative)]
        internal extern static SafeEvpMdCtxHandle EvpMdCtxCreate(IntPtr type);

        [DllImport(Libraries.CryptoNative)]
        internal extern static void EvpMdCtxDestroy(IntPtr ctx);

        [DllImport(Libraries.CryptoNative)]
        internal extern static int EvpDigestReset(SafeEvpMdCtxHandle ctx, IntPtr type);

        [DllImport(Libraries.CryptoNative)]
        internal extern static unsafe int EvpDigestUpdate(SafeEvpMdCtxHandle ctx, byte* d, int cnt);

        [DllImport(Libraries.CryptoNative)]
        internal extern static unsafe int EvpDigestFinalEx(SafeEvpMdCtxHandle ctx, byte* md, ref uint s);

        [DllImport(Libraries.CryptoNative)]
        internal extern static int EvpMdSize(IntPtr md);


        [DllImport(Libraries.CryptoNative)]
        internal extern static IntPtr EvpMd5();

        [DllImport(Libraries.CryptoNative)]
        internal extern static IntPtr EvpSha1();

        [DllImport(Libraries.CryptoNative)]
        internal extern static IntPtr EvpSha256();

        [DllImport(Libraries.CryptoNative)]
        internal extern static IntPtr EvpSha384();

        [DllImport(Libraries.CryptoNative)]
        internal extern static IntPtr EvpSha512();


        [DllImport(Libraries.CryptoNative)]
        private extern static int GetMaxMdSize();

        internal static readonly int EVP_MAX_MD_SIZE = GetMaxMdSize();
    }
}
