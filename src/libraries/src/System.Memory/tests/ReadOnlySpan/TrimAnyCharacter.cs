// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ZeroLengthTrimCharacter()
        {
            ReadOnlySpan<char> span = new ReadOnlySpan<char>(Array.Empty<char>());
            Assert.True(span.SequenceEqual(span.Trim('a')));
            Assert.True(span.SequenceEqual(span.TrimStart('a')));
            Assert.True(span.SequenceEqual(span.TrimEnd('a')));
        }

        [Fact]
        public static void NoTrimCharacter()
        {
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'b';
                }
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(span.SequenceEqual(span.Trim('a')));
                Assert.True(span.SequenceEqual(span.TrimStart('a')));
                Assert.True(span.SequenceEqual(span.TrimEnd('a')));
            }
        }

        [Fact]
        public static void OnlyTrimCharacter()
        {
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(span.Trim('a')));
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(span.TrimStart('a')));
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(span.TrimEnd('a')));
            }
        }

        [Fact]
        public static void TrimCharacterAtStart()
        {
            for (int length = 2; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'b';
                }
                a[0] = 'a';
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(span.Slice(1).SequenceEqual(span.Trim('a')));
                Assert.True(span.Slice(1).SequenceEqual(span.TrimStart('a')));
                Assert.True(span.SequenceEqual(span.TrimEnd('a')));
            }
        }

        [Fact]
        public static void TrimCharacterAtEnd()
        {
            for (int length = 2; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'b';
                }
                a[length - 1] = 'a';
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.Trim('a')));
                Assert.True(span.SequenceEqual(span.TrimStart('a')));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.TrimEnd('a')));
            }
        }
        
        [Fact]
        public static void TrimCharacterAtStartAndEnd()
        {
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'b';
                }
                a[0] = 'a';
                a[length - 1] = 'a';
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(span.Slice(1, length - 2).SequenceEqual(span.Trim('a')));
                Assert.True(span.Slice(1).SequenceEqual(span.TrimStart('a')));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.TrimEnd('a')));
            }
        }

        [Fact]
        public static void TrimCharacterInMiddle()
        {
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'b';
                }
                a[1] = 'a';
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(span.SequenceEqual(span.Trim('a')));
                Assert.True(span.SequenceEqual(span.TrimStart('a')));
                Assert.True(span.SequenceEqual(span.TrimEnd('a')));
            }
        }

        [Fact]
        public static void TrimCharacterMultipleTimes()
        {
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'b';
                }
                a[0] = 'a';
                a[length - 1] = 'a';
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                ReadOnlySpan<char> trimResult = span.Trim('a');
                ReadOnlySpan<char> trimStartResult = span.TrimStart('a');
                ReadOnlySpan<char> trimEndResult = span.TrimEnd('a');
                Assert.True(span.Slice(1, length - 2).SequenceEqual(trimResult));
                Assert.True(span.Slice(1).SequenceEqual(trimStartResult));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(trimEndResult));

                // 2nd attempt should do nothing
                Assert.True(trimResult.SequenceEqual(trimResult.Trim('a')));
                Assert.True(trimStartResult.SequenceEqual(trimStartResult.TrimStart('a')));
                Assert.True(trimEndResult.SequenceEqual(trimEndResult.TrimEnd('a')));
            }
        }

        [Fact]
        public static void MakeSureNoTrimCharacterChecksGoOutOfRange()
        {
            for (int length = 3; length < 64; length++)
            {
                char[] first = new char[length];
                first[0] = 'a';
                first[length - 1] = 'a';
                var span = new ReadOnlySpan<char>(first, 1, length - 2);
                Assert.True(span.SequenceEqual(span.Trim('a')));
                Assert.True(span.SequenceEqual(span.TrimStart('a')));
                Assert.True(span.SequenceEqual(span.TrimEnd('a')));
            }
        }
    }
}
