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
        String testDir = GetTestFileName();
        String testFile1 = Path.Combine(TestDirectory, GetTestFileName());
        String testFile2 = Path.Combine(TestDirectory, testDir, GetTestFileName());
        Directory.CreateDirectory(Path.Combine(TestDirectory, testDir));
        using (File.Create(testFile1))
        using (File.Create(testFile2))
        {
            String[] results = GetEntries(TestDirectory, "*", SearchOption.AllDirectories);
            Assert.Contains(testFile1, results);
            Assert.Contains(testFile2, results);
        }
    }

    [Fact]
    public void SearchPatternIncludeSubDirectories()
    {
        String testDir = GetTestFileName();
        String testFile1 = Path.Combine(TestDirectory, GetTestFileName());
        String testFile2 = Path.Combine(TestDirectory, testDir, GetTestFileName());
        Directory.CreateDirectory(Path.Combine(TestDirectory, testDir));
        using (File.Create(testFile1))
        using (File.Create(testFile2))
        {
            String[] results = GetEntries(Directory.GetCurrentDirectory(), Path.Combine(new DirectoryInfo(TestDirectory).Name, "*"), SearchOption.AllDirectories);
            Assert.Contains(testFile1, results);
            Assert.Contains(testFile2, results);
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
