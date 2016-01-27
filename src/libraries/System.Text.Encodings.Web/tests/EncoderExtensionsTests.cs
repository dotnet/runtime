// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Framework.WebEncoders;
using System;
using System.IO;
using Xunit;
using System.Text.Unicode;

namespace System.Text.Encodings.Web
{
    public class EncoderExtensionsTests
    {
        [Fact]
        public void HtmlEncode_ParameterChecks()
        {
            Assert.Throws<ArgumentNullException>(() => EncoderExtensions.HtmlEncode(null, "Hello!", new StringWriter()));
        }

        [Fact]
        public void HtmlEncode_PositiveTestCase()
        {
            // Arrange
            HtmlEncoder encoder = HtmlEncoder.Create(UnicodeRanges.All);
            StringWriter writer = new StringWriter();

            // Act
            encoder.Encode(writer, "Hello+there!");

            // Assert
            Assert.Equal("Hello&#x2B;there!", writer.ToString());
        }

        [Fact]
        public void JavaScriptStringEncode_ParameterChecks()
        {
            Assert.Throws<ArgumentNullException>(() => EncoderExtensions.JavaScriptStringEncode(null, "Hello!", new StringWriter()));
        }

        [Fact]
        public void JavaScriptStringEncode_PositiveTestCase()
        {
            // Arrange
            IJavaScriptStringEncoder encoder = new JavaScriptStringEncoder(UnicodeRanges.All);
            StringWriter writer = new StringWriter();

            // Act
            encoder.JavaScriptStringEncode("Hello+there!", writer);

            // Assert
            Assert.Equal(@"Hello\u002Bthere!", writer.ToString());
        }

        [Fact]
        public void UrlEncode_ParameterChecks()
        {
            Assert.Throws<ArgumentNullException>(() => EncoderExtensions.UrlEncode(null, "Hello!", new StringWriter()));
        }

        [Fact]
        public void UrlEncode_PositiveTestCase()
        {
            // Arrange
            UrlEncoder encoder = UrlEncoder.Create(UnicodeRanges.All);
            StringWriter writer = new StringWriter();

            // Act
            encoder.Encode(writer, "Hello+there!");

            // Assert
            Assert.Equal("Hello%2Bthere!", writer.ToString());
        }
    }
}
