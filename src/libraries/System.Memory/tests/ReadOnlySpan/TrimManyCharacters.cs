// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ZeroLengthTrimCharacters()
        {
            ReadOnlySpan<char> span = new ReadOnlySpan<char>(Array.Empty<char>());
            ReadOnlySpan<char> trimChars = new ReadOnlySpan<char>(Array.Empty<char>());
            Assert.True(span.SequenceEqual(span.Trim(trimChars)));
            Assert.True(span.SequenceEqual(span.TrimStart(trimChars)));
            Assert.True(span.SequenceEqual(span.TrimEnd(trimChars)));

            char[] chars = { 'a', 'b', 'c', 'd', 'e' };
            trimChars = new ReadOnlySpan<char>(chars);
            Assert.True(span.SequenceEqual(span.Trim(trimChars)));
            Assert.True(span.SequenceEqual(span.TrimStart(trimChars)));
            Assert.True(span.SequenceEqual(span.TrimEnd(trimChars)));

            ReadOnlySpan<char> stringSpan = "".AsSpan();
            ReadOnlySpan<char> trimCharsFromString = "abcde".AsSpan();
            Assert.True(stringSpan.SequenceEqual(stringSpan.Trim(trimCharsFromString)));
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimStart(trimCharsFromString)));
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimEnd(trimCharsFromString)));
        }

        [Fact]
        public static void NoTrimCharacters()
        {
            ReadOnlySpan<char> trimChars = new ReadOnlySpan<char>(Array.Empty<char>());
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'f';
                }
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(span.SequenceEqual(span.Trim(trimChars)));
                Assert.True(span.SequenceEqual(span.TrimStart(trimChars)));
                Assert.True(span.SequenceEqual(span.TrimEnd(trimChars)));
            }

            char[] chars = { 'a', 'b', 'c', 'd', 'e' };
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'f';
                }
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(span.SequenceEqual(span.Trim(chars)));
                Assert.True(span.SequenceEqual(span.TrimStart(chars)));
                Assert.True(span.SequenceEqual(span.TrimEnd(chars)));
            }

            ReadOnlySpan<char> stringSpan = "ffghifhig".AsSpan();
            ReadOnlySpan<char> trimCharsFromString = "abcde".AsSpan();
            Assert.True(stringSpan.SequenceEqual(stringSpan.Trim(trimCharsFromString)));
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimStart(trimCharsFromString)));
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimEnd(trimCharsFromString)));
        }

        [Fact]
        public static void OnlyTrimCharacters()
        {
            char[] chars = { 'a', 'b', 'c', 'd', 'e' };
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = chars[i % chars.Length];
                }
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(span.Trim(chars)), "G: " + length);
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(span.TrimStart(chars)), "H: " + length);
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(span.TrimEnd(chars)), "I: " + length);
            }

            ReadOnlySpan<char> stringSpan = "babedebcabba".AsSpan();
            ReadOnlySpan<char> trimChars = "abcde".AsSpan();
            Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(stringSpan.Trim(trimChars)), "J");
            Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(stringSpan.TrimStart(trimChars)), "K");
            Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(stringSpan.TrimEnd(trimChars)), "L");
        }

        [Fact]
        public static void TrimCharactersAtStart()
        {
            char[] chars = { 'a', 'b', 'c', 'd', 'e' };
            for (int length = 2; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                     a[i] = 'f';
                }
                a[0] = 'c';
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(span.Slice(1).SequenceEqual(span.Trim(chars)), "A: " + length);
                Assert.True(span.Slice(1).SequenceEqual(span.TrimStart(chars)), "B: " + length);
                Assert.True(span.SequenceEqual(span.TrimEnd(chars)), "C: " + length);
            }

            ReadOnlySpan<char> stringSpan = "babffffff".AsSpan();
            ReadOnlySpan<char> trimChars = "abcde".AsSpan();
            Assert.True(stringSpan.Slice(3).SequenceEqual(stringSpan.Trim(trimChars)), "D");
            Assert.True(stringSpan.Slice(3).SequenceEqual(stringSpan.TrimStart(trimChars)), "E");
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimEnd(trimChars)), "F");
        }

        [Fact]
        public static void TrimCharactersAtEnd()
        {
            char[] chars = { 'a', 'b', 'c', 'd', 'e' };
            for (int length = 2; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'f';
                }
                a[length - 1] = 'c';
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.Trim(chars)));
                Assert.True(span.SequenceEqual(span.TrimStart(chars)));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.TrimEnd(chars)));
            }

            ReadOnlySpan<char> stringSpan = "fffffcced".AsSpan();
            ReadOnlySpan<char> trimChars = "abcde".AsSpan();
            Assert.True(stringSpan.Slice(0, 5).SequenceEqual(stringSpan.Trim(trimChars)));
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimStart(trimChars)));
            Assert.True(stringSpan.Slice(0, 5).SequenceEqual(stringSpan.TrimEnd(trimChars)));
        }

        [Fact]
        public static void TrimCharactersAtStartAndEnd()
        {
            char[] chars = { 'a', 'b', 'c', 'd', 'e' };
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'f';
                }
                a[0] = 'c';
                a[length - 1] = 'c';
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(span.Slice(1, length - 2).SequenceEqual(span.Trim(chars)));
                Assert.True(span.Slice(1).SequenceEqual(span.TrimStart(chars)));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.TrimEnd(chars)));
            }

            ReadOnlySpan<char> stringSpan = "ccedafffffbdaa".AsSpan();
            ReadOnlySpan<char> trimChars = "abcde".AsSpan();
            Assert.True(stringSpan.Slice(5, 5).SequenceEqual(stringSpan.Trim(trimChars)));
            Assert.True(stringSpan.Slice(5).SequenceEqual(stringSpan.TrimStart(trimChars)));
            Assert.True(stringSpan.Slice(0, 10).SequenceEqual(stringSpan.TrimEnd(trimChars)));
        }

        [Fact]
        public static void TrimCharactersInMiddle()
        {
            char[] chars = { 'a', 'b', 'c', 'd', 'e' };
            for (int length = chars.Length + 2; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'f';
                }
                Array.Copy(chars, 0, a, 1, chars.Length);
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(span.SequenceEqual(span.Trim(chars)));
                Assert.True(span.SequenceEqual(span.TrimStart(chars)));
                Assert.True(span.SequenceEqual(span.TrimEnd(chars)));
            }

            ReadOnlySpan<char> stringSpan = "fabbacddeeddef".AsSpan();
            ReadOnlySpan<char> trimChars = "abcde".AsSpan();
            Assert.True(stringSpan.SequenceEqual(stringSpan.Trim(trimChars)));
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimStart(trimChars)));
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimEnd(trimChars)));
        }

        [Fact]
        public static void TrimCharactersMultipleTimes()
        {
            char[] chars = { 'a', 'b', 'c', 'd', 'e' };
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'f';
                }
                a[0] = 'c';
                a[length - 1] = 'c';
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                ReadOnlySpan<char> trimResult = span.Trim(chars);
                ReadOnlySpan<char> trimStartResult = span.TrimStart(chars);
                ReadOnlySpan<char> trimEndResult = span.TrimEnd(chars);
                Assert.True(span.Slice(1, length - 2).SequenceEqual(trimResult));
                Assert.True(span.Slice(1).SequenceEqual(trimStartResult));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(trimEndResult));

                // 2nd attempt should do nothing
                Assert.True(trimResult.SequenceEqual(trimResult.Trim(chars)));
                Assert.True(trimStartResult.SequenceEqual(trimStartResult.TrimStart(chars)));
                Assert.True(trimEndResult.SequenceEqual(trimEndResult.TrimEnd(chars)));
            }

            ReadOnlySpan<char> stringSpan = "ccedafffffbdaa".AsSpan();
            ReadOnlySpan<char> trimChars = "abcde".AsSpan();

            ReadOnlySpan<char> trimStringResult = stringSpan.Trim(trimChars);
            ReadOnlySpan<char> trimStartStringResult = stringSpan.TrimStart(trimChars);
            ReadOnlySpan<char> trimEndStringResult = stringSpan.TrimEnd(trimChars);
            Assert.True(stringSpan.Slice(5, 5).SequenceEqual(trimStringResult));
            Assert.True(stringSpan.Slice(5).SequenceEqual(trimStartStringResult));
            Assert.True(stringSpan.Slice(0, 10).SequenceEqual(trimEndStringResult));

            // 2nd attempt should do nothing
            Assert.True(trimStringResult.SequenceEqual(trimStringResult.Trim(trimChars)));
            Assert.True(trimStartStringResult.SequenceEqual(trimStartStringResult.TrimStart(trimChars)));
            Assert.True(trimEndStringResult.SequenceEqual(trimEndStringResult.TrimEnd(trimChars)));
        }

        [Fact]
        public static void MakeSureNoTrimCharactersChecksGoOutOfRange()
        {
            char[] chars = { 'a', 'b', 'c', 'd', 'e' };
            for (int length = 3; length < 64; length++)
            {
                char[] first = new char[length];
                first[0] = 'f';
                first[length - 1] = 'f';
                var span = new ReadOnlySpan<char>(first, 1, length - 2);
                Assert.Equal(span.ToArray().Length, span.Trim(chars).ToArray().Length);
                Assert.True(span.SequenceEqual(span.Trim(chars)), "A : " + span.Length);
                Assert.True(span.SequenceEqual(span.TrimStart(chars)), "B :" + span.Length);
                Assert.True(span.SequenceEqual(span.TrimEnd(chars)));
            }

            string testString = "afghijklmnopqrstfe";
            ReadOnlySpan<char> stringSpan = testString.AsSpan(1, testString.Length - 2);
            ReadOnlySpan<char> trimChars = "abcde".AsSpan();
            Assert.True(stringSpan.SequenceEqual(stringSpan.Trim(trimChars)));
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimStart(trimChars)));
            Assert.True(stringSpan.SequenceEqual(stringSpan.TrimEnd(trimChars)));
        }
    }
}
