// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ZeroLengthTrim()
        {
            ReadOnlySpan<char> span = new ReadOnlySpan<char>(Array.Empty<char>());
            Assert.True(span.SequenceEqual(span.Trim()));
            Assert.True(span.SequenceEqual(span.TrimStart()));
            Assert.True(span.SequenceEqual(span.TrimEnd()));
        }

        [Fact]
        public static void NoWhiteSpaceTrim()
        {
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(span.SequenceEqual(span.Trim()));
                Assert.True(span.SequenceEqual(span.TrimStart()));
                Assert.True(span.SequenceEqual(span.TrimEnd()));
            }
        }

        [Fact]
        public static void OnlyWhiteSpaceTrim()
        {
            for (int length = 0; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = ' ';
                }
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(span.Trim()));
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(span.TrimStart()));
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(span.TrimEnd()));
            }
        }

        [Fact]
        public static void WhiteSpaceAtStartTrim()
        {
            for (int length = 2; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }
                a[0] = ' ';
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(span.Slice(1).SequenceEqual(span.Trim()));
                Assert.True(span.Slice(1).SequenceEqual(span.TrimStart()));
                Assert.True(span.SequenceEqual(span.TrimEnd()));
            }
        }

        [Fact]
        public static void WhiteSpaceAtEndTrim()
        {
            for (int length = 2; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }
                a[length - 1] = ' ';
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.Trim()));
                Assert.True(span.SequenceEqual(span.TrimStart()));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.TrimEnd()));
            }
        }
        
        [Fact]
        public static void WhiteSpaceAtStartAndEndTrim()
        {
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }
                a[0] = ' ';
                a[length - 1] = ' ';
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(span.Slice(1, length - 2).SequenceEqual(span.Trim()));
                Assert.True(span.Slice(1).SequenceEqual(span.TrimStart()));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(span.TrimEnd()));
            }
        }

        [Fact]
        public static void WhiteSpaceInMiddleTrim()
        {
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }
                a[1] = ' ';
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                Assert.True(span.SequenceEqual(span.Trim()));
                Assert.True(span.SequenceEqual(span.TrimStart()));
                Assert.True(span.SequenceEqual(span.TrimEnd()));
            }
        }

        [Fact]
        public static void TrimWhiteSpaceMultipleTimes()
        {
            for (int length = 3; length < 32; length++)
            {
                char[] a = new char[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 'a';
                }
                a[0] = ' ';
                a[length - 1] = ' ';
                ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
                ReadOnlySpan<char> trimResult = span.Trim();
                ReadOnlySpan<char> trimStartResult = span.TrimStart();
                ReadOnlySpan<char> trimEndResult = span.TrimEnd();
                Assert.True(span.Slice(1, length - 2).SequenceEqual(trimResult));
                Assert.True(span.Slice(1).SequenceEqual(trimStartResult));
                Assert.True(span.Slice(0, length - 1).SequenceEqual(trimEndResult));

                // 2nd attempt should do nothing
                Assert.True(trimResult.SequenceEqual(trimResult.Trim()));
                Assert.True(trimStartResult.SequenceEqual(trimStartResult.TrimStart()));
                Assert.True(trimEndResult.SequenceEqual(trimEndResult.TrimEnd()));
            }
        }

        [Fact]
        public static void MakeSureNoTrimChecksGoOutOfRange()
        {
            for (int length = 3; length < 64; length++)
            {
                char[] first = new char[length];
                first[0] = ' ';
                first[length - 1] = ' ';
                var span = new ReadOnlySpan<char>(first, 1, length - 2);
                Assert.True(span.SequenceEqual(span.Trim()));
                Assert.True(span.SequenceEqual(span.TrimStart()));
                Assert.True(span.SequenceEqual(span.TrimEnd()));
            }
        }
    }
}
