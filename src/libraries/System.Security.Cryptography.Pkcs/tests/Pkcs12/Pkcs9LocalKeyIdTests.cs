// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests.Pkcs12
{
    public static class Pkcs9LocalKeyIdTests
    {
        [Fact]
        public static void DefaultCtor()
        {
            Pkcs9LocalKeyId localKeyId = new Pkcs9LocalKeyId();
            Assert.Equal(0, localKeyId.KeyId.Length);

            Oid oid = localKeyId.Oid;
            Assert.NotNull(oid);
            Assert.Equal(Oids.LocalKeyId, oid.Value);

            Assert.Null(localKeyId.RawData);
        }

        [Fact]
        public static void KeyIdCtorAcceptsEmpty()
        {
            Pkcs9LocalKeyId localKeyId = new Pkcs9LocalKeyId(ReadOnlySpan<byte>.Empty);
            Assert.Equal(0, localKeyId.KeyId.Length);

            Oid oid = localKeyId.Oid;
            Assert.NotNull(oid);
            Assert.Equal(Oids.LocalKeyId, oid.Value);

            Assert.Equal("0400", localKeyId.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void KeyIdCtorPreservesValue()
        {
            byte[] keyId = { 2, 3, 5, 7, 11, 13, 17, 19, 23 };
            string keyIdHex = keyId.ByteArrayToHex();

            Pkcs9LocalKeyId localKeyId = new Pkcs9LocalKeyId(keyId);
            Assert.Equal(keyIdHex, localKeyId.KeyId.ByteArrayToHex());

            Oid oid = localKeyId.Oid;
            Assert.NotNull(oid);
            Assert.Equal(Oids.LocalKeyId, oid.Value);

            Assert.Equal(
                $"04{keyId.Length:X2}{keyIdHex}",
                localKeyId.RawData.ByteArrayToHex());
        }

        [Theory]
        [InlineData("040301")]
        [InlineData("3000")]
        [InlineData("04010203")]
        [InlineData("030100")]
        public static void KeyIdFromInvalidData(string invalidHex)
        {
            var attr = new Pkcs9AttributeObject(Oids.LocalKeyId, invalidHex.HexToByteArray());
            Pkcs9LocalKeyId localKeyId = new Pkcs9LocalKeyId();
            localKeyId.CopyFrom(attr);

            Assert.ThrowsAny<CryptographicException>(() => localKeyId.KeyId);
        }

        [Theory]
        [InlineData("0400", "")]
        [InlineData("040440302010", "40302010")]
        [InlineData("248004030100010000", "010001")]
        public static void KeyIdFromRawData(string inputHex, string expectedHex)
        {
            var attr = new Pkcs9AttributeObject(Oids.LocalKeyId, inputHex.HexToByteArray());
            Pkcs9LocalKeyId localKeyId = new Pkcs9LocalKeyId();
            localKeyId.CopyFrom(attr);

            Assert.Equal(expectedHex, localKeyId.KeyId.ByteArrayToHex());
        }
    }
}
