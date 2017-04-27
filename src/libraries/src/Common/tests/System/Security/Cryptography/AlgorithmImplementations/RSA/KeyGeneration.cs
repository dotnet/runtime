// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    public class KeyGeneration
    {
        [Fact]
        public static void GenerateMinKey()
        {
            GenerateKey(rsa => GetMin(rsa.LegalKeySizes));
        }

        [Fact]
        public static void GenerateSecondMinKey()
        {
            GenerateKey(rsa => GetSecondMin(rsa.LegalKeySizes));
        }

        // This test method takes approximately 1600 seconds to execute, so it is not on by default.
        public static void GenerateMaxKey()
        {
            GenerateKey(rsa => GetMax(rsa.LegalKeySizes));
        }

        [Fact]
        public static void GenerateKey_2048()
        {
            GenerateKey(2048);
        }

        [Fact]
        public static void GenerateKey_4096()
        {
            GenerateKey(4096);
        }

        private static void GenerateKey(int size)
        {
            GenerateKey(rsa => size);
        }

        private static void GenerateKey(Func<RSA, int> getSize)
        {
            int keySize;

            using (RSA rsa = RSAFactory.Create())
            {
                keySize = getSize(rsa);
            }

            using (RSA rsa = RSAFactory.Create(keySize))
            {
                Assert.Equal(keySize, rsa.KeySize);

                // Some providers may generate the key in the constructor, but
                // all of them should have generated it before answering ExportParameters.
                RSAParameters keyParameters = rsa.ExportParameters(false);
                ImportExport.ValidateParameters(ref keyParameters);

                // KeySize should still be what we set it to originally.
                Assert.Equal(keySize, rsa.KeySize);

                // KeySize describes the size of the modulus in bits
                // So, 8 * the number of bytes in the modulus should be the same value.
                Assert.Equal(keySize, keyParameters.Modulus.Length * 8);
            }
        }

        private static int GetMin(KeySizes[] keySizes)
        {
            int min = int.MaxValue;

            foreach (var keySize in keySizes)
            {
                if (keySize.MinSize < min)
                {
                    min = keySize.MinSize;
                }
            }

            return min;
        }

        private static int GetSecondMin(KeySizes[] keySizes)
        {
            int secondMin = int.MaxValue;
            int min = secondMin;

            foreach (var keySize in keySizes)
            {
                int localMin = keySize.MinSize;

                if (localMin < min)
                {
                    secondMin = min;
                    min = localMin;
                }
                else if (localMin < secondMin)
                {
                    secondMin = localMin;
                }

                if (keySize.MaxSize != keySize.MinSize)
                {
                    int secondLocal = localMin + keySize.SkipSize;

                    if (secondLocal < secondMin)
                    {
                        secondMin = secondLocal;
                    }
                }
            }

            return secondMin;
        }

        private static int GetMax(KeySizes[] keySizes)
        {
            int max = 0;

            foreach (var keySize in keySizes)
            {
                if (keySize.MaxSize > max)
                {
                    max = keySize.MaxSize;
                }
            }

            return max;
        }
    }
}
