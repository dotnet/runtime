// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UnicodeEncodingGetString
    {
        public static IEnumerable<object[]> GetString_TestData()
        {
            string randomString = EncodingHelpers.GetUnicodeString(10);
            byte[] randomBytes = new UnicodeEncoding().GetBytes(randomString);
            yield return new object[] { randomBytes, 0, randomBytes.Length, randomString };

            yield return new object[] { randomBytes, 0, 2, randomString.Substring(0, 1) };
            yield return new object[] { randomBytes, 0, 0, string.Empty };
            yield return new object[] { randomBytes, randomBytes.Length, 0, string.Empty };
        }

        [Theory]
        [MemberData(nameof(GetString_TestData))]
        public void GetString(byte[] bytes, int index, int count, string expected)
        {
            EncodingHelpers.GetString(new UnicodeEncoding(), bytes, index, count, expected);
        }

        [Fact]
        public void GetString_Invalid()
        {
            EncodingHelpers.GetString_Invalid(new UnicodeEncoding());
        }
    }
}
