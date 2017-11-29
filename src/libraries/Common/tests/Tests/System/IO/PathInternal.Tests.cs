// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Xunit;

namespace Tests.System.IO
{
    public class PathInternalTests
    {
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

        [Theory,
            InlineData("", "", true, 0),
            InlineData("", "", false, 0),
            InlineData("a", "", true, 0),
            InlineData("a", "", false, 0),
            InlineData("", "b", true, 0),
            InlineData("", "b", false, 0),
            InlineData("\0", "\0", true, 1),
            InlineData("\0", "\0", false, 1),
            InlineData("ABcd", "ABCD", true, 4),
            InlineData("ABCD", "ABcd", true, 4),
            InlineData("ABcd", "ABCD", false, 2),
            InlineData("ABCD", "ABcd", false, 2),
            InlineData("AB\0cd", "AB\0CD", true, 5),
            InlineData("AB\0CD", "AB\0cd", true, 5),
            InlineData("AB\0cd", "AB\0CD", false, 3),
            InlineData("AB\0CD", "AB\0cd", false, 3),
            InlineData("ABc\0", "ABC\0", true, 4),
            InlineData("ABC\0", "ABc\0", true, 4),
            InlineData("ABc\0", "ABC\0", false, 2),
            InlineData("ABC\0", "ABc\0", false, 2),
            InlineData("ABcdxyzl", "ABCDpdq", true, 4),
            InlineData("ABCDxyz", "ABcdpdql", true, 4),
            InlineData("ABcdxyz", "ABCDpdq", false, 2),
            InlineData("ABCDxyzoo", "ABcdpdq", false, 2)]
        public void EqualStartingCharacterCount(string first, string second, bool ignoreCase, int expected)
        {
            Assert.Equal(expected, PathInternal.EqualStartingCharacterCount(first, second, ignoreCase));
        }


        [Theory,
            InlineData(@"", @"", true, 0),
            InlineData(@"", @"", false, 0),
            InlineData(@"a", @"A", true, 1),
            InlineData(@"A", @"a", true, 1),
            InlineData(@"a", @"A", false, 0),
            InlineData(@"A", @"a", false, 0),
            InlineData(@"foo", @"foobar", true, 0),
            InlineData(@"foo", @"foobar", false, 0),
            InlineData(@"foo", @"foo/bar", true, 3),
            InlineData(@"foo", @"foo/bar", false, 3),
            InlineData(@"foo/", @"foo/bar", true, 4),
            InlineData(@"foo/", @"foo/bar", false, 4),
            InlineData(@"foo/bar", @"foo/bar", true, 7),
            InlineData(@"foo/bar", @"foo/bar", false, 7),
            InlineData(@"foo/bar", @"foo/BAR", true, 7),
            InlineData(@"foo/bar", @"foo/BAR", false, 4),
            InlineData(@"foo/bar", @"foo/barb", true, 4),
            InlineData(@"foo/bar", @"foo/barb", false, 4)]
        public void GetCommonPathLength(string first, string second, bool ignoreCase, int expected)
        {
            Assert.Equal(expected, PathInternal.GetCommonPathLength(first, second, ignoreCase));
        }
    }
}
