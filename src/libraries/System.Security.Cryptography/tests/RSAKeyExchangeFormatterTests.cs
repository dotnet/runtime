// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    public partial class RSAKeyExchangeFormatterTests
    {
        [Fact]
        public static void RSAOAEPFormatterArguments()
        {
            InvalidFormatterArguments(new RSAOAEPKeyExchangeFormatter());
        }

        [Fact]
        public static void RSAOAEPDeformatterArguments()
        {
            InvalidDeformatterArguments(new RSAOAEPKeyExchangeDeformatter());
        }

        [Fact]
        public static void RSAPKCS1FormatterArguments()
        {
            InvalidFormatterArguments(new RSAPKCS1KeyExchangeFormatter());
        }

        [Fact]
        public static void RSAPKCS1DeformatterArguments()
        {
            InvalidDeformatterArguments(new RSAPKCS1KeyExchangeDeformatter());
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void RSAOAEPFormatterRng()
        {
            using (RSALease lease = RSAKeyPool.Rent())
            {
                RSAOAEPKeyExchangeFormatter keyex = new RSAOAEPKeyExchangeFormatter(lease.Key);
                Assert.Null(keyex.Rng);
                keyex.Rng = RandomNumberGenerator.Create();
                Assert.NotNull(keyex.Rng);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void RSAPKCS1FormatterRng()
        {
            using (RSALease lease = RSAKeyPool.Rent())
            {
                RSAPKCS1KeyExchangeFormatter keyex = new RSAPKCS1KeyExchangeFormatter(lease.Key);
                Assert.Null(keyex.Rng);
                keyex.Rng = RandomNumberGenerator.Create();
                Assert.NotNull(keyex.Rng);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        public static void RSAPKCS1DeformatterRng()
        {
            using (RSALease lease = RSAKeyPool.Rent())
            {
                RSAPKCS1KeyExchangeDeformatter keyex = new RSAPKCS1KeyExchangeDeformatter(lease.Key);
                Assert.Null(keyex.RNG);
                keyex.RNG = RandomNumberGenerator.Create();
                Assert.NotNull(keyex.RNG);
            }
        }

        private static void InvalidFormatterArguments(AsymmetricKeyExchangeFormatter formatter)
        {
            Assert.Throws<ArgumentNullException>(() => formatter.SetKey(null));
            Assert.Throws<CryptographicUnexpectedOperationException>(() => formatter.CreateKeyExchange(new byte[] { 0, 1, 2, 3 }));
        }

        private static void InvalidDeformatterArguments(AsymmetricKeyExchangeDeformatter deformatter)
        {
            Assert.Throws<ArgumentNullException>(() => deformatter.SetKey(null));
            Assert.Throws<CryptographicUnexpectedOperationException>(() => deformatter.DecryptKeyExchange(new byte[] { 0, 1, 2 }));
        }
    }
}
