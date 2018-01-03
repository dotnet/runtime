// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public class CertTests
    {
        private const string PrivateKeySectionHeader = "[Private Key]";
        private const string PublicKeySectionHeader = "[Public Key]";

        private readonly ITestOutputHelper _log;

        public CertTests(ITestOutputHelper output)
        {
            _log = output;
        }

        [Fact]
        public static void X509CertTest()
        {
            string certSubject = @"CN=Microsoft Corporate Root Authority, OU=ITG, O=Microsoft, L=Redmond, S=WA, C=US, E=pkit@microsoft.com";

            using (X509Certificate cert = new X509Certificate(Path.Combine("TestData", "microsoft.cer")))
            {
                Assert.Equal(certSubject, cert.Subject);
                Assert.Equal(certSubject, cert.Issuer);

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
        public static void X509Cert2Test()
        {
            string certName = @"E=admin@digsigtrust.com, CN=ABA.ECOM Root CA, O=""ABA.ECOM, INC."", L=Washington, S=DC, C=US";

            DateTime notBefore = new DateTime(1999, 7, 12, 17, 33, 53, DateTimeKind.Utc).ToLocalTime();
            DateTime notAfter = new DateTime(2009, 7, 9, 17, 33, 53, DateTimeKind.Utc).ToLocalTime();

            using (X509Certificate2 cert2 = new X509Certificate2(Path.Combine("TestData", "test.cer")))
            {
                Assert.Equal(certName, cert2.IssuerName.Name);
                Assert.Equal(certName, cert2.SubjectName.Name);

                Assert.Equal("ABA.ECOM Root CA", cert2.GetNameInfo(X509NameType.DnsName, true));

                PublicKey pubKey = cert2.PublicKey;
                Assert.Equal("RSA", pubKey.Oid.FriendlyName);

                Assert.Equal(notAfter, cert2.NotAfter);
                Assert.Equal(notBefore, cert2.NotBefore);

                Assert.Equal(notAfter.ToString(), cert2.GetExpirationDateString());
                Assert.Equal(notBefore.ToString(), cert2.GetEffectiveDateString());

                Assert.Equal("00D01E4090000046520000000100000004", cert2.SerialNumber);
                Assert.Equal("1.2.840.113549.1.1.5", cert2.SignatureAlgorithm.Value);
                Assert.Equal("7A74410FB0CD5C972A364B71BF031D88A6510E9E", cert2.Thumbprint);
                Assert.Equal(3, cert2.Version);
            }
        }

        [Fact]
        [OuterLoop("May require using the network, to download CRLs and intermediates")]
        public void TestVerify()
        {
            bool success;

            using (var microsoftDotCom = new X509Certificate2(TestData.MicrosoftDotComSslCertBytes))
            {
                // Fails because expired (NotAfter = 10/16/2016)
                Assert.False(microsoftDotCom.Verify(), "MicrosoftDotComSslCertBytes");
            }

            using (var microsoftDotComIssuer = new X509Certificate2(TestData.MicrosoftDotComIssuerBytes))
            {
                // NotAfter=10/31/2023
                success = microsoftDotComIssuer.Verify();
                if (!success)
                {
                    LogVerifyErrors(microsoftDotComIssuer, "MicrosoftDotComIssuerBytes");
                }
                Assert.True(success, "MicrosoftDotComIssuerBytes");
            }

            // High Sierra fails to build a chain for a self-signed certificate with revocation enabled.
            // https://github.com/dotnet/corefx/issues/21875
            if (!PlatformDetection.IsMacOsHighSierraOrHigher)
            {
                using (var microsoftDotComRoot = new X509Certificate2(TestData.MicrosoftDotComRootBytes))
                {
                    // NotAfter=7/17/2036
                    success = microsoftDotComRoot.Verify();
                    if (!success)
                    {
                        LogVerifyErrors(microsoftDotComRoot, "MicrosoftDotComRootBytes");
                    }
                    Assert.True(success, "MicrosoftDotComRootBytes");
                }
            }
        }

        private void LogVerifyErrors(X509Certificate2 cert, string testName)
        {
            // Emulate cert.Verify() implementation in order to capture and log errors.
            try
            {
                using (var chain = new X509Chain())
                {
                    if (!chain.Build(cert))
                    {
                        foreach (X509ChainStatus chainStatus in chain.ChainStatus)
                        {
                            _log.WriteLine(string.Format($"X509Certificate2.Verify error: {testName}, {chainStatus.Status}, {chainStatus.StatusInformation}"));
                        }
                    }
                    else
                    {
                        _log.WriteLine(string.Format($"X509Certificate2.Verify expected error; received none: {testName}"));
                    }
                }
            }
            catch (Exception e)
            {
                _log.WriteLine($"X509Certificate2.Verify exception: {testName}, {e}");
            }
        }

        [Fact]
        public static void X509CertEmptyToString()
        {
            using (var c = new X509Certificate())
            {
                string expectedResult = "System.Security.Cryptography.X509Certificates.X509Certificate";
                Assert.Equal(expectedResult, c.ToString());
                Assert.Equal(expectedResult, c.ToString(false));
                Assert.Equal(expectedResult, c.ToString(true));
            }
        }

        [Fact]
        public static void X509Cert2EmptyToString()
        {
            using (var c2 = new X509Certificate2())
            {
                string expectedResult = "System.Security.Cryptography.X509Certificates.X509Certificate2";
                Assert.Equal(expectedResult, c2.ToString());
                Assert.Equal(expectedResult, c2.ToString(false));
                Assert.Equal(expectedResult, c2.ToString(true));
            }
        }

        [Fact]
        public static void X509Cert2ToStringVerbose()
        {
            using (X509Store store = new X509Store("My", StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);

                foreach (X509Certificate2 c in store.Certificates)
                {
                    Assert.False(string.IsNullOrWhiteSpace(c.ToString(true)));
                    c.Dispose();
                }
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void X509Certificate2ToStringVerbose_WithPrivateKey(X509KeyStorageFlags keyStorageFlags)
        {
            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, keyStorageFlags))
            {
                string certToString = cert.ToString(true);
                Assert.Contains(PrivateKeySectionHeader, certToString);
                Assert.Contains(PublicKeySectionHeader, certToString);
            }
        }

        [Fact]
        public static void X509Certificate2ToStringVerbose_NoPrivateKey()
        {
            using (var cert = new X509Certificate2(TestData.MsCertificatePemBytes))
            {
                string certToString = cert.ToString(true);
                Assert.DoesNotContain(PrivateKeySectionHeader, certToString);
                Assert.Contains(PublicKeySectionHeader, certToString);
            }
        }

        [Fact]
        public static void X509Cert2CreateFromEmptyPfx()
        {
            Assert.ThrowsAny<CryptographicException>(() => new X509Certificate2(TestData.EmptyPfx));
        }

        [Fact]
        public static void X509Cert2CreateFromPfxFile()
        {
            using (X509Certificate2 cert2 = new X509Certificate2(Path.Combine("TestData", "DummyTcpServer.pfx")))
            {
                // OID=RSA Encryption
                Assert.Equal("1.2.840.113549.1.1.1", cert2.GetKeyAlgorithm());
            }
        }

        [Fact]
        public static void X509Cert2CreateFromPfxWithPassword()
        {
            using (X509Certificate2 cert2 = new X509Certificate2(Path.Combine("TestData", "test.pfx"), "test"))
            {
                // OID=RSA Encryption
                Assert.Equal("1.2.840.113549.1.1.1", cert2.GetKeyAlgorithm());
            }
        }

        [Fact]
        public static void X509Certificate2FromPkcs7DerFile()
        {
            Assert.ThrowsAny<CryptographicException>(() => new X509Certificate2(Path.Combine("TestData", "singlecert.p7b")));
        }

        [Fact]
        public static void X509Certificate2FromPkcs7PemFile()
        {
            Assert.ThrowsAny<CryptographicException>(() => new X509Certificate2(Path.Combine("TestData", "singlecert.p7c")));
        }

        [Fact]
        public static void X509Certificate2FromPkcs7DerBlob()
        {
            Assert.ThrowsAny<CryptographicException>(() => new X509Certificate2(TestData.Pkcs7SingleDerBytes));
        }

        [Fact]
        public static void X509Certificate2FromPkcs7PemBlob()
        {
            Assert.ThrowsAny<CryptographicException>(() => new X509Certificate2(TestData.Pkcs7SinglePemBytes));
        }

        [Fact]
        public static void UseAfterDispose()
        {
            using (X509Certificate2 c = new X509Certificate2(TestData.MsCertificate))
            {
                IntPtr h = c.Handle;

                // Do a couple of things that would only be true on a valid certificate, as a precondition.
                Assert.NotEqual(IntPtr.Zero, h);
                byte[] actualThumbprint = c.GetCertHash();

                c.Dispose();

                // For compat reasons, Dispose() acts like the now-defunct Reset() method rather than
                // causing ObjectDisposedExceptions.
                h = c.Handle;
                Assert.Equal(IntPtr.Zero, h);

                // State held on X509Certificate
                Assert.ThrowsAny<CryptographicException>(() => c.GetCertHash());
                Assert.ThrowsAny<CryptographicException>(() => c.GetCertHash(HashAlgorithmName.SHA256));
                Assert.ThrowsAny<CryptographicException>(() => c.GetCertHashString());
                Assert.ThrowsAny<CryptographicException>(() => c.GetCertHashString(HashAlgorithmName.SHA256));
                Assert.ThrowsAny<CryptographicException>(() => c.GetKeyAlgorithm());
                Assert.ThrowsAny<CryptographicException>(() => c.GetKeyAlgorithmParameters());
                Assert.ThrowsAny<CryptographicException>(() => c.GetKeyAlgorithmParametersString());
                Assert.ThrowsAny<CryptographicException>(() => c.GetPublicKey());
                Assert.ThrowsAny<CryptographicException>(() => c.GetSerialNumber());
                Assert.ThrowsAny<CryptographicException>(() => c.Issuer);
                Assert.ThrowsAny<CryptographicException>(() => c.Subject);
                Assert.ThrowsAny<CryptographicException>(() => c.NotBefore);
                Assert.ThrowsAny<CryptographicException>(() => c.NotAfter);

                Assert.ThrowsAny<CryptographicException>(
                    () => c.TryGetCertHash(HashAlgorithmName.SHA256, Array.Empty<byte>(), out _));

                // State held on X509Certificate2
                Assert.ThrowsAny<CryptographicException>(() => c.RawData);
                Assert.ThrowsAny<CryptographicException>(() => c.SignatureAlgorithm);
                Assert.ThrowsAny<CryptographicException>(() => c.Version);
                Assert.ThrowsAny<CryptographicException>(() => c.SubjectName);
                Assert.ThrowsAny<CryptographicException>(() => c.IssuerName);
                Assert.ThrowsAny<CryptographicException>(() => c.PublicKey);
                Assert.ThrowsAny<CryptographicException>(() => c.Extensions);
                Assert.ThrowsAny<CryptographicException>(() => c.PrivateKey);
            }
        }

        [Fact]
        public static void ExportPublicKeyAsPkcs12()
        {
            using (X509Certificate2 publicOnly = new X509Certificate2(TestData.MsCertificate))
            {
                // Pre-condition: There's no private key
                Assert.False(publicOnly.HasPrivateKey);

                // macOS 10.12 (Sierra) fails to create a PKCS#12 blob if it has no private keys within it.
                bool shouldThrow = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

                try
                {
                    byte[] pkcs12Bytes = publicOnly.Export(X509ContentType.Pkcs12);

                    Assert.False(shouldThrow, "PKCS#12 export of a public-only certificate threw as expected");

                    // Read it back as a collection, there should be only one cert, and it should
                    // be equal to the one we started with.
                    using (ImportedCollection ic = Cert.Import(pkcs12Bytes))
                    {
                        X509Certificate2Collection fromPfx = ic.Collection;

                        Assert.Equal(1, fromPfx.Count);
                        Assert.Equal(publicOnly, fromPfx[0]);
                    }
                }
                catch (CryptographicException)
                {
                    if (!shouldThrow)
                    {
                        throw;
                    }
                }
            }
        }

        public static IEnumerable<object> StorageFlags => CollectionImportTests.StorageFlags;
    }
}
