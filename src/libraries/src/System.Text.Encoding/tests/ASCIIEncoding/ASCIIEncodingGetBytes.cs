// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingGetBytes
    {
        public static IEnumerable<object[]> GetBytes_TestData()
        {
            string testString = "Hello World123#?!";
            yield return new object[] { testString, 0, testString.Length };
            yield return new object[] { testString, 4, 5 };

            yield return new object[] { string.Empty, 0, 0 };

            string unicodeString = "a\u1234\u2345b";
            yield return new object[] { unicodeString, 0, unicodeString.Length };
        }
        
        [Theory]
        [MemberData(nameof(GetBytes_TestData))]
        public void GetBytes(string source, int index, int count)
        {
            byte[] expectedBytes = new byte[count];
            for (int i = 0; i < expectedBytes.Length; i++)
            {
                if (source[i] <= 0x7f)
                {
                    expectedBytes[i] = (byte)source[i + index];
                }
                else
                {
                    // Verify the fallback character for non-ASCII chars
                    expectedBytes[i] = (byte)'?';
                }
            }
            EncodingHelpers.Encode(new ASCIIEncoding(), source, index, count, expectedBytes);
        }
    }
}
