// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF8EncodingProperties
    {
        public static IEnumerable<object[]> Encodings_TestData()
        {
            yield return new object[] { Encoding.UTF8 };
            yield return new object[] { new UTF8Encoding() };
            yield return new object[] { Encoding.GetEncoding("utf-8") };
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void WebName(UTF8Encoding encoding)
        {
            Assert.Equal("utf-8", encoding.WebName);
        }
    }
}
