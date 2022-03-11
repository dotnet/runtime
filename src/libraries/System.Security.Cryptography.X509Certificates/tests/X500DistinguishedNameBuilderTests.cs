// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class X500DistinguishedNameBuilderTests
    {
        private const string TestOid = "2.25.77135202736018529853602245419149860647";

        [Fact]
        public static void AddEmailAddress_Success_EncodedToIA5()
        {
            AssertBuilder(
                "30223120301E06092A864886F70D01090116116B6576696E406578616D706C652E636F6D",
                builder => builder.AddEmailAddress("kevin@example.com"));
        }

        [Fact]
        public static void AddEmailAddress_ExceedsMaximumLength_Fails()
        {
            X500DistinguishedNameBuilder builder = new();
            string bigEmail = new string('a', 244) + "@example.com";
            AssertExtensions.Throws<ArgumentException>("emailAddress", () => builder.AddEmailAddress(bigEmail));
        }

        [Fact]
        public static void AddEmailAddress_InvalidIA5_Fails()
        {
            X500DistinguishedNameBuilder builder = new();
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(
                "emailAddress",
                () => builder.AddEmailAddress("\u043A@example.com"));
            Assert.Contains("'IA5String'", ex.Message);
        }

        [Fact]
        public static void AddEmailAddress_NullOrEmpty_Fails()
        {
            AssertAddThrowsOnNullAndEmpty("emailAddress", builder => builder.AddEmailAddress);
        }

        [Fact]
        public static void AddCommonName_Success_EncodeToUTF8String()
        {
            AssertBuilder("300D310B300906035504030C024B4A", builder => builder.AddCommonName("KJ"));
        }

        [Fact]
        public static void AddCommonName_NullOrEmpty_Fails()
        {
            AssertAddThrowsOnNullAndEmpty("commonName", builder => builder.AddCommonName);
        }

        [Fact]
        public static void AddLocalityName_Success_EncodeToUTF8String()
        {
            AssertBuilder("300F310D300B06035504070C04486F6D65", builder => builder.AddLocalityName("Home"));
        }

        [Fact]
        public static void AddLocalityName_NullOrEmpty_Fails()
        {
            AssertAddThrowsOnNullAndEmpty("localityName", builder => builder.AddLocalityName);
        }

        [Fact]
        public static void AddCountryOrRegion_Success_EncodeToPrintableString()
        {
            AssertBuilder("300D310B3009060355040613025553", builder => builder.AddCountryOrRegion("US"));
        }

        [Theory]
        [InlineData("A")]
        [InlineData("AAA")]
        public static void AddCountryOrRegion_InvalidLength_Fails(string countryOrRegion)
        {
            X500DistinguishedNameBuilder builder = new();
            AssertExtensions.Throws<ArgumentException>(
                "twoLetterCode",
                () => builder.AddCountryOrRegion(countryOrRegion));
        }

        [Fact]
        public static void AddCountryOrRegion_InvalidContents_Fails()
        {
            X500DistinguishedNameBuilder builder = new();
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(
                "twoLetterCode",
                () => builder.AddCountryOrRegion("``"));
            Assert.Contains("'PrintableString'", ex.Message);
        }

        [Fact]
        public static void AddOrganizationName_Success_EncodeToUTF8String()
        {
            AssertBuilder("3011310F300D060355040A0C06476974487562", builder => builder.AddOrganizationName("GitHub"));
        }

        [Fact]
        public static void AddOrganizationName_NullOrEmpty_Fails()
        {
            AssertAddThrowsOnNullAndEmpty("organizationName", builder => builder.AddOrganizationName);
        }

        [Fact]
        public static void AddOrganizationalUnitName_Success_EncodeToUTF8String()
        {
            AssertBuilder("300E310C300A060355040B0C03505345", builder => builder.AddOrganizationalUnitName("PSE"));
        }

        [Fact]
        public static void AddOrganizationalUnitName_NullOrEmpty_Fails()
        {
            AssertAddThrowsOnNullAndEmpty("organizationalUnitName", builder => builder.AddOrganizationalUnitName);
        }

        [Fact]
        public static void AddStateOrProvinceName_Success_EncodeToUTF8String()
        {
            AssertBuilder("300D310B300906035504080C024341", builder => builder.AddStateOrProvinceName("CA"));
        }

        [Fact]
        public static void AddStateOrProvinceName_NullOrEmpty_Fails()
        {
            AssertAddThrowsOnNullAndEmpty("stateOrProvinceName", builder => builder.AddStateOrProvinceName);
        }

        [Fact]
        public static void AddDomainComponent_Success_EncodeToIA5String()
        {
            AssertBuilder(
                "301A31183016060A0992268993F22C6401191608494E5445524E414C",
                builder => builder.AddDomainComponent("INTERNAL"));
        }

        [Fact]
        public static void AddDomainComponent_NullOrEmpty_Fails()
        {
            AssertAddThrowsOnNullAndEmpty("domainComponent", builder => builder.AddDomainComponent);
        }

        [Fact]
        public static void AddDomainComponent_InvalidIA5_Fails()
        {
            X500DistinguishedNameBuilder builder = new();
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(
                "domainComponent",
                () => builder.AddDomainComponent("\u043Aevin"));
            Assert.Contains("'IA5String'", ex.Message);
        }

        [Fact]
        public static void AddEncoded_StringOid_Success()
        {
            AssertBuilder(
                "301C311A3018061369F487D9C7B5D0AEA2CCCBE8C8C7F2F6F2BE27130141",
                builder => builder.AddEncoded(TestOid, new byte[] { 0x13, 0x01, 65 }));

            AssertBuilder(
                "301C311A3018061369F487D9C7B5D0AEA2CCCBE8C8C7F2F6F2BE27130141",
                builder => builder.AddEncoded(TestOid, stackalloc byte[] { 0x13, 0x01, 65 }));
        }

        [Fact]
        public static void AddEncoded_Oid_Success()
        {
            AssertBuilder(
                "301C311A3018061369F487D9C7B5D0AEA2CCCBE8C8C7F2F6F2BE27130141",
                builder => builder.AddEncoded(new Oid(TestOid), new byte[] { 0x13, 0x01, 65 }));

            AssertBuilder(
                "301C311A3018061369F487D9C7B5D0AEA2CCCBE8C8C7F2F6F2BE27130141",
                builder => builder.AddEncoded(new Oid(TestOid), stackalloc byte[] { 0x13, 0x01, 65 }));
        }

        private static void AssertAddThrowsOnNullAndEmpty(string paramName, Func<X500DistinguishedNameBuilder, Action<string>> adder)
        {
            AssertExtensions.Throws<ArgumentNullException>(
                paramName, () => adder(new X500DistinguishedNameBuilder())(null));
            AssertExtensions.Throws<ArgumentException>(
                paramName, () => adder(new X500DistinguishedNameBuilder())(string.Empty));
        }

        private static X500DistinguishedName AssertBuilder(string expectedHex, Action<X500DistinguishedNameBuilder> callback)
        {
            X500DistinguishedNameBuilder builder = new();
            callback(builder);
            X500DistinguishedName dn = builder.Build();
            Assert.Equal(expectedHex, Convert.ToHexString(dn.RawData));
            return dn;
        }
    }
}
