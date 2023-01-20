// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace System.Collections.Frozen.Tests
{
    public static class ComparerTests
    {
        private static void Equal(SubstringComparerBase c, string a, string b, bool fullEqual)
        {
            Assert.True(c.EqualsPartial(a, b));
            Assert.Equal(c.GetHashCode(a), c.GetHashCode(b));
            Assert.Equal(fullEqual, c.Equals(a, b));
        }

        private static void Equal(StringComparerBase c, string a, string b, bool fullEqual)
        {
            Assert.Equal(c.GetHashCode(a), c.GetHashCode(b));
            Assert.Equal(fullEqual, c.Equals(a, b));
        }

        private static void NotEqual(SubstringComparerBase c, string a, string b)
        {
            Assert.False(c.EqualsPartial(a, b));
            Assert.False(c.Equals(a, b));
            Assert.NotEqual(c.GetHashCode(a), c.GetHashCode(b));
        }

        private static void NotEqual(StringComparerBase c, string a, string b)
        {
            Assert.False(c.Equals(a, b));
            Assert.NotEqual(c.GetHashCode(a), c.GetHashCode(b));
        }

        [Fact]
        public static void LeftHand()
        {
            var c = new LeftJustifiedSubstringComparer
            {
                Index = 0,
                Count = 1
            };

            Equal(c, "a", "a", true);
            Equal(c, "a", "aa", false);
            Equal(c, "a", "ab", false);
            NotEqual(c, "a", "A");
            NotEqual(c, "a", "b");

            c.Index = 1;
            c.Count = 1;
            Equal(c, "Aa", "Ba", false);
            Equal(c, "Aa", "Baa", false);
            Equal(c, "aa", "Bab", false);
            Equal(c, "Aa", "Aa", true);
            Equal(c, "Aab", "Aab", true);
            NotEqual(c, "Aa", "BA");
            NotEqual(c, "Aa", "Bb");

            c.Index = 1;
            c.Count = 2;
            Equal(c, "Aaa", "Baa", false);
            Equal(c, "Aaa", "Baaa", false);
            Equal(c, "aaa", "Baab", false);
            Equal(c, "Aaa", "Aaa", true);
            Equal(c, "Aaab", "Aaab", true);
            NotEqual(c, "Aaa", "BaA");
            NotEqual(c, "Aaa", "Bab");
        }

        [Fact]
        public static void LeftHandSingleChar()
        {
            var c = new LeftJustifiedSingleCharComparer
            {
                Index = 0,
                Count = 1
            };

            Equal(c, "a", "a", true);
            Equal(c, "a", "aa", false);
            Equal(c, "a", "ab", false);
            NotEqual(c, "a", "A");
            NotEqual(c, "a", "b");

            c.Index = 1;
            c.Count = 1;
            Equal(c, "Aa", "Ba", false);
            Equal(c, "Aa", "Baa", false);
            Equal(c, "aa", "Bab", false);
            Equal(c, "Aa", "Aa", true);
            Equal(c, "Aab", "Aab", true);
            NotEqual(c, "Aa", "BA");
            NotEqual(c, "Aa", "Bb");
        }

        [Fact]
        public static void LeftHandCaseInsensitive()
        {
            var c = new LeftJustifiedCaseInsensitiveSubstringComparer
            {
                Index = 0,
                Count = 1
            };

            Equal(c, "a", "a", true);
            Equal(c, "a", "A", true);
            Equal(c, "a", "aa", false);
            Equal(c, "a", "AA", false);
            Equal(c, "a", "ab", false);
            Equal(c, "a", "AB", false);
            NotEqual(c, "a", "b");

            c.Index = 1;
            c.Count = 1;
            Equal(c, "Xa", "Ya", false);
            Equal(c, "Xa", "YA", false);
            Equal(c, "Xa", "Xa", true);
            Equal(c, "Xa", "XA", true);
            Equal(c, "Xa", "Yaa", false);
            Equal(c, "Xa", "YAA", false);
            Equal(c, "Xa", "Yab", false);
            Equal(c, "Xa", "YAB", false);
            NotEqual(c, "Xa", "Yb");
        }

        [Fact]
        public static void LeftHandCaseInsensitiveAscii()
        {
            var c = new LeftJustifiedCaseInsensitiveAsciiSubstringComparer
            {
                Index = 0,
                Count = 1
            };

            Equal(c, "a", "a", true);
            Equal(c, "a", "A", true);
            Equal(c, "a", "aa", false);
            Equal(c, "a", "AA", false);
            Equal(c, "a", "ab", false);
            Equal(c, "a", "AB", false);
            NotEqual(c, "a", "b");

            c.Index = 1;
            c.Count = 1;
            Equal(c, "Xa", "Ya", false);
            Equal(c, "Xa", "YA", false);
            Equal(c, "Xa", "Xa", true);
            Equal(c, "Xa", "XA", true);
            Equal(c, "Xa", "Yaa", false);
            Equal(c, "Xa", "YAA", false);
            Equal(c, "Xa", "Yab", false);
            Equal(c, "Xa", "YAB", false);
            NotEqual(c, "Xa", "Yb");
        }

        [Fact]
        public static void RightHand()
        {
            var c = new RightJustifiedSubstringComparer
            {
                Index = -1,
                Count = 1
            };

            Equal(c, "a", "a", true);
            Equal(c, "a", "aa", false);
            Equal(c, "a", "ba", false);
            NotEqual(c, "a", "A");
            NotEqual(c, "a", "b");

            c.Index = -2;
            c.Count = 1;
            Equal(c, "aX", "aY", false);
            Equal(c, "XaX", "YaY", false);
            Equal(c, "XaX", "YYaY", false);
            Equal(c, "XXaX", "YaY", false);
            NotEqual(c, "XXaX", "YYa");

            c.Index = -2;
            c.Count = 2;
            Equal(c, "aa", "aa", true);
            Equal(c, "aa", "aaa", false);
            Equal(c, "aa", "baa", false);
            NotEqual(c, "aa", "AA");
            NotEqual(c, "aa", "bb");
        }

        [Fact]
        public static void RightHandSingleChar()
        {
            var c = new RightJustifiedSingleCharComparer
            {
                Index = -1,
                Count = 1
            };

            Equal(c, "a", "a", true);
            Equal(c, "a", "aa", false);
            Equal(c, "a", "ba", false);
            NotEqual(c, "a", "A");
            NotEqual(c, "a", "b");

            c.Index = -2;
            c.Count = 1;
            Equal(c, "aX", "aY", false);
            Equal(c, "XaX", "YaY", false);
            Equal(c, "XaX", "YYaY", false);
            Equal(c, "XXaX", "YaY", false);
            NotEqual(c, "XXaX", "YYa");
        }

        [Fact]
        public static void RightHandCaseInsensitive()
        {
            var c = new RightJustifiedCaseInsensitiveSubstringComparer
            {
                Index = -1,
                Count = 1
            };

            Equal(c, "a", "a", true);
            Equal(c, "a", "aa", false);
            Equal(c, "a", "ba", false);
            Equal(c, "a", "A", true);
            Equal(c, "a", "AA", false);
            Equal(c, "a", "BA", false);
            NotEqual(c, "a", "b");

            c.Index = -2;
            c.Count = 1;
            Equal(c, "aX", "aY", false);
            Equal(c, "XaX", "YaY", false);
            Equal(c, "XaX", "YYaY", false);
            Equal(c, "XXaX", "YaY", false);
            Equal(c, "aX", "AY", false);
            Equal(c, "XaX", "YAY", false);
            Equal(c, "XaX", "YYAY", false);
            Equal(c, "XXaX", "YAY", false);
            NotEqual(c, "XXaX", "YYa");

            c.Index = -2;
            c.Count = 2;
            Equal(c, "aa", "aa", true);
            Equal(c, "aa", "aaa", false);
            Equal(c, "aa", "baa", false);
            Equal(c, "aa", "AA", true);
            Equal(c, "aa", "AAA", false);
            Equal(c, "aa", "bAA", false);
            NotEqual(c, "aa", "bb");
        }

        [Fact]
        public static void RightHandCaseInsensitiveAscii()
        {
            var c = new RightJustifiedCaseInsensitiveAsciiSubstringComparer
            {
                Index = -1,
                Count = 1
            };

            Equal(c, "a", "a", true);
            Equal(c, "a", "aa", false);
            Equal(c, "a", "ba", false);
            Equal(c, "a", "A", true);
            Equal(c, "a", "AA", false);
            Equal(c, "a", "BA", false);
            NotEqual(c, "a", "b");

            c.Index = -2;
            c.Count = 1;
            Equal(c, "aX", "aY", false);
            Equal(c, "XaX", "YaY", false);
            Equal(c, "XaX", "YYaY", false);
            Equal(c, "XXaX", "YaY", false);
            Equal(c, "aX", "AY", false);
            Equal(c, "XaX", "YAY", false);
            Equal(c, "XaX", "YYAY", false);
            Equal(c, "XXaX", "YAY", false);
            NotEqual(c, "XXaX", "YYa");

            c.Index = -2;
            c.Count = 2;
            Equal(c, "aa", "aa", true);
            Equal(c, "aa", "aaa", false);
            Equal(c, "aa", "baa", false);
            Equal(c, "aa", "AA", true);
            Equal(c, "aa", "AAA", false);
            Equal(c, "aa", "bAA", false);
            NotEqual(c, "aa", "bb");
        }

        [Fact]
        public static void Full()
        {
            var c = new FullStringComparer();

            Equal(c, "", "", true);
            Equal(c, "A", "A", true);
            Equal(c, "AA", "AA", true);

            NotEqual(c, "A", "AA");
            NotEqual(c, "AA", "A");
        }

        [Fact]
        public static void FullCaseInsensitive()
        {
            var c = new FullCaseInsensitiveStringComparer();

            Equal(c, "", "", true);
            Equal(c, "A", "A", true);
            Equal(c, "A", "a", true);
            Equal(c, "a", "A", true);
            Equal(c, "AA", "aa", true);
            Equal(c, "aa", "AA", true);

            NotEqual(c, "A", "AA");
            NotEqual(c, "AA", "A");
        }

        [Fact]
        public static void FullCaseInsensitiveAscii()
        {
            var c = new FullCaseInsensitiveAsciiStringComparer();

            Equal(c, "", "", true);
            Equal(c, "A", "A", true);
            Equal(c, "A", "a", true);
            Equal(c, "a", "A", true);
            Equal(c, "AA", "aa", true);
            Equal(c, "aa", "AA", true);

            NotEqual(c, "A", "AA");
            NotEqual(c, "AA", "A");
        }
    }
}
