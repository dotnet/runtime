// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.XUnitExtensions;
using Test.Cryptography;
using Test.IO.Streams;
using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public sealed class SignVerify_Array : SignVerify
    {
        protected override byte[] SignData(RSA rsa, byte[] data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            rsa.SignData(data, hashAlgorithm, padding);
        protected override byte[] SignHash(RSA rsa, byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            rsa.SignHash(hash, hashAlgorithm, padding);
        protected override bool VerifyData(RSA rsa, byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            rsa.VerifyData(data, signature, hashAlgorithm, padding);
        protected override bool VerifyHash(RSA rsa, byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            rsa.VerifyHash(hash, signature, hashAlgorithm, padding);

        [Fact]
        public void NullArray_Throws()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                AssertExtensions.Throws<ArgumentNullException>("data", () => SignData(rsa, null, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
                AssertExtensions.Throws<ArgumentNullException>("hash", () => SignHash(rsa, null, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

                AssertExtensions.Throws<ArgumentNullException>("data", () => VerifyData(rsa, null, new byte[1], HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
                AssertExtensions.Throws<ArgumentNullException>("hash", () => VerifyHash(rsa, null, new byte[1], HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

                AssertExtensions.Throws<ArgumentNullException>("signature", () => VerifyData(rsa, new byte[1], null, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
                AssertExtensions.Throws<ArgumentNullException>("signature", () => VerifyHash(rsa, new byte[1], null, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            }
        }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public abstract class SignVerify
    {
        public static bool SupportsPss => RSAFactory.SupportsPss;

        protected abstract byte[] SignData(RSA rsa, byte[] data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding);
        protected abstract byte[] SignHash(RSA rsa, byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding);
        protected abstract bool VerifyData(RSA rsa, byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding);
        protected abstract bool VerifyHash(RSA rsa, byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding);

        [Fact]
        public void InvalidHashAlgorithmName_Throws()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                var invalidName = new HashAlgorithmName(null);
                AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => SignData(rsa, new byte[1], invalidName, RSASignaturePadding.Pkcs1));
                AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => SignHash(rsa, new byte[1], invalidName, RSASignaturePadding.Pkcs1));
                AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => VerifyData(rsa, new byte[1], new byte[1], invalidName, RSASignaturePadding.Pkcs1));
                AssertExtensions.Throws<ArgumentNullException>("hashAlgorithm", () => VerifyHash(rsa, new byte[1], new byte[1], invalidName, RSASignaturePadding.Pkcs1));

                invalidName = new HashAlgorithmName("");
                AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => SignData(rsa, new byte[1], invalidName, RSASignaturePadding.Pkcs1));
                AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => SignHash(rsa, new byte[1], invalidName, RSASignaturePadding.Pkcs1));
                AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => VerifyData(rsa, new byte[1], new byte[1], invalidName, RSASignaturePadding.Pkcs1));
                AssertExtensions.Throws<ArgumentException>("hashAlgorithm", () => VerifyHash(rsa, new byte[1], new byte[1], invalidName, RSASignaturePadding.Pkcs1));
            }
        }

        [Fact]
        public void NullPadding_Throws()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                AssertExtensions.Throws<ArgumentNullException>("padding", () => SignData(rsa, new byte[1], HashAlgorithmName.SHA256, null));
                AssertExtensions.Throws<ArgumentNullException>("padding", () => SignHash(rsa, new byte[1], HashAlgorithmName.SHA256, null));
                AssertExtensions.Throws<ArgumentNullException>("padding", () => VerifyData(rsa, new byte[1], new byte[1], HashAlgorithmName.SHA256, null));
                AssertExtensions.Throws<ArgumentNullException>("padding", () => VerifyHash(rsa, new byte[1], new byte[1], HashAlgorithmName.SHA256, null));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UseAfterDispose(bool importKey)
        {
            RSA rsa = importKey ? RSAFactory.Create(TestData.RSA2048Params) : RSAFactory.Create(1024);
            byte[] data = TestData.HelloBytes;
            byte[] sig;
            HashAlgorithmName alg = HashAlgorithmName.SHA256;
            RSASignaturePadding padding = RSASignaturePadding.Pkcs1;

            using (rsa)
            {
                sig = SignData(rsa, data, alg, padding);
            }

            Assert.Throws<ObjectDisposedException>(
                () => VerifyData(rsa, sig, data, alg, padding));

            Assert.Throws<ObjectDisposedException>(
                () => VerifyHash(rsa, sig, data, alg, padding));

            // Either set_KeySize or SignData should throw.
            Assert.Throws<ObjectDisposedException>(
                () =>
                {
                    rsa.KeySize = 1024 + 64;
                    SignData(rsa, data, alg, padding);
                });
        }

        [Fact]
        public void InvalidKeySize_DoesNotInvalidateKey()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                byte[] signature = SignData(rsa, TestData.HelloBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                // A 2049-bit key is hard to describe, none of the providers support it.
                Assert.ThrowsAny<CryptographicException>(() => rsa.KeySize = 2049);

                Assert.True(VerifyData(rsa, TestData.HelloBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            }
        }

        [Fact]
        public void PublicKey_CannotSign()
        {
            using (RSA rsa = RSAFactory.Create())
            using (RSA rsaPub = RSAFactory.Create())
            {
                rsaPub.ImportParameters(rsa.ExportParameters(false));

                Assert.ThrowsAny<CryptographicException>(
                    () => rsaPub.SignData(TestData.HelloBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            }
        }

        [Fact]
        public void SignEmptyHash()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => SignHash(rsa, Array.Empty<byte>(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            }
        }

        [ConditionalFact(typeof(RSAFactory), nameof(RSAFactory.SupportsSha1Signatures))]
        public void ExpectedSignature_SHA1_384()
        {
            byte[] expectedSignature =
            {
                0x79, 0xD9, 0x3C, 0xBF, 0x54, 0xFA, 0x55, 0x8C,
                0x44, 0xC3, 0xC3, 0x83, 0x85, 0xBB, 0x78, 0x44,
                0xCD, 0x0F, 0x5A, 0x8E, 0x71, 0xC9, 0xC2, 0x68,
                0x68, 0x0A, 0x33, 0x93, 0x19, 0x37, 0x02, 0x06,
                0xE2, 0xF7, 0x67, 0x97, 0x3C, 0x67, 0xB3, 0xF4,
                0x11, 0xE0, 0x6E, 0xD2, 0x22, 0x75, 0xE7, 0x7C,
            };

            try
            {
                ExpectSignature(expectedSignature, TestData.HelloBytes, "SHA1", TestData.RSA384Parameters);

                Assert.True(RSAFactory.Supports384PrivateKey, "RSAFactory.Supports384PrivateKey");
            }
            catch (CryptographicException)
            {
                // If the provider is not known to fail loading a 384-bit key, let the exception be the
                // test failure. (If it is known to fail loading that key, we've now suppressed the throw,
                // and the test will pass.)
                if (RSAFactory.Supports384PrivateKey)
                {
                    throw;
                }
            }
        }

        [ConditionalFact(typeof(RSAFactory), nameof(RSAFactory.SupportsSha1Signatures))]
        public void ExpectedSignature_SHA1_1032()
        {
            byte[] expectedSignature =
            {
                0x49, 0xBC, 0x1C, 0xBE, 0x72, 0xEF, 0x83, 0x6E,
                0x2D, 0xFA, 0xE7, 0xFA, 0xEB, 0xBC, 0xF0, 0x16,
                0xF7, 0x2C, 0x07, 0x6D, 0x9F, 0xA6, 0x68, 0x71,
                0xDC, 0x78, 0x9C, 0xA3, 0x42, 0x9E, 0xBB, 0xF5,
                0x72, 0xE0, 0xAB, 0x4B, 0x4B, 0x6A, 0xE7, 0x3C,
                0xE2, 0xC8, 0x1F, 0xA2, 0x07, 0xED, 0xD3, 0x98,
                0xE9, 0xDF, 0x9A, 0x7A, 0x86, 0xB8, 0x06, 0xED,
                0x97, 0x46, 0xF9, 0x8A, 0xED, 0x53, 0x1D, 0x90,
                0xC3, 0x57, 0x7E, 0x5A, 0xE4, 0x7C, 0xEC, 0xB9,
                0x45, 0x95, 0xAB, 0xCC, 0xBA, 0x9B, 0x2C, 0x1A,
                0x64, 0xC2, 0x2C, 0xA0, 0x36, 0x7C, 0x56, 0xF0,
                0x78, 0x77, 0x0B, 0x27, 0xB8, 0x1C, 0xCA, 0x7D,
                0xD4, 0x71, 0x37, 0xBF, 0xC6, 0x4C, 0x64, 0x76,
                0xBC, 0x8A, 0x87, 0xA0, 0x81, 0xF9, 0x4A, 0x94,
                0x7B, 0xAA, 0x80, 0x95, 0x47, 0x51, 0xF9, 0x02,
                0xA3, 0x44, 0x5C, 0x56, 0x60, 0xFB, 0x94, 0xA8,
                0x52,
            };

            ExpectSignature(expectedSignature, TestData.HelloBytes, "SHA1", TestData.RSA1032Parameters);
        }

        [ConditionalFact(typeof(RSAFactory), nameof(RSAFactory.SupportsSha1Signatures))]
        public void ExpectedSignature_SHA1_2048()
        {
            byte[] expectedSignature = new byte[]
            {
                0xA1, 0xFC, 0x74, 0x67, 0x49, 0x91, 0xF4, 0x28,
                0xB0, 0xF6, 0x2B, 0xB8, 0x5E, 0x5F, 0x2E, 0x0F,
                0xD8, 0xBC, 0xB4, 0x6E, 0x0A, 0xF7, 0x11, 0xC2,
                0x65, 0x35, 0x5C, 0x1B, 0x1B, 0xC1, 0x20, 0xC0,
                0x7D, 0x5B, 0x98, 0xAF, 0xB4, 0xC1, 0x6A, 0x25,
                0x17, 0x47, 0x2C, 0x7F, 0x20, 0x2A, 0xDD, 0xF0,
                0x5F, 0xDF, 0x6F, 0x5B, 0x7D, 0xEE, 0xAA, 0x4B,
                0x9E, 0x8B, 0xA6, 0x0D, 0x81, 0x54, 0x93, 0x6E,
                0xB2, 0x86, 0xC8, 0x14, 0x4F, 0xE7, 0x4A, 0xCC,
                0xBE, 0x51, 0x2D, 0x0B, 0x9B, 0x46, 0xF1, 0x39,
                0x80, 0x1D, 0xD0, 0x07, 0xBA, 0x46, 0x48, 0xFC,
                0x7A, 0x50, 0x17, 0xC9, 0x7F, 0xEF, 0xDD, 0x42,
                0xC5, 0x8B, 0x69, 0x38, 0x67, 0xAB, 0xBD, 0x39,
                0xA6, 0xF4, 0x02, 0x34, 0x88, 0x56, 0x50, 0x05,
                0xEA, 0x95, 0x24, 0x7D, 0x34, 0xD9, 0x9F, 0xB1,
                0x05, 0x39, 0x6A, 0x42, 0x9E, 0x5E, 0xEB, 0xC9,
                0x90, 0xC1, 0x93, 0x63, 0x29, 0x0C, 0xC5, 0xBC,
                0xC8, 0x65, 0xB0, 0xFA, 0x63, 0x61, 0x77, 0xD9,
                0x16, 0x59, 0xF0, 0xAD, 0x28, 0xC7, 0x98, 0x3C,
                0x53, 0xF1, 0x6C, 0x91, 0x7E, 0x36, 0xC3, 0x3A,
                0x23, 0x87, 0xA7, 0x3A, 0x18, 0x18, 0xBF, 0xD2,
                0x3E, 0x51, 0x9E, 0xAB, 0x9E, 0x4C, 0x65, 0xBA,
                0x43, 0xC0, 0x7E, 0xA2, 0x6B, 0xCF, 0x69, 0x7C,
                0x8F, 0xAB, 0x22, 0x28, 0xD6, 0xF1, 0x65, 0x0B,
                0x4A, 0x5B, 0x9B, 0x1F, 0xD4, 0xAA, 0xEF, 0x35,
                0xA2, 0x42, 0x32, 0x00, 0x9F, 0x42, 0xBB, 0x19,
                0x99, 0x49, 0x6D, 0xB8, 0x03, 0x3D, 0x35, 0x96,
                0x0C, 0x57, 0xBB, 0x6B, 0x07, 0xA4, 0xB9, 0x7F,
                0x9B, 0xEC, 0x78, 0x90, 0xB7, 0xC8, 0x5E, 0x7F,
                0x3B, 0xAB, 0xC1, 0xB6, 0x0C, 0x84, 0x3C, 0xBC,
                0x7F, 0x04, 0x79, 0xB7, 0x9C, 0xC0, 0xFE, 0xB0,
                0xAE, 0xBD, 0xA5, 0x57, 0x2C, 0xEC, 0x3D, 0x0D,
            };

            ExpectSignature(expectedSignature, TestData.HelloBytes, "SHA1", TestData.RSA2048Params);
        }

        [Fact]
        public void ExpectedSignature_SHA256_1024()
        {
            byte[] expectedSignature = new byte[]
            {
                0x5C, 0x2F, 0x00, 0xA9, 0xE4, 0x63, 0xD7, 0xB7,
                0x94, 0x93, 0xCE, 0xA8, 0x7E, 0x71, 0xAE, 0x97,
                0xC2, 0x6B, 0x37, 0x31, 0x5B, 0xB8, 0xE3, 0x30,
                0xDF, 0x77, 0xF8, 0xBB, 0xB5, 0xBF, 0x41, 0x9F,
                0x14, 0x6A, 0x61, 0x26, 0x2E, 0x80, 0xE5, 0xE6,
                0x8A, 0xEA, 0xC7, 0x60, 0x0B, 0xAE, 0x2B, 0xB2,
                0x18, 0xD8, 0x5D, 0xC8, 0x58, 0x86, 0x5E, 0x23,
                0x62, 0x44, 0x72, 0xEA, 0x3B, 0xF7, 0x70, 0xC6,
                0x4C, 0x2B, 0x54, 0x5B, 0xF4, 0x24, 0xA1, 0xE5,
                0x63, 0xDD, 0x50, 0x3A, 0x29, 0x26, 0x84, 0x06,
                0xEF, 0x13, 0xD0, 0xCE, 0xCC, 0xA1, 0x05, 0xB4,
                0x72, 0x81, 0x0A, 0x2E, 0x33, 0xF6, 0x2F, 0xD1,
                0xEA, 0x41, 0xB0, 0xB3, 0x93, 0x4C, 0xF3, 0x0F,
                0x6F, 0x21, 0x3E, 0xD7, 0x5F, 0x57, 0x2E, 0xC7,
                0x5F, 0xF5, 0x28, 0x89, 0xB8, 0x07, 0xDB, 0xAC,
                0x70, 0x95, 0x25, 0x49, 0x8A, 0x1A, 0xD7, 0xFC,
            };

            ExpectSignature(expectedSignature, TestData.HelloBytes, "SHA256", TestData.RSA1024Params);
        }

        [Fact]
        public void ExpectedSignature_SHA256_2048()
        {
            byte[] expectedSignature = new byte[]
            {
                0x2C, 0x74, 0x98, 0x23, 0xF4, 0x38, 0x7F, 0x49,
                0x82, 0xB6, 0x55, 0xCF, 0xC3, 0x25, 0x4F, 0xE3,
                0x4B, 0x17, 0xE7, 0xED, 0xEA, 0x58, 0x1E, 0x63,
                0x57, 0x58, 0xCD, 0xB5, 0x06, 0xD6, 0xCA, 0x13,
                0x28, 0x81, 0xE6, 0xE0, 0x8B, 0xDC, 0xC6, 0x05,
                0x35, 0x35, 0x40, 0x73, 0x76, 0x61, 0x67, 0x42,
                0x94, 0xF7, 0x54, 0x0E, 0xB6, 0x30, 0x9A, 0x70,
                0xC3, 0x06, 0xC1, 0x59, 0xA7, 0x89, 0x66, 0x38,
                0x02, 0x5C, 0x52, 0x02, 0x17, 0x4E, 0xEC, 0x21,
                0xE9, 0x24, 0x85, 0xCB, 0x56, 0x42, 0xAB, 0x21,
                0x3A, 0x19, 0xC3, 0x95, 0x06, 0xBA, 0xDB, 0xD9,
                0x89, 0x7C, 0xB9, 0xEC, 0x1D, 0x8B, 0x5A, 0x64,
                0x87, 0xAF, 0x36, 0x71, 0xAC, 0x0A, 0x2B, 0xC7,
                0x7D, 0x2F, 0x44, 0xAA, 0xB4, 0x1C, 0xBE, 0x0B,
                0x0A, 0x4E, 0xEA, 0xF8, 0x75, 0x40, 0xD9, 0x4A,
                0x82, 0x1C, 0x82, 0x81, 0x97, 0xC2, 0xF1, 0xC8,
                0xA7, 0x4B, 0x45, 0x9A, 0x66, 0x8E, 0x35, 0x2E,
                0xE5, 0x1A, 0x2B, 0x0B, 0xF9, 0xAB, 0xC4, 0x2A,
                0xE0, 0x47, 0x72, 0x2A, 0xC2, 0xD8, 0xC6, 0xFD,
                0x91, 0x30, 0xD2, 0x45, 0xA4, 0x7F, 0x0F, 0x39,
                0x80, 0xBC, 0xA9, 0xBD, 0xEC, 0xA5, 0x03, 0x6F,
                0x01, 0xF6, 0x19, 0xD5, 0x2B, 0xD9, 0x40, 0xCD,
                0x7F, 0xEF, 0x0F, 0x9D, 0x93, 0x02, 0xCD, 0x89,
                0xB8, 0x2C, 0xC7, 0xD6, 0xFD, 0xAA, 0x12, 0x6E,
                0x4C, 0x06, 0x35, 0x08, 0x61, 0x79, 0x27, 0xE1,
                0xEA, 0x46, 0x75, 0x08, 0x5B, 0x51, 0xA1, 0x80,
                0x78, 0x02, 0xEA, 0x3E, 0xEC, 0x29, 0xD2, 0x8B,
                0xC5, 0x9E, 0x7D, 0xA4, 0x85, 0x8D, 0xAD, 0x73,
                0x39, 0x17, 0x64, 0x82, 0x46, 0x4A, 0xA4, 0x34,
                0xF0, 0xCC, 0x2F, 0x9F, 0x55, 0xA4, 0xEA, 0xEC,
                0xC9, 0xA7, 0xAB, 0xBA, 0xA8, 0x84, 0x14, 0x62,
                0x6B, 0x9B, 0x97, 0x2D, 0x8C, 0xB2, 0x1C, 0x16,
            };

            ExpectSignature(expectedSignature, TestData.HelloBytes, "SHA256", TestData.RSA2048Params);
        }

        [Fact]
        public void ExpectSignature_SHA256_1024_Stream()
        {
            byte[] expectedSignature = new byte[]
            {
                0x78, 0x6F, 0x42, 0x00, 0xF4, 0x5A, 0xDB, 0x09,
                0x72, 0xB9, 0xCD, 0xBE, 0xB8, 0x46, 0x54, 0xE0,
                0xCF, 0x02, 0xB5, 0xA1, 0xF1, 0x7C, 0xA7, 0x5A,
                0xCF, 0x09, 0x60, 0xB6, 0xFF, 0x6B, 0x8A, 0x92,
                0x8E, 0xB4, 0xD5, 0x2C, 0x64, 0x90, 0x3E, 0x38,
                0x8B, 0x1D, 0x7D, 0x0E, 0xE8, 0x3C, 0xF0, 0xB9,
                0xBB, 0xEF, 0x90, 0x49, 0x7E, 0x6A, 0x1C, 0xEC,
                0x51, 0xB9, 0x13, 0x9B, 0x02, 0x02, 0x66, 0x59,
                0xC6, 0xB1, 0x51, 0xBD, 0x17, 0x2E, 0x03, 0xEC,
                0x93, 0x2B, 0xE9, 0x41, 0x28, 0x57, 0x8C, 0xB2,
                0x42, 0x60, 0xDE, 0xB4, 0x18, 0x85, 0x81, 0x55,
                0xAE, 0x09, 0xD9, 0xC4, 0x87, 0x57, 0xD1, 0x90,
                0xB3, 0x18, 0xD2, 0x96, 0x18, 0x91, 0x2D, 0x38,
                0x98, 0x0E, 0x68, 0x3C, 0xA6, 0x2E, 0xFE, 0x0D,
                0xD0, 0x50, 0x18, 0x55, 0x75, 0xA9, 0x85, 0x40,
                0xAB, 0x72, 0xE6, 0x7F, 0x9F, 0xDC, 0x30, 0xB9,
            };

            byte[] signature;

            using (Stream stream = new PositionValueStream(10))
            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.RSA1024Params);
                signature = rsa.SignData(stream, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }

            Assert.Equal(expectedSignature, signature);
        }

        [ConditionalFact(typeof(RSAFactory), nameof(RSAFactory.SupportsSha1Signatures))]
        public void VerifySignature_SHA1_384()
        {
            byte[] signature =
            {
                0x79, 0xD9, 0x3C, 0xBF, 0x54, 0xFA, 0x55, 0x8C,
                0x44, 0xC3, 0xC3, 0x83, 0x85, 0xBB, 0x78, 0x44,
                0xCD, 0x0F, 0x5A, 0x8E, 0x71, 0xC9, 0xC2, 0x68,
                0x68, 0x0A, 0x33, 0x93, 0x19, 0x37, 0x02, 0x06,
                0xE2, 0xF7, 0x67, 0x97, 0x3C, 0x67, 0xB3, 0xF4,
                0x11, 0xE0, 0x6E, 0xD2, 0x22, 0x75, 0xE7, 0x7C,
            };

            VerifySignature(signature, TestData.HelloBytes, "SHA1", TestData.RSA384Parameters);
        }

        [ConditionalFact(typeof(RSAFactory), nameof(RSAFactory.SupportsSha1Signatures))]
        public void VerifySignature_SHA1_1032()
        {
            byte[] signature =
            {
                0x49, 0xBC, 0x1C, 0xBE, 0x72, 0xEF, 0x83, 0x6E,
                0x2D, 0xFA, 0xE7, 0xFA, 0xEB, 0xBC, 0xF0, 0x16,
                0xF7, 0x2C, 0x07, 0x6D, 0x9F, 0xA6, 0x68, 0x71,
                0xDC, 0x78, 0x9C, 0xA3, 0x42, 0x9E, 0xBB, 0xF5,
                0x72, 0xE0, 0xAB, 0x4B, 0x4B, 0x6A, 0xE7, 0x3C,
                0xE2, 0xC8, 0x1F, 0xA2, 0x07, 0xED, 0xD3, 0x98,
                0xE9, 0xDF, 0x9A, 0x7A, 0x86, 0xB8, 0x06, 0xED,
                0x97, 0x46, 0xF9, 0x8A, 0xED, 0x53, 0x1D, 0x90,
                0xC3, 0x57, 0x7E, 0x5A, 0xE4, 0x7C, 0xEC, 0xB9,
                0x45, 0x95, 0xAB, 0xCC, 0xBA, 0x9B, 0x2C, 0x1A,
                0x64, 0xC2, 0x2C, 0xA0, 0x36, 0x7C, 0x56, 0xF0,
                0x78, 0x77, 0x0B, 0x27, 0xB8, 0x1C, 0xCA, 0x7D,
                0xD4, 0x71, 0x37, 0xBF, 0xC6, 0x4C, 0x64, 0x76,
                0xBC, 0x8A, 0x87, 0xA0, 0x81, 0xF9, 0x4A, 0x94,
                0x7B, 0xAA, 0x80, 0x95, 0x47, 0x51, 0xF9, 0x02,
                0xA3, 0x44, 0x5C, 0x56, 0x60, 0xFB, 0x94, 0xA8,
                0x52,
            };

            VerifySignature(signature, TestData.HelloBytes, "SHA1", TestData.RSA1032Parameters);
        }

        [ConditionalFact(typeof(RSAFactory), nameof(RSAFactory.SupportsSha1Signatures))]
        public void VerifySignature_SHA1_2048()
        {
            byte[] signature = new byte[]
            {
                0xA1, 0xFC, 0x74, 0x67, 0x49, 0x91, 0xF4, 0x28,
                0xB0, 0xF6, 0x2B, 0xB8, 0x5E, 0x5F, 0x2E, 0x0F,
                0xD8, 0xBC, 0xB4, 0x6E, 0x0A, 0xF7, 0x11, 0xC2,
                0x65, 0x35, 0x5C, 0x1B, 0x1B, 0xC1, 0x20, 0xC0,
                0x7D, 0x5B, 0x98, 0xAF, 0xB4, 0xC1, 0x6A, 0x25,
                0x17, 0x47, 0x2C, 0x7F, 0x20, 0x2A, 0xDD, 0xF0,
                0x5F, 0xDF, 0x6F, 0x5B, 0x7D, 0xEE, 0xAA, 0x4B,
                0x9E, 0x8B, 0xA6, 0x0D, 0x81, 0x54, 0x93, 0x6E,
                0xB2, 0x86, 0xC8, 0x14, 0x4F, 0xE7, 0x4A, 0xCC,
                0xBE, 0x51, 0x2D, 0x0B, 0x9B, 0x46, 0xF1, 0x39,
                0x80, 0x1D, 0xD0, 0x07, 0xBA, 0x46, 0x48, 0xFC,
                0x7A, 0x50, 0x17, 0xC9, 0x7F, 0xEF, 0xDD, 0x42,
                0xC5, 0x8B, 0x69, 0x38, 0x67, 0xAB, 0xBD, 0x39,
                0xA6, 0xF4, 0x02, 0x34, 0x88, 0x56, 0x50, 0x05,
                0xEA, 0x95, 0x24, 0x7D, 0x34, 0xD9, 0x9F, 0xB1,
                0x05, 0x39, 0x6A, 0x42, 0x9E, 0x5E, 0xEB, 0xC9,
                0x90, 0xC1, 0x93, 0x63, 0x29, 0x0C, 0xC5, 0xBC,
                0xC8, 0x65, 0xB0, 0xFA, 0x63, 0x61, 0x77, 0xD9,
                0x16, 0x59, 0xF0, 0xAD, 0x28, 0xC7, 0x98, 0x3C,
                0x53, 0xF1, 0x6C, 0x91, 0x7E, 0x36, 0xC3, 0x3A,
                0x23, 0x87, 0xA7, 0x3A, 0x18, 0x18, 0xBF, 0xD2,
                0x3E, 0x51, 0x9E, 0xAB, 0x9E, 0x4C, 0x65, 0xBA,
                0x43, 0xC0, 0x7E, 0xA2, 0x6B, 0xCF, 0x69, 0x7C,
                0x8F, 0xAB, 0x22, 0x28, 0xD6, 0xF1, 0x65, 0x0B,
                0x4A, 0x5B, 0x9B, 0x1F, 0xD4, 0xAA, 0xEF, 0x35,
                0xA2, 0x42, 0x32, 0x00, 0x9F, 0x42, 0xBB, 0x19,
                0x99, 0x49, 0x6D, 0xB8, 0x03, 0x3D, 0x35, 0x96,
                0x0C, 0x57, 0xBB, 0x6B, 0x07, 0xA4, 0xB9, 0x7F,
                0x9B, 0xEC, 0x78, 0x90, 0xB7, 0xC8, 0x5E, 0x7F,
                0x3B, 0xAB, 0xC1, 0xB6, 0x0C, 0x84, 0x3C, 0xBC,
                0x7F, 0x04, 0x79, 0xB7, 0x9C, 0xC0, 0xFE, 0xB0,
                0xAE, 0xBD, 0xA5, 0x57, 0x2C, 0xEC, 0x3D, 0x0D,
            };

            VerifySignature(signature, TestData.HelloBytes, "SHA1", TestData.RSA2048Params);
        }

        [Fact]
        public void VerifySignature_SHA256_1024()
        {
            byte[] signature = new byte[]
            {
                0x5C, 0x2F, 0x00, 0xA9, 0xE4, 0x63, 0xD7, 0xB7,
                0x94, 0x93, 0xCE, 0xA8, 0x7E, 0x71, 0xAE, 0x97,
                0xC2, 0x6B, 0x37, 0x31, 0x5B, 0xB8, 0xE3, 0x30,
                0xDF, 0x77, 0xF8, 0xBB, 0xB5, 0xBF, 0x41, 0x9F,
                0x14, 0x6A, 0x61, 0x26, 0x2E, 0x80, 0xE5, 0xE6,
                0x8A, 0xEA, 0xC7, 0x60, 0x0B, 0xAE, 0x2B, 0xB2,
                0x18, 0xD8, 0x5D, 0xC8, 0x58, 0x86, 0x5E, 0x23,
                0x62, 0x44, 0x72, 0xEA, 0x3B, 0xF7, 0x70, 0xC6,
                0x4C, 0x2B, 0x54, 0x5B, 0xF4, 0x24, 0xA1, 0xE5,
                0x63, 0xDD, 0x50, 0x3A, 0x29, 0x26, 0x84, 0x06,
                0xEF, 0x13, 0xD0, 0xCE, 0xCC, 0xA1, 0x05, 0xB4,
                0x72, 0x81, 0x0A, 0x2E, 0x33, 0xF6, 0x2F, 0xD1,
                0xEA, 0x41, 0xB0, 0xB3, 0x93, 0x4C, 0xF3, 0x0F,
                0x6F, 0x21, 0x3E, 0xD7, 0x5F, 0x57, 0x2E, 0xC7,
                0x5F, 0xF5, 0x28, 0x89, 0xB8, 0x07, 0xDB, 0xAC,
                0x70, 0x95, 0x25, 0x49, 0x8A, 0x1A, 0xD7, 0xFC,
            };

            VerifySignature(signature, TestData.HelloBytes, "SHA256", TestData.RSA1024Params);
        }

        [Fact]
        public void VerifySignature_SHA256_2048()
        {
            byte[] signature = new byte[]
            {
                0x2C, 0x74, 0x98, 0x23, 0xF4, 0x38, 0x7F, 0x49,
                0x82, 0xB6, 0x55, 0xCF, 0xC3, 0x25, 0x4F, 0xE3,
                0x4B, 0x17, 0xE7, 0xED, 0xEA, 0x58, 0x1E, 0x63,
                0x57, 0x58, 0xCD, 0xB5, 0x06, 0xD6, 0xCA, 0x13,
                0x28, 0x81, 0xE6, 0xE0, 0x8B, 0xDC, 0xC6, 0x05,
                0x35, 0x35, 0x40, 0x73, 0x76, 0x61, 0x67, 0x42,
                0x94, 0xF7, 0x54, 0x0E, 0xB6, 0x30, 0x9A, 0x70,
                0xC3, 0x06, 0xC1, 0x59, 0xA7, 0x89, 0x66, 0x38,
                0x02, 0x5C, 0x52, 0x02, 0x17, 0x4E, 0xEC, 0x21,
                0xE9, 0x24, 0x85, 0xCB, 0x56, 0x42, 0xAB, 0x21,
                0x3A, 0x19, 0xC3, 0x95, 0x06, 0xBA, 0xDB, 0xD9,
                0x89, 0x7C, 0xB9, 0xEC, 0x1D, 0x8B, 0x5A, 0x64,
                0x87, 0xAF, 0x36, 0x71, 0xAC, 0x0A, 0x2B, 0xC7,
                0x7D, 0x2F, 0x44, 0xAA, 0xB4, 0x1C, 0xBE, 0x0B,
                0x0A, 0x4E, 0xEA, 0xF8, 0x75, 0x40, 0xD9, 0x4A,
                0x82, 0x1C, 0x82, 0x81, 0x97, 0xC2, 0xF1, 0xC8,
                0xA7, 0x4B, 0x45, 0x9A, 0x66, 0x8E, 0x35, 0x2E,
                0xE5, 0x1A, 0x2B, 0x0B, 0xF9, 0xAB, 0xC4, 0x2A,
                0xE0, 0x47, 0x72, 0x2A, 0xC2, 0xD8, 0xC6, 0xFD,
                0x91, 0x30, 0xD2, 0x45, 0xA4, 0x7F, 0x0F, 0x39,
                0x80, 0xBC, 0xA9, 0xBD, 0xEC, 0xA5, 0x03, 0x6F,
                0x01, 0xF6, 0x19, 0xD5, 0x2B, 0xD9, 0x40, 0xCD,
                0x7F, 0xEF, 0x0F, 0x9D, 0x93, 0x02, 0xCD, 0x89,
                0xB8, 0x2C, 0xC7, 0xD6, 0xFD, 0xAA, 0x12, 0x6E,
                0x4C, 0x06, 0x35, 0x08, 0x61, 0x79, 0x27, 0xE1,
                0xEA, 0x46, 0x75, 0x08, 0x5B, 0x51, 0xA1, 0x80,
                0x78, 0x02, 0xEA, 0x3E, 0xEC, 0x29, 0xD2, 0x8B,
                0xC5, 0x9E, 0x7D, 0xA4, 0x85, 0x8D, 0xAD, 0x73,
                0x39, 0x17, 0x64, 0x82, 0x46, 0x4A, 0xA4, 0x34,
                0xF0, 0xCC, 0x2F, 0x9F, 0x55, 0xA4, 0xEA, 0xEC,
                0xC9, 0xA7, 0xAB, 0xBA, 0xA8, 0x84, 0x14, 0x62,
                0x6B, 0x9B, 0x97, 0x2D, 0x8C, 0xB2, 0x1C, 0x16,
            };

            VerifySignature(signature, TestData.HelloBytes, "SHA256", TestData.RSA2048Params);
        }

        [Theory]
        [MemberData(nameof(RoundTripTheories))]
        public void SignAndVerify_Roundtrip(string hashAlgorithm, RSAParameters rsaParameters)
        {
            SignAndVerify(TestData.HelloBytes, hashAlgorithm, rsaParameters);
        }

        public static IEnumerable<object[]> RoundTripTheories
        {
            get
            {
                foreach (RSAParameters rsaParameters in new[] { TestData.RSA1024Params, TestData.RSA2048Params })
                {
                    if (RSAFactory.SupportsSha1Signatures)
                    {
                        yield return new object[] { nameof(HashAlgorithmName.SHA1), rsaParameters };
                    }

                    yield return new object[] { nameof(HashAlgorithmName.MD5), rsaParameters };
                    yield return new object[] { nameof(HashAlgorithmName.SHA256), rsaParameters };
                }

                yield return new object[] { nameof(HashAlgorithmName.SHA384), TestData.RSA2048Params };
                yield return new object[] { nameof(HashAlgorithmName.SHA512), TestData.RSA2048Params };
            }
        }

        [Fact]
        public void NegativeVerify_WrongAlgorithm()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.RSA2048Params);
                byte[] signature = SignData(rsa, TestData.HelloBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                bool signatureMatched = VerifyData(rsa, TestData.HelloBytes, signature, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1);

                Assert.False(signatureMatched);
            }
        }

        [Fact]
        public void NegativeVerify_WrongSignature()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.RSA2048Params);
                byte[] signature = SignData(rsa, TestData.HelloBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                // Invalidate the signature.
                signature[0] = unchecked((byte)~signature[0]);

                bool signatureMatched = VerifyData(rsa, TestData.HelloBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                Assert.False(signatureMatched);
            }
        }

        [Fact]
        public void NegativeVerify_TamperedData()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.RSA2048Params);
                byte[] signature = SignData(rsa, TestData.HelloBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                bool signatureMatched = VerifyData(rsa, Array.Empty<byte>(), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                Assert.False(signatureMatched);
            }
        }

        [Fact]
        public void NegativeVerify_BadKeysize()
        {
            byte[] signature;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.RSA2048Params);
                signature = SignData(rsa, TestData.HelloBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.RSA1024Params);
                bool signatureMatched = VerifyData(rsa, TestData.HelloBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                Assert.False(signatureMatched);
            }
        }

        [Fact]
        public void PkcsSignHash_MismatchedHashSize()
        {
            RSASignaturePadding padding = RSASignaturePadding.Pkcs1;

            using (RSA rsa = RSAFactory.Create(TestData.RSA2048Params))
            {
                byte[] data152 = new byte[152 / 8];
                byte[] data168 = new byte[168 / 8];

                Assert.ThrowsAny<CryptographicException>(
                    () => SignHash(rsa, data152, HashAlgorithmName.SHA1, padding));

                Assert.ThrowsAny<CryptographicException>(
                    () => SignHash(rsa, data168, HashAlgorithmName.SHA1, padding));

                byte[] data160 = new byte[160 / 8];

                Assert.ThrowsAny<CryptographicException>(
                    () => SignHash(rsa, data160, HashAlgorithmName.SHA256, padding));
            }
        }

        [ConditionalFact(typeof(RSAFactory), nameof(RSAFactory.SupportsSha1Signatures))]
        public void ExpectedHashSignature_SHA1_2048()
        {
            byte[] expectedHashSignature = new byte[]
            {
                0xA1, 0xFC, 0x74, 0x67, 0x49, 0x91, 0xF4, 0x28,
                0xB0, 0xF6, 0x2B, 0xB8, 0x5E, 0x5F, 0x2E, 0x0F,
                0xD8, 0xBC, 0xB4, 0x6E, 0x0A, 0xF7, 0x11, 0xC2,
                0x65, 0x35, 0x5C, 0x1B, 0x1B, 0xC1, 0x20, 0xC0,
                0x7D, 0x5B, 0x98, 0xAF, 0xB4, 0xC1, 0x6A, 0x25,
                0x17, 0x47, 0x2C, 0x7F, 0x20, 0x2A, 0xDD, 0xF0,
                0x5F, 0xDF, 0x6F, 0x5B, 0x7D, 0xEE, 0xAA, 0x4B,
                0x9E, 0x8B, 0xA6, 0x0D, 0x81, 0x54, 0x93, 0x6E,
                0xB2, 0x86, 0xC8, 0x14, 0x4F, 0xE7, 0x4A, 0xCC,
                0xBE, 0x51, 0x2D, 0x0B, 0x9B, 0x46, 0xF1, 0x39,
                0x80, 0x1D, 0xD0, 0x07, 0xBA, 0x46, 0x48, 0xFC,
                0x7A, 0x50, 0x17, 0xC9, 0x7F, 0xEF, 0xDD, 0x42,
                0xC5, 0x8B, 0x69, 0x38, 0x67, 0xAB, 0xBD, 0x39,
                0xA6, 0xF4, 0x02, 0x34, 0x88, 0x56, 0x50, 0x05,
                0xEA, 0x95, 0x24, 0x7D, 0x34, 0xD9, 0x9F, 0xB1,
                0x05, 0x39, 0x6A, 0x42, 0x9E, 0x5E, 0xEB, 0xC9,
                0x90, 0xC1, 0x93, 0x63, 0x29, 0x0C, 0xC5, 0xBC,
                0xC8, 0x65, 0xB0, 0xFA, 0x63, 0x61, 0x77, 0xD9,
                0x16, 0x59, 0xF0, 0xAD, 0x28, 0xC7, 0x98, 0x3C,
                0x53, 0xF1, 0x6C, 0x91, 0x7E, 0x36, 0xC3, 0x3A,
                0x23, 0x87, 0xA7, 0x3A, 0x18, 0x18, 0xBF, 0xD2,
                0x3E, 0x51, 0x9E, 0xAB, 0x9E, 0x4C, 0x65, 0xBA,
                0x43, 0xC0, 0x7E, 0xA2, 0x6B, 0xCF, 0x69, 0x7C,
                0x8F, 0xAB, 0x22, 0x28, 0xD6, 0xF1, 0x65, 0x0B,
                0x4A, 0x5B, 0x9B, 0x1F, 0xD4, 0xAA, 0xEF, 0x35,
                0xA2, 0x42, 0x32, 0x00, 0x9F, 0x42, 0xBB, 0x19,
                0x99, 0x49, 0x6D, 0xB8, 0x03, 0x3D, 0x35, 0x96,
                0x0C, 0x57, 0xBB, 0x6B, 0x07, 0xA4, 0xB9, 0x7F,
                0x9B, 0xEC, 0x78, 0x90, 0xB7, 0xC8, 0x5E, 0x7F,
                0x3B, 0xAB, 0xC1, 0xB6, 0x0C, 0x84, 0x3C, 0xBC,
                0x7F, 0x04, 0x79, 0xB7, 0x9C, 0xC0, 0xFE, 0xB0,
                0xAE, 0xBD, 0xA5, 0x57, 0x2C, 0xEC, 0x3D, 0x0D,
            };

            byte[] dataHash;

            using (HashAlgorithm hash = SHA1.Create())
            {
                dataHash = hash.ComputeHash(TestData.HelloBytes);
            }

            ExpectHashSignature(expectedHashSignature, dataHash, "SHA1", TestData.RSA2048Params);
        }

        [Fact]
        public void ExpectedHashSignature_SHA256_1024()
        {
            byte[] expectedHashSignature = new byte[]
            {
                0x5C, 0x2F, 0x00, 0xA9, 0xE4, 0x63, 0xD7, 0xB7,
                0x94, 0x93, 0xCE, 0xA8, 0x7E, 0x71, 0xAE, 0x97,
                0xC2, 0x6B, 0x37, 0x31, 0x5B, 0xB8, 0xE3, 0x30,
                0xDF, 0x77, 0xF8, 0xBB, 0xB5, 0xBF, 0x41, 0x9F,
                0x14, 0x6A, 0x61, 0x26, 0x2E, 0x80, 0xE5, 0xE6,
                0x8A, 0xEA, 0xC7, 0x60, 0x0B, 0xAE, 0x2B, 0xB2,
                0x18, 0xD8, 0x5D, 0xC8, 0x58, 0x86, 0x5E, 0x23,
                0x62, 0x44, 0x72, 0xEA, 0x3B, 0xF7, 0x70, 0xC6,
                0x4C, 0x2B, 0x54, 0x5B, 0xF4, 0x24, 0xA1, 0xE5,
                0x63, 0xDD, 0x50, 0x3A, 0x29, 0x26, 0x84, 0x06,
                0xEF, 0x13, 0xD0, 0xCE, 0xCC, 0xA1, 0x05, 0xB4,
                0x72, 0x81, 0x0A, 0x2E, 0x33, 0xF6, 0x2F, 0xD1,
                0xEA, 0x41, 0xB0, 0xB3, 0x93, 0x4C, 0xF3, 0x0F,
                0x6F, 0x21, 0x3E, 0xD7, 0x5F, 0x57, 0x2E, 0xC7,
                0x5F, 0xF5, 0x28, 0x89, 0xB8, 0x07, 0xDB, 0xAC,
                0x70, 0x95, 0x25, 0x49, 0x8A, 0x1A, 0xD7, 0xFC,
            };

            byte[] dataHash;

            using (HashAlgorithm hash = SHA256.Create())
            {
                dataHash = hash.ComputeHash(TestData.HelloBytes);
            }

            ExpectHashSignature(expectedHashSignature, dataHash, "SHA256", TestData.RSA1024Params);
        }

        [Fact]
        public void ExpectedHashSignature_SHA256_2048()
        {
            byte[] expectedHashSignature = new byte[]
            {
                0x2C, 0x74, 0x98, 0x23, 0xF4, 0x38, 0x7F, 0x49,
                0x82, 0xB6, 0x55, 0xCF, 0xC3, 0x25, 0x4F, 0xE3,
                0x4B, 0x17, 0xE7, 0xED, 0xEA, 0x58, 0x1E, 0x63,
                0x57, 0x58, 0xCD, 0xB5, 0x06, 0xD6, 0xCA, 0x13,
                0x28, 0x81, 0xE6, 0xE0, 0x8B, 0xDC, 0xC6, 0x05,
                0x35, 0x35, 0x40, 0x73, 0x76, 0x61, 0x67, 0x42,
                0x94, 0xF7, 0x54, 0x0E, 0xB6, 0x30, 0x9A, 0x70,
                0xC3, 0x06, 0xC1, 0x59, 0xA7, 0x89, 0x66, 0x38,
                0x02, 0x5C, 0x52, 0x02, 0x17, 0x4E, 0xEC, 0x21,
                0xE9, 0x24, 0x85, 0xCB, 0x56, 0x42, 0xAB, 0x21,
                0x3A, 0x19, 0xC3, 0x95, 0x06, 0xBA, 0xDB, 0xD9,
                0x89, 0x7C, 0xB9, 0xEC, 0x1D, 0x8B, 0x5A, 0x64,
                0x87, 0xAF, 0x36, 0x71, 0xAC, 0x0A, 0x2B, 0xC7,
                0x7D, 0x2F, 0x44, 0xAA, 0xB4, 0x1C, 0xBE, 0x0B,
                0x0A, 0x4E, 0xEA, 0xF8, 0x75, 0x40, 0xD9, 0x4A,
                0x82, 0x1C, 0x82, 0x81, 0x97, 0xC2, 0xF1, 0xC8,
                0xA7, 0x4B, 0x45, 0x9A, 0x66, 0x8E, 0x35, 0x2E,
                0xE5, 0x1A, 0x2B, 0x0B, 0xF9, 0xAB, 0xC4, 0x2A,
                0xE0, 0x47, 0x72, 0x2A, 0xC2, 0xD8, 0xC6, 0xFD,
                0x91, 0x30, 0xD2, 0x45, 0xA4, 0x7F, 0x0F, 0x39,
                0x80, 0xBC, 0xA9, 0xBD, 0xEC, 0xA5, 0x03, 0x6F,
                0x01, 0xF6, 0x19, 0xD5, 0x2B, 0xD9, 0x40, 0xCD,
                0x7F, 0xEF, 0x0F, 0x9D, 0x93, 0x02, 0xCD, 0x89,
                0xB8, 0x2C, 0xC7, 0xD6, 0xFD, 0xAA, 0x12, 0x6E,
                0x4C, 0x06, 0x35, 0x08, 0x61, 0x79, 0x27, 0xE1,
                0xEA, 0x46, 0x75, 0x08, 0x5B, 0x51, 0xA1, 0x80,
                0x78, 0x02, 0xEA, 0x3E, 0xEC, 0x29, 0xD2, 0x8B,
                0xC5, 0x9E, 0x7D, 0xA4, 0x85, 0x8D, 0xAD, 0x73,
                0x39, 0x17, 0x64, 0x82, 0x46, 0x4A, 0xA4, 0x34,
                0xF0, 0xCC, 0x2F, 0x9F, 0x55, 0xA4, 0xEA, 0xEC,
                0xC9, 0xA7, 0xAB, 0xBA, 0xA8, 0x84, 0x14, 0x62,
                0x6B, 0x9B, 0x97, 0x2D, 0x8C, 0xB2, 0x1C, 0x16,
            };

            byte[] dataHash;

            using (HashAlgorithm hash = SHA256.Create())
            {
                dataHash = hash.ComputeHash(TestData.HelloBytes);
            }

            ExpectHashSignature(expectedHashSignature, dataHash, "SHA256", TestData.RSA2048Params);
        }

        [ConditionalFact(typeof(RSAFactory), nameof(RSAFactory.SupportsSha1Signatures))]
        public void VerifyHashSignature_SHA1_2048()
        {
            byte[] hashSignature = new byte[]
            {
                0xA1, 0xFC, 0x74, 0x67, 0x49, 0x91, 0xF4, 0x28,
                0xB0, 0xF6, 0x2B, 0xB8, 0x5E, 0x5F, 0x2E, 0x0F,
                0xD8, 0xBC, 0xB4, 0x6E, 0x0A, 0xF7, 0x11, 0xC2,
                0x65, 0x35, 0x5C, 0x1B, 0x1B, 0xC1, 0x20, 0xC0,
                0x7D, 0x5B, 0x98, 0xAF, 0xB4, 0xC1, 0x6A, 0x25,
                0x17, 0x47, 0x2C, 0x7F, 0x20, 0x2A, 0xDD, 0xF0,
                0x5F, 0xDF, 0x6F, 0x5B, 0x7D, 0xEE, 0xAA, 0x4B,
                0x9E, 0x8B, 0xA6, 0x0D, 0x81, 0x54, 0x93, 0x6E,
                0xB2, 0x86, 0xC8, 0x14, 0x4F, 0xE7, 0x4A, 0xCC,
                0xBE, 0x51, 0x2D, 0x0B, 0x9B, 0x46, 0xF1, 0x39,
                0x80, 0x1D, 0xD0, 0x07, 0xBA, 0x46, 0x48, 0xFC,
                0x7A, 0x50, 0x17, 0xC9, 0x7F, 0xEF, 0xDD, 0x42,
                0xC5, 0x8B, 0x69, 0x38, 0x67, 0xAB, 0xBD, 0x39,
                0xA6, 0xF4, 0x02, 0x34, 0x88, 0x56, 0x50, 0x05,
                0xEA, 0x95, 0x24, 0x7D, 0x34, 0xD9, 0x9F, 0xB1,
                0x05, 0x39, 0x6A, 0x42, 0x9E, 0x5E, 0xEB, 0xC9,
                0x90, 0xC1, 0x93, 0x63, 0x29, 0x0C, 0xC5, 0xBC,
                0xC8, 0x65, 0xB0, 0xFA, 0x63, 0x61, 0x77, 0xD9,
                0x16, 0x59, 0xF0, 0xAD, 0x28, 0xC7, 0x98, 0x3C,
                0x53, 0xF1, 0x6C, 0x91, 0x7E, 0x36, 0xC3, 0x3A,
                0x23, 0x87, 0xA7, 0x3A, 0x18, 0x18, 0xBF, 0xD2,
                0x3E, 0x51, 0x9E, 0xAB, 0x9E, 0x4C, 0x65, 0xBA,
                0x43, 0xC0, 0x7E, 0xA2, 0x6B, 0xCF, 0x69, 0x7C,
                0x8F, 0xAB, 0x22, 0x28, 0xD6, 0xF1, 0x65, 0x0B,
                0x4A, 0x5B, 0x9B, 0x1F, 0xD4, 0xAA, 0xEF, 0x35,
                0xA2, 0x42, 0x32, 0x00, 0x9F, 0x42, 0xBB, 0x19,
                0x99, 0x49, 0x6D, 0xB8, 0x03, 0x3D, 0x35, 0x96,
                0x0C, 0x57, 0xBB, 0x6B, 0x07, 0xA4, 0xB9, 0x7F,
                0x9B, 0xEC, 0x78, 0x90, 0xB7, 0xC8, 0x5E, 0x7F,
                0x3B, 0xAB, 0xC1, 0xB6, 0x0C, 0x84, 0x3C, 0xBC,
                0x7F, 0x04, 0x79, 0xB7, 0x9C, 0xC0, 0xFE, 0xB0,
                0xAE, 0xBD, 0xA5, 0x57, 0x2C, 0xEC, 0x3D, 0x0D,
            };

            byte[] dataHash;

            using (HashAlgorithm hash = SHA1.Create())
            {
                dataHash = hash.ComputeHash(TestData.HelloBytes);
            }

            VerifyHashSignature(hashSignature, dataHash, "SHA1", TestData.RSA2048Params);
        }

        [Fact]
        public void VerifyHashSignature_SHA256_1024()
        {
            byte[] hashSignature = new byte[]
            {
                0x5C, 0x2F, 0x00, 0xA9, 0xE4, 0x63, 0xD7, 0xB7,
                0x94, 0x93, 0xCE, 0xA8, 0x7E, 0x71, 0xAE, 0x97,
                0xC2, 0x6B, 0x37, 0x31, 0x5B, 0xB8, 0xE3, 0x30,
                0xDF, 0x77, 0xF8, 0xBB, 0xB5, 0xBF, 0x41, 0x9F,
                0x14, 0x6A, 0x61, 0x26, 0x2E, 0x80, 0xE5, 0xE6,
                0x8A, 0xEA, 0xC7, 0x60, 0x0B, 0xAE, 0x2B, 0xB2,
                0x18, 0xD8, 0x5D, 0xC8, 0x58, 0x86, 0x5E, 0x23,
                0x62, 0x44, 0x72, 0xEA, 0x3B, 0xF7, 0x70, 0xC6,
                0x4C, 0x2B, 0x54, 0x5B, 0xF4, 0x24, 0xA1, 0xE5,
                0x63, 0xDD, 0x50, 0x3A, 0x29, 0x26, 0x84, 0x06,
                0xEF, 0x13, 0xD0, 0xCE, 0xCC, 0xA1, 0x05, 0xB4,
                0x72, 0x81, 0x0A, 0x2E, 0x33, 0xF6, 0x2F, 0xD1,
                0xEA, 0x41, 0xB0, 0xB3, 0x93, 0x4C, 0xF3, 0x0F,
                0x6F, 0x21, 0x3E, 0xD7, 0x5F, 0x57, 0x2E, 0xC7,
                0x5F, 0xF5, 0x28, 0x89, 0xB8, 0x07, 0xDB, 0xAC,
                0x70, 0x95, 0x25, 0x49, 0x8A, 0x1A, 0xD7, 0xFC,
            };

            byte[] dataHash;

            using (HashAlgorithm hash = SHA256.Create())
            {
                dataHash = hash.ComputeHash(TestData.HelloBytes);
            }

            VerifyHashSignature(hashSignature, dataHash, "SHA256", TestData.RSA1024Params);
        }

        [Fact]
        public void VerifyHashSignature_SHA256_2048()
        {
            byte[] hashSignature = new byte[]
            {
                0x2C, 0x74, 0x98, 0x23, 0xF4, 0x38, 0x7F, 0x49,
                0x82, 0xB6, 0x55, 0xCF, 0xC3, 0x25, 0x4F, 0xE3,
                0x4B, 0x17, 0xE7, 0xED, 0xEA, 0x58, 0x1E, 0x63,
                0x57, 0x58, 0xCD, 0xB5, 0x06, 0xD6, 0xCA, 0x13,
                0x28, 0x81, 0xE6, 0xE0, 0x8B, 0xDC, 0xC6, 0x05,
                0x35, 0x35, 0x40, 0x73, 0x76, 0x61, 0x67, 0x42,
                0x94, 0xF7, 0x54, 0x0E, 0xB6, 0x30, 0x9A, 0x70,
                0xC3, 0x06, 0xC1, 0x59, 0xA7, 0x89, 0x66, 0x38,
                0x02, 0x5C, 0x52, 0x02, 0x17, 0x4E, 0xEC, 0x21,
                0xE9, 0x24, 0x85, 0xCB, 0x56, 0x42, 0xAB, 0x21,
                0x3A, 0x19, 0xC3, 0x95, 0x06, 0xBA, 0xDB, 0xD9,
                0x89, 0x7C, 0xB9, 0xEC, 0x1D, 0x8B, 0x5A, 0x64,
                0x87, 0xAF, 0x36, 0x71, 0xAC, 0x0A, 0x2B, 0xC7,
                0x7D, 0x2F, 0x44, 0xAA, 0xB4, 0x1C, 0xBE, 0x0B,
                0x0A, 0x4E, 0xEA, 0xF8, 0x75, 0x40, 0xD9, 0x4A,
                0x82, 0x1C, 0x82, 0x81, 0x97, 0xC2, 0xF1, 0xC8,
                0xA7, 0x4B, 0x45, 0x9A, 0x66, 0x8E, 0x35, 0x2E,
                0xE5, 0x1A, 0x2B, 0x0B, 0xF9, 0xAB, 0xC4, 0x2A,
                0xE0, 0x47, 0x72, 0x2A, 0xC2, 0xD8, 0xC6, 0xFD,
                0x91, 0x30, 0xD2, 0x45, 0xA4, 0x7F, 0x0F, 0x39,
                0x80, 0xBC, 0xA9, 0xBD, 0xEC, 0xA5, 0x03, 0x6F,
                0x01, 0xF6, 0x19, 0xD5, 0x2B, 0xD9, 0x40, 0xCD,
                0x7F, 0xEF, 0x0F, 0x9D, 0x93, 0x02, 0xCD, 0x89,
                0xB8, 0x2C, 0xC7, 0xD6, 0xFD, 0xAA, 0x12, 0x6E,
                0x4C, 0x06, 0x35, 0x08, 0x61, 0x79, 0x27, 0xE1,
                0xEA, 0x46, 0x75, 0x08, 0x5B, 0x51, 0xA1, 0x80,
                0x78, 0x02, 0xEA, 0x3E, 0xEC, 0x29, 0xD2, 0x8B,
                0xC5, 0x9E, 0x7D, 0xA4, 0x85, 0x8D, 0xAD, 0x73,
                0x39, 0x17, 0x64, 0x82, 0x46, 0x4A, 0xA4, 0x34,
                0xF0, 0xCC, 0x2F, 0x9F, 0x55, 0xA4, 0xEA, 0xEC,
                0xC9, 0xA7, 0xAB, 0xBA, 0xA8, 0x84, 0x14, 0x62,
                0x6B, 0x9B, 0x97, 0x2D, 0x8C, 0xB2, 0x1C, 0x16,
            };

            byte[] dataHash;

            using (HashAlgorithm hash = SHA256.Create())
            {
                dataHash = hash.ComputeHash(TestData.HelloBytes);
            }

            VerifyHashSignature(hashSignature, dataHash, "SHA256", TestData.RSA2048Params);
        }

        [ConditionalTheory]
        [InlineData("SHA256")]
        [InlineData("SHA384")]
        [InlineData("SHA512")]
        [InlineData("MD5")]
        [InlineData("SHA1")]
        public void PssRoundtrip(string hashAlgorithmName)
        {
            if (!RSAFactory.SupportsSha1Signatures && hashAlgorithmName == "SHA1")
            {
                throw new SkipTestException("Platform does not support RSA with SHA1 signatures.");
            }

            RSAParameters privateParameters = TestData.RSA2048Params;
            RSAParameters publicParameters = new RSAParameters
            {
                Modulus = privateParameters.Modulus,
                Exponent = privateParameters.Exponent,
            };

            using (RSA privateKey = RSAFactory.Create())
            using (RSA publicKey = RSAFactory.Create())
            {
                privateKey.ImportParameters(privateParameters);
                publicKey.ImportParameters(publicParameters);

                byte[] data = TestData.RsaBigExponentParams.Modulus;
                HashAlgorithmName hashAlgorithm = new HashAlgorithmName(hashAlgorithmName);
                RSASignaturePadding padding = RSASignaturePadding.Pss;

                if (RSAFactory.SupportsPss)
                {
                    byte[] signature = SignData(privateKey, data, hashAlgorithm, padding);

                    Assert.NotNull(signature);
                    Assert.Equal(publicParameters.Modulus.Length, signature.Length);

                    Assert.True(VerifyData(publicKey, data, signature, hashAlgorithm, padding));
                }
                else
                {
                    Assert.ThrowsAny<CryptographicException>(
                        () => SignData(privateKey, data, hashAlgorithm, padding));

                    byte[] signature = new byte[privateParameters.Modulus.Length];

                    Assert.ThrowsAny<CryptographicException>(
                        () => VerifyData(privateKey, data, signature, hashAlgorithm, padding));
                }
            }
        }

        [Fact]
        public void VerifyExpectedSignature_PssSha256_RSA2048()
        {
            byte[] modulus2048Signature = (
                "460CA7273FF6CC02DD57F07CB18E65E5AF23B0285E26122B810EC6D2F4EE7E1A" +
                "1B01A203623E800C9940CE827614B2F1DC7C7B1CC3A976D27F82517EB64AC90B" +
                "9A97D1CC17FB4731C63CA02C9F46B57A4A03981D73265CDB36E28EF08FCA77ED" +
                "FAB34EE91FE6AABF00045489ACEB631FB004438344EA7997ADE2191C1A70E9F9" +
                "2BA809FEFB4EFA0DBC1075A7EBBBCF57747DA8D0B3467BD3DAC5EA8B47F76F07" +
                "7043497E7459A83349FE74320E77D471008CB7B43707561FA8DC9251F8EAE531" +
                "5AC1894C4F9E6B7BECF993C146C5D6CF0DB60992A297F358A0895831965887C4" +
                "B9153B96771C998CD61DA0C487D63555AE66F917F1BFDF509BFFEB21440F6A3C").HexToByteArray();

            VerifyExpectedSignature_Pss(
                TestData.RSA2048Params,
                HashAlgorithmName.SHA256,
                TestData.RSA2048Params.Modulus,
                modulus2048Signature);
        }

        [Fact]
        public void VerifyExpectedSignature_PssSha256_RSA16384()
        {
            byte[] modulus2048Signature = (
                "1D92D529567F6922866FFDE4BF44C427FA511BF5EDF163ED51A0D14ADECD98FB" +
                "C6A61A61532404AF74C3AB65119BB1358855A68362FBABAED7D8E56403EE9AFA" +
                "33C8D73E35880066556A7304F8E8A3EBF981C8318958AC867B32F3A01F085F53" +
                "B0885781DFCF4DE4183805B7B26C4718E58031FF8D0B82B38D958BF0147C263F" +
                "A4012FE29E8B3D7EA18780C3A6B01E15D81387C4367AFB35FF4868309928C112" +
                "86F030AFED02B2C8CED24C527B8EAA126076B268F469427E70D0ACFC4C3E007D" +
                "05F84E2F3D3DC3028674DA38026F9054F54636FBED099CD528782F60F1882045" +
                "E2F297B467496F01AE566EC80C384A5E775481EECEA713D0F35C75CBFDB33F52" +
                "FC4699EDBEB1F938368AA2B261402634BEE548F38821E6FCBFE7C26C0C44EB1D" +
                "58D215500E11EA400AE44B349727BE28A62188770393863B0F9BCC6C82717C35" +
                "34334205B931BDD6FEA4FDCC681566BCB2AF80266D692007E027682535BFD265" +
                "3E8D906D3531575975F2BB77457378FA84A34F2F064F6E5B986D48FBFD9F8BF8" +
                "BD3CDFDA21C624A315C8246764841C811939B60BC73AB4CD15B141A1C3D063B6" +
                "9529FB4B4342B1064650272CECE2B0A398A5F9B5FD2D107A9FE84781CA11D29F" +
                "00986BEE1850BC5E46570FDFD54DBD15C6C8BA12AC0CA7825765009346AB7E95" +
                "848F95928368E65AFDCB8AA058B0EE31320584248327F4D22017480BAB38BA47" +
                "4FFCBBE68C5A24582AB9594EAD26ACE67170319B1BA9FB514070A5051744EAB3" +
                "BDC1C131A8AF580CE5B11B23B50FF66D4DC6F6064C7FBA88D8CB74A3A85A51EB" +
                "C344F7226871286109B0B9D33B7137B223FDC152EDD1BA0641D906A22F91D146" +
                "8B39EAC8261EB05CA7757AEA46051599087CE92A9962D1DEF8DAF910F10169E0" +
                "A0F8F864C0E4B29DC12958B06E8E4225C90CFEB6D7367DFEB7F8DF2891D50DCC" +
                "89F435466A3D25B676BD06C69D39EDE7EDD639703C262B7C0257C88BD197542F" +
                "0CE25F8E317037C1DC4380E2AA43CB4FBBA078AF83CDCA8DD8A8545267E3F853" +
                "8DE7A897269E492A1400715FC3BABF2E30D2696216F51FD30FC3BE67FB9813CA" +
                "DAEE3E4CB779A5F8A10DBCA11927CBC50721A5412680E490A68CA3DFF74B9E2A" +
                "774DCC32B84B9D5B253268465A911A6CF3D189F51D21DDEF30006F929C151402" +
                "E821F4097A8514192F95CA8B9ABFD0B7C7C86AAC5B0FDAECB02EABBC3B1F8442" +
                "2A1921AE0B4BC01B6C037038DF382DC130843B15F5F042A98053578248E8DB02" +
                "A1B5FC702B59FEAD99C32F6DAF15308A53CA139E408CE0F45DEC48FF1E5DB77B" +
                "0305196F16598A21AF92603F77BA061A2A12B5D6F69B19F2EBDDE47578DD3895" +
                "8D36D15D88015F5E51AA818669B6A65DE40C264CAD22B8E25D4866E8BBC0A64E" +
                "59E0D95C69DF925BB9B9C88AB0542E53C6034DDE4FB5763D21C62765FA7A39AA" +
                "B50652F20D464652710162EBBE7ECFEFF255864B459A0DD83DD3E7DD88EA1271" +
                "442D70A944106A47EF22ADF67AD9D7CC24FC47C66B5D9B15E3D0104491D8C060" +
                "A6E46F96A8E0C11A7D25211E2206B1CE272143B4B369D7B07645CB1E94668C43" +
                "3D412A4600364122F22D7EC6B79227FA215CD230E3D09D0AA5BE3B291C4BE343" +
                "808582C7F6EE20D7457DE1955F9E7E40A4C9EF55C5A5A0D3D125D8F53E69477B" +
                "F0D91BD8B3401ECEEFE9D94382F836ACFB81814DFEF86F614406B02B6001E90E" +
                "E84DA2BD0441BB943A6295F530AA7B7F375CD91EBA4CA83A0CF35FCBCA9F9915" +
                "3BA0C28D1DA762D6257C99378F21FD6D890C31B9606BB6238CD3B0DBD4012649" +
                "602E5352D20DA067576A94DB21E323B7902885F8892C844411027C3F4ED1F28B" +
                "DBFB929E986DA6AF15F552975705C9C2C5500CE52F90903EF4BD53B145FB713B" +
                "8A62FA292E608438A1CBF663FCCCEC99489CE9D709BF92AA9260F3950B058618" +
                "B4EED63DD02376057460AF3854976C6A9C605148B0882F337253AD8AE8FA3AA6" +
                "4194EF462A403D8198F1FBEFCC2ECAAE6B3C3A52AC79F5311F60B3EF2B281FE1" +
                "C22E2C820570C687A1B5C7A1BB5013844DEE5AD720BE9D186B14EF38EA2FCB12" +
                "4358CF552BEB3B9A0B36FB298FE527EEE2CD428680C4D6C55CCBFCE6F8E81162" +
                "32198584267DF41CF50BDDBA22C601ADCA005A2187FC0097DC0B6AF0552B3034" +
                "BA6E432DD0D7D6F3D58DDA91AC4756D2CFFC28DCF0A7EEE2D2A6CC23C77A2E2F" +
                "9DD26143AAD7062093D7592C282A02FABC3815DD285064F6F5F0848294D781B3" +
                "20C3F2DA3C26E1CCF6DB171908D5AFABA1A7BABC6D3F31FD7B566B7321AF6297" +
                "F3EE652ABD11DD4FFF39D77B4FE06A838412B85C4877534369D115C65FA36BA3" +
                "DDFF9B50C95B2AD649A5C814C9183ED743FA5CB23F65C5216C0F61B16CB73409" +
                "D105EE6321C7D210C4DABC7A80C63B383178669FA9E79DCDD3DB1C175EED5199" +
                "9F51BBAC06C90794B77491D0BC2FED10199EE322B7B23DB5B63B6C6B85E39ED3" +
                "D145BC070EA912820C2E59FE9ED3670D8FBC44B9B2D6FAEEF95154972BA509CC" +
                "96F83328DD7243DB11F9CDD5D8013DB8C7DD5ED58DEEFAC7FD282085715A063E" +
                "320B167C904A65761233361B8232DAE539A8B5B38D9506ABBA9844E24D64E2DA" +
                "ACD1E4F22546959282B721ACDA8289AE92C5FE0775F59A4EA10C732EE22FA01C" +
                "E6556E8CCA94E6DD87F3A50EDF6FFDC4D10B07B3FBC55111DF62088A1AFCE2B6" +
                "C6CF4C18CAA3BA05E7117368546B241236DDE91DF9CE30AE691C6044F30EA85A" +
                "F169F0B64C353A40BC4AFF467C4B304B70751248B1B09F3781DDB84087B972FD" +
                "0C92C6ABE141D38327BD810F87F0E058098B6E8A538E236C40955005AC4A232D" +
                "22F7F9B479D0C093F18C4C4756B06F80132980E30716A3282306D1352CBBCD31").HexToByteArray();

            VerifyExpectedSignature_Pss(
                TestData.RSA16384Params,
                HashAlgorithmName.SHA256,
                TestData.RSA2048Params.Modulus,
                modulus2048Signature);
        }

        [Fact]
        public void VerifyExpectedSignature_PssSha384()
        {
            byte[] bigModulusSignature = (
                "70F48CA4E8640701369DB986C4D09C91E4C197DB1BE4F32C3F37A67AEC4BA95D" +
                "733EAACAE139B7B9C8E66C5BC82629971C3BEBF93A949CB81763FECDF96B73DA" +
                "7D5929A15DFEF58B51E6D43F46238FC1121AAA3A5F3DF6B56E0FE2B6205192AB" +
                "BA9752FC9CFD3000B08E3A823514A93FD90871FD09A005DA191431487DAF6364" +
                "22").HexToByteArray();

            VerifyExpectedSignature_Pss(
                TestData.RSA1032Parameters,
                HashAlgorithmName.SHA384,
                TestData.RSA16384Params.Modulus,
                bigModulusSignature);
        }

        [Fact]
        public void VerifyExpectedSignature_PssSha512()
        {
            byte[] helloSignature = (
                "60678D68816149206AD33F7153FFBAA1043FF7ABC539D6C88E5D2C94BCC10CF4" +
                "E66A6F0F08DEA15781B8FA06F9E27D0B01347DAAA4B760D8978EC2EF87B508A2" +
                "680FBE59F8BCC8A6AF413A1CB2373DFF32C4217542A9EE86179083DD316485FB" +
                "E496EEF0EBE3E4A2793C888E988962C5EAF35136172E74B02724770863D10B19" +
                "AACDE7D31CE77BE96EA54DE7A2409648AB3105FAC1003B00E32FAE4527284352" +
                "A859C17F4C7D611DE4C451291A3096A0D6230EE2699B79CD571DE6D441CB372A" +
                "9D6E46080AB8041D45D4B9475CBE6B48D10F4332910869D8C3931133224475D9" +
                "BA1E0B92161BB2C17A96F92432F2BA1AEBAD8C7CD33D79F5C6EFB9BF6F192205").HexToByteArray();

            VerifyExpectedSignature_Pss(
                TestData.RSA2048Params,
                HashAlgorithmName.SHA512,
                TestData.HelloBytes,
                helloSignature);
        }

        private void VerifyExpectedSignature_Pss(
            RSAParameters keyParameters,
            HashAlgorithmName hashAlgorithm,
            byte[] data,
            byte[] signature,
            [System.Runtime.CompilerServices.CallerMemberName] string callerName = null)
        {
            RSAParameters publicParameters = new RSAParameters
            {
                Modulus = keyParameters.Modulus,
                Exponent = keyParameters.Exponent,
            };

            RSASignaturePadding padding = RSASignaturePadding.Pss;

            using (RSA rsaPublic = RSAFactory.Create())
            using (RSA rsaPrivate = RSAFactory.Create())
            {
                try
                {
                    rsaPublic.ImportParameters(publicParameters);
                }
                catch (CryptographicException)
                {
                    // The key didn't load, not anything else this test can do.
                    return;
                }

                rsaPrivate.ImportParameters(keyParameters);

                // Generator for new tests.
                if (signature == null)
                {
                    signature = SignData(rsaPrivate, data, hashAlgorithm, padding);
                    Console.WriteLine($"{callerName}: {signature.ByteArrayToHex()}");
                }

                if (RSAFactory.SupportsPss)
                {
                    Assert.True(
                        VerifyData(rsaPublic, data, signature, hashAlgorithm, padding),
                        "Public key verified the signature");

                    Assert.True(
                        VerifyData(rsaPrivate, data, signature, hashAlgorithm, padding),
                        "Private key verified the signature");
                }
                else
                {
                    Assert.ThrowsAny<CryptographicException>(
                        () => VerifyData(rsaPublic, data, signature, hashAlgorithm, padding));

                    Assert.ThrowsAny<CryptographicException>(
                        () => VerifyData(rsaPrivate, data, signature, hashAlgorithm, padding));
                }
            }
        }

        [ConditionalFact(nameof(SupportsPss))]
        public void PssSignature_WrongHashAlgorithm()
        {
            RSASignaturePadding padding = RSASignaturePadding.Pss;
            byte[] data = TestData.HelloBytes;

            using (RSA rsa = RSAFactory.Create(TestData.RSA2048Params))
            {
                byte[] signature = SignData(rsa, data, HashAlgorithmName.SHA256, padding);
                Assert.False(VerifyData(rsa, data, signature, HashAlgorithmName.SHA384, padding));
            }
        }

        [ConditionalFact(nameof(SupportsPss))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void PssVerifyHash_MismatchedHashSize()
        {
            // This is a legal SHA-1 value, which we're going to use with SHA-2-256 instead.
            byte[] hash = "75ED7E627DB9AECBD870E27EED49ED7D9AEB2F52".HexToByteArray();

            byte[] sig = (
                "837A17C13618030A6C0C17551D8A34CA5AE4BB1D45A5DAD091F4016C630E0838" +
                "5F9D9F1F75EF4CCBBE723C0630AC699C43587D81BD16AFBD2F797215F68F8062" +
                "87A352BB269FB9D042DA4D9D664172E4B3B39FC3457879C8DBDD56FAB44F2515" +
                "71E2E607A964CB548CB36198004ACD8D3E3B80D10917CE582710BB65513C0310" +
                "4A0A82C63D2B8898F5BAF97618B5EBE5F3B0824561C059FD7FC949B12837E8B1" +
                "E86380E9A68F6D7E8E8BD5C57B04E831DBBDBDCA20403EC988635F62D4B48382" +
                "56E2AF4213FDCA6BF801C06AF6381DAC61288C13B08806A323B3E956A13BCB29" +
                "680F62CCA9880A8A1FD1A2CA61DCFE008AC7FC55E98ACCE9B7BE010E5BCB836A").HexToByteArray();

            using (RSA rsa = RSAFactory.Create(TestData.RSA2048Params))
            {
                Assert.False(VerifyHash(rsa, hash, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
            }
        }

        [ConditionalFact(nameof(SupportsPss))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void PssSignHash_MismatchedHashSize()
        {
            RSASignaturePadding padding = RSASignaturePadding.Pss;

            using (RSA rsa = RSAFactory.Create(TestData.RSA2048Params))
            {
                byte[] data152 = new byte[152 / 8];
                byte[] data168 = new byte[168 / 8];

                Assert.ThrowsAny<CryptographicException>(
                    () => SignHash(rsa, data152, HashAlgorithmName.SHA1, padding));

                Assert.ThrowsAny<CryptographicException>(
                    () => SignHash(rsa, data168, HashAlgorithmName.SHA1, padding));

                byte[] data160 = new byte[160 / 8];

                Assert.ThrowsAny<CryptographicException>(
                    () => SignHash(rsa, data160, HashAlgorithmName.SHA256, padding));
            }
        }

        [ConditionalFact(nameof(SupportsPss))]
        public void PssSignature_WrongData()
        {
            RSASignaturePadding padding = RSASignaturePadding.Pss;
            byte[] dataCopy = (byte[])TestData.HelloBytes.Clone();
            HashAlgorithmName hashAlgorithmName = HashAlgorithmName.SHA256;

            using (RSA rsa = RSAFactory.Create(TestData.RSA2048Params))
            {
                byte[] signature = SignData(rsa, dataCopy, hashAlgorithmName, padding);
                dataCopy[0] ^= 0xFF;
                Assert.False(VerifyData(rsa, dataCopy, signature, hashAlgorithmName, padding));
            }
        }

        [ConditionalFact(nameof(SupportsPss))]
        public void PssSignature_WrongLength()
        {
            RSASignaturePadding padding = RSASignaturePadding.Pss;
            byte[] data = TestData.HelloBytes;
            HashAlgorithmName hashAlgorithmName = HashAlgorithmName.SHA256;

            using (RSA rsa = RSAFactory.Create(TestData.RSA2048Params))
            {
                byte[] signature = SignData(rsa, data, hashAlgorithmName, padding);

                // Too long by a byte
                Array.Resize(ref signature, signature.Length + 1);
                Assert.False(VerifyData(rsa, data, signature, hashAlgorithmName, padding));

                // Net too short by a byte
                Array.Resize(ref signature, signature.Length - 2);
                Assert.False(VerifyData(rsa, data, signature, hashAlgorithmName, padding));
            }
        }

        private void ExpectSignature(
            byte[] expectedSignature,
            byte[] data,
            string hashAlgorithmName,
            RSAParameters rsaParameters)
        {
            // RSA signatures use PKCS 1.5 EMSA encoding (encoding method, signature algorithm).
            // EMSA specifies a fixed filler type of { 0x01, 0xFF, 0xFF ... 0xFF, 0x00 } whose length
            // is as long as it needs to be to match the block size.  Since the filler is deterministic,
            // the signature is deterministic, so we can safely verify it here.
            byte[] signature;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(rsaParameters);
                signature = SignData(rsa, data, new HashAlgorithmName(hashAlgorithmName), RSASignaturePadding.Pkcs1);
            }

            Assert.Equal(expectedSignature, signature);
        }

        private void ExpectHashSignature(
            byte[] expectedSignature,
            byte[] dataHash,
            string hashAlgorithmName,
            RSAParameters rsaParameters)
        {
            // RSA signatures use PKCS 1.5 EMSA encoding (encoding method, signature algorithm).
            // EMSA specifies a fixed filler type of { 0x01, 0xFF, 0xFF ... 0xFF, 0x00 } whose length
            // is as long as it needs to be to match the block size.  Since the filler is deterministic,
            // the signature is deterministic, so we can safely verify it here.
            byte[] signature;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(rsaParameters);
                signature = SignHash(rsa, dataHash, new HashAlgorithmName(hashAlgorithmName), RSASignaturePadding.Pkcs1);
            }

            Assert.Equal(expectedSignature, signature);
        }

        private void VerifySignature(
            byte[] signature,
            byte[] data,
            string hashAlgorithmName,
            RSAParameters rsaParameters)
        {
            RSAParameters publicOnly = new RSAParameters
            {
                Modulus = rsaParameters.Modulus,
                Exponent = rsaParameters.Exponent,
            };

            bool signatureMatched;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(publicOnly);
                signatureMatched = VerifyData(rsa, data, signature, new HashAlgorithmName(hashAlgorithmName), RSASignaturePadding.Pkcs1);
            }

            Assert.True(signatureMatched);
        }

        private void VerifyHashSignature(
            byte[] signature,
            byte[] dataHash,
            string hashAlgorithmName,
            RSAParameters rsaParameters)
        {
            RSAParameters publicOnly = new RSAParameters
            {
                Modulus = rsaParameters.Modulus,
                Exponent = rsaParameters.Exponent,
            };

            bool signatureMatched;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(publicOnly);
                signatureMatched = VerifyHash(rsa, dataHash, signature, new HashAlgorithmName(hashAlgorithmName), RSASignaturePadding.Pkcs1);
            }

            Assert.True(signatureMatched);
        }

        private void SignAndVerify(byte[] data, string hashAlgorithmName, RSAParameters rsaParameters)
        {
            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(rsaParameters);
                byte[] signature = SignData(rsa, data, new HashAlgorithmName(hashAlgorithmName), RSASignaturePadding.Pkcs1);
                bool signatureMatched = VerifyData(rsa, data, signature, new HashAlgorithmName(hashAlgorithmName), RSASignaturePadding.Pkcs1);
                Assert.True(signatureMatched);
            }
        }
    }
}
