// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.FileSystem.Tests;
using Xunit;

public class Directory_GetFiles_str : FileSystemTest
{
    #region Utilities

    public virtual String[] GetFiles(String path)
    {
        return Directory.GetFiles(path);
    }

    #endregion

    #region UniversalTests

    [Fact]
    public void NullPath()
    {
        Assert.Throws<ArgumentNullException>(() => GetFiles(null));
    }

    [Fact]
    public void EmptyPath()
    {
        Assert.Throws<ArgumentException>(() => GetFiles(String.Empty));
    }

    [Fact]
    public void InvalidFileNames()
    {
        DirectoryInfo testDir = new DirectoryInfo(TestDirectory);
        Assert.Throws<DirectoryNotFoundException>(() => GetFiles(","));
        Assert.Throws<DirectoryNotFoundException>(() => GetFiles("DoesNotExist"));
    }

    [Fact]
    public void WhitespacePath()
    {
        Assert.Throws<ArgumentException>(() => GetFiles("\n"));
        Assert.Throws<ArgumentException>(() => GetFiles("          "));
        Assert.Throws<ArgumentException>(() => GetFiles(" "));
        Assert.Throws<ArgumentException>(() => GetFiles(""));
        if (Interop.IsWindows)
            Assert.Throws<ArgumentException>(() => GetFiles(">"));
        else
            Assert.Throws<ArgumentException>(() => GetFiles("\0"));
    }

    [Fact]
    public void EmptyDirectory()
    {
        String testDir = Path.Combine(TestDirectory, GetTestFilePath());
        new DirectoryInfo(testDir).Create();
        Assert.Empty(GetFiles(testDir));
    }

    [Fact]
    public void GetFilesThenDelete()
    {
        String testDirPath = Path.Combine(TestDirectory, GetTestFilePath());
        DirectoryInfo testDirInfo = new DirectoryInfo(testDirPath);
        testDirInfo.Create();
        String testDir1 = GetTestFileName();
        String testDir2 = GetTestFileName();
        String testDir3 = GetTestFileName();
        String testDir4 = GetTestFileName();
        String testDir5 = GetTestFileName();
        String testFile1 = GetTestFileName();
        String testFile2 = GetTestFileName();
        String testFile3 = GetTestFileName();
        String testFile4 = GetTestFileName();
        String testFile5 = GetTestFileName();
        testDirInfo.CreateSubdirectory(testDir1);
        testDirInfo.CreateSubdirectory(testDir2);
        testDirInfo.CreateSubdirectory(testDir3);
        testDirInfo.CreateSubdirectory(testDir4);
        testDirInfo.CreateSubdirectory(testDir5);
        using (new FileInfo(Path.Combine(testDirPath, testFile1)).Create())
        using (new FileInfo(Path.Combine(testDirPath, testFile2)).Create())
        using (new FileInfo(Path.Combine(testDirPath, testFile3)).Create())
        {
            String[] results;
            using (new FileInfo(Path.Combine(testDirPath, testFile4)).Create())
            using (new FileInfo(Path.Combine(testDirPath, testFile5)).Create())
            {
                results = GetFiles(testDirPath);
                Assert.NotNull(results);
                Assert.NotEmpty(results);
                Assert.Equal(5, results.Length);
                Assert.Contains(Path.Combine(testDirPath, testFile1), results);
                Assert.Contains(Path.Combine(testDirPath, testFile2), results);
                Assert.Contains(Path.Combine(testDirPath, testFile3), results);
                Assert.Contains(Path.Combine(testDirPath, testFile4), results);
                Assert.Contains(Path.Combine(testDirPath, testFile5), results);
            }

            File.Delete(Path.Combine(testDirPath, testFile4));
            File.Delete(Path.Combine(testDirPath, testFile5));

            results = GetFiles(testDirPath);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            Assert.Equal(3, results.Length);
            Assert.Contains(Path.Combine(testDirPath, testFile1), results);
            Assert.Contains(Path.Combine(testDirPath, testFile2), results);
            Assert.Contains(Path.Combine(testDirPath, testFile3), results);
        }
    }

    [Fact]
    public void IgnoreSubDirectoryFiles()
    {
        String testDir1Str = GetTestFileName();
        String testDir2Str = GetTestFileName();
        String testDir3Str = GetTestFileName();
        DirectoryInfo testDir = new DirectoryInfo(TestDirectory);
        DirectoryInfo testDir1 = testDir.CreateSubdirectory(testDir1Str);
        DirectoryInfo testDir2 = testDir1.CreateSubdirectory(testDir2Str);
        testDir2.CreateSubdirectory(testDir3Str);
        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, GetTestFileName())).Create())
        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, testDir2Str, GetTestFileName())).Create())
        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, testDir2Str, testDir3Str, GetTestFileName())).Create())
        {
            Assert.Equal(1, GetFiles(Path.Combine(TestDirectory, testDir1Str)).Length);
        }
    }

    #endregion

    #region WindowsOnly

    [Fact]
    [PlatformSpecific(PlatformID.Windows)]
    public void WildCharactersFileName()
    {
        String strTempDir = Path.Combine("dls;d", "442349-0", "v443094(*)(+*$#$*") + new string(Path.DirectorySeparatorChar, 3);
        Assert.Throws<ArgumentException>(() => GetFiles(strTempDir));
    }

    #endregion
}