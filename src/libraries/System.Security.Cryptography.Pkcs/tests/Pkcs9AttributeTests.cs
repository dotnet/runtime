// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

using Test.Cryptography;

namespace System.Security.Cryptography.Pkcs.Tests
{
    public static class Pkcs9AttributeTests
    {
        [Fact]
        public static void Pkcs9AttributeObjectNullaryCtor()
        {
            Pkcs9AttributeObject p = new Pkcs9AttributeObject();
            Assert.Null(p.Oid);
            if (PlatformDetection.IsNetCore)
            {
                Assert.Empty(p.RawData);
            }
            else
            {
                Assert.Null(p.RawData);
            }
        }

        [Fact]
        public static void Pkcs9AttributeAsnEncodedDataCtorNullOid()
        {
            AsnEncodedData a = new AsnEncodedData(new byte[3]);
            object ign;
            AssertExtensions.Throws<ArgumentException, ArgumentNullException>("asnEncodedData", "asnEncodedData.Oid", () => ign = new Pkcs9AttributeObject(a));
        }

        [Fact]
        public static void InputDateTimeAsWindowsFileTimeBefore1601()
        {
            DateTime dt = new DateTime(1600, 12, 31, 11, 59, 59, DateTimeKind.Utc);
            AssertExtensions.Throws<CryptographicException, ArgumentOutOfRangeException>(() => new Pkcs9SigningTime(dt));
        }

        [Fact]
        public static void Pkcs9SigningTime_DateTimeMinValue()
        {
            AssertExtensions.Throws<CryptographicException, ArgumentOutOfRangeException>(() => new Pkcs9SigningTime(DateTime.MinValue));
        }

