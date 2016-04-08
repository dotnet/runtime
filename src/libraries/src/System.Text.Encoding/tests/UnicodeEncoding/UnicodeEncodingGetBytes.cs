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
            string testString = "Hello World\u0500";
            byte[] testStringBytes = new byte[] { 72, 0, 101, 0, 108, 0, 108, 0, 111, 0, 32, 0, 87, 0, 111, 0, 114, 0, 108, 0, 100, 0, 0, 5 };

            yield return new object[] { testString, 0, testString.Length, testStringBytes };
            yield return new object[] { testString, 0, 1, new byte[] { 72, 0 } };
            yield return new object[] { testString, 0, 0, new byte[0] };
            
            yield return new object[] { string.Empty, 0, 0, new byte[0] };
        }

        [Theory]
        [MemberData(nameof(GetBytes_TestData))]
        public void GetBytes(string source, int index, int count, byte[] expected)
        {
            EncodingHelpers.Encode(new UnicodeEncoding(), source, index, count, expected);
        }
    }
}
