// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF8EncodingGetBytes
    {
        public static IEnumerable<object[]> GetBytes_TestData()
        {
            yield return new object[] { "\u0023\u0025\u03a0\u03a3", 1, 2, new byte[] { 37, 206, 160 } };
            
            string asciiChars = "UTF8 Encoding Example";
            yield return new object[] { asciiChars, 1, 2, new byte[] { 84, 70} };
            yield return new object[] { asciiChars, 0, asciiChars.Length, new byte[] { 85, 84, 70, 56, 32, 69, 110, 99, 111, 100, 105, 110, 103, 32, 69, 120, 97, 109, 112, 108, 101 } };

            yield return new object[] { string.Empty, 0, 0, new byte[0] };
        }

        [Theory]
        [MemberData(nameof(GetBytes_TestData))]
        public void GetBytes(string chars, int index, int count, byte[] expected)
        {
            EncodingHelpers.Encode(new UTF8Encoding(), chars, index, count, expected);
        }
    }
}
