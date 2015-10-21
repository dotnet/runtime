// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Ssl
    {
        [DllImport(Interop.Libraries.CryptoNative)]
        internal static extern IntPtr SslV2_3Method();

        [DllImport(Interop.Libraries.CryptoNative)]
        internal static extern IntPtr SslV3Method();

        [DllImport(Interop.Libraries.CryptoNative)]
        internal static extern IntPtr TlsV1Method();

        [DllImport(Interop.Libraries.CryptoNative)]
        internal static extern IntPtr TlsV1_1Method();

        [DllImport(Interop.Libraries.CryptoNative)]
        internal static extern IntPtr TlsV1_2Method();

        [DllImport(Interop.Libraries.CryptoNative)]
        internal static extern libssl.SafeSslContextHandle SslCtxCreate(IntPtr method);

        [DllImport(Interop.Libraries.CryptoNative)]
        internal static unsafe extern int SslWrite(libssl.SafeSslHandle ssl, byte* buf, int num);

        [DllImport(Interop.Libraries.CryptoNative)]
        internal static extern int SslRead(libssl.SafeSslHandle ssl, byte[] buf, int num);

        // NOTE: this is just an (unsafe) overload to the BioWrite method from Interop.Bio.cs.
        [DllImport(Libraries.CryptoNative)]
        internal static unsafe extern int BioWrite(SafeBioHandle b, byte* data, int len);
    }
}
