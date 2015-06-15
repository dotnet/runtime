// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class CertTests
    {
        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void X509CertTest()
        {
            const string CertSubject =
                @"CN=Microsoft Corporate Root Authority, OU=ITG, O=Microsoft, L=Redmond, S=WA, C=US, E=pkit@microsoft.com";

            using (X509Certificate cert = new X509Certificate(Path.Combine("TestData", "microsoft.cer")))
            {
                Assert.Equal(CertSubject, cert.Subject);
                Assert.Equal(CertSubject, cert.Issuer);

                int snlen = cert.GetSerialNumber().Length;
                Assert.Equal(16, snlen);

                byte[] serialNumber = new byte[snlen];
                Buffer.BlockCopy(cert.GetSerialNumber(), 0,
                                     serialNumber, 0,
                                     snlen);

                Assert.Equal(0xF6, serialNumber[0]);
                Assert.Equal(0xB3, serialNumber[snlen / 2]);
                Assert.Equal(0x2A, serialNumber[snlen - 1]);

                Assert.Equal("1.2.840.113549.1.1.1", cert.GetKeyAlgorithm());

                int pklen = cert.GetPublicKey().Length;
                Assert.Equal(270, pklen);

                byte[] publicKey = new byte[pklen];
                Buffer.BlockCopy(cert.GetPublicKey(), 0,
                                     publicKey, 0,
                                     pklen);

                Assert.Equal(0x30, publicKey[0]);
                Assert.Equal(0xB6, publicKey[9]);
                Assert.Equal(1, publicKey[pklen - 1]);
            }
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void X509Cert2Test()
        {
            const string CertName =
                @"E=admin@digsigtrust.com, CN=ABA.ECOM Root CA, O=""ABA.ECOM, INC."", L=Washington, S=DC, C=US";

            DateTime notBefore = new DateTime(1999, 7, 12, 17, 33, 53, DateTimeKind.Utc).ToLocalTime();
            DateTime notAfter = new DateTime(2009, 7, 9, 17, 33, 53, DateTimeKind.Utc).ToLocalTime();

            using (X509Certificate2 cert2 = new X509Certificate2(Path.Combine("TestData", "test.cer")))
            {
                Assert.Equal(CertName, cert2.IssuerName.Name);
                Assert.Equal(CertName, cert2.SubjectName.Name);

                Assert.Equal("ABA.ECOM Root CA", cert2.GetNameInfo(X509NameType.DnsName, true));

                PublicKey pubKey = cert2.PublicKey;
                Assert.True(pubKey.Key is RSACryptoServiceProvider);
                Assert.Equal("RSA", pubKey.Oid.FriendlyName);

                Assert.Equal(notAfter, cert2.NotAfter);
                Assert.Equal(notBefore, cert2.NotBefore);

                Assert.Equal("00D01E4090000046520000000100000004", cert2.SerialNumber);
                Assert.Equal("1.2.840.113549.1.1.5", cert2.SignatureAlgorithm.Value);
                Assert.Equal("7A74410FB0CD5C972A364B71BF031D88A6510E9E", cert2.Thumbprint);
                Assert.Equal(3, cert2.Version);
            }
        }

        /// <summary>
        /// This test is for excerising X509Store and X509Chain code without actually installing any certificate 
        /// </summary>
        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void X509CertStoreChain()
        {
            X509Store store = new X509Store("My", StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            // can't guarantee there is a certificate in store
            if (store.Certificates.Count > 0)
            {
                X509Chain chain = new X509Chain();
                foreach (X509Certificate2 c in store.Certificates)
                {
                    // can't guarantee success, so no Assert 
                    if (chain.Build(c))
                    {
                        foreach (X509ChainElement k in chain.ChainElements)
                        {
                            Assert.NotNull(k.Certificate.IssuerName.Name);
                        }
                    }
                }
            }
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void X509Cert2ToStringVerbose()
        {
            X509Store store = new X509Store("My", StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            foreach (X509Certificate2 c in store.Certificates)
            {
                Assert.NotNull(c.ToString(true));
            }
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void X509Cert2CreateFromPfxFile()
        {
            using (X509Certificate2 cert2 = new X509Certificate2(Path.Combine("TestData", "DummyTcpServer.pfx")))
            {
                // OID=RSA Encryption
                Assert.Equal("1.2.840.113549.1.1.1", cert2.GetKeyAlgorithm());
            }
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void X509Cert2CreateFromPfxWithPassword()
        {
            using (X509Certificate2 cert2 = new X509Certificate2(Path.Combine("TestData", "test.pfx"), "test"))
            {
                // OID=RSA Encryption
                Assert.Equal("1.2.840.113549.1.1.1", cert2.GetKeyAlgorithm());
            }
        }
    }
}
