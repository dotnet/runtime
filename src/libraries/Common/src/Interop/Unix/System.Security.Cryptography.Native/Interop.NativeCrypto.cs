// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NativeCrypto
    {
        [DllImport(Libraries.CryptoInterop)]
        internal static extern int GetX509Thumbprint(SafeX509Handle x509, byte[] buf, int cBuf);

        [DllImport(Libraries.CryptoInterop)]
        internal static extern int GetX509NameRawBytes(IntPtr x509Name, byte[] buf, int cBuf);

        [DllImport(Libraries.CryptoInterop)]
        internal static extern IntPtr GetX509NotBefore(SafeX509Handle x509);

        [DllImport(Libraries.CryptoInterop)]
        internal static extern IntPtr GetX509NotAfter(SafeX509Handle x509);

        [DllImport(Libraries.CryptoInterop)]
        internal static extern int GetX509Version(SafeX509Handle x509);

        [DllImport(Libraries.CryptoInterop)]
        internal static extern IntPtr GetX509SignatureAlgorithm(SafeX509Handle x509);

        [DllImport(Libraries.CryptoInterop)]
        internal static extern IntPtr GetX509PublicKeyAlgorithm(SafeX509Handle x509);

        [DllImport(Libraries.CryptoInterop)]
        internal static extern int GetX509PublicKeyParameterBytes(SafeX509Handle x509, byte[] buf, int cBuf);

        [DllImport(Libraries.CryptoInterop)]
        internal static extern IntPtr GetX509PublicKeyBytes(SafeX509Handle x509);

        [DllImport(Libraries.CryptoInterop)]
        internal static extern int GetX509EkuFieldCount(SafeEkuExtensionHandle eku);

        [DllImport(Libraries.CryptoInterop)]
        internal static extern IntPtr GetX509EkuField(SafeEkuExtensionHandle eku, int loc);

        [DllImport(Libraries.CryptoInterop)]
        internal static extern SafeBioHandle GetX509NameInfo(SafeX509Handle x509, int nameType, [MarshalAs(UnmanagedType.Bool)] bool forIssuer);

        [DllImport(Libraries.CryptoInterop)]
        private static extern int GetAsn1StringBytes(IntPtr asn1, byte[] buf, int cBuf);

        internal static byte[] GetAsn1StringBytes(IntPtr asn1)
        {
            int negativeSize = GetAsn1StringBytes(asn1, null, 0);

            if (negativeSize > 0)
            {
                throw new CryptographicException();
            }

            byte[] bytes = new byte[-negativeSize];

            int ret = GetAsn1StringBytes(asn1, bytes, bytes.Length);

            if (ret != 1)
            {
                throw new CryptographicException();
            }

            return bytes;
        }
    }
}
