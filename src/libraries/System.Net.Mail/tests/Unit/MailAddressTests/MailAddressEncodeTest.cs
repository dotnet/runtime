// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Mime;
using System.Text;
using Xunit;

namespace System.Net.Mail.Tests
{
    public class MailAddressEncodeTest
    {
        public static IEnumerable<object[]> MailAddressFactory_Address()
        {
            yield return new object[] { new Func<string, MailAddress>((string address) => new MailAddress(address)) };
            yield return new object[] { new Func<string, MailAddress>(
            (string address) =>
            {
                Assert.True(MailAddress.TryCreate(address, out MailAddress result));
                return result;
            })};
        }

        public static IEnumerable<object[]> MailAddressFactory_AddressDisplayName()
        {
            yield return new object[] { new Func<string, string, MailAddress>((string address, string displayName) => new MailAddress(address, displayName)) };
            yield return new object[] { new Func<string,string, MailAddress>(
            (string address, string displayName) =>
            {
                Assert.True(MailAddress.TryCreate(address, displayName, out MailAddress result));
                return result;
            })};
        }

        public static IEnumerable<object[]> MailAddressFactory_Mixed()
        {
            yield return new object[] { new Func<string, MailAddress>((string address) => new MailAddress(address)), new Func<string, string, MailAddress>((string address, string displayName) => new MailAddress(address, displayName)) };
            yield return new object[] {
            new Func<string, MailAddress>(
            (string address) =>
            {
                Assert.True(MailAddress.TryCreate(address, out MailAddress result));
                return result;
            }),
            new Func<string,string, MailAddress>(
            (string address, string displayName) =>
            {
                Assert.True(MailAddress.TryCreate(address, displayName, out MailAddress result));
                return result;
            })};
        }

        [Theory]
        [MemberData(nameof(MailAddressFactory_Address))]
        public void EncodeSingleMailAddress_WithAddressAndNoUnicode_AndPaddingValueOfNonZero_ShouldEncodeCorrectly(Func<string, MailAddress> factory)
        {
            MailAddress testAddress = factory("test@example.com");

            string result = testAddress.Encode(2, false);
            Assert.Equal("test@example.com", result);

            result = testAddress.Encode(2, true);
            Assert.Equal("test@example.com", result);
        }

        [Theory]
        [MemberData(nameof(MailAddressFactory_AddressDisplayName))]
        public void EncodeSingleMailAddress_WithAddressAndUnicode_AndPaddingValueOfNonZero_ShouldEncodeCorrectly(Func<string, string, MailAddress> factory)
        {
            MailAddress testAddress = factory("jeff@nclmailtest.com", "jeff \u00C3\u00DA\u00EA\u00EB\u00EF\u00EF");
            string result = testAddress.Encode(2, false);
            Assert.Equal("=?utf-8?Q?jeff_=C3=83=C3=9A=C3=AA=C3=AB=C3=AF=C3=AF?= <jeff@nclmailtest.com>", result);

            result = testAddress.Encode(2, true);
            Assert.Equal("\"jeff \u00C3\u00DA\u00EA\u00EB\u00EF\u00EF\" <jeff@nclmailtest.com>", result);
        }

        [Theory]
        [MemberData(nameof(MailAddressFactory_Address))]
        public void EncodeSingleMailAddress_WithAddressOnly_ShouldEncodeAsAddress(Func<string, MailAddress> factory)
        {
            MailAddress testAddress = factory("test@example.com");
            string result = testAddress.Encode(0, false);
            Assert.Equal("test@example.com", result);
        }

        [Theory]
        [MemberData(nameof(MailAddressFactory_AddressDisplayName))]
        public void EncodeSingleMailAddress_WithAddressAndDisplayName_ShouldEncodeAsDisplayNameAndAddressInBrackets(Func<string, string, MailAddress> factory)
        {
            MailAddress testAddress = factory("test@example.com", "test");
            string result = testAddress.Encode(0, false);
            Assert.Equal("\"test\" <test@example.com>", result);

            result = testAddress.Encode(0, true);
            Assert.Equal("\"test\" <test@example.com>", result);
        }

