// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingGetBytes
    {
        private const int MinStringLength = 2;
        private const int MaxStringLength = 260;

        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> GetBytes_TestData()
        {
            yield return new object[] { string.Empty, 0, 0, new byte[0], 0 };

            string testString = "Hello World";
            yield return new object[] { testString, 0, testString.Length, new byte[testString.Length], 0 };
            yield return new object[] { testString, 0, testString.Length, new byte[testString.Length + 1], 1 };
            yield return new object[] { testString, 4, 5, new byte[5], 0 };

            string unicodeString = "a\u1234\u2345b";
            yield return new object[] { unicodeString, 0, unicodeString.Length, new byte[unicodeString.Length], 0 };
        }
        
        [Theory]
        [MemberData(nameof(GetBytes_TestData))]
        public void GetBytes(string source, int index, int count, byte[] bytes, int byteIndex)
        {
            byte[] expectedBytes = new byte[count];
            for (int i = 0; i < expectedBytes.Length; i++)
            {
                if (source[i] >= 0x0 && source[i] <= 0x7f)
                {
                    expectedBytes[i] = (byte)source[i + index];
                }
                else
                {
                    // Verify the fallback character for non-ASCII chars
                    expectedBytes[i] = (byte)'?';
                }
            }
            EncodingHelpers.GetBytes(new ASCIIEncoding(), source, index, count, bytes, byteIndex, expectedBytes);
        }
    }
}
