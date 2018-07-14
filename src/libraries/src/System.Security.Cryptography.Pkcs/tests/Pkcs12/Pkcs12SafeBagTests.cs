// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests.Pkcs12
{
    public static class Pkcs12SafeBagTests
    {
        [Fact]
        public static void OidRequired()
        {
            AssertExtensions.Throws<ArgumentNullException>(
                "bagIdValue",
                () => new TestSafeBag(null));
        }

        [Fact]
        public static void OidValidatedLate()
        {
            Pkcs12SafeBag safeBag = new TestSafeBag("potato");
            Assert.ThrowsAny<CryptographicException>(() => safeBag.Encode());
        }

        [Fact]
        public static void OidHasNoNamespaceRequirement()
        {
            Pkcs12SafeBag safeBag = new TestSafeBag(Oids.Aes192);
            byte[] encoded = safeBag.Encode();
            Assert.NotNull(encoded);
        }

        [Fact]
        public static void TryEncodeBoundary()
        {
            TestSafeBag safeBag = new TestSafeBag(Oids.ContentType);
            byte[] encoded = safeBag.Encode();

            byte[] buf = new byte[encoded.Length + 4];
            buf.AsSpan().Fill(0xCA);

            Assert.False(safeBag.TryEncode(buf.AsSpan(0, encoded.Length - 1), out int bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.True(buf.All(b => b == 0xCA));
            
            Assert.True(safeBag.TryEncode(buf.AsSpan(1), out bytesWritten));
            Assert.Equal(encoded.Length, bytesWritten);
            Assert.Equal(0xCA, buf[0]);
            Assert.Equal(0xCA, buf[bytesWritten + 1]);
            Assert.True(encoded.AsSpan().SequenceEqual(buf.AsSpan(1, bytesWritten)));

            buf.AsSpan().Fill(0xCA);
            Assert.True(safeBag.TryEncode(buf.AsSpan(2, bytesWritten), out bytesWritten));
            Assert.Equal(encoded.Length, bytesWritten);
            Assert.True(encoded.AsSpan().SequenceEqual(buf.AsSpan(2, bytesWritten)));
        }

        [Fact]
        public static void GetBagIdIsFactory()
        {
            Pkcs12SafeBag safeBag = new TestSafeBag(Oids.Aes192);
            Oid firstCall = safeBag.GetBagId();
            Oid secondCall = safeBag.GetBagId();
            Assert.NotSame(firstCall, secondCall);
            Assert.Equal(Oids.Aes192, firstCall.Value);
            Assert.Equal(firstCall.Value, secondCall.Value);

            secondCall.Value = Oids.Cms3DesWrap;
            Assert.NotEqual(firstCall.Value, secondCall.Value);
            Assert.Equal(Oids.Aes192, firstCall.Value);
        }

        [Fact]
        public static void AttributesIsMutable()
        {
            Pkcs12SafeBag safeBag = new TestSafeBag(Oids.Aes192);
            CryptographicAttributeObjectCollection firstCall = safeBag.Attributes;
            Assert.Same(firstCall, safeBag.Attributes);

            Assert.Equal(0, firstCall.Count);
            firstCall.Add(new Pkcs9DocumentDescription("Description"));

            Assert.Equal(1, safeBag.Attributes.Count);
            Assert.Same(firstCall, safeBag.Attributes);
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
        public static void BaseClassVerifiesSingleBer(string inputHex, bool expectSuccess)
        {
            byte[] inputBytes = inputHex.HexToByteArray();
            Func<TestSafeBag> func = () => new TestSafeBag(Oids.BasicConstraints2, inputBytes);

            if (expectSuccess)
            {
                TestSafeBag bag = func();
                Assert.True(bag.EncodedBagValue.Span.SequenceEqual(inputBytes));
            }
            else
            {
                Assert.ThrowsAny<CryptographicException>(func);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void BaseClassHonorsSkipCopy(bool skipCopy)
        {
            byte[] test = { 0x05, 0x00 };
            Pkcs12SafeBag bag = new TestSafeBag(Oids.ContentType, test, skipCopy);
            bool isSame = test.AsSpan().Overlaps(bag.EncodedBagValue.Span);

            if (skipCopy)
            {
                Assert.True(isSame, "Is same memory");
            }
            else
            {
                Assert.False(isSame, "Is same memory");
            }
        }

        private class TestSafeBag : Pkcs12SafeBag
        {
            private static readonly ReadOnlyMemory<byte> s_derNull = new byte[] { 0x05, 0x00 };

            public TestSafeBag(string bagIdValue) : base(bagIdValue, s_derNull, skipCopy: true)
            {
            }

            public TestSafeBag(string bagIdValue, ReadOnlyMemory<byte> encodedValue, bool skipCopy = true)
                : base(bagIdValue, encodedValue, skipCopy)
            {
            }
        }
    }
}
