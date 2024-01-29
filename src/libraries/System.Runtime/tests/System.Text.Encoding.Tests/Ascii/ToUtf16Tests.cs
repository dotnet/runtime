// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Xunit;

namespace System.Text.Tests
{
    public static class ToUtf16Tests
    {
        [Fact]
        public static void EmptyInputs()
        {
            Assert.Equal(OperationStatus.Done, Ascii.ToUtf16(ReadOnlySpan<byte>.Empty, Span<char>.Empty, out int charsWritten));
            Assert.Equal(0, charsWritten);
        }

        [Fact]
        public static void AllAsciiInput()
        {
            using BoundedMemory<byte> asciiMem = BoundedMemory.Allocate<byte>(256);
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(256);

            // Fill source with 00 .. 7F, then trap future writes.

            Span<byte> asciiSpan = asciiMem.Span;
            for (int i = 0; i < asciiSpan.Length; i++)
            {
                asciiSpan[i] = (byte)(i % 128);
            }
            asciiMem.MakeReadonly();

            // We'll write to the UTF-16 span.
            // We test with a variety of span lengths to test alignment and fallthrough code paths.

            Span<char> utf16Span = utf16Mem.Span;

            for (int i = 0; i < asciiSpan.Length; i++)
            {
                utf16Span.Clear(); // remove any data from previous iteration

                // First, validate that the workhorse saw the incoming data as all-ASCII.

                Assert.Equal(OperationStatus.Done, Ascii.ToUtf16(asciiSpan.Slice(i), utf16Span.Slice(i), out int charsWritten));
                Assert.Equal(256 - i, charsWritten);

                // Then, validate that the data was transcoded properly.

                for (int j = i; j < 256; j++)
                {
                    Assert.Equal((ushort)asciiSpan[i], (ushort)utf16Span[i]);
                }
            }
        }

        [Fact]
        public static void SomeNonAsciiInput()
        {
            using BoundedMemory<byte> asciiMem = BoundedMemory.Allocate<byte>(256);
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(256);

            // Fill source with 00 .. 7F, then trap future writes.

            Span<byte> asciiSpan = asciiMem.Span;
            for (int i = 0; i < asciiSpan.Length; i++)
            {
                asciiSpan[i] = (byte)(i % 128);
            }

            // We'll write to the UTF-16 span.

            Span<char> utf16Span = utf16Mem.Span;

            for (int i = asciiSpan.Length - 1; i >= 0; i--)
            {
                RandomNumberGenerator.Fill(MemoryMarshal.Cast<char, byte>(utf16Span)); // fill with garbage

                // First, keep track of the garbage we wrote to the destination.
                // We want to ensure it wasn't overwritten.

                char[] expectedTrailingData = utf16Span.Slice(i).ToArray();

                // Then, set the desired byte as non-ASCII, then check that the workhorse
                // correctly saw the data as non-ASCII.

                asciiSpan[i] |= (byte)0x80;

                Assert.Equal(OperationStatus.InvalidData, Ascii.ToUtf16(asciiSpan, utf16Span, out int charsWritten));
                Assert.Equal(i, charsWritten);

                // Next, validate that the ASCII data was transcoded properly.

                for (int j = 0; j < i; j++)
                {
                    Assert.Equal((ushort)asciiSpan[j], (ushort)utf16Span[j]);
                }

                // Finally, validate that the trailing data wasn't overwritten with non-ASCII data.

                Assert.Equal(expectedTrailingData, utf16Span.Slice(i).ToArray());
            }
        }
    }
}
