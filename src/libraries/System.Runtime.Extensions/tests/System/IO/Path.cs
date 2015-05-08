// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

public static class PathTests
{
    [Fact]
    public static void ChangeExtension()
    {
        Assert.Null(Path.ChangeExtension(null, null));
        Assert.Equal("file", Path.ChangeExtension("file.exe", null));
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

        Assert.Null(Path.GetExtension(null));
        Assert.False(Path.HasExtension(null));

        Assert.Equal("", Path.GetExtension("file."));
        Assert.False(Path.HasExtension("file."));

        Assert.Equal("", Path.GetExtension(Path.Combine("test", "file")));
        Assert.False(Path.HasExtension(Path.Combine("test", "file")));
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
        Assert.Null(Path.GetFileNameWithoutExtension(null));
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

    [Fact]
    public static void GetInvalidPathChars()
    {
        Assert.NotNull(Path.GetInvalidPathChars());
        Assert.NotSame(Path.GetInvalidPathChars(), Path.GetInvalidPathChars());
        Assert.Equal((IEnumerable<char>)Path.GetInvalidPathChars(), (IEnumerable<char>)Path.GetInvalidPathChars());
        Assert.True(Path.GetInvalidPathChars().Length > 0);
    }

    [Fact]
    public static void GetInvalidFileNameChars()
    {
        Assert.NotNull(Path.GetInvalidFileNameChars());
        Assert.NotSame(Path.GetInvalidFileNameChars(), Path.GetInvalidFileNameChars());
        Assert.Equal((IEnumerable<char>)Path.GetInvalidFileNameChars(), (IEnumerable<char>)Path.GetInvalidFileNameChars());
        Assert.True(Path.GetInvalidFileNameChars().Length > 0);
    }

    [Fact]
    public static void GetTempPath()
    {
        string tmpPath = Path.GetTempPath();
        Assert.False(string.IsNullOrEmpty(tmpPath));
        Assert.Equal(tmpPath, Path.GetTempPath());
        Assert.Equal(Path.DirectorySeparatorChar, tmpPath[tmpPath.Length - 1]);
        Assert.True(Directory.Exists(tmpPath));
    }

    [Fact]
    public static void GetTempFileName()
    {
        string tmpFile = Path.GetTempFileName();
        try
        {
            Assert.True(File.Exists(tmpFile));
            Assert.Equal(".tmp", Path.GetExtension(tmpFile), ignoreCase: true);
            using (FileStream fs = File.OpenRead(tmpFile))
                Assert.Equal(0, fs.Length);
            Assert.Equal(Path.Combine(Path.GetTempPath(), Path.GetFileName(tmpFile)), tmpFile);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public static void GetFullPath()
    {
        Assert.Throws<ArgumentNullException>(() => Path.GetFullPath(null));

        string curDir = Directory.GetCurrentDirectory();
        Assert.Equal(curDir, Path.GetFullPath(Path.Combine(curDir, ".", ".", ".", ".", ".")));
        Assert.Equal(curDir, Path.GetFullPath(curDir + Path.DirectorySeparatorChar + Path.DirectorySeparatorChar + Path.DirectorySeparatorChar + "."));
        Assert.Equal(curDir, Path.GetFullPath(Path.Combine(curDir, "..", Path.GetFileName(curDir), ".", "..", Path.GetFileName(curDir))));
    }

}
