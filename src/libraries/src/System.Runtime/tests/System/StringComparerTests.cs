// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.Tests
{
    public partial class StringComparerTests
    {
        [Fact]
        public void Create_InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("culture", () => StringComparer.Create(null, ignoreCase: true));
        }

        [Fact]
        public void Create_CreatesValidComparer()
        {
            StringComparer c = StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: true);
            Assert.NotNull(c);
            Assert.True(c.Equals((object)"hello", (object)"HEllO"));
            Assert.True(c.Equals("hello", "HEllO"));
            Assert.False(c.Equals((object)"bello", (object)"HEllO"));
            Assert.False(c.Equals("bello", "HEllO"));

            c = StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: false);
            Assert.NotNull(c);
#pragma warning disable 0618 // suppress obsolete warning for String.Copy
            Assert.True(c.Equals((object)"hello", (object)string.Copy("hello")));
#pragma warning restore 0618 // restore warning when accessing obsolete members
            Assert.False(c.Equals((object)"hello", (object)"HEllO"));
            Assert.False(c.Equals("hello", "HEllO"));
            Assert.False(c.Equals((object)"bello", (object)"HEllO"));
            Assert.False(c.Equals("bello", "HEllO"));

            object obj = new object();
            Assert.Equal(c.GetHashCode((object)"hello"), c.GetHashCode((object)"hello"));
            Assert.Equal(c.GetHashCode("hello"), c.GetHashCode("hello"));
            Assert.Equal(c.GetHashCode("hello"), c.GetHashCode((object)"hello"));
            Assert.Equal(obj.GetHashCode(), c.GetHashCode(obj));
            Assert.Equal(42.CompareTo(84), c.Compare(42, 84));
            Assert.Throws<ArgumentException>(() => c.Compare("42", 84));
            Assert.Equal(1, c.Compare("42", null));
            Assert.Throws<ArgumentException>(() => c.Compare(42, "84"));
        }

        [Fact]
        public void Compare_InvalidArguments_Throws()
        {
            StringComparer c = StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: true);
            Assert.Throws<ArgumentException>(() => c.Compare(new object(), 42));
        }

        [Fact]
        public void GetHashCode_InvalidArguments_Throws()
        {
            StringComparer c = StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: true);
            AssertExtensions.Throws<ArgumentNullException>("obj", () => c.GetHashCode(null));
            AssertExtensions.Throws<ArgumentNullException>("obj", () => c.GetHashCode((object)null));
        }

        [Fact]
        public void Compare_ViaSort_SortsAsExpected()
        {
            string[] strings = new[] { "a", "b", "AB", "A", "cde", "abc", "f", "123", "ab" };

            Array.Sort(strings, StringComparer.OrdinalIgnoreCase);
            Assert.Equal<string>(strings, new[] { "123", "a", "A", "AB", "ab", "abc", "b", "cde", "f" });

            Array.Sort(strings, StringComparer.Ordinal);
            Assert.Equal<string>(strings, new[] { "123", "A", "AB", "a", "ab", "abc", "b", "cde", "f" });
        }

        [Fact]
        public void Compare_ExpectedResults()
        {
            StringComparer c = StringComparer.Ordinal;

            Assert.Equal(0, c.Compare((object)"hello", (object)"hello"));
#pragma warning disable 0618 // suppress obsolete warning for String.Copy
            Assert.Equal(0, c.Compare((object)"hello", (object)string.Copy("hello")));
#pragma warning restore 0618 // restore warning when accessing obsolete members
            Assert.Equal(-1, c.Compare(null, (object)"hello"));
            Assert.Equal(1, c.Compare((object)"hello", null));

            Assert.InRange(c.Compare((object)"hello", (object)"world"), int.MinValue, -1);
            Assert.InRange(c.Compare((object)"world", (object)"hello"), 1, int.MaxValue);
        }

        [Fact]
        public void Equals_ExpectedResults()
        {
            StringComparer c = StringComparer.Ordinal;

            Assert.True(c.Equals((object)null, (object)null));
            Assert.True(c.Equals(null, null));
            Assert.True(c.Equals((object)"hello", (object)"hello"));
            Assert.True(c.Equals("hello", "hello"));

            Assert.False(c.Equals((object)null, "hello"));
            Assert.False(c.Equals(null, "hello"));
            Assert.False(c.Equals("hello", (object)null));
            Assert.False(c.Equals("hello", null));

            Assert.True(c.Equals(42, 42));
            Assert.False(c.Equals(42, 84));
            Assert.False(c.Equals("42", 84));
            Assert.False(c.Equals(42, "84"));
        }
    }
}
