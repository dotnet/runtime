// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.DeriveBytesTests
{
    public class Rfc2898Tests
    {
        // 8 bytes is the minimum accepted value, by using it we've already assured that the minimum is acceptable.
        private static readonly byte[] s_testSalt = new byte[] { 9, 5, 5, 5, 1, 2, 1, 2 };
        private static readonly byte[] s_testSaltB = new byte[] { 0, 4, 0, 4, 1, 9, 7, 5 };
        private const string TestPassword = "PasswordGoesHere";
        private const string TestPasswordB = "FakePasswordsAreHard";
        private const int DefaultIterationCount = 1000;

        [Fact]
        public static void Ctor_NullPasswordBytes()
        {
            Assert.Throws<NullReferenceException>(() => new Rfc2898DeriveBytes((byte[])null, s_testSalt, DefaultIterationCount));
        }

        [Fact]
        public static void Ctor_NullPasswordString()
        {
            Assert.Throws<ArgumentNullException>(() => new Rfc2898DeriveBytes((string)null, s_testSalt, DefaultIterationCount));
        }

        [Fact]
        public static void Ctor_NullSalt()
        {
            Assert.Throws<ArgumentNullException>(() => new Rfc2898DeriveBytes(TestPassword, null, DefaultIterationCount));
        }

        [Fact]
        public static void Ctor_EmptySalt()
        {
            Assert.Throws<ArgumentException>(() => new Rfc2898DeriveBytes(TestPassword, Array.Empty<byte>(), DefaultIterationCount));
        }

        [Fact]
        public static void Ctor_DiminishedSalt()
        {
            Assert.Throws<ArgumentException>(() => new Rfc2898DeriveBytes(TestPassword, new byte[7], DefaultIterationCount));
        }

        [Fact]
        public static void Ctor_GenerateZeroSalt()
        {
            Assert.Throws<ArgumentException>(() => new Rfc2898DeriveBytes(TestPassword, 0));
        }

        [Fact]
        public static void Ctor_GenerateNegativeSalt()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Rfc2898DeriveBytes(TestPassword, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Rfc2898DeriveBytes(TestPassword, int.MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Rfc2898DeriveBytes(TestPassword, int.MinValue / 2));
        }

        [Fact]
        public static void Ctor_GenerateDiminishedSalt()
        {
            Assert.Throws<ArgumentException>(() => new Rfc2898DeriveBytes(TestPassword, 7));
        }

        [Fact]
        public static void Ctor_TooFewIterations()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Rfc2898DeriveBytes(TestPassword, s_testSalt, 0));
        }

        [Fact]
        public static void Ctor_NegativeIterations()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Rfc2898DeriveBytes(TestPassword, s_testSalt, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Rfc2898DeriveBytes(TestPassword, s_testSalt, int.MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Rfc2898DeriveBytes(TestPassword, s_testSalt, int.MinValue / 2));
        }

        [Fact]
        public static void Ctor_SaltCopied()
        {
            byte[] saltIn = (byte[])s_testSalt.Clone();

            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, saltIn, DefaultIterationCount))
            {
                byte[] saltOut = deriveBytes.Salt;

                Assert.NotSame(saltIn, saltOut);
                Assert.Equal(saltIn, saltOut);

                // Right now we know that at least one of the constructor and get_Salt made a copy, if it was
                // only get_Salt then this next part would fail.

                saltIn[0] = (byte)~saltIn[0];

                // Have to read the property again to prove it's detached.
                Assert.NotEqual(saltIn, deriveBytes.Salt);
            }
        }

        [Fact]
        public static void Ctor_DefaultIterations()
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt))
            {
                Assert.Equal(DefaultIterationCount, deriveBytes.IterationCount);
            }
        }

        [Fact]
        public static void Ctor_IterationsRespected()
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt, 1))
            {
                Assert.Equal(1, deriveBytes.IterationCount);
            }
        }

        [Fact]
        public static void GetSaltCopies()
        {
            byte[] first;
            byte[] second;

            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt, DefaultIterationCount))
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

            using (var deriveBytes = new Rfc2898DeriveBytes("", new byte[8], 1))
            {
                output = deriveBytes.GetBytes(1);
            }

            Assert.Equal(1, output.Length);
            Assert.Equal(0xA6, output[0]);
        }

        [Fact]
        public static void GetBytes_ZeroLength()
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => deriveBytes.GetBytes(0));
            }
        }

        [Fact]
        public static void GetBytes_NegativeLength()
        {
            Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt);
            Assert.Throws<ArgumentOutOfRangeException>(() => deriveBytes.GetBytes(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => deriveBytes.GetBytes(int.MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => deriveBytes.GetBytes(int.MinValue / 2));
        }

        [Fact]
        public static void GetBytes_NotIdempotent()
        {
            byte[] first;
            byte[] second;

            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt))
            {
                first = deriveBytes.GetBytes(32);
                second = deriveBytes.GetBytes(32);
            }

            Assert.NotEqual(first, second);
        }

        [Fact]
        public static void GetBytes_StreamLike()
        {
            byte[] first;

            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt))
            {
                first = deriveBytes.GetBytes(32);
            }

            byte[] second = new byte[first.Length];

            // Reset
            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt))
            {
                byte[] secondFirstHalf = deriveBytes.GetBytes(first.Length / 2);
                byte[] secondSecondHalf = deriveBytes.GetBytes(first.Length - secondFirstHalf.Length);

                Buffer.BlockCopy(secondFirstHalf, 0, second, 0, secondFirstHalf.Length);
                Buffer.BlockCopy(secondSecondHalf, 0, second, secondFirstHalf.Length, secondSecondHalf.Length);
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

#if netstandard17
        public static void CryptDeriveKey_NotSupported()
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(TestPassword, s_testSalt))
            {
                Assert.Throws<PlatformNotSupportedException>(() => deriveBytes.CryptDeriveKey("RC2", "SHA1", 128, new byte[8]));
            }
        }
#endif

        private static void TestKnownValue(string password, byte[] salt, int iterationCount, byte[] expected)
        {
            byte[] output;

            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterationCount))
            {
                output = deriveBytes.GetBytes(expected.Length);
            }

            Assert.Equal(expected, output);
        }
    }
}
