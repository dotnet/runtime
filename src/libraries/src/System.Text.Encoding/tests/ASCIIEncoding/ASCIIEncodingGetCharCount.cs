// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingGetCharCount
    {
        private const int MinStringLength = 2;
        private const int MaxStringLength = 260;
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> GetCharCount_TestData()
        {
            string randomString = s_randomDataGenerator.GetString(-55, false, MinStringLength, MaxStringLength);
            int randomIndex = s_randomDataGenerator.GetInt32(-55) % randomString.Length;
            int randomCount = s_randomDataGenerator.GetInt32(-55) % (randomString.Length - randomIndex) + 1;

            yield return new object[] { new byte[0], 0, 0, 0 };
            yield return new object[] { new ASCIIEncoding().GetBytes(randomString), 0, randomString.Length, randomString.Length };
            yield return new object[] { new ASCIIEncoding().GetBytes(randomString), randomIndex, randomCount, randomCount };
        }

        [Theory]
        [MemberData(nameof(GetCharCount_TestData))]
        public void GetCharCount(byte[] bytes, int index, int count, int expected)
        {
            if (index == 0 && count == bytes.Length)
            {
                Assert.Equal(expected, new ASCIIEncoding().GetCharCount(bytes));
            }
            Assert.Equal(expected, new ASCIIEncoding().GetCharCount(bytes, index, count));
        }

        [Fact]
        public void GetCharCount_Invalid()
        {
            // Bytes is null
            Assert.Throws<ArgumentNullException>("bytes", () => new ASCIIEncoding().GetCharCount(null));
            Assert.Throws<ArgumentNullException>("bytes", () => new ASCIIEncoding().GetCharCount(null, 0, 0));

            // Index or count < 0
            Assert.Throws<ArgumentOutOfRangeException>("index", () => new ASCIIEncoding().GetCharCount(new byte[4], -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => new ASCIIEncoding().GetCharCount(new byte[4], 0, -1));

            // Index + count > bytes.Length
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => new ASCIIEncoding().GetCharCount(new byte[4], 5, 0));
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => new ASCIIEncoding().GetCharCount(new byte[4], 4, 1));
            Assert.Throws<ArgumentOutOfRangeException>("bytes", () => new ASCIIEncoding().GetCharCount(new byte[4], 0, 5));
        }
    }
}
