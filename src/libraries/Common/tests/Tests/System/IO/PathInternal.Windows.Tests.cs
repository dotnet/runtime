// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using Xunit;

public class PathInternal_Windows_Tests
{
    [Theory]
    [InlineData(PathInternal.ExtendedPathPrefix, PathInternal.ExtendedPathPrefix)]
    [InlineData(@"Foo", @"Foo")]
    [InlineData(@"C:\Foo", @"\\?\C:\Foo")]
    [InlineData(@"\\.\Foo", @"\\.\Foo")]
    [InlineData(@"\\Server\Share", PathInternal.UncExtendedPathPrefix + @"Server\Share")]
    [PlatformSpecific(PlatformID.Windows)]
    public void EnsureExtendedPrefixTest(string path, string expected)
    {
        StringBuilder sb = new StringBuilder(path);
        PathInternal.EnsureExtendedPrefix(sb);
        Assert.Equal(expected, sb.ToString());

        Assert.Equal(expected, PathInternal.EnsureExtendedPrefix(path));
    }

    [Theory,
        InlineData("", true),
        InlineData("C:", true),
        InlineData("**", true),
        InlineData(@"\\.\path", false),
        InlineData(@"\\?\path", false),
        InlineData(@"\\.", false),
        InlineData(@"\\?", false),
        InlineData(@"\\", false),
        InlineData(@"//", false),
        InlineData(@"\a", true),
        InlineData(@"/a", true),
        InlineData(@"\", true),
        InlineData(@"/", true),
        InlineData(@"C:Path", true),
        InlineData(@"C:\Path", false),
        InlineData(@"\\?\C:\Path", false),
        InlineData(@"Path", true),
        InlineData(@"X", true)]
    [PlatformSpecific(PlatformID.Windows)]
    public void IsRelativeTest(string path, bool expected)
    {
        StringBuilder sb = new StringBuilder(path);
        Assert.Equal(expected, PathInternal.IsRelative(sb));

        Assert.Equal(expected, PathInternal.IsRelative(path));
    }

    [Theory,
        InlineData(@"", 0),
        InlineData(@"  :", 0),
        InlineData(@"  C:", 2),
        InlineData(@"   C:\", 3),
        InlineData(@"C:\", 0),
        InlineData(@"  ", 0),
        InlineData(@"  \", 2),
        InlineData(@"  8:", 0),
        InlineData(@"    \\", 4),
        InlineData(@"\\", 0),
        ]
    [PlatformSpecific(PlatformID.Windows)]
    public void PathStartSkipTest(string path, int expected)
    {
        Assert.Equal(expected, PathInternal.PathStartSkip(path));
    }

    [Theory,
        InlineData(@"", @""),
        InlineData(null, null),
        InlineData(@"\", @"\"),
        InlineData(@"/", @"\"),
        InlineData(@"\\", @"\\"),
        InlineData(@"\\\", @"\\"),
        InlineData(@"//", @"\\"),
        InlineData(@"///", @"\\"),
        InlineData(@"\/", @"\\"),
        InlineData(@"\/\", @"\\"),

        InlineData(@"a\a", @"a\a"),
        InlineData(@"a\\a", @"a\a"),
        InlineData(@"a/a", @"a\a"),
        InlineData(@"a//a", @"a\a"),
        InlineData(@"a\", @"a\"),
        InlineData(@"a\\", @"a\"),
        InlineData(@"a/", @"a\"),
        InlineData(@"a//", @"a\"),
        InlineData(@"\a", @"\a"),
        InlineData(@"\\a", @"\\a"),
        InlineData(@"/a", @"\a"),
        InlineData(@"//a", @"\\a"),

        // Skip tests
        InlineData(@"  :", @"  :"),
        InlineData(@"  C:", @"C:"),
        InlineData(@"   C:\", @"C:\"),
        InlineData(@"   C:/", @"C:\"),
        InlineData(@"  ", @"  "),
        InlineData(@"  \", @"\"),
        InlineData(@"  /", @"\"),
        InlineData(@"  8:", @"  8:"),
        InlineData(@"    \\", @"\\"),
        InlineData(@"    //", @"\\"),
        ]
    [PlatformSpecific(PlatformID.Windows)]
    public void NormalizeDirectorySeparatorTests(string path, string expected)
    {
        string result = PathInternal.NormalizeDirectorySeparators(path);
        Assert.Equal(expected, result);
        if (string.Equals(path, expected, StringComparison.Ordinal))
            Assert.Same(path, result);
    }
}
