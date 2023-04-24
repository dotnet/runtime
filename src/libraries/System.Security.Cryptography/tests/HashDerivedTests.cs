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
            Assert.Equal(160, sha.HashSize);
        }

        [Fact]
        public static void HashSize_SetForDerived_SHA256()
        {
            using DerivedSHA256 sha = new DerivedSHA256();
            Assert.Equal(256, sha.HashSize);
        }

        [Fact]
        public static void HashSize_SetForDerived_SHA384()
        {
            using DerivedSHA384 sha = new DerivedSHA384();
            Assert.Equal(384, sha.HashSize);
        }

        [Fact]
        public static void HashSize_SetForDerived_SHA512()
        {
            using DerivedSHA512 sha = new DerivedSHA512();
            Assert.Equal(512, sha.HashSize);
        }

        [Fact]
        [SkipOnMono("Not supported on Browser")]
        public static void HashSize_SetForDerived_MD5()
        {
            using DerivedMD5 sha = new DerivedMD5();
            Assert.Equal(128, sha.HashSize);
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

        private class DerivedMD5 : MD5
        {
            public override void Initialize() => throw null;
            protected override byte[] HashFinal() => throw null;
            protected override void HashCore(byte[] array, int ibStart, int cbSize) => throw null;
        }

        private class DerivedHMACMD5 : HMACMD5
        {
            public override void Initialize() => throw null;
            protected override byte[] HashFinal() => throw null;
            protected override void HashCore(byte[] array, int ibStart, int cbSize) => throw null;
        }
    }
}
