// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using Xunit;

namespace System.Tests;

public class ConsoleStreamReaderTests
{
    [Theory]
    [InlineData("", new string[0])]
    [InlineData("one", new[] { "one" })]
    [InlineData("one\r", new[] { "one" })]
    [InlineData("one\n", new[] { "one" })]
    [InlineData("one\r\n", new[] { "one" })]
    [InlineData("one\rtwo\r", new[] { "one", "two" })]
    [InlineData("one\ntwo\n", new[] { "one", "two" })]
    [InlineData("one\r\ntwo\r\n", new[] { "one", "two" })]
    [InlineData("one\r\ntwo\rthree\nfour", new[] { "one", "two", "three", "four" })]
    [InlineData("\r\r", new[] { "", "" })]
    [InlineData("\n\n", new[] { "", "" })]
    [InlineData("\r\n\r\n", new[] { "", "" })]
    [InlineData("one\r\n\r\ntwo\r", new[] { "one", "", "two" })]
    public void ReadLine_EndsALineOnEveryTerminator(string input, string[] expected)
    {
        using ConsoleStreamReader reader = CreateReader(input);

        foreach (string line in expected)
        {
            Assert.Equal(line, reader.ReadLine());
        }

        Assert.Null(reader.ReadLine());
    }

    [Fact]
    public void ReadLine_LineEndingInCarriageReturn_DoesNotReadPastIt()
    {
        // A console that ends a line with a lone '\r' does not answer another read until the user
        // enters another line, so the '\r' has to end the line without one.
        using ConsoleStreamReader reader = CreateReader(new ChunkStream(throwWhenExhausted: true, "one\r"));

        Assert.Equal("one", reader.ReadLine());
    }

    [Fact]
    public void ReadLine_LineFeedDeliveredInALaterRead_DoesNotBecomeAnEmptyLine()
    {
        using ConsoleStreamReader reader = CreateReader(new ChunkStream(throwWhenExhausted: false, "one\r", "\ntwo\r\n"));

        Assert.Equal("one", reader.ReadLine());
        Assert.Equal("two", reader.ReadLine());
        Assert.Null(reader.ReadLine());
    }

    [Fact]
    public void ReadLine_LineLongerThanTheBuffers_IsReturnedWhole()
    {
        string first = new string('a', 3000);
        string second = new string('b', 3000);
        using ConsoleStreamReader reader = CreateReader($"{first}\r{second}\r\nend");

        Assert.Equal(first, reader.ReadLine());
        Assert.Equal(second, reader.ReadLine());
        Assert.Equal("end", reader.ReadLine());
        Assert.Null(reader.ReadLine());
    }

