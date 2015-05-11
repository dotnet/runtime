// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Text;

namespace EncodingTests
{
    public class EncodingTestHelper
    {
        private readonly string _encodingName;
        private const int BUFFER_AUTO = -1;
        private const int BUFFER_NULL = -2;
        private const int COUNT_AUTO = -100;

        public EncodingTestHelper(string encodingName)
        {
            _encodingName = encodingName;
        }

        private Encoding GetSpecificEncoding()
        {
            return Encoding.GetEncoding(_encodingName);
        }

        private Encoding GetSpecificEncoding(bool bEmit, bool bThrow)
        {
            switch (_encodingName)
            {
                case "UTF-7":
                    return new UTF7Encoding(bEmit); // allow optionals if set so
                case "UTF-8":
                    return new UTF8Encoding(bEmit, bThrow);
                case "UTF-16BE":
                    return new UnicodeEncoding(true, bEmit, bThrow);
                case "UTF-16LE":
                    return new UnicodeEncoding(false, bEmit, bThrow);
                case "UTF-32BE":
                    return new UTF32Encoding(true, bEmit, bThrow);
                case "UTF-32LE":
                    return new UTF32Encoding(false, bEmit, bThrow);
                default:
                    return Encoding.GetEncoding(_encodingName);
            }
        }

        // Tests all overloads of Encoding.GetBytCount.  The starting index is assumed to be 0
        public void GetByteCountTest(string inputBuffer, int count, int expected)
        {
            GetByteCountTest(inputBuffer, 0, count, expected);
        }

        // Tests all overloads of Encoding.GetByteCount with construction parameters
        public void GetByteCountTest(bool constructionParameter1, bool constructionParameter2, string input, int startIndex, int count, int expected)
        {
            var encoding = GetSpecificEncoding(constructionParameter1, constructionParameter2);
            GetByteCountTest(encoding, input, startIndex, count, expected); ;
        }

        // Tests all overloads of Encoding.GetByteCount.
        public void GetByteCountTest(string input, int startIndex, int count, int expected)
        {
            var encoding = GetSpecificEncoding();
            GetByteCountTest(encoding, input, startIndex, count, expected);
        }

        private void GetByteCountTest(Encoding encoding, string input, int startIndex, int count, int expected)
        {
            if (input != null)
            {
                GetByteCount(encoding, input.Substring(startIndex, count), expected);
                GetByteCount(encoding, input.ToCharArray(), startIndex, count, expected);
                GetByteCountUnsafe(encoding, input.ToCharArray(), startIndex, count, expected);
            }
            else
            {
                GetByteCount(encoding, null, expected);
                GetByteCount(encoding, null, startIndex, count, expected);
                GetByteCountUnsafe(encoding, null, startIndex, count, expected);
            }
        }

        // Test Encoding.GetByteCount(string) overload
        private void GetByteCount(Encoding encoding, string input, int expected)
        {
            var result = encoding.GetByteCount(input);
            Assert.Equal(expected, result);
        }

        // Test Encoding.GetByteCount(char[], int, int) overload
        private void GetByteCount(Encoding encoding, char[] input, int startIndex, int count, int expected)
        {
            var result = encoding.GetByteCount(input, startIndex, count);
            Assert.Equal(expected, result);
        }

        // Test Encoding.GetByteCount(char*, int) overload
        private unsafe void GetByteCountUnsafe(Encoding encoding, char[] input, int startIndex, int count, int expected)
        {
            // If length is 0, pointer will be null
            if (input.Length == 0) return;
            fixed (char* pInput = input)
            {
                var result = encoding.GetByteCount(pInput, count);
                Assert.Equal(expected, result);
            }
        }

        public void GetBytesTest(bool constructionParameter1, bool constructionParameter2, string input, int inputIndex, int inputCount, int outputBufferCreationMethod, int outputIndex, byte[] expectedOutput, int expectedOutputCount)
        {
            GetBytesTest(GetSpecificEncoding(constructionParameter1, constructionParameter2), input, inputIndex, inputCount, outputBufferCreationMethod, outputIndex, expectedOutput, expectedOutputCount);
        }

