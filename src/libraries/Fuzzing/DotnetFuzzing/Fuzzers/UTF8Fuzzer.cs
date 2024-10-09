// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text.Unicode;
using System.Text;

namespace DotnetFuzzing.Fuzzers;

// Adapted from https://github.com/GrabYourPitchforks/utf8fuzz
internal sealed class UTF8Fuzzer : IFuzzer
{
    public string[] TargetAssemblies => [];
    public string[] TargetCoreLibPrefixes { get; } = ["System.Text"];

    private static readonly Encoding s_encodingWithCustomReplacement =
        Encoding.GetEncoding("utf-8", EncoderFallback.ExceptionFallback, new DecoderReplacementFallback("{BAD}"));

    private static readonly ArrayBufferWriter<char> s_replacementBufferWriter = new(4096);

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        using var poisonAfter = PooledBoundedMemory<byte>.Rent(bytes, PoisonPagePlacement.After);

        Test(poisonAfter.Span);
    }

    private static void Test(ReadOnlySpan<byte> input)
    {
        int charCount = Encoding.UTF8.GetCharCount(input);

        Assert.Equal(charCount, GetCharCountFromRunes(input));

        using var chars = PooledBoundedMemory<char>.Rent(charCount, PoisonPagePlacement.After);
        using var chars2 = PooledBoundedMemory<char>.Rent(charCount + 1024, PoisonPagePlacement.After);

        Assert.Equal(charCount, Encoding.UTF8.GetChars(input, chars.Span));

        CompareUtf8AndUtf16RuneEnumeration(input, chars.Span);

        // ToUtf16 with replace=true and exact size buffer.
        {
            OperationStatus opStatus = Utf8.ToUtf16(input, chars2.Span.Slice(0, charCount), out int bytesReadJustNow, out int charsWrittenJustNow, replaceInvalidSequences: true, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, opStatus);
            Assert.Equal(input.Length, bytesReadJustNow);
            Assert.Equal(charCount, charsWrittenJustNow);
            Assert.SequenceEqual<char>(chars.Span, chars2.Span.Slice(0, charCount));
        }

        // ToUtf16 with replace=true and extra large buffer.
        {
            OperationStatus opStatus = Utf8.ToUtf16(input, chars2.Span, out int bytesReadJustNow, out int charsWrittenJustNow, replaceInvalidSequences: true, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, opStatus);
            Assert.Equal(input.Length, bytesReadJustNow);
            Assert.Equal(charCount, charsWrittenJustNow);
            Assert.SequenceEqual<char>(chars.Span, chars2.Span.Slice(0, charCount));
        }

        // Running ToUtf16 with replace=false and extra large buffer.
        {
            ReadOnlySpan<byte> utf8 = input;
            Span<char> output = chars2.Span;
            output.Clear();

            while (!utf8.IsEmpty)
            {
                OperationStatus opStatus = Utf8.ToUtf16(utf8, output, out int bytesReadJustNow, out int charsWrittenJustNow, replaceInvalidSequences: false, isFinalBlock: true);

                CompareUtf8AndUtf16RuneEnumeration(utf8.Slice(0, bytesReadJustNow), output.Slice(0, charsWrittenJustNow), expectIsValid: true);

                utf8 = utf8.Slice(bytesReadJustNow);

                if (opStatus != OperationStatus.Done)
                {
                    Assert.Equal(OperationStatus.InvalidData, opStatus);

                    // Skip over invalid data
                    Rune.DecodeFromUtf8(utf8, out _, out int bytesToSkip);
                    utf8 = utf8.Slice(bytesToSkip);
                }
            }
        }

        // Trying a custom decoder replacement.
        {
            ReadOnlySpan<byte> utf8 = input;

            string decoded = s_encodingWithCustomReplacement.GetString(utf8);

            using var decodedChars = PooledBoundedMemory<char>.Rent(decoded.Length, PoisonPagePlacement.After);

            s_replacementBufferWriter.ResetWrittenCount();

            while (!utf8.IsEmpty)
            {
                OperationStatus opStatus = Utf8.ToUtf16(utf8, decodedChars.Span, out int bytesReadJustNow, out int charsWrittenJustNow, replaceInvalidSequences: false, isFinalBlock: true);
                s_replacementBufferWriter.Write(decodedChars.Span.Slice(0, charsWrittenJustNow));

                utf8 = utf8.Slice(bytesReadJustNow);

                if (opStatus != OperationStatus.Done)
                {
                    Assert.Equal(OperationStatus.InvalidData, opStatus);

                    // Skip over invalid data
                    Rune.DecodeFromUtf8(utf8, out _, out int bytesToSkip);
                    utf8 = utf8.Slice(bytesToSkip);

                    s_replacementBufferWriter.Write("{BAD}");
                }
            }

            Assert.SequenceEqual(decoded, s_replacementBufferWriter.WrittenSpan);
        }
    }

    private static int GetCharCountFromRunes(ReadOnlySpan<byte> utf8)
    {
        int charCount = 0;

        while (!utf8.IsEmpty)
        {
            Rune.DecodeFromUtf8(utf8, out Rune thisRune, out int bytesConsumed);
            charCount += thisRune.Utf16SequenceLength; // ok if U+FFFD replacement
            utf8 = utf8.Slice(bytesConsumed);
        }

        return charCount;
    }

    private static void CompareUtf8AndUtf16RuneEnumeration(ReadOnlySpan<byte> utf8, ReadOnlySpan<char> utf16, bool expectIsValid = false)
    {
        while (!utf8.IsEmpty && !utf16.IsEmpty)
        {
            OperationStatus utf8Status = Rune.DecodeFromUtf8(utf8, out Rune inputUtf8Rune, out int bytesConsumed);
            OperationStatus utf16Status = Rune.DecodeFromUtf16(utf16, out Rune inputUtf16Rune, out int charsConsumed);

            if (expectIsValid)
            {
                Assert.Equal(OperationStatus.Done, utf8Status);
                Assert.Equal(OperationStatus.Done, utf16Status);
            }

            Assert.Equal(inputUtf8Rune, inputUtf16Rune);

            utf8 = utf8.Slice(bytesConsumed);
            utf16 = utf16.Slice(charsConsumed);
        }

        Assert.Equal(utf8.Length, utf16.Length);
    }
}
