// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class X500DistinguishedNameBuilderTests
    {
        private const string TestOid = "2.25.77135202736018529853602245419149860647";

        [Theory]
        [InlineData("kevin@example.com", "30223120301E06092A864886F70D01090116116B6576696E406578616D706C652E636F6D")]
        [InlineData("totes not an email", "30233121301F06092A864886F70D0109011612746F746573206E6F7420616E20656D61696C")]
        public static void AddEmailAddress_Success_EncodedToIA5(string emailAddress, string expectedHex)
        {
            AssertBuilder(
                expectedHex,
                builder => builder.AddEmailAddress(emailAddress));
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

        [Theory]
        [InlineData("US", "300D310B3009060355040613025553")]
        [InlineData("AA", "300D310B3009060355040613024141")]
        [InlineData("ZZ", "300D310B3009060355040613025A5A")]
        [InlineData("aa", "300D310B3009060355040613024141")] // Normalized to AA
        [InlineData("zz", "300D310B3009060355040613025A5A")] // Normalized to ZZ.
        public static void AddCountryOrRegion_Success_EncodeToPrintableString(string twoLetterCode, string expectedHex)
        {
            AssertBuilder(expectedHex, builder => builder.AddCountryOrRegion(twoLetterCode));
        }

        [Theory]
        [InlineData("A")] // Not two characters
        [InlineData("AAA")] // Not two characters
        [InlineData("01")] // PrintableString, but not alpha.
        [InlineData("@@")] // Boundary case, one below 'A'.
        [InlineData("[[")] // Boundary case, one above 'Z'.
        [InlineData("``")] // Boundary case, one below 'a'. Also tests not a PrintableString
        [InlineData("{{")] // Boundary case, one above 'z'.
        public static void AddCountryOrRegion_Invalid_Fails(string countryOrRegion)
        {
            X500DistinguishedNameBuilder builder = new();
            AssertExtensions.Throws<ArgumentException>(
                "twoLetterCode",
                () => builder.AddCountryOrRegion(countryOrRegion));
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
            AssertBuilder(
                "30153113301106035504080C0A43616C69666F726E6961",
                builder => builder.AddStateOrProvinceName("California"));
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
                builder => builder.AddEncoded(new Oid(TestOid, null), new byte[] { 0x13, 0x01, 65 }));

            AssertBuilder(
                "301C311A3018061369F487D9C7B5D0AEA2CCCBE8C8C7F2F6F2BE27130141",
                builder => builder.AddEncoded(new Oid(TestOid, null), stackalloc byte[] { 0x13, 0x01, 65 }));
        }

        [Fact]
        public static void AddEncoded_InvalidDer()
        {
            X500DistinguishedNameBuilder builder = new();
            AssertExtensions.Throws<ArgumentException>(
                "encodedValue",
                () => builder.AddEncoded(TestOid, new byte[] { 0xFF, 0xFF, 0xFF }));
            AssertExtensions.Throws<ArgumentException>(
                "encodedValue",
                () => builder.AddEncoded(TestOid, stackalloc byte[] { 0xFF, 0xFF, 0xFF }));
            AssertExtensions.Throws<ArgumentException>(
                "encodedValue",
                () => builder.AddEncoded(new Oid(TestOid), new byte[] { 0xFF, 0xFF, 0xFF }));
            AssertExtensions.Throws<ArgumentException>(
                "encodedValue",
                () => builder.AddEncoded(new Oid(TestOid), stackalloc byte[] { 0xFF, 0xFF, 0xFF }));
        }

        [Fact]
        public static void AddEncoded_StringOid_NullOrEmpty_Fails()
        {
            X500DistinguishedNameBuilder builder = new();
            AssertExtensions.Throws<ArgumentNullException>(
                "oidValue",
                () => builder.AddEncoded((string)null, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentException>(
                "oidValue",
                () => builder.AddEncoded("", Array.Empty<byte>()));
        }

        [Fact]
        public static void AddEncoded_Oid_NullOrEmptyValue_Fails()
        {
            X500DistinguishedNameBuilder builder = new();
            AssertExtensions.Throws<ArgumentNullException>(
                "oid",
                () => builder.AddEncoded((Oid)null, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentException>(
                "oid",
                () => builder.AddEncoded(new Oid(), Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentException>(
                "oid",
                () => builder.AddEncoded(new Oid("", null), Array.Empty<byte>()));
        }

        [Fact]
        public static void AddEncoded_Value_Null_Fails()
        {
            X500DistinguishedNameBuilder builder = new();
            AssertExtensions.Throws<ArgumentNullException>(
                "encodedValue",
                () => builder.AddEncoded(TestOid, (byte[])null));
            AssertExtensions.Throws<ArgumentNullException>(
                "encodedValue",
                () => builder.AddEncoded(new Oid(TestOid, null), (byte[])null));
        }

        [Fact]
        public static void Add_InvalidUniversalTagNumber()
        {
            X500DistinguishedNameBuilder builder = new();
            AssertExtensions.Throws<ArgumentException>(
                "stringEncodingType",
                () => builder.Add(TestOid, "True", UniversalTagNumber.Boolean));
            AssertExtensions.Throws<ArgumentException>(
                "stringEncodingType",
                () => builder.Add(new Oid(TestOid, null), "True", UniversalTagNumber.Boolean));
        }

        [Fact]
        public static void Add_OidString_NullOrEmpty_Fails()
        {
            X500DistinguishedNameBuilder builder = new();
            AssertExtensions.Throws<ArgumentNullException>(
                "oidValue",
                () => builder.Add((string)null, "banana"));
            AssertExtensions.Throws<ArgumentException>(
                "oidValue",
                () => builder.Add("", "banana"));
        }

        [Fact]
        public static void Add_OidString_NotAnOid_Fails()
        {
            X500DistinguishedNameBuilder builder = new();
            AssertExtensions.Throws<ArgumentException>(
                "oidValue",
                () => builder.Add("strawberry", "banana"));
        }

        [Theory]
        [MemberData(nameof(AddStringTheories))]
        public static void Add_StringOid_Success(string oid, string value, UniversalTagNumber? tag, string expectedHex)
        {
            AssertBuilder(expectedHex, builder => builder.Add(oid, value, tag));
        }

        [Theory]
        [MemberData(nameof(AddStringTheories))]
        public static void Add_Oid_Success(string oid, string value, UniversalTagNumber? tag, string expectedHex)
        {
            AssertBuilder(expectedHex, builder => builder.Add(new Oid(oid, null), value, tag));
        }

        [Fact]
        public static void Add_WithException_ValidState()
        {
            X500DistinguishedNameBuilder builder = new();

            // Cause an exception to be raised, and handled.
            AssertExtensions.Throws<ArgumentException>(
                "emailAddress",
                () => builder.AddEmailAddress("\u0411\u0430\u043d\u0430\u043d"));

            // Make sure the handled exception didn't put the builder in an invalid state.
            builder.AddEmailAddress("k@example.com");
            X500DistinguishedName dn = builder.Build();
            Assert.Equal("301E311C301A06092A864886F70D010901160D6B406578616D706C652E636F6D", Convert.ToHexString(dn.RawData));
        }

        [Fact]
        public static void Build_Empty()
        {
            AssertBuilder("3000", builder => { });
        }

        public static IEnumerable<object[]> AddStringTheories
        {
            get
            {
                yield return new object[]
                {
                    TestOid,
                    "banana",
                    UniversalTagNumber.UTF8String,
                    "3021311F301D061369F487D9C7B5D0AEA2CCCBE8C8C7F2F6F2BE270C0662616E616E61",
                };
                yield return new object[]
                {
                    TestOid,
                    "banana",
                    null,
                    "3021311F301D061369F487D9C7B5D0AEA2CCCBE8C8C7F2F6F2BE270C0662616E616E61",
                };
                yield return new object[]
                {
                    TestOid,
                    "banana",
                    UniversalTagNumber.PrintableString,
                    "3021311F301D061369F487D9C7B5D0AEA2CCCBE8C8C7F2F6F2BE27130662616E616E61",
                };
                yield return new object[]
                {
                    TestOid,
                    "banana",
                    UniversalTagNumber.VisibleString,
                    "3021311F301D061369F487D9C7B5D0AEA2CCCBE8C8C7F2F6F2BE271A0662616E616E61",
                };
                yield return new object[]
                {
                    TestOid,
                    "banana",
                    UniversalTagNumber.IA5String,
                    "3021311F301D061369F487D9C7B5D0AEA2CCCBE8C8C7F2F6F2BE27160662616E616E61",
                };
                yield return new object[]
                {
                    TestOid,
                    "banana",
                    UniversalTagNumber.PrintableString,
                    "3021311F301D061369F487D9C7B5D0AEA2CCCBE8C8C7F2F6F2BE27130662616E616E61",
                };
                yield return new object[]
                {
                    TestOid,
                    "\u0411\u0430\u043d\u0430\u043d",
                    UniversalTagNumber.BMPString,
                    "302531233021061369F487D9C7B5D0AEA2CCCBE8C8C7F2F6F2BE271E0A04110430043D0430043D",
                };
                yield return new object[]
                {
                    TestOid,
                    "\u0411\u0430\u043d\u0430\u043d",
                    UniversalTagNumber.UTF8String,
                    "302531233021061369F487D9C7B5D0AEA2CCCBE8C8C7F2F6F2BE270C0AD091D0B0D0BDD0B0D0BD",
                };
                yield return new object[]
                {
                    TestOid,
                    "0123456789",
                    UniversalTagNumber.NumericString,
                    "302531233021061369F487D9C7B5D0AEA2CCCBE8C8C7F2F6F2BE27120A30313233343536373839",
                };
                yield return new object[]
                {
                    TestOid,
                    "",
                    UniversalTagNumber.UTF8String,
                    "301B31193017061369F487D9C7B5D0AEA2CCCBE8C8C7F2F6F2BE270C00",
                };
            }
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
