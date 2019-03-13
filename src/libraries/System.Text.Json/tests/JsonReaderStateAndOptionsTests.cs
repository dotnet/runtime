// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class JsonReaderStateAndOptionsTests
    {
        [Fact]
        public static void DefaultJsonReaderState()
        {
            JsonReaderState state = default;
            Assert.Equal(0, state.BytesConsumed);

            var expectedOption = new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 0
            };
            Assert.Equal(expectedOption, state.Options);
        }

        [Fact]
        public static void JsonReaderStateDefaultCtor()
        {
            var state = new JsonReaderState();
            Assert.Equal(0, state.BytesConsumed);

            var expectedOption = new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 0
            };
            Assert.Equal(expectedOption, state.Options);
        }

        [Fact]
        public static void JsonReaderStateCtor()
        {
            var state = new JsonReaderState(default);
            Assert.Equal(0, state.BytesConsumed);

            var expectedOption = new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 0
            };
            Assert.Equal(expectedOption, state.Options);

            state = new JsonReaderState(new JsonReaderOptions { CommentHandling = JsonCommentHandling.Disallow, MaxDepth = 0 });
            Assert.Equal(0, state.BytesConsumed);
            Assert.Equal(expectedOption, state.Options);

            expectedOption = new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 32
            };
            state = new JsonReaderState(new JsonReaderOptions { MaxDepth = 32 });
            Assert.Equal(0, state.BytesConsumed);
            Assert.Equal(32, state.Options.MaxDepth);
            Assert.Equal(expectedOption, state.Options);
        }

        [Theory]
        [InlineData(-1)]
        public static void TestDepthInvalid(int depth)
        {
            try
            {
                var options = new JsonReaderOptions { MaxDepth = depth };
                Assert.True(false, "Expected ArgumentException was not thrown. Max depth must be set to greater than or equal to 0.");
            }
            catch (ArgumentException)
            { }

            try
            {
                var state = new JsonReaderState(new JsonReaderOptions { MaxDepth = depth });
                Assert.True(false, "Expected ArgumentException was not thrown. Max depth must be set to greater than or equal to 0.");
            }
            catch (ArgumentException)
            { }
        }

        [Fact]
        public static void DefaultJsonReaderOptions()
        {
            JsonReaderOptions options = default;

            var expectedOption = new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 0
            };
            Assert.Equal(expectedOption, options);

            options = new JsonReaderOptions { CommentHandling = JsonCommentHandling.Disallow };
            Assert.Equal(expectedOption, options);

            options = new JsonReaderOptions { MaxDepth = 0 };
            Assert.Equal(expectedOption, options);

            options = new JsonReaderOptions { CommentHandling = JsonCommentHandling.Disallow, MaxDepth = 0 };
            Assert.Equal(expectedOption, options);
        }

        [Fact]
        public static void ZeroMaxDepthDefaultsTo64()
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes("{}");

            ReadOnlyMemory<byte> dataMemory = dataUtf8;
            var firstSegment = new BufferSegment<byte>(dataMemory.Slice(0, 1));
            ReadOnlyMemory<byte> secondMem = dataMemory.Slice(1);
            BufferSegment<byte> secondSegment = firstSegment.Append(secondMem);
            var sequence = new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, secondMem.Length);

            var state = new JsonReaderState();
            var reader = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);
            Assert.Equal(64, reader.CurrentState.Options.MaxDepth);

            JsonReaderOptions options = new JsonReaderOptions { CommentHandling = JsonCommentHandling.Disallow, MaxDepth = 0 };
            state = new JsonReaderState(options);
            reader = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);
            Assert.Equal(0, options.MaxDepth);
            Assert.Equal(64, reader.CurrentState.Options.MaxDepth);

            state = new JsonReaderState();
            reader = new Utf8JsonReader(sequence, isFinalBlock: true, state);
            Assert.Equal(64, reader.CurrentState.Options.MaxDepth);

            state = new JsonReaderState(options);
            reader = new Utf8JsonReader(sequence, isFinalBlock: true, state);
            Assert.Equal(0, options.MaxDepth);
            Assert.Equal(64, reader.CurrentState.Options.MaxDepth);
        }

        [Fact]
        public static void MaxDepthIsHonored()
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes("{}");

            ReadOnlyMemory<byte> dataMemory = dataUtf8;
            var firstSegment = new BufferSegment<byte>(dataMemory.Slice(0, 1));
            ReadOnlyMemory<byte> secondMem = dataMemory.Slice(1);
            BufferSegment<byte> secondSegment = firstSegment.Append(secondMem);
            var sequence = new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, secondMem.Length);

            JsonReaderOptions options = new JsonReaderOptions { CommentHandling = JsonCommentHandling.Disallow, MaxDepth = 64 };
            var state = new JsonReaderState(options);
            var reader = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);
            Assert.Equal(64, options.MaxDepth);
            Assert.Equal(64, reader.CurrentState.Options.MaxDepth);

            state = new JsonReaderState(options);
            reader = new Utf8JsonReader(sequence, isFinalBlock: true, state);
            Assert.Equal(64, options.MaxDepth);
            Assert.Equal(64, reader.CurrentState.Options.MaxDepth);

            options = new JsonReaderOptions { CommentHandling = JsonCommentHandling.Disallow, MaxDepth = 32 };
            state = new JsonReaderState(options);
            reader = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);
            Assert.Equal(32, options.MaxDepth);
            Assert.Equal(32, reader.CurrentState.Options.MaxDepth);

            state = new JsonReaderState(options);
            reader = new Utf8JsonReader(sequence, isFinalBlock: true, state);
            Assert.Equal(32, options.MaxDepth);
            Assert.Equal(32, reader.CurrentState.Options.MaxDepth);
        }
    }
}