        public void GetBytesTest(string inputBuffer, int inputCount, int outputBufferCreationMethod, int outputCount, byte[] expectedOutput, int expectedOutputCount)
        {
            var outputIndex = ((outputCount < 0) && (outputCount != COUNT_AUTO)) ? outputCount : 0;
            GetBytesTest(inputBuffer, 0, inputCount, outputBufferCreationMethod, outputIndex, expectedOutput, expectedOutputCount);
        }

        public void GetBytesTest(string input, int inputIndex, int inputCount, int outputBufferCreationMethod, int outputIndex, byte[] expectedOutput, int expectedOutputCount)
        {
            GetBytesTest(GetSpecificEncoding(), input, inputIndex, inputCount, outputBufferCreationMethod, outputIndex, expectedOutput, expectedOutputCount);
        }

        private void GetBytesTest(Encoding encoding, string input, int inputIndex, int inputCount, int outputBufferCreationMethod, int outputIndex, byte[] expectedOutput, int expectedOutputCount)
        {
            var inputBuffer = input == null ? null : input.ToCharArray();

            switch (outputBufferCreationMethod)
            {
                case BUFFER_AUTO:
                    GetBytesTestAutoBuffer(encoding, inputBuffer, inputIndex, inputCount, outputIndex, expectedOutput, expectedOutputCount);
                    return;
                case BUFFER_NULL:
                    GetBytesTestNullBuffer(encoding, inputBuffer, inputIndex, inputCount, outputIndex, expectedOutput, expectedOutputCount);
                    return;
                default:
                    GetBytesTestBuffer(encoding, inputBuffer, inputIndex, inputCount, outputIndex, outputBufferCreationMethod, expectedOutput, expectedOutputCount);
                    return;
            }
        }

        public void GetCharCountTest(bool constructionParameter1, bool constructionParameter2, byte[] buffer, int startIndex, int count, int expected)
        {
            var encoding = GetSpecificEncoding(constructionParameter1, constructionParameter2);
            GetCharCountTest(encoding, buffer, startIndex, count, expected); ;
        }

        public void GetCharCountTest(byte[] buffer, int count, int expected)
        {
            GetCharCountTest(buffer, 0, count, expected);
        }

        public void GetCharCountTest(byte[] buffer, int startIndex, int count, int expected)
        {
            var encoding = GetSpecificEncoding();
            GetCharCountTest(encoding, buffer, startIndex, count, expected); ;
        }

        private void GetCharCountTest(Encoding encoding, byte[] buffer, int startIndex, int count, int expected)
        {
            var result = encoding.GetCharCount(buffer, startIndex, count);

            Assert.Equal(expected, result);
        }

        private int CalculateOutputIndex(int outputCount, int outputBufferCreationMethod)
        {
            if ((outputCount == 0) && (outputBufferCreationMethod > 0))
            {
                return 0;
            }

            return ((outputCount < 0) && (outputCount != COUNT_AUTO)) ? outputCount : 0;
        }

        public void GetCharsTest(byte[] inputBuffer, int inputCount, int outputBufferCreationmethod, int outputCount, string expectedOutput, int expectedOutputCount)
        {
            var outputIndex = CalculateOutputIndex(outputCount, outputBufferCreationmethod);
            GetCharsTest(inputBuffer, 0, inputCount, outputBufferCreationmethod, outputIndex, expectedOutput, expectedOutputCount);
        }

        public void GetCharsTest(bool constructionParameter1, bool constructionParameter2, byte[] inputBuffer, int inputIndex, int inputCount, int outputBufferCreationMethod, int outputIndex, string expectedOutput, int expectedOutputCount)
        {
            GetCharsTest(GetSpecificEncoding(constructionParameter1, constructionParameter2), inputBuffer, inputIndex, inputCount, outputBufferCreationMethod, outputIndex, expectedOutput, expectedOutputCount);
        }

        public void GetCharsTest(byte[] inputBuffer, int inputIndex, int inputCount, int outputBufferCreationMethod, int outputIndex, string expectedOutput, int expectedOutputCount)
        {
            GetCharsTest(GetSpecificEncoding(), inputBuffer, inputIndex, inputCount, outputBufferCreationMethod, outputIndex, expectedOutput, expectedOutputCount);
        }

