// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class CrlDistPointBuilderTests
    {
        [Fact]
        public static void NullEnumerable()
        {
            Assert.Throws<ArgumentNullException>(
                "uris",
                () => CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(null));
        }

        [Fact]
        public static void NullUriInEnumerable()
        {
            Assert.Throws<ArgumentException>(
                "uris",
                () => CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(
                    new[]
                    {
                        "http://cert.example/ca1.crl",
                        null,
                        "http://cdn.cert.example/ca1.crl",
                    }));
        }

        [Fact]
        public static void BuildEmpty()
        {
            Assert.Throws<ArgumentException>(
                "uris",
                () => CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(
                    System.Linq.Enumerable.Empty<string>()));
        }

        [Fact]
        public static void BuildOneEntry()
        {
            X509Extension ext = CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(
                new[]
                {
                    "http://crl.microsoft.com/pki/crl/products/MicCodSigPCA_08-31-2010.crl",
                });

            Assert.False(ext.Critical, "ext.Critical");
            Assert.Equal("2.5.29.31", ext.Oid.Value);

            byte[] expected = (
                "304d304ba049a0478645687474703a2f2f63726c2e6d6963726f736f" +
                "66742e636f6d2f706b692f63726c2f70726f64756374732f4d696343" +
                "6f645369675043415f30382d33312d323031302e63726c").HexToByteArray();

            AssertExtensions.SequenceEqual(expected, ext.RawData);
        }

        [Fact]
        public static void BuildTwoEntries()
        {
            // Recreate the encoding of the CDP extension from https://crt.sh/?id=3777044
            // (the original wasn't marked as critical, but that doesn't affect the RawData value)
            X509Extension ext = CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(
                new[]
                {
                    "http://crl3.digicert.com/sha2-ev-server-g1.crl",
                    "http://crl4.digicert.com/sha2-ev-server-g1.crl",
                },
                critical: true);

            Assert.True(ext.Critical, "ext.Critical");
            Assert.Equal("2.5.29.31", ext.Oid.Value);

            byte[] expected = (
                "306C3034A032A030862E687474703A2F2F63726C332E64696769636572742E63" +
                "6F6D2F736861322D65762D7365727665722D67312E63726C3034A032A030862E" +
                "687474703A2F2F63726C342E64696769636572742E636F6D2F736861322D6576" +
                "2D7365727665722D67312E63726C").HexToByteArray();

            AssertExtensions.SequenceEqual(expected, ext.RawData);
        }

        [Fact]
        public static void UriNotValidated()
        {
            X509Extension ext = CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(
                new[]
                {
                    "!!!!",
                });

            Assert.Equal("300C300AA008A006860421212121", ext.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void OnlyAscii7Permitted()
        {
            Assert.Throws<CryptographicException>(
                () => CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(
                    new[]
                    {
                        // http://[nihongo].example/ca4.crl
                        "http://\u65E5\u672C\u8A8E.example/ca4.crl",
                    }));
        }
    }
}
