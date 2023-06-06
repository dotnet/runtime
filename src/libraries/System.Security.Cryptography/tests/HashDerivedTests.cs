// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class HashDerivedTests
    {
        [Fact]
        public static void HashSize_SetForDerived_SHA1()
        {
            using DerivedSHA1 sha = new DerivedSHA1();
            Assert.Equal(SHA1.HashSizeInBits, sha.HashSize);
        }

        [Fact]
        public static void HashSize_SetForDerived_SHA256()
        {
            using DerivedSHA256 sha = new DerivedSHA256();
            Assert.Equal(SHA256.HashSizeInBits, sha.HashSize);
        }

        [Fact]
        public static void HashSize_SetForDerived_SHA384()
        {
            using DerivedSHA384 sha = new DerivedSHA384();
            Assert.Equal(SHA384.HashSizeInBits, sha.HashSize);
        }

        [Fact]
        public static void HashSize_SetForDerived_SHA512()
        {
            using DerivedSHA512 sha = new DerivedSHA512();
            Assert.Equal(SHA512.HashSizeInBits, sha.HashSize);
        }

        [Fact]
        [SkipOnMono("Not supported on Browser")]
        public static void HashSize_SetForDerived_MD5()
        {
            using DerivedMD5 sha = new DerivedMD5();
            Assert.Equal(MD5.HashSizeInBits, sha.HashSize);
        }

        [Fact]
        public static void HashSize_SetForDerived_SHA3_256()
        {
            using DerivedSHA3_256 sha = new DerivedSHA3_256();
            Assert.Equal(SHA3_256.HashSizeInBits, sha.HashSize);
        }

        [Fact]
        public static void HashSize_SetForDerived_SHA3_384()
        {
            using DerivedSHA3_384 sha = new DerivedSHA3_384();
            Assert.Equal(SHA3_384.HashSizeInBits, sha.HashSize);
        }

        [Fact]
        public static void HashSize_SetForDerived_SHA3_512()
        {
            using DerivedSHA3_512 sha = new DerivedSHA3_512();
            Assert.Equal(SHA3_512.HashSizeInBits, sha.HashSize);
        }

        private class DerivedSHA1 : SHA1
        {
            public override void Initialize() => throw null;
            protected override byte[] HashFinal() => throw null;
            protected override void HashCore(byte[] array, int ibStart, int cbSize) => throw null;
        }

        private class DerivedSHA256 : SHA256
        {
            public override void Initialize() => throw null;
            protected override byte[] HashFinal() => throw null;
            protected override void HashCore(byte[] array, int ibStart, int cbSize) => throw null;
        }

        private class DerivedSHA384 : SHA384
        {
            public override void Initialize() => throw null;
            protected override byte[] HashFinal() => throw null;
            protected override void HashCore(byte[] array, int ibStart, int cbSize) => throw null;
        }

        private class DerivedSHA512 : SHA512
        {
            public override void Initialize() => throw null;
            protected override byte[] HashFinal() => throw null;
            protected override void HashCore(byte[] array, int ibStart, int cbSize) => throw null;
        }

        private class DerivedSHA3_256 : SHA3_256
        {
            public override void Initialize() => throw null;
            protected override byte[] HashFinal() => throw null;
            protected override void HashCore(byte[] array, int ibStart, int cbSize) => throw null;
        }

        private class DerivedSHA3_384 : SHA3_384
        {
            public override void Initialize() => throw null;
            protected override byte[] HashFinal() => throw null;
            protected override void HashCore(byte[] array, int ibStart, int cbSize) => throw null;
        }

        private class DerivedSHA3_512 : SHA3_512
        {
            public override void Initialize() => throw null;
            protected override byte[] HashFinal() => throw null;
            protected override void HashCore(byte[] array, int ibStart, int cbSize) => throw null;
        }

        private class DerivedMD5 : MD5
        {
            public override void Initialize() => throw null;
            protected override byte[] HashFinal() => throw null;
            protected override void HashCore(byte[] array, int ibStart, int cbSize) => throw null;
        }
    }
}
