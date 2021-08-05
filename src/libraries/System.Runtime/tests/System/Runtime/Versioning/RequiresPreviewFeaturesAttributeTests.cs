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
            var attribute = new RequiresPreviewFeaturesAttribute();
            Assert.NotNull(attribute);
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
            var attribute = new RequiresPreviewFeaturesAttribute(message) {};
            Assert.Equal(message, attribute.Message);
            Assert.Null(attribute.Url);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", null)]
        [InlineData("message", "https://aka.ms/obsolete/{0}")]
        public void Ctor_String_Url(string message, string url)
        {
            var attribute = new RequiresPreviewFeaturesAttribute(message) { Url = url };
            Assert.Equal(message, attribute.Message);
            Assert.Equal(url, attribute.Url);
        }
    }
}
