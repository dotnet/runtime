// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public partial class RSAKeyExchangeFormatterTests
    {
        [Fact]
        public static void VerifyDecryptKeyExchangeOaep()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.RSA2048Params);

                var formatter = new RSAOAEPKeyExchangeFormatter(rsa);
                var deformatter = new RSAOAEPKeyExchangeDeformatter(rsa);
                VerifyDecryptKeyExchange(formatter, deformatter);
            }
        }

        [Fact]
        public static void VerifyDecryptKeyExchangePkcs1()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.RSA2048Params);

                var formatter = new RSAPKCS1KeyExchangeFormatter(rsa);
                var deformatter = new RSAPKCS1KeyExchangeDeformatter(rsa);
                VerifyDecryptKeyExchange(formatter, deformatter);
            }
        }

        [Fact]
        public static void TestKnownValueOaep()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.RSA1024Params);
                byte[] encrypted =
                    ( "19134ffba4025a1c651120ca07258a46e005a327c3927f615465060734dc0339114cabfd13803288883abf9329296a3e3a5cb1587927"
                    + "a6e8a2e736f0a756e342b4adb0f1de5bba9ba5faee30456fb7409678eb71a70185606eda3303d9425fbeb730ab7803bea50e208b563f"
                    + "e9bfa97a8966deefb211a3bd6abe08cd15e0b927").HexToByteArray();
                RSAOAEPKeyExchangeDeformatter deformatter = new RSAOAEPKeyExchangeDeformatter(rsa);
                byte[] plain = deformatter.DecryptKeyExchange(encrypted);
                byte[] expectedPlain = { 0x41, 0x42, 0x43 };
                Assert.Equal(expectedPlain, plain);
            }
        }

        [Fact]
        public static void TestKnownValuePkcs1()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.RSA1024Params);
                byte[] encrypted =
                    ( "7061adb87a8759f0a0dc6ece42f5b63bf186f845237c6b16bf824b303812486efbb8f5febb681902228a609d4330a6c21abf0fc0d271"
                    + "ba63d1d0d9e486668270c2dbf73ab33055dfc0b797938557b99c0e9a535605c0a4bceefe5a37594732bb566ab026e4e8d5ce47d0967d"
                    + "f1c66e7ee4d39d804f6d558670222d708f943eb0").HexToByteArray();
                RSAPKCS1KeyExchangeDeformatter deformatter = new RSAPKCS1KeyExchangeDeformatter(rsa);
                byte[] plain = deformatter.DecryptKeyExchange(encrypted);
                byte[] expectedPlain = { 0x41, 0x42, 0x43 };
                Assert.Equal(expectedPlain, plain);
            }
        }

        private static void VerifyDecryptKeyExchange(
            AsymmetricKeyExchangeFormatter formatter,
            AsymmetricKeyExchangeDeformatter deformatter)
        {
            byte[] encrypted = formatter.CreateKeyExchange(TestData.HelloBytes);
            byte[] decrypted = deformatter.DecryptKeyExchange(encrypted);
            Assert.Equal(TestData.HelloBytes, decrypted);

            encrypted[encrypted.Length - 1] ^= 0xff;

            try
            {
                byte[] invalidMessage = deformatter.DecryptKeyExchange(encrypted);

                // RSAEncryptionPadding.Pkcs1 has loose integrity checking, recognizing ~1/110000
                // messages as decryptable. So we only have a logic problem in our code if we produce
                // the original input again. (The odds of a random payload producing "Hello" for a
                // 2048-bit key are 1 in 49 quintillion (4.869e19)).
                //
                // Since we're basing "invalid" off of "valid" the odds will be different than true
                // random, but it's not obvious if they're better or worse.
                if (invalidMessage.SequenceEqual(TestData.HelloBytes))
                {
                    string msg = $"Decrypt was unexpectedly successful: {encrypted.ByteArrayToHex()}";

                    // Just in case the exception text gets trimmed from test logs, Console.WriteLine it.
                    Console.WriteLine(msg);
                    throw new InvalidOperationException(msg);
                }
            }
            catch (CryptographicException)
            {
                // Equivalent to Assert.ThrowsAny<CryptographicException>
            }
        }
    }
}
