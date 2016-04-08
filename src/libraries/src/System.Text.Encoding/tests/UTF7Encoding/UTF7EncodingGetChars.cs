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
            yield return new object[] { bytes, 2, 8, "F7 Encod".ToCharArray() };
            yield return new object[] { bytes, 0, bytes.Length, "UTF7 Encoding Example".ToCharArray() };

            yield return new object[] { new byte[0], 0, 0, new char[0] };
        }

        [Theory]
        [MemberData(nameof(GetChars_TestData))]
        public void GetChars(byte[] bytes, int index, int count, char[] expected)
        {
            EncodingHelpers.Decode(new UTF7Encoding(), bytes, index, count, expected);
        }
    }
}
