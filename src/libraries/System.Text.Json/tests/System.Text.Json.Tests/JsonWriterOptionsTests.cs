// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                SkipValidation = false,
                MaxDepth = 0,
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
                SkipValidation = false,
                MaxDepth = 0,
            };
            Assert.Equal(expectedOption, options);
        }

        [Theory]
        [InlineData(true, true, 0)]
        [InlineData(true, false, 1)]
        [InlineData(false, true, 1024)]
        [InlineData(false, false, 1024 * 1024)]
        public static void JsonWriterOptions(bool indented, bool skipValidation, int maxDepth)
        {
            var options = new JsonWriterOptions();
            options.Indented = indented;
            options.SkipValidation = skipValidation;
            options.MaxDepth = maxDepth;

            var expectedOption = new JsonWriterOptions
            {
                Indented = indented,
                SkipValidation = skipValidation,
                MaxDepth = maxDepth,
            };
            Assert.Equal(expectedOption, options);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public static void JsonWriterOptions_MaxDepth_InvalidParameters(int maxDepth)
        {
            var options = new JsonWriterOptions();
            Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxDepth = maxDepth);
        }
    }
}
