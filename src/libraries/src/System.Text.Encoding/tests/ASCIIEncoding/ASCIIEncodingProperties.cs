// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingProperties
    {
        public static IEnumerable<object[]> Encodings_TestData()
        {
            yield return new object[] { new ASCIIEncoding() };
            yield return new object[] { Encoding.ASCII };
            yield return new object[] { Encoding.GetEncoding("ascii") };
            yield return new object[] { Encoding.GetEncoding("us-ascii") };
        }

        [Theory]
        [MemberData(nameof(Encodings_TestData))]
        public void WebName(ASCIIEncoding encoding)
        {
            Assert.Equal("us-ascii", encoding.WebName);
        }
    }
}
