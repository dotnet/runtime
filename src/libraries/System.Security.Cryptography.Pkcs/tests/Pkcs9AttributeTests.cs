// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography.X509Certificates;
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
            Assert.Null(p.RawData);
        }

        [Fact]
        public static void Pkcs9AttributeAsnEncodedDataCtorNullOid()
        {
            AsnEncodedData a = new AsnEncodedData(new byte[3]);
            object ign;
            Assert.Throws<ArgumentNullException>(() => ign = new Pkcs9AttributeObject(a));
        }


        [Fact]
        public static void Pkcs9AttributeAsnEncodedDataCtorNullOidValue()
        {
            Oid oid = new Oid(Oids.Aes128);
            oid.Value = null;

            AsnEncodedData a = new AsnEncodedData(oid, new byte[3]);
            object ign;
            Assert.Throws<ArgumentNullException>(() => ign = new Pkcs9AttributeObject(a));
        }

        [Fact]
        public static void Pkcs9AttributeAsnEncodedDataCtorEmptyOidValue()
        {
            Oid oid = new Oid(Oids.Aes128);
            oid.Value = string.Empty;

            AsnEncodedData a = new AsnEncodedData(oid, new byte[3]);
            object ign;
            AssertExtensions.Throws<ArgumentException>("oid.Value", () => ign = new Pkcs9AttributeObject(a));
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
            Assert.Null(p.RawData);
            Assert.Null(p.DocumentDescription);
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
            Assert.Null(p.RawData);
            Assert.Null(p.DocumentName);
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
            Assert.Equal(dateTime, cookedData);
            string oid = p.Oid.Value;
            Assert.Equal(s_OidSigningTime, oid);
        }

        [Fact]
        public static void SigningTimeFromCookedData()
        {
            DateTime dateTime = new DateTime(2015, 4, 1);
            Pkcs9SigningTime p = new Pkcs9SigningTime(dateTime);
            string oid = p.Oid.Value;
            Assert.Equal(s_OidSigningTime, oid);

            Pkcs9SigningTime p2 = new Pkcs9SigningTime(p.RawData);
            DateTime cookedData = p2.SigningTime;
            Assert.Equal(dateTime, cookedData);
        }

        [Fact]
        public static void ContentTypeNullary()
        {
            Pkcs9ContentType p = new Pkcs9ContentType();
            Assert.Null(p.RawData);
            Assert.Null(p.ContentType);
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
            Assert.Null(p.RawData);
            Assert.Null(p.MessageDigest);
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

