// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
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

    private const byte IndentFlag = 1;
    private const byte EncoderFlag = 1 << 1;
    private const byte MaxDepthFlag = 1 << 2;
    private const byte NewLineFlag = 1 << 3;
    private const byte SkipValidationFlag = 1 << 4;
    private const byte EncodingFlag = 1 << 5;
    private const byte PoisonFlag = 1 << 5;

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        const int minLength = 10; // 2 ints, 1 byte, and 1 padding to align chars
        if (bytes.Length < minLength)
        {
            return;
        }

        ReadOnlySpan<int> ints = MemoryMarshal.Cast<byte, int>(bytes);
        int slice1 = ints[0];
        int slice2 = ints[1];
        byte optionsByte = bytes[8];
        bytes = bytes.Slice(minLength);
        ReadOnlySpan<char> chars = MemoryMarshal.Cast<byte, char>(bytes);

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
        
        byte[] buffer = ArrayPool<byte>.Shared.Rent(6 * bytes.Length + 2);
        int written;
        Span<byte> expected = utf8
            ? EncodeToUtf8(bytes, buffer, options.Encoder, out written)
            : EncodeToUtf8(chars, buffer, options.Encoder, out written);

        for (int i = 1; i <= 3; i++)
        {
            using PooledBoundedMemory<byte> memory = PooledBoundedMemory<byte>.Rent(expected.Length, (optionsByte & PoisonFlag) == 0 ? PoisonPagePlacement.After : PoisonPagePlacement.Before);
            using MemoryBackedStream stream = new(memory.Memory);
            using Utf8JsonWriter writer = new(stream, options);
            try
            {
                int start = 0;
                if (utf8)
                {
                    if (i == 3)
                    {
                        writer.WriteStringValueSegment(bytes.Slice(start, slice1), false);
                        start = slice1;
                    }

                    if (i >= 2)
                    {
                        writer.WriteStringValueSegment(bytes.Slice(start, slice2 - start), false);
                        start = slice2;
                    }

                    writer.WriteStringValueSegment(bytes.Slice(start), true);
                    writer.Flush();
                }
                else
                {
                    if (i == 3)
                    {
                        writer.WriteStringValueSegment(chars.Slice(0, slice1), false);
                        start = slice1;
                    }

                    if (i >= 2)
                    {
                        writer.WriteStringValueSegment(chars.Slice(start, slice2 - start), false);
                        start = slice2;
                    }

                    writer.WriteStringValueSegment(chars.Slice(start), true);
                    writer.Flush();
                }
            }
            catch (JsonException) { return; }

            ReadOnlySpan<byte> actual = memory.Span;

            // Compare the expected and actual results
            Assert.SequenceEqual(expected, actual);
        }

        ArrayPool<byte>.Shared.Return(buffer);
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
