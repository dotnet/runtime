// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public class Base64UrlUnicodeAPIsUnitTests
    {
        [Theory]
        [InlineData("", 0)]
        [InlineData("t", 2)]
        [InlineData("te", 3)]
        [InlineData("tes", 4)]
        [InlineData("test", 6)]
        [InlineData("test/", 7)]
        [InlineData("test/+", 8)]
        public static void DecodeEncodeToFromCharsStringRoundTrip(string str, int expectedWritten)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(str);
            Span<char> resultChars = new char[Base64Url.GetEncodedLength(inputBytes.Length)];
            OperationStatus operationStatus = Base64Url.EncodeToChars(inputBytes, resultChars, out int bytesConsumed, out int charsWritten);
            Assert.Equal(OperationStatus.Done, operationStatus);
            Assert.Equal(str.Length, bytesConsumed);
            Assert.Equal(expectedWritten, charsWritten);
            string result = Base64Url.EncodeToString(inputBytes);
            Assert.Equal(result, resultChars);
            Assert.Equal(expectedWritten, Base64Url.EncodeToChars(inputBytes, resultChars));
            Assert.True(Base64Url.TryEncodeToChars(inputBytes, resultChars, out charsWritten));
            Assert.Equal(expectedWritten, charsWritten);
            Assert.Equal(result, resultChars);

            Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(resultChars.Length)];
            operationStatus = Base64Url.DecodeFromChars(resultChars, decodedBytes, out bytesConsumed, out int bytesWritten);
            Assert.Equal(OperationStatus.Done, operationStatus);
            Assert.Equal(resultChars.Length, bytesConsumed);
            Assert.Equal(str.Length, bytesWritten);
            Assert.Equal(inputBytes, decodedBytes);
            Assert.Equal(str.Length, Base64Url.DecodeFromChars(resultChars, decodedBytes));
            Assert.True(Base64Url.TryDecodeFromChars(resultChars, decodedBytes, out bytesConsumed));
            Assert.Equal(str.Length, bytesConsumed);
            Assert.Equal(inputBytes, decodedBytes);
            Assert.Equal(str, Encoding.UTF8.GetString(decodedBytes));
        }

        [Fact]
        public void EncodingWithLargeSpan()
        {
            var rnd = new Random(42);
            for (int i = 0; i < 5; i++)
            {
                int numBytes = rnd.Next(100, 1000 * 1000);
                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeBytes(source, numBytes);

                Span<char> encodedBytes = new char[Base64Url.GetEncodedLength(source.Length)];
                OperationStatus result = Base64Url.EncodeToChars(source, encodedBytes, out int consumed, out int encodedBytesCount);
                Assert.Equal(OperationStatus.Done, result);
                Assert.Equal(source.Length, consumed);
                Assert.Equal(encodedBytes.Length, encodedBytesCount);
                string expectedText = Convert.ToBase64String(source).Replace('+', '-').Replace('/', '_').TrimEnd('=');
                Assert.Equal(expectedText, encodedBytes);
            }
        }

        [Fact]
        public void DecodeWithLargeSpan()
        {
            var rnd = new Random(42);
            for (int i = 0; i < 5; i++)
            {
                int numBytes;
                do
                {
                    numBytes = rnd.Next(100, 1000 * 1000);
                } while (numBytes % 4 == 1); // ensure we have a valid length

                Span<char> source = new char[numBytes];
                Base64TestHelper.InitializeUrlDecodableChars(source, numBytes);

                Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];
                Assert.Equal(OperationStatus.Done, Base64Url.DecodeFromChars(source, decodedBytes, out int consumed, out int decodedByteCount));
                Assert.Equal(source.Length, consumed);
                Assert.Equal(decodedBytes.Length, decodedByteCount);

                string sourceString = source.ToString();
                string padded = sourceString.Length % 4 == 0 ? sourceString :
                    sourceString.PadRight(sourceString.Length + (4 - sourceString.Length % 4), '=');
                string base64 = padded.Replace("_", "/").Replace("-", "+");
                byte[] expectedBytes = Convert.FromBase64String(base64);
                Assert.True(expectedBytes.AsSpan().SequenceEqual(decodedBytes.Slice(0, decodedByteCount)));
            }
        }

        [Fact]
        public void RoundTripWithLargeSpan()
        {
            var rnd = new Random(42);
            for (int i = 0; i < 5; i++)
            {
                int numBytes = rnd.Next(100, 1000 * 1000);
                Span<byte> source = new byte[numBytes];
                Base64TestHelper.InitializeBytes(source, numBytes);

                int expectedLength = Base64Url.GetEncodedLength(source.Length);
                char[] encodedBytes = Base64Url.EncodeToChars(source);
                Assert.Equal(expectedLength, encodedBytes.Length);
                Assert.Equal(new String(encodedBytes), Base64Url.EncodeToString(source));

                byte[] decoded = Base64Url.DecodeFromChars(encodedBytes);
                Assert.Equal(source, decoded);
            }
        }


        public static IEnumerable<object[]> EncodeToStringTests_TestData()
        {
            yield return new object[] { Enumerable.Range(0, 0).Select(i => (byte)i).ToArray(), "" };
            yield return new object[] { Enumerable.Range(0, 1).Select(i => (byte)i).ToArray(), "AA" };
            yield return new object[] { Enumerable.Range(0, 2).Select(i => (byte)i).ToArray(), "AAE" };
            yield return new object[] { Enumerable.Range(0, 3).Select(i => (byte)i).ToArray(), "AAEC" };
            yield return new object[] { Enumerable.Range(0, 4).Select(i => (byte)i).ToArray(), "AAECAw" };
            yield return new object[] { Enumerable.Range(0, 5).Select(i => (byte)i).ToArray(), "AAECAwQ" };
            yield return new object[] { Enumerable.Range(0, 6).Select(i => (byte)i).ToArray(), "AAECAwQF" };
            yield return new object[] { Enumerable.Range(0, 7).Select(i => (byte)i).ToArray(), "AAECAwQFBg" };
            yield return new object[] { Enumerable.Range(0, 8).Select(i => (byte)i).ToArray(), "AAECAwQFBgc" };
            yield return new object[] { Enumerable.Range(0, 9).Select(i => (byte)i).ToArray(), "AAECAwQFBgcI" };
            yield return new object[] { Enumerable.Range(0, 10).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQ" };
            yield return new object[] { Enumerable.Range(0, 11).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQo" };
            yield return new object[] { Enumerable.Range(0, 12).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoL" };
            yield return new object[] { Enumerable.Range(0, 13).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA" };
            yield return new object[] { Enumerable.Range(0, 14).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0" };
            yield return new object[] { Enumerable.Range(0, 15).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0O" };
            yield return new object[] { Enumerable.Range(0, 16).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODw" };
            yield return new object[] { Enumerable.Range(0, 17).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxA" };
            yield return new object[] { Enumerable.Range(0, 18).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAR" };
            yield return new object[] { Enumerable.Range(0, 19).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREg" };
            yield return new object[] { Enumerable.Range(0, 20).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhM" };
            yield return new object[] { Enumerable.Range(0, 21).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMU" };
            yield return new object[] { Enumerable.Range(0, 22).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFQ" };
            yield return new object[] { Enumerable.Range(0, 23).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRY" };
            yield return new object[] { Enumerable.Range(0, 24).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYX" };
            yield return new object[] { Enumerable.Range(0, 25).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGA" };
            yield return new object[] { Enumerable.Range(0, 26).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBk" };
            yield return new object[] { Enumerable.Range(0, 27).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBka" };
            yield return new object[] { Enumerable.Range(0, 28).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGw" };
            yield return new object[] { Enumerable.Range(0, 29).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxw" };
            yield return new object[] { Enumerable.Range(0, 30).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwd" };
            yield return new object[] { Enumerable.Range(0, 31).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHg" };
            yield return new object[] { Enumerable.Range(0, 32).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8" };
            yield return new object[] { Enumerable.Range(0, 33).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8g" };
            yield return new object[] { Enumerable.Range(0, 34).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gIQ" };
            yield return new object[] { Enumerable.Range(0, 35).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISI" };
            yield return new object[] { Enumerable.Range(0, 36).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIj" };
            yield return new object[] { Enumerable.Range(0, 37).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJA" };
            yield return new object[] { Enumerable.Range(0, 38).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCU" };
            yield return new object[] { Enumerable.Range(0, 39).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUm" };
            yield return new object[] { Enumerable.Range(0, 40).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJw" };
            yield return new object[] { Enumerable.Range(0, 41).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJyg" };
            yield return new object[] { Enumerable.Range(0, 42).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygp" };
            yield return new object[] { Enumerable.Range(0, 43).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKg" };
            yield return new object[] { Enumerable.Range(0, 44).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKis" };
            yield return new object[] { Enumerable.Range(0, 45).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKiss" };
            yield return new object[] { Enumerable.Range(0, 46).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLQ" };
            yield return new object[] { Enumerable.Range(0, 47).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4" };
            yield return new object[] { Enumerable.Range(0, 48).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4v" };
            yield return new object[] { Enumerable.Range(0, 49).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMA" };
            yield return new object[] { Enumerable.Range(0, 50).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDE" };
            yield return new object[] { Enumerable.Range(0, 51).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEy" };
            yield return new object[] { Enumerable.Range(0, 52).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMw" };
            yield return new object[] { Enumerable.Range(0, 53).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ" };
            yield return new object[] { Enumerable.Range(0, 54).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1" };
            yield return new object[] { Enumerable.Range(0, 55).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Ng" };
            yield return new object[] { Enumerable.Range(0, 56).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc" };
            yield return new object[] { Enumerable.Range(0, 57).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4" };
            yield return new object[] { Enumerable.Range(0, 58).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OQ" };
            yield return new object[] { Enumerable.Range(0, 59).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo" };
            yield return new object[] { Enumerable.Range(0, 60).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7" };
            yield return new object[] { Enumerable.Range(0, 61).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PA" };
            yield return new object[] { Enumerable.Range(0, 62).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0" };
            yield return new object[] { Enumerable.Range(0, 63).Select(i => (byte)i).ToArray(), "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0-" };
            yield return new object[] { Encoding.Unicode.GetBytes("aaaabbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccdd"), "YQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAA" };
            yield return new object[] { Encoding.Unicode.GetBytes("vbnmbbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccddx"), "dgBiAG4AbQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAHgA" };
            yield return new object[] { Encoding.Unicode.GetBytes("rrrrbbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccdd\0"), "cgByAHIAcgBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAAAA" };
            yield return new object[] { Encoding.Unicode.GetBytes("uuuubbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccdd\0feffe"), "dQB1AHUAdQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAAAAZgBlAGYAZgBlAA" };
            yield return new object[] { Encoding.Unicode.GetBytes("kkkkkbbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccddx\u043F\u0440\u0438\u0432\u0435\u0442\u043C\u0438\u0440\u4F60\u597D\u4E16\u754C"), "awBrAGsAawBrAGIAYgBiAGIAYwBjAGMAYwBkAGQAZABkAGQAZABkAGUAZQBlAGUAZQBhAGEAYQBhAGIAYgBiAGIAYwBjAGMAYwBkAGQAZABkAGQAZABkAGUAZQBlAGUAZQBhAGEAYQBhAGIAYgBiAGIAYwBjAGMAYwBkAGQAeAA_BEAEOAQyBDUEQgQ8BDgEQARgT31ZFk5MdQ" };
            yield return new object[] { Encoding.Unicode.GetBytes(",,,,bbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccddx\u043F\u0440\u0438\u0432\u0435\u0442\u043C\u0438\u0440\u4F60\u597D\u4E16\u754Cddddeeeeea"), "LAAsACwALABiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAHgAPwRABDgEMgQ1BEIEPAQ4BEAEYE99WRZOTHVkAGQAZABkAGUAZQBlAGUAZQBhAA" };
            yield return new object[] { Encoding.Unicode.GetBytes("____bbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccddaaaabbbbccccdddddddeeeeeaaaabbbbccccdcccd"), "XwBfAF8AXwBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGEAYQBhAGEAYgBiAGIAYgBjAGMAYwBjAGQAZABkAGQAZABkAGQAZQBlAGUAZQBlAGEAYQBhAGEAYgBiAGIAYgBjAGMAYwBjAGQAYwBjAGMAZAA" };
            yield return new object[] { Encoding.Unicode.GetBytes("    bbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccddaaaabbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccd"), "IAAgACAAIABiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGEAYQBhAGEAYgBiAGIAYgBjAGMAYwBjAGQAZABkAGQAZABkAGQAZQBlAGUAZQBlAGEAYQBhAGEAYgBiAGIAYgBjAGMAYwBjAGQAZABkAGQAZABkAGQAZQBlAGUAZQBlAGEAYQBhAGEAYgBiAGIAYgBjAGMAYwBjAGQA" };
            yield return new object[] { Encoding.Unicode.GetBytes("\0\0bbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccddaaaabbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccddx"), "AAAAAGIAYgBiAGIAYwBjAGMAYwBkAGQAZABkAGQAZABkAGUAZQBlAGUAZQBhAGEAYQBhAGIAYgBiAGIAYwBjAGMAYwBkAGQAZABkAGQAZABkAGUAZQBlAGUAZQBhAGEAYQBhAGIAYgBiAGIAYwBjAGMAYwBkAGQAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAHgA" };
            yield return new object[] { Encoding.Unicode.GetBytes("eeeebbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccdgggdaaaabbbbccccdddddddeeeeeaaaabbbbccccdddddddeeeeeaaaabbbbccccddx"), "ZQBlAGUAZQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABkAGQAZABkAGQAZABlAGUAZQBlAGUAYQBhAGEAYQBiAGIAYgBiAGMAYwBjAGMAZABnAGcAZwBkAGEAYQBhAGEAYgBiAGIAYgBjAGMAYwBjAGQAZABkAGQAZABkAGQAZQBlAGUAZQBlAGEAYQBhAGEAYgBiAGIAYgBjAGMAYwBjAGQAZABkAGQAZABkAGQAZQBlAGUAZQBlAGEAYQBhAGEAYgBiAGIAYgBjAGMAYwBjAGQAZAB4AA" };
        }

        [Theory]
        [InlineData("\u5948cz_T", 0, 0)]                                              // scalar code-path
        [InlineData("z_Ta123\u5948", 4, 3)]
        [InlineData("\u5948z_T-H7sqEkerqMweH1uSw==", 0, 0)]                          // Vector128 code-path
        [InlineData("z_T-H7sqEkerqMweH1uSw\u5948==", 20, 15)]
        [InlineData("\u5948z_T-H7sqEkerqMweH1uSw1a5ebaAF9xa8B0ze1wet4epo==", 0, 0)]  // Vector256 / AVX code-path
        [InlineData("z_T-H7sqEkerqMweH1uSw1a5ebaAF9xa8B0ze1wet4epo\u5948==", 44, 33)]
        [InlineData("\u5948z_T-H7sqEkerqMweH1uSw1a5ebaAF9xa8B0ze1wet4epo01234567890123456789012345678901234567890123456789==", 0, 0)]  // Vector512 / Avx512Vbmi code-path
        [InlineData("z_T-H7sqEkerqMweH1uSw1a5ebaAF9xa8B0ze1wet4epo01234567890123456789012345678901234567890123456789\u5948==", 92, 69)]
        public void BasicDecodingNonAsciiInputInvalid(string inputString, int expectedConsumed, int expectedWritten)
        {
            Span<char> source = inputString.ToArray();
            Span<byte> decodedBytes = new byte[Base64Url.GetMaxDecodedLength(source.Length)];

            Assert.Equal(OperationStatus.InvalidData, Base64Url.DecodeFromChars(source, decodedBytes, out int consumed, out int decodedByteCount));
            Assert.Equal(expectedConsumed, consumed);
            Assert.Equal(expectedWritten, decodedByteCount);
        }


        [Theory]
        [MemberData(nameof(EncodeToStringTests_TestData))]
        public static void EncodeToStringTests(byte[] inputBytes, string expectedBase64)
        {
            Assert.Equal(expectedBase64, Base64Url.EncodeToString(inputBytes));
            Span<char> chars = new char[Base64Url.GetEncodedLength(inputBytes.Length)];
            Assert.Equal(OperationStatus.Done, Base64Url.EncodeToChars(inputBytes, chars, out int _, out int charsWritten));
            Assert.Equal(expectedBase64, chars.Slice(0, charsWritten));
        }

        [Fact]
        public void EncodingOutputTooSmall()
        {
            for (int numBytes = 4; numBytes < 20; numBytes++)
            {
                byte[] source = new byte[numBytes];
                Base64TestHelper.InitializeBytes(source, numBytes);
                int expectedConsumed = 3;
                char[] encodedBytes = new char[4];

                Assert.Equal(OperationStatus.DestinationTooSmall, Base64Url.EncodeToChars(source, encodedBytes, out int consumed, out int written));
                Assert.Equal(expectedConsumed, consumed);
                Assert.Equal(encodedBytes.Length, written);
                Assert.True(source.AsSpan().Slice(0, consumed).SequenceEqual(Base64Url.DecodeFromChars(encodedBytes)));

                Assert.Throws<ArgumentException>("destination", () => Base64Url.EncodeToChars(source, encodedBytes));
            }
        }

        [Fact]
        public static void Roundtrip()
        {
            string input = "test";
            Verify(input, result =>
            {
                Assert.Equal(3, result.Length);

                uint triplet = (uint)((result[0] << 16) | (result[1] << 8) | result[2]);
                Assert.Equal<uint>(45, triplet >> 18); // 't'
                Assert.Equal<uint>(30, (triplet << 14) >> 26); // 'e'
                Assert.Equal<uint>(44, (triplet << 20) >> 26); // 's'
                Assert.Equal<uint>(45, (triplet << 26) >> 26); // 't'

                Assert.Equal(input, Base64Url.EncodeToString(result));
            });
        }

        [Fact]
        public static void PartialRoundtripWithoutPadding()
        {
            string input = "ab";
            Verify(input, result =>
            {
                Assert.Equal(1, result.Length);

                string roundtrippedString = Base64Url.EncodeToString(result);
                Assert.NotEqual(input, roundtrippedString);
                Assert.Equal(input[0], roundtrippedString[0]);
            });
        }

        [Fact]
        public static void PartialRoundtripWithPadding2()
        {
            string input = "ab==";
            Verify(input, result =>
            {
                Assert.Equal(1, result.Length);

                string roundtrippedString = Base64Url.EncodeToString(result);
                Assert.NotEqual(input, roundtrippedString);
                Assert.Equal(input[0], roundtrippedString[0]);
            });
        }

        [Fact]
        public static void PartialRoundtripWithPadding1()
        {
            string input = "789=";
            Verify(input, result =>
            {
                Assert.Equal(2, result.Length);

                string roundtrippedString = Base64Url.EncodeToString(result);
                Assert.NotEqual(input, roundtrippedString);
                Assert.Equal(input[0], roundtrippedString[0]);
                Assert.Equal(input[1], roundtrippedString[1]);
            });
        }

        [Fact]
        public static void ParseWithWhitespace()
        {
            Verify("abc= \t \r\n =");
        }

        [Fact]
        public static void RoundtripWithWhitespace2()
        {
            string input = "abc=  \t\n\t\r ";
            VerifyRoundtrip(input, "abc");
        }

        [Fact]
        public static void RoundtripWithWhitespace3()
        {
            string input = " \r\n\t abc  =  \t\n\t\r ";
            VerifyRoundtrip(input, "abc");
        }

        [Fact]
        public static void RoundtripWithWhitespace4()
        {
            string expected = "test";
            string input = expected.Insert(1, new string(' ', 17)).PadLeft(31, ' ').PadRight(12, ' ');
            VerifyRoundtrip(input, expected, expectedLengthBytes: 3);
        }

        [Fact]
        public static void RoundtripLargeString()
        {
            string input = new string('a', 10000);
            VerifyRoundtrip(input, input);
        }

        [Fact]
        public static void InvalidInput()
        {
            // Input must not contain invalid characters
            VerifyInvalidInput("2+34");
            VerifyInvalidInput("23/4");

            // Input must not contain 3 or more padding characters in a row
            VerifyInvalidInput("a===");
            VerifyInvalidInput("abc=====");
            VerifyInvalidInput("a===\r  \t  \n");

            // Input must not contain padding characters in the middle of the string
            VerifyInvalidInput("No=n");
            VerifyInvalidInput("abcd====abcd");

            // Input must not contain extra trailing padding characters
            VerifyInvalidInput("=");
            VerifyInvalidInput("abc===");
        }

        [Fact]
        public static void ExtraPaddingCharacter()
        {
            VerifyInvalidInput("abcdxyz=" + "=");
        }

        [Fact]
        public static void InvalidCharactersInInput()
        {
            ushort[] invalidChars = { 30122, 62608, 13917, 19498, 2473, 40845, 35988, 2281, 51246, 36372 };

            foreach (char ch in invalidChars)
            {
                var builder = new StringBuilder("abc");
                builder.Insert(1, ch);
                VerifyInvalidInput(builder.ToString());
            }
        }

        private static void VerifyRoundtrip(string input, string expected = null, int? expectedLengthBytes = null)
        {
            if (expected == null)
            {
                expected = input;
            }

            Verify(input, result =>
            {
                if (expectedLengthBytes.HasValue)
                {
                    Assert.Equal(expectedLengthBytes.Value, result.Length);
                }
                Assert.Equal(expected, Base64Url.EncodeToString(result));
            });
        }

        private static void VerifyInvalidInput(string input)
        {
            char[] inputChars = input.ToCharArray();

            Assert.Throws<FormatException>(() => Base64Url.DecodeFromChars(input));
        }

        private static void Verify(string input, Action<byte[]> action = null)
        {
            if (action != null)
            {
                action(Base64Url.DecodeFromChars(input));
            }
        }

        [Fact]
        public static void Base64_AllMethodsRoundtripConsistently()
        {
            var r = new Random(42);
            for (int length = 0; length < 128; length++)
            {
                var original = new byte[length];
                r.NextBytes(original);

                string encodedString = Base64Url.EncodeToString(original);

                char[] encodedArray = new char[encodedString.Length];
                Assert.Equal(OperationStatus.Done, Base64Url.EncodeToChars(original, encodedArray, out _, out int charsWritten));
                Assert.Equal(encodedArray.Length, charsWritten);
                AssertExtensions.SequenceEqual<char>(encodedString, encodedArray);

                char[] encodedSpan = new char[encodedString.Length];
                Assert.True(Base64Url.TryEncodeToChars(original, encodedSpan, out charsWritten));
                Assert.Equal(encodedSpan.Length, charsWritten);
                AssertExtensions.SequenceEqual<char>(encodedString, encodedSpan);

                AssertExtensions.SequenceEqual(original, Base64Url.DecodeFromChars(encodedString));
                Span<byte> decodedBytes = new byte[original.Length];
                int decoded = Base64Url.DecodeFromChars(encodedArray, decodedBytes);
                Assert.Equal(original.Length, decoded);
                AssertExtensions.SequenceEqual(original, decodedBytes);

                byte[] actualBytes = new byte[original.Length];
                Assert.True(Base64Url.TryDecodeFromChars(encodedSpan, actualBytes, out int bytesWritten));
                Assert.Equal(original.Length, bytesWritten);
                AssertExtensions.SequenceEqual(original, actualBytes);
            }
        }

        [Theory]
        [MemberData(nameof(Base64TestData))]
        public static void TryDecodeFromChars(string encodedAsString, byte[] expected)
        {
            char[] encoded = encodedAsString.ToCharArray();
            if (expected == null)
            {
                byte[] actual = new byte[Base64Url.GetMaxDecodedLength(encodedAsString.Length)];
                Assert.Throws<FormatException>(() => Base64Url.TryDecodeFromChars(encoded, actual, out _));
            }
            else
            {
                // Destination buffer size enough
                {
                    Span<byte> actual = new byte[Base64Url.GetMaxDecodedLength(encodedAsString.Length)];
                    Assert.True(Base64Url.TryDecodeFromChars(encoded, actual, out int bytesWritten));
                    Assert.Equal(expected, actual.Slice(0, bytesWritten));
                    Assert.Equal(expected.Length, bytesWritten);
                }

                // Buffer too short
                if (expected.Length != 0)
                {
                    byte[] actual = new byte[expected.Length - 1];
                    Assert.False(Base64Url.TryDecodeFromChars(encoded, actual, out int bytesWritten));
                    Assert.Equal(0, bytesWritten);
                }
            }
        }

        public static IEnumerable<object[]> Base64TestData
        {
            get
            {
                foreach ((string bse64UrlString, byte[] expectedArray) tuple in Base64TestDataSeed)
                {
                    yield return new object[] { tuple.bse64UrlString, tuple.expectedArray };
                    yield return new object[] { InsertSpaces(tuple.bse64UrlString, 1), tuple.expectedArray };
                    yield return new object[] { InsertSpaces(tuple.bse64UrlString, 4), tuple.expectedArray };
                }
            }
        }

        public static IEnumerable<(string, byte[])> Base64TestDataSeed
        {
            get
            {
                // Empty
                yield return ("", Array.Empty<byte>());

                // All whitespace characters.
                yield return (" \t\r\n", Array.Empty<byte>());

                // Invalid Input length
                yield return ("A", null);

                // Cannot continue past end pad
                yield return ("AAA=BBBB", null);
                yield return ("AA==BBBB", null);

                // Cannot have more than two end pads
                yield return ("A===", null);
                yield return ("====", null);

                // Verify negative entries of charmap.
                for (int i = 0; i < 256; i++)
                {
                    char c = (char)i;
                    if (!IsValidBase64Char(c))
                    {
                        string text = new string(c, 1) + "AAA";
                        yield return (text, null);
                    }
                }

                // Verify >255 character handling.
                string largerThanByte = new string((char)256, 1);
                yield return (largerThanByte + "AAA", null);
                yield return ("A" + largerThanByte + "AA", null);
                yield return ("AA" + largerThanByte + "A", null);
                yield return ("AAA" + largerThanByte, null);
                yield return ("AAAA" + largerThanByte + "AAA", null);
                yield return ("AAAA" + "A" + largerThanByte + "AA", null);
                yield return ("AAAA" + "AA" + largerThanByte + "A", null);
                yield return ("AAAA" + "AAA" + largerThanByte, null);

                // Verify positive entries of charmap.
                yield return ("-A==", new byte[] { 0xf8 });
                yield return ("_A=", new byte[] { 0xfc });
                yield return ("0A==", new byte[] { 0xd0 });
                yield return ("1A==", new byte[] { 0xd4 });
                yield return ("2A==", new byte[] { 0xd8 });
                yield return ("3A==", new byte[] { 0xdc });
                yield return ("4A", new byte[] { 0xe0 });
                yield return ("5A=", new byte[] { 0xe4 });
                yield return ("6A==", new byte[] { 0xe8 });
                yield return ("7A==", new byte[] { 0xec });
                yield return ("8A", new byte[] { 0xf0 });
                yield return ("9A=", new byte[] { 0xf4 });
                yield return ("AA=", new byte[] { 0x00 });
                yield return ("BA", new byte[] { 0x04 });
                yield return ("CA", new byte[] { 0x08 });
                yield return ("DA==", new byte[] { 0x0c });
                yield return ("EA==", new byte[] { 0x10 });
                yield return ("FA==", new byte[] { 0x14 });
                yield return ("GA==", new byte[] { 0x18 });
                yield return ("HA==", new byte[] { 0x1c });
                yield return ("IA==", new byte[] { 0x20 });
                yield return ("JA==", new byte[] { 0x24 });
                yield return ("KA==", new byte[] { 0x28 });
                yield return ("LA==", new byte[] { 0x2c });
                yield return ("MA==", new byte[] { 0x30 });
                yield return ("NA==", new byte[] { 0x34 });
                yield return ("OA==", new byte[] { 0x38 });
                yield return ("PA==", new byte[] { 0x3c });
                yield return ("QA==", new byte[] { 0x40 });
                yield return ("RA==", new byte[] { 0x44 });
                yield return ("SA==", new byte[] { 0x48 });
                yield return ("TA==", new byte[] { 0x4c });
                yield return ("UA==", new byte[] { 0x50 });
                yield return ("VA==", new byte[] { 0x54 });
                yield return ("WA==", new byte[] { 0x58 });
                yield return ("XA==", new byte[] { 0x5c });
                yield return ("YA==", new byte[] { 0x60 });
                yield return ("ZA==", new byte[] { 0x64 });
                yield return ("aA==", new byte[] { 0x68 });
                yield return ("bA==", new byte[] { 0x6c });
                yield return ("cA==", new byte[] { 0x70 });
                yield return ("dA==", new byte[] { 0x74 });
                yield return ("eA==", new byte[] { 0x78 });
                yield return ("fA==", new byte[] { 0x7c });
                yield return ("gA==", new byte[] { 0x80 });
                yield return ("hA==", new byte[] { 0x84 });
                yield return ("iA==", new byte[] { 0x88 });
                yield return ("jA==", new byte[] { 0x8c });
                yield return ("kA==", new byte[] { 0x90 });
                yield return ("lA==", new byte[] { 0x94 });
                yield return ("mA==", new byte[] { 0x98 });
                yield return ("nA==", new byte[] { 0x9c });
                yield return ("oA==", new byte[] { 0xa0 });
                yield return ("pA==", new byte[] { 0xa4 });
                yield return ("qA==", new byte[] { 0xa8 });
                yield return ("rA==", new byte[] { 0xac });
                yield return ("sA==", new byte[] { 0xb0 });
                yield return ("tA==", new byte[] { 0xb4 });
                yield return ("uA==", new byte[] { 0xb8 });
                yield return ("vA==", new byte[] { 0xbc });
                yield return ("wA==", new byte[] { 0xc0 });
                yield return ("xA==", new byte[] { 0xc4 });
                yield return ("yA==", new byte[] { 0xc8 });
                yield return ("zA==", new byte[] { 0xcc });
            }
        }

        private static string InsertSpaces(string text, int period)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                if ((i % period) == 0)
                {
                    sb.Append("  ");
                }
                sb.Append(text[i]);
            }
            sb.Append("  ");
            return sb.ToString();
        }

        private static bool IsValidBase64Char(char c)
        {
            return char.IsAsciiLetterOrDigit(c) || c is '-' or '_' || char.IsWhiteSpace(c);
        }
    }
}
