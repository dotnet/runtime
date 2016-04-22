// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UnicodeEncodingProperties
    {
        public static IEnumerable<object[]> Encodings_TestData()
        {
            yield return new object[] { Encoding.Unicode, "utf-16" };
            yield return new object[] { new UnicodeEncoding(), "utf-16" };
            yield return new object[] { Encoding.GetEncoding("utf-16"), "utf-16" };
            yield return new object[] { Encoding.GetEncoding("utf-16LE"), "utf-16" };
            yield return new object[] { Encoding.GetEncoding("utf-16BE"), "utf-16BE" };
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void WebName(UnicodeEncoding encoding, string expected)
        {
            Assert.Equal(expected, encoding.WebName);
        }
    }
}
