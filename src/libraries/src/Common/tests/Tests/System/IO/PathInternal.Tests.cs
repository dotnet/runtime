// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using Xunit;

namespace Tests.System.IO
{
    public class PathInternalTests
    {
        [Fact]
        public void CheckInvalidPathChars_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => PathInternal.CheckInvalidPathChars(null));
        }

        [Theory,
            InlineData("Foo", "Foos"),
            InlineData("Foo", null)]
        public void StartsWithOrdinal_NegativeCases(string source, string value)
        {
            Assert.False(PathInternal.StartsWithOrdinal(source, value));
            Assert.False(PathInternal.StartsWithOrdinal(new StringBuilder(source), value));
        }

        [Theory,
            InlineData("FOO", "foo", false),
            InlineData("FOO", "FOO", true),
            InlineData("FOO", "fo", false),
            InlineData("FOO", "FO", true),
            InlineData("FOO", "oo", false),
            InlineData("FOO", "OO", false)]
        public void StartsWithOrdinal_PositiveCases(string source, string value, bool expected)
        {
            Assert.Equal(expected, PathInternal.StartsWithOrdinal(source, value));
            Assert.Equal(expected, PathInternal.StartsWithOrdinal(new StringBuilder(source), value));
        }
    }
}
