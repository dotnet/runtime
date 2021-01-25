// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public class ObsoleteAttributeTests
    {
        [Fact]
        public static void Ctor_Default()
        {
            var attribute = new ObsoleteAttribute();
            Assert.Null(attribute.Message);
            Assert.False(attribute.IsError);
            Assert.Null(attribute.DiagnosticId);
            Assert.Null(attribute.UrlFormat);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "BCL0006")]
        [InlineData("message", "")]
        public void Ctor_String_Id(string message, string id)
        {
            var attribute = new ObsoleteAttribute(message) { DiagnosticId = id };
            Assert.Equal(message, attribute.Message);
            Assert.False(attribute.IsError);
            Assert.Equal(id, attribute.DiagnosticId);
            Assert.Null(attribute.UrlFormat);
        }

        [Theory]
        [InlineData(null, true, "")]
        [InlineData("", false, null)]
        [InlineData("message", true, "https://aka.ms/obsolete/{0}")]
        public void Ctor_String_Bool_Url(string message, bool error, string url)
        {
            var attribute = new ObsoleteAttribute(message, error) { UrlFormat = url };
            Assert.Equal(message, attribute.Message);
            Assert.Equal(error, attribute.IsError);
            Assert.Null(attribute.DiagnosticId);
            Assert.Equal(url, attribute.UrlFormat);
        }
    }
}