    [Fact]
    public void ReadLine_CharactersSplitAcrossReads_AreReturnedWhole()
    {
        // "a\U0001F600\r" + "\né\r", with the four UTF-8 bytes of the surrogate pair split two and two,
        // and the two bytes of 'é' split one and one.
        byte[] input = Encoding.UTF8.GetBytes("a\U0001F600\r\né\r");
        using ConsoleStreamReader reader = CreateReader(
            new ChunkStream(throwWhenExhausted: false, input[0..3], input[3..6], input[6..8], input[8..]),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        Assert.Equal("a\U0001F600", reader.ReadLine());
        Assert.Equal("é", reader.ReadLine());
        Assert.Null(reader.ReadLine());
    }

    [Fact]
    public void Read_AfterALineEndingInCarriageReturn_SkipsTheLineFeedThatPairsWithIt()
    {
        using ConsoleStreamReader reader = CreateReader("one\r\ntwo\r\n");

        Assert.Equal("one", reader.ReadLine());
        Assert.Equal('t', (char)reader.Peek());
        Assert.Equal('t', (char)reader.Read());
        Assert.Equal("wo", reader.ReadLine());
        Assert.Equal(-1, reader.Peek());
    }

    [Fact]
    public void ReadToEnd_AfterALineEndingInCarriageReturn_SkipsTheLineFeedThatPairsWithIt()
    {
        using ConsoleStreamReader reader = CreateReader("one\r\ntwo\r\nthree");

        Assert.Equal("one", reader.ReadLine());
        Assert.Equal("two\r\nthree", reader.ReadToEnd());
    }

    [Fact]
    public void ReadBlock_AfterALineEndingInCarriageReturn_SkipsTheLineFeedThatPairsWithIt()
    {
        using ConsoleStreamReader reader = CreateReader("one\r\ntwo\r\n");
        char[] buffer = new char[3];

        Assert.Equal("one", reader.ReadLine());
        Assert.Equal(3, reader.ReadBlock(buffer, 0, 3));
        Assert.Equal("two", new string(buffer));
    }

    [Fact]
    public void SpanReads_AfterALineEndingInCarriageReturn_SkipTheLineFeedThatPairsWithIt()
    {
        // StreamReader passes these to Read(char[], int, int) through TextReader; the reader relies on that.
        using ConsoleStreamReader reader = CreateReader("one\r\ntwo\r\nthree\r\n");
        char[] buffer = new char[5];

        Assert.Equal("one", reader.ReadLine());
        Assert.Equal(3, reader.Read(buffer.AsSpan(0, 3)));
        Assert.Equal("two", new string(buffer, 0, 3));
        Assert.Equal("", reader.ReadLine());
        Assert.Equal(5, reader.ReadBlock(buffer.AsSpan()));
        Assert.Equal("three", new string(buffer));
    }

    [Fact]
    public void Read_OfNoCharacters_DoesNotWaitForInput()
    {
        using ConsoleStreamReader reader = CreateReader(new ChunkStream(throwWhenExhausted: true, "one\r"));

        Assert.Equal("one", reader.ReadLine());
        Assert.Equal(0, reader.Read(new char[1], 0, 0));
        Assert.Equal(0, reader.ReadBlock(new char[1], 0, 0));
        Assert.Equal(0, reader.Read(Span<char>.Empty));
        Assert.Equal(0, reader.ReadBlock(Span<char>.Empty));
    }

    [Fact]
    public void Read_WithInvalidArguments_ThrowsWithoutWaitingForInput()
    {
        using ConsoleStreamReader reader = CreateReader(new ChunkStream(throwWhenExhausted: true, "one\r"));

        Assert.Equal("one", reader.ReadLine());
        Assert.Throws<ArgumentNullException>(() => reader.Read(null!, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.Read(new char[1], -1, 1));
        Assert.Throws<ArgumentException>(() => reader.Read(new char[1], 1, 1));
    }

    private static ConsoleStreamReader CreateReader(string input) =>
        CreateReader(new MemoryStream(Encoding.ASCII.GetBytes(input)));

    private static ConsoleStreamReader CreateReader(Stream stream, Encoding? encoding = null) =>
        new ConsoleStreamReader(stream, encoding ?? Encoding.ASCII, bufferSize: 1024);

    /// <summary>A stream that hands out one chunk per read, the way a console hands out one line.</summary>
    private sealed class ChunkStream : Stream
    {
        private readonly bool _throwWhenExhausted;
        private readonly byte[][] _chunks;
        private int _next;

        public ChunkStream(bool throwWhenExhausted, params string[] chunks)
            : this(throwWhenExhausted, Array.ConvertAll(chunks, Encoding.ASCII.GetBytes))
        {
        }

        public ChunkStream(bool throwWhenExhausted, params byte[][] chunks)
        {
            _throwWhenExhausted = throwWhenExhausted;
            _chunks = chunks;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_next == _chunks.Length)
            {
                if (_throwWhenExhausted)
                {
                    throw new ShouldNotBeInvokedException();
                }

                return 0;
            }

            byte[] chunk = _chunks[_next++];
            Assert.InRange(chunk.Length, 0, count);
            chunk.CopyTo(buffer, offset);
            return chunk.Length;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
