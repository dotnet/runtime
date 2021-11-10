// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SecKeychainEnumerateCerts(
            out SafeCFArrayHandle matches);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SecKeychainEnumerateIdentities(
            out SafeCFArrayHandle matches);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_X509StoreAddCertificate(
            SafeHandle certOrIdentity);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_X509StoreRemoveCertificate(
            SafeHandle certOrIdentity,
            bool isReadOnlyMode);

        internal static SafeCFArrayHandle KeychainEnumerateCerts()
        {
            SafeCFArrayHandle matches;
            int osStatus = AppleCryptoNative_SecKeychainEnumerateCerts(out matches);

            if (osStatus == 0)
            {
                return matches;
            }

            matches.Dispose();
            throw CreateExceptionForOSStatus(osStatus);
        }

        internal static SafeCFArrayHandle KeychainEnumerateIdentities()
        {
            SafeCFArrayHandle matches;
            int osStatus = AppleCryptoNative_SecKeychainEnumerateIdentities(out matches);

            if (osStatus == 0)
            {
                return matches;
            }

            matches.Dispose();
            throw CreateExceptionForOSStatus(osStatus);
        }

        internal static void X509StoreAddCertificate(SafeHandle certOrIdentity)
        {
            int osStatus = AppleCryptoNative_X509StoreAddCertificate(certOrIdentity);

            if (osStatus != 0)
            {
                throw CreateExceptionForOSStatus(osStatus);
            }
        }

        internal static void X509StoreRemoveCertificate(SafeHandle certOrIdentity, bool isReadOnlyMode)
        {
            const int errSecItemNotFound = -25300;

            int osStatus = AppleCryptoNative_X509StoreRemoveCertificate(certOrIdentity, isReadOnlyMode);

            if (osStatus == 0 && isReadOnlyMode)
            {
                // The certificate exists in the store otherwise we would get errSecItemNotFound error
                throw new CryptographicException(SR.Cryptography_X509_StoreReadOnly);
            }

            if (osStatus != 0 && osStatus != errSecItemNotFound)
            {
                throw CreateExceptionForOSStatus(osStatus);
            }
        }
    }
}
