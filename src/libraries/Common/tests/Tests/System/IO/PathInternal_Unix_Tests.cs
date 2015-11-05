// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void NormalizeDirectorySeparatorTests(string path, string expected)
    {
        string result = PathInternal.NormalizeDirectorySeparators(path);
        Assert.Equal(expected, result);
        if (string.Equals(path, expected, StringComparison.Ordinal))
            Assert.Same(path, result);
    }
}
