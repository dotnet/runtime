// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF7EncodingGetChars
    {
        public static IEnumerable<object[]> GetChars_TestData()
        {
            byte[] bytes = new byte[] { 85, 84, 70, 55, 32, 69, 110, 99, 111, 100, 105, 110, 103, 32, 69, 120, 97, 109, 112, 108, 101 };
            int charCount = new UTF7Encoding().GetCharCount(bytes, 2, 8);
            char[] chars = new char[charCount];
            yield return new object[] { bytes, 2, 8, chars, 0, new char[] { 'F', '7', ' ', 'E', 'n', 'c', 'o', 'd' } };

            yield return new object[] { new byte[0], 0, 0, new char[0], 0, new char[0] };
        }

        [Theory]
        [MemberData(nameof(GetChars_TestData))]
        public void GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, char[] expectedChars)
        {
            EncodingHelpers.GetChars(new UTF7Encoding(), bytes, byteIndex, byteCount, chars, charIndex, expectedChars);
        }
    }
}
