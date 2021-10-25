// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.Versioning.Tests
{
    public class RequiresPreviewFeaturesAttributeTests
    {
        [Fact]
        public void RequiresPreviewFeaturesAttributeTest()
        {
            new RequiresPreviewFeaturesAttribute();
        }

        [Fact]
        public static void Ctor_Default()
        {
            var attribute = new RequiresPreviewFeaturesAttribute();
            Assert.Null(attribute.Message);
            Assert.Null(attribute.Url);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("message")]
        public void Ctor_String_Message(string message)
        {
            var attribute = new RequiresPreviewFeaturesAttribute(message);
            Assert.Same(message, attribute.Message);
            Assert.Null(attribute.Url);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", null)]
        [InlineData("message", "https://aka.ms/preview-features/")]
        public void Ctor_String_Url(string message, string url)
        {
            var attribute = new RequiresPreviewFeaturesAttribute(message) { Url = url };
            Assert.Same(message, attribute.Message);
            Assert.Same(url, attribute.Url);
        }
    }
}
