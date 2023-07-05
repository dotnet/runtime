// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static partial class X509StoreMutableTests
    {
        public static bool PermissionsAllowStoreWrite { get; } = true;

        [Theory]
        [InlineData(nameof(TestData.RsaCertificate), TestData.RsaCertificate, TestData.RsaPkcs8Key)]
        [InlineData(nameof(TestData.EcDhCertificate), TestData.EcDhCertificate, TestData.EcDhPkcs8Key)]
        [InlineData(nameof(TestData.ECDsaCertificate), TestData.ECDsaCertificate, TestData.ECDsaPkcs8Key)]
        public static void AddRemove_CertWithPrivateKey(string testCase, string certPem, string keyPem)
        {
            _ = testCase;
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            using (var cert = X509Certificate2.CreateFromPem(certPem, keyPem))
            {
                store.Open(OpenFlags.ReadWrite);

                // Make sure cert is not already in the store
                store.Remove(cert);
                Assert.False(IsCertInStore(cert, store), "Certificate should not be found on pre-condition");

                // Add
                store.Add(cert);
                Assert.True(IsCertInStore(cert, store), "Certificate should be found after add");
                Assert.True(StoreHasPrivateKey(store, cert), "Certificate in store should have a private key");

                // Remove
                store.Remove(cert);
                Assert.False(IsCertInStore(cert, store), "Certificate should not be found after remove");
            }
        }
    }
}
