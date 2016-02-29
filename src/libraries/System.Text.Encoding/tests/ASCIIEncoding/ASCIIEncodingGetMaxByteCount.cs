// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingGetMaxByteCount
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> GetMaxByteCount_TestData()
        {
            int randomCharCount = s_randomDataGenerator.GetInt32(-55);
            yield return new object[] { 0, 1 };
            yield return new object[] { randomCharCount, randomCharCount + 1 };
        }

        [Theory]
        [MemberData(nameof(GetMaxByteCount_TestData))]
        public void GetMaxByteCount(int charCount, int expected)
        {
            Assert.Equal(expected, new ASCIIEncoding().GetMaxByteCount(charCount));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        public void GetMaxByteCount_Invalid(int charCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>("charCount", () => new ASCIIEncoding().GetMaxByteCount(charCount));
        }
    }
}
