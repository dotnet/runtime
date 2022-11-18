// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Memory.Tests.SequenceReader;
using Xunit;

namespace System.Text.Tests
{
    public class EncodingExtensionsTests
    {
        private static readonly char[] AllScalarsAsUtf16 = CreateAllScalarsAsUtf16(); // 2,160,640 chars
        private static readonly byte[] AllScalarsAsUtf8 = Encoding.UTF8.GetBytes(AllScalarsAsUtf16); // 4,382,592 bytes

        private static char[] CreateAllScalarsAsUtf16()
        {
            List<char> list = new List<char>(2_160_640);

            // Add U+0000 .. U+D7FF

            for (int i = 0; i < 0xD800; i++)
            {
                list.Add((char)i);
            }

            // Add U+E000 .. U+10FFFF

            Span<char> scratch = stackalloc char[2]; // max UTF-16 sequence length
            for (int i = 0xE000; i <= 0x10FFFF; i++)
            {
                foreach (char ch in scratch.Slice(0, new Rune(i).EncodeToUtf16(scratch)))
                {
                    list.Add(ch);
                }
            }

            char[] allScalarsAsChars = list.ToArray();

            //  U+0000 ..   U+D7FF =     55,296 1-char sequences
            //  U+E000 ..   U+FFFF =     8,192 1-char sequences
            // U+10000 .. U+10FFFF = 1,048,576 2-char sequences
            //               total = 2,160,640 chars to encode all scalars as UTF-16
            //
            //  U+0000 ..   U+007F =       128 1-byte sequences
            //  U+0080 ..   U+07FF =     1,920 2-byte sequences
            //  U+0800 ..   U+D7FF =    53,247 3-byte sequences
            //  U+E000 ..   U+FFFF =     8,192 3-byte sequences
            // U+10000 .. U+10FFFF = 1,048,576 4-byte sequences
            //               total = 4,382,592 bytes to encode all scalars as UTF-8

            Assert.Equal(2_160_640, allScalarsAsChars.Length);
            Assert.Equal(4_382_592, Encoding.UTF8.GetByteCount(allScalarsAsChars));

            return allScalarsAsChars;
        }

        [Fact]
        public static void Convert_Decoder_ReadOnlySpan_IBufferWriter_ParamChecks()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            IBufferWriter<char> writer = new ArrayBufferWriter<char>();

            Assert.Throws<ArgumentNullException>("decoder", () => EncodingExtensions.Convert((Decoder)null, ReadOnlySpan<byte>.Empty, writer, true, out _, out _));
            Assert.Throws<ArgumentNullException>("writer", () => EncodingExtensions.Convert(decoder, ReadOnlySpan<byte>.Empty, (IBufferWriter<char>)null, true, out _, out _));
        }

        [Fact]
        public static void Convert_Decoder_ReadOnlySpan_IBufferWriter()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            ArrayBufferWriter<char> writer = new ArrayBufferWriter<char>();

            // First, a small input with no flushing and no leftover data.

            ReadOnlySpan<byte> inputData = "Hello"u8;
            EncodingExtensions.Convert(decoder, inputData, writer, flush: false, out long charsUsed, out bool completed);
            Assert.Equal(5, charsUsed);
            Assert.True(completed);

            // Then, a large input with no flushing and leftover data.

            inputData = Encoding.UTF8.GetBytes(new string('x', 20_000_000)).Concat(new byte[] { 0xE0, 0xA0 }).ToArray();
            EncodingExtensions.Convert(decoder, inputData, writer, flush: false, out charsUsed, out completed);
            Assert.Equal(20_000_000, charsUsed);
            Assert.True(completed);

            // Then, a large input with flushing and leftover data (should be replaced).

            inputData = new byte[] { 0x80 }.Concat(Encoding.UTF8.GetBytes(new string('x', 20_000_000))).Concat(new byte[] { 0xE0 }).ToArray();
            EncodingExtensions.Convert(decoder, inputData, writer, flush: true, out charsUsed, out completed);
            Assert.Equal(20_000_002, charsUsed); // 1 for leftover at beginning, 1 for replacement at end
            Assert.True(completed);

