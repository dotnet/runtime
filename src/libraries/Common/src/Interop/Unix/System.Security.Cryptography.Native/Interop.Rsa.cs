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
        internal static extern SafeRsaHandle RsaCreate();

        [DllImport(Libraries.CryptoNative)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RsaUpRef(IntPtr rsa);

        [DllImport(Libraries.CryptoNative)]
        internal static extern void RsaDestroy(IntPtr rsa);

        [DllImport(Libraries.CryptoNative)]
        internal static extern SafeRsaHandle DecodeRsaPublicKey(byte[] buf, int len);

        [DllImport(Libraries.CryptoNative)]
        internal extern static int RsaPublicEncrypt(
            int flen, 
            byte[] from, 
            byte[] to, 
            SafeRsaHandle rsa, 
            [MarshalAs(UnmanagedType.Bool)] bool useOaepPadding);

        [DllImport(Libraries.CryptoNative)]
        internal extern static int RsaPrivateDecrypt(
            int flen, 
            byte[] from, 
            byte[] to, 
            SafeRsaHandle rsa, 
            [MarshalAs(UnmanagedType.Bool)] bool useOaepPadding);

        [DllImport(Libraries.CryptoNative)]
        internal static extern int RsaSize(SafeRsaHandle rsa);

        [DllImport(Libraries.CryptoNative)]
        internal static extern int RsaGenerateKeyEx(SafeRsaHandle rsa, int bits, SafeBignumHandle e);

        [DllImport(Libraries.CryptoNative)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RsaSign(int type, byte[] m, int m_len, byte[] sigret, out int siglen, SafeRsaHandle rsa);

        [DllImport(Libraries.CryptoNative)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RsaVerify(int type, byte[] m, int m_len, byte[] sigbuf, int siglen, SafeRsaHandle rsa);
    }
}
