// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public static class EncodingHelpers
    {
        private static readonly RandomDataGenerator s_randomGenerator = new RandomDataGenerator();

        public static void GetByteCount(Encoding encoding, string chars, int index, int count, int expected)
        {
            char[] charArray = chars.ToCharArray();
            if (index == 0 && count == chars.Length)
            {
                // Use GetByteCount(string) or GetByteCount(char[])
                Assert.Equal(expected, encoding.GetByteCount(chars));
                Assert.Equal(expected, encoding.GetByteCount(charArray));
            }
            // Use GetByteCount(char[], int, int)
            Assert.Equal(expected, encoding.GetByteCount(charArray, index, count));
        }
        
        public static void GetBytes(Encoding encoding, string source, int index, int count, byte[] bytes, int byteIndex, byte[] expectedBytes)
        {
            byte[] originalBytes = (byte[])bytes.Clone();

            if (index == 0 && count == source.Length)
            {
                // Use GetBytes(string)
                byte[] stringResultBasic = encoding.GetBytes(source);
                VerifyGetBytes(stringResultBasic, 0, stringResultBasic.Length, originalBytes, expectedBytes);

                // Use GetBytes(char[])
                byte[] charArrayResultBasic = encoding.GetBytes(source.ToCharArray());
                VerifyGetBytes(charArrayResultBasic, 0, charArrayResultBasic.Length, originalBytes, expectedBytes);
            }
            // Use GetBytes(char[], int, int)
            byte[] charArrayResultAdvanced = encoding.GetBytes(source.ToCharArray(), index, count);
            VerifyGetBytes(charArrayResultAdvanced, 0, charArrayResultAdvanced.Length, originalBytes, expectedBytes);

            // Use GetBytes(string, int, int, byte[], int)
            byte[] stringBytes = (byte[])bytes.Clone();
            int stringByteCount = encoding.GetBytes(source, index, count, stringBytes, byteIndex);
            VerifyGetBytes(stringBytes, byteIndex, stringByteCount, originalBytes, expectedBytes);
            Assert.Equal(expectedBytes.Length, stringByteCount);

            // Use GetBytes(char[], int, int, byte[], int)
            byte[] charArrayBytes = (byte[])bytes.Clone();
            int charArrayByteCount = encoding.GetBytes(source.ToCharArray(), index, count, charArrayBytes, byteIndex);
            VerifyGetBytes(charArrayBytes, byteIndex, charArrayByteCount, originalBytes, expectedBytes);
            Assert.Equal(expectedBytes.Length, charArrayByteCount);
        }

        private static void VerifyGetBytes(byte[] bytes, int byteIndex, int byteCount, byte[] originalBytes, byte[] expectedBytes)
        {
            for (int i = 0; i < byteIndex; i++)
            {
                // Bytes outside the range should be ignored
                Assert.Equal(originalBytes[i], bytes[i]);
            }
            for (int i = byteIndex; i < byteIndex + byteCount; i++)
            {
                Assert.Equal(expectedBytes[i - byteIndex], bytes[i]);
            }
            for (int i = byteIndex + byteCount; i < bytes.Length; i++)
            {
                // Bytes outside the range should be ignored
                Assert.Equal(originalBytes[i], bytes[i]);
            }
        }

        public static void GetCharCount(Encoding encoding, byte[] bytes, int index, int count, int expected)
        {
            if (index == 0 && count == bytes.Length)
            {
                // Use GetCharCount(byte[])
                Assert.Equal(expected, encoding.GetCharCount(bytes));
            }
            // Use GetCharCount(byte[], int, int)
            Assert.Equal(expected, encoding.GetCharCount(bytes, index, count));
        }

        public static void GetChars(Encoding encoding, byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, char[] expectedChars)
        {
            char[] originalChars = (char[])chars.Clone();

            // Use GetChars(byte[])
            if (byteIndex == 0 && byteCount == bytes.Length)
            {
                char[] resultBasic = encoding.GetChars(bytes);
                VerifyGetChars(resultBasic, 0, resultBasic.Length, originalChars, expectedChars);
            }
            // Use GetChars(byte[], int, int)
            char[] resultAdvanced = encoding.GetChars(bytes, byteIndex, byteCount);
            VerifyGetChars(resultAdvanced, 0, resultAdvanced.Length, originalChars, expectedChars);

            // Use GetChars(byte[], int, int, char[], int)
            int charCount = encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            VerifyGetChars(chars, charIndex, charCount, originalChars, expectedChars);
            Assert.Equal(expectedChars.Length, charCount);
        }

        private static void VerifyGetChars(char[] chars, int charIndex, int charCount, char[] originalChars, char[] expectedChars)
        {
            for (int i = 0; i < charIndex; i++)
            {
                // Chars outside the range should be ignored
                Assert.Equal(originalChars[i], chars[i]);
            }
            for (int i = charIndex; i < charIndex + charCount; i++)
            {
                Assert.Equal(expectedChars[i - charIndex], chars[i]);
            }
            for (int i = charIndex + charCount; i < chars.Length; i++)
            {
                // Chars outside the range should be ignored
                Assert.Equal(originalChars[i], chars[i]);
            }
        }
        
        public static void GetString(Encoding encoding, byte[] bytes, int index, int count, string expected)
        {
            if (index == 0 && count == bytes.Length)
            {
                // Use GetString(byte[])
                Assert.Equal(expected, encoding.GetString(bytes));
            }
            // Use GetString(byte[], int, int)
            Assert.Equal(expected, encoding.GetString(bytes, index, count));
        }

        public static string GetString() => GetString(2, 260);
        public static string GetString(int length) => GetString(length, length);
        public static string GetString(int minLength, int maxLength) => s_randomGenerator.GetString(-55, false, minLength, maxLength);

        public static string GetRandomString(int length)
        {
            string result = string.Empty;
            int i = 0;
            while (i < length)
            {
                char temp = s_randomGenerator.GetChar(-55);
                if (!char.IsSurrogate(temp))
                {
                    result = result + temp.ToString();
                    i++;
                }
            }
            return result;
        }
    }
}
