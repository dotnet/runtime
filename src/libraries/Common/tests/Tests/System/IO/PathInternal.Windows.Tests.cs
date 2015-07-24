// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

public class PathInternal_Windows_Tests
{
    [Theory]
    [InlineData(PathInternal.ExtendedPathPrefix, PathInternal.ExtendedPathPrefix)]
    [InlineData(@"Foo", PathInternal.ExtendedPathPrefix + @"Foo")]
    [InlineData(@"\\Server\Share", PathInternal.UncExtendedPathPrefix + @"Server\Share")]
    [PlatformSpecific(PlatformID.Windows)]
    public void AddExtendedPathPrefixTest(string path, string expected)
    {
        Assert.Equal(expected, PathInternal.AddExtendedPathPrefix(path));
    }
}
