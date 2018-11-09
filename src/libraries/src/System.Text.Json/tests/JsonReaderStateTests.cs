// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class JsonReaderStateTests
    {
        [Fact]
        public static void DefaultJsonReaderState()
        {
            JsonReaderState state = default;
            Assert.Equal(0, state.BytesConsumed);
            Assert.Equal(0, state.MaxDepth);

            var expectedOption = new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Disallow
            };
            Assert.Equal(expectedOption, state.Options);
        }

        [Fact]
        public static void JsonReaderStateDefaultCtor()
        {
            var state = new JsonReaderState();
            Assert.Equal(0, state.BytesConsumed);
            Assert.Equal(0, state.MaxDepth);

            var expectedOption = new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Disallow
            };
            Assert.Equal(expectedOption, state.Options);
        }

        [Fact]
        public static void JsonReaderStateCtor()
        {
            var state = new JsonReaderState(options: default);
            Assert.Equal(0, state.BytesConsumed);
            Assert.Equal(64, state.MaxDepth);

            var expectedOption = new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Disallow
            };
            Assert.Equal(expectedOption, state.Options);

            state = new JsonReaderState(maxDepth: 32);
            Assert.Equal(0, state.BytesConsumed);
            Assert.Equal(32, state.MaxDepth);
            Assert.Equal(expectedOption, state.Options);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public static void TestDepthInvalid(int depth)
        {
            try
            {
                var state = new JsonReaderState(maxDepth: depth);
                Assert.True(false, "Expected ArgumentException was not thrown. Max depth must be set to greater than 0.");
            }
            catch (ArgumentException)
            { }
        }
    }
}
