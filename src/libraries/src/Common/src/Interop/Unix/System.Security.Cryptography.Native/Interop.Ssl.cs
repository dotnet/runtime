// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Ssl
    {
        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr SslV2_3Method();

        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr SslV3Method();

        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr TlsV1Method();

        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr TlsV1_1Method();

        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr TlsV1_2Method();

        [DllImport(Libraries.CryptoNative)]
        internal static extern SafeSslContextHandle SslCtxCreate(IntPtr method);

        [DllImport(Libraries.CryptoNative)]
        internal static extern long SslCtxCtrl(SafeSslContextHandle ctx, int cmd, long larg, IntPtr parg);

        [DllImport(Libraries.CryptoNative)]
        internal static extern SafeSslHandle SslCreate(SafeSslContextHandle ctx);

        [DllImport(Libraries.CryptoNative)]
        internal static extern libssl.SslErrorCode SslGetError(SafeSslHandle ssl, int ret);

        [DllImport(Libraries.CryptoNative)]
        internal static extern void SslDestroy(IntPtr ssl);

        [DllImport(Libraries.CryptoNative)]
        internal static extern void SslCtxDestroy(IntPtr ctx);

        [DllImport(Libraries.CryptoNative)]
        internal static unsafe extern int SslWrite(SafeSslHandle ssl, byte* buf, int num);

        [DllImport(Libraries.CryptoNative)]
        internal static extern int SslRead(SafeSslHandle ssl, byte[] buf, int num);

        // NOTE: this is just an (unsafe) overload to the BioWrite method from Interop.Bio.cs.
        [DllImport(Libraries.CryptoNative)]
        internal static unsafe extern int BioWrite(SafeBioHandle b, byte* data, int len);
    }
}
