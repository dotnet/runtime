// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Mime.Tests
{
    public class EightBitStreamTest
    {
        [Theory]
        [InlineData("Hello World", "ASCII", false)]
        [InlineData(".Hello World", "ASCII", false)]
        [InlineData("Hello World \x7406", "Default", true)]
        [InlineData("Hello World \x7406", "UTF8", true)]
        [InlineData("Hello World 1 Hello World 2 Hello World 3 Hello World 4 Hello World 5 Hello World 6 Hello World 7 Hello World 8 Hello World 9 Hello World 10 ", "ASCII", false)]
        [InlineData("Hello World 1 Hello World 2 Hello World 3 Hello World 4 Hello World 5 Hello World 6 Hello World 7 Hello World 8 Hello World 9 Hello World 10 ", "ASCII", true)]
        public static void TestEncodeStream(string input, string encodingName, bool padLeadingDots)
        {
            Encoding encoding =
                encodingName == "ASCII" ? Encoding.ASCII :
                encodingName == "UTF8" ? Encoding.UTF8 :
                Encoding.Default;

            string expectedOutput = input;
            if (padLeadingDots && input.Length > 0 && input[0] == '.')
            {
                expectedOutput = "." + expectedOutput;
            }

            var outputStream = new MemoryStream();
            var testStream = new EightBitStream(outputStream, padLeadingDots);

            byte[] bytesToWrite = encoding.GetBytes(input);
            testStream.Write(bytesToWrite, 0, bytesToWrite.Length);

            outputStream.Seek(0, SeekOrigin.Begin);
            byte[] bytesRead = new byte[encoding.GetByteCount(expectedOutput) * 2];
            int bytesReadCount = outputStream.Read(bytesRead, 0, bytesRead.Length);

            Assert.Equal(expectedOutput, encoding.GetString(bytesRead, 0, bytesReadCount));
        }

        [Theory]
        [InlineData(new[] { "" }, true, "")]
        [InlineData(new[] { ".Hello.World" }, true, "..Hello.World")]
        [InlineData(new[] { "Hello\r\n.World" }, true, "Hello\r\n..World")]
        // A CRLF split across writes remains a single line ending, and the next leading dot is padded.
        [InlineData(new[] { "Hello\r", "\n.World" }, true, "Hello\r\n..World")]
        // An empty write between CR and LF must not interrupt CRLF recognition.
        [InlineData(new[] { "Hello\r", "", "\n.World" }, true, "Hello\r\n..World")]
        [InlineData(new[] { "Hello\r", "\n.World" }, false, "Hello\r\n.World")]
        // With transport encoding disabled, a standalone CR passes through verbatim.
        [InlineData(new[] { "Hello\r", "World" }, false, "Hello\rWorld")]
        // A bare LF is normalized to CRLF, and the next leading dot is padded.
        [InlineData(new[] { "Hello\n.World" }, true, "Hello\r\n..World")]
        // Some SMTP servers interpret "\n.\n" as end-of-data, so normalize and dot-stuff it.
        [InlineData(new[] { "a\n.\nb" }, true, "a\r\n..\r\nb")]
        // A bare LF at the end of a write still resets the line state for the next write.
        [InlineData(new[] { "Hello\n", ".World" }, true, "Hello\r\n..World")]
        // With transport encoding disabled, bare LFs and leading dots pass through verbatim.
        [InlineData(new[] { "Hello\n.World" }, false, "Hello\n.World")]
        [InlineData(new[] { "a\n.\nb" }, false, "a\n.\nb")]
        // A bare CR is normalized to CRLF, and the next leading dot is padded.
        [InlineData(new[] { "Hello\r.World" }, true, "Hello\r\n..World")]
        // Protect against servers that also interpret "\r.\r" as end-of-data.
        [InlineData(new[] { "a\r.\rb" }, true, "a\r\n..\r\nb")]
        // A bare CR at the end of a write still resets the line state for the next write.
        [InlineData(new[] { "Hello\r", ".World" }, true, "Hello\r\n..World")]
        // Ordinary data after CR ends CRLF recognition; a later LF starts a separate line.
        [InlineData(new[] { "Hello\r", "World", "\n.End" }, true, "Hello\r\nWorld\r\n..End")]
        // Consecutive line endings remain distinct, including across writes.
        [InlineData(new[] { "Hello\r", "\n", "\n.World" }, true, "Hello\r\n\r\n..World")]
        [InlineData(new[] { "\r", "\r", ".World" }, true, "\r\n\r\n..World")]
        [InlineData(new[] { "\n\r\r\n.\r\n\n.End\r" }, true, "\r\n\r\n\r\n..\r\n\r\n..End\r\n")]
        // A trailing CR is normalized immediately, even if only an empty write follows.
        [InlineData(new[] { "Hello\r", "" }, true, "Hello\r\n")]
        // With transport encoding disabled, bare CRs and leading dots pass through verbatim.
        [InlineData(new[] { "Hello\r.World" }, false, "Hello\r.World")]
        [InlineData(new[] { "a\r.\rb" }, false, "a\r.\rb")]
        [InlineData(new[] { "Hello\r", "" }, false, "Hello\r")]
        public static async Task TestEncodeStream_LineEndingsCanonicalizedToCrlf(string[] chunks, bool padLeadingDots, string expectedOutput)
        {
            foreach (bool useAsync in new[] { false, true })
            {
                using var outputStream = new MemoryStream();
                using var testStream = new EightBitStream(outputStream, padLeadingDots);

                foreach (string chunk in chunks)
                {
                    byte[] bytesToWrite = Encoding.ASCII.GetBytes(chunk);
                    if (useAsync)
                    {
                        await testStream.WriteAsync(bytesToWrite.AsMemory());
                        await testStream.FlushAsync();
                    }
                    else
                    {
                        testStream.Write(bytesToWrite.AsSpan());
                        testStream.Flush();
                    }
                }

                Assert.Equal(expectedOutput, Encoding.ASCII.GetString(outputStream.ToArray()));
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(15)]
        [InlineData(33)]
        [InlineData(65)]
        [InlineData(1025)]
        public static void TestEncodeStream_LongLines(int length)
        {
            using BoundedMemory<byte> bytesToWrite = BoundedMemory.Allocate<byte>(2 * length + 5);
            bytesToWrite.Span.Fill(0x80);
            bytesToWrite.Span[0] = (byte)'.';
            "\r\n."u8.CopyTo(bytesToWrite.Span.Slice(length + 1));
            bytesToWrite.Span[^1] = (byte)'\r';
            using var outputStream = new MemoryStream();
            using var testStream = new EightBitStream(outputStream, shouldEncodeLeadingDots: true);

            testStream.Write(bytesToWrite.Span);
            testStream.Write("\n."u8);

            byte[] expectedOutput = new byte[2 * length + 10];
            expectedOutput.AsSpan().Fill(0x80);
            ".."u8.CopyTo(expectedOutput);
            "\r\n.."u8.CopyTo(expectedOutput.AsSpan(length + 2));
            "\r\n.."u8.CopyTo(expectedOutput.AsSpan(2 * length + 6));
            Assert.Equal(expectedOutput, outputStream.ToArray());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static async Task TestEncodeStream_TrailingCarriageReturnCanonicalizedBeforeDispose(bool useAsyncDispose)
        {
            var outputStream = new MemoryStream();
            var testStream = new EightBitStream(outputStream, shouldEncodeLeadingDots: true);

            byte[] bytesToWrite = Encoding.ASCII.GetBytes("Hello\r");
            testStream.Write(bytesToWrite, 0, bytesToWrite.Length);

            Assert.Equal("Hello\r\n", Encoding.ASCII.GetString(outputStream.ToArray()));

            if (useAsyncDispose)
            {
                await testStream.DisposeAsync();
            }
            else
            {
                testStream.Dispose();
            }

            Assert.Equal("Hello\r\n", Encoding.ASCII.GetString(outputStream.ToArray()));

            // Disposal also disposes the underlying stream.
            Assert.False(outputStream.CanWrite);
        }
    }
}
