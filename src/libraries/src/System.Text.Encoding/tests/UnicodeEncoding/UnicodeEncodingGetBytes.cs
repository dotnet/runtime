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
            string testString = "Hello World";
            byte[] testStringBytes = new byte[] { 72, 0, 101, 0, 108, 0, 108, 0, 111, 0, 32, 0, 87, 0, 111, 0, 114, 0, 108, 0, 100, 0 };

            yield return new object[] { testString, 0, testString.Length, new byte[30], 5, testStringBytes };
            yield return new object[] { testString, 0, 1, new byte[30], 0, new byte[] { 72, 0 } };
            yield return new object[] { testString, 0, 0, new byte[20], 20, new byte[0] };
            yield return new object[] { testString, testString.Length, 0, new byte[20], 20, new byte[0] };

            byte[] allBytes = new byte[256];
            for (int i = 0; i <= byte.MaxValue; i++)
            {
                allBytes[i] = (byte)i;
            }
            yield return new object[] { string.Empty, 0, 0, new byte[0], 0, new byte[0] };
            yield return new object[] { string.Empty, 0, 0, allBytes, 0, new byte[0] };
        }

        [Theory]
        [MemberData(nameof(GetBytes_TestData))]
        public void GetBytes(string source, int index, int count, byte[] bytes, int byteIndex, byte[] expectedBytes)
        {
            EncodingHelpers.GetBytes(new UnicodeEncoding(), source, index, count, bytes, byteIndex, expectedBytes);
        }
    }
}
