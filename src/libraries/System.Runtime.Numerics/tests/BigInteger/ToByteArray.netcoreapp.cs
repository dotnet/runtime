// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Numerics.Tests
{
    public partial class ExtractBytesMembersTests
    {
        public static IEnumerable<object[]> FromIntTests_MemberData() =>
            MatrixGenerator(FromIntTests_MemberDataSeed(), false);

        [Theory]
        [MemberData(nameof(FromIntTests_MemberData))]
        public void ToByteArray_FromIntTests(int i, bool isUnsigned, bool isBigEndian, byte[] expectedBytes)
        {
            BigInteger bi = new BigInteger(i);

            if (i < 0 && isUnsigned)
            {
                Assert.Throws<OverflowException>(() => bi.ToByteArray(isUnsigned, isBigEndian));
                return;
            }

            byte[] bytes = bi.ToByteArray(isUnsigned, isBigEndian);
            Assert.Equal(expectedBytes, bytes);
            BigInteger bi2 = new BigInteger(bytes, isUnsigned, isBigEndian);
            Assert.Equal(bi, bi2);
        }

        public static IEnumerable<object[]> FromLongTests_MemberData() =>
            MatrixGenerator(FromLongTests_MemberDataSeed(), false);

        [Theory]
        [MemberData(nameof(FromLongTests_MemberData))]
        public void ToByteArray_FromLongTests(long l, bool isUnsigned, bool isBigEndian, byte[] expectedBytes)
        {
            BigInteger bi = new BigInteger(l);

            if (l < 0 && isUnsigned)
            {
                Assert.Throws<OverflowException>(() => bi.ToByteArray(isUnsigned, isBigEndian));
                return;
            }

            byte[] bytes = bi.ToByteArray(isUnsigned, isBigEndian);
            Assert.Equal(expectedBytes, bytes);
            BigInteger bi2 = new BigInteger(bytes, isUnsigned, isBigEndian);
            Assert.Equal(bi, bi2);
        }

        public static IEnumerable<object[]> FromStringTests_MemberData() =>
            MatrixGenerator(FromStringTests_MemberDataSeed(), true);

        [Theory]
        public void ToByteArray_FromStringTests(string str, bool isUnsigned, bool isBigEndian, byte[] expectedBytes)
        {
            BigInteger bi = BigInteger.Parse(str);

            if (str[0] == '-' && isUnsigned)
            {
                Assert.Throws<OverflowException>(() => bi.ToByteArray(isUnsigned, isBigEndian));
                return;
            }

            byte[] bytes = bi.ToByteArray(isUnsigned, isBigEndian);
            Assert.Equal(expectedBytes, bytes);
            BigInteger bi2 = new BigInteger(bytes, isUnsigned, isBigEndian);
            Assert.Equal(bi, bi2);
        }

        private static IEnumerable<object[]> MatrixGenerator(IEnumerable<object[]> seedData, bool dataIsBigEndian)
        {
            foreach (object[] seed in seedData)
            {
                object value = seed[0];
                byte[] leSignedBytes = (byte[])seed[1];
                byte[] beSignedBytes = (byte[])leSignedBytes.Clone();
                Array.Reverse(beSignedBytes);

                if (dataIsBigEndian)
                {
                    var tmp = leSignedBytes;
                    leSignedBytes = beSignedBytes;
                    beSignedBytes = tmp;
                }

                // Signed Little Endian
                yield return new object[] { value, false, false, leSignedBytes };

                // Signed Big Endian
                yield return new object[] { value, false, true, beSignedBytes };

                byte[] leUnsignedBytes;
                byte[] beUnsignedBytes;

                if (beSignedBytes.Length > 1 &&
                    beSignedBytes[0] == 0)
                {
                    leUnsignedBytes = new Span<byte>(leSignedBytes, 0, leSignedBytes.Length - 1).ToArray();
                    beUnsignedBytes = new Span<byte>(beSignedBytes, 1, beSignedBytes.Length - 1).ToArray();
                }
                else
                {
                    // No padding was required, the unsigned data is the same as the signed data.
                    leUnsignedBytes = leSignedBytes;
                    beUnsignedBytes = beSignedBytes;
                }

                // Unsigned Big Endian
                yield return new object[] { value, true, true, beUnsignedBytes };

                // Unsigned Little Endian
                yield return new object[] { value, true, false, leUnsignedBytes };
            }
        }
    }
}
