// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

public class Directory_GetFileSystemEntries_str_str : Directory_GetFileSystemEntries_str
{
    #region Utilities

    /// <summary>
    /// Makes all parent tests run with a wildcard Search Parameter
    /// </summary>
    public override String[] GetEntries(String dirName)
    {
        return Directory.GetFileSystemEntries(dirName, "*");
    }

    /// <summary>
    /// Indirection around Directory.GetFileSystemEntries that child test classes may
    /// replace with a more fitting version
    /// </summary>
    public virtual String[] GetEntries(String dirName, String searchPattern)
    {
        return Directory.GetFileSystemEntries(dirName, searchPattern);
    }

    #endregion

    #region UniversalTests

    [Fact]
    public void NullSearchPattern()
    {
        Assert.Throws<ArgumentNullException>(() => GetEntries(TestDirectory, null));
    }

    [Fact]
    public void EmptySearchPattern()
    {
        // To avoid OS differences we have decided not to throw an argument exception when empty
        // string passed. But we should return 0 items.
        Assert.Empty(GetEntries(TestDirectory, ""));
    }

    [Fact]
    public void ValidSearchPattern()
    {
        String strTempDir = "a..b abc..d";
        GetEntries(TestDirectory, strTempDir); //Should not throw
    }

    [Fact]
    public void InvalidSearchPattern()
    {
        String strTempDir = Path.Combine("..ab ab.. .. abc..d", "abc..");
        Assert.Throws<ArgumentException>(() => GetEntries(TestDirectory, strTempDir));
    }

    [Fact]
    public void SearchPatternWithSpaces()
    {
        Assert.Empty(GetEntries(TestDirectory, "           "));
        Assert.Empty(GetEntries(TestDirectory, "\n"));
    }

    [Fact]
    public void SearchPatternWithTrailingStar()
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
            String[] strArr = GetEntries(TestDirectory, "Test1*");
            Assert.Equal(2, strArr.Length);
            Assert.Contains(Path.Combine(TestDirectory, "Test1File2"), strArr);
            Assert.Contains(Path.Combine(TestDirectory, "Test1Dir2"), strArr);
        }
    }

    [Fact]
    public void SearchPatternWithLeadingStar()
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
            String[] strArr = GetEntries(TestDirectory, "*2");
            Assert.Equal(4, strArr.Length);
            Assert.Contains(Path.Combine(TestDirectory, "Test1Dir2"), strArr);
            Assert.Contains(Path.Combine(TestDirectory, "TestDir2"), strArr);
            Assert.Contains(Path.Combine(TestDirectory, "Test1File2"), strArr);
            Assert.Contains(Path.Combine(TestDirectory, "TestFile2"), strArr);

            strArr = GetEntries(TestDirectory, "*Dir2");
            Assert.Equal(2, strArr.Length);
            Assert.Contains(Path.Combine(TestDirectory, "Test1Dir2"), strArr);
            Assert.Contains(Path.Combine(TestDirectory, "TestDir2"), strArr);
        }
    }

    [Fact]
    public void SearchPatternCaseSensitivity()
    {
        DirectoryInfo testDir = new DirectoryInfo(TestDirectory);
        using (new FileInfo(Path.Combine(TestDirectory, "AAABB")).Create())
        {
            Directory.CreateDirectory(Path.Combine(TestDirectory, "aaabbcc"));
            String[] strArr = GetEntries(TestDirectory, "*BB*");
            Assert.Contains(Path.Combine(TestDirectory, "AAABB"), strArr);
            if (Interop.IsWindows)
                Assert.Contains(Path.Combine(TestDirectory, "aaabbcc"), strArr);
            else
                Assert.DoesNotContain(Path.Combine(TestDirectory, "aaabbcc"), strArr);
        }
    }

    [Fact]
    public void SearchPatternNonFullPath()
    {
        //Shouldn't get files on full path by default
        String testDir1Str = GetTestFileName();
        DirectoryInfo testDir = new DirectoryInfo(TestDirectory);
        testDir.CreateSubdirectory(testDir1Str);
        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, GetTestFileName())).Create())
        {
            Assert.Equal(1, GetEntries(TestDirectory, Path.Combine(testDir1Str, "*")).Length);
        }
    }

    [Fact]
    public void SearchPatternIgnoreSubDirectories()
    {
        String testDir1Str = GetTestFileName();
        String testDir11Str = GetTestFileName();
        DirectoryInfo testDir = new DirectoryInfo(TestDirectory);
        DirectoryInfo testDir1 = testDir.CreateSubdirectory(testDir1Str);
        testDir1.CreateSubdirectory(testDir11Str);

        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, testDir11Str, GetTestFileName())).Create())
        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, GetTestFileName())).Create())
        {
            Assert.Equal(2, GetEntries(TestDirectory, Path.Combine(testDir1Str, "*")).Length);
        }
    }

    #endregion

    #region WindowsOnly

    [Fact]
    [PlatformSpecific(PlatformID.Windows)]
    public void WildCharactersSearchPattern()
    {
        String strTempDir = Path.Combine("dls;d", "442349-0", "v443094(*)(+*$#$*") + new string(Path.DirectorySeparatorChar, 3);
        Assert.Throws<ArgumentException>(() => GetEntries(TestDirectory, strTempDir));
    }

    #endregion
}



