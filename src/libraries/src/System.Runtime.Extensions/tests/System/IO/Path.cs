// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

public static class PathTests
{
    [Fact]
    public static void ChangeExtension()
    {
        Assert.Equal("file.exe", Path.ChangeExtension("file.txt", "exe"));
    }

    [Fact]
    public static void GetDirectoryName()
    {
        Assert.Equal("dir", Path.GetDirectoryName(@"dir\baz"));
        Assert.Equal(null, Path.GetDirectoryName(@"c:\"));
    }

    [Fact]
    public static void GetExtension()
    {
        Assert.Equal(".exe", Path.GetExtension("file.exe"));
        Assert.True(Path.HasExtension("file.exe"));
        Assert.Equal(string.Empty, Path.GetExtension("file"));
        Assert.False(Path.HasExtension("file"));
    }

    [Fact]
    public static void GetFileName()
    {
        Assert.Equal("file.exe", Path.GetFileName(@"c:\bar\baz\file.exe"));
        Assert.Equal(string.Empty, Path.GetFileName(@"c:\bar\baz\"));
    }

    [Fact]
    public static void GetFileNameWithoutExtension()
    {
        Assert.Equal("file", Path.GetFileNameWithoutExtension(@"c:\bar\baz\file.exe"));
        Assert.Equal(string.Empty, Path.GetFileNameWithoutExtension(@"c:\bar\baz\"));
    }

    [Fact]
    public static void GetPathRoot()
    {
        Assert.Equal(@"c:\", Path.GetPathRoot(@"c:\x\y\z\"));
        Assert.True(Path.IsPathRooted(@"c:\x\y\z\"));
        Assert.Equal(string.Empty, Path.GetPathRoot(@"file.exe"));
        Assert.False(Path.IsPathRooted("file.exe"));
    }

    [Fact]
    public static void GetRandomFileName()
    {
        for (int i = 0; i < 100; i++)
        {
            String s = Path.GetRandomFileName();
            Assert.Equal(s.Length, 8 + 1 + 3);
            Assert.Equal(s[8], '.');
        }
    }
}
