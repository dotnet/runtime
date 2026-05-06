// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace System.Net.Http.Functional.Tests
{
    public static class AndroidKeyStoreHelper
    {
        public static (X509Store, string) AddCertificate(X509Certificate2 cert)
        {
            // Add the certificate to the Android keystore via X509Store
            // the alias is the certificate hash string (sha256)
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);
            string alias = cert.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256);
            return (store, alias);
        }

        public static X509Certificate2 GetCertificateViaAlias(X509Store store, string alias)
        {
            IntPtr privateKeyEntry = Interop.AndroidCrypto.X509StoreGetPrivateKeyEntry(store.StoreHandle, alias);
            return new X509Certificate2(privateKeyEntry);
        }

        public static bool DeleteAlias(X509Store store, string alias)
        {
            return Interop.AndroidCrypto.X509StoreDeleteEntry(store.StoreHandle, alias);
        }
    }
}
