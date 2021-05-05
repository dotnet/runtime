// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Dsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst, "Not supported on Browser/iOS/tvOS/MacCatalyst")]
    public partial class DSAKeyGeneration
    {
        public static bool SupportsKeyGeneration => DSAFactory.SupportsKeyGeneration;
        public static bool HasSecondMinSize { get; } = GetHasSecondMinSize();

        [Fact]
        public static void VerifyDefaultKeySize_Fips186_2()
        {
            if (!DSAFactory.SupportsFips186_3)
            {
                using (DSA dsa = DSAFactory.Create())
                {
                    Assert.True(dsa.KeySize <= 1024); // KeySize must be <= 1024 for FIPS 186-2
                }
            }
        }

        [ConditionalFact(nameof(SupportsKeyGeneration))]
        public static void GenerateMinKey()
        {
            GenerateKey(dsa => GetMin(dsa.LegalKeySizes));
        }

        [ConditionalFact(nameof(SupportsKeyGeneration), nameof(HasSecondMinSize))]
        public static void GenerateSecondMinKey()
        {
            GenerateKey(dsa => GetSecondMin(dsa.LegalKeySizes));
        }

        [ConditionalFact(nameof(SupportsKeyGeneration))]
        public static void GenerateKey_1024()
        {
            GenerateKey(1024);
        }

        private static void GenerateKey(int size)
        {
            GenerateKey(dsa => size);
        }

        private static void GenerateKey(Func<DSA, int> getSize)
        {
            int keySize;

            using (DSA dsa = DSAFactory.Create())
            {
                keySize = getSize(dsa);
            }

            using (DSA dsa = DSAFactory.Create(keySize))
            {
                Assert.Equal(keySize, dsa.KeySize);

                // Some providers may generate the key in the constructor, but
                // all of them should have generated it before answering ExportParameters.
                DSAParameters keyParameters = dsa.ExportParameters(false);
                DSAImportExport.ValidateParameters(ref keyParameters);

                // KeySize should still be what we set it to originally.
                Assert.Equal(keySize, dsa.KeySize);

                dsa.ImportParameters(keyParameters);
                Assert.Equal(keySize, dsa.KeySize);
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

        private static bool GetHasSecondMinSize()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                return GetSecondMin(dsa.LegalKeySizes) != int.MaxValue;
            }
        }
    }
}
