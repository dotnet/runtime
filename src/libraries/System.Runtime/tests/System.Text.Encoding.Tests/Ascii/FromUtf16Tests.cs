// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Security.Cryptography;
using Xunit;

namespace System.Text.Tests
{
    public static class FromUtf16Tests
    {
        [Fact]
        public static unsafe void EmptyInputs()
        {
            Assert.Equal(OperationStatus.Done, Ascii.FromUtf16(ReadOnlySpan<char>.Empty, Span<byte>.Empty, out int bytesWritten));
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public static void AllAsciiInput()
        {
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(256);
            using BoundedMemory<byte> asciiMem = BoundedMemory.Allocate<byte>(256);

            // Fill source with 00 .. 7F.

            Span<char> utf16Span = utf16Mem.Span;
            for (int i = 0; i < utf16Span.Length; i++)
            {
                utf16Span[i] = (char)(i % 128);
            }
            utf16Mem.MakeReadonly();

            // We'll write to the ASCII span.
            // We test with a variety of span lengths to test alignment and fallthrough code paths.

            Span<byte> asciiSpan = asciiMem.Span;

            for (int i = 0; i < utf16Span.Length; i++)
            {
                asciiSpan.Clear(); // remove any data from previous iteration

                // First, validate that the workhorse saw the incoming data as all-ASCII.
                Assert.Equal(OperationStatus.Done, Ascii.FromUtf16(utf16Span.Slice(i), asciiSpan.Slice(i), out int bytesWritten));
                Assert.Equal(256 - i, bytesWritten);

                // Then, validate that the data was transcoded properly.

                for (int j = i; j < 256; j++)
                {
                    Assert.Equal((ushort)utf16Span[i], (ushort)asciiSpan[i]);
                }
            }
        }

        [Fact]
        public static void SomeNonAsciiInput()
        {
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(256);
            using BoundedMemory<byte> asciiMem = BoundedMemory.Allocate<byte>(256);

            // Fill source with 00 .. 7F.

            Span<char> utf16Span = utf16Mem.Span;
            for (int i = 0; i < utf16Span.Length; i++)
            {
                utf16Span[i] = (char)(i % 128);
            }

            // We'll write to the ASCII span.

            Span<byte> asciiSpan = asciiMem.Span;

            for (int i = utf16Span.Length - 1; i >= 0; i--)
            {
                RandomNumberGenerator.Fill(asciiSpan); // fill with garbage

                // First, keep track of the garbage we wrote to the destination.
                // We want to ensure it wasn't overwritten.

                byte[] expectedTrailingData = asciiSpan.Slice(i).ToArray();

                // Then, set the desired byte as non-ASCII, then check that the workhorse
                // correctly saw the data as non-ASCII.

                utf16Span[i] = '\u0123'; // use U+0123 instead of U+0080 since it catches inappropriate pmovmskb usage
                Assert.Equal(OperationStatus.InvalidData, Ascii.FromUtf16(utf16Span, asciiSpan, out int bytesWritten));
                Assert.Equal(i, bytesWritten);

                // Next, validate that the ASCII data was transcoded properly.

                for (int j = 0; j < i; j++)
                {
                    Assert.Equal((ushort)utf16Span[j], (ushort)asciiSpan[j]);
                }

                // Finally, validate that the trailing data wasn't overwritten with non-ASCII data.

                Assert.Equal(expectedTrailingData, asciiSpan.Slice(i).ToArray());
            }
        }
    }
}
