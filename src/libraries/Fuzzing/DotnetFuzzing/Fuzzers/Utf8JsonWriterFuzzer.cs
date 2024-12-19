// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
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

    // Options for choosing between UTF-8 and UTF-16 encoding
    private const byte EncodingFlag = 1 << 5;

    // Options for choosing whether to poison previous or next page
    private const byte PoisonFlag = 1 << 6;

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
        bool utf8 = (optionsByte & EncodingFlag) == 0;
        if (!(0 <= slice1 && slice1 <= slice2 && slice2 <= (utf8 ? bytes.Length : chars.Length)))
        {
            return;
        }

        // Set up options based on the first byte
        bool indented = (optionsByte & IndentFlag) == 0;
        JsonWriterOptions options = new()
        {
            Encoder = (optionsByte & EncodingFlag) == 0 ? JavaScriptEncoder.Default : JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Indented = indented,
            MaxDepth = (optionsByte & MaxDepthFlag) == 0 ? 1 : 0,
            NewLine = (optionsByte & NewLineFlag) == 0 ? "\n" : "\r\n",
            SkipValidation = (optionsByte & SkipValidationFlag) == 0,
        };

        // Compute the expected result by using the encoder directly and the input
        int maxExpandedSizeBytes = 6 * bytes.Length + 2;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(maxExpandedSizeBytes);
        int written;
        Span<byte> expected = utf8
            ? EncodeToUtf8(bytes, buffer, options.Encoder, out written)
            : EncodeToUtf8(chars, buffer, options.Encoder, out written);

        // Compute the actual result by using Utf8JsonWriter. Each iteration is a different slice of the input, but the result should be the same.
        foreach (ReadOnlySpan<Range> ranges in new[]
        {
            new[] { 0.. },
            new[] { 0..slice1, slice1.. },
            new[] { 0..slice1, slice1..slice2, slice2.. },
        })
        {
            // Use a stream backed by bounded memory to detect out-of-bounds accesses
            using PooledBoundedMemory<byte> memory = PooledBoundedMemory<byte>.Rent(expected.Length, (optionsByte & PoisonFlag) == 0 ? PoisonPagePlacement.After : PoisonPagePlacement.Before);
            using MemoryBackedStream stream = new(memory.Memory);
            using Utf8JsonWriter writer = new(stream, options);
            
            if (utf8)
            {
                WriteStringValueSegments(writer, bytes, ranges);
                writer.Flush();
            }
            else
            {
                WriteStringValueSegments(writer, chars, ranges);
                writer.Flush();
            }

            ReadOnlySpan<byte> actual = memory.Span;

            // Compare the expected and actual results
            Assert.SequenceEqual(expected, actual);
        }

        // Additional test for mixing UTF-8 and UTF-16 encoding. The alignment math is easier in UTF-16 mode so just run it for that.
        if (!utf8)
        {
            {
                using PooledBoundedMemory<byte> memory = PooledBoundedMemory<byte>.Rent(maxExpandedSizeBytes, PoisonPagePlacement.Before);
                using MemoryBackedStream stream = new(memory.Memory);
                using Utf8JsonWriter writer = new(stream, options);

                writer.WriteStringValueSegment(chars[0..slice1], false);
                writer.WriteStringValueSegment(bytes[(2 * slice1)..], true);
                writer.Flush();
            }

            {
                using PooledBoundedMemory<byte> memory = PooledBoundedMemory<byte>.Rent(maxExpandedSizeBytes, PoisonPagePlacement.Before);
                using MemoryBackedStream stream = new(memory.Memory);
                using Utf8JsonWriter writer = new(stream, options);

                writer.WriteStringValueSegment(bytes[0..(2 * slice1)], false);
                writer.WriteStringValueSegment(chars[slice1..], true);
                writer.Flush();
            }
        }

        ArrayPool<byte>.Shared.Return(buffer);
    }

    private static void WriteStringValueSegments(Utf8JsonWriter writer, ReadOnlySpan<byte> bytes, ReadOnlySpan<Range> ranges)
    {
        for (int i = 0; i < ranges.Length; i++)
        {
            writer.WriteStringValueSegment(bytes[ranges[i]], i == ranges.Length - 1);
        }
    }

    private static void WriteStringValueSegments(Utf8JsonWriter writer, ReadOnlySpan<char> chars, ReadOnlySpan<Range> ranges)
    {
        for (int i = 0; i < ranges.Length; i++)
        {
            writer.WriteStringValueSegment(chars[ranges[i]], i == ranges.Length - 1);
        }
    }

    private static Span<byte> EncodeToUtf8(ReadOnlySpan<byte> bytes, Span<byte> destBuffer, JavaScriptEncoder encoder, out int written)
    {
        destBuffer[0] = (byte)'"';
        encoder.EncodeUtf8(bytes, destBuffer[1..], out _, out written, isFinalBlock: true);
        destBuffer[written + 1] = (byte)'"';
        return destBuffer.Slice(0, written + 2);
    }

    private static Span<byte> EncodeToUtf8(ReadOnlySpan<char> chars, Span<byte> destBuffer, JavaScriptEncoder encoder, out int written)
    {
        var utf16buffer = ArrayPool<char>.Shared.Rent(6 * chars.Length + 2);
        utf16buffer[0] = '"';
        encoder.Encode(chars, utf16buffer.AsSpan(1), out _, out written, isFinalBlock: true);
        utf16buffer[written + 1] = '"';

        Utf8.FromUtf16(utf16buffer.AsSpan(0, written + 2), destBuffer, out _, out written, isFinalBlock: true);
        ArrayPool<char>.Shared.Return(utf16buffer);
        return destBuffer[0..written];
    }
}
