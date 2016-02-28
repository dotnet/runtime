// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UnicodeEncodingGetCharCount
    {
        public static IEnumerable<object[]> GetCharCount_TestData()
        {
            string randomString = EncodingHelpers.GetUnicodeString(10);
            byte[] randomBytes = new UnicodeEncoding().GetBytes(randomString);
            yield return new object[] { randomBytes, 0, randomBytes.Length, randomBytes.Length / 2 };
            yield return new object[] { randomBytes, 0, 0, 0 };
            yield return new object[] { randomBytes, randomBytes.Length, 0, 0 };
            yield return new object[] { randomBytes, 0, 2, 1 };
        }

        [Theory]
        [MemberData(nameof(GetCharCount_TestData))]
        public void GetCharCount(byte[] bytes, int index, int count, int expected)
        {
            EncodingHelpers.GetCharCount(new UnicodeEncoding(), bytes, index, count, expected);
        }
        
        [Fact]
        public void GetCharCount_Invalid()
        {
            EncodingHelpers.GetCharCount_Invalid(new UnicodeEncoding());
        }
    }
}
