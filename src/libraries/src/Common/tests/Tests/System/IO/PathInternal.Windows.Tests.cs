// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

public class PathInternal_Windows_Tests
{
    [Theory,
        InlineData(PathInternal.ExtendedDevicePathPrefix, PathInternal.ExtendedDevicePathPrefix),
        InlineData(@"Foo", @"Foo"),
        InlineData(@"C:\Foo", @"\\?\C:\Foo"),
        InlineData(@"\\.\Foo", @"\\.\Foo"),
        InlineData(@"\\?\Foo", @"\\?\Foo"),
        InlineData(@"\??\Foo", @"\??\Foo"),
        InlineData(@"//?/Foo", @"//?/Foo"),
        InlineData(@"\\Server\Share", PathInternal.UncExtendedPathPrefix + @"Server\Share")
        ]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void EnsureExtendedPrefixTest(string path, string expected)
    {
        Assert.Equal(expected, PathInternal.EnsureExtendedPrefix(path));
    }

    [Theory,
        InlineData(@"", false),
        InlineData(@"\\?\", true),
        InlineData(@"\??\", true),
        InlineData(@"\\.\", false),
        InlineData(@"\\?", false),
        InlineData(@"\??", false),
        InlineData(@"//?/", false),
        InlineData(@"/??/", false)
        ]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void IsExtendedTest(string path, bool expected)
    {
        Assert.Equal(expected, PathInternal.IsExtended(path));
    }

    [Theory,
        InlineData(@"", false),
        InlineData(@"\\?\", true),
        InlineData(@"\??\", true),
        InlineData(@"\\.\", true),
        InlineData(@"\\?", false),
        InlineData(@"\??", false),
        InlineData(@"//?/", true),
        InlineData(@"/??/", false)
        ]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void IsDeviceTest(string path, bool expected)
    {
        Assert.Equal(expected, PathInternal.IsDevice(path));
    }

    [Theory,
        InlineData("", true),
        InlineData("C:", true),
        InlineData("**", true),
        InlineData(@"\\.\path", false),
        InlineData(@"\\?\path", false),
        InlineData(@"\\.", false),
        InlineData(@"\\?", false),
        InlineData(@"\?", false),
        InlineData(@"/?", false),
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
        InlineData(@"X", true)
        ]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void IsPartiallyQualifiedTest(string path, bool expected)
    {
        Assert.Equal(expected, PathInternal.IsPartiallyQualified(path));
    }
}