        [Theory]
        [MemberData(nameof(MailAddressFactory_AddressDisplayName))]
        public void EncodeSingleMailAddress_WithAddressAndDisplayNameUnicode_ShouldQEncode(Func<string, string, MailAddress> factory)
        {
            MailAddress testAddress = factory("test@example.com", "test\u00DC");

            string result = testAddress.Encode(0, false);
            Assert.Equal("=?utf-8?Q?test=C3=9C?= <test@example.com>", result);

            result = testAddress.Encode(0, true);
            Assert.Equal("\"test\u00DC\" <test@example.com>", result);
        }

        [Theory]
        [MemberData(nameof(MailAddressFactory_AddressDisplayName))]
        public void EncodeSingleMailAddress_WithAddressAndLongDisplayNameUnicode_ShouldQEncode(Func<string, string, MailAddress> factory)
        {
            MailAddress testAddress = factory("test@example.com",
                "test\u00DCtesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttest");
            string result = testAddress.Encode(0, false);
            Assert.Equal("=?utf-8?Q?test=C3=9Ctesttesttesttesttesttesttesttesttesttesttesttest?=\r\n "
                + "=?utf-8?Q?testtesttesttesttesttest?= <test@example.com>", result);

            result = testAddress.Encode(0, true);
            Assert.Equal("\"test\u00DCtesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttest\" "
                + "<test@example.com>", result);
        }

        [Theory]
        [MemberData(nameof(MailAddressFactory_AddressDisplayName))]
        public void EncodeSingleMailAddress_WithAddressAndNonAsciiAndRangeOfChars_ShouldQEncode(Func<string, string, MailAddress> factory)
        {
            MailAddress testAddress = factory("test@example.com",
                "\u00AE !#$%&'()+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~");
            string result = testAddress.Encode(0, false);
            Assert.Equal("=?utf-8?Q?=C2=AE_=21=23=24=25=26=27=28=29=2B=2C=2D=2E=2F0123456789=3A?="
                + "\r\n =?utf-8?Q?=3B=3C=3D=3E=3F=40ABCDEFGHIJKLMNOPQRSTUVWXYZ=5B=5C=5D=5E=5F?="
                + "\r\n =?utf-8?Q?=60abcdefghijklmnopqrstuvwxyz=7B=7C=7D=7E?= <test@example.com>", result);

            result = testAddress.Encode(0, true);
            Assert.Equal(
                "\"\u00AE !#$%&'()+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\" "
                + "<test@example.com>", result);
        }

        [Theory]
        [MemberData(nameof(MailAddressFactory_AddressDisplayName))]
        public void EncodeMultipleMailAddress_WithOneAddressAndDisplayName_ShouldEncodeAsDisplayNameAndAddressInBrackets(Func<string, string, MailAddress> factory)
        {
            MailAddress testAddress = factory("test@example.com", "test");
            MailAddressCollection collection = new MailAddressCollection();
            collection.Add(testAddress);

            string result = collection.Encode(0, false);
            Assert.Equal("\"test\" <test@example.com>", result);

            result = collection.Encode(0, true);
            Assert.Equal("\"test\" <test@example.com>", result);
        }

        [Theory]
        [MemberData(nameof(MailAddressFactory_AddressDisplayName))]
        public void EncodeMultipleMailAddress_WithTwoAddressesThatAreTheSame_ShouldEncodeCorrectly(Func<string, string, MailAddress> factory)
        {
            MailAddress testAddress = factory("test@example.com", "test");
            MailAddress testAddress2 = factory("test@example.com", "test");
            MailAddressCollection collection = new MailAddressCollection();
            collection.Add(testAddress);
            collection.Add(testAddress2);

            string result = collection.Encode(0, false);
            Assert.Equal("\"test\" <test@example.com>, \"test\" <test@example.com>", result);

            result = collection.Encode(0, true);
            Assert.Equal("\"test\" <test@example.com>, \"test\" <test@example.com>", result);
        }

