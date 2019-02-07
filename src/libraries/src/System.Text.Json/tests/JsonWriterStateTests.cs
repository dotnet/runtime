// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class JsonWriterStateTests
    {
        [Fact]
        public static void DefaultJsonWriterState()
        {
            JsonWriterState state = default;
            Assert.Equal(0, state.BytesCommitted);
            Assert.Equal(0, state.BytesWritten);

            var expectedOption = new JsonWriterOptions
            {
                Indented = false,
                SkipValidation = false
            };
            Assert.Equal(expectedOption, state.Options);
        }

        [Fact]
        public static void JsonWriterStateDefaultCtor()
        {
            var state = new JsonWriterState();
            Assert.Equal(0, state.BytesCommitted);
            Assert.Equal(0, state.BytesWritten);

            var expectedOption = new JsonWriterOptions
            {
                Indented = false,
                SkipValidation = false
            };
            Assert.Equal(expectedOption, state.Options);
        }

        [Fact]
        public static void JsonWriterStateCtor()
        {
            var state = new JsonWriterState(options: default);
            Assert.Equal(0, state.BytesCommitted);
            Assert.Equal(0, state.BytesWritten);

            var expectedOption = new JsonWriterOptions
            {
                Indented = false,
                SkipValidation = false
            };
            Assert.Equal(expectedOption, state.Options);
        }
    }
}
