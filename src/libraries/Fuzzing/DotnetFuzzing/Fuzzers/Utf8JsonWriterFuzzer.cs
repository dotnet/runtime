// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using SharpFuzz;

namespace DotnetFuzzing.Fuzzers;

internal sealed class Utf8JsonWriterFuzzer : IFuzzer
{
    public string[] TargetAssemblies { get; } = ["System.Text.Json"];

    public string[] TargetCoreLibPrefixes => [];

    // One of the bytes in the input is used to set various test options.
    // Each bit in that byte represents a different option as indicated here.

    // Options for JsonWriterOptions
    private const byte IndentFlag = 1;
    private const byte EncoderFlag = 1 << 1;
    private const byte MaxDepthFlag = 1 << 2;
    private const byte NewLineFlag = 1 << 3;
    private const byte SkipValidationFlag = 1 << 4;

    // Options for choosing between base64, UTF-8 and UTF-16 encoding
    private const byte EncodingMask = 0b11 << 5;
    private const byte Utf8EncodingFlag = 0b00 << 5;
    private const byte Utf16EncodingFlag = 0b01 << 5;
    private const byte Base64EncodingFlag = 0b10 << 5;

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        const int minLength = 10; // 2 ints, 1 byte, and 1 padding to align chars
        if (bytes.Length < minLength)
        {
            return;
        }

        // First 2 ints are used as indices to slice the input and the following byte is used for options
        ReadOnlySpan<int> ints = MemoryMarshal.Cast<byte, int>(bytes);
        int slice1 = ints[0];
        int slice2 = ints[1];
        byte optionsByte = bytes[8];
        bytes = bytes.Slice(minLength);
        ReadOnlySpan<char> chars = MemoryMarshal.Cast<byte, char>(bytes);

        // Validate that the indices are within bounds of the input
        int encoding = optionsByte & EncodingMask;
        if (encoding is not Utf8EncodingFlag and not Utf16EncodingFlag and not Base64EncodingFlag)
        {
            return;
        }

        if (!(0 <= slice1 && slice1 <= slice2 && slice2 <= (encoding is Utf16EncodingFlag ? chars.Length : bytes.Length)))
        {
            return;
        }

        // Set up options based on the first byte
        bool indented = (optionsByte & IndentFlag) == 0;
        JsonWriterOptions options = new()
        {
            Encoder = (optionsByte & EncoderFlag) == 0 ? JavaScriptEncoder.Default : JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Indented = indented,
            MaxDepth = (optionsByte & MaxDepthFlag) == 0 ? 1 : 0,
            NewLine = (optionsByte & NewLineFlag) == 0 ? "\n" : "\r\n",
            SkipValidation = (optionsByte & SkipValidationFlag) == 0,
        };

        // Compute the expected result by using the encoder directly and the input
        int maxExpandedSizeBytes = 6 * bytes.Length + 2;
        byte[] expectedBuffer = ArrayPool<byte>.Shared.Rent(maxExpandedSizeBytes);
        Span<byte> expected =
            expectedBuffer.AsSpan(0, encoding == Utf16EncodingFlag
                ? EncodeToUtf8(chars, expectedBuffer, options.Encoder)
                : EncodeToUtf8(bytes, expectedBuffer, options.Encoder, encoding == Base64EncodingFlag));

        // Compute the actual result by using Utf8JsonWriter. Each iteration is a different slice of the input, but the result should be the same.
        byte[] actualBuffer = new byte[expected.Length];
        foreach (ReadOnlySpan<Range> ranges in new[]
        {
            new[] { 0.. },
            new[] { 0..slice1, slice1.. },
            new[] { 0..slice1, slice1..slice2, slice2.. },
        })
        {
            using MemoryStream stream = new(actualBuffer);
            using Utf8JsonWriter writer = new(stream, options);

            if (encoding == Utf16EncodingFlag)
            {
                WriteStringValueSegments(writer, chars, ranges);
            }
            else
            {
                WriteStringValueSegments(writer, bytes, ranges, encoding == Base64EncodingFlag);
            }

            writer.Flush();

            // Compare the expected and actual results
            Assert.SequenceEqual(expected, actualBuffer);
            Assert.Equal(expected.Length, writer.BytesCommitted);
            Assert.Equal(0, writer.BytesPending);

            Array.Clear(actualBuffer);
        }

