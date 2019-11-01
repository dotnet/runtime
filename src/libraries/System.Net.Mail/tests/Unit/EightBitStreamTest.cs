// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
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
    }
}
