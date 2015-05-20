// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Security.Cryptography.Hashing.Tests
{
    public class KeyedHashAlgorithmTest
    {
        [Fact]
        public void EnsureKeyIsolation()
        {
            byte[] key = new[] { (byte)0x00, (byte)0x01, (byte)0x02, (byte)0x03, (byte)0x04, (byte)0x05, };
            byte[] keyCopy = (byte[])key.Clone();

            using (var keyedHash = new TestKeyedHashAlgorithm())
            {
                keyedHash.Key = key;

                key[0]++;

                byte[] hashKey = keyedHash.Key;

                Assert.Equal(keyCopy, hashKey);
                Assert.NotEqual(key, hashKey);
            }
        }

        [Fact]
        public void EnsureGetKeyCopies()
        {
            byte[] key = new[] { (byte)0x00, (byte)0x01, (byte)0x02, (byte)0x03, (byte)0x04, (byte)0x05, };

            using (var keyedHash = new TestKeyedHashAlgorithm())
            {
                keyedHash.Key = key;

                byte[] getKey1 = keyedHash.Key;
                byte[] getKey2 = keyedHash.Key;

                Assert.NotSame(getKey1, getKey2);
                Assert.Equal(getKey1, getKey2);
                Assert.Equal(key, getKey1);

                getKey1[0]++;
                Assert.NotEqual(getKey1, getKey2);
            }
        }

        [Fact]
        public void EnsureDisposeFreesKey()
        {
            byte[] key = new[] { (byte)0x00, (byte)0x01, (byte)0x02, (byte)0x03, (byte)0x04, (byte)0x05, };

            using (var keyedHash = new TestKeyedHashAlgorithm())
            {
                keyedHash.Key = key;

                Assert.NotNull(keyedHash.Key);

                keyedHash.Dispose();

                byte[] ignored;
                Assert.Throws<NullReferenceException>(() => ignored = keyedHash.Key);
            }
        }

        [Fact]
        public void SetKeyNull()
        {
            using (var keyedHash = new TestKeyedHashAlgorithm())
            {
                Assert.Throws<NullReferenceException>(() => keyedHash.Key = null);
            }
        }

        private class TestKeyedHashAlgorithm : KeyedHashAlgorithm
        {
            protected override void HashCore(byte[] array, int ibStart, int cbSize)
            {
            }

            protected override byte[] HashFinal()
            {
                return Array.Empty<byte>(); ;
            }

            public override void Initialize()
            {
            }
        }
    }
}