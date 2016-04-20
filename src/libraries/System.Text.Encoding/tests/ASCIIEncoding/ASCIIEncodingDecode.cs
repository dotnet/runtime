// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingDecode
    {
        public static IEnumerable<object[]> Decode_TestData()
        {
            yield return new object[] { new byte[0], 0, 0 };
            yield return new object[] { new byte[10], 5, 0 };
            yield return new object[] { new byte[10], 5, 5 };

            // All ASCII chars
            for (int i = 0; i <= byte.MaxValue; i++)
            {
                byte b = (byte)i;
                yield return new object[] { new byte[] { b }, 0, 1 };
                yield return new object[] { new byte[] { 96, b, 97 }, 1, 1 };
                yield return new object[] { new byte[] { 97, b, 97 }, 0, 3 };
            }
        }

        [Theory]
        [MemberData(nameof(Decode_TestData))]
        public void Decode(byte[] bytes, int index, int count)
        {
            char[] expectedChars = new char[count];
            for (int i = 0; i < count; i++)
            {
                byte b = bytes[i + index];
                if (b <= 0x7F)
                {
                    expectedChars[i] = (char)b;
                }
                else
                {
                    expectedChars[i] = '?';
                }
            }
            EncodingHelpers.Decode(new ASCIIEncoding(), bytes, index, count, new string(expectedChars));
        }
    }
}
