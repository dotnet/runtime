// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.CertificateCreation
{
    public static class SubjectAlternativeNameBuilderTests
    {
        private const string SubjectAltNameOid = "2.5.29.17";

        [Fact]
        public static void ArgumentValidation()
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();

            AssertExtensions.Throws<ArgumentOutOfRangeException>("dnsName", () => builder.AddDnsName(null));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("dnsName", () => builder.AddDnsName(string.Empty));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("emailAddress", () => builder.AddEmailAddress(null));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("emailAddress", () => builder.AddEmailAddress(string.Empty));
            AssertExtensions.Throws<ArgumentNullException>("uri", () => builder.AddUri(null));
            AssertExtensions.Throws<ArgumentNullException>("ipAddress", () => builder.AddIpAddress(null));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("upn", () => builder.AddUserPrincipalName(null));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("upn", () => builder.AddUserPrincipalName(string.Empty));
        }

        [Fact]
        public static void SingleValue_DnsName_Ascii()
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();
            builder.AddDnsName("www.example.org");

            X509Extension extension = builder.Build();
            Assert.Equal(SubjectAltNameOid, extension.Oid.Value);

            Assert.Equal("3011820F7777772E6578616D706C652E6F7267", extension.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void SingleValue_DnsName_Unicode()
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();

            // [nihongo].example.org
            builder.AddDnsName("\u65E5\u672C\u8A8E.example.org");

            X509Extension extension = builder.Build();
            Assert.Equal(SubjectAltNameOid, extension.Oid.Value);

            Assert.Equal(
                "301C821A786E2D2D7767763731616F3039652E6578616D706C652E6F7267",
                extension.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void SingleValue_EmailAddress_Ascii()
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();
            builder.AddEmailAddress("user@example.org");

            X509Extension extension = builder.Build();
            Assert.Equal(SubjectAltNameOid, extension.Oid.Value);

            Assert.Equal("3012811075736572406578616D706C652E6F7267", extension.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void SingleValue_EmailAddress_Unicode()
        {
            // There's not a good example of what an IDNA-converted email address
            // looks like, so this isn't easy to verify.  For now let it be restricted to IA5.
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();

            // [nihongo]@[nihongo].example.org
            Assert.Throws<CryptographicException>(
                () => builder.AddEmailAddress("\u65E5\u672C\u8A8E@\u65E5\u672C\u8A8E.example.org"));
        }

        [Fact]
        public static void SingleValue_IPAddress_v4()
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();

            builder.AddIpAddress(IPAddress.Loopback);

            X509Extension extension = builder.Build();
            Assert.Equal(SubjectAltNameOid, extension.Oid.Value);

            Assert.Equal("300687047F000001", extension.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void SingleValue_IPAddress_v6()
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();

            builder.AddIpAddress(IPAddress.IPv6Loopback);

            X509Extension extension = builder.Build();
            Assert.Equal(SubjectAltNameOid, extension.Oid.Value);

            Assert.Equal("3012871000000000000000000000000000000001", extension.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void SingleValue_Uri_Ascii()
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();

            builder.AddUri(new Uri("http://www.example.org/"));

            X509Extension extension = builder.Build();
            Assert.Equal(SubjectAltNameOid, extension.Oid.Value);

            Assert.Equal(
                "30198617687474703A2F2F7777772E6578616D706C652E6F72672F",
                extension.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void SingleValue_Uri_UnicodeHost()
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();

            Assert.Throws<CryptographicException>(
                () => builder.AddUri(new Uri("http://\u65E5\u672C\u8A8E.example.org/")));
        }

        [Fact]
        public static void SingleValue_Uri_UnicodePath()
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();

            builder.AddUri(new Uri("http://www.example.org/\u65E5\u672C\u8A8E"));

            X509Extension extension = builder.Build();
            Assert.Equal(SubjectAltNameOid, extension.Oid.Value);

            const string expectedHex =
                "30348632687474703A2F2F7777772E6578616D706C652E6F72672F2545362539" +
                "37254135254536253943254143254538254141253845";

            Assert.Equal(
                expectedHex,
                extension.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void SingleValue_Upn()
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();

            builder.AddUserPrincipalName("user@example.org");

            X509Extension extension = builder.Build();
            Assert.Equal(SubjectAltNameOid, extension.Oid.Value);

            Assert.Equal(
                "3022A020060A2B060104018237140203A0120C1075736572406578616D706C652E6F7267",
                extension.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void MultiValue()
        {
            // This produces the same value as the "ComplexGetNameInfo" certificate/test suite.
            //   Subject Alternative Names:
            //     DNS Name=dns1.subject.example.org
            //     DNS Name=dns2.subject.example.org
            //     RFC822 Name=sanemail1@example.org
            //     RFC822 Name=sanemail2@example.org
            //     Other Name:
            //       Principal Name=subjectupn1@example.org
            //     Other Name:
            //       Principal Name=subjectupn2@example.org
            //     URL=http://uri1.subject.example.org/
            //     URL=http://uri2.subject.example.org/

            const string expectedHex =
                "3081F88218646E73312E7375626A6563742E6578616D706C652E6F7267821864" +
                "6E73322E7375626A6563742E6578616D706C652E6F7267811573616E656D6169" +
                "6C31406578616D706C652E6F7267811573616E656D61696C32406578616D706C" +
                "652E6F7267A027060A2B060104018237140203A0190C177375626A6563747570" +
                "6E31406578616D706C652E6F7267A027060A2B060104018237140203A0190C17" +
                "7375626A65637475706E32406578616D706C652E6F72678620687474703A2F2F" +
                "757269312E7375626A6563742E6578616D706C652E6F72672F8620687474703A" +
                "2F2F757269322E7375626A6563742E6578616D706C652E6F72672F";

            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();
            builder.AddDnsName("dns1.subject.example.org");
            builder.AddDnsName("dns2.subject.example.org");
            builder.AddEmailAddress("sanemail1@example.org");
            builder.AddEmailAddress("sanemail2@example.org");
            builder.AddUserPrincipalName("subjectupn1@example.org");
            builder.AddUserPrincipalName("subjectupn2@example.org");
            builder.AddUri(new Uri("http://uri1.subject.example.org/"));
            builder.AddUri(new Uri("http://uri2.subject.example.org/"));

            X509Extension extension = builder.Build();
            Assert.Equal(SubjectAltNameOid, extension.Oid.Value);

            Assert.Equal(
                expectedHex,
                extension.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void MultipleBuilds()
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();

            builder.AddIpAddress(IPAddress.Loopback);

            X509Extension extension = builder.Build();
            Assert.Equal(SubjectAltNameOid, extension.Oid.Value);

            X509Extension secondExtension = builder.Build();

            Assert.NotSame(extension, secondExtension);
            Assert.Equal(extension.Oid.Value, secondExtension.Oid.Value);
            Assert.Equal(extension.Critical, secondExtension.Critical);
            Assert.Equal(extension.RawData, secondExtension.RawData);
        }

        [Fact]
        public static void MultipleBuilds_WithModification()
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();

            builder.AddIpAddress(IPAddress.Loopback);

            X509Extension extension = builder.Build();
            Assert.Equal(SubjectAltNameOid, extension.Oid.Value);

            builder.AddIpAddress(IPAddress.IPv6Loopback);
            X509Extension secondExtension = builder.Build();

            Assert.NotSame(extension, secondExtension);
            Assert.Equal(extension.Oid.Value, secondExtension.Oid.Value);
            Assert.Equal(extension.Critical, secondExtension.Critical);

            Assert.True(
                secondExtension.RawData.Length > extension.RawData.Length,
                $"secondExtension.RawData.Length > extension.RawData.Length");
        }

        [Fact]
        public static void CheckCritical()
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();

            builder.AddIpAddress(IPAddress.Loopback);

            X509Extension extension = builder.Build();
            X509Extension secondExtension = builder.Build(false);
            X509Extension thirdExtension = builder.Build(true);

            Assert.False(extension.Critical, "extension.Critical");
            Assert.False(secondExtension.Critical, "secondExtension.Critical");
            Assert.True(thirdExtension.Critical, "thirdExtension.Critical");
        }
    }
}
