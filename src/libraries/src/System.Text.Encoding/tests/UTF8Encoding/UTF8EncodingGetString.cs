// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF8EncodingGetString
    {
        public static IEnumerable<object[]> GetString_TestData()
        {
            byte[] bytes = new byte[] { 85, 84, 70, 56, 32, 69, 110, 99, 111, 100, 105, 110, 103, 32, 69, 120, 97, 109, 112, 108, 101 };
            string expected = "UTF8 Encoding Example";
            yield return new object[] { bytes, 0, bytes.Length, expected };
        }

        [Theory]
        [MemberData(nameof(GetString_TestData))]
        public void GetString(byte[] bytes, int index, int count, string expected)
        {
            EncodingHelpers.GetString(new UTF8Encoding(), bytes, index, count, expected);
        }
    }
}