        // Additional test for mixing UTF-8 and UTF-16 encoding. The alignment math is easier in UTF-16 mode so just run it for that.
        if (encoding == Utf16EncodingFlag)
        {
            Array.Clear(expectedBuffer);

            {
                ReadOnlySpan<char> firstSegment = chars[slice1..];
                ReadOnlySpan<byte> secondSegment = bytes[0..(2 * slice1)];

                expected = expectedBuffer.AsSpan(0, EncodeToUtf8(firstSegment, expectedBuffer, options.Encoder));

                actualBuffer = new byte[expected.Length];
                using MemoryStream stream = new(actualBuffer);
                using Utf8JsonWriter writer = new(stream, options);

                // UTF-16 + UTF-8
                writer.WriteStringValueSegment(firstSegment, false);
                Assert.Throws<InvalidOperationException, ReadOnlySpan<byte>>(state => writer.WriteStringValueSegment(state, true), secondSegment);

                stream.Position = 0;
                writer.Reset();

                // UTF-16 + Base64
                writer.WriteStringValueSegment(firstSegment, false);
                Assert.Throws<InvalidOperationException, ReadOnlySpan<byte>>(state => writer.WriteBase64StringSegment(state, true), secondSegment);
            }

            Array.Clear(expectedBuffer);

            {
                ReadOnlySpan<byte> firstSegment = bytes[0..(2 * slice1)];
                ReadOnlySpan<char> secondSegment = chars[slice1..];

                expected = expectedBuffer.AsSpan(0, EncodeToUtf8(firstSegment, expectedBuffer, options.Encoder, base64Encode: false));

                actualBuffer = new byte[expected.Length];
                using MemoryStream stream = new(actualBuffer);
                using Utf8JsonWriter writer = new(stream, options);

                // UTF-8 + UTF-16
                writer.WriteStringValueSegment(firstSegment, false);
                Assert.Throws<InvalidOperationException, ReadOnlySpan<char>>(state => writer.WriteStringValueSegment(state, true), secondSegment);

                stream.Position = 0;
                writer.Reset();

                // UTF-8 + Base64
                writer.WriteStringValueSegment(firstSegment, false);
                Assert.Throws<InvalidOperationException, ReadOnlySpan<byte>>(state => writer.WriteBase64StringSegment(state, true), MemoryMarshal.AsBytes(secondSegment));
            }

            Array.Clear(expectedBuffer);

            {
                ReadOnlySpan<byte> firstSegment = bytes[0..(2 * slice1)];
                ReadOnlySpan<char> secondSegment = chars[slice1..];

                expected = expectedBuffer.AsSpan(0, EncodeToUtf8(firstSegment, expectedBuffer, options.Encoder, base64Encode: true));

                actualBuffer = new byte[expected.Length];
                using MemoryStream stream = new(actualBuffer);
                using Utf8JsonWriter writer = new(stream, options);

                // Base64 + UTF-16
                writer.WriteBase64StringSegment(firstSegment, false);
                Assert.Throws<InvalidOperationException, ReadOnlySpan<char>>(state => writer.WriteStringValueSegment(state, true), secondSegment);

                stream.Position = 0;
                writer.Reset();

                // Base64 + UTF-8
                writer.WriteBase64StringSegment(firstSegment, false);
                Assert.Throws<InvalidOperationException, ReadOnlySpan<byte>>(state => writer.WriteStringValueSegment(state, true), MemoryMarshal.AsBytes(secondSegment));
            }
        }

        ArrayPool<byte>.Shared.Return(expectedBuffer);
    }

    private static void WriteStringValueSegments(Utf8JsonWriter writer, ReadOnlySpan<byte> bytes, ReadOnlySpan<Range> ranges, bool base64Encode)
    {
        if (base64Encode)
        {
            for (int i = 0; i < ranges.Length; i++)
            {
                writer.WriteBase64StringSegment(bytes[ranges[i]], i == ranges.Length - 1);
            }
        }
        else
        {
            for (int i = 0; i < ranges.Length; i++)
            {
                writer.WriteStringValueSegment(bytes[ranges[i]], i == ranges.Length - 1);
            }
        }
    }

    private static void WriteStringValueSegments(Utf8JsonWriter writer, ReadOnlySpan<char> chars, ReadOnlySpan<Range> ranges)
    {
        for (int i = 0; i < ranges.Length; i++)
        {
            writer.WriteStringValueSegment(chars[ranges[i]], i == ranges.Length - 1);
        }
    }

    private static int EncodeToUtf8(ReadOnlySpan<byte> bytes, Span<byte> destBuffer, JavaScriptEncoder encoder, bool base64Encode)
    {
        destBuffer[0] = (byte)'"';

        int written;
        if (base64Encode)
        {
            Base64.EncodeToUtf8(bytes, destBuffer[1..], out _, out written, isFinalBlock: true);
        }
        else
        {
            encoder.EncodeUtf8(bytes, destBuffer[1..], out _, out written, isFinalBlock: true);
        }

        destBuffer[++written] = (byte)'"';
        return written + 1;
    }

    private static int EncodeToUtf8(ReadOnlySpan<char> chars, Span<byte> destBuffer, JavaScriptEncoder encoder)
    {
        int written = 1;
        destBuffer[0] = (byte)'"';
        destBuffer[written += EncodeTranscode(chars, destBuffer[1..], encoder)] = (byte)'"';
        return written + 1;
    }

    private static int EncodeTranscode(ReadOnlySpan<char> chars, Span<byte> destBuffer, JavaScriptEncoder encoder, bool isFinalBlock = true)
    {
        var utf16buffer = ArrayPool<char>.Shared.Rent(6 * chars.Length);
        encoder.Encode(chars, utf16buffer, out _, out int written, isFinalBlock: true);

        Utf8.FromUtf16(utf16buffer.AsSpan(0, written), destBuffer, out _, out written, isFinalBlock);
        ArrayPool<char>.Shared.Return(utf16buffer);
        return written;
    }
}
