// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using System;

namespace DotnetFuzzing.Fuzzers
{
    internal sealed class TextEncoding
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: TextEncoding <base64-encoded-bytes>");
                return;
            }

            byte[] bytes = Convert.FromBase64String(args[0]);

            TextEncoding textEncoding = new TextEncoding();
            textEncoding.FuzzTarget(bytes);
        }

        public void FuzzTarget(ReadOnlySpan<byte> bytes)
        {
            using (PooledBoundedMemory<byte> poisonAfter = PooledBoundedMemory<byte>.Rent(bytes, PoisonPagePlacement.After))
            {
                TestLatin1(poisonAfter.Span);
                TestASCII(poisonAfter.Span);
                TestUnicode(poisonAfter.Span);
                TestUtf32(poisonAfter.Span);
                TestUtf7(poisonAfter.Span);
                TestUtf8(poisonAfter.Span);
            }
        }

        // Use individual methods for each encoding, so if there's an exception then 
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

        unsafe private static void TestWithSubstitution(ReadOnlySpan<byte> input, Encoding encoding)
        {
            Decoder decoder = encoding.GetDecoder();

            fixed (byte* pInput = input)
            {
                int charCount = encoding.GetCharCount(pInput, input.Length);

                using (PooledBoundedMemory<char> chars = PooledBoundedMemory<char>.Rent(charCount, PoisonPagePlacement.After))
                using (PooledBoundedMemory<char> chars2 = PooledBoundedMemory<char>.Rent(charCount, PoisonPagePlacement.After))
                using (PooledBoundedMemory<byte> bytes = PooledBoundedMemory<byte>.Rent(charCount * 4 + 2, PoisonPagePlacement.After))
                fixed (char* pchars = chars.Span)
                fixed (char* pchars2 = chars2.Span)
                fixed (byte* pbytes = bytes.Span)
                {
                    if (chars.Span.Length == 0)
                    {
                        return;
                    }

                    decoder.Reset();
                    int written = decoder.GetChars(pInput, input.Length, pchars, chars.Span.Length, flush: true);
                    Assert.Equal(charCount, written);

                    Encoder encoder = encoding.GetEncoder();
                    int bytesWritten = encoder.GetBytes(pchars, chars.Span.Length, pbytes, bytes.Span.Length, flush: true);

                    // Decode the encoded values. Any substitutions will be comparable now.
                    decoder.Reset();
                    written = decoder.GetChars(pbytes, bytesWritten, pchars2, chars2.Span.Length, flush: true);
                    Assert.Equal(charCount, written);

                    // Verify that we round-tripped the values.
                    Assert.SequenceEqual(chars.Span, chars2.Span);
                }
            }
        }

        // If there are substitutions, these cases will fail with DecoderFallbackException early on,
        // otherwise there should be no DecoderFallbackExceptions.
        unsafe private static void TestWithExceptions(ReadOnlySpan<byte> input, Encoding encoding)
        {
            Assert.Equal(typeof(DecoderExceptionFallback), encoding.DecoderFallback.GetType());
            Assert.Equal(typeof(EncoderExceptionFallback), encoding.EncoderFallback.GetType());

            Decoder decoder = encoding.GetDecoder();

            int charCount;
            try
            {
                fixed (byte* pinput = input)
                {
                    charCount = decoder.GetCharCount(pinput, input.Length, flush: true);
                }
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
        unsafe private static void TestWithConvert(ReadOnlySpan<byte> input, Encoding encoding, int blockSize)
        {
            Decoder decoder = encoding.GetDecoder();
            Encoder encoder = encoding.GetEncoder();

            fixed (byte* pinput = input)
            {
                int charCount = decoder.GetCharCount(pinput, input.Length, flush: true);

                using (PooledBoundedMemory<char> chars = PooledBoundedMemory<char>.Rent(charCount, PoisonPagePlacement.After))
                using (PooledBoundedMemory<char> chars2 = PooledBoundedMemory<char>.Rent(charCount, PoisonPagePlacement.After))
                fixed (char* pchars = chars.Span)
                fixed (char* pchars2 = chars2.Span)
                {
                    decoder.Reset();
                    int charsUsedTotal = 0;
                    int i = 0;

                    while (i < input.Length)
                    {
                        bool lastIteration = i + blockSize >= input.Length;
                        int bytesToRead = lastIteration ? input.Length - i : blockSize;

                        decoder.Convert(
                            bytes: pinput + i,
                            byteCount: bytesToRead,
                            chars: pchars + charsUsedTotal,
                            charCount: charCount - charsUsedTotal,
                            flush: lastIteration,
                            out int bytesUsed,
                            out int charsUsed,
                            out bool _);

                        i += bytesUsed;
                        charsUsedTotal += charsUsed;
                    }

                    Assert.Equal(charsUsedTotal, charCount);
                    decoder.Reset();
                    decoder.GetChars(pinput, input.Length, pchars2, chars2.Span.Length, flush: true);
                    Assert.SequenceEqual(chars.Span, chars2.Span);
                }
            }
        }
    }
}
