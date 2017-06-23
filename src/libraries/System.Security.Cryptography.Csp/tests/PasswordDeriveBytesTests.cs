// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.DeriveBytesTests
{
    public class PasswordDeriveBytesTests
    {
        // Note some tests were copied from Rfc2898DeriveBytes (and modified accordingly).

        private static readonly byte[] s_testSalt = new byte[] { 9, 5, 5, 5, 1, 2, 1, 2 };
        private static readonly byte[] s_testSaltB = new byte[] { 0, 4, 0, 4, 1, 9, 7, 5 };
        private const string TestPassword = "PasswordGoesHere";
        private const string TestPasswordB = "FakePasswordsAreHard";
        private const int DefaultIterationCount = 100;

        [Fact]
        public static void Ctor_NullPasswordBytes()
        {
            using (var pdb = new PasswordDeriveBytes((byte[])null, s_testSalt))
            {
                Assert.Equal(DefaultIterationCount, pdb.IterationCount);
                Assert.Equal(s_testSalt, pdb.Salt);
                Assert.Equal("SHA1", pdb.HashName);
            }
        }

        [Fact]
        public static void Ctor_NullPasswordString()
        {
            Assert.Throws<ArgumentNullException>(() => new PasswordDeriveBytes((string)null, s_testSalt));
        }

        [Fact]
        public static void Ctor_NullSalt()
        {
            using (var pdb = new PasswordDeriveBytes(TestPassword, null))
            {
                Assert.Equal(DefaultIterationCount, pdb.IterationCount);
                Assert.Equal(null, pdb.Salt);
                Assert.Equal("SHA1", pdb.HashName);
            }
        }

        [Fact]
        public static void Ctor_EmptySalt()
        {
            using (var pdb = new PasswordDeriveBytes(TestPassword, Array.Empty<byte>()))
            {
                Assert.Equal(DefaultIterationCount, pdb.IterationCount);
                Assert.Equal(Array.Empty<byte>(), pdb.Salt);
                Assert.Equal("SHA1", pdb.HashName);
            }
        }

        [Fact]
        public static void Ctor_DiminishedSalt()
        {
            using (var pdb = new PasswordDeriveBytes(TestPassword, new byte[7]))
            {
                Assert.Equal(DefaultIterationCount, pdb.IterationCount);
                Assert.Equal(7, pdb.Salt.Length);
                Assert.Equal("SHA1", pdb.HashName);
            }
        }

        [Fact]
        public static void Ctor_TooFewIterations()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PasswordDeriveBytes(TestPassword, s_testSalt, "SHA1", 0));
        }

        [Fact]
        public static void Ctor_NegativeIterations()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PasswordDeriveBytes(TestPassword, s_testSalt, "SHA1", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PasswordDeriveBytes(TestPassword, s_testSalt, "SHA1", int.MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PasswordDeriveBytes(TestPassword, s_testSalt, "SHA1", int.MinValue / 2));
        }

        [Fact]
        public static void Ctor_DefaultIterations()
        {
            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt))
            {
                Assert.Equal(DefaultIterationCount, deriveBytes.IterationCount);
            }
        }

        [Fact]
        public static void Ctor_IterationsRespected()
        {
            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt, "SHA1", 1))
            {
                Assert.Equal(1, deriveBytes.IterationCount);
            }
        }

        [Fact]
        public static void Ctor_CspParameters()
        {
            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt, new CspParameters())) { }
            using (var deriveBytes = new PasswordDeriveBytes(string.Empty, s_testSalt, new CspParameters())) { }
            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt, "SHA1", 100, new CspParameters())) { }
            using (var deriveBytes = new PasswordDeriveBytes(string.Empty, s_testSalt, "SHA1", 100, new CspParameters())) { }
        }

        [Fact]
        public static void Ctor_CspParameters_Null()
        {
            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt, null)) { }
            using (var deriveBytes = new PasswordDeriveBytes(string.Empty, s_testSalt, null)) { }
            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt, "SHA1", 100, null)) { }
            using (var deriveBytes = new PasswordDeriveBytes(string.Empty, s_testSalt, "SHA1", 100, null)) { }
        }

        [Fact]
        public static void Ctor_SaltCopied()
        {
            byte[] saltIn = (byte[])s_testSalt.Clone();

            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, saltIn, "SHA1", DefaultIterationCount))
            {
                byte[] saltOut = deriveBytes.Salt;

                Assert.NotSame(saltIn, saltOut);
                Assert.Equal(saltIn, saltOut);

                // Right now we know that at least one of the constructor and get_Salt made a copy, if it was
                // only get_Salt then this next part would fail.

                saltIn[0] = unchecked((byte)~saltIn[0]);

                // Have to read the property again to prove it's detached.
                Assert.NotEqual(saltIn, deriveBytes.Salt);
            }
        }

        [Fact]
        public static void GetSaltCopies()
        {
            byte[] first;
            byte[] second;

            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt, "SHA1", DefaultIterationCount))
            {
                first = deriveBytes.Salt;
                second = deriveBytes.Salt;
            }

            Assert.NotSame(first, second);
            Assert.Equal(first, second);
        }

        [Fact]
        public static void SetSaltAfterGetBytes_Throws()
        {
            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt))
            {
                deriveBytes.GetBytes(1);
                Assert.Throws<CryptographicException>(() => deriveBytes.Salt = s_testSalt);
            }
        }

        [Fact]
        public static void SetSaltAfterGetBytes_Reset()
        {
            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt))
            {
                deriveBytes.GetBytes(1);
                deriveBytes.Reset();
                deriveBytes.Salt = s_testSaltB;
                Assert.Equal(s_testSaltB, deriveBytes.Salt);
            }
        }

        [Fact]
        public static void MinimumAcceptableInputs()
        {
            byte[] output;

            using (var deriveBytes = new PasswordDeriveBytes(string.Empty, new byte[8], "SHA1", 1))
            {
                output = deriveBytes.GetBytes(1);
            }

            Assert.Equal(1, output.Length);
            Assert.Equal(0xF8, output[0]);
        }

        [Fact]
        public static void GetBytes_ZeroLength()
        {
            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt))
            {
                AssertExtensions.Throws<ArgumentException>("", () => deriveBytes.GetBytes(0));
            }
        }

        [Fact]
        public static void GetBytes_NegativeLength()
        {
            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt))
            {
                Assert.Throws<OverflowException>(() => deriveBytes.GetBytes(-1));
                Assert.Throws<OverflowException>(() => deriveBytes.GetBytes(int.MinValue));
                Assert.Throws<OverflowException>(() => deriveBytes.GetBytes(int.MinValue / 2));
            }
        }

        [Fact]
        public static void GetBytes_NotIdempotent()
        {
            byte[] first;
            byte[] second;

            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt))
            {
                first = deriveBytes.GetBytes(32);
                second = deriveBytes.GetBytes(32);
            }

            Assert.NotEqual(first, second);
        }

        [Fact]
        public static void GetBytes_StableIfReset()
        {
            byte[] first;
            byte[] second;

            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt))
            {
                first = deriveBytes.GetBytes(32);
                deriveBytes.Reset();
                second = deriveBytes.GetBytes(32);
            }

            Assert.Equal(first, second);
        }

        [Fact]
        public static void GetBytes_StreamLike_ExtraBytes()
        {
            byte[] first;

            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt))
            {
                // SHA1 default
                Assert.Equal("SHA1", deriveBytes.HashName);

                // Request double of SHA1 hash size
                first = deriveBytes.GetBytes(40);
            }

            byte[] second = new byte[first.Length];

            // Reset
            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt))
            {
                // Make two passes over the hash
                byte[] secondFirstHalf = deriveBytes.GetBytes(first.Length / 2);
                // Since we requested 20 bytes there are no 'extra' bytes left over to cause the "_extraCount" bug
                // in GetBytes(); that issue is tested in GetBytes_StreamLike_Bug_Compat.

                // Request 20 'extra' bytes in one call
                byte[] secondSecondHalf = deriveBytes.GetBytes(first.Length - secondFirstHalf.Length);

                Buffer.BlockCopy(secondFirstHalf, 0, second, 0, secondFirstHalf.Length);
                Buffer.BlockCopy(secondSecondHalf, 0, second, secondFirstHalf.Length, secondSecondHalf.Length);
            }

            Assert.Equal(first, second);
        }

        [Fact]
        public static void GetBytes_StreamLike_Bug_Compat()
        {
            byte[] first;

            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt))
            {
                Assert.Equal("SHA1", deriveBytes.HashName);

                // Request 20 bytes (SHA1 hash size) plus 12 extra bytes
                first = deriveBytes.GetBytes(32);
            }

            byte[] second = new byte[first.Length];

            // Reset
            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt))
            {
                // Ask for half now (16 bytes)
                byte[] firstHalf = deriveBytes.GetBytes(first.Length / 2);

                // Ask for the other half now (16 bytes)
                byte[] lastHalf = deriveBytes.GetBytes(first.Length - firstHalf.Length);

                // lastHalf should contain the last 4 bytes from the SHA1 hash plus 12 extra bytes
                // but due to the _extraCount bug it doesn't.

                // Merge the two buffers into the second array
                Buffer.BlockCopy(firstHalf, 0, second, 0, firstHalf.Length);
                Buffer.BlockCopy(lastHalf, 0, second, firstHalf.Length, lastHalf.Length);
            }

            // Fails due to _extraCount bug (the bug is fixed in Rfc2898DeriveBytes)
            Assert.NotEqual(first, second);

            // However, the first 16 bytes will be equal because the _extraCount bug does
            // not affect the first call, only the subsequent GetBytes() call.
            byte[] first_firstHalf = new byte[first.Length / 2];
            byte[] second_firstHalf = new byte[first.Length / 2];
            Buffer.BlockCopy(first, 0, first_firstHalf, 0, first_firstHalf.Length);
            Buffer.BlockCopy(second, 0, second_firstHalf, 0, second_firstHalf.Length);
            Assert.Equal(first_firstHalf, second_firstHalf);
        }

        [Fact]
        public static void GetBytes_Boundary()
        {
            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt))
            {
                // Boundary case success
                deriveBytes.GetBytes(1000 * 20);

                // Boundary case failure
                Assert.Throws<CryptographicException>(() => deriveBytes.GetBytes(1));
            }
        }

        [Fact]
        public static void GetBytes_KnownValues_MD5_32()
        {
            TestKnownValue_GetBytes(
                HashAlgorithmName.MD5,
                TestPassword,
                s_testSalt,
                DefaultIterationCount,
                ByteUtils.HexToByteArray("F8D88E9DAFC828DA2400F5144271C2F630A1C061C654FC9DE2E7900E121461B9"));
        }

        [Fact]
        public static void GetBytes_KnownValues_SHA256_40()
        {
            TestKnownValue_GetBytes(
                HashAlgorithmName.SHA256,
                TestPassword,
                s_testSalt,
                DefaultIterationCount,
                ByteUtils.HexToByteArray("3774A17468276057717A90C25B72915921D8F8C046F7868868DBB99BB4C4031CADE9E26BE77BEA39"));
        }

        [Fact]
        public static void GetBytes_KnownValues_SHA1_40()
        {
            TestKnownValue_GetBytes(
                HashAlgorithmName.SHA1,
                TestPassword,
                s_testSalt,
                DefaultIterationCount,
                ByteUtils.HexToByteArray("12F2497EC3EB78B0EA32AABFD8B9515FBC800BEEB6316A4DDF4EA62518341488A116DA3BBC26C685"));
        }

        [Fact]
        public static void GetBytes_KnownValues_SHA1_40_2()
        {
            TestKnownValue_GetBytes(
                HashAlgorithmName.SHA1,
                TestPassword,
                s_testSalt,
                DefaultIterationCount + 1,
                ByteUtils.HexToByteArray("FB6199E4D9BB017D2F3AF6964F3299971607C6B984934A9E43140631957429160C33A6630EF12E31"));
        }

        [Fact]
        public static void GetBytes_KnownValues_SHA1_40_3()
        {
            TestKnownValue_GetBytes(
                HashAlgorithmName.SHA1,
                TestPassword,
                s_testSaltB,
                DefaultIterationCount,
                ByteUtils.HexToByteArray("DCA4851AB3C9960CF387E64DE7A1B2E09616BEA6A4666AAFAC31F1670F23530E38BD4BF4D9248A08"));
        }

        [Fact]
        public static void GetBytes_KnownValues_SHA1_40_4()
        {
            TestKnownValue_GetBytes(
                HashAlgorithmName.SHA1,
                TestPasswordB,
                s_testSalt,
                DefaultIterationCount,
                ByteUtils.HexToByteArray("1DCA2A3405E93D9E3F7CD10653444F2FD93F5BE32C4B1BEDDF94D0D67461CBE86B5BDFEB32071E96"));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CryptDeriveKey on Unix
        public static void CryptDeriveKey_KnownValues_TripleDes()
        {
            byte[] key = TestKnownValue_CryptDeriveKey(
                HashAlgorithmName.SHA1,
                TestPassword,
                "TripleDES",
                192,
                s_testSalt,
                ByteUtils.HexToByteArray("97628A641949D99DCED35DB0ABCE20F21FF4DA9B46E00BCE"));

            // Verify key is valid
            using (var alg = new TripleDESCryptoServiceProvider())
            {
                alg.Key = key;
                alg.IV = new byte[8];
                alg.Padding = PaddingMode.None;
                alg.Mode = CipherMode.CBC;

                byte[] plainText = "79a86903608e133e020e1dc68c9835250c2f17b0ebeed91b".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = "9DC863445642B88AC46B3B107CB5A0ACC1596A176962EE8F".HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "79a86903608e133e020e1dc68c9835250c2f17b0ebeed91b".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CryptDeriveKey on Unix
        public static void CryptDeriveKey_KnownValues_RC2()
        {
            TestKnownValue_CryptDeriveKey(
                HashAlgorithmName.SHA1,
                TestPassword,
                "RC2",
                128,
                s_testSalt,
                ByteUtils.HexToByteArray("B0695D8D98F5844B9650A9F68EFF105B"));

            TestKnownValue_CryptDeriveKey(
                HashAlgorithmName.SHA256,
                TestPassword,
                "RC2",
                128,
                s_testSalt,
                ByteUtils.HexToByteArray("CF4A1CA60093E71D6B740DBB962B3C66"));

            TestKnownValue_CryptDeriveKey(
                HashAlgorithmName.MD5,
                TestPassword,
                "RC2",
                128,
                s_testSalt,
                ByteUtils.HexToByteArray("84F4B6854CDF896A86FB493B852B6E1F"));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CryptDeriveKey on Unix
        public static void CryptDeriveKey_KnownValues_RC2_NoSalt()
        {
            TestKnownValue_CryptDeriveKey(
                HashAlgorithmName.SHA1,
                TestPassword,
                "RC2",
                128,
                null, // Salt is not used here so we should get same key value
                ByteUtils.HexToByteArray("B0695D8D98F5844B9650A9F68EFF105B"));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CryptDeriveKey on Unix
        public static void CryptDeriveKey_KnownValues_DES()
        {
            TestKnownValue_CryptDeriveKey(
                HashAlgorithmName.SHA1,
                TestPassword,
                "DES",
                64,
                s_testSalt,
                ByteUtils.HexToByteArray("B0685D8C98F4854A"));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CryptDeriveKey on Unix
        public static void CryptDeriveKey_Invalid_KeyLength()
        {
            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt))
            {
                Assert.ThrowsAny<CryptographicException>(() => deriveBytes.CryptDeriveKey("RC2", "SHA1", 127, s_testSalt));
                Assert.ThrowsAny<CryptographicException>(() => deriveBytes.CryptDeriveKey("RC2", "SHA1", 129, s_testSalt));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CryptDeriveKey on Unix
        public static void CryptDeriveKey_Invalid_Algorithm()
        {
            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt))
            {
                Assert.Throws<CryptographicException>(() => deriveBytes.CryptDeriveKey("BADALG", "SHA1", 128, s_testSalt));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CryptDeriveKey on Unix
        public static void CryptDeriveKey_Invalid_HashAlgorithm()
        {
            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt))
            {
                Assert.Throws<CryptographicException>(() => deriveBytes.CryptDeriveKey("RC2", "BADALG", 128, s_testSalt));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CryptDeriveKey on Unix
        public static void CryptDeriveKey_Invalid_IV()
        {
            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt))
            {
                Assert.Throws<CryptographicException>(() => deriveBytes.CryptDeriveKey("RC2", "SHA1", 128, null));
                Assert.Throws<CryptographicException>(() => deriveBytes.CryptDeriveKey("RC2", "SHA1", 128, new byte[1]));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void CryptDeriveKey_Throws_Unix()
        {
            using (var deriveBytes = new PasswordDeriveBytes(TestPassword, s_testSalt))
            {
                Assert.Throws<PlatformNotSupportedException>(() => (deriveBytes.CryptDeriveKey("RC2", "SHA1", 128, null)));
            }
        }

        private static byte[] TestKnownValue_CryptDeriveKey(HashAlgorithmName hashName, string password, string alg, int keySize, byte[] salt, byte[] expected)
        {
            byte[] output;
            byte[] iv = new byte[8];

            using (var deriveBytes = new PasswordDeriveBytes(password, salt))
            {
                output = deriveBytes.CryptDeriveKey(alg, hashName.Name, keySize, iv);
            }

            Assert.Equal(expected, output);

            // For these tests, the returned IV is always zero
            Assert.Equal(new byte[8], iv);

            return output;
        }

        private static void TestKnownValue_GetBytes(HashAlgorithmName hashName, string password, byte[] salt, int iterationCount, byte[] expected)
        {
            byte[] output;

            using (var deriveBytes = new PasswordDeriveBytes(password, salt, hashName.Name, iterationCount))
            {
                output = deriveBytes.GetBytes(expected.Length);
            }

            Assert.Equal(expected, output);
        }
    }
}
