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
        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509StoreAddCertificate", StringMarshalling = StringMarshalling.Utf8)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool X509StoreAddCertificate(
            SafeX509StoreHandle store,
            SafeX509Handle cert,
            string hashString);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509StoreAddCertificateWithPrivateKey", StringMarshalling = StringMarshalling.Utf8)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool X509StoreAddCertificateWithPrivateKey(
            SafeX509StoreHandle store,
            SafeX509Handle cert,
            SafeKeyHandle key,
            PAL_KeyAlgorithm algorithm,
            string hashString);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509StoreContainsCertificate", StringMarshalling = StringMarshalling.Utf8)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool X509StoreContainsCertificate(
            SafeX509StoreHandle store,
            SafeX509Handle cert,
            string hashString);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509StoreEnumerateCertificates")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool X509StoreEnumerateCertificates(
            SafeX509StoreHandle storeHandle,
            delegate* unmanaged<void*, void*, Interop.AndroidCrypto.PAL_KeyAlgorithm, void*, void> callback,
            void *callbackContext);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509StoreEnumerateTrustedCertificates")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool X509StoreEnumerateTrustedCertificates(
            byte systemOnly,
            delegate* unmanaged<void*, void*, void> callback,
            void *callbackContext);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509StoreOpenDefault")]
        internal static unsafe partial SafeX509StoreHandle X509StoreOpenDefault();

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X509StoreRemoveCertificate", StringMarshalling = StringMarshalling.Utf8)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool X509StoreRemoveCertificate(
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
