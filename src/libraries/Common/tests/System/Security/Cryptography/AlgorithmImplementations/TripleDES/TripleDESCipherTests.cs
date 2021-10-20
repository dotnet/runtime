// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.TripleDes.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public static partial class TripleDESCipherTests
    {
        [Fact]
        public static void TripleDESDefaults()
        {
            using (TripleDES des = TripleDESFactory.Create())
            {
                Assert.Equal(192, des.KeySize);
                Assert.Equal(64, des.BlockSize);
            }
        }

        [Fact]
        public static void TripleDESGenerate128Key()
        {
            using (TripleDES des = TripleDESFactory.Create())
            {
                des.KeySize = 128;
                byte[] key = des.Key;
                Assert.Equal(128, key.Length * 8);
            }
        }

        [Fact]
        public static void TripleDESInvalidKeySizes()
        {
            using (TripleDES des = TripleDESFactory.Create())
            {
                Assert.Throws<CryptographicException>(() => des.KeySize = 128 - des.BlockSize);
                Assert.Throws<CryptographicException>(() => des.KeySize = 192 + des.BlockSize);
            }
        }

        [Theory]
        [InlineData(PaddingMode.None)]
        [InlineData(PaddingMode.Zeros)]
        public static void VerifyKnownTransform_CFB8_NoOrZeroPadding_0(PaddingMode paddingMode)
        {
            // NIST CAVS TDESMMT.ZIP TCFB8MMT2.rsp, [DECRYPT] COUNT=0
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                paddingMode,
                key: "fb978a0b6dc2c467e3cb52329de95161fb978a0b6dc2c467".HexToByteArray(),
                iv: "8b97579ea5ac300f".HexToByteArray(),
                plainBytes: "80".HexToByteArray(),
                cipherBytes: "05".HexToByteArray(),
                feedbackSize: 8
            );
        }

        [Theory]
        [InlineData(PaddingMode.None)]
        [InlineData(PaddingMode.Zeros)]
        public static void VerifyKnownTransform_CFB8_NoOrZeroPadding_1(PaddingMode paddingMode)
        {
            // NIST CAVS TDESMMT.ZIP TCFB8MMT2.rsp, [DECRYPT] COUNT=1
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                paddingMode,
                key: "9b04c86dd31a8a589876101549d6e0109b04c86dd31a8a58".HexToByteArray(),
                iv: "52cd77d49fc72347".HexToByteArray(),
                plainBytes: "2fef".HexToByteArray(),
                cipherBytes: "5818".HexToByteArray(),
                feedbackSize: 8
            );
        }

        [Theory]
        [InlineData(PaddingMode.None)]
        [InlineData(PaddingMode.Zeros)]
        public static void VerifyKnownTransform_CFB8_NoOrZeroPadding_2(PaddingMode paddingMode)
        {
            // NIST CAVS TDESMMT.ZIP TCFB8MMT2.rsp, [DECRYPT] COUNT=2
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                paddingMode,
                key: "fbb667e340586b5b5ef7c87049b93257fbb667e340586b5b".HexToByteArray(),
                iv: "459e8b8736715791".HexToByteArray(),
                plainBytes: "061704".HexToByteArray(),
                cipherBytes: "93b378".HexToByteArray(),
                feedbackSize: 8
            );
        }

        [Fact]
        public static void VerifyKnownTransform_CFB8_PKCS7_2()
        {
            // NIST CAVS TDESMMT.ZIP TCFB8MMT2.rsp, [DECRYPT] COUNT=2
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.PKCS7,
                key: "fbb667e340586b5b5ef7c87049b93257fbb667e340586b5b".HexToByteArray(),
                iv: "459e8b8736715791".HexToByteArray(),
                plainBytes: "061704".HexToByteArray(),
                cipherBytes: "93b37808".HexToByteArray(),
                feedbackSize: 8
            );
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB64_PKCS7_2()
        {
            // NIST CAVS TDESMMT.ZIP TCFB8MMT2.rsp, [DECRYPT] COUNT=2
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.PKCS7,
                key: "fbb667e340586b5b5ef7c87049b93257fbb667e340586b5b".HexToByteArray(),
                iv: "459e8b8736715791".HexToByteArray(),
                plainBytes: "061704".HexToByteArray(),
                cipherBytes: "931f41eccdab4f99".HexToByteArray(),
                feedbackSize: 64
            );
        }

        [Theory]
        [InlineData(PaddingMode.None)]
        [InlineData(PaddingMode.Zeros)]
        public static void VerifyKnownTransform_CFB8_NoOrZeroPadding_3(PaddingMode paddingMode)
        {
            // NIST CAVS TDESMMT.ZIP TCFB8MMT2.rsp, [DECRYPT] COUNT=3
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                paddingMode,
                key: "4a575d02515d40b0a40d830bd9b315134a575d02515d40b0".HexToByteArray(),
                iv: "ab27e9f02affa532".HexToByteArray(),
                plainBytes: "55f75b95".HexToByteArray(),
                cipherBytes: "2ef5dddc".HexToByteArray(),
                feedbackSize: 8
            );
        }

        [Theory]
        [InlineData(PaddingMode.None)]
        [InlineData(PaddingMode.Zeros)]
        public static void VerifyKnownTransform_CFB8_NoOrZeroPadding_4(PaddingMode paddingMode)
        {
            // NIST CAVS TDESMMT.ZIP TCFB8MMT2.rsp, [DECRYPT] COUNT=4
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                paddingMode,
                key: "91a834855e6bab31c7fd6be657ceb9ec91a834855e6bab31".HexToByteArray(),
                iv: "7838aaad4e64640b".HexToByteArray(),
                plainBytes: "c3851c0ab4".HexToByteArray(),
                cipherBytes: "fe451f35f1".HexToByteArray(),
                feedbackSize: 8
            );
        }

        [Theory]
        [InlineData(PaddingMode.None)]
        [InlineData(PaddingMode.Zeros)]
        public static void VerifyKnownTransform_CFB8_NoOrZeroPadding_5(PaddingMode paddingMode)
        {
            // NIST CAVS TDESMMT.ZIP TCFB8MMT2.rsp, [DECRYPT] COUNT=5
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                paddingMode,
                key: "04d923abd9291c3e4954a8b52fdabcc804d923abd9291c3e".HexToByteArray(),
                iv: "191f8794944e601c".HexToByteArray(),
                plainBytes: "6fe8f67d2af1".HexToByteArray(),
                cipherBytes: "3bd78a8d24ad".HexToByteArray(),
                feedbackSize: 8
            );
        }

        [Theory]
        [InlineData(PaddingMode.None)]
        [InlineData(PaddingMode.Zeros)]
        public static void VerifyKnownTransform_CFB8_NoOrZeroPadding_6(PaddingMode paddingMode)
        {
            // NIST CAVS TDESMMT.ZIP TCFB8MMT2.rsp, [DECRYPT] COUNT=6
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                paddingMode,
                key: "a7799e7f5dfe54ce13376401e96de075a7799e7f5dfe54ce".HexToByteArray(),
                iv: "370184c749d04a20".HexToByteArray(),
                plainBytes: "2b4228b769795b".HexToByteArray(),
                cipherBytes: "6f32e4495e4259".HexToByteArray(),
                feedbackSize: 8
            );
        }

        [Theory]
        [InlineData(PaddingMode.None)]
        [InlineData(PaddingMode.Zeros)]
        public static void VerifyKnownTransform_CFB8_NoOrZeroPadding_7(PaddingMode paddingMode)
        {
            // NIST CAVS TDESMMT.ZIP TCFB8MMT2.rsp, [DECRYPT] COUNT=7
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                paddingMode,
                key: "6bfe3d3df8c1e0d34ffe0dbf854c940e6bfe3d3df8c1e0d3".HexToByteArray(),
                iv: "51e4c5c29e858da6".HexToByteArray(),
                plainBytes: "4cb3554fd0b9ec82".HexToByteArray(),
                cipherBytes: "72e1738d80d285e2".HexToByteArray(),
                feedbackSize: 8
            );
        }

        [Theory]
        [InlineData(PaddingMode.None)]
        [InlineData(PaddingMode.Zeros)]
        public static void VerifyKnownTransform_CFB8_NoOrZeroPadding_8(PaddingMode paddingMode)
        {
            // NIST CAVS TDESMMT.ZIP TCFB8MMT2.rsp, [DECRYPT] COUNT=8
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                paddingMode,
                key: "e0264aec13e63db991f8c120c4b9b6dae0264aec13e63db9".HexToByteArray(),
                iv: "bd8795dba79930d6".HexToByteArray(),
                plainBytes: "79068e2943f02914af".HexToByteArray(),
                cipherBytes: "9b78c5636c5965f88e".HexToByteArray(),
                feedbackSize: 8
            );
        }

        [Theory]
        [InlineData(PaddingMode.None)]
        [InlineData(PaddingMode.Zeros)]
        public static void VerifyKnownTransform_CFB8_NoOrZeroPadding_9(PaddingMode paddingMode)
        {
            // NIST CAVS TDESMMT.ZIP TCFB8MMT2.rsp, [DECRYPT] COUNT=9
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                paddingMode,
                key: "7ca28938ba6bec1ffec78f7cd69761947ca28938ba6bec1f".HexToByteArray(),
                iv: "953896586e49d38f".HexToByteArray(),
                plainBytes: "2ea956d4a211db6859b7".HexToByteArray(),
                cipherBytes: "f20e536674a66fa73805".HexToByteArray(),
                feedbackSize: 8
            );
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB64_NoPadding_0()
        {
            // NIST CAVS TDESMMT.ZIP TCFB64MMT2.rsp, [DECRYPT] COUNT=0
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: "9ee0b59b25865154588551341c4fef9e9ee0b59b25865154".HexToByteArray(),
                iv: "6e37d197376db595".HexToByteArray(),
                plainBytes: "dcd3cf9746d6e42b".HexToByteArray(),
                cipherBytes: "63cad52260e0a1cd".HexToByteArray(),
                feedbackSize: 64
            );
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        [InlineData(CipherMode.CBC, 0)]
        [InlineData(CipherMode.CFB, 8)]
        [InlineData(CipherMode.CFB, 64)]
        [InlineData(CipherMode.ECB, 0)]
        public static void EncryptorReuse_LeadsToSameResults(CipherMode cipherMode, int feedbackSize)
        {
            // AppleCCCryptor does not allow calling Reset on CFB cipher.
            // this test validates that the behavior is taken into consideration.
            var input = "b72606c98d8e4fabf08839abf7a0ac61".HexToByteArray();

            using (TripleDES tdes = TripleDESFactory.Create())
            {
                tdes.Mode = cipherMode;

                if (feedbackSize > 0)
                {
                    tdes.FeedbackSize = feedbackSize;
                }

                using (ICryptoTransform transform = tdes.CreateEncryptor())
                {
                    byte[] output1 = transform.TransformFinalBlock(input, 0, input.Length);
                    byte[] output2 = transform.TransformFinalBlock(input, 0, input.Length);

                    Assert.Equal(output1, output2);
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        [InlineData(CipherMode.CBC, 0)]
        [InlineData(CipherMode.CFB, 8)]
        [InlineData(CipherMode.CFB, 64)]
        [InlineData(CipherMode.ECB, 0)]
        public static void DecryptorReuse_LeadsToSameResults(CipherMode cipherMode, int feedbackSize)
        {
            // AppleCCCryptor does not allow calling Reset on CFB cipher.
            // this test validates that the behavior is taken into consideration.
            var input = "896072ab28e5fdfc9e8b3610627bf27a".HexToByteArray();
            var key = "c179d0fdd073a1910e51f1d5fe70047ac179d0fdd073a191".HexToByteArray();
            var iv = "b956d5426d02b247".HexToByteArray();

            using (TripleDES tdes = TripleDESFactory.Create())
            {
                tdes.Mode = cipherMode;
                tdes.Key = key;
                tdes.IV = iv;
                tdes.Padding = PaddingMode.None;

                if (feedbackSize > 0)
                {
                    tdes.FeedbackSize = feedbackSize;
                }

                using (ICryptoTransform transform = tdes.CreateDecryptor())
                {
                    byte[] output1 = transform.TransformFinalBlock(input, 0, input.Length);
                    byte[] output2 = transform.TransformFinalBlock(input, 0, input.Length);

                    Assert.Equal(output1, output2);
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB64_NoPadding_1()
        {
            // NIST CAVS TDESMMT.ZIP TCFB64MMT2.rsp, [DECRYPT] COUNT=1
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: "c179d0fdd073a1910e51f1d5fe70047ac179d0fdd073a191".HexToByteArray(),
                iv: "b956d5426d02b247".HexToByteArray(),
                plainBytes: "32bd529065e26a27643097925e3a726b".HexToByteArray(),
                cipherBytes: "896072ab28e5fdfc9e8b3610627bf27a".HexToByteArray(),
                feedbackSize: 64
            );
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB64_NoPadding_2()
        {
            // NIST CAVS TDESMMT.ZIP TCFB64MMT2.rsp, [DECRYPT] COUNT=2
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: "a8084a04495bfb45b3575ee03d732967a8084a04495bfb45".HexToByteArray(),
                iv: "00fd7b4fdb4b3382".HexToByteArray(),
                plainBytes: "c20c7041007a67de7b4355be7406095496923b75dfb98080".HexToByteArray(),
                cipherBytes: "9198c138edb037de25d0bcdebe7b9be10ebd7e7ea103edae".HexToByteArray(),
                feedbackSize: 64
            );
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB64_NoPadding_3()
        {
            // NIST CAVS TDESMMT.ZIP TCFB64MMT2.rsp, [DECRYPT] COUNT=3
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: "c1497fdf67cecbab80d543f16d13c8d5c1497fdf67cecbab".HexToByteArray(),
                iv: "a1241ca0fe9378cd".HexToByteArray(),
                plainBytes: "157dcfa7ad6758335e561fa7dd7f98dca592e9128e7be30ccd1af7dc5a4536d5".HexToByteArray(),
                cipherBytes: "08fcace492f82282fb3255884a64a231dd438069ffbcb432bd7ec446f5b8adfd".HexToByteArray(),
                feedbackSize: 64
            );
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB64_NoPadding_4()
        {
            // NIST CAVS TDESMMT.ZIP TCFB64MMT2.rsp, [DECRYPT] COUNT=4
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: "fd0e3262ec38fe5710389d0779c2fb43fd0e3262ec38fe57".HexToByteArray(),
                iv: "33c9e4adfb4634ac".HexToByteArray(),
                plainBytes: "37536dda516aab8a992131004134ce48c56fee05261164aae0a88db0f43410617f105e20940cf3e9".HexToByteArray(),
                cipherBytes: "80e8a96c3fe83857fc738ac7b6639f0d8c28bfa617c56a60fd1b8fbdc36afe9ce3151e161fa5e3a7".HexToByteArray(),
                feedbackSize: 64
            );
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB64_NoPadding_5()
        {
            // NIST CAVS TDESMMT.ZIP TCFB64MMT2.rsp, [DECRYPT] COUNT=5
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: "ae32253be61040157a7c10b6011fcde3ae32253be6104015".HexToByteArray(),
                iv: "47be2286dbccdfe6".HexToByteArray(),
                plainBytes: "e579282129c123c914c700ad8c099b593fe83fdef7be7e5ffb36add9c6b91644cc79c1e457212017488963e16198c528".HexToByteArray(),
                cipherBytes: "7185c5800ca4d5432b50f5b7920e26296c2913e7e3f847a1ef639e156ba4f9ec6e4b36ded885601d2b9d22f19dc3829f".HexToByteArray(),
                feedbackSize: 64
            );
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB64_NoPadding_6()
        {
            // NIST CAVS TDESMMT.ZIP TCFB64MMT2.rsp, [DECRYPT] COUNT=6
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: "df83498cec83084acb7aaef26e58f1e0df83498cec83084a".HexToByteArray(),
                iv: "158d2ca6e70b18f6".HexToByteArray(),
                plainBytes: "4fb7cf2a244ff20beddf8719b2d9c78ab0710703036f804f08bc1f7927ea9906ba1ef57afd1553c5304c0b72694cd88bb6cb1289772dfff0".HexToByteArray(),
                cipherBytes: "158b396cd1969a07042e808d0c875d74166ce77291df233fe300c29c5a30b1946575ec02042093537dae3f8d51ed96906e601d9da6e34e14".HexToByteArray(),
                feedbackSize: 64
            );
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB64_NoPadding_7()
        {
            // NIST CAVS TDESMMT.ZIP TCFB64MMT2.rsp, [DECRYPT] COUNT=7
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: "ce31cd2067c157199bfb3b8ad9ef9223ce31cd2067c15719".HexToByteArray(),
                iv: "d31741512b6a7471".HexToByteArray(),
                plainBytes: "a0447f5abebf8623db81b600699ce8373353442908fefe8c63f5e29e22ba1057f759635505ed0ac059887def2d31f6996128d4fbe2df6534429744d7f6496768".HexToByteArray(),
                cipherBytes: "b3a791b128f003bc28cd17bbb5c68990faec73f88c10b664f1349b045f3fba24c5f51bbb10259c41a72492c2377bb331b6dd34fea25c2eea8adc461bd0c78d6b".HexToByteArray(),
                feedbackSize: 64
            );
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB64_NoPadding_8()
        {
            // NIST CAVS TDESMMT.ZIP TCFB64MMT2.rsp, [DECRYPT] COUNT=8
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: "5bbc3423bf67e05262d65740708019f15bbc3423bf67e052".HexToByteArray(),
                iv: "14544ea4813c49d9".HexToByteArray(),
                plainBytes: "a21f26496f74fd8a93aa5423e2a4fc76facbff015db2f4ef14f08b8c13a29d0561e4e57d04b0b00211f8fba46d025a9c0727c8aebb7d25f27f1606321909ba50e660fa25358c63f9".HexToByteArray(),
                cipherBytes: "c3acc89b9b6037effc65eacdc23b36c38d0e609566d360eba594e4481108983b4a67a5d9647c776ad5fcc4639116ca95734bd8a3df800fb9a6526a7b29a9fc3cc29079715f44f865".HexToByteArray(),
                feedbackSize: 64
            );
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB64_NoPadding_9()
        {
            // NIST CAVS TDESMMT.ZIP TCFB64MMT2.rsp, [DECRYPT] COUNT=9
            TestTripleDESTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: "197c738cfb6e0bc2fee57ffb1ca72675197c738cfb6e0bc2".HexToByteArray(),
                iv: "f1a42447a333caa3".HexToByteArray(),
                plainBytes: "6f914b6996ee8e7ea625b2fddd7677b4384320be0aba3af81d1210965ac37983f340d5698ddf35d45dfccbf783a50c6eed1a730b5c98675cb6b7645fc8374e10d8b340c44b0eae988c1ef635fab913da".HexToByteArray(),
                cipherBytes: "8aabb83216e4bd5a3dd20586e598bb8e956dcbf7d09cde17a2cf8b7a788ecb853503ae5981004dfa644300b115f8d1ae0c7f30f25e70e86c4adc51620fd6c71301325c9bdc8dca16588eac08fe6aedfd".HexToByteArray(),
                feedbackSize: 64
            );
        }

        private static byte[] TripleDESEncryptDirectKey(TripleDES tdes, byte[] key, byte[] iv, byte[] plainBytes)
        {
            using (MemoryStream output = new MemoryStream())
            using (CryptoStream cryptoStream = new CryptoStream(output, tdes.CreateEncryptor(key, iv), CryptoStreamMode.Write))
            {
                cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                cryptoStream.FlushFinalBlock();

                return output.ToArray();
            }
        }

        private static byte[] TripleDESDecryptDirectKey(TripleDES tdes, byte[] key, byte[] iv, byte[] cipherBytes)
        {
            using (MemoryStream output = new MemoryStream())
            using (CryptoStream cryptoStream = new CryptoStream(output, tdes.CreateDecryptor(key, iv), CryptoStreamMode.Write))
            {
                cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);
                cryptoStream.FlushFinalBlock();

                return output.ToArray();
            }
        }

        private static void TestTripleDESTransformDirectKey(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            byte[] iv,
            byte[] plainBytes,
            byte[] cipherBytes,
            int? feedbackSize = default)
        {
            byte[] liveEncryptBytes;
            byte[] liveDecryptBytes;
            byte[] liveOneShotDecryptBytes = null;
            byte[] liveOneShotEncryptBytes = null;

            using (TripleDES tdes = TripleDESFactory.Create())
            {
                tdes.Mode = cipherMode;
                tdes.Padding = paddingMode;
                tdes.Key = key;

                if (feedbackSize.HasValue)
                {
                    tdes.FeedbackSize = feedbackSize.Value;
                }

                liveEncryptBytes = TripleDESEncryptDirectKey(tdes, key, iv, plainBytes);
                liveDecryptBytes = TripleDESDecryptDirectKey(tdes, key, iv, cipherBytes);

                if (cipherMode == CipherMode.ECB)
                {
                    liveOneShotDecryptBytes = tdes.DecryptEcb(cipherBytes, paddingMode);
                    liveOneShotEncryptBytes = tdes.EncryptEcb(plainBytes, paddingMode);
                }
                else if (cipherMode == CipherMode.CBC)
                {
                    liveOneShotDecryptBytes = tdes.DecryptCbc(cipherBytes, iv, paddingMode);
                    liveOneShotEncryptBytes = tdes.EncryptCbc(plainBytes, iv, paddingMode);
                }
                else if (cipherMode == CipherMode.CFB)
                {
                    liveOneShotDecryptBytes = tdes.DecryptCfb(cipherBytes, iv, paddingMode, feedbackSizeInBits: feedbackSize.Value);
                    liveOneShotEncryptBytes = tdes.EncryptCfb(plainBytes, iv, paddingMode, feedbackSizeInBits: feedbackSize.Value);
                }

                if (liveOneShotDecryptBytes is not null)
                {
                    Assert.Equal(plainBytes, liveOneShotDecryptBytes);
                }

                if (liveOneShotEncryptBytes is not null)
                {
                    Assert.Equal(cipherBytes, liveOneShotEncryptBytes);
                }
            }

            Assert.Equal(cipherBytes, liveEncryptBytes);
            Assert.Equal(plainBytes, liveDecryptBytes);
        }

        [Theory]
        [InlineData(192, "e56f72478c7479d169d54c0548b744af5b53efb1cdd26037", "c5629363d957054eba793093b83739bb78711db221a82379")]
        [InlineData(128, "1387b981dbb40f34b915c4ed89fd681a740d3b4869c0b575", "c5629363d957054eba793093b83739bb")]
        [InlineData(192, "1387b981dbb40f34b915c4ed89fd681a740d3b4869c0b575", "c5629363d957054eba793093b83739bbc5629363d957054e")]
        public static void TripleDESRoundTripNoneECB(int keySize, string expectedCipherHex, string keyHex)
        {
            byte[] key = keyHex.HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                Assert.Equal(keySize, alg.KeySize);

                alg.Padding = PaddingMode.None;
                alg.Mode = CipherMode.ECB;

                byte[] plainText = "de7d2dddea96b691e979e647dc9d3ca27d7f1ad673ca9570".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = expectedCipherHex.HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "de7d2dddea96b691e979e647dc9d3ca27d7f1ad673ca9570".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

        [Theory]
        [InlineData(192, "dea36279600f19c602b6ed9bf3ffdac5ebf25c1c470eb61c", "b43eaf0260813fb47c87ae073a146006d359ad04061eb0e6")]
        [InlineData(128, "a25e55381f0cc45541741b9ce6e96b7799aa1e0db70780f7", "b43eaf0260813fb47c87ae073a146006")]
        [InlineData(192, "a25e55381f0cc45541741b9ce6e96b7799aa1e0db70780f7", "b43eaf0260813fb47c87ae073a146006b43eaf0260813fb4")]
        public static void TripleDESRoundTripNoneCBC(int keySize, string expectedCipherHex, string keyHex)
        {
            byte[] key = keyHex.HexToByteArray();
            byte[] iv = "5fbc5bc21b8597d8".HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                Assert.Equal(keySize, alg.KeySize);

                alg.IV = iv;
                alg.Padding = PaddingMode.None;
                alg.Mode = CipherMode.CBC;

                byte[] plainText = "79a86903608e133e020e1dc68c9835250c2f17b0ebeed91b".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = expectedCipherHex.HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "79a86903608e133e020e1dc68c9835250c2f17b0ebeed91b".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

        [Theory]
        [InlineData(192, "149ec32f558b27c7e4151e340d8184f18b4e25d2518f69d9", "9da5b265179d65f634dfc95513f25094411e51bb3be877ef")]
        [InlineData(128, "02ac5db31cfada874f6042c4e92b09175fd08e93a20f936b", "9da5b265179d65f634dfc95513f25094")]
        [InlineData(192, "02ac5db31cfada874f6042c4e92b09175fd08e93a20f936b", "9da5b265179d65f634dfc95513f250949da5b265179d65f6")]
        public static void TripleDESRoundTripZerosECB(int keySize, string expectedCipherHex, string keyHex)
        {
            byte[] key = keyHex.HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                Assert.Equal(keySize, alg.KeySize);

                alg.Padding = PaddingMode.Zeros;
                alg.Mode = CipherMode.ECB;

                byte[] plainText = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = expectedCipherHex.HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8000000".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

        [Theory]
        [InlineData(192, "9da5b265179d65f634dfc95513f25094411e51bb3be877ef")]
        [InlineData(128, "9da5b265179d65f634dfc95513f25094")]
        [InlineData(192, "9da5b265179d65f634dfc95513f250949da5b265179d65f6")]
        public static void TripleDESRoundTripISO10126ECB(int keySize, string keyHex)
        {
            byte[] key = keyHex.HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                Assert.Equal(keySize, alg.KeySize);

                alg.Padding = PaddingMode.ISO10126;
                alg.Mode = CipherMode.ECB;

                byte[] plainText = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);

                // the padding data for ISO10126 is made up of random bytes, so we cannot actually test
                // the full encrypted text. We need to strip the padding and then compare
                byte[] decrypted = alg.Decrypt(cipher);

                Assert.Equal<byte>(plainText, decrypted);
            }
        }

        [Theory]
        [InlineData(192, "149ec32f558b27c7e4151e340d8184f1c90f0a499e20fda9", "9da5b265179d65f634dfc95513f25094411e51bb3be877ef")]
        [InlineData(128, "02ac5db31cfada874f6042c4e92b091783620e54a1e75957", "9da5b265179d65f634dfc95513f25094")]
        [InlineData(192, "02ac5db31cfada874f6042c4e92b091783620e54a1e75957", "9da5b265179d65f634dfc95513f250949da5b265179d65f6")]
        public static void TripleDESRoundTripANSIX923ECB(int keySize, string expectedCipherHex, string keyHex)
        {
            byte[] key = keyHex.HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                Assert.Equal(keySize, alg.KeySize);

                alg.Padding = PaddingMode.ANSIX923;
                alg.Mode = CipherMode.ECB;

                byte[] plainText = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);

                byte[] expectedCipher = expectedCipherHex.HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8".HexToByteArray();
                Assert.Equal<byte>(plainText, decrypted);
            }
        }

        [Fact]
        public static void TripleDES_FailureToRoundTrip192Bits_DifferentPadding_ANSIX923_ZerosECB()
        {
            byte[] key = "9da5b265179d65f634dfc95513f25094411e51bb3be877ef".HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                alg.Padding = PaddingMode.ANSIX923;
                alg.Mode = CipherMode.ECB;

                byte[] plainText = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);

                byte[] expectedCipher = "149ec32f558b27c7e4151e340d8184f1c90f0a499e20fda9".HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                alg.Padding = PaddingMode.Zeros;
                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8".HexToByteArray();

                // They should not decrypt to the same value
                Assert.NotEqual<byte>(plainText, decrypted);
            }
        }

        [Theory]
        [InlineData(192, "65f3dc211876a9daad238aa7d0c7ed7a3662296faf77dff9", "5e970c0d2323d53b28fa3de507d6d20f9f0cd97123398b4d")]
        [InlineData(128, "2f55ff6bd8270f1d68dcb342bb674f914d9e1c0e61017a77", "5e970c0d2323d53b28fa3de507d6d20f")]
        [InlineData(192, "2f55ff6bd8270f1d68dcb342bb674f914d9e1c0e61017a77", "5e970c0d2323d53b28fa3de507d6d20f5e970c0d2323d53b")]
        public static void TripleDESRoundTripZerosCBC(int keySize, string expectedCipherHex, string keyHex)
        {
            byte[] key = keyHex.HexToByteArray();
            byte[] iv = "95498b5bf570f4c8".HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                Assert.Equal(keySize, alg.KeySize);

                alg.IV = iv;
                alg.Padding = PaddingMode.Zeros;
                alg.Mode = CipherMode.CBC;

                byte[] plainText = "f9e9a1385bf3bd056d6a06eac662736891bd3e6837".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = expectedCipherHex.HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "f9e9a1385bf3bd056d6a06eac662736891bd3e6837000000".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

        [Theory]
        [InlineData(192, "7b8d982ee0c14821daf1b8cf4e407c2eb328627b696ac36e", "155425f12109cd89378795a4ca337b3264689dca497ba2fa")]
        [InlineData(128, "ce7daa4723c4f880fb44c2809821fc2183b46f0c32084620", "155425f12109cd89378795a4ca337b32")]
        [InlineData(192, "ce7daa4723c4f880fb44c2809821fc2183b46f0c32084620", "155425f12109cd89378795a4ca337b32155425f12109cd89")]
        public static void TripleDESRoundTripPKCS7ECB(int keySize, string expectedCipherHex, string keyHex)
        {
            byte[] key = keyHex.HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                Assert.Equal(keySize, alg.KeySize);

                alg.Padding = PaddingMode.PKCS7;
                alg.Mode = CipherMode.ECB;

                byte[] plainText = "5bd3c4e16a723a17ac60dd0efdb158e269cddfd0fa".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = expectedCipherHex.HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "5bd3c4e16a723a17ac60dd0efdb158e269cddfd0fa".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

        [Theory]
        [InlineData(192, "446f57875e107702afde16b57eaf250b87b8110bef29af89", "6b42da08f93e819fbd26fce0785b0eec3d0cb6bfa053c505")]
        [InlineData(128, "ebf995606ceceddf5c90a7302521bc1f6d31f330969cb768", "6b42da08f93e819fbd26fce0785b0eec")]
        [InlineData(192, "ebf995606ceceddf5c90a7302521bc1f6d31f330969cb768", "6b42da08f93e819fbd26fce0785b0eec6b42da08f93e819f")]
        public static void TripleDESRoundTripPKCS7CBC(int keySize, string expectedCipherHex, string keyHex)
        {
            byte[] key = keyHex.HexToByteArray();
            byte[] iv = "8fc67ce5e7f28cde".HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                Assert.Equal(keySize, alg.KeySize);

                alg.IV = iv;
                alg.Padding = PaddingMode.PKCS7;
                alg.Mode = CipherMode.CBC;

                byte[] plainText = "e867f915e275eab27d6951165d26dec6dd0acafcfc".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = expectedCipherHex.HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "e867f915e275eab27d6951165d26dec6dd0acafcfc".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void EncryptWithLargeOutputBuffer(bool blockAlignedOutput)
        {
            using (TripleDES alg = TripleDESFactory.Create())
            using (ICryptoTransform xform = alg.CreateEncryptor())
            {
                // 8 blocks, plus maybe three bytes
                int outputPadding = blockAlignedOutput ? 0 : 3;
                byte[] output = new byte[alg.BlockSize + outputPadding];
                // 2 blocks of 0x00
                byte[] input = new byte[alg.BlockSize / 4];
                int outputOffset = 0;

                outputOffset += xform.TransformBlock(input, 0, input.Length, output, outputOffset);
                byte[] overflow = xform.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                Buffer.BlockCopy(overflow, 0, output, outputOffset, overflow.Length);
                outputOffset += overflow.Length;

                Assert.Equal(3 * (alg.BlockSize / 8), outputOffset);
                string outputAsHex = output.ByteArrayToHex();
                Assert.NotEqual(new string('0', outputOffset * 2), outputAsHex.Substring(0, outputOffset * 2));
                Assert.Equal(new string('0', (output.Length - outputOffset) * 2), outputAsHex.Substring(outputOffset * 2));
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public static void TransformWithTooShortOutputBuffer(bool encrypt, bool blockAlignedOutput)
        {
            using (TripleDES alg = TripleDESFactory.Create())
            using (ICryptoTransform xform = encrypt ? alg.CreateEncryptor() : alg.CreateDecryptor())
            {
                // 1 block, plus maybe three bytes
                int outputPadding = blockAlignedOutput ? 0 : 3;
                byte[] output = new byte[alg.BlockSize / 8 + outputPadding];
                // 3 blocks of 0x00
                byte[] input = new byte[3 * (alg.BlockSize / 8)];

                Type exceptionType = typeof(ArgumentOutOfRangeException);

                // TripleDESCryptoServiceProvider doesn't throw the ArgumentOutOfRangeException,
                // giving a CryptographicException when CAPI reports the destination too small.
                if (PlatformDetection.IsNetFramework)
                {
                    exceptionType = typeof(CryptographicException);
                }

                Assert.Throws(
                    exceptionType,
                    () => xform.TransformBlock(input, 0, input.Length, output, 0));

                Assert.Equal(new byte[output.Length], output);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void MultipleBlockDecryptTransform(bool blockAlignedOutput)
        {
            const string ExpectedOutput = "This is a test";

            int outputPadding = blockAlignedOutput ? 0 : 3;
            byte[] key = "0123456789ABCDEFFEDCBA9876543210ABCDEF0123456789".HexToByteArray();
            byte[] iv = "0123456789ABCDEF".HexToByteArray();
            byte[] outputBytes = new byte[iv.Length * 2 + outputPadding];
            byte[] input = "A61C8F1D393202E1E3C71DCEAB9B08DB".HexToByteArray();
            int outputOffset = 0;

            using (TripleDES alg = TripleDESFactory.Create())
            using (ICryptoTransform xform = alg.CreateDecryptor(key, iv))
            {
                Assert.Equal(2 * alg.BlockSize, (outputBytes.Length - outputPadding) * 8);
                outputOffset += xform.TransformBlock(input, 0, input.Length, outputBytes, outputOffset);
                byte[] overflow = xform.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                Buffer.BlockCopy(overflow, 0, outputBytes, outputOffset, overflow.Length);
                outputOffset += overflow.Length;
            }

            string decrypted = Encoding.ASCII.GetString(outputBytes, 0, outputOffset);
            Assert.Equal(ExpectedOutput, decrypted);
        }

        [Fact]
        public static void VerifyNetFxCompat_CFB8_PKCS7Padding()
        {
            // .NET Framework would always pad to the nearest block
            // with CFB8 and PKCS7 padding even though the shortest possible
            // padding is always 1 byte. This ensures we can continue to decrypt
            // .NET Framework encrypted data with the excessive padding.

            byte[] key = "531bd715cbf785c10169b6e4926562b8e1e5c4c8884ed791".HexToByteArray();
            byte[] iv = "dbeba40532a5304a".HexToByteArray();
            byte[] plaintext = "70656e6e79".HexToByteArray();
            byte[] ciphertext = "8798c2da055c9ea0".HexToByteArray();

            using TripleDES tdes = TripleDESFactory.Create();
            tdes.Mode = CipherMode.CFB;
            tdes.Padding = PaddingMode.PKCS7;
            tdes.FeedbackSize = 8;

            byte[] decrypted = TripleDESDecryptDirectKey(tdes, key, iv, ciphertext);
            Assert.Equal(plaintext, decrypted);
        }
    }
}
