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
        public void CreateCultureOptions_InvalidArguments_Throws()
        {
            Assert.Throws<ArgumentException>(() => StringComparer.Create(null, CompareOptions.None));
        }

        [Fact]
        public void CreateCultureOptions_CreatesValidComparer()
        {
            StringComparer c = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
            Assert.NotNull(c);
            Assert.True(c.Equals((object)"hello", (object)"HEllO"));
            Assert.True(c.Equals("hello", "HEllO"));
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
    }
}
