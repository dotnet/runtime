// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_PemReadBioPkcs7")]
        internal static extern SafePkcs7Handle PemReadBioPkcs7(SafeBioHandle bp);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DecodePkcs7")]
        internal static extern SafePkcs7Handle DecodePkcs7(byte[] buf, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_D2IPkcs7Bio")]
        internal static extern SafePkcs7Handle D2IPkcs7Bio(SafeBioHandle bp);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Pkcs7CreateSigned")]
        internal static extern SafePkcs7Handle Pkcs7CreateSigned();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Pkcs7Destroy")]
        internal static extern void Pkcs7Destroy(IntPtr p7);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetPkcs7Certificates")]
        private static extern int GetPkcs7Certificates(SafePkcs7Handle p7, out SafeSharedX509StackHandle certs);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Pkcs7AddCertificate")]
        internal static extern bool Pkcs7AddCertificate(SafePkcs7Handle p7, IntPtr x509);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetPkcs7DerSize")]
        internal static extern int GetPkcs7DerSize(SafePkcs7Handle p7);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EncodePkcs7")]
        internal static extern int EncodePkcs7(SafePkcs7Handle p7, byte[] buf);

        internal static SafeSharedX509StackHandle GetPkcs7Certificates(SafePkcs7Handle p7)
        {
            if (p7 == null || p7.IsInvalid)
            {
                return SafeSharedX509StackHandle.InvalidHandle;
            }

            SafeSharedX509StackHandle certs;
            int result = GetPkcs7Certificates(p7, out certs);

            if (result != 1)
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            // Track the parent relationship for the interior pointer so lifetime is well-managed.
            certs.SetParent(p7);

            return certs;
        }
    }
}