        [Theory]
        [MemberData(nameof(MailAddressFactory_AddressDisplayName))]
        public void EncodeMultipleMailAddress_WithTwoAddressesThatAreDifferentAndContainUnicode_ShouldEncodeCorrectly(Func<string, string, MailAddress> factory)
        {
            MailAddress testAddress = factory("test@example.com", "test");
            MailAddress testAddress2 = factory("test2@example.com", "test\u00DC");
            MailAddressCollection collection = new MailAddressCollection();
            collection.Add(testAddress);
            collection.Add(testAddress2);

            string result = collection.Encode(0, false);
            Assert.Equal("\"test\" <test@example.com>, =?utf-8?Q?test=C3=9C?= <test2@example.com>", result);

            result = collection.Encode(0, true);
            Assert.Equal("\"test\" <test@example.com>, \"test\u00DC\" <test2@example.com>", result);
        }

        [Theory]
        [MemberData(nameof(MailAddressFactory_Mixed))]
        public void EncodeMultipleMailAddress_WithManyAddressesThatAreDifferentAndContainUnicode_ShouldEncodeCorrectly(Func<string, MailAddress> factoryAddress, Func<string, string, MailAddress> factoryAddressDisplayName)
        {
            MailAddress testAddress = factoryAddressDisplayName("test@example.com", "test");
            MailAddress testAddress2 = factoryAddressDisplayName("test2@example.com", "test\u00DC");
            MailAddress testAddress3 = factoryAddress("test5@example2.com");
            MailAddress testAddress4 = factoryAddressDisplayName("test2@example.com", "test\u00DC");

            MailAddressCollection collection = new MailAddressCollection();
            collection.Add(testAddress);
            collection.Add(testAddress2);
            collection.Add(testAddress3);
            collection.Add(testAddress4);

            string result = collection.Encode(0, false);
            Assert.Equal("\"test\" <test@example.com>, =?utf-8?Q?test=C3=9C?= <test2@example.com>,"
                + " test5@example2.com, =?utf-8?Q?test=C3=9C?= <test2@example.com>", result);

            result = collection.Encode(0, true);
            Assert.Equal("\"test\" <test@example.com>, \"test\u00DC\" <test2@example.com>, test5@example2.com,"
                + " \"test\u00DC\" <test2@example.com>", result);
        }

        [Fact]
        public void CustomEncoding_Null()
        {
            Encoding defaultCharset = Encoding.GetEncoding(MimeBasePart.DefaultCharSet);

            Assert.True(MailAddress.TryCreate("test@example.com", "te\u03a0st", null, out MailAddress mailAddress));
            Assert.Contains(defaultCharset.BodyName, mailAddress.Encode(0, false));

            mailAddress = new MailAddress("test@example.com", "te\u03a0st", null);
            Assert.Contains(defaultCharset.BodyName, mailAddress.Encode(0, false));
        }

        public static IEnumerable<object[]> EncodingList()
        {
            yield return new object[] { Encoding.Unicode };
            yield return new object[] { Encoding.ASCII };
        }

        [Theory]
        [MemberData(nameof(EncodingList))]
        public void CustomEncoding(Encoding encoding)
        {
            Assert.True(MailAddress.TryCreate("test@example.com", "te\u03a0st", encoding, out MailAddress mailAddress));
            Assert.Contains(encoding.BodyName, mailAddress.Encode(0, false));

            mailAddress = new MailAddress("test@example.com", "te\u03a0st", encoding);
            Assert.Contains(encoding.BodyName, mailAddress.Encode(0, false));
        }
    }
}
