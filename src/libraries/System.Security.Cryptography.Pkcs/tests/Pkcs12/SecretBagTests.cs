// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests.Pkcs12
{
    public static class SecretBagTests
    {
        [Fact]
        public static void OidRequired()
        {
            Pkcs12SafeContents contents = new Pkcs12SafeContents();

            AssertExtensions.Throws<ArgumentNullException>(
                "secretType",
                () => contents.AddSecret(null, ReadOnlyMemory<byte>.Empty));
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
        public static void LegalBerPayloadRequired(string inputHex, bool expectSuccess)
        {
            Pkcs12SafeContents contents = new Pkcs12SafeContents();

            Action addAction =
                () => contents.AddSecret(new Oid("0.0", "0.0"), inputHex.HexToByteArray());

            if (expectSuccess)
            {
                addAction();
            }
            else
            {
                Assert.ThrowsAny<CryptographicException>(addAction);
            }
        }

        [Fact]
        public static void BadOidFails()
        {
            string payloadHex = "0403090807";

            Pkcs12SafeContents contents = new Pkcs12SafeContents();
            Assert.ThrowsAny<CryptographicException>(
                () => contents.AddSecret(new Oid("Hi", "There"), payloadHex.HexToByteArray()));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void HasExpectedEncode(bool withAttribute)
        {
            string expectedHexWithAttribute =
                "303F060B2A864886F70D010C0A0105A01C301A060100A0150C1353776F726466" +
                "6973682E20436C6561726C792E3112301006092A864886F70D01091531030401" +
                "01";

            string expectedHexNoAttribute =
                "302B060B2A864886F70D010C0A0105A01C301A060100A0150C1353776F726466" +
                "6973682E20436C6561726C792E";

            string expectedHex = withAttribute ? expectedHexWithAttribute : expectedHexNoAttribute;

            // UTF8String ("Swordfish. Clearly.")
            string payloadHex = "0C1353776F7264666973682E20436C6561726C792E";

            Pkcs12SafeContents contents = new Pkcs12SafeContents();
            Pkcs12SecretBag bag = contents.AddSecret(new Oid("0.0", "0.0"), payloadHex.HexToByteArray());

            if (withAttribute)
            {
                bag.Attributes.Add(new Pkcs9LocalKeyId(new byte[] { 0x01 }));
            }

            byte[] encoded = bag.Encode();
            Assert.Equal(expectedHex, encoded.ByteArrayToHex());

            Span<byte> tooBig = new byte[encoded.Length + 10];
            tooBig.Fill(0xCA);

            Assert.False(bag.TryEncode(tooBig.Slice(0, encoded.Length - 1), out int bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(0xCA, tooBig[0]);

            Assert.True(bag.TryEncode(tooBig.Slice(3), out bytesWritten));
            Assert.Equal(encoded.Length, bytesWritten);
            Assert.Equal(expectedHex, tooBig.Slice(3, bytesWritten).ByteArrayToHex());

            tooBig.Fill(0xCA);
            bytesWritten = 0;

            Assert.True(bag.TryEncode(tooBig.Slice(3, encoded.Length), out bytesWritten));
            Assert.Equal(encoded.Length, bytesWritten);
            Assert.Equal(expectedHex, tooBig.Slice(3, bytesWritten).ByteArrayToHex());
        }
    }
}
