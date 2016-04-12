// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF8EncodingGetByteCount
    {
        public static IEnumerable<object[]> GetByteCount_TestData()
        {
            string chars = "UTF8 Encoding Example";
            yield return new object[] { chars, 0, chars.Length, chars.Length };
            yield return new object[] { "\u0023\u0025\u03a0\u03a3", 1, 2, 3 };
            yield return new object[] { "", 0, 0, 0 };
        }

        [Theory]
        [MemberData(nameof(GetByteCount_TestData))]
        public void GetByteCount(string chars, int index, int count, int expected)
        {
            EncodingHelpers.GetByteCount(new UTF8Encoding(), chars, index, count, expected);
        }
    }
}
