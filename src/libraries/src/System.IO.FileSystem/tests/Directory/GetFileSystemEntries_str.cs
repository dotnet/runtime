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
        Assert.Throws<ArgumentException>(() => GetEntries(String.Empty));
    }

    [Fact]
    public void InvalidFileNames()
    {
        DirectoryInfo testDir = new DirectoryInfo(TestDirectory);
        Assert.Throws<DirectoryNotFoundException>(() => GetEntries(","));
        Assert.Throws<DirectoryNotFoundException>(() => GetEntries("DoesNotExist"));
    }

    [Fact]
    public void NonexistentFile()
    {
        Assert.Throws<DirectoryNotFoundException>(() => GetEntries(GetTestFilePath()));
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
        String testDir = Path.Combine(TestDirectory, GetTestFileName());
        new DirectoryInfo(testDir).Create();
        String[] strArr = GetEntries(testDir);
        Assert.NotNull(strArr);
        Assert.Empty(strArr);
    }

    [Fact]
    public void ValidDirectoryNotEmpty()
    {
        using (File.Create(Path.Combine(TestDirectory, GetTestFileName())))
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
    public void GetEntriesThenDelete()
    {
        String testDirPath = Path.Combine(TestDirectory, GetTestFilePath());
        DirectoryInfo testDirInfo = new DirectoryInfo(testDirPath);
        testDirInfo.Create();
        String testDir1 = GetTestFileName();
        String testDir2 = GetTestFileName();
        String testFile1 = GetTestFileName();
        String testFile2 = GetTestFileName();
        String testFile3 = GetTestFileName();
        String testFile4 = GetTestFileName();
        String testFile5 = GetTestFileName();
        testDirInfo.CreateSubdirectory(testDir1);
        testDirInfo.CreateSubdirectory(testDir2);
        using (File.Create(Path.Combine(testDirPath, testFile1)))
        using (File.Create(Path.Combine(testDirPath, testFile2)))
        using (File.Create(Path.Combine(testDirPath, testFile3)))
        {
            String[] results;
            using (File.Create(Path.Combine(testDirPath, testFile4)))
            using (File.Create(Path.Combine(testDirPath, testFile5)))
            {
                results = GetEntries(testDirPath);
                Assert.NotNull(results);
                Assert.NotEmpty(results);
                Assert.Contains(Path.Combine(testDirPath, testFile1), results);
                Assert.Contains(Path.Combine(testDirPath, testFile2), results);
                Assert.Contains(Path.Combine(testDirPath, testFile3), results);
                Assert.Contains(Path.Combine(testDirPath, testFile4), results);
                Assert.Contains(Path.Combine(testDirPath, testFile5), results);
                Assert.Contains(Path.Combine(testDirPath, testDir1), results);
                Assert.Contains(Path.Combine(testDirPath, testDir2), results);
            }

            File.Delete(Path.Combine(testDirPath, testFile4));
            File.Delete(Path.Combine(testDirPath, testFile5));
            FailSafeDirectoryOperations.DeleteDirectory(testDir1, true);

            results = GetEntries(testDirPath);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            Assert.Contains(Path.Combine(testDirPath, testFile1), results);
            Assert.Contains(Path.Combine(testDirPath, testFile2), results);
            Assert.Contains(Path.Combine(testDirPath, testFile3), results);
            Assert.Contains(Path.Combine(testDirPath, testDir2), results);
        }
    }

    [Fact]
    public void IgnoreSubDirectoryFiles()
    {
        String testDir = GetTestFileName();
        String testFile1 = Path.Combine(TestDirectory, GetTestFileName());
        String testFile2 = Path.Combine(TestDirectory, testDir, GetTestFileName());
        Directory.CreateDirectory(Path.Combine(TestDirectory, testDir));
        using (File.Create(testFile1))
        using (File.Create(testFile2))
        {
            String[] results = GetEntries(TestDirectory);
            Assert.Contains(testFile1, results);
            Assert.DoesNotContain(testFile2, results);
        }
    }

    #endregion

    #region PlatformSpecific

    [Fact]
    [PlatformSpecific(PlatformID.Windows)]
    public void WildCharactersFileName()
    {
        String strTempDir = Path.Combine("dls;d", "442349-0", "v443094(*)(+*$#$*") + new string(Path.DirectorySeparatorChar, 3);
        Assert.Throws<ArgumentException>(() => GetEntries(strTempDir));
    }

    [Fact]
    [PlatformSpecific(PlatformID.Windows)]
    public void WindowsFileNameWithSpaces()
    {
        Assert.Throws<ArgumentException>(() => GetEntries("\n"));
        Assert.Throws<ArgumentException>(() => GetEntries("          "));
        Assert.Throws<ArgumentException>(() => GetEntries(" "));
        Assert.Throws<ArgumentException>(() => GetEntries(""));
        Assert.Throws<ArgumentException>(() => GetEntries(">"));
        Assert.Throws<ArgumentException>(() => GetEntries("\0"));
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void UnixFileNameWithSpaces()
    {
        String testDirPath = Path.Combine(TestDirectory, GetTestFileName());
        Directory.CreateDirectory(testDirPath);
        using (File.Create(Path.Combine(testDirPath, " ")))
        using (File.Create(Path.Combine(testDirPath, "          ")))
        using (File.Create(Path.Combine(testDirPath, "\n")))
        {
            String[] results = GetEntries(testDirPath);
            Assert.Contains(Path.Combine(testDirPath, " "), results);
            Assert.Contains(Path.Combine(testDirPath, "          "), results);
            Assert.Contains(Path.Combine(testDirPath, "\n"), results);
        }
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void UnixDirectoryNameWithSpaces()
    {
        String testDirPath = Path.Combine(TestDirectory, GetTestFileName());
        DirectoryInfo testDir = new DirectoryInfo(testDirPath);
        testDir.CreateSubdirectory(" ");
        testDir.CreateSubdirectory("\n");
        testDir.CreateSubdirectory("          ");

        String[] results = GetEntries(testDirPath);
        Assert.Contains(Path.Combine(testDirPath, " "), results);
        Assert.Contains(Path.Combine(testDirPath, "          "), results);
        Assert.Contains(Path.Combine(testDirPath, "\n"), results);

        testDir.Delete(true);
    }
    #endregion
}

