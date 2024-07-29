// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Diagnostics.CodeAnalysis.Tests
{
    public class ExperimentalAttributeTests
    {
        [Theory]
        // Note: Once the compiler support is implemented these should fail
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        // Note end
        [InlineData("diagnosticId")]
        public void TestConstructor(string expected)
        {
            var attr = new ExperimentalAttribute(expected);

            Assert.Equal(expected, attr.DiagnosticId);
            Assert.Null(attr.UrlFormat);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("https://contoso.com/obsoletion-warnings/{0}")]
        public void TestSetUrlFormat(string urlFormat)
        {
            var attr = new ExperimentalAttribute("diagnosticId")
            {
                UrlFormat = urlFormat
            };

            Assert.Equal("diagnosticId", attr.DiagnosticId);
            Assert.Equal(urlFormat, attr.UrlFormat);
        }
    }
}
