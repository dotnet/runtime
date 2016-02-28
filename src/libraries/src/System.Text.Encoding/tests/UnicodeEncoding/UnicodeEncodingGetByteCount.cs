// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UnicodeEncodingGetByteCount
    {
        public static IEnumerable<object[]> GetByteCount_TestData()
        {
            string randomString = EncodingHelpers.GetUnicodeString(10);

            yield return new object[] { string.Empty, 0, 0, 0 };
            yield return new object[] { randomString, 0, randomString.Length, 20 };
            yield return new object[] { randomString, 0, 1, 2 };
        }

        [Theory]
        [MemberData(nameof(GetByteCount_TestData))]
        public void GetByteCount(string chars, int index, int count, int expected)
        {
            EncodingHelpers.GetByteCount(new UnicodeEncoding(), chars, index, count, expected);
        }

        [Fact]
        public void GetByteCount_Invalid()
        {
            EncodingHelpers.GetByteCount_Invalid(new UnicodeEncoding());
        }
    }
}
