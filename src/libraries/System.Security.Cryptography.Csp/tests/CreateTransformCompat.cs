// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Cryptography.Encryption.RC2.Tests;
using Xunit;

namespace System.Security.Cryptography.Csp.Tests
{
    public static class CreateTransformCompat
    {
        public static IEnumerable<object[]> CreateTransformTestData()
        {
            yield return new object[] { typeof(AesCryptoServiceProvider), null };
            yield return new object[] { typeof(DESCryptoServiceProvider), 9 };
            yield return new object[] { typeof(DESCryptoServiceProvider), 13 };

            if (RC2Factory.IsSupported)
            {
                yield return new object[] { typeof(RC2CryptoServiceProvider), 9 };
                yield return new object[] { typeof(RC2CryptoServiceProvider), 17 };
            }

            yield return new object[] { typeof(TripleDESCryptoServiceProvider), 9 };
            yield return new object[] { typeof(TripleDESCryptoServiceProvider), 24 };
            yield return new object[] { typeof(TripleDESCryptoServiceProvider), 31 };
        }

        [Theory]
        [MemberData(nameof(CreateTransformTestData))]
        public static void CreateTransform_IVTooBig(Type t, int? ivSizeBytes)
        {
            using (SymmetricAlgorithm alg = (SymmetricAlgorithm)Activator.CreateInstance(t))
            {
                alg.Mode = CipherMode.CBC;
                byte[] key = alg.Key;

                // If it isn't supposed to work
                if (ivSizeBytes == null)
                {
                    // badSize is in bytes, BlockSize is in bits.
                    // So badSize is 8 times as big as it should be.
                    int badSize = alg.BlockSize;
                    Assert.Throws<ArgumentException>(() => alg.CreateEncryptor(key, new byte[badSize]));
                    Assert.Throws<ArgumentException>(() => alg.CreateDecryptor(key, new byte[badSize]));

                    return;
                }

                int correctSize = alg.BlockSize / 8;
                byte[] data = { 1, 2, 3, 4, 5 };

                byte[] iv = new byte[ivSizeBytes.Value];

                for (int i = 0; i < iv.Length; i++)
                {
                    iv[i] = (byte)((byte.MaxValue - i) ^ correctSize);
                }

                byte[] correctIV = iv.AsSpan(0, correctSize).ToArray();

                using (ICryptoTransform correctEnc = alg.CreateEncryptor(key, correctIV))
                using (ICryptoTransform badIvEnc = alg.CreateEncryptor(key, iv))
                using (ICryptoTransform badIvDec = alg.CreateDecryptor(key, iv))
                {
                    byte[] encrypted = badIvEnc.TransformFinalBlock(data, 0, data.Length);
                    byte[] correctEncrypted = correctEnc.TransformFinalBlock(data, 0, data.Length);

                    Assert.Equal(correctEncrypted, encrypted);

                    byte[] decrypted1 = badIvDec.TransformFinalBlock(correctEncrypted, 0, correctEncrypted.Length);

                    Assert.Equal(data, decrypted1);
                }
            }
        }
    }
}
