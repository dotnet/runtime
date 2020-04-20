// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public partial class AsciiUnitTests
    {
        [Fact]
        public void WidenAsciiToUtf16_EmptyInput_NullReferences()
        {
            OperationStatus status = Ascii.WidenToUtf16(ReadOnlySpan<byte>.Empty, Span<char>.Empty, out int bytesConsumed, out int charsWritten);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, charsWritten);
        }

        [Fact]
        public void WidenAsciiToUtf16_EmptyInput_NonNullReference()
        {
            using BoundedMemory<byte> sourceMem = BoundedMemory.Allocate<byte>(0);
            sourceMem.MakeReadonly();

            using BoundedMemory<char> destMem = BoundedMemory.Allocate<char>(0);

            OperationStatus status = Ascii.WidenToUtf16(sourceMem.Span, destMem.Span, out int bytesConsumed, out int charsWritten);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, charsWritten);
        }

        [Fact]
        public void WidenAsciiToUtf16_AllAsciiInput()
        {
            using BoundedMemory<byte> asciiMem = BoundedMemory.Allocate<byte>(128);
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(128);

            // Fill source with 00 .. 7F, then trap future writes.

            Span<byte> asciiSpan = asciiMem.Span;
            for (int i = 0; i < asciiSpan.Length; i++)
            {
                asciiSpan[i] = (byte)i;
            }
            asciiMem.MakeReadonly();

            // We'll write to the UTF-16 span.
            // We test with a variety of span lengths to test alignment and fallthrough code paths.

            Span<char> utf16Span = utf16Mem.Span;

            for (int i = 0; i < asciiSpan.Length; i++)
            {
                utf16Span.Clear(); // remove any data from previous iteration

                // First, validate that the workhorse saw the incoming data as all-ASCII.

                OperationStatus status = Ascii.WidenToUtf16(asciiSpan.Slice(i), utf16Span.Slice(i), out int bytesConsumed, out int charsWritten);
                Assert.Equal(OperationStatus.Done, status);
                Assert.Equal(128 - i, bytesConsumed);
                Assert.Equal(128 - i, charsWritten);

                // Then, validate that the data was transcoded properly.

                for (int j = i; j < 128; j++)
                {
                    Assert.Equal((ushort)asciiSpan[i], (ushort)utf16Span[i]);
                }
            }
        }

        [Fact]
        public void WidenAsciiToUtf16_SomeNonAsciiInput()
        {
            using BoundedMemory<byte> asciiMem = BoundedMemory.Allocate<byte>(128);
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(128);

            // Fill source with 00 .. 7F, then trap future writes.

            Span<byte> asciiSpan = asciiMem.Span;
            for (int i = 0; i < asciiSpan.Length; i++)
            {
                asciiSpan[i] = (byte)i;
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
                OperationStatus status = Ascii.WidenToUtf16(asciiSpan, utf16Span, out int bytesConsumed, out int charsWritten);
                Assert.Equal((i == asciiSpan.Length) ? OperationStatus.Done : OperationStatus.InvalidData, status);
                Assert.Equal(i, bytesConsumed);
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

        [Fact]
        public void WidenAsciiToUtf16_DestTooShort()
        {
            using BoundedMemory<byte> asciiMem = BoundedMemory.Allocate<byte>(129);
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(129);

            // Fill source with 00..80 (last byte is non-ASCII), then trap future writes.

            Span<byte> asciiSpan = asciiMem.Span;
            for (int i = 0; i < asciiSpan.Length; i++)
            {
                asciiSpan[i] = (byte)i;
            }
            asciiMem.MakeReadonly();

            // We'll write to the UTF-16 span

            Span<char> utf16Span = utf16Mem.Span;
            utf16Span.Clear();

            // If dest buffer runs out before we find non-ASCII data in the source,
            // we report "dest too small".

            OperationStatus status = Ascii.WidenToUtf16(asciiSpan, utf16Span.Slice(0, 128), out int bytesConsumed, out int charsWritten);
            Assert.Equal(OperationStatus.DestinationTooSmall, status);
            Assert.Equal(128, bytesConsumed);
            Assert.Equal(128, charsWritten);

            for (int i = 0; i < 128; i++)
            {
                Assert.Equal((ushort)asciiSpan[i], (ushort)utf16Span[i]);
            }

            // Otherwise we report that non-ASCII data was found.

            utf16Span.Clear();
            status = Ascii.WidenToUtf16(asciiSpan, utf16Span, out bytesConsumed, out charsWritten);
            Assert.Equal(OperationStatus.InvalidData, status);
            Assert.Equal(128, bytesConsumed);
            Assert.Equal(128, charsWritten);

            for (int i = 0; i < 128; i++)
            {
                Assert.Equal((ushort)asciiSpan[i], (ushort)utf16Span[i]);
            }
        }

        [Fact]
        public void NarrowUtf16ToAscii_EmptyInput_NullReferences()
        {
            OperationStatus status = Ascii.NarrowFromUtf16(ReadOnlySpan<char>.Empty, Span<byte>.Empty, out int charsConsumed, out int bytesWritten);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(0, charsConsumed);
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void NarrowUtf16ToAscii_EmptyInput_NonNullReference()
        {
            using BoundedMemory<char> sourceMem = BoundedMemory.Allocate<char>(0);
            sourceMem.MakeReadonly();

            using BoundedMemory<byte> destMem = BoundedMemory.Allocate<byte>(0);

            OperationStatus status = Ascii.NarrowFromUtf16(sourceMem.Span, destMem.Span, out int charsConsumed, out int bytesWritten);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(0, charsConsumed);
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void NarrowUtf16ToAscii_AllAsciiInput()
        {
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(128);
            using BoundedMemory<byte> asciiMem = BoundedMemory.Allocate<byte>(128);

            // Fill source with 00 .. 7F.

            Span<char> utf16Span = utf16Mem.Span;
            for (int i = 0; i < utf16Span.Length; i++)
            {
                utf16Span[i] = (char)i;
            }
            utf16Mem.MakeReadonly();

            // We'll write to the ASCII span.
            // We test with a variety of span lengths to test alignment and fallthrough code paths.

            Span<byte> asciiSpan = asciiMem.Span;

            for (int i = 0; i < utf16Span.Length; i++)
            {
                asciiSpan.Clear(); // remove any data from previous iteration

                // First, validate that the workhorse saw the incoming data as all-ASCII.

                OperationStatus status = Ascii.NarrowFromUtf16(utf16Span.Slice(i), asciiSpan.Slice(i), out int charsConsumed, out int bytesWritten);
                Assert.Equal(OperationStatus.Done, status);
                Assert.Equal(128 - i, charsConsumed);
                Assert.Equal(128 - i, bytesWritten);

                // Then, validate that the data was transcoded properly.

                for (int j = i; j < 128; j++)
                {
                    Assert.Equal((ushort)utf16Span[i], (ushort)asciiSpan[i]);
                }
            }
        }

        [Fact]
        public void NarrowUtf16ToAscii_SomeNonAsciiInput()
        {
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(128);
            using BoundedMemory<byte> asciiMem = BoundedMemory.Allocate<byte>(128);

            // Fill source with 00 .. 7F.

            Span<char> utf16Span = utf16Mem.Span;
            for (int i = 0; i < utf16Span.Length; i++)
            {
                utf16Span[i] = (char)i;
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
                OperationStatus status = Ascii.NarrowFromUtf16(utf16Span, asciiSpan, out int charsConsumed, out int bytesWritten);
                Assert.Equal((i == asciiSpan.Length) ? OperationStatus.Done : OperationStatus.InvalidData, status);
                Assert.Equal(i, charsConsumed);
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

        [Fact]
        public void NarrowUtf16ToAscii_DestTooShort()
        {
            using BoundedMemory<char> utf16Mem = BoundedMemory.Allocate<char>(129);
            using BoundedMemory<byte> asciiMem = BoundedMemory.Allocate<byte>(129);

            // Fill source with U+0000..U+007F, plus U+0123 for the last char (to catch bad pmovmskb usage), then trap future writes

            Span<char> utf16Span = utf16Mem.Span;
            for (int i = 0; i < 128; i++)
            {
                utf16Span[i] = (char)i;
            }
            utf16Span[^1] = '\u0123';
            utf16Mem.MakeReadonly();

            // We'll write to the ASCII span

            Span<byte> asciiSpan = asciiMem.Span;
            asciiSpan.Clear();

            // If dest buffer runs out before we find non-ASCII data in the source,
            // we report "dest too small".

            OperationStatus status = Ascii.NarrowFromUtf16(utf16Span, asciiSpan.Slice(0, 128), out int charsConsumed, out int bytesWritten);
            Assert.Equal(OperationStatus.DestinationTooSmall, status);
            Assert.Equal(128, charsConsumed);
            Assert.Equal(128, bytesWritten);

            for (int i = 0; i < 128; i++)
            {
                Assert.Equal((ushort)utf16Span[i], (ushort)asciiSpan[i]);
            }

            // Otherwise we report that non-ASCII data was found.

            asciiSpan.Clear();
            status = Ascii.NarrowFromUtf16(utf16Span, asciiSpan, out charsConsumed, out bytesWritten);
            Assert.Equal(OperationStatus.InvalidData, status);
            Assert.Equal(128, charsConsumed);
            Assert.Equal(128, bytesWritten);

            for (int i = 0; i < 128; i++)
            {
                Assert.Equal((ushort)utf16Span[i], (ushort)asciiSpan[i]);
            }
        }
    }
}
