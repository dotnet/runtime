// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingGetMaxCharCount
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> GetMaxCharCount_TestData()
        {
            int randomByteCount = s_randomDataGenerator.GetInt32(-55);
            yield return new object[] { 0, 0 };
            yield return new object[] { randomByteCount, randomByteCount };
        }

        [Theory]
        [MemberData(nameof(GetMaxCharCount_TestData))]
        public void GetMaxCharCount(int byteCount, int expected)
        {
            Assert.Equal(expected, new ASCIIEncoding().GetMaxCharCount(byteCount));
        }

        [Fact]
        public void GetMaxCharCount_Invalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>("byteCount", () => new ASCIIEncoding().GetMaxCharCount(-1));
        }
    }
}
