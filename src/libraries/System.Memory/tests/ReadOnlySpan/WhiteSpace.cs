// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void IsWhiteSpace_True()
        {
            Assert.True(Span<char>.Empty.IsWhiteSpace());

            List<char> chars = [];
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (char.IsWhiteSpace((char)i))
                    chars.Add((char)i);
            }

            Assert.True(CollectionsMarshal.AsSpan(chars).IsWhiteSpace());
        }

        [Fact]
        public static void IsWhiteSpace_False()
        {
            List<char> chars = [];
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (char.IsWhiteSpace((char)i))
                    chars.Add((char)i);
            }

            var index = chars.Count;
            chars.AddRange(chars.ToArray());
            chars.Insert(index, ' ');
            var span = CollectionsMarshal.AsSpan(chars);

            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (!char.IsWhiteSpace((char)i))
                {
                    chars[index] = (char)i;
                    Assert.False(span.IsWhiteSpace());
                }
            }
        }

        [Fact]
        public static void ContainsAnyWhiteSpace_Found()
        {
            List<char> chars = [];
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (!char.IsWhiteSpace((char)i))
                    chars.Add((char)i);
            }

            var index = chars.Count;
            chars.AddRange(chars.ToArray());
            chars.Insert(index, ' ');
            var span = CollectionsMarshal.AsSpan(chars);

            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (char.IsWhiteSpace((char)i))
                {
                    chars[index] = (char)i;
                    Assert.True(span.ContainsAnyWhiteSpace());
                }
            }
        }

        [Fact]
        public static void ContainsAnyWhiteSpace_NotFound()
        {
            Assert.False(Span<char>.Empty.ContainsAnyWhiteSpace());

            List<char> chars = [];
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (!char.IsWhiteSpace((char)i))
                    chars.Add((char)i);
            }

            Assert.False(CollectionsMarshal.AsSpan(chars).ContainsAnyWhiteSpace());
        }

        [Fact]
        public static void IndexOfAnyWhiteSpace_Found()
        {
            List<char> chars = [];
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (!char.IsWhiteSpace((char)i))
                    chars.Add((char)i);
            }

            var index = chars.Count;
            chars.AddRange(chars.ToArray());
            chars.Insert(index, ' ');
            chars.Insert(index, ' ');
            var span = CollectionsMarshal.AsSpan(chars);

            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (char.IsWhiteSpace((char)i))
                {
                    chars[index] = (char)i;
                    chars[index + 1] = (char)i;
                    Assert.Equal(index, span.IndexOfAnyWhiteSpace());
                }
            }
        }

        [Fact]
        public static void IndexOfAnyWhiteSpace_NotFound()
        {
            Assert.Equal(-1, Span<char>.Empty.IndexOfAnyWhiteSpace());

            List<char> chars = [];
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (!char.IsWhiteSpace((char)i))
                    chars.Add((char)i);
            }

            Assert.Equal(-1, CollectionsMarshal.AsSpan(chars).IndexOfAnyWhiteSpace());
        }

        [Fact]
        public static void IndexOfAnyExceptWhiteSpace_Found()
        {
            List<char> chars = [];
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (char.IsWhiteSpace((char)i))
                    chars.Add((char)i);
            }

            var index = chars.Count;
            chars.AddRange(chars.ToArray());
            chars.Insert(index, ' ');
            chars.Insert(index, ' ');
            var span = CollectionsMarshal.AsSpan(chars);

            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (!char.IsWhiteSpace((char)i))
                {
                    chars[index] = (char)i;
                    chars[index + 1] = (char)i;
                    Assert.Equal(index, span.IndexOfAnyExceptWhiteSpace());
                }
            }
        }

        [Fact]
        public static void IndexOfAnyExceptWhiteSpace_NotFound()
        {
            Assert.Equal(-1, Span<char>.Empty.IndexOfAnyExceptWhiteSpace());

            List<char> chars = [];
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (char.IsWhiteSpace((char)i))
                    chars.Add((char)i);
            }

            Assert.Equal(-1, CollectionsMarshal.AsSpan(chars).IndexOfAnyExceptWhiteSpace());
        }

        [Fact]
        public static void LastIndexOfAnyWhiteSpace_Found()
        {
            List<char> chars = [];
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (!char.IsWhiteSpace((char)i))
                    chars.Add((char)i);
            }

            var index = chars.Count;
            chars.AddRange(chars.ToArray());
            chars.Insert(index, ' ');
            chars.Insert(index, ' ');
            var span = CollectionsMarshal.AsSpan(chars);

            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (char.IsWhiteSpace((char)i))
                {
                    chars[index] = (char)i;
                    chars[index + 1] = (char)i;
                    Assert.Equal(index + 1, span.LastIndexOfAnyWhiteSpace());
                }
            }
        }

        [Fact]
        public static void LastIndexOfAnyWhiteSpace_NotFound()
        {
            Assert.Equal(-1, Span<char>.Empty.LastIndexOfAnyWhiteSpace());

            List<char> chars = [];
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (!char.IsWhiteSpace((char)i))
                    chars.Add((char)i);
            }

            Assert.Equal(-1, CollectionsMarshal.AsSpan(chars).LastIndexOfAnyWhiteSpace());
        }

        [Fact]
        public static void LastIndexOfAnyExceptWhiteSpace_Found()
        {
            List<char> chars = [];
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (char.IsWhiteSpace((char)i))
                    chars.Add((char)i);
            }

            var index = chars.Count;
            chars.AddRange(chars.ToArray());
            chars.Insert(index, ' ');
            chars.Insert(index, ' ');
            var span = CollectionsMarshal.AsSpan(chars);

            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (!char.IsWhiteSpace((char)i))
                {
                    chars[index] = (char)i;
                    chars[index + 1] = (char)i;
                    Assert.Equal(index + 1, span.LastIndexOfAnyExceptWhiteSpace());
                }
            }
        }

        [Fact]
        public static void LastIndexOfAnyExceptWhiteSpace_NotFound()
        {
            Assert.Equal(-1, Span<char>.Empty.LastIndexOfAnyExceptWhiteSpace());

            List<char> chars = [];
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (char.IsWhiteSpace((char)i))
                    chars.Add((char)i);
            }

            Assert.Equal(-1, CollectionsMarshal.AsSpan(chars).LastIndexOfAnyExceptWhiteSpace());
        }
    }
}
