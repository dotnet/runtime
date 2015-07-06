// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.FileSystem.Tests;
using System.Linq;
using System.Runtime.InteropServices;
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
        using (File.Create(Path.Combine(testDirPath, testFile1)))
        using (File.Create(Path.Combine(testDirPath, testFile2)))
        using (File.Create(Path.Combine(testDirPath, testFile3)))
        {
            String[] results;
            using (File.Create(Path.Combine(testDirPath, testFile4)))
            using (File.Create(Path.Combine(testDirPath, testFile5)))
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
        String testDir = GetTestFileName();
        String testFile1 = Path.Combine(TestDirectory, GetTestFileName());
        String testFile2 = Path.Combine(TestDirectory, testDir, GetTestFileName());
        Directory.CreateDirectory(Path.Combine(TestDirectory, testDir));
        using (File.Create(testFile1))
        using (File.Create(testFile2))
        {
            String[] results = GetFiles(TestDirectory);
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
        Assert.Throws<ArgumentException>(() => GetFiles(strTempDir));
    }

    [Fact]
    [PlatformSpecific(PlatformID.Windows)]
    public void WindowsWhitespacePath()
    {
        Assert.Throws<ArgumentException>(() => GetFiles("\n"));
        Assert.Throws<ArgumentException>(() => GetFiles("          "));
        Assert.Throws<ArgumentException>(() => GetFiles(" "));
        Assert.Throws<ArgumentException>(() => GetFiles(""));
        Assert.Throws<ArgumentException>(() => GetFiles(">"));
        Assert.Throws<ArgumentException>(() => GetFiles("\0"));
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    [ActiveIssue(2205)]
    public void UnixFilePathWithSpaces()
    {
        Assert.Throws<ArgumentException>(() => GetFiles("\0"));
        using (File.Create(Path.Combine(TestDirectory, " ")))
        using (File.Create(Path.Combine(TestDirectory, "          ")))
        using (File.Create(Path.Combine(TestDirectory, "\n")))
        {
            String[] results = GetFiles(TestDirectory);
            Assert.Contains(Path.Combine(TestDirectory, " "), results);
            Assert.Contains(Path.Combine(TestDirectory, "          "), results);
            Assert.Contains(Path.Combine(TestDirectory, "\n"), results);
        }
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void EnumerateWithSymLinkToFile()
    {
        using (var containingFolder = new TemporaryDirectory())
        {
            string linkPath;

            // Test a symlink to a file that does and then doesn't exist
            using (var targetFile = new TemporaryFile())
            {
                linkPath = Path.Combine(containingFolder.Path, Path.GetRandomFileName());
                Assert.Equal(0, symlink(targetFile.Path, linkPath));
                Assert.True(File.Exists(linkPath));
                Assert.Equal(1, GetFiles(containingFolder.Path).Count());
            }

            // The symlink still exists even though the target file is gone.
            Assert.Equal(1, GetFiles(containingFolder.Path).Count());

            // The symlink is gone
            File.Delete(linkPath);
            Assert.Equal(0, GetFiles(containingFolder.Path).Count());
        }
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int symlink(string path1, string path2);
    
    #endregion
}
