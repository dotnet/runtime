// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Tests
{
    public static class EncodingHelpers
    {
        public static void Encode(Encoding encoding, string chars, int index, int count, byte[] expected)
        {
            GetByteCount(encoding, chars, index, count, expected.Length);
            GetBytes(encoding, chars, index, count, expected);

            GetByteCount_NetCoreApp(encoding, chars, index, count, expected.Length);
            GetBytes_NetCoreApp(encoding, chars, index, count, expected);
        }

        private static unsafe void GetByteCount(Encoding encoding, string chars, int index, int count, int expected)
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

            fixed (char* pChars = chars)
            {
                // Use GetByteCount(char*, int)
                Assert.Equal(expected, encoding.GetByteCount(pChars + index, count));
            }
        }

        private static void GetBytes(Encoding encoding, string source, int index, int count, byte[] expectedBytes)
        {
            byte[] fullArray = new byte[expectedBytes.Length + 4];
            for (int i = 0; i < fullArray.Length; i++)
            {
                fullArray[i] = (byte)i;
            }

            VerifyGetBytes(encoding, source, index, count, new byte[expectedBytes.Length], 0, expectedBytes);
            VerifyGetBytes(encoding, source, index, count, fullArray, 2, expectedBytes);

            if (count == 0)
            {
                // If count == 0, GetBytes should not throw even though byteIndex is invalid
                VerifyGetBytes(encoding, source, index, count, new byte[10], 10, expectedBytes);
            }
        }

        public static bool IsLatin1(this Encoding encoding)
        {
            return encoding != null
                && encoding.CodePage == 28591; // hardcoded Latin-1 code page
        }

        public static bool IsLatin1BestFitFallback(this EncoderFallback fallback)
        {
            // We rely on the type name because the type itself isn't public.

            return fallback != null
                && fallback.GetType().Name == "EncoderLatin1BestFitFallback";
        }

        private static unsafe void VerifyGetBytes(Encoding encoding, string source, int index, int count, byte[] bytes, int byteIndex, byte[] expectedBytes)
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

            // Use GetBytes(char*, int, byte*, int) - only works for non-null/non-empty char* or byte*
            if (expectedBytes.Length > 0)
            {
                byte[] charPointerBytes = (byte[])bytes.Clone();
                fixed (char* pChars = source.ToCharArray())
                fixed (byte* pBytes = charPointerBytes)
                {
                    int charPointerByteCount = encoding.GetBytes(pChars + index, count, pBytes + byteIndex, charPointerBytes.Length - byteIndex);
                    Assert.Equal(expectedBytes.Length, charPointerByteCount);
                }
                VerifyGetBytes(charPointerBytes, byteIndex, charArrayByteCount, originalBytes, expectedBytes);
            }
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

        public static void Decode(Encoding encoding, byte[] bytes, int index, int count, string expected)
        {
            GetCharCount(encoding, bytes, index, count, expected.Length);
            GetChars(encoding, bytes, index, count, expected.ToCharArray());
            GetString(encoding, bytes, index, count, expected);

            GetCharCount_NetCoreApp(encoding, bytes, index, count, expected.Length);
            GetString_NetCoreApp(encoding, bytes, index, count, expected);
        }

        private static unsafe void GetCharCount(Encoding encoding, byte[] bytes, int index, int count, int expected)
        {
            if (index == 0 && count == bytes.Length)
            {
                // Use GetCharCount(byte[])
                Assert.Equal(expected, encoding.GetCharCount(bytes));
            }
            // Use GetCharCount(byte[], int, int)
            Assert.Equal(expected, encoding.GetCharCount(bytes, index, count));

            // Use GetCharCount(byte*, int) - only works for non-null/non-empty byte*
            if (expected > 0)
            {
                fixed (byte* pBytes = bytes)
                {
                    Assert.Equal(expected, encoding.GetCharCount(pBytes + index, count));
                }
            }
        }

        private static void GetChars(Encoding encoding, byte[] bytes, int index, int count, char[] expectedChars)
        {
            char[] fullArray = new char[expectedChars.Length + 4];
            for (int i = 0; i < fullArray.Length; i++)
            {
                fullArray[i] = (char)i;
            }

            VerifyGetChars(encoding, bytes, index, count, new char[expectedChars.Length], 0, expectedChars);
            VerifyGetChars(encoding, bytes, index, count, fullArray, 2, expectedChars);

            if (count == 0)
            {
                // If count == 0, GetChars should not throw even though charIndex is invalid
                VerifyGetChars(encoding, bytes, index, count, new char[10], 10, expectedChars);
            }
        }

        private static unsafe void VerifyGetChars(Encoding encoding, byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, char[] expectedChars)
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
            char[] byteChars = (char[])chars.Clone();
            int charCount = encoding.GetChars(bytes, byteIndex, byteCount, byteChars, charIndex);
            VerifyGetChars(byteChars, charIndex, charCount, originalChars, expectedChars);
            Assert.Equal(expectedChars.Length, charCount);

            // Use GetCharCount(byte*, int, char*, int) - only works for non-null/non-empty byte* or char*
            if (expectedChars.Length > 0)
            {
                char[] bytePointerChars = (char[])chars.Clone();
                fixed (byte* pBytes = bytes)
                fixed (char* pChars = bytePointerChars)
                {
                    int charPointerCount = encoding.GetChars(pBytes + byteIndex, byteCount, pChars + charIndex, bytePointerChars.Length - charIndex);
                    Assert.Equal(expectedChars.Length, charPointerCount);
                }
                VerifyGetChars(bytePointerChars, charIndex, charCount, originalChars, expectedChars);
            }

            VerifyGetChars_NetCoreApp(encoding, bytes, byteIndex, byteCount, chars, charIndex, expectedChars);
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

        private static void GetString(Encoding encoding, byte[] bytes, int index, int count, string expected)
        {
            if (index == 0 && count == bytes.Length)
            {
                // Use GetString(byte[])
                Assert.Equal(expected, encoding.GetString(bytes));
            }
            // Use GetString(byte[], int, int)
            Assert.Equal(expected, encoding.GetString(bytes, index, count));
        }

        static void GetByteCount_NetCoreApp(Encoding encoding, string chars, int index, int count, int expected)
        {
            // Use GetByteCount(string, int, int)
            Assert.Equal(expected, encoding.GetByteCount(chars, index, count));

            // Use GetByteCount(ReadOnlySpan<char> chars)
            Assert.Equal(expected, encoding.GetByteCount(chars.AsSpan(index, count)));

            if (count == 0)
                Assert.Equal(expected, encoding.GetByteCount(ReadOnlySpan<char>.Empty));
        }

        static void GetBytes_NetCoreApp(Encoding encoding, string chars, int index, int count, byte[] expected)
        {
            // Use GetBytes(string, int, int)
            byte[] stringResultAdvanced = encoding.GetBytes(chars, index, count);
            VerifyGetBytes(stringResultAdvanced, 0, stringResultAdvanced.Length, new byte[expected.Length], expected);

            // Use GetBytes(ReadOnlySpan<char>, Span<byte>)
            Array.Clear(stringResultAdvanced);
            Assert.Equal(expected.Length, encoding.GetBytes(chars.AsSpan(index, count), (Span<byte>)stringResultAdvanced));
            VerifyGetBytes(stringResultAdvanced, 0, stringResultAdvanced.Length, new byte[expected.Length], expected);

            // Use TryGetBytes(ReadOnlySpan<char>, Span<byte>, out int bytesWritten)
            Array.Clear(stringResultAdvanced);
            Assert.True(encoding.TryGetBytes(chars.AsSpan(index, count), (Span<byte>)stringResultAdvanced, out int bytesWritten));
            Assert.Equal(expected.Length, bytesWritten);
            VerifyGetBytes(stringResultAdvanced, 0, stringResultAdvanced.Length, new byte[expected.Length], expected);

            if (count == 0)
                Assert.Equal(expected.Length, encoding.GetBytes(ReadOnlySpan<char>.Empty, (Span<byte>)stringResultAdvanced));
        }

        static void GetCharCount_NetCoreApp(Encoding encoding, byte[] bytes, int index, int count, int expected)
        {
            // Use GetCharCount(ReadOnlySpan<byte>)
            Assert.Equal(expected, encoding.GetCharCount(new ReadOnlySpan<byte>(bytes, index, count)));

            if (count == 0)
                Assert.Equal(expected, encoding.GetCharCount(ReadOnlySpan<byte>.Empty));
        }

        static void VerifyGetChars_NetCoreApp(Encoding encoding, byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, char[] expectedChars)
        {
            // Use GetChars(ReadOnlySpan<byte>, Span<char>)
            char[] byteChars = (char[])chars.Clone();
            int charCount = encoding.GetChars(new ReadOnlySpan<byte>(bytes, byteIndex, byteCount), new Span<char>(byteChars).Slice(charIndex));
            VerifyGetChars(byteChars, charIndex, charCount, (char[])chars.Clone(), expectedChars);
            Assert.Equal(expectedChars.Length, charCount);

            // Use TryGetChars(ReadOnlySpan<byte>, Span<char>, out int charsWritten)
            byteChars = (char[])chars.Clone();
            Assert.True(encoding.TryGetChars(new ReadOnlySpan<byte>(bytes, byteIndex, byteCount), new Span<char>(byteChars).Slice(charIndex), out charCount));
            VerifyGetChars(byteChars, charIndex, charCount, (char[])chars.Clone(), expectedChars);
            Assert.Equal(expectedChars.Length, charCount);

            if (byteCount == 0)
            {
                charCount = encoding.GetChars(ReadOnlySpan<byte>.Empty, new Span<char>(byteChars).Slice(charIndex));
                Assert.Equal(expectedChars.Length, charCount);
            }
        }

        static void GetString_NetCoreApp(Encoding encoding, byte[] bytes, int index, int count, string expected)
        {
            // Use GetString(ReadOnlySpan<byte>)
            Assert.Equal(expected, encoding.GetString(new ReadOnlySpan<byte>(bytes, index, count)));

            if (count == 0)
                Assert.Equal(expected, encoding.GetString(ReadOnlySpan<byte>.Empty));
        }
    }
}
