// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Xunit;

public class PathInternal_Unix_Tests
{
    [Theory,
        InlineData(@"", @""),
        InlineData(null, null),
        InlineData(@"/", @"/"),
        InlineData(@"//", @"/"),
        InlineData(@"///", @"/"),
        InlineData(@"\", @"\"),
        InlineData(@"\\", @"\\"),
        InlineData(@"\\\", @"\\\"),
        InlineData(@"\/", @"\/"),
        InlineData(@"\/\", @"\/\"),

        InlineData(@"a/a", @"a/a"),
        InlineData(@"a//a", @"a/a"),
        InlineData(@"a\\a", @"a\\a"),
        InlineData(@"/a", @"/a"),
        InlineData(@"//a", @"/a"),
        InlineData(@"\\a", @"\\a"),
        InlineData(@"a/", @"a/"),
        InlineData(@"a//", @"a/"),
        InlineData(@"a\\", @"a\\"),
        ]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public void NormalizeDirectorySeparatorTests(string path, string expected)
    {
        string result = PathInternal.NormalizeDirectorySeparators(path);
        Assert.Equal(expected, result);
        if (string.Equals(path, expected, StringComparison.Ordinal))
            Assert.Same(path, result);
    }
    
    [Theory]
    [InlineData(@"\", 0)]
    [InlineData("", 0)]
    [InlineData(":", 0)]
    [InlineData(";", 0)]
    [InlineData("/", 1)]
    [InlineData(@"Foo\/\/\", 7)]
    [InlineData("Foo:Bar", 0)]
    [InlineData("/usr/foo/", 9)]
    [InlineData("/home/bar", 6)]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public void FindFileNameIndexTests(string path, int expected)
    {
        Assert.Equal(expected, PathInternal.FindFileNameIndex(path));
    }
}
