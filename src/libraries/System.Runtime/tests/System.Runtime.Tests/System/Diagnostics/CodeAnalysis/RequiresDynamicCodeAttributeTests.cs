// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Diagnostics.CodeAnalysis.Tests
{
    public class RequiresDynamicCodeAttributeTests
    {
        [Fact]
        public void TestConstructor()
        {
            var attr = new RequiresDynamicCodeAttribute("User Message");

            Assert.Equal("User Message", attr.Message);
            Assert.Null(attr.Url);
        }

        [Theory]
        [InlineData("https://dot.net")]
        [InlineData("")]
        [InlineData(null)]
        public void TestSetUrl(string url)
        {
            var attr = new RequiresDynamicCodeAttribute("User Message")
            {
                Url = url
            };

            Assert.Equal("User Message", attr.Message);
            Assert.Equal(url, attr.Url);
        }
    }
}
