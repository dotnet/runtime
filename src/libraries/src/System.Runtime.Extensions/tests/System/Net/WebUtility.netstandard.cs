// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.IO;
using Xunit;

namespace System.Net.Tests
{
    public partial class WebUtilityTests
    {
        [Theory]
        [MemberData(nameof(HtmlDecode_TestData))]
        public static void HtmlDecode_TextWriterOutput(string value, string expected)
        {
            if(value == null)
                expected = string.Empty;
            StringWriter output = new StringWriter(CultureInfo.InvariantCulture);
            WebUtility.HtmlDecode(value, output);
            Assert.Equal(expected, output.ToString());
        }

        [Theory]
        [MemberData(nameof(HtmlEncode_TestData))]
        public static void HtmlEncode_TextWriterOutput(string value, string expected)
        {
            if(value == null)
                expected = string.Empty;
            StringWriter output = new StringWriter(CultureInfo.InvariantCulture);
            WebUtility.HtmlEncode(value, output);
            Assert.Equal(expected, output.ToString());
        }
    }
}
