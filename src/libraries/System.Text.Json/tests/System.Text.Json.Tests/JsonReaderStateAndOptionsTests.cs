// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

            var expectedOption = new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 0
            };
            Assert.Equal(expectedOption, state.Options);

            state = new JsonReaderState(new JsonReaderOptions { CommentHandling = JsonCommentHandling.Disallow, MaxDepth = 0 });
            Assert.Equal(expectedOption, state.Options);

            expectedOption = new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 32
            };
            state = new JsonReaderState(new JsonReaderOptions { MaxDepth = 32 });
            Assert.Equal(32, state.Options.MaxDepth);
            Assert.Equal(expectedOption, state.Options);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        [InlineData(byte.MaxValue)]
        [InlineData(byte.MaxValue + 4)] // Other values, like byte.MaxValue + 1 overflows to 0 (i.e. JsonCommentHandling.Disallow), which is valid.
        public static void TestCommentHandlingInvalid(int enumValue)
        {
            Assert.Throws<ArgumentOutOfRangeException>("value", () => new JsonReaderOptions { CommentHandling = (JsonCommentHandling)enumValue });
            Assert.Throws<ArgumentOutOfRangeException>("value", () => new JsonReaderState(new JsonReaderOptions { CommentHandling = (JsonCommentHandling)enumValue }));
        }

        [Theory]
        [InlineData(-1)]
        public static void TestDepthInvalid(int depth)
        {
            Assert.Throws<ArgumentOutOfRangeException>("value", () => new JsonReaderOptions { MaxDepth = depth });
            Assert.Throws<ArgumentOutOfRangeException>("value", () => new JsonReaderState(new JsonReaderOptions { MaxDepth = depth }));
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
            byte[] dataUtf8 = "{}"u8.ToArray();

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
            byte[] dataUtf8 = "{}"u8.ToArray();

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
