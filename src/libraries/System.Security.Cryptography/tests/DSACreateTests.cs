// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Dsa.Tests;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(DSAFactory), nameof(DSAFactory.IsSupported))]
    public static class DSACreateTests
    {
        [Theory]
        [SkipOnPlatform(TestPlatforms.Android, "Android only supports key sizes that are a multiple of 1024")]
        [InlineData(512)]
        [InlineData(960)]
        public static void CreateWithKeysize_SmallKeys(int keySizeInBits)
        {
            using (DSA dsa = DSA.Create(keySizeInBits))
            {
                Assert.Equal(keySizeInBits, dsa.KeySize);

                DSAParameters parameters = dsa.ExportParameters(false);
                Assert.Equal(keySizeInBits, parameters.Y.Length << 3);
                Assert.Equal(keySizeInBits, dsa.KeySize);
            }
        }

        [Fact]
        public static void CreateWithKeysize()
        {
            const int KeySizeInBits = 1024;

            using (DSA dsa = DSA.Create(KeySizeInBits))
            {
                Assert.Equal(KeySizeInBits, dsa.KeySize);

                DSAParameters parameters = dsa.ExportParameters(false);
                Assert.Equal(KeySizeInBits, parameters.Y.Length << 3);
                Assert.Equal(KeySizeInBits, dsa.KeySize);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Android, "Android only supports key sizes that are a multiple of 1024")]
        public static void CreateWithKeysize_BigKey()
        {
            const int KeySizeInBits = 1088;

            using (DSA dsa = DSA.Create(KeySizeInBits))
            {
                Assert.Equal(KeySizeInBits, dsa.KeySize);

                DSAParameters parameters = dsa.ExportParameters(false);
                Assert.Equal(KeySizeInBits, parameters.Y.Length << 3);
                Assert.Equal(KeySizeInBits, dsa.KeySize);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(1023)]
        public static void CreateWithKeysize_InvalidKeySize(int keySizeInBits)
        {
            Assert.Throws<CryptographicException>(() => DSA.Create(keySizeInBits));
        }

        [Fact]
        public static void CreateWithParameters_512()
        {
            CreateWithParameters(DSATestData.Dsa512Parameters);
        }

        [Fact]
        public static void CreateWithParameters_1024()
        {
            CreateWithParameters(DSATestData.GetDSA1024Params());
        }

        [ConditionalFact(typeof(DSAFactory), nameof(DSAFactory.SupportsFips186_3))]
        public static void CreateWithParameters_2048()
        {
            CreateWithParameters(DSATestData.GetDSA2048Params());
        }

        private static void CreateWithParameters(DSAParameters parameters)
        {
            DSAParameters exportedPrivate;

            using (DSA dsa = DSA.Create(parameters))
            {
                exportedPrivate = dsa.ExportParameters(true);
            }

            DSAImportExport.AssertKeyEquals(parameters, exportedPrivate);
        }

        [Fact]
        public static void CreateWithInvalidParameters()
        {
            DSAParameters parameters = DSATestData.GetDSA1024Params();
            parameters.X = null;
            parameters.Y = null;

            AssertExtensions.Throws<ArgumentException>(null, () => DSA.Create(parameters));
        }
    }
}
