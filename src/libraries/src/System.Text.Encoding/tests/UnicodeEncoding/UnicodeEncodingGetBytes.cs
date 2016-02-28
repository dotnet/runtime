// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UnicodeEncodingGetBytes
    {
        public static IEnumerable<object[]> GetBytes_TestData()
        {
            string randomString = EncodingHelpers.GetUnicodeString(10);
            yield return new object[] { randomString, 0, randomString.Length, new byte[30], 5, 20 };
            yield return new object[] { randomString, 0, 1, new byte[30], 0, 2 };
            yield return new object[] { randomString, 0, 0, new byte[20], 20, 0 };
            yield return new object[] { randomString, randomString.Length, 0, new byte[20], 20, 0 };

            yield return new object[] { string.Empty, 0, 0, new byte[0], 0, 0 };
        }

        [Theory]
        [MemberData(nameof(GetBytes_TestData))]
        public void GetBytes(string source, int index, int count, byte[] bytes, int byteIndex, int expected)
        {
            EncodingHelpers.GetBytes(new UnicodeEncoding(), source, index, count, bytes, byteIndex, expected);
        }
        
        [Fact]
        public void GetBytes_Invalid()
        {
            EncodingHelpers.GetBytes_Invalid(new UnicodeEncoding());
        }
    }
}
