// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.ExtensionsTests
{
    public static class AuthorityInformationAccessTests
    {
        [Fact]
        public static void DefaultConstructor()
        {
            X509AuthorityInformationAccessExtension aia = new();
            Assert.NotNull(aia.Oid);
            Assert.Equal("1.3.6.1.5.5.7.1.1", aia.Oid.Value);
            Assert.Empty(aia.EnumerateCAIssuersUris());
            Assert.Empty(aia.EnumerateOcspUris());
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support asymmetric cryptography")]
        public static void NoProtocolFilter()
        {
            // While most of the callers of the various EnumerateUri methods will be interested in only the
            // http(non-S) values, this test is just a quick sanity check that the built-in methods don't
            // apply that filter internally.
            using (X509Certificate2 cert = new X509Certificate2(TestFiles.MicrosoftRootCertFile))
            {
                X509AuthorityInformationAccessExtension aia =
                    cert.Extensions.OfType<X509AuthorityInformationAccessExtension>().Single();

                string[] caIssuersValues =
                {
                    "ldap:///CN=Microsoft%20Corporate%20Root%20Authority,CN=AIA,CN=Public%20Key%20Services,CN=Services,CN=Configuration,DC=Corp,DC=Microsoft,DC=Com?cACertificate?base?objectclass=certificationAuthority",
                    "ldap://corp.microsoft.com/CN=Microsoft%20Corporate%20Root%20Authority,CN=AIA,CN=Public%20Key%20Services,CN=Services,CN=Configuration,DC=Corp,DC=Microsoft,DC=Com?cACertificate?base?objectclass=certificationAuthority",
                    "http://www.microsoft.com/pki/mscorp/mscra1.crt",
                };

                const string CertificateAuthorityIssuers = "1.3.6.1.5.5.7.48.2";

                Assert.Equal(caIssuersValues, aia.EnumerateCAIssuersUris());
                Assert.Equal(caIssuersValues, aia.EnumerateUris(CertificateAuthorityIssuers));
                Assert.Equal(caIssuersValues, aia.EnumerateUris(new Oid(CertificateAuthorityIssuers)));
            }
        }

        [Fact]
        public static void DecodeComplexValue()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            static void WriteAccessMethod(AsnWriter writer, string oidValue, int choice, string value)
            {
                using (writer.PushSequence())
                {
                    writer.WriteObjectIdentifier(oidValue);
                    writer.WriteCharacterString(UniversalTagNumber.IA5String, value, new Asn1Tag(TagClass.ContextSpecific, choice));
                }
            }

            const int UriChoice = 6;
            const int Rfc822Choice = 1;
            const string OcspMethod = "1.3.6.1.5.5.7.48.1";
            const string CaIssuersMethod = "1.3.6.1.5.5.7.48.2";

            // SEQUENCE OF
            using (writer.PushSequence())
            {
                WriteAccessMethod(writer, "0.0", UriChoice, "hello");
                WriteAccessMethod(writer, "0.1", UriChoice, "world");
                WriteAccessMethod(writer, "0.1", Rfc822Choice, "terra?");
                WriteAccessMethod(writer, "0.2", Rfc822Choice, "firma");
                WriteAccessMethod(writer, OcspMethod, UriChoice, "ldap:////CN=Greetings");
                WriteAccessMethod(writer, OcspMethod, Rfc822Choice, "ocsp@some.example");
                WriteAccessMethod(writer, CaIssuersMethod, Rfc822Choice, "potato");
                WriteAccessMethod(writer, OcspMethod, UriChoice, "https://ocsp.some.example");
                WriteAccessMethod(writer, CaIssuersMethod, UriChoice, "potato");
                WriteAccessMethod(writer, CaIssuersMethod, UriChoice, "salad");
                WriteAccessMethod(writer, OcspMethod, UriChoice, "https://ocsp.some.example/");
                WriteAccessMethod(writer, OcspMethod, UriChoice, "https://ocsp.some.example");
            }

            X509AuthorityInformationAccessExtension aia = new(writer.Encode(), critical: true);
            Assert.NotNull(aia.Oid);
            Assert.Equal("1.3.6.1.5.5.7.1.1", aia.Oid.Value);
            Assert.True(aia.Critical);

            // Just top-down for OcspMethod+UriChoice
            string[] expectedOcsp =
            {
                "ldap:////CN=Greetings",
                "https://ocsp.some.example",
                "https://ocsp.some.example/",
                "https://ocsp.some.example",
            };

            string[] expectedCaIssuers =
            {
                "potato",
                "salad",
            };

            Assert.Equal(expectedOcsp, aia.EnumerateOcspUris());
            Assert.Equal(expectedCaIssuers, aia.EnumerateCAIssuersUris());
            Assert.Equal(new[] { "hello" }, aia.EnumerateUris("0.0"));
            Assert.Equal(new[] { "world" }, aia.EnumerateUris("0.1"));
            Assert.Equal(Array.Empty<string>(), aia.EnumerateUris("0.2"));
            Assert.Equal(Array.Empty<string>(), aia.EnumerateUris("0.3"));
            Assert.Equal(Array.Empty<string>(), aia.EnumerateUris("gibberish"));
            Assert.Equal(expectedOcsp, aia.EnumerateUris(OcspMethod));
            Assert.Equal(expectedCaIssuers, aia.EnumerateUris(CaIssuersMethod));
        }

        [Fact]
        public static void BuildEmpty()
        {
            static void AssertProperException(Action action)
            {
                ArgumentException ex = Assert.Throws<ArgumentException>(action);
                Assert.Null(ex.ParamName);
            }
            AssertProperException(
                () => new X509AuthorityInformationAccessExtension(
                    Enumerable.Empty<string>(),
                    Enumerable.Empty<string>()));

            AssertProperException(
                () => new X509AuthorityInformationAccessExtension(
                    Enumerable.Empty<string>(),
                    null));

            AssertProperException(
                () => new X509AuthorityInformationAccessExtension(
                    null,
                    Enumerable.Empty<string>()));

            AssertProperException(
                () => new X509AuthorityInformationAccessExtension(
                    null,
                    null));
        }

        [Fact]
        public static void BuildOcspOnly()
        {
            X509AuthorityInformationAccessExtension aia = new X509AuthorityInformationAccessExtension(
                new[]
                {
                    "ocsp1",
                    "ocsp2",
                },
                Enumerable.Empty<string>());

            Assert.False(aia.Critical, "aia.Critical");
            Assert.NotNull(aia.Oid);
            Assert.Equal("1.3.6.1.5.5.7.1.1", aia.Oid.Value);
            Assert.NotNull(aia.RawData);

            Assert.Equal(
                "3026301106082B0601050507300186056F63737031301106082B060105050730" +
                "0186056F63737032",
                aia.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void BuildCAIssuersOnly()
        {
            X509AuthorityInformationAccessExtension aia = new X509AuthorityInformationAccessExtension(
                Enumerable.Empty<string>(),
                new[]
                {
                    "ca1",
                    "ca2",
                    "ca3",
                },
                true);

            Assert.True(aia.Critical, "aia.Critical");
            Assert.NotNull(aia.Oid);
            Assert.Equal("1.3.6.1.5.5.7.1.1", aia.Oid.Value);
            Assert.NotNull(aia.RawData);

            Assert.Equal(
                "3033300F06082B060105050730028603636131300F06082B0601050507300286" +
                "03636132300F06082B060105050730028603636133",
                aia.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void BuildBothMethods()
        {
            X509AuthorityInformationAccessExtension aia = new X509AuthorityInformationAccessExtension(
                new[]
                {
                    "A",
                    "B",
                    "C",
                },
                new[]
                {
                    "D",
                },
                false);

            Assert.False(aia.Critical, "aia.Critical");
            Assert.NotNull(aia.Oid);
            Assert.Equal("1.3.6.1.5.5.7.1.1", aia.Oid.Value);
            Assert.NotNull(aia.RawData);

            Assert.Equal(
                "303C300D06082B06010505073001860141300D06082B06010505073001860142" +
                "300D06082B06010505073001860143300D06082B06010505073002860144",
                aia.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void BuildInvalidOcsp()
        {
            const string BadEntry = "\u212C is not a B";

            CryptographicException ex = Assert.Throws<CryptographicException>(
                () => new X509AuthorityInformationAccessExtension(
                    new[] { "A", BadEntry, "C" },
                    new[] { "D" }));
        }

        [Fact]
        public static void BuildInvalidCAIssuer()
        {
            const string BadEntry = "\u212B is not an A";

            CryptographicException ex = Assert.Throws<CryptographicException>(
                () => new X509AuthorityInformationAccessExtension(
                    new[] { "D" },
                    new[] { "C", "B", BadEntry }));
        }

        [Fact]
        public static void BuildNullOcspValue()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => new X509AuthorityInformationAccessExtension(
                    new[] { "A", null, "C" },
                    new[] { "D" }));

            Assert.Null(ex.ParamName);
        }

        [Fact]
        public static void BuildNullCAIssuerValue()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => new X509AuthorityInformationAccessExtension(
                    new[] { "D" },
                    new[] { "C", "B", null }));

            Assert.Null(ex.ParamName);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates.")]
        public static void EnumerateNull()
        {
            using (X509Certificate2 cert = new X509Certificate2(TestFiles.MicrosoftRootCertFile))
            {
                X509AuthorityInformationAccessExtension aia =
                    cert.Extensions.OfType<X509AuthorityInformationAccessExtension>().Single();

                Assert.Throws<ArgumentNullException>("accessMethodOid", () => aia.EnumerateUris((string)null));
                Assert.Throws<ArgumentNullException>("accessMethodOid", () => aia.EnumerateUris((Oid)null));
                Assert.Throws<ArgumentNullException>("accessMethodOid.Value", () => aia.EnumerateUris(new Oid(null, "potato")));
                Assert.Throws<ArgumentException>("accessMethodOid.Value", () => aia.EnumerateUris(new Oid("", "potato")));
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates.")]
        public static void EnumerateOidUsesValue()
        {
            using (X509Certificate2 cert = new X509Certificate2(TestFiles.MicrosoftRootCertFile))
            {
                X509AuthorityInformationAccessExtension aia =
                    cert.Extensions.OfType<X509AuthorityInformationAccessExtension>().Single();

                string[] caIssuersValues =
                {
                    "ldap:///CN=Microsoft%20Corporate%20Root%20Authority,CN=AIA,CN=Public%20Key%20Services,CN=Services,CN=Configuration,DC=Corp,DC=Microsoft,DC=Com?cACertificate?base?objectclass=certificationAuthority",
                    "ldap://corp.microsoft.com/CN=Microsoft%20Corporate%20Root%20Authority,CN=AIA,CN=Public%20Key%20Services,CN=Services,CN=Configuration,DC=Corp,DC=Microsoft,DC=Com?cACertificate?base?objectclass=certificationAuthority",
                    "http://www.microsoft.com/pki/mscorp/mscra1.crt",
                };

                const string OcspEndpoint = "1.3.6.1.5.5.7.48.1";
                const string CertificateAuthorityIssuers = "1.3.6.1.5.5.7.48.2";

                Assert.Empty(aia.EnumerateUris(new Oid(value: OcspEndpoint, friendlyName: CertificateAuthorityIssuers)));
                Assert.Equal(
                    caIssuersValues,
                    aia.EnumerateUris(new Oid(value: CertificateAuthorityIssuers, friendlyName: OcspEndpoint)));
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates.")]
        public static void IndependentEnumeration()
        {
            X509AuthorityInformationAccessExtension aia;

            string[] caIssuersValues =
            {
                "ldap:///CN=Microsoft%20Corporate%20Root%20Authority,CN=AIA,CN=Public%20Key%20Services,CN=Services,CN=Configuration,DC=Corp,DC=Microsoft,DC=Com?cACertificate?base?objectclass=certificationAuthority",
                "ldap://corp.microsoft.com/CN=Microsoft%20Corporate%20Root%20Authority,CN=AIA,CN=Public%20Key%20Services,CN=Services,CN=Configuration,DC=Corp,DC=Microsoft,DC=Com?cACertificate?base?objectclass=certificationAuthority",
                "http://www.microsoft.com/pki/mscorp/mscra1.crt",
            };

            using (X509Certificate2 cert = new X509Certificate2(TestFiles.MicrosoftRootCertFile))
            {
                aia = cert.Extensions.OfType<X509AuthorityInformationAccessExtension>().Single();
            }

            IEnumerable<string> able1 = aia.EnumerateCAIssuersUris();
            IEnumerator<string> ator1 = able1.GetEnumerator();

            Assert.True(ator1.MoveNext());
            Assert.Equal(caIssuersValues[0], ator1.Current);

            IEnumerator<string> ator2 = able1.GetEnumerator();

            Assert.True(ator1.MoveNext());
            Assert.Equal(caIssuersValues[1], ator1.Current);
            Assert.True(ator2.MoveNext());
            Assert.Equal(caIssuersValues[0], ator2.Current);

            IEnumerable<string> able2 = aia.EnumerateCAIssuersUris();
            IEnumerator<string> ator3 = able2.GetEnumerator();

            Assert.True(ator1.MoveNext());
            Assert.Equal(caIssuersValues[2], ator1.Current);
            Assert.True(ator2.MoveNext());
            Assert.Equal(caIssuersValues[1], ator2.Current);
            Assert.True(ator3.MoveNext());
            Assert.Equal(caIssuersValues[0], ator3.Current);

            Assert.False(ator1.MoveNext());
            Assert.True(ator2.MoveNext());
            Assert.Equal(caIssuersValues[2], ator2.Current);
            Assert.True(ator3.MoveNext());
            Assert.Equal(caIssuersValues[1], ator3.Current);

            Assert.False(ator1.MoveNext());
            Assert.False(ator2.MoveNext());
            Assert.True(ator3.MoveNext());
            Assert.Equal(caIssuersValues[2], ator3.Current);

            Assert.False(ator1.MoveNext());
            Assert.False(ator2.MoveNext());
            Assert.False(ator3.MoveNext());
        }

        [Fact]
        public static void DecodeInvalid_Boolean()
        {
            DecodeInvalid("0101FF");
        }

        [Fact]
        public static void DecodeInvalid_SequenceOfBoolean()
        {
            DecodeInvalid("03060101000101FF");
        }

        [Fact]
        public static void DecodeInvalid_NonIA5Uri()
        {
            // SEQUENCE OF
            //   SEQUENCE(OCSP, "A")
            //   SEQUENCE(OCSP, "B")
            //   SEQUENCE(OCSP, 0x80)
            //   SEQUENCE(CAIssuers, "D")

            const string BadHex =
                "303C300D06082B06010505073001860141300D06082B06010505073001860142" +
                "300D06082B06010505073001860180300D06082B06010505073002860144";

            DecodeInvalid(BadHex);
        }

        [Fact]
        public static void DecodeInvalid_TwoValues()
        {
            // SEQUENCE OF (empty)
            // SEQUENCE OF (empty)
            DecodeInvalid("30003000");
        }

        [Fact]
        public static void DecodeInvalid_TooManyFields()
        {
            // SEQUENCE OF
            //   SEQUENCE(OCSP, "A", FALSE)

            DecodeInvalid("301006082B06010505073002860144010100");
        }

        [Fact]
        public static void DecodeInvalid_TooFew()
        {
            // SEQUENCE OF
            //   SEQUENCE(OCSP)

            DecodeInvalid("300A06082B06010505073002");
        }

        private static void DecodeInvalid(string invalidEncodingHex)
        {
            byte[] invalidEncoding = Convert.FromHexString(invalidEncodingHex);

            Assert.Throws<CryptographicException>(
                () => new X509AuthorityInformationAccessExtension(invalidEncoding));

            Assert.Throws<CryptographicException>(
                () => new X509AuthorityInformationAccessExtension(new ReadOnlySpan<byte>(invalidEncoding)));

            X509Extension unverified = new X509Extension(
                "1.3.6.1.5.5.7.1.1",
                invalidEncoding,
                critical: false);

            X509AuthorityInformationAccessExtension aia = new X509AuthorityInformationAccessExtension();
            aia.CopyFrom(unverified);

            Assert.Throws<CryptographicException>(() => aia.EnumerateCAIssuersUris());
            Assert.Throws<CryptographicException>(() => aia.EnumerateOcspUris());
            Assert.Throws<CryptographicException>(() => aia.EnumerateUris("0.0"));
        }
    }
}
