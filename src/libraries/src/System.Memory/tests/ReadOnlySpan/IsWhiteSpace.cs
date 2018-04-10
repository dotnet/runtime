// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        private static readonly char[] s_whiteSpaceCharacters = { '\u0009', '\u000a', '\u000b', '\u000c', '\u000d', '\u0020', '\u0085', '\u00a0', '\u1680' };

        [Fact]
        public static void ZeroLengthIsWhiteSpace()
        {
            var span = new ReadOnlySpan<char>(Array.Empty<char>());
            bool result = span.IsWhiteSpace();
            Assert.Equal(string.IsNullOrWhiteSpace(""), result);
        }

        [Fact]
        public static void IsWhiteSpaceTrueLatin1()
        {
            Random rand = new Random(42);
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = s_whiteSpaceCharacters[rand.Next(0, s_whiteSpaceCharacters.Length - 1)];
                }
                var span = new Span<char>(a);
                bool result = ((ReadOnlySpan<char>)span).IsWhiteSpace();
                Assert.Equal(string.IsNullOrWhiteSpace(new string(a)), result);

                for (int i = 0; i < s_whiteSpaceCharacters.Length - 1; i++)
                {
                    span.Fill(s_whiteSpaceCharacters[i]);
                    Assert.Equal(string.IsNullOrWhiteSpace(new string(span.ToArray())), ((ReadOnlySpan<char>)span).IsWhiteSpace());
                }
            }
        }

        [Fact]
        public static void IsWhiteSpaceTrue()
        {
            Random rand = new Random(42);
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = s_whiteSpaceCharacters[rand.Next(0, s_whiteSpaceCharacters.Length)];
                }
                var span = new ReadOnlySpan<char>(a);
                bool result = span.IsWhiteSpace();
                Assert.Equal(string.IsNullOrWhiteSpace(new string(span.ToArray())), result);
            }
        }

        [Fact]
        public static void IsWhiteSpaceFalse()
        {
            Random rand = new Random(42);
            for (int length = 1; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = s_whiteSpaceCharacters[rand.Next(0, s_whiteSpaceCharacters.Length)];
                }
                var span = new Span<char>(a);

                // first character is not a white-space character
                a[0] = 'a';
                bool result = ((ReadOnlySpan<char>)span).IsWhiteSpace();
                Assert.Equal(string.IsNullOrWhiteSpace(new string(span.ToArray())), result);
                a[0] = ' ';

                // last character is not a white-space character
                a[length - 1] = 'a';
                result = ((ReadOnlySpan<char>)span).IsWhiteSpace();
                Assert.Equal(string.IsNullOrWhiteSpace(new string(span.ToArray())), result);
                a[length - 1] = ' ';

                // character in the middle is not a white-space character
                a[length/2] = 'a';
                result = ((ReadOnlySpan<char>)span).IsWhiteSpace();
                Assert.Equal(string.IsNullOrWhiteSpace(new string(span.ToArray())), result);
                a[length/2] = ' ';

                // no character is a white-space character
                span.Fill('a');
                result = ((ReadOnlySpan<char>)span).IsWhiteSpace();
                Assert.Equal(string.IsNullOrWhiteSpace(new string(span.ToArray())), result);
            }
        }

        [Fact]
        public static void MakeSureNoIsWhiteSpaceChecksGoOutOfRange()
        {
            for (int length = 3; length < 64; length++)
            {
                char[] first = new char[length];
                first[0] = ' ';
                first[length - 1] = ' ';
                var span = new ReadOnlySpan<char>(first, 1, length - 2);
                bool result = span.IsWhiteSpace();
                Assert.Equal(string.IsNullOrWhiteSpace(new string(span.ToArray())), result);
            }
        }
    }
}
