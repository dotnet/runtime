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
        
        public static void GetBytes(Encoding encoding, string source, int index, int count, byte[] bytes, int byteIndex, int expected)
        {
            // Use GetBytes(string, int, int, byte[], int)
            byte[] stringBytes = (byte[])bytes.Clone();
            int stringResult = encoding.GetBytes(source, index, count, stringBytes, byteIndex);
            Assert.Equal(expected, stringResult);

            // Use GetBytes(char[], int, int, byte[], int)
            byte[] charArrayBytes = (byte[])bytes.Clone();
            int charArrayResult = encoding.GetBytes(source.ToCharArray(), index, count, charArrayBytes, byteIndex);
            Assert.Equal(expected, charArrayResult);
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

        public static void GetChars(Encoding encoding, byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int expected)
        {
            int result = encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            Assert.Equal(expected, result);
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
