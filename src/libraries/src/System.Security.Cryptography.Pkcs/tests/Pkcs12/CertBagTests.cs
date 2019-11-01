// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests.Pkcs12
{
    public static class CertBagTests
    {
        private static readonly ReadOnlyMemory<byte> s_derNull = new byte[] { 0x05, 0x00 };
        private static readonly Oid s_x509TypeOid = new Oid("1.2.840.113549.1.9.22.1");

        [Fact]
        public static void CertificateTypeRequired()
        {
            AssertExtensions.Throws<ArgumentNullException>(
                "certificateType",
                () => new Pkcs12CertBag(null, ReadOnlyMemory<byte>.Empty));
        }

        [Fact]
        public static void InvalidCertificateTypeVerifiedInCtor()
        {
            Assert.ThrowsAny<CryptographicException>(
                () => new Pkcs12CertBag(new Oid(null, null), s_derNull));
        }

        [Fact]
        public static void DataNotValidatedInCtor()
        {
            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.GetCertificate())
            {
                var certBag = new Pkcs12CertBag(s_x509TypeOid, cert.RawData);

                Assert.True(certBag.IsX509Certificate, "certBag.IsX509Certificate");
                Assert.ThrowsAny<CryptographicException>(() => certBag.GetCertificate());
            }
        }

        [Fact]
        public static void OidCtorPreservesFriendlyName()
        {
            Oid oid = new Oid(Oids.Pkcs7Data, "Hello");
            var certBag = new Pkcs12CertBag(oid, s_derNull);
            Oid firstCall = certBag.GetCertificateType();
            Oid secondCall = certBag.GetCertificateType();

            Assert.NotSame(oid, firstCall);
            Assert.NotSame(oid, secondCall);
            Assert.NotSame(firstCall, secondCall);
            Assert.Equal(oid.Value, firstCall.Value);
            Assert.Equal(oid.Value, secondCall.Value);
            Assert.Equal("Hello", firstCall.FriendlyName);
            Assert.Equal("Hello", secondCall.FriendlyName);
        }

        [Theory]
        [InlineData(Oids.Pkcs7Data, false)]
        [InlineData("1.2.840.113549.1.9.22.1", true)]
        [InlineData("1.2.840.113549.1.9.22.2", false)]
        [InlineData("1.2.840.113549.1.9.22.11", false)]
        public static void VerifyIsX509(string oidValue, bool expectedValue)
        {
            var certBag = new Pkcs12CertBag(new Oid(oidValue), s_derNull);

            if (expectedValue)
            {
                Assert.True(certBag.IsX509Certificate, "certBag.IsX509Certificate");
                Assert.ThrowsAny<CryptographicException>(() => certBag.GetCertificate());
            }
            else
            {
                Assert.False(certBag.IsX509Certificate, "certBag.IsX509Certificate");
                Assert.Throws<InvalidOperationException>(() => certBag.GetCertificate());
            }
        }

        [Fact]
        public static void CertificateReadsSuccessfully()
        {
            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.GetCertificate())
            {
                Pkcs12SafeContents contents = new Pkcs12SafeContents();
                Pkcs12CertBag certBag = contents.AddCertificate(cert);

                using (X509Certificate2 extracted = certBag.GetCertificate())
                {
                    Assert.True(extracted.RawData.AsSpan().SequenceEqual(cert.RawData));
                }
            }
        }

        [Theory]
        // No data
        [InlineData("", false)]
        // Length exceeds payload
        [InlineData("0401", false)]
        // Two values (aka length undershoots payload)
        [InlineData("0400020100", false)]
        // No length
        [InlineData("04", false)]
        // Legal
        [InlineData("0400", true)]
        // A legal tag-length-value, but not a legal BIT STRING value.
        [InlineData("0300", true)]
        // SEQUENCE (indefinite length) {
        //   Constructed OCTET STRING (indefinite length) {
        //     OCTET STRING (inefficient encoded length 01): 07
        //   }
        // }
        [InlineData("30802480048200017F00000000", true)]
        // Previous example, trailing byte
        [InlineData("30802480048200017F0000000000", false)]
        public static void EncodedCertificateMustBeValidBer(string inputHex, bool expectSuccess)
        {
            byte[] data = inputHex.HexToByteArray();
            Func<Pkcs12CertBag> func = () => new Pkcs12CertBag(s_x509TypeOid, data);

            if (!expectSuccess)
            {
                Assert.ThrowsAny<CryptographicException>(func);
            }
            else
            {
                // Assert.NoThrow
                func();
            }
        }
    }
}
