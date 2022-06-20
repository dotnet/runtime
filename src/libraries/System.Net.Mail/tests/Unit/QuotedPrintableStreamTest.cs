// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using Xunit;

namespace System.Net.Mime.Tests
{
    public class QuotedPrintableStreamTest
    {
        [Theory]
        [InlineData("Hello \r\n World", "Hello \r\n World", "ASCII", false)]
        [InlineData("Hello World  ", "Hello World =20", "ASCII", true)]
        [InlineData(".Hello World", ".Hello World", "ASCII", true)]
        [InlineData("Hello \r World \n", "Hello =0D World =0A", "ASCII", true)]
        [InlineData("Hello \r World \n", "Hello =0D World =0A", "ASCII", false)]
        [InlineData("Hello \r\n World", "Hello =0D=0A World", "ASCII", true)]
        [InlineData("Hello World", "Hello World", "ASCII", true)]
        [InlineData("Hello World \x00E9", "Hello World =C3=A9", "Default", true)]
        [InlineData("Hello World \x7406", "Hello World =E7=90=86", "UTF8", true)]
        [InlineData("Hello World \x7406\x7406\x7406\x7406\x7406\x7406\x7406", "Hello World =E7=90=86=E7=90=86=E7=90=86=E7=90=86=E7=90=86=E7=90=86=\r\n=E7=90=86", "UTF8", true)]
        [InlineData("Hello World 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21 22 23", "Hello World 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21=\r\n 22 23", "ASCII", true)]
        public static void TestEncodeStream(string input, string expectedOutput, string encodingName, bool encodeCRLF)
        {
            Encoding encoding =
                encodingName == "ASCII" ? Encoding.ASCII :
                encodingName == "UTF8" ? Encoding.UTF8 :
                Encoding.Default;

            var outputStream = new MemoryStream();
            var testStream = new QuotedPrintableStream(outputStream, encodeCRLF);

            byte[] bytesToWrite = encoding.GetBytes(input);
            testStream.Write(bytesToWrite, 0, bytesToWrite.Length);
            testStream.Flush();

            outputStream.Seek(0, SeekOrigin.Begin);
            byte[] bytesRead = new byte[encoding.GetByteCount(expectedOutput) * 2];
            int bytesReadCount = outputStream.Read(bytesRead, 0, bytesRead.Length);

            string results = encoding.GetString(bytesRead, 0, bytesReadCount);
            Assert.Equal(expectedOutput, results);
        }

        [Fact]
        public void QuotedPrintableStream_EmbededCRAndLFSpltBetweenWrites_EncodeCrlfFlagNotHonored()
        {
            // When split across writes, the stream encodes CRLF even though we asked it not to.
            var outputStream = new MemoryStream();
            var testStream = new QuotedPrintableStream(outputStream, false);

            testStream.Write("Hello \r"u8);
            testStream.Write("\n World"u8);
            testStream.Flush();

            // We told it not to encode them, but they got split across writes so it could not
            // detect the sequence.  This can happen any time we try to encode only a subset
            // of the data at a time.
            const string ExpectedOutput = "Hello =0D=0A World";
            outputStream.Seek(0, SeekOrigin.Begin);
            byte[] bytesRead = new byte[Encoding.ASCII.GetByteCount(ExpectedOutput) * 2];
            int bytesReadCount = outputStream.Read(bytesRead, 0, bytesRead.Length);

            string results = Encoding.ASCII.GetString(bytesRead, 0, bytesReadCount);
            Assert.Equal(ExpectedOutput, results);
        }
    }
}
