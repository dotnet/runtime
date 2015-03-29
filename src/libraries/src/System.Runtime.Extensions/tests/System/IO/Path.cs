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
        Assert.Equal("dir", Path.GetDirectoryName(Path.Combine("dir", "baz")));
        Assert.Equal(null, Path.GetDirectoryName(Path.GetPathRoot(Directory.GetCurrentDirectory())));
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
        Assert.Equal("file.exe", Path.GetFileName(Path.Combine("bar", "baz", "file.exe")));
        Assert.Equal(string.Empty, Path.GetFileName(Path.Combine("bar", "baz") + Path.DirectorySeparatorChar));
    }

    [Fact]
    public static void GetFileNameWithoutExtension()
    {
        Assert.Equal("file", Path.GetFileNameWithoutExtension(Path.Combine("bar","baz","file.exe")));
        Assert.Equal(string.Empty, Path.GetFileNameWithoutExtension(Path.Combine("bar","baz") + Path.DirectorySeparatorChar));
    }

    [Fact]
    public static void GetPathRoot()
    {
        string cwd = Directory.GetCurrentDirectory();
        Assert.Equal(cwd.Substring(0, cwd.IndexOf(Path.DirectorySeparatorChar) + 1), Path.GetPathRoot(cwd));
        Assert.True(Path.IsPathRooted(cwd));
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
