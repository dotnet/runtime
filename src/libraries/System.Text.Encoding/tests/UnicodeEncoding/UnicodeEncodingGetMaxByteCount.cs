// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UnicodeEncodingGetMaxByteCount
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> GetMaxByteCount_TestData()
        {
            yield return new object[] { 0, 2 };
            yield return new object[] { 1, 4 };

            int randomCharCount = (s_randomDataGenerator.GetInt32(-55) % int.MaxValue + 1) / 2;
            yield return new object[] { randomCharCount, (randomCharCount + 1) * 2 };
        }

        [Theory]
        [MemberData(nameof(GetMaxByteCount_TestData))]
        public void GetMaxByteCount(int charCount, int expected)
        {
            Assert.Equal(expected, new UnicodeEncoding().GetMaxByteCount(charCount));
        }

        [Fact]
        public void GetMaxByteCount_Invalid()
        {
            EncodingHelpers.GetMaxByteCount_Invalid(new UnicodeEncoding());
        }
    }
}
