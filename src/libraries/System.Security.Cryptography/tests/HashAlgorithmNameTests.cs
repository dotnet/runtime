// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.Primitives.Tests
{
    public static class HashAlgorithmNameTests
    {
        [Theory]
        [MemberData(nameof(ValidInputs))]
        public static void FromOid_ValidInput(string oid, HashAlgorithmName expected)
        {
            Assert.Equal(expected, HashAlgorithmName.FromOid(oid));
        }

        [Fact]
        public static void FromOid_ThrowsForNullInput()
        {
            Assert.Throws<ArgumentNullException>(() => HashAlgorithmName.FromOid(null));
        }

        [Fact]
        public static void FromOid_ThrowsForInvalidInput()
        {
            CryptographicException exception = Assert.Throws<CryptographicException>(() => HashAlgorithmName.FromOid("1.2.3.4"));
            Assert.Contains("1.2.3.4", exception.Message);
        }

        [Fact]
        public static void TryFromOid_ThrowsForNullInput()
        {
            Assert.Throws<ArgumentNullException>(() => HashAlgorithmName.TryFromOid(null, out _));
        }

        [Theory]
        [MemberData(nameof(ValidInputs))]
        public static void TryFromOid_ValidInput(string oid, HashAlgorithmName expected)
        {
            Assert.True(HashAlgorithmName.TryFromOid(oid, out HashAlgorithmName actual));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("1.2.3.4")]
        [InlineData("SHA1")]
        [InlineData("1.2.840.113549.1.1.5")]
        public static void TryFromOid_ReturnsFalseForInvalidInput(string oidValue)
        {
            Assert.False(HashAlgorithmName.TryFromOid(oidValue, out _));
        }

        public static IEnumerable<object[]> ValidInputs
        {
            get
            {
                yield return new object[] { "1.2.840.113549.2.5", HashAlgorithmName.MD5 };
                yield return new object[] { "1.3.14.3.2.26", HashAlgorithmName.SHA1 };
                yield return new object[] { "2.16.840.1.101.3.4.2.1", HashAlgorithmName.SHA256 };
                yield return new object[] { "2.16.840.1.101.3.4.2.2", HashAlgorithmName.SHA384 };
                yield return new object[] { "2.16.840.1.101.3.4.2.3", HashAlgorithmName.SHA512 };
            }
        }
    }
}
