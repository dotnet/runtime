// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.FileSystem.Tests;
using Xunit;

public class Directory_GetFileSystemEntries_str : FileSystemTest
{
    #region Utilities

    /// <summary>
    /// Indirection around Directory.GetFileSystemEntries that child test classes may
    /// replace with a more fitting version
    /// </summary>
    public virtual String[] GetEntries(String dirName)
    {
        return Directory.GetFileSystemEntries(dirName);
    }

    #endregion

    #region UniversalTests

    [Fact]
    public void NullFileName()
    {
        Assert.Throws<ArgumentNullException>(() => GetEntries(null));
    }

    [Fact]
    public void EmptyFileName()
    {
        Assert.Throws<ArgumentException>(() => GetEntries(""));
    }

    [Fact]
    public void NonexistentDirectory()
    {
        Assert.Throws<DirectoryNotFoundException>(() => GetEntries(GetTestFilePath()));
    }

    [Fact]
    public void FileNameWithSpaces()
    {
        String strTempDir = "             ";
        Assert.Throws<ArgumentException>(() => GetEntries(strTempDir));
    }

    [Fact]
    public void TrailingSlashes()
    {
        String strTempDir = Directory.GetCurrentDirectory() + new string(Path.DirectorySeparatorChar, 5);
        String[] strArr = GetEntries(strTempDir);
        Assert.NotNull(strArr);
        Assert.NotEmpty(strArr);
    }

    [Fact]
    public void CurrentDirectory()
    {
        String[] strArr = GetEntries(Directory.GetCurrentDirectory());
        Assert.NotNull(strArr);
        Assert.NotEmpty(strArr);
    }

    [Fact]
    public void ValidDirectoryEmpty()
    {
        String testDir = Path.Combine(TestDirectory, GetTestFilePath());
        new DirectoryInfo(testDir).Create();
        String[] strArr = GetEntries(testDir);
        Assert.NotNull(strArr);
        Assert.Empty(strArr);
    }

    [Fact]
    public void ValidDirectoryNotEmpty()
    {
        using (FileStream fs1 = new FileInfo(Path.Combine(TestDirectory, GetTestFilePath())).Create())
        {
            String[] strArr = GetEntries(TestDirectory);
            Assert.NotNull(strArr);
            Assert.NotEmpty(strArr);
        }
    }

    [Fact]
    public void NonexistentPath()
    {
        Assert.Throws<DirectoryNotFoundException>(() => GetEntries("ThisDirectoryShouldNotExist"));
    }

    [Fact]
    public void SubDirectories()
    {
        DirectoryInfo testDir = new DirectoryInfo(TestDirectory);
        testDir.CreateSubdirectory("TestDir1");
        testDir.CreateSubdirectory("TestDir2");
        testDir.CreateSubdirectory("TestDir3");
        using (new FileInfo(Path.Combine(TestDirectory, "TestFile1")).Create())
        using (new FileInfo(Path.Combine(TestDirectory, "TestFile2")).Create())
        using (new FileInfo(Path.Combine(TestDirectory, "Test1File2")).Create())
        using (new FileInfo(Path.Combine(TestDirectory, "Test1Dir2")).Create())
        {
            String[] strArr = GetEntries(TestDirectory);
            Assert.Equal(7, strArr.Length);
            Assert.Contains(Path.Combine(TestDirectory, "TestDir1"), strArr);
            Assert.Contains(Path.Combine(TestDirectory, "TestDir2"), strArr);
            Assert.Contains(Path.Combine(TestDirectory, "TestDir3"), strArr);
            Assert.Contains(Path.Combine(TestDirectory, "Test1File2"), strArr);
            Assert.Contains(Path.Combine(TestDirectory, "Test1Dir2"), strArr);
            Assert.Contains(Path.Combine(TestDirectory, "TestFile1"), strArr);
            Assert.Contains(Path.Combine(TestDirectory, "TestFile2"), strArr);
        }
    }

    [Fact]
    public void IgnoreSubDirectoryFiles()
    {
        String testDir1Str = GetTestFileName();
        String testDir11Str = GetTestFileName();
        DirectoryInfo testDir = new DirectoryInfo(TestDirectory);
        DirectoryInfo testDir1 = testDir.CreateSubdirectory(testDir1Str);
        testDir1.CreateSubdirectory(testDir11Str);
        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, testDir11Str, GetTestFileName())).Create())
        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, GetTestFileName())).Create())
        {
            Assert.Equal(2, GetEntries(Path.Combine(TestDirectory, testDir1Str)).Length);
        }
    }

    #endregion

    #region WindowsOnly

    [Fact]
    [PlatformSpecific(PlatformID.Windows)]
    public void WildCharactersFileName()
    {
        String strTempDir = Path.Combine("dls;d", "442349-0", "v443094(*)(+*$#$*") + new string(Path.DirectorySeparatorChar, 3);
        Assert.Throws<ArgumentException>(() => GetEntries(strTempDir));
    }

    #endregion
}




