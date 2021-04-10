// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Dsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public partial class DSAImportExport
    {
        public static bool SupportsFips186_3 => DSAFactory.SupportsFips186_3;
        public static bool SupportsKeyGeneration => DSAFactory.SupportsKeyGeneration;

        [ConditionalFact(nameof(SupportsKeyGeneration))]
        public static void ExportAutoKey()
        {
            DSAParameters privateParams;
            DSAParameters publicParams;
            int keySize;
            using (DSA dsa = DSAFactory.Create())
            {
                keySize = dsa.KeySize;

                // We've not done anything with this instance yet, but it should automatically
                // create the key, because we'll now asked about it.
                privateParams = dsa.ExportParameters(true);
                publicParams = dsa.ExportParameters(false);

                // It shouldn't be changing things when it generated the key.
                Assert.Equal(keySize, dsa.KeySize);
            }

            Assert.Null(publicParams.X);
            Assert.NotNull(privateParams.X);

            ValidateParameters(ref publicParams);
            ValidateParameters(ref privateParams);

            Assert.Equal(privateParams.G, publicParams.G);
            Assert.Equal(privateParams.Y, publicParams.Y);
        }

        [Fact]
        public static void Import_512()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(DSATestData.Dsa512Parameters);

                Assert.Equal(512, dsa.KeySize);
            }
        }

        [Fact]
        public static void Import_576()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(DSATestData.Dsa576Parameters);

                Assert.Equal(576, dsa.KeySize);
            }
        }

        [Fact]
        public static void Import_1024()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(DSATestData.GetDSA1024Params());

                Assert.Equal(1024, dsa.KeySize);
            }
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public static void Import_2048()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(DSATestData.GetDSA2048Params());

                Assert.Equal(2048, dsa.KeySize);
            }
        }

        [Fact]
        public static void MultiExport()
        {
            DSAParameters imported = DSATestData.GetDSA1024Params();

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(imported);

                DSAParameters exportedPrivate = dsa.ExportParameters(true);
                DSAParameters exportedPrivate2 = dsa.ExportParameters(true);
                DSAParameters exportedPublic = dsa.ExportParameters(false);
                DSAParameters exportedPublic2 = dsa.ExportParameters(false);
                DSAParameters exportedPrivate3 = dsa.ExportParameters(true);
                DSAParameters exportedPublic3 = dsa.ExportParameters(false);

                AssertKeyEquals(imported, exportedPrivate);

                ValidateParameters(ref exportedPublic);

                AssertKeyEquals(exportedPrivate, exportedPrivate2);
                AssertKeyEquals(exportedPrivate, exportedPrivate3);

                AssertKeyEquals(exportedPublic, exportedPublic2);
                AssertKeyEquals(exportedPublic, exportedPublic3);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void ImportRoundTrip(bool includePrivate)
        {
            DSAParameters imported = DSATestData.GetDSA1024Params();

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(imported);
                DSAParameters exported = dsa.ExportParameters(includePrivate);
                using (DSA dsa2 = DSAFactory.Create())
                {
                    dsa2.ImportParameters(exported);
                    DSAParameters exported2 = dsa2.ExportParameters(includePrivate);
                    AssertKeyEquals(in exported, in exported2);
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void ExportAfterDispose(bool importKey)
        {
            DSA key = importKey ? DSAFactory.Create(DSATestData.GetDSA1024Params()) : DSAFactory.Create(1024);
            byte[] hash = new byte[20];

            // Ensure that the key got created, and then Dispose it.
            using (key)
            {
                try
                {
                    key.CreateSignature(hash);
                }
                catch (PlatformNotSupportedException) when (!SupportsKeyGeneration)
                {
                }
            }

            Assert.Throws<ObjectDisposedException>(() => key.ExportParameters(false));
            Assert.Throws<ObjectDisposedException>(() => key.ExportParameters(true));
            Assert.Throws<ObjectDisposedException>(() => key.ImportParameters(DSATestData.GetDSA1024Params()));
        }

        internal static void AssertKeyEquals(in DSAParameters expected, in DSAParameters actual)
        {
            Assert.Equal(expected.G, actual.G);
            Assert.Equal(expected.P, actual.P);
            Assert.Equal(expected.Q, actual.Q);
            Assert.Equal(expected.Y, actual.Y);
            Assert.Equal(expected.X, actual.X);
        }

        internal static void ValidateParameters(ref DSAParameters dsaParams)
        {
            Assert.NotNull(dsaParams.G);
            Assert.True(dsaParams.G.Length > 0);

            Assert.NotNull(dsaParams.P);
            Assert.True(dsaParams.P.Length > 0);

            Assert.NotNull(dsaParams.Q);
            Assert.True(dsaParams.Q.Length > 0);

            Assert.True(dsaParams.Y.Length > 0);
            Assert.NotNull(dsaParams.Y);
        }
    }
}
