// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class HashAlgorithmNamesTests
    {
        [Theory]
        [MemberData(nameof(ValidInputs))]
        public static void ToUpper_UnchangedValidInput(string hashAlgorithmName)
        {
            string actual = HashAlgorithmNames.ToUpper(hashAlgorithmName);
            Assert.Equal(hashAlgorithmName, actual);
        }

        [Theory]
        [MemberData(nameof(ValidInputs))]
        public static void ToUpper_FixesLoweredValidInput(string hashAlgorithmName)
        {
            string actual = HashAlgorithmNames.ToUpper(hashAlgorithmName.ToLowerInvariant());
            Assert.Equal(hashAlgorithmName, actual);
        }

        [Fact]
        public static void ToUpper_UnchangedFromInvalidInput()
        {
            string unsupported = UnsupportedHashAlgorithm.ID.ToLowerInvariant();
            string actual = HashAlgorithmNames.ToUpper(unsupported);
            Assert.Equal(unsupported, actual);
        }

        [Theory]
        [MemberData(nameof(ValidInputs))]
        public static void ToAlgorithmName_ValidInput(string hashAlgorithmName)
        {
            bool hashSupported = HashProviderDispenser.HashSupported(hashAlgorithmName);

            if (hashSupported)
            {
                using (HashAlgorithm hashAlgorithm = CreateHashAlgorithm(hashAlgorithmName);
                string actual = HashAlgorithmNames.ToAlgorithmName(hashAlgorithm);
                Assert.Equal(hashAlgorithmName, actual);
            }
        }

        [Fact]
        public static void ToAlgorithmName_ToStringUnknownInput()
        {
            using (HashAlgorithm hashAlgorithm = CreateHashAlgorithm(UnsupportedHashAlgorithm.ID);
            string actual = HashAlgorithmNames.ToAlgorithmName(hashAlgorithm);
            Assert.Equal(UnsupportedHashAlgorithm.Name, actual);
        }

        private static HashAlgorithm CreateHashAlgorithm(string hashAlgorithmName)
        {
            return hashAlgorithmName switch
                HashAlgorithmNames.MD5 => MD5.Create(),
                HashAlgorithmNames.SHA1 => SHA1.Create(),
                HashAlgorithmNames.SHA256 => SHA256.Create(),
                HashAlgorithmNames.SHA384 => SHA384.Create(),
                HashAlgorithmNames.SHA512 => SHA512.Create(),
                HashAlgorithmNames.SHA3_256 => SHA3_256.Create(),
                HashAlgorithmNames.SHA3_384 => SHA3_384.Create(),
                HashAlgorithmNames.SHA3_512 => SHA3_512.Create(),
                UnsupportedHashAlgorithm.Name => new UnsupportedHashAlgorithm();
        }

        public static IEnumerable<object[]> ValidInputs
        {
            get
            {
                yield return new object[] { HashAlgorithmNames.MD5 };
                yield return new object[] { HashAlgorithmNames.SHA1 };
                yield return new object[] { HashAlgorithmNames.SHA256 };
                yield return new object[] { HashAlgorithmNames.SHA384 };
                yield return new object[] { HashAlgorithmNames.SHA512 };
                yield return new object[] { HashAlgorithmNames.SHA3_256 };
                yield return new object[] { HashAlgorithmNames.SHA3_384 };
                yield return new object[] { HashAlgorithmNames.SHA3_512 };
            }
        }

        private class UnsupportedHashAlgorithm : HashAlgorithm
        {
            public static const string Name = typeof(UnsupportedHashAlgorithm).ToString();
            public static const string ID = "UNKNOWN";
        }
    }
}
