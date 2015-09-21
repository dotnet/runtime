// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.Des.Tests
{

    public static partial class DesTests
    {
        [Fact]
        public static void DesDefaultCtor()
        {
            using (DES des = new DESMinimal())
            {
                Assert.Equal(64, des.KeySize);
                Assert.Equal(64, des.BlockSize);
                Assert.Equal(CipherMode.CBC, des.Mode);
                Assert.Equal(PaddingMode.PKCS7, des.Padding);
            }
        }

        [Fact]
        public static void DesIsWeakPositive()
        {
            foreach (byte[] key in _knownWeakKeys)
            {
                bool isWeak = DES.IsWeakKey(key);
                Assert.True(isWeak);

                byte[] noParityKey = new byte[key.Length];
                for (int i = 0; i < key.Length; i++)
                {
                    noParityKey[i] = (byte)(key[i] & 0xfe);
                }

                isWeak = DES.IsWeakKey(key);
                Assert.True(isWeak);
            }
        }

        [Fact]
        public static void DesIsSemiWeakPositive()
        {
            foreach (byte[] key in _knownSemiWeakKeys)
            {
                bool isSemiWeak = DES.IsSemiWeakKey(key);
                Assert.True(isSemiWeak);

                byte[] noParityKey = key.RemoveDesParityBits();
                isSemiWeak = DES.IsSemiWeakKey(noParityKey);
                Assert.True(isSemiWeak);
            }
        }

        [Fact]
        public static void DesCannotSetWeakKey()
        {
            using (DES d = new DESMinimal())
            {
                foreach (byte[] key in BadKeys())
                {
                    Assert.Throws<CryptographicException>(() => d.Key = key);
                }
            }
        }

        private static byte[] RemoveDesParityBits(this byte[] key)
        {
            byte[] noParityKey = new byte[key.Length];
            for (int i = 0; i < key.Length; i++)
            {
                noParityKey[i] = (byte)(key[i] & 0xfe);
            }
            return noParityKey;
        }

        private static IEnumerable<byte[]> BadKeys()
        {
            foreach (byte[] key in _knownWeakKeys)
            {
                yield return key;
                yield return key.RemoveDesParityBits();
            }
            foreach (byte[] key in _knownSemiWeakKeys)
            {
                yield return key;
                yield return key.RemoveDesParityBits();
            }
        }

        private static byte[][] _knownWeakKeys =
        {
            "0101010101010101".HexToByteArray(),
            "fefefefefefefefe".HexToByteArray(),
            "1f1f1f1f0e0e0e0e".HexToByteArray(),
            "e0e0e0e0f1f1f1f1".HexToByteArray(),
        };

        private static byte[][] _knownSemiWeakKeys =
        {
            "01fe01fe01fe01fe".HexToByteArray(),
            "fe01fe01fe01fe01".HexToByteArray(),
            "1fe01fe00ef10ef1".HexToByteArray(),
            "e01fe01ff10ef10e".HexToByteArray(),
            "01e001e001f101f1".HexToByteArray(),
            "e001e001f101f101".HexToByteArray(),
            "1ffe1ffe0efe0efe".HexToByteArray(),
            "fe1ffe1ffe0efe0e".HexToByteArray(),
            "011f011f010e010e".HexToByteArray(),
            "1f011f010e010e01".HexToByteArray(),
            "e0fee0fef1fef1fe".HexToByteArray(),
            "fee0fee0fef1fef1".HexToByteArray(),
        };

        private sealed class DESMinimal : DES
        {
            public DESMinimal()
            {
            }

            public sealed override void GenerateIV()
            {
                throw new NotSupportedException();
            }

            public sealed override void GenerateKey()
            {
                throw new NotSupportedException();
            }

            public sealed override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
            {
                throw new NotSupportedException();
            }

            public sealed override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
            {
                throw new NotSupportedException();
            }
        }
    }
}
