// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        // A CRLF split across two writes is treated as a single line ending, so the
        // leading dot on the following line is padded when padLeadingDots is enabled.
        [InlineData(new[] { "Hello\r", "\n.World" }, true, "Hello\r\n..World")]
        [InlineData(new[] { "Hello\r", "\n.World" }, false, "Hello\r\n.World")]
        // A standalone CR (not followed by LF) is emitted verbatim before the next byte.
        [InlineData(new[] { "Hello\r", "World" }, false, "Hello\rWorld")]
        public static void TestEncodeStream_CrlfSplitAcrossWrites(string[] chunks, bool padLeadingDots, string expectedOutput)
        {
            var outputStream = new MemoryStream();
            var testStream = new EightBitStream(outputStream, padLeadingDots);

            foreach (string chunk in chunks)
            {
                byte[] bytesToWrite = Encoding.ASCII.GetBytes(chunk);
                testStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            }

            Assert.Equal(expectedOutput, Encoding.ASCII.GetString(outputStream.ToArray()));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static async Task TestEncodeStream_TrailingCarriageReturnEmittedOnDispose(bool useAsyncDispose)
        {
            var outputStream = new MemoryStream();
            var testStream = new EightBitStream(outputStream, shouldEncodeLeadingDots: true);

            byte[] bytesToWrite = Encoding.ASCII.GetBytes("Hello\r");
            testStream.Write(bytesToWrite, 0, bytesToWrite.Length);

            // The trailing CR is deferred until we know whether an LF follows it.
            Assert.Equal("Hello", Encoding.ASCII.GetString(outputStream.ToArray()));

            if (useAsyncDispose)
            {
                await testStream.DisposeAsync();
            }
            else
            {
                testStream.Dispose();
            }

            // Disposal flushes the deferred CR.
            Assert.Equal("Hello\r", Encoding.ASCII.GetString(outputStream.ToArray()));
        }
    }
}