            // Now make sure all of the data was decoded properly.

            Assert.Equal(
                expected: "Hello" + new string('x', 20_000_000) + '\u0800' + new string('x', 20_000_000) + '\ufffd',
                actual: writer.WrittenSpan.ToString());
        }

        [Fact]
        public static void Convert_Decoder_ReadOnlySequence_IBufferWriter_ParamChecks()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            IBufferWriter<char> writer = new ArrayBufferWriter<char>();

            Assert.Throws<ArgumentNullException>("decoder", () => EncodingExtensions.Convert((Decoder)null, ReadOnlySequence<byte>.Empty, writer, true, out _, out _));
            Assert.Throws<ArgumentNullException>("writer", () => EncodingExtensions.Convert(decoder, ReadOnlySequence<byte>.Empty, (IBufferWriter<char>)null, true, out _, out _));
        }

        [Fact]
        public static void Convert_Decoder_ReadOnlySequence_IBufferWriter()
        {
            Decoder decoder = Encoding.UTF8.GetDecoder();
            ArrayBufferWriter<char> writer = new ArrayBufferWriter<char>();

            // First, input with no flushing and no leftover data.

            ReadOnlySequence<byte> inputData = SequenceFactory.Create(
                new byte[] { 0x20 }, // U+0020
                new byte[] { 0x61, 0xC2 }, // U+0061 and U+0080 (continues on next line)
                new byte[] { 0x80, 0xED, 0x9F, 0xBF }); // (cont.) + U+D7FF
            EncodingExtensions.Convert(decoder, inputData, writer, flush: false, out long charsUsed, out bool completed);
            Assert.Equal(4, charsUsed);
            Assert.True(completed);

            // Then, input with no flushing and leftover data.

            inputData = SequenceFactory.Create(
                new byte[] { 0xF4, 0x80 }); // U+100000 (continues on next line)
            EncodingExtensions.Convert(decoder, inputData, writer, flush: false, out charsUsed, out completed);
            Assert.Equal(0, charsUsed);
            Assert.True(completed);

            // Then, input with flushing and leftover data (should be replaced).

            inputData = SequenceFactory.Create(
                new byte[] { 0x80, 0x80 }, // (cont.)
                new byte[] { 0xC2 }); // leftover data (should be replaced)
            EncodingExtensions.Convert(decoder, inputData, writer, flush: true, out charsUsed, out completed);
            Assert.Equal(3, charsUsed);
            Assert.True(completed);

            // Now make sure all of the data was decoded properly.

            Assert.Equal("\u0020\u0061\u0080\ud7ff\U00100000\ufffd", writer.WrittenSpan.ToString());
        }

        [Fact]
        public static void Convert_Encoder_ReadOnlySpan_IBufferWriter_ParamChecks()
        {
            Encoder encoder = Encoding.UTF8.GetEncoder();
            IBufferWriter<byte> writer = new ArrayBufferWriter<byte>();

            Assert.Throws<ArgumentNullException>("encoder", () => EncodingExtensions.Convert((Encoder)null, ReadOnlySpan<char>.Empty, writer, true, out _, out _));
            Assert.Throws<ArgumentNullException>("writer", () => EncodingExtensions.Convert(encoder, ReadOnlySpan<char>.Empty, (IBufferWriter<byte>)null, true, out _, out _));
        }

        [Fact]
        public static void Convert_Encoder_ReadOnlySpan_IBufferWriter()
        {
            Encoder encoder = Encoding.UTF8.GetEncoder();
            ArrayBufferWriter<byte> writer = new ArrayBufferWriter<byte>();

            // First, a small input with no flushing and no leftover data.

            ReadOnlySpan<char> inputData = "Hello";
            EncodingExtensions.Convert(encoder, inputData, writer, flush: false, out long bytesUsed, out bool completed);
            Assert.Equal(5, bytesUsed);
            Assert.True(completed);

            // Then, a large input with no flushing and leftover data.

            inputData = new string('x', 20_000_000) + '\ud800';
            EncodingExtensions.Convert(encoder, inputData, writer, flush: false, out bytesUsed, out completed);
            Assert.Equal(20_000_000, bytesUsed);
            Assert.True(completed);

            // Then, a large input with flushing and leftover data (should be replaced).

            inputData = '\udc00' + new string('x', 20_000_000) + '\ud800';
            EncodingExtensions.Convert(encoder, inputData, writer, flush: true, out bytesUsed, out completed);
            Assert.Equal(20_000_007, bytesUsed); // 4 for supplementary at beginning, 3 for replacement at end
            Assert.True(completed);

            // Now make sure all of the data was encoded properly.
            // Use SequenceEqual instead of Assert.Equal for perf.

            Assert.True(
                Encoding.UTF8.GetBytes("Hello" + new string('x', 20_000_000) + "\U00010000" + new string('x', 20_000_000) + '\ufffd').AsSpan().SequenceEqual(writer.WrittenSpan));
        }

        [Fact]
        public static void Convert_Encoder_ReadOnlySequence_IBufferWriter_ParamChecks()
        {
            Encoder encoder = Encoding.UTF8.GetEncoder();
            IBufferWriter<byte> writer = new ArrayBufferWriter<byte>();

            Assert.Throws<ArgumentNullException>("encoder", () => EncodingExtensions.Convert((Encoder)null, ReadOnlySequence<char>.Empty, writer, true, out _, out _));
            Assert.Throws<ArgumentNullException>("writer", () => EncodingExtensions.Convert(encoder, ReadOnlySequence<char>.Empty, (IBufferWriter<byte>)null, true, out _, out _));
        }

        [Fact]
        public static void Convert_Encoder_ReadOnlySequence_IBufferWriter()
        {
            Encoder encoder = Encoding.UTF8.GetEncoder();
            ArrayBufferWriter<byte> writer = new ArrayBufferWriter<byte>();

            // First, input with no flushing and no leftover data.

            ReadOnlySequence<char> inputData = SequenceFactory.Create(
                new char[] { '\u0020' }, // U+0020
                new char[] { '\ud7ff' }); // U+D7FF
            EncodingExtensions.Convert(encoder, inputData, writer, flush: false, out long bytesUsed, out bool completed);
            Assert.Equal(4, bytesUsed);
            Assert.True(completed);

            // Then, input with no flushing and leftover data.

            inputData = SequenceFactory.Create(
                new char[] { '\udbc0' }); // U+100000 (continues on next line)
            EncodingExtensions.Convert(encoder, inputData, writer, flush: false, out bytesUsed, out completed);
            Assert.Equal(0, bytesUsed);
            Assert.True(completed);

            // Then, input with flushing and leftover data (should be replaced).

            inputData = SequenceFactory.Create(
                new char[] { '\udc00' }, // (cont.)
                new char[] { '\ud800' }); // leftover data (should be replaced)
            EncodingExtensions.Convert(encoder, inputData, writer, flush: true, out bytesUsed, out completed);
            Assert.Equal(7, bytesUsed);
            Assert.True(completed);

            // Now make sure all of the data was decoded properly.

            Assert.Equal("\u0020\ud7ff\U00100000\ufffd"u8.ToArray(), writer.WrittenSpan.ToArray());
        }

        [Fact]
        public static void GetBytes_Encoding_ReadOnlySequence_ParamChecks()
        {
            ReadOnlySequence<char> sequence = new ReadOnlySequence<char>(new char[0]);
            Assert.Throws<ArgumentNullException>("encoding", () => EncodingExtensions.GetBytes(null, sequence));
        }

        [Fact]
        public static void GetBytes_Encoding_ReadOnlySequence()
        {
            // First try the single-segment code path.

            ReadOnlySequence<char> sequence = new ReadOnlySequence<char>("Hello!".ToCharArray());
            Assert.Equal("Hello!"u8.ToArray(), EncodingExtensions.GetBytes(Encoding.UTF8, sequence));

            // Next try the multi-segment code path.
            // We've intentionally split multi-char subsequences here to test flushing mechanisms.

            sequence = SequenceFactory.Create(
                new char[] { '\u0020' }, // U+0020
                new char[] { '\u0061', '\u0080' }, // U+0061 and U+0080 (continues on next line)
                new char[] { '\ud800' }, // U+10000 (continues on next line)
                new char[] { }, // empty segment, just to make sure we handle it correctly
                new char[] { '\udc00', '\udbff' }, // (cont.) + U+10FFFF (continues on next line)
                new char[] { '\udfff' }, // (cont.)
                new char[] { '\ud800' }); // leftover data (should be replaced)

            Assert.Equal("\u0020\u0061\u0080\U00010000\U0010FFFF\ufffd"u8.ToArray(), EncodingExtensions.GetBytes(Encoding.UTF8, sequence));
        }

        [Fact]
        public static void GetBytes_Encoding_ReadOnlySequence_IBufferWriter_SingleSegment()
        {
            ReadOnlySequence<char> sequence = new ReadOnlySequence<char>("Hello".ToCharArray());
            ArrayBufferWriter<byte> writer = new ArrayBufferWriter<byte>();

            long bytesWritten = EncodingExtensions.GetBytes(Encoding.UTF8, sequence, writer);

            Assert.Equal(5, bytesWritten);
            Assert.Equal("Hello"u8.ToArray(), writer.WrittenSpan.ToArray());
        }

        [Fact]
        [OuterLoop] // this test takes ~10 seconds on modern hardware since it operates over GBs of data
        public static void GetBytes_Encoding_ReadOnlySequence_IBufferWriter_LargeMultiSegment()
        {
            ReadOnlySequence<char> sequence = GetLargeRepeatingReadOnlySequence<char>(AllScalarsAsUtf16, 1500); // ~ 3.2bn chars of UTF-16 input
            RepeatingValidatingBufferWriter<byte> writer = new RepeatingValidatingBufferWriter<byte>(AllScalarsAsUtf8);

            long expectedBytesWritten = 1500 * (long)AllScalarsAsUtf8.Length;
            long actualBytesWritten = EncodingExtensions.GetBytes(Encoding.UTF8, sequence, writer);

            Assert.Equal(expectedBytesWritten, actualBytesWritten);
            Assert.Equal(expectedBytesWritten, writer.TotalElementsWritten); // our writer will validate as data is written to it
        }

        [Fact]
        public static void GetBytes_Encoding_ReadOnlySequence_IBufferWriter_ParamChecks()
        {
            ReadOnlySequence<char> sequence = new ReadOnlySequence<char>(new char[0]);
            IBufferWriter<byte> writer = new ArrayBufferWriter<byte>();
            Assert.Throws<ArgumentNullException>("encoding", () => EncodingExtensions.GetBytes((Encoding)null, sequence, writer));
            Assert.Throws<ArgumentNullException>("writer", () => EncodingExtensions.GetBytes(Encoding.UTF8, sequence, (IBufferWriter<byte>)null));
        }

        [Fact]
        public static void GetBytes_Encoding_ReadOnlySequence_Span_ParamChecks()
        {
            ReadOnlySequence<char> sequence = new ReadOnlySequence<char>(new char[0]);
            Assert.Throws<ArgumentNullException>("encoding", () => EncodingExtensions.GetBytes((Encoding)null, sequence, Span<byte>.Empty));
        }

        [Fact]
        public static void GetBytes_Encoding_ReadOnlySequence_Span()
        {
            Span<byte> destination = stackalloc byte[32];

            // First try the single-segment code path.

            ReadOnlySequence<char> sequence = new ReadOnlySequence<char>("Hello!".ToCharArray());
            Assert.Equal(
                expected: "Hello!"u8.ToArray(),
                actual: destination.Slice(0, EncodingExtensions.GetBytes(Encoding.UTF8, sequence, destination)).ToArray());

            // Next try the multi-segment code path.
            // We've intentionally split multi-char subsequences here to test flushing mechanisms.

            sequence = SequenceFactory.Create(
                new char[] { '\u0020' }, // U+0020
                new char[] { '\u0061', '\u0080' }, // U+0061 and U+0080 (continues on next line)
                new char[] { '\ud800' }, // U+10000 (continues on next line)
                new char[] { }, // empty segment, just to make sure we handle it correctly
                new char[] { '\udc00', '\udbff' }, // (cont.) + U+10FFFF (continues on next line)
                new char[] { '\udfff' }, // (cont.)
                new char[] { '\ud800' }); // leftover data (should be replaced)

            Assert.Equal(
                expected: "\u0020\u0061\u0080\U00010000\U0010FFFF\ufffd"u8.ToArray(),
                actual: destination.Slice(0, EncodingExtensions.GetBytes(Encoding.UTF8, sequence, destination)).ToArray());
        }

        [Fact]
        public static void GetBytes_Encoding_ReadOnlySpan_IBufferWriter_ParamChecks()
        {
            IBufferWriter<byte> writer = new ArrayBufferWriter<byte>();

            Assert.Throws<ArgumentNullException>("encoding", () => EncodingExtensions.GetBytes((Encoding)null, ReadOnlySpan<char>.Empty, writer));
            Assert.Throws<ArgumentNullException>("writer", () => EncodingExtensions.GetBytes(Encoding.UTF8, ReadOnlySpan<char>.Empty, (IBufferWriter<byte>)null));
        }

        [Fact]
        public static void GetBytes_Encoding_ReadOnlySpan_IBufferWriter()
        {
            ArrayBufferWriter<byte> writer = new ArrayBufferWriter<byte>();

            // First, a small input that goes through the one-shot code path.

            ReadOnlySpan<char> inputData = "Hello";
            long bytesWritten = EncodingExtensions.GetBytes(Encoding.UTF8, inputData, writer);
            Assert.Equal(5, bytesWritten);
            Assert.Equal("Hello"u8.ToArray(), writer.WrittenSpan.ToArray());

            // Then, a large input that goes through the chunked path.
            // We alternate between 1-char and 2-char sequences so that the input will be split in
            // several locations by the internal GetChars chunking logic. This helps us test
            // that we're flowing the 'flush' parameter through the system correctly.

            string largeString = string.Create(5_000_000, (object)null, (span, _) =>
            {
                while (span.Length >= 3)
                {
                    span[0] = '\u00EA'; // U+00EA LATIN SMALL LETTER E WITH CIRCUMFLEX
                    span[1] = '\uD83D'; // U+1F405 TIGER
                    span[2] = '\uDC05';

                    span = span.Slice(3);
                }

                // There are 2 bytes left over.

                Assert.Equal(2, span.Length);
                span[0] = 'x';
                span[1] = 'y';
            });

            writer = new ArrayBufferWriter<byte>();
            inputData = largeString + '\uD800'; // standalone lead surrogate at end of input, testing replacement
            bytesWritten = EncodingExtensions.GetBytes(Encoding.UTF8, inputData, writer);
            Assert.Equal(10_000_001, bytesWritten); // 9,999,998 for data + 3 for repalcement char at end

            // Now make sure all of the data was encoded properly.

            Assert.True(Encoding.UTF8.GetBytes(largeString + "\ufffd").AsSpan().SequenceEqual(writer.WrittenSpan));
        }

        [Fact]
        public static void GetString_Encoding_ReadOnlySequence()
        {
            // First try the single-segment code path.

            ReadOnlySequence<byte> sequence = new ReadOnlySequence<byte>("Hello!"u8.ToArray());
            Assert.Equal("Hello!", EncodingExtensions.GetString(Encoding.UTF8, sequence));

            // Next try the multi-segment code path.
            // We've intentionally split multi-byte subsequences here to test flushing mechanisms.

            sequence = SequenceFactory.Create(
                new byte[] { 0x20 }, // U+0020
                new byte[] { 0x61, 0xC2 }, // U+0061 and U+0080 (continues on next line)
                new byte[] { 0x80, 0xED }, // (cont.) + U+D7FF (continues on next line)
                new byte[] { }, // empty segment, just to make sure we handle it correctly
                new byte[] { 0x9F, 0xBF, 0xF4, 0x80 }, // (cont.) + U+100000 (continues on next line)
                new byte[] { 0x80, 0x80 }, // (cont.)
                new byte[] { 0xC2 }); // leftover data (should be replaced)

            Assert.Equal("\u0020\u0061\u0080\ud7ff\U00100000\ufffd", EncodingExtensions.GetString(Encoding.UTF8, sequence));
        }

        [Fact]
        public static void GetString_Encoding_ReadOnlySequence_ParamChecks()
        {
            ReadOnlySequence<byte> sequence = new ReadOnlySequence<byte>(new byte[0]);
            Assert.Throws<ArgumentNullException>("encoding", () => EncodingExtensions.GetString(null, sequence));
        }

        [Fact]
        public static void GetChars_Encoding_ReadOnlySequence_IBufferWriter_SingleSegment()
        {
            ReadOnlySequence<byte> sequence = new ReadOnlySequence<byte>("Hello"u8.ToArray());
            ArrayBufferWriter<char> writer = new ArrayBufferWriter<char>();

            long charsWritten = EncodingExtensions.GetChars(Encoding.UTF8, sequence, writer);

            Assert.Equal(5, charsWritten);
            Assert.Equal("Hello", writer.WrittenSpan.ToString());
        }

        [Fact]
        [OuterLoop] // this test takes ~10 seconds on modern hardware since it operates over GBs of data
        public static void GetChars_Encoding_ReadOnlySequence_IBufferWriter_LargeMultiSegment()
        {
            ReadOnlySequence<byte> sequence = GetLargeRepeatingReadOnlySequence<byte>(AllScalarsAsUtf8, 1500); // ~ 6.5bn bytes of UTF-8 input
            RepeatingValidatingBufferWriter<char> writer = new RepeatingValidatingBufferWriter<char>(AllScalarsAsUtf16);

            long expectedCharsWritten = 1500 * (long)AllScalarsAsUtf16.Length;
            long actualCharsWritten = EncodingExtensions.GetChars(Encoding.UTF8, sequence, writer);

            Assert.Equal(expectedCharsWritten, actualCharsWritten);
            Assert.Equal(expectedCharsWritten, writer.TotalElementsWritten); // our writer will validate as data is written to it
        }

        [Fact]
        public static void GetChars_Encoding_ReadOnlySequence_IBufferWriter_ParamChecks()
        {
            ReadOnlySequence<byte> sequence = new ReadOnlySequence<byte>(new byte[0]);
            IBufferWriter<char> writer = new ArrayBufferWriter<char>();
            Assert.Throws<ArgumentNullException>("encoding", () => EncodingExtensions.GetChars((Encoding)null, sequence, writer));
            Assert.Throws<ArgumentNullException>("writer", () => EncodingExtensions.GetChars(Encoding.UTF8, sequence, (IBufferWriter<char>)null));
        }

        [Fact]
        public static void GetChars_Encoding_ReadOnlySequence_Span()
        {
            Span<char> destination = stackalloc char[32];

            // First try the single-segment code path.

            ReadOnlySequence<byte> sequence = new ReadOnlySequence<byte>("Hello!"u8.ToArray());
            Assert.Equal("Hello!", destination.Slice(0, EncodingExtensions.GetChars(Encoding.UTF8, sequence, destination)).ToString());

            // Next try the multi-segment code path.
            // We've intentionally split multi-byte subsequences here to test flushing mechanisms.

            sequence = SequenceFactory.Create(
                new byte[] { 0x20 }, // U+0020
                new byte[] { 0x61, 0xC2 }, // U+0061 and U+0080 (continues on next line)
                new byte[] { 0x80, 0xED }, // (cont.) + U+D7FF (continues on next line)
                new byte[] { }, // empty segment, just to make sure we handle it correctly
                new byte[] { 0x9F, 0xBF, 0xF4, 0x80 }, // (cont.) + U+100000 (continues on next line)
                new byte[] { 0x80, 0x80 }, // (cont.)
                new byte[] { 0xC2 }); // leftover data (should be replaced)

            Assert.Equal("\u0020\u0061\u0080\ud7ff\U00100000\ufffd", destination.Slice(0, EncodingExtensions.GetChars(Encoding.UTF8, sequence, destination)).ToString());
        }

        [Fact]
        public static void GetChars_Encoding_ReadOnlySequence_Span_ParamChecks()
        {
            ReadOnlySequence<byte> sequence = new ReadOnlySequence<byte>(new byte[0]);
            Assert.Throws<ArgumentNullException>("encoding", () => EncodingExtensions.GetChars((Encoding)null, sequence, Span<char>.Empty));
        }

        [Fact]
        public static void GetChars_Encoding_ReadOnlySpan_IBufferWriter_ParamChecks()
        {
            IBufferWriter<char> writer = new ArrayBufferWriter<char>();

            Assert.Throws<ArgumentNullException>("encoding", () => EncodingExtensions.GetChars((Encoding)null, ReadOnlySpan<byte>.Empty, writer));
            Assert.Throws<ArgumentNullException>("writer", () => EncodingExtensions.GetChars(Encoding.UTF8, ReadOnlySpan<byte>.Empty, (IBufferWriter<char>)null));
        }

        [Fact]
        public static void GetChars_Encoding_ReadOnlySpan_IBufferWriter()
        {
            ArrayBufferWriter<char> writer = new ArrayBufferWriter<char>();

            // First, a small input that goes through the one-shot code path.

            ReadOnlySpan<byte> inputData = "Hello"u8;
            long charsWritten = EncodingExtensions.GetChars(Encoding.UTF8, inputData, writer);
            Assert.Equal(5, charsWritten);
            Assert.Equal("Hello", writer.WrittenSpan.ToString());

            // Then, a large input that goes through the chunked path.
            // We use U+1234 because it's a 3-byte UTF-8 sequence, which means it'll be split in
            // several locations by the internal GetBytes chunking logic. This helps us test
            // that we're flowing the 'flush' parameter through the system correctly.

            writer = new ArrayBufferWriter<char>();
            inputData = Encoding.UTF8.GetBytes(new string('\u1234', 5_000_000)).Concat(new byte[] { 0xE0 }).ToArray();
            charsWritten = EncodingExtensions.GetChars(Encoding.UTF8, inputData, writer);
            Assert.Equal(5_000_001, charsWritten); // 5 MM for data, 1 for replacement char at end

            // Now make sure all of the data was decoded properly.

            Assert.Equal(
                expected: new string('\u1234', 5_000_000) + '\ufffd',
                actual: writer.WrittenSpan.ToString());
        }

        /// <summary>
        /// Returns a <see cref="ReadOnlySequence{T}"/> consisting of <paramref name="dataToRepeat"/> repeated <paramref name="repetitionCount"/> times.
        /// This can be used to produce a sequence consisting of billions of elements while consuming a fraction of that memory.
        /// </summary>
        /// <returns></returns>
        private static ReadOnlySequence<T> GetLargeRepeatingReadOnlySequence<T>(ReadOnlyMemory<T> dataToRepeat, int repetitionCount)
        {
            const int MAX_SEGMENT_LENGTH = 300_007; // a prime number, which ensures we'll have some multi-byte / multi-char splits if the data is long

            MockSequenceSegment<T> firstSegment = null;
            MockSequenceSegment<T> previousSegment = null;
            MockSequenceSegment<T> lastSegment = null;
            long runningTotalLength = 0;

            for (int i = 0; i < repetitionCount; i++)
            {
                ReadOnlyMemory<T> remainingData = dataToRepeat;
                while (!remainingData.IsEmpty)
                {
                    int thisSegmentLength = Math.Min(remainingData.Length, MAX_SEGMENT_LENGTH);

                    lastSegment = new MockSequenceSegment<T>
                    {
                        Memory = remainingData.Slice(0, thisSegmentLength),
                        RunningIndex = runningTotalLength
                    };

                    if (previousSegment != null)
                    {
                        previousSegment.Next = lastSegment;
                    }

                    previousSegment = lastSegment;
                    if (firstSegment == null)
                    {
                        firstSegment = lastSegment;
                    }

                    remainingData = remainingData.Slice(thisSegmentLength);
                    runningTotalLength += thisSegmentLength;
                }
            }

            return new ReadOnlySequence<T>(firstSegment, 0, lastSegment, lastSegment.Memory.Length);
        }

        /// <summary>
        /// An <see cref="IBufferWriter{T}"/> that validates that the data written to it consists of 'knownGoodData' repeated indefinitely.
        /// </summary>
        private class RepeatingValidatingBufferWriter<T> : IBufferWriter<T> where T : unmanaged, IEquatable<T>
        {
            private T[] _buffer;
            private readonly ReadOnlyMemory<T> _knownGoodData;

            public long TotalElementsWritten { get; private set; }

            public RepeatingValidatingBufferWriter(ReadOnlyMemory<T> knownGoodData)
            {
                Assert.False(knownGoodData.IsEmpty);
                _knownGoodData = knownGoodData;
            }

            public void Advance(int count)
            {
                ReadOnlySpan<T> bufferSpan = _buffer.AsSpan(0, count);
                ReadOnlySpan<T> remainingGoodDataSpan = _knownGoodData.Span.Slice((int)(TotalElementsWritten % _knownGoodData.Length));

                while (!bufferSpan.IsEmpty)
                {
                    int compareLength = Math.Min(bufferSpan.Length, remainingGoodDataSpan.Length);
                    Assert.True(remainingGoodDataSpan.Slice(0, compareLength).SequenceEqual(bufferSpan.Slice(0, compareLength)));

                    remainingGoodDataSpan = remainingGoodDataSpan.Slice(compareLength);
                    if (remainingGoodDataSpan.IsEmpty)
                    {
                        remainingGoodDataSpan = _knownGoodData.Span;
                    }

                    bufferSpan = bufferSpan.Slice(compareLength);
                }

                TotalElementsWritten += count;
            }

            public Memory<T> GetMemory(int sizeHint) => throw new NotImplementedException();

            public Span<T> GetSpan(int sizeHint)
            {
                if (_buffer is null || sizeHint > _buffer.Length)
                {
                    _buffer = new T[Math.Max(sizeHint, 128)];
                }

                return _buffer;
            }
        }

        /// <summary>
        /// A <see cref="ReadOnlySequenceSegment{T}"/> where all members are public.
        /// </summary>
        private sealed class MockSequenceSegment<T> : ReadOnlySequenceSegment<T>
        {
            public new ReadOnlyMemory<T> Memory
            {
                get => base.Memory;
                set => base.Memory = value;
            }

            public new ReadOnlySequenceSegment<T> Next
            {
                get => base.Next;
                set => base.Next = value;
            }

            public new long RunningIndex
            {
                get => base.RunningIndex;
                set => base.RunningIndex = value;
            }
        }
    }
}
