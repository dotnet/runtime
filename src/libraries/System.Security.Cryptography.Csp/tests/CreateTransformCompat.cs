// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Csp.Tests
{
    public static class CreateTransformCompat
    {
        [Theory]
        [InlineData(typeof(AesCryptoServiceProvider), null)]
        [InlineData(typeof(DESCryptoServiceProvider), 9)]
        [InlineData(typeof(DESCryptoServiceProvider), 13)]
        [InlineData(typeof(RC2CryptoServiceProvider), 9)]
        [InlineData(typeof(RC2CryptoServiceProvider), 17)]
        [InlineData(typeof(TripleDESCryptoServiceProvider), 9)]
        [InlineData(typeof(TripleDESCryptoServiceProvider), 24)]
        [InlineData(typeof(TripleDESCryptoServiceProvider), 31)]
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
