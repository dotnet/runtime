// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509StoreAddCertificate")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool X509StoreAddCertificate(
            SafeX509StoreHandle store,
            SafeX509Handle cert,
            string hashString);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509StoreAddCertificateWithPrivateKey")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool X509StoreAddCertificateWithPrivateKey(
            SafeX509StoreHandle store,
            SafeX509Handle cert,
            SafeKeyHandle key,
            PAL_KeyAlgorithm algorithm,
            string hashString);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509StoreContainsCertificate")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool X509StoreContainsCertificate(
            SafeX509StoreHandle store,
            SafeX509Handle cert,
            string hashString);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509StoreEnumerateCertificates")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool X509StoreEnumerateCertificates(
            SafeX509StoreHandle storeHandle,
            delegate* unmanaged<void*, void*, Interop.AndroidCrypto.PAL_KeyAlgorithm, void*, void> callback,
            void *callbackContext);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509StoreEnumerateTrustedCertificates")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool X509StoreEnumerateTrustedCertificates(
            byte systemOnly,
            delegate* unmanaged<void*, void*, void> callback,
            void *callbackContext);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509StoreOpenDefault")]
        internal static extern unsafe SafeX509StoreHandle X509StoreOpenDefault();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509StoreRemoveCertificate")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool X509StoreRemoveCertificate(
            SafeX509StoreHandle store,
            SafeX509Handle cert,
            string hashString);
    }
}

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class SafeX509StoreHandle : Interop.JObjectLifetime.SafeJObjectHandle
    {
        public SafeX509StoreHandle()
        {
        }
    }
}
