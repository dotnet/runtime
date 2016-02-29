// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingGetByteCount
    {
        private const int MinStringLength = 1;
        private const int MaxStringLength = 260;
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> GetByteCount_TestData()
        {
            string randomString = s_randomDataGenerator.GetString(-55, false, MinStringLength, MaxStringLength);
            int randomIndex = s_randomDataGenerator.GetInt32(-55) % randomString.Length;
            int randomCount = s_randomDataGenerator.GetInt32(-55) % (randomString.Length - randomIndex) + 1;

            yield return new object[] { string.Empty, 0, 0, 0 };
            yield return new object[] { randomString, 0, randomString.Length, randomString.Length };
            yield return new object[] { randomString, randomIndex, randomCount, randomCount };
        }

        [Theory]
        [MemberData(nameof(GetByteCount_TestData))]
        private void GetByteCount(string chars, int index, int count, int expected)
        {
            char[] charArray = chars.ToCharArray();
            if (index == 0 && count == chars.Length)
            {
                Assert.Equal(expected, new ASCIIEncoding().GetByteCount(chars));
                Assert.Equal(expected, new ASCIIEncoding().GetByteCount(charArray));
            }
            Assert.Equal(expected, new ASCIIEncoding().GetByteCount(charArray, index, count));
        }

        [Fact]
        public void GetByteCount_Invalid()
        {
            // Chars is null
            Assert.Throws<ArgumentNullException>("chars", () => new ASCIIEncoding().GetByteCount((string)null));
            Assert.Throws<ArgumentNullException>("chars", () => new ASCIIEncoding().GetByteCount((char[])null));
            Assert.Throws<ArgumentNullException>("chars", () => new ASCIIEncoding().GetByteCount(null, 0, 0));

            // Index or count < 0
            Assert.Throws<ArgumentOutOfRangeException>("index", () => new ASCIIEncoding().GetByteCount(new char[3], -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => new ASCIIEncoding().GetByteCount(new char[3], 0, -1));

            // Index + count > chars.Length
            Assert.Throws<ArgumentOutOfRangeException>("chars", () => new ASCIIEncoding().GetByteCount(new char[3], 0, 4));
            Assert.Throws<ArgumentOutOfRangeException>("chars", () => new ASCIIEncoding().GetByteCount(new char[3], 4, 0));
            Assert.Throws<ArgumentOutOfRangeException>("chars", () => new ASCIIEncoding().GetByteCount(new char[3], 3, 1));
        }
    }
}
