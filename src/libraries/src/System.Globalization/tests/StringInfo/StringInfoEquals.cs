// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoEquals
    {
        private const int MinStringLength = 8;
        private const int MaxStringLength = 256;
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> Equals_TestData()
        {
            string randomString = s_randomDataGenerator.GetString(-55, false, MinStringLength, MaxStringLength);
            StringInfo randomStringInfo = new StringInfo(randomString);
            yield return new object[] { randomStringInfo, new StringInfo(randomString), true };
            yield return new object[] { randomStringInfo, randomStringInfo, true };
            yield return new object[] { new StringInfo(), new StringInfo(), true };
            yield return new object[] { new StringInfo("stringinfo1"), new StringInfo("stringinfo2"), false };
            yield return new object[] { new StringInfo("stringinfo1"), "stringinfo1", false };
            yield return new object[] { new StringInfo("stringinfo1"), 123, false };
            yield return new object[] { new StringInfo("stringinfo1"), null, false };
        }
        
        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals(StringInfo stringInfo, object value, bool expected)
        {
            Assert.Equal(expected, stringInfo.Equals(value));
            if (value is StringInfo)
            {
                Assert.Equal(expected, stringInfo.GetHashCode().Equals(value.GetHashCode()));
            }
        }
    }
}
