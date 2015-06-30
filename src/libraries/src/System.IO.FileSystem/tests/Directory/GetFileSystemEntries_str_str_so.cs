// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

public class Directory_GetFileSystemEntries_str_str_so : Directory_GetFileSystemEntries_str_str
{
    #region Utilities

    public override String[] GetEntries(String dirName)
    {
        return Directory.GetFileSystemEntries(dirName, "*", SearchOption.TopDirectoryOnly);
    }

    public override String[] GetEntries(String dirName, String searchPattern)
    {
        return Directory.GetFileSystemEntries(dirName, searchPattern, SearchOption.TopDirectoryOnly);
    }

    public virtual String[] GetEntries(String dirName, String searchPattern, SearchOption option)
    {
        return Directory.GetFileSystemEntries(dirName, searchPattern, option);
    }

    #endregion

    #region UniversalTests

    [Fact]
    public void IncludeSubDirectoryFiles()
    {
        String testDir1Str = GetTestFileName();
        String testDir11Str = GetTestFileName();
        DirectoryInfo testDir = new DirectoryInfo(TestDirectory);
        DirectoryInfo testDir1 = testDir.CreateSubdirectory(testDir1Str);
        testDir1.CreateSubdirectory(testDir11Str);
        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, testDir11Str, GetTestFileName())).Create())
        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, GetTestFileName())).Create())
        {
            Assert.Equal(3, GetEntries(Path.Combine(TestDirectory, testDir1Str), "*", SearchOption.AllDirectories).Length);
        }
    }

    [Fact]
    public void SearchPatternIncludeSubDirectories()
    {
        String testDir1Str = GetTestFileName();
        String testDir11Str = GetTestFileName();
        DirectoryInfo testDir = new DirectoryInfo(TestDirectory);
        DirectoryInfo testDir1 = testDir.CreateSubdirectory(testDir1Str);
        testDir1.CreateSubdirectory(testDir11Str);

        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, testDir11Str, GetTestFileName())).Create())
        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, GetTestFileName())).Create())
        {
            Assert.Equal(3, GetEntries(TestDirectory, Path.Combine(testDir1Str, "*"), SearchOption.AllDirectories).Length);
        }
    }

    [Fact]
    public void InvalidSearchOption()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GetEntries(".", "*", (SearchOption)100));
        Assert.Throws<ArgumentOutOfRangeException>(() => GetEntries(".", "*", (SearchOption)(-1)));
    }

    #endregion
}



