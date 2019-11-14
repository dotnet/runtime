// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class JsonWriterOptionsTests
    {
        [Fact]
        public static void JsonWriterOptionsDefaultCtor()
        {
            JsonWriterOptions options = default;

            var expectedOption = new JsonWriterOptions
            {
                Indented = false,
                SkipValidation = false
            };
            Assert.Equal(expectedOption, options);
        }

        [Fact]
        public static void JsonWriterOptionsCtor()
        {
            var options = new JsonWriterOptions();

            var expectedOption = new JsonWriterOptions
            {
                Indented = false,
                SkipValidation = false
            };
            Assert.Equal(expectedOption, options);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public static void JsonWriterOptions(bool indented, bool skipValidation)
        {
            var options = new JsonWriterOptions();
            options.Indented = indented;
            options.SkipValidation = skipValidation;

            var expectedOption = new JsonWriterOptions
            {
                Indented = indented,
                SkipValidation = skipValidation
            };
            Assert.Equal(expectedOption, options);
        }
    }
}
