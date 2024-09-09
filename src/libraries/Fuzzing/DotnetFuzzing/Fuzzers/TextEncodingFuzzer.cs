// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace DotnetFuzzing.Fuzzers;

// The fuzzing infrastructure currently does not support fuzzing .NET Framework.
// However, this test class, while running under .NET Core, was used to foward the fuzzing
// input to a .NET Framework console app. That app had the same test semantics as the tests
// here, although used slightly different supporting APIs since not all supporting library
// and language features are present in .NET Framework.
// This fowarding approach and .NET Framework test code is presevered in the original Pull
// Request for this file. The approach used Base64 encoding to convert the incoming
// ReadOnlySpan<byte> to a string which was then passed to the Main() method of the .NET
// Framework app which was then converted back to bytes before being passed to the .NET
// Framework fuzzing tests.
internal sealed class TextEncodingFuzzer : IFuzzer
{
    string[] IFuzzer.TargetAssemblies => [];
    string[] IFuzzer.TargetCoreLibPrefixes { get; } = ["System.Text"];

    void IFuzzer.FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        using PooledBoundedMemory<byte> poisonAfter = PooledBoundedMemory<byte>.Rent(bytes, PoisonPagePlacement.After);

        TestLatin1(poisonAfter.Span);
        TestASCII(poisonAfter.Span);
        TestUnicode(poisonAfter.Span);
        TestUtf32(poisonAfter.Span);
        TestUtf7(poisonAfter.Span);
        TestUtf8(poisonAfter.Span);
    }

    // We use individual methods for each encoding, so if there's an exception then
    // it's clear which encoding failed based on the call stack.

    private static void TestLatin1(ReadOnlySpan<byte> input)
    {
        TestWithSubstitution(input, Encoding.GetEncoding("ISO-8859-1"));
        TestWithConvert(input, Encoding.GetEncoding("ISO-8859-1"));
    }

    private static void TestASCII(ReadOnlySpan<byte> input)
    {
        TestWithSubstitution(input, new ASCIIEncoding());
        TestWithConvert(input, new ASCIIEncoding());
    }

    private static void TestUnicode(ReadOnlySpan<byte> input)
    {
        TestWithSubstitution(input, new UnicodeEncoding());
        TestWithExceptions(input, new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true));
        TestWithConvert(input, new UnicodeEncoding());
    }

    private static void TestUtf32(ReadOnlySpan<byte> input)
    {
        TestWithSubstitution(input, new UTF32Encoding());
        TestWithExceptions(input, new UTF32Encoding(bigEndian: false, byteOrderMark: false, throwOnInvalidCharacters: true));
        TestWithConvert(input, new UTF32Encoding());
    }

    private static void TestUtf7(ReadOnlySpan<byte> input)
    {
#pragma warning disable SYSLIB0001 // Type or member is obsolete
        TestWithSubstitution(input, new UTF7Encoding());
#pragma warning restore SYSLIB0001
    }

    private static void TestUtf8(ReadOnlySpan<byte> input)
    {
        TestWithSubstitution(input, new UTF8Encoding());
        TestWithExceptions(input, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
        TestWithConvert(input, new UTF8Encoding());
    }

    private static void TestWithSubstitution(ReadOnlySpan<byte> input, Encoding encoding)
    {
        Decoder decoder = encoding.GetDecoder();
        int charCount = decoder.GetCharCount(input, flush: true);

        using PooledBoundedMemory<char> chars = PooledBoundedMemory<char>.Rent(charCount, PoisonPagePlacement.After);
        using PooledBoundedMemory<char> chars2 = PooledBoundedMemory<char>.Rent(charCount, PoisonPagePlacement.After);
        
        // *4 for worst case scenario (*2 for char->byte + *2 for encoding)
        // +2 is for possible Base64 padding with UTF7Encoding.
        using PooledBoundedMemory<byte> bytes = PooledBoundedMemory<byte>.Rent(charCount * 4 + 2, PoisonPagePlacement.After);

        decoder.Reset();
        int written = decoder.GetChars(input, chars.Span, flush: true);
        Assert.Equal(charCount, written);

        Encoder encoder = encoding.GetEncoder();
        // We use flush:true here for UTF7Encoding which may do Base64 padding at the end.
        int bytesWritten = encoder.GetBytes(chars.Span, bytes.Span, flush: true);

        // Decode the encoded values. Any substitutions will be comparable now.
        decoder.Reset();
        written = decoder.GetChars(bytes.Span.Slice(0, bytesWritten), chars2.Span, flush: true);
        Assert.Equal(charCount, written);

        // Verify that we round-tripped the values.
        Assert.SequenceEqual<char>(chars.Span, chars2.Span);
    }

    // If there are substitutions, these cases will fail with DecoderFallbackException early on,
    // otherwise there should be no DecoderFallbackExceptions.
    private static void TestWithExceptions(ReadOnlySpan<byte> input, Encoding encoding)
    {
        Assert.Equal(typeof(DecoderExceptionFallback), encoding.DecoderFallback.GetType());
        Assert.Equal(typeof(EncoderExceptionFallback), encoding.EncoderFallback.GetType());

        Decoder decoder = encoding.GetDecoder();

        int charCount;
        try
        {
            charCount = decoder.GetCharCount(input, flush: true);
        }
        catch (DecoderFallbackException)
        {
            // The input is not valid without fallbacks.
            return;
        }

        TestWithSubstitution(input, encoding);
    }

    private static void TestWithConvert(ReadOnlySpan<byte> input, Encoding encoding)
    {
        // Use a few boundary cases.
        TestWithConvert(input, encoding, 1);
        TestWithConvert(input, encoding, 2);
        TestWithConvert(input, encoding, 3);
        TestWithConvert(input, encoding, 4);
        TestWithConvert(input, encoding, input.Length);

        if (input.Length >= 6)
        {
            TestWithConvert(input, encoding, input.Length - 1);

            if (input.Length >= 12)
            {
                TestWithConvert(input, encoding, input.Length / 2);
            }
        }
    }

    // Verify that obtaining data using several Convert() calls matches the result from a single GetChars() call.
    private static void TestWithConvert(ReadOnlySpan<byte> input, Encoding encoding, int blockSize)
    {
        Decoder decoder = encoding.GetDecoder();
        Encoder encoder = encoding.GetEncoder();

        int charCount = decoder.GetCharCount(input, flush: true);

        using PooledBoundedMemory<char> chars = PooledBoundedMemory<char>.Rent(charCount, PoisonPagePlacement.After);
        using PooledBoundedMemory<char> chars2 = PooledBoundedMemory<char>.Rent(charCount, PoisonPagePlacement.After);

        decoder.Reset();        
        int charsUsedTotal = 0;
        int i = 0;

        while (i < input.Length)
        {
            bool lastIteration = i + blockSize >= input.Length;
            int bytesToRead = lastIteration ? input.Length - i : blockSize;

            decoder.Convert(
                input.Slice(i, bytesToRead),
                chars.Span.Slice(charsUsedTotal, charCount - charsUsedTotal),
                flush: lastIteration,
                out int bytesUsed,
                out int charsUsed,
                out bool _);

            i += bytesUsed;
            charsUsedTotal += charsUsed;
        }

        Assert.Equal(charsUsedTotal, charCount);
        decoder.Reset();
        decoder.GetChars(input, chars2.Span, flush: true);
        Assert.SequenceEqual<char>(chars.Span, chars2.Span);
    }
}
