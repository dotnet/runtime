// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF7EncodingGetMaxCharCount
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> GetMaxCharCount_TestData()
        {
            int randomByteCount = s_randomDataGenerator.GetInt32(-55);
            yield return new object[] { randomByteCount, randomByteCount };
            yield return new object[] { 0, 1 };
            yield return new object[] { int.MaxValue, int.MaxValue };
        }

        [Theory]
        [MemberData(nameof(GetMaxCharCount_TestData))]
        public void GetMaxCharCount(int byteCount, int expected)
        {
            Assert.Equal(expected, new UTF7Encoding().GetMaxCharCount(byteCount));
        }
    }
}