        private void GetCharsTest(Encoding encoding, byte[] inputBuffer, int inputIndex, int inputCount, int outputBufferCreationMethod, int outputIndex, string expectedOutput, int expectedOutputCount)
        {
            switch (outputBufferCreationMethod)
            {
                case BUFFER_AUTO:
                    GetCharsTestAutoBuffer(encoding, inputBuffer, inputIndex, inputCount, outputIndex, expectedOutput, expectedOutputCount);
                    return;
                case BUFFER_NULL:
                    GetCharsTestNullBuffer(encoding, inputBuffer, inputIndex, inputCount, outputIndex, expectedOutput, expectedOutputCount);
                    return;
                default:
                    GetCharsTestBuffer(encoding, inputBuffer, inputIndex, inputCount, outputIndex, outputBufferCreationMethod, expectedOutput, expectedOutputCount);
                    return;
            }
        }

        private void GetCharsTestBuffer(Encoding encoding, byte[] inputBuffer, int inputIndex, int inputCount, int outputIndex, int bufferSize, string expectedOutput, int expectedOutputCount)
        {
            var buffer = new char[bufferSize];
            var result = encoding.GetChars(inputBuffer, inputIndex, inputCount, buffer, outputIndex);
            var actual = new string(buffer);

            Assert.Equal(expectedOutputCount, result);
            Assert.Equal(expectedOutput, actual);
        }

        private void GetCharsTestNullBuffer(Encoding encoding, byte[] inputBuffer, int inputIndex, int inputCount, int outputIndex, string expectedOutput, int expectedOutputCount)
        {
            var result = encoding.GetChars(inputBuffer, inputIndex, inputCount, null, 0);
        }

        private void GetCharsTestAutoBuffer(Encoding encoding, byte[] inputBuffer, int inputIndex, int inputCount, int outputIndex, string expectedOutput, int expectedOutputCount)
        {
            var outputBuffer = new char[encoding.GetCharCount(inputBuffer, inputIndex, inputCount)];
            var result = encoding.GetChars(inputBuffer, inputIndex, inputCount, outputBuffer, outputIndex == COUNT_AUTO ? 0 : outputIndex);
            var actual = new string(outputBuffer);

            Assert.Equal(expectedOutputCount, result);
            Assert.Equal(expectedOutput, actual);
        }

        private void GetBytesTestBuffer(Encoding encoding, char[] inputBuffer, int inputIndex, int inputCount, int outputIndex, int bufferSize, byte[] expectedOutput, int expectedOutputCount)
        {
            var actual = new byte[bufferSize];
            var result = encoding.GetBytes(inputBuffer, inputIndex, inputCount, actual, outputIndex == COUNT_AUTO ? 0 : outputIndex);

            Assert.Equal(expectedOutputCount, result);
            Assert.Equal(expectedOutput, actual);
        }

        private void GetBytesTestNullBuffer(Encoding encoding, char[] inputBuffer, int inputIndex, int inputCount, int outputIndex, byte[] expectedOutput, int expectedOutputCount)
        {
            var result = encoding.GetBytes(inputBuffer, inputIndex, inputCount, null, 0);
        }

        private void GetBytesTestAutoBuffer(Encoding encoding, char[] inputBuffer, int inputIndex, int inputCount, int outputIndex, byte[] expectedOutput, int expectedOutputCount)
        {
            var actual = new byte[encoding.GetByteCount(inputBuffer, inputIndex, inputCount)];
            var result = encoding.GetBytes(inputBuffer, inputIndex, inputCount, actual, outputIndex == COUNT_AUTO ? 0 : outputIndex);

            Assert.Equal(expectedOutputCount, result);
            Assert.Equal(expectedOutput, actual);
        }

        public void GetPreambleTest(byte[] expected)
        {
            var encoding = GetSpecificEncoding();
            Assert.Equal(expected, encoding.GetPreamble());
        }

        public void GetPreambleTest(bool constructionParameter1, bool constructionParameter2, byte[] expected)
        {
            var encoding = GetSpecificEncoding(constructionParameter1, constructionParameter2);
            Assert.Equal(expected, encoding.GetPreamble());
        }

        public void GetMaxByteCountTest(int charCount, int expected)
        {
            var encoding = GetSpecificEncoding();
            Assert.Equal(expected, encoding.GetMaxByteCount(charCount));
        }

        public void GetMaxCharCountTest(int byteCount, int expected)
        {
            var encoding = GetSpecificEncoding();
            Assert.Equal(expected, encoding.GetMaxCharCount(byteCount));
        }
    }
}
