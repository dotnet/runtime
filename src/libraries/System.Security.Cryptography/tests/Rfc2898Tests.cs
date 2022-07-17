// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography
{
    public class Rfc2898Tests
    {
        private static readonly byte[] s_testSalt = new byte[] { 9, 5, 5, 5, 1, 2, 1, 2 };
        private static readonly byte[] s_testSaltB = new byte[] { 0, 4, 0, 4, 1, 9, 7, 5 };
        private const string TestPassword = "PasswordGoesHere";
        private const string TestPasswordB = "FakePasswordsAreHard";
        private const int DefaultIterationCount = 1000;

        [Fact]
        public static void Ctor_NullPasswordBytes()
        {
            Assert.Throws<ArgumentNullException>("password", () =>
                new Rfc2898DeriveBytes((byte[])null, s_testSalt, DefaultIterationCount, HashAlgorithmName.SHA1));
        }

        [Fact]
        public static void Ctor_NullPasswordString()
        {
            Assert.Throws<ArgumentNullException>("password", () =>
                new Rfc2898DeriveBytes((string)null, s_testSalt, DefaultIterationCount, HashAlgorithmName.SHA1));
        }

        [Fact]
        public static void Ctor_NullSalt()
        {
            AssertExtensions.Throws<ArgumentNullException>("salt", () =>
                new Rfc2898DeriveBytes(TestPassword, null, DefaultIterationCount, HashAlgorithmName.SHA1));
        }

        [Fact]
        public static void Ctor_GenerateNegativeSalt()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("saltSize", () =>
                new Rfc2898DeriveBytes(TestPassword, -1, DefaultIterationCount, HashAlgorithmName.SHA1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("saltSize", () =>
                new Rfc2898DeriveBytes(TestPassword, int.MinValue, DefaultIterationCount, HashAlgorithmName.SHA1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("saltSize", () =>
                new Rfc2898DeriveBytes(TestPassword, int.MinValue / 2, DefaultIterationCount, HashAlgorithmName.SHA1));
        }

        [Fact]
        public static void Ctor_TooFewIterations()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("iterations", () =>
                new Rfc2898DeriveBytes(TestPassword, s_testSalt, 0, HashAlgorithmName.SHA1));
        }

        [Fact]
        public static void Ctor_NegativeIterations()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("iterations", () =>
                new Rfc2898DeriveBytes(TestPassword, s_testSalt, -1, HashAlgorithmName.SHA1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("iterations", () =>
                new Rfc2898DeriveBytes(TestPassword, s_testSalt, int.MinValue, HashAlgorithmName.SHA1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("iterations", () =>
                new Rfc2898DeriveBytes(TestPassword, s_testSalt, int.MinValue / 2, HashAlgorithmName.SHA1));
        }

        [Fact]
        public static void Ctor_EmptyAlgorithm()
        {
            HashAlgorithmName alg = default(HashAlgorithmName);

            // (byte[], byte[], int, HashAlgorithmName)
            Assert.Throws<CryptographicException>(() => new Rfc2898DeriveBytes(s_testSalt, s_testSalt, DefaultIterationCount, alg));
            // (string, byte[], int, HashAlgorithmName)
            Assert.Throws<CryptographicException>(() => new Rfc2898DeriveBytes(TestPassword, s_testSalt, DefaultIterationCount, alg));
            // (string, int, int, HashAlgorithmName)
            Assert.Throws<CryptographicException>(() => new Rfc2898DeriveBytes(TestPassword, 8, DefaultIterationCount, alg));
        }

        [Fact]
        public static void Ctor_MD5NotSupported()
        {
            Assert.Throws<CryptographicException>(
                () => new Rfc2898DeriveBytes(TestPassword, s_testSalt, DefaultIterationCount, HashAlgorithmName.MD5));
        }

        [Fact]
        public static void Ctor_UnknownAlgorithm()
        {
            Assert.Throws<CryptographicException>(
                () => new Rfc2898DeriveBytes(TestPassword, s_testSalt, DefaultIterationCount, new HashAlgorithmName("PotatoLemming")));
        }

        [Fact]
        public static void Ctor_SaltCopied()
        {
            byte[] saltIn = (byte[])s_testSalt.Clone();

            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, saltIn, DefaultIterationCount, HashAlgorithmName.SHA1))
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
        public static void Ctor_DefaultIterations()
        {
#pragma warning disable SYSLIB0041 // Rfc2898DeriveBytes insecure constructor defaults
            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt))
#pragma warning restore SYSLIB0041
            {
                Assert.Equal(DefaultIterationCount, deriveBytes.IterationCount);
            }
        }

        [Fact]
        public static void Ctor_GenerateEmptySalt()
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, 0, 1, HashAlgorithmName.SHA1))
            {
                Assert.Empty(deriveBytes.Salt);
            }
        }

        [Fact]
        public static void Ctor_IterationsRespected()
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt, 1, HashAlgorithmName.SHA1))
            {
                Assert.Equal(1, deriveBytes.IterationCount);
            }
        }

        [Fact]
        public static void GetSaltCopies()
        {
            byte[] first;
            byte[] second;

            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt, DefaultIterationCount, HashAlgorithmName.SHA1))
            {
                first = deriveBytes.Salt;
                second = deriveBytes.Salt;
            }

            Assert.NotSame(first, second);
            Assert.Equal(first, second);
        }

        [Fact]
        public static void MinimumAcceptableInputs()
        {
            byte[] output;

            using (var deriveBytes = new Rfc2898DeriveBytes("", Array.Empty<byte>(), 1, HashAlgorithmName.SHA1))
            {
                output = deriveBytes.GetBytes(1);
                Assert.Empty(deriveBytes.Salt);
            }

            Assert.Equal(1, output.Length);
            Assert.Equal(0x1E, output[0]);
        }

        [Fact]
        public static void GetBytes_ZeroLength()
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt, DefaultIterationCount, HashAlgorithmName.SHA1))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => deriveBytes.GetBytes(0));
            }
        }

        [Fact]
        public static void GetBytes_NegativeLength()
        {
            using Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt, DefaultIterationCount, HashAlgorithmName.SHA1);
            Assert.Throws<ArgumentOutOfRangeException>(() => deriveBytes.GetBytes(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => deriveBytes.GetBytes(int.MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => deriveBytes.GetBytes(int.MinValue / 2));
        }

        [Fact]
        public static void GetBytes_NotIdempotent()
        {
            byte[] first;
            byte[] second;

            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt, DefaultIterationCount, HashAlgorithmName.SHA1))
            {
                first = deriveBytes.GetBytes(32);
                second = deriveBytes.GetBytes(32);
            }

            Assert.NotEqual(first, second);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(16)]
        [InlineData(20)]
        [InlineData(25)]
        [InlineData(32)]
        [InlineData(40)]
        [InlineData(192)]
        public static void GetBytes_StreamLike(int size)
        {
            byte[] first;

            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt, DefaultIterationCount, HashAlgorithmName.SHA1))
            {
                first = deriveBytes.GetBytes(size);
            }

            byte[] second = new byte[first.Length];

            // Reset
            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt, DefaultIterationCount, HashAlgorithmName.SHA1))
            {
                byte[] secondFirstHalf = deriveBytes.GetBytes(first.Length / 2);
                byte[] secondSecondHalf = deriveBytes.GetBytes(first.Length - secondFirstHalf.Length);

                Buffer.BlockCopy(secondFirstHalf, 0, second, 0, secondFirstHalf.Length);
                Buffer.BlockCopy(secondSecondHalf, 0, second, secondFirstHalf.Length, secondSecondHalf.Length);
            }

            Assert.Equal(first, second);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(16)]
        [InlineData(20)]
        [InlineData(25)]
        [InlineData(32)]
        [InlineData(40)]
        [InlineData(192)]
        public static void GetBytes_StreamLike_OneAtATime(int size)
        {
            byte[] first;

            using (var deriveBytes = new Rfc2898DeriveBytes(TestPasswordB, s_testSaltB, DefaultIterationCount, HashAlgorithmName.SHA1))
            {
                first = deriveBytes.GetBytes(size);
            }

            byte[] second = new byte[first.Length];

            // Reset
            using (var deriveBytes = new Rfc2898DeriveBytes(TestPasswordB, s_testSaltB, DefaultIterationCount, HashAlgorithmName.SHA1))
            {
                for (int i = 0; i < second.Length; i++)
                {
                    second[i] = deriveBytes.GetBytes(1)[0];
                }
            }

            Assert.Equal(first, second);
        }

        [Fact]
        public static void GetBytes_KnownValues_1()
        {
            TestKnownValue(
                TestPassword,
                s_testSalt,
                DefaultIterationCount,
                new byte[]
                {
                    0x6C, 0x3C, 0x55, 0xA4, 0x2E, 0xE9, 0xD6, 0xAE,
                    0x7D, 0x28, 0x6C, 0x83, 0xE4, 0xD7, 0xA3, 0xC8,
                    0xB5, 0x93, 0x9F, 0x45, 0x2F, 0x2B, 0xF3, 0x68,
                    0xFA, 0xE8, 0xB2, 0x74, 0x55, 0x3A, 0x36, 0x8A,
                });
        }

        [Fact]
        public static void GetBytes_KnownValues_2()
        {
            TestKnownValue(
                TestPassword,
                s_testSalt,
                DefaultIterationCount + 1,
                new byte[]
                {
                    0x8E, 0x9B, 0xF7, 0xC1, 0x83, 0xD4, 0xD1, 0x20,
                    0x87, 0xA8, 0x2C, 0xD7, 0xCD, 0x84, 0xBC, 0x1A,
                    0xC6, 0x7A, 0x7A, 0xDD, 0x46, 0xFA, 0x40, 0xAA,
                    0x60, 0x3A, 0x2B, 0x8B, 0x79, 0x2C, 0x8A, 0x6D,
                });
        }

        [Fact]
        public static void GetBytes_KnownValues_3()
        {
            TestKnownValue(
                TestPassword,
                s_testSaltB,
                DefaultIterationCount,
                new byte[]
                {
                    0x4E, 0xF5, 0xA5, 0x85, 0x92, 0x9D, 0x8B, 0xC5,
                    0x57, 0x0C, 0x83, 0xB5, 0x19, 0x69, 0x4B, 0xC2,
                    0x4B, 0xAA, 0x09, 0xE9, 0xE7, 0x9C, 0x29, 0x94,
                    0x14, 0x19, 0xE3, 0x61, 0xDA, 0x36, 0x5B, 0xB3,
                });
        }

        [Fact]
        public static void GetBytes_KnownValues_4()
        {
            TestKnownValue(
                TestPasswordB,
                s_testSalt,
                DefaultIterationCount,
                new byte[]
                {
                    0x86, 0xBB, 0xB3, 0xD7, 0x99, 0x0C, 0xAC, 0x4D,
                    0x1D, 0xB2, 0x78, 0x9D, 0x57, 0x5C, 0x06, 0x93,
                    0x97, 0x50, 0x72, 0xFF, 0x56, 0x57, 0xAC, 0x7F,
                    0x9B, 0xD2, 0x14, 0x9D, 0xE9, 0x95, 0xA2, 0x6D,
                });
        }

        [Theory]
        [MemberData(nameof(KnownValuesTestCases))]
        public static void GetBytes_KnownValues_WithAlgorithm(KnownValuesTestCase testCase)
        {
            byte[] output;

            var pbkdf2 = new Rfc2898DeriveBytes(
                testCase.Password,
                testCase.Salt,
                testCase.IterationCount,
                new HashAlgorithmName(testCase.HashAlgorithmName));

            using (pbkdf2)
            {
                output = pbkdf2.GetBytes(testCase.AnswerHex.Length / 2);
            }

            Assert.Equal(testCase.AnswerHex, output.ByteArrayToHex());
        }

        [Theory]
        [InlineData("SHA1")]
        [InlineData("SHA256")]
        [InlineData("SHA384")]
        [InlineData("SHA512")]
        public static void CheckHashAlgorithmValue(string hashAlgorithmName)
        {
            HashAlgorithmName hashAlgorithm = new HashAlgorithmName(hashAlgorithmName);

            using (var pbkdf2 = new Rfc2898DeriveBytes(TestPassword, s_testSalt, DefaultIterationCount, hashAlgorithm))
            {
                Assert.Equal(hashAlgorithm, pbkdf2.HashAlgorithm);
            }
        }

        [Fact]
        public static void CryptDeriveKey_NotSupported()
        {
#pragma warning disable SYSLIB0041 // Rfc2898DeriveBytes insecure constructor defaults
#pragma warning disable SYSLIB0033 // Rfc2898DeriveBytes.CryptDeriveKey is obsolete
            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt))
            {
                Assert.Throws<PlatformNotSupportedException>(() => deriveBytes.CryptDeriveKey("RC2", "SHA1", 128, new byte[8]));
#pragma warning restore SYSLIB0033
#pragma warning restore SYSLIB0041
            }
        }

        [Fact]
        public static void GetBytes_ExceedCounterLimit()
        {
            FieldInfo blockField = typeof(Rfc2898DeriveBytes).GetField("_block", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(blockField);

            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt, 1, HashAlgorithmName.SHA1))
            {
                // Set the internal block counter to be on the last possible block. This should succeed.
                blockField.SetValue(deriveBytes, uint.MaxValue - 1);
                deriveBytes.GetBytes(20); // Extract 20 bytes, a full block.

                // Block should now be uint.MaxValue, which will overflow when writing.
                Assert.Throws<CryptographicException>(() => deriveBytes.GetBytes(1));
            }
        }

        [Fact]
        public static void Ctor_PasswordMutatedAfterCreate()
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(TestPassword);
            byte[] derived;

            using (var deriveBytes = new Rfc2898DeriveBytes(passwordBytes, s_testSaltB, DefaultIterationCount, HashAlgorithmName.SHA1))
            {
                derived = deriveBytes.GetBytes(64);
            }

            using (var deriveBytes = new Rfc2898DeriveBytes(passwordBytes, s_testSaltB, DefaultIterationCount, HashAlgorithmName.SHA1))
            {
                passwordBytes[0] ^= 0xFF; // Flipping a byte after the object is constructed should not be observed.

                byte[] actual = deriveBytes.GetBytes(64);
                Assert.Equal(derived, actual);
            }
        }

        [Fact]
        public static void Ctor_PasswordBytes_NotCleared()
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(TestPassword);
            byte[] passwordBytesOriginal = passwordBytes.AsSpan().ToArray();

            using (var deriveBytes = new Rfc2898DeriveBytes(passwordBytes, s_testSaltB, DefaultIterationCount, HashAlgorithmName.SHA1))
            {
                Assert.Equal(passwordBytesOriginal, passwordBytes);
            }
        }

        private static void TestKnownValue(string password, byte[] salt, int iterationCount, byte[] expected)
        {
            byte[] output;

            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterationCount, HashAlgorithmName.SHA1))
            {
                output = deriveBytes.GetBytes(expected.Length);
            }

            Assert.Equal(expected, output);
        }

        public static IEnumerable<object[]> KnownValuesTestCases()
        {
            HashSet<string> testCaseNames = new HashSet<string>();

            // Wrap the class in the MemberData-required-object[].
            foreach (KnownValuesTestCase testCase in GetKnownValuesTestCases())
            {
                if (!testCaseNames.Add(testCase.CaseName))
                {
                    throw new InvalidOperationException($"Duplicate test case name: {testCase.CaseName}");
                }

                yield return new object[] { testCase };
            }
        }

        private static IEnumerable<KnownValuesTestCase> GetKnownValuesTestCases()
        {
            Encoding ascii = Encoding.ASCII;

            yield return new KnownValuesTestCase
            {
                CaseName = "RFC 3211 Section 3 #1",
                HashAlgorithmName = "SHA1",
                Password = "password",
                Salt = "1234567878563412".HexToByteArray(),
                IterationCount = 5,
                AnswerHex = "D1DAA78615F287E6",
            };

            yield return new KnownValuesTestCase
            {
                CaseName = "RFC 3211 Section 3 #2",
                HashAlgorithmName = "SHA1",
                Password = "All n-entities must communicate with other n-entities via n-1 entiteeheehees",
                Salt = "1234567878563412".HexToByteArray(),
                IterationCount = 500,
                AnswerHex = "6A8970BF68C92CAEA84A8DF28510858607126380CC47AB2D",
            };

            yield return new KnownValuesTestCase
            {
                CaseName = "RFC 6070 Case 1",
                HashAlgorithmName = "SHA1",
                Password = "password",
                Salt = ascii.GetBytes("salt"),
                IterationCount = 1,
                AnswerHex="0C60C80F961F0E71F3A9B524AF6012062FE037A6",
            };

            yield return new KnownValuesTestCase
            {
                CaseName = "RFC 6070 Case 5",
                HashAlgorithmName = "SHA1",
                Password = "passwordPASSWORDpassword",
                Salt = ascii.GetBytes("saltSALTsaltSALTsaltSALTsaltSALTsalt"),
                IterationCount = 4096,
                AnswerHex = "3D2EEC4FE41C849B80C8D83662C0E44A8B291A964CF2F07038",
            };

            // From OpenSSL.
            // https://github.com/openssl/openssl/blob/6f0ac0e2f27d9240516edb9a23b7863e7ad02898/test/evptests.txt
            // Corroborated on http://stackoverflow.com/questions/5130513/pbkdf2-hmac-sha2-test-vectors,
            // though the SO answer stopped at 25 bytes.
            yield return new KnownValuesTestCase
            {
                CaseName = "RFC 6070#5 SHA256",
                HashAlgorithmName = "SHA256",
                Password = "passwordPASSWORDpassword",
                Salt = ascii.GetBytes("saltSALTsaltSALTsaltSALTsaltSALTsalt"),
                IterationCount = 4096,
                AnswerHex =
                    "348C89DBCBD32B2F32D814B8116E84CF2B17347EBC1800181C4E2A1FB8DD53E1C635518C7DAC47E9",
            };

            // From OpenSSL.
            yield return new KnownValuesTestCase
            {
                CaseName = "RFC 6070#5 SHA512",
                HashAlgorithmName = "SHA512",
                Password = "passwordPASSWORDpassword",
                Salt = ascii.GetBytes("saltSALTsaltSALTsaltSALTsaltSALTsalt"),
                IterationCount = 4096,
                AnswerHex = (
                    "8C0511F4C6E597C6AC6315D8F0362E225F3C501495BA23B868C005174DC4EE71" +
                    "115B59F9E60CD9532FA33E0F75AEFE30225C583A186CD82BD4DAEA9724A3D3B8"),
            };

            // Verified against BCryptDeriveKeyPBKDF2, as an independent implementation.
            yield return new KnownValuesTestCase
            {
                CaseName = "RFC 3962 Appendix B#1 SHA384-24000",
                HashAlgorithmName = "SHA384",
                Password = "password",
                Salt = ascii.GetBytes("ATHENA.MIT.EDUraeburn"),
                IterationCount = 24000,
                AnswerHex = (
                    "4B138897F289129C6E80965F96B940F76BBC0363CD22190E0BD94ADBA79BE33E" +
                    "02C9D8E0AF0D19B295B02828770587F672E0ED182A9A59BA5E07120CA936E6BF" +
                    "F5D425688253C2A8336ED30DA898C67FD9DDFD8EF3F8C708392E2E2458716DF8" +
                    "6799372DEF27AB36AF239D7D654A56A51395086A322B9322977F62A98662B57E"),
            };

            // These "alternate" tests are made up, due to a lack of test corpus diversity
            yield return new KnownValuesTestCase
            {
                CaseName = "SHA256 alternate",
                HashAlgorithmName = "SHA256",
                Password = "PLACEHOLDER",
                Salt = ascii.GetBytes("abcdefghij"),
                IterationCount = 1,
                AnswerHex = (
                    // T-Block 1
                    "9352784113E5E6DC21FC82ADA3A321D64962F760DF6EAA8E46CEEF4FAF6C6E" +
                    // T-Block 2
                    "EE6DB97E5852FC4C15FA7C52FACDEDE89B916BCC864028084A2CF0889F7F76"),
            };

            yield return new KnownValuesTestCase
            {
                CaseName = "SHA384 alternate",
                HashAlgorithmName = "SHA384",
                Password = "PLACEHOLDER",
                Salt = ascii.GetBytes("abcdefghij"),
                IterationCount = 1,
                AnswerHex = (
                    // T-Block 1
                    "B9A10C6C82F36482D76C0C38C982C05F8BB21211ACBE1D1104B4F647DDEAEE179B92ACB0E00A304B791FD0" +
                    // T-Block 2
                    "3C6A08364D0A47CD1F15E0E314800FF3AC9CF2E93B3F81A5EB67FE9F2FE6E86B0430B59902CCB5FD190E67"),
            };

            yield return new KnownValuesTestCase
            {
                CaseName = "SHA512 alternate",
                HashAlgorithmName = "SHA512",
                Password = "PLACEHOLDER",
                Salt = ascii.GetBytes("abcdefghij"),
                IterationCount = 1,
                AnswerHex = (
                    // T-Block 1
                    "AD8CE08CFA8F932CF9FEDDCDB6E4BC6417D61F0465D408C0BFE9656E2C1C47" +
                    "1424537ADB2D9EBE4E4232F474EFEE2AF347F21A804F64CBC05474A6DCE0A5" +
                    // T-Block 2
                    "078100F813C1F8388EC233C1397D5E18C6509B5483141EF836C15A34D6DC67" +
                    "A3C46A45798A2839CFD239749219E9F2EDAD3249EC8221AFB17C0028A4A0A5"),
            };

            // Taken from the OneShot tests, which is implemented using native platform APIs.
            yield return new KnownValuesTestCase
            {
                CaseName = "SHA1 empty",
                HashAlgorithmName = "SHA1",
                Password = "",
                Salt = Array.Empty<byte>(),
                IterationCount = 1,
                AnswerHex = "1E437A1C79D75BE61E91141DAE20",
            };
        }

        public class KnownValuesTestCase
        {
            public string CaseName { get; set; }
            public string HashAlgorithmName { get; set; }
            public string Password { get; set; }
            public byte[] Salt { get; set; }
            public int IterationCount { get; set; }
            public string AnswerHex { get; set; }

            public override string ToString()
            {
                return CaseName;
            }
        }
    }
}
