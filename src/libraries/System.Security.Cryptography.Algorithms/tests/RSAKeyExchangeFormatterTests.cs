// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
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
        public static void RSAOAEPFormatterRng()
        {
            using (RSA key = RSA.Create())
            {
                RSAOAEPKeyExchangeFormatter keyex = new RSAOAEPKeyExchangeFormatter(key);
                Assert.Null(keyex.Rng);
                keyex.Rng = RandomNumberGenerator.Create();
                Assert.NotNull(keyex.Rng);
            }
        }

        [Fact]
        public static void RSAPKCS1FormatterRng()
        {
            using (RSA key = RSA.Create())
            {
                RSAPKCS1KeyExchangeFormatter keyex = new RSAPKCS1KeyExchangeFormatter(key);
                Assert.Null(keyex.Rng);
                keyex.Rng = RandomNumberGenerator.Create();
                Assert.NotNull(keyex.Rng);
            }
        }

        [Fact]
        public static void RSAPKCS1DeformatterRng()
        {
            using (RSA key = RSA.Create())
            {
                RSAPKCS1KeyExchangeDeformatter keyex = new RSAPKCS1KeyExchangeDeformatter(key);
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