        [Fact]
        public static void InputDateTimeAsX509TimeBefore1950_Utc()
        {
            DateTime dt = new DateTime(1949, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            Assert.ThrowsAny<CryptographicException>(() => new Pkcs9SigningTime(dt));
        }

        [Fact]
        public static void InputDateTimeAsX509TimeBefore1950_Unspecified()
        {
            DateTime dt = new DateTime(1949, 12, 30);
            Assert.ThrowsAny<CryptographicException>(() => new Pkcs9SigningTime(dt));
        }

        [Fact]
        public static void InputDateTimeAsX509TimeBefore1950_Local()
        {
            DateTime dt = new DateTime(1949, 12, 30, 00, 00, 00, DateTimeKind.Local);
            Assert.ThrowsAny<CryptographicException>(() => new Pkcs9SigningTime(dt));
        }

        [Fact]
        public static void InputDateTimeAsX509TimeAfter2049_Utc()
        {
            DateTime dt = new DateTime(2050, 01, 01, 00, 00, 00, DateTimeKind.Utc);
            Assert.ThrowsAny<CryptographicException>(() => new Pkcs9SigningTime(dt));
        }

        [Fact]
        public static void InputDateTimeAsX509TimeAfter2049_Unspecified()
        {
            DateTime dt = new DateTime(2050, 01, 02);
            Assert.ThrowsAny<CryptographicException>(() => new Pkcs9SigningTime(dt));
        }

        [Fact]
        public static void InputDateTimeAsX509TimeAfter2049_Local()
        {
            DateTime dt = new DateTime(2050, 01, 02, 00, 00, 00, DateTimeKind.Local);
            Assert.ThrowsAny<CryptographicException>(() => new Pkcs9SigningTime(dt));
        }

        [Fact]
        public static void InputDateTimeAsX509TimeBetween1950And2049_Utc()
        {
            var exception = Record.Exception(() => {
                DateTime dt = new DateTime(1950, 1, 1, 00, 00, 00, DateTimeKind.Utc);
                Pkcs9SigningTime st = new Pkcs9SigningTime(dt);
                dt = new DateTime(2049, 12, 31, 23, 59, 59, DateTimeKind.Utc);
                st = new Pkcs9SigningTime(dt);

                dt = new DateTime(1950, 1, 2);
                st = new Pkcs9SigningTime(dt);
                dt = new DateTime(2049, 12, 30);
                st = new Pkcs9SigningTime(dt);

                dt = new DateTime(1950, 1, 2, 00, 00, 00, DateTimeKind.Local);
                st = new Pkcs9SigningTime(dt);
                dt = new DateTime(2049, 12, 30, 23, 59, 59, DateTimeKind.Local);
                st = new Pkcs9SigningTime(dt);
            });
            Assert.Null(exception);
        }

        [Fact]
        public static void Pkcs9AttributeAsnEncodedDataCtorNullOidValue()
        {
            Oid oid = new Oid(null, null);

            AsnEncodedData a = new AsnEncodedData(oid, new byte[3]);
            object ign;
            AssertExtensions.Throws<ArgumentException, ArgumentNullException>("asnEncodedData", "oid.Value", () => ign = new Pkcs9AttributeObject(a));
        }

        [Fact]
        public static void Pkcs9AttributeAsnEncodedDataCtorEmptyOidValue()
        {
            Oid oid = new Oid(string.Empty, null);

            AsnEncodedData a = new AsnEncodedData(oid, new byte[3]);
            object ign;
            AssertExtensions.Throws<ArgumentException>("asnEncodedData", "oid.Value", () => ign = new Pkcs9AttributeObject(a));
        }

        [Fact]
        public static void Pkcs9AttributeCopyFromNullAsn()
        {
            Pkcs9AttributeObject p = new Pkcs9AttributeObject();
            Assert.Throws<ArgumentNullException>(() => p.CopyFrom(null));
        }

        [Fact]
        public static void Pkcs9AttributeCopyFromAsnNotAPkcs9Attribute()
        {
            // Pkcs9AttributeObject.CopyFrom(AsnEncodedData) refuses to accept any AsnEncodedData that isn't a Pkcs9AttributeObject-derived class.
            Pkcs9AttributeObject p = new Pkcs9AttributeObject();
            byte[] rawData = "041e4d00790020004400650073006300720069007000740069006f006e000000".HexToByteArray();
            AsnEncodedData a = new AsnEncodedData(Oids.DocumentName, rawData);
            AssertExtensions.Throws<ArgumentException>(null, () => p.CopyFrom(a));
        }

        [Fact]
        public static void DocumentDescriptionNullary()
        {
            Pkcs9DocumentDescription p = new Pkcs9DocumentDescription();
            if (PlatformDetection.IsNetCore)
            {
                Assert.Empty(p.RawData);
                Assert.Throws<CryptographicException>(() => p.DocumentDescription);
            }
            else
            {
                Assert.Null(p.RawData);
                Assert.Null(p.DocumentDescription);
            }
            string oid = p.Oid.Value;
            Assert.Equal(s_OidDocumentDescription, oid);
        }

        [Fact]
        public static void DocumentDescriptionFromRawData()
        {
            byte[] rawData = "041e4d00790020004400650073006300720069007000740069006f006e000000".HexToByteArray();
            Pkcs9DocumentDescription p = new Pkcs9DocumentDescription(rawData);
            Assert.Equal(rawData, p.RawData);
            string cookedData = p.DocumentDescription;
            Assert.Equal("My Description", cookedData);
            string oid = p.Oid.Value;
            Assert.Equal(s_OidDocumentDescription, oid);
        }

        [Fact]
        public static void DocumentDescriptionMissingTerminator()
        {
            byte[] rawData = "041e4d00790020004400650073006300720069007000740069006f006e002100".HexToByteArray();
            Pkcs9DocumentDescription p = new Pkcs9DocumentDescription(rawData);
            Assert.Equal(rawData, p.RawData);
            string cookedData = p.DocumentDescription;
            Assert.Equal("My Description!", cookedData);
            string oid = p.Oid.Value;
            Assert.Equal(s_OidDocumentDescription, oid);
        }

        [Fact]
        public static void DocumentDescriptionEmbeddedTerminator()
        {
            byte[] rawData = "041e4d00790020004400650073006300720000007000740069006f006e000000".HexToByteArray();
            Pkcs9DocumentDescription p = new Pkcs9DocumentDescription(rawData);
            Assert.Equal(rawData, p.RawData);
            string cookedData = p.DocumentDescription;
            Assert.Equal("My Descr", cookedData);
            string oid = p.Oid.Value;
            Assert.Equal(s_OidDocumentDescription, oid);
        }

        [Fact]
        public static void DocumentDescriptionFromCookedData()
        {
            Pkcs9DocumentDescription p = new Pkcs9DocumentDescription("My Description");
            string oid = p.Oid.Value;
            Assert.Equal(s_OidDocumentDescription, oid);

            Pkcs9DocumentDescription p2 = new Pkcs9DocumentDescription(p.RawData);
            string cookedData = p2.DocumentDescription;
            Assert.Equal("My Description", cookedData);
        }

        [Fact]
        public static void DocumentDescriptionNullValue()
        {
            object ignore;
            Assert.Throws<ArgumentNullException>(() => ignore = new Pkcs9DocumentDescription((string)null));
        }

        [Fact]
        public static void DocumentNameNullary()
        {
            Pkcs9DocumentName p = new Pkcs9DocumentName();
            if (PlatformDetection.IsNetCore)
            {
                Assert.Empty(p.RawData);
                Assert.Throws<CryptographicException>(() => p.DocumentName);
            }
            else
            {
                Assert.Null(p.RawData);
                Assert.Null(p.DocumentName);
            }
            string oid = p.Oid.Value;
            Assert.Equal(s_OidDocumentName, oid);
        }

        [Fact]
        public static void DocumentNameFromRawData()
        {
            byte[] rawData = "04104d00790020004e0061006d0065000000".HexToByteArray();
            Pkcs9DocumentName p = new Pkcs9DocumentName(rawData);
            Assert.Equal(rawData, p.RawData);
            string cookedData = p.DocumentName;
            Assert.Equal("My Name", cookedData);
            string oid = p.Oid.Value;
            Assert.Equal(s_OidDocumentName, oid);
        }

        [Fact]
        public static void DocumentNameMissingTerminator()
        {
            byte[] rawData = "04104d00790020004e0061006d0065002100".HexToByteArray();
            Pkcs9DocumentName p = new Pkcs9DocumentName(rawData);
            Assert.Equal(rawData, p.RawData);
            string cookedData = p.DocumentName;
            Assert.Equal("My Name!", cookedData);
            string oid = p.Oid.Value;
            Assert.Equal(s_OidDocumentName, oid);
        }

        [Fact]
        public static void DocumentNameEmbeddedTerminator()
        {
            byte[] rawData = "04104d00790020004e006100000065000000".HexToByteArray();
            Pkcs9DocumentName p = new Pkcs9DocumentName(rawData);
            Assert.Equal(rawData, p.RawData);
            string cookedData = p.DocumentName;
            Assert.Equal("My Na", cookedData);
            string oid = p.Oid.Value;
            Assert.Equal(s_OidDocumentName, oid);
        }

        [Fact]
        public static void DocumentNameFromCookedData()
        {
            Pkcs9DocumentName p = new Pkcs9DocumentName("My Name");
            string oid = p.Oid.Value;
            Assert.Equal(s_OidDocumentName, oid);

            Pkcs9DocumentName p2 = new Pkcs9DocumentName(p.RawData);
            string cookedData = p2.DocumentName;
            Assert.Equal("My Name", cookedData);
        }

        [Fact]
        public static void DocumentNamenNullValue()
        {
            object ignore;
            Assert.Throws<ArgumentNullException>(() => ignore = new Pkcs9DocumentName((string)null));
        }

        [Fact]
        public static void SigningTimeNullary()
        {
            Pkcs9SigningTime p = new Pkcs9SigningTime();

            // the default constructor initializes with DateTime.Now.
            Assert.NotNull(p.RawData);
            Assert.Equal(DateTimeKind.Local, p.SigningTime.Kind);
            string oid = p.Oid.Value;
            Assert.Equal(s_OidSigningTime, oid);
        }

        [Fact]
        public static void SigningTimeFromRawData()
        {
            DateTime dateTime = new DateTime(2015, 4, 1);
            byte[] rawData = "170d3135303430313030303030305a".HexToByteArray();
            Pkcs9SigningTime p = new Pkcs9SigningTime(rawData);
            Assert.Equal(rawData, p.RawData);
            DateTime cookedData = p.SigningTime;
            Assert.Equal(DateTimeKind.Utc, cookedData.Kind);
            Assert.Equal(dateTime, cookedData);
            string oid = p.Oid.Value;
            Assert.Equal(s_OidSigningTime, oid);
        }

        [Fact]
        public static void SigningTimeFromCookedData_Unspecified()
        {
            DateTime dateTime = new DateTime(2015, 4, 1);
            Pkcs9SigningTime p = new Pkcs9SigningTime(dateTime);
            string oid = p.Oid.Value;
            Assert.Equal(s_OidSigningTime, oid);

            Pkcs9SigningTime p2 = new Pkcs9SigningTime(p.RawData);
            DateTime cookedData = p2.SigningTime;
            Assert.Equal(dateTime, cookedData);
            Assert.Equal(DateTimeKind.Utc, cookedData.Kind);
        }

        [Fact]
        public static void SigningTimeFromCookedData_Local()
        {
            DateTime dateTime = new DateTime(2015, 4, 1, 0, 0, 0, DateTimeKind.Local);
            Pkcs9SigningTime p = new Pkcs9SigningTime(dateTime);
            string oid = p.Oid.Value;
            Assert.Equal(s_OidSigningTime, oid);

            Pkcs9SigningTime p2 = new Pkcs9SigningTime(p.RawData);
            DateTime cookedData = p2.SigningTime;
            Assert.Equal(dateTime, cookedData.ToLocalTime());
            Assert.Equal(DateTimeKind.Utc, cookedData.Kind);
        }

        [Fact]
        public static void SigningTimeFromCookedData_Utc()
        {
            DateTime dateTime = new DateTime(2015, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            Pkcs9SigningTime p = new Pkcs9SigningTime(dateTime);
            string oid = p.Oid.Value;
            Assert.Equal(s_OidSigningTime, oid);

            Pkcs9SigningTime p2 = new Pkcs9SigningTime(p.RawData);
            DateTime cookedData = p2.SigningTime;
            Assert.Equal(dateTime, cookedData);
            Assert.Equal(DateTimeKind.Utc, cookedData.Kind);
        }

        [Fact]
        public static void ContentTypeNullary()
        {
            Pkcs9ContentType p = new Pkcs9ContentType();
            if (PlatformDetection.IsNetCore)
            {
                Assert.Empty(p.RawData);
                Assert.Throws<CryptographicException>(() => p.ContentType);
            }
            else
            {
                Assert.Null(p.RawData);
                Assert.Null(p.ContentType);
            }
            string oid = p.Oid.Value;
            Assert.Equal(s_OidContentType, oid);
        }

        [Fact]
        public static void ContentTypeFromRawData()
        {
            byte[] rawData = { ASN_TAG_OBJID, 0, 42, 0x9f, 0xa2, 0, 0x82, 0xf3, 0 };
            rawData[1] = (byte)(rawData.Length - 2);
            Pkcs9ContentType p = CreatePkcs9ContentType(rawData);
            Assert.Equal(rawData, p.RawData);
            string cookedData = p.ContentType.Value;
            Assert.Equal("1.2.512256.47488", cookedData);
            string oid = p.Oid.Value;
            Assert.Equal(s_OidContentType, oid);
        }

        [Fact]
        public static void ContentTypeFromCookedData()
        {
            string contentType = "1.3.8473.23.4773.23";
            byte[] encodedContentType = "06072bc21917a52517".HexToByteArray();
            Pkcs9ContentType p = new Pkcs9ContentType();
            Pkcs9AttributeObject pkcs9AttributeObject = new Pkcs9AttributeObject(p.Oid, encodedContentType);
            p.CopyFrom(pkcs9AttributeObject);

            string cookedData = p.ContentType.Value;
            Assert.Equal(contentType, cookedData);
            string oid = p.Oid.Value;
            Assert.Equal(s_OidContentType, oid);
        }

        [Fact]
        public static void ContentTypeFromRawDataMinimal()
        {
            byte[] rawData = { ASN_TAG_OBJID, 0 };
            rawData[1] = (byte)(rawData.Length - 2);
            Pkcs9ContentType p = CreatePkcs9ContentType(rawData);
            Assert.Equal(rawData, p.RawData);
            string cookedData = p.ContentType.Value;
            Assert.Equal("", cookedData);
            string oid = p.Oid.Value;
            Assert.Equal(s_OidContentType, oid);
        }

        [Fact]
        public static void ContentTypeBadData()
        {
            Assert.ThrowsAny<CryptographicException>(() => CreatePkcs9ContentTypeAndExtractContentType(new byte[0]));  // Too short
            Assert.ThrowsAny<CryptographicException>(() => CreatePkcs9ContentTypeAndExtractContentType(new byte[1]));  // Too short
            Assert.ThrowsAny<CryptographicException>(() => CreatePkcs9ContentTypeAndExtractContentType(new byte[2]));  // Does not start with ASN_TAG_OBJID.
            Assert.ThrowsAny<CryptographicException>(() => CreatePkcs9ContentTypeAndExtractContentType(new byte[] { ASN_TAG_OBJID, 1 }));  // Bad length byte.
        }

        [Fact]
        public static void MessageDigestNullary()
        {
            Pkcs9MessageDigest p = new Pkcs9MessageDigest();
            if (PlatformDetection.IsNetCore)
            {
                Assert.Empty(p.RawData);
                Assert.Throws<CryptographicException>(() => p.MessageDigest);
            }
            else
            {
                Assert.Null(p.RawData);
                Assert.Null(p.MessageDigest);
            }
            string oid = p.Oid.Value;
            Assert.Equal(s_OidMessageDigest, oid);
        }

        [Fact]
        public static void MessageDigestFromRawData()
        {
            byte[] messageDigest = { 3, 45, 88, 128, 93 };
            List<byte> encodedMessageDigestList = new List<byte>(messageDigest.Length + 2);
            encodedMessageDigestList.Add(4);
            encodedMessageDigestList.Add(checked((byte)(messageDigest.Length)));
            encodedMessageDigestList.AddRange(messageDigest);
            byte[] encodedMessageDigest = encodedMessageDigestList.ToArray();

            Pkcs9MessageDigest p = new Pkcs9MessageDigest();
            Pkcs9AttributeObject pAttribute = new Pkcs9AttributeObject(s_OidMessageDigest, encodedMessageDigest);
            p.CopyFrom(pAttribute);
            Assert.Equal<byte>(encodedMessageDigest, p.RawData);
            Assert.Equal<byte>(messageDigest, p.MessageDigest);
            string oid = p.Oid.Value;
            Assert.Equal(s_OidMessageDigest, oid);
        }

        private static void CreatePkcs9ContentTypeAndExtractContentType(byte[] rawData)
        {
            Oid contentType = CreatePkcs9ContentType(rawData).ContentType;
        }

        private static Pkcs9ContentType CreatePkcs9ContentType(byte[] rawData)
        {
            Pkcs9ContentType pkcs9ContentType = new Pkcs9ContentType();
            Pkcs9AttributeObject pkcs9AttributeObject = new Pkcs9AttributeObject(pkcs9ContentType.Oid, rawData);
            pkcs9ContentType.CopyFrom(pkcs9AttributeObject);
            return pkcs9ContentType;
        }

        private const byte ASN_TAG_OBJID = 0x06;

        private const string s_OidDocumentDescription = "1.3.6.1.4.1.311.88.2.2";
        private const string s_OidDocumentName = "1.3.6.1.4.1.311.88.2.1";
        private const string s_OidSigningTime = "1.2.840.113549.1.9.5";
        private const string s_OidContentType = "1.2.840.113549.1.9.3";
        private const string s_OidMessageDigest = "1.2.840.113549.1.9.4";
    }
}
