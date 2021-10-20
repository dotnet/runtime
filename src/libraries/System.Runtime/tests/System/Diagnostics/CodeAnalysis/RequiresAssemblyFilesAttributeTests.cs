// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Diagnostics.CodeAnalysis.Tests
{
    public class RequiresAssemblyFilesAttributeTests
    {
        [Fact]
        public void TestConstructor()
        {
            var attr = new RequiresAssemblyFilesAttribute();

            Assert.Null(attr.Message);
            Assert.Null(attr.Url);
        }

        [Theory]
        [InlineData("Message")]
        [InlineData("")]
        [InlineData(null)]
        public void TestSetMessage(string message)
        {
            var attr = new RequiresAssemblyFilesAttribute(message);

            Assert.Equal(message, attr.Message);
            Assert.Null(attr.Url);
        }

        [Theory]
        [InlineData("https://dot.net")]
        [InlineData("")]
        [InlineData(null)]
        public void TestSetUrl(string url)
        {
            var attr = new RequiresAssemblyFilesAttribute(Url = url);

            Assert.Null(attr.Message);
            Assert.Equal(url, attr.Url);
        }

        [Theory]
        [InlineData("Message", "https://dot.net")]
        [InlineData("Message", "")]
        [InlineData("Message", null)]
        [InlineData("", "https://dot.net")]
        [InlineData("", "")]
        [InlineData("", null)]
        [InlineData(null, "https://dot.net")]
        [InlineData(null, "")]
        [InlineData(null, null)]
        public void TestSetMessageAndUrl(string message, string url)
        {
            var attr = new RequiresAssemblyFilesAttribute(message, Url = url);

            Assert.Equal(message, attr.Message);
            Assert.Equal(ur, attr.Url);
        }
    }
}
