// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.X509Certificates;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_X509StoreAddCertificate(
            SafeKeychainItemHandle cert,
            SafeKeychainHandle keychain,
            out int pOSStatus);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_X509StoreRemoveCertificate(
            SafeSecCertificateHandle cert,
            SafeKeychainHandle keychain,
            out int pOSStatus);

        internal static void X509StoreAddCertificate(SafeKeychainItemHandle certOrIdentity, SafeKeychainHandle keychain)
        {
            int osStatus;
            int ret = AppleCryptoNative_X509StoreAddCertificate(certOrIdentity, keychain, out osStatus);

            if (ret == 0)
            {
                throw CreateExceptionForOSStatus(osStatus);
            }

            if (ret != 1)
            {
                Debug.Fail($"Unexpected result from AppleCryptoNative_X509StoreAddCertificate: {ret}");
                throw new CryptographicException();
            }
        }

        internal static void X509StoreRemoveCertificate(SafeSecCertificateHandle certHandle, SafeKeychainHandle keychain)
        {
            int osStatus;
            int ret = AppleCryptoNative_X509StoreRemoveCertificate(certHandle, keychain, out osStatus);

            if (ret == 0)
            {
                throw CreateExceptionForOSStatus(osStatus);
            }

            const int SuccessOrNoMatch = 1;
            const int UserTrustExists = 2;
            const int AdminTrustExists = 3;

            switch (ret)
            {
                case SuccessOrNoMatch:
                    break;
                case UserTrustExists:
                    throw new CryptographicException(SR.Cryptography_X509Store_WouldModifyUserTrust);
                case AdminTrustExists:
                    throw new CryptographicException(SR.Cryptography_X509Store_WouldModifyAdminTrust);
                default:
                    Debug.Fail($"Unexpected result from AppleCryptoNative_X509StoreRemoveCertificate: {ret}");
                    throw new CryptographicException();
            }
        }
    }
}