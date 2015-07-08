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
        return System.IO.Directory.GetFileSystemEntries(dirName, "*");
    }

    /// <summary>
    /// Indirection around Directory.GetFileSystemEntries that child test classes may
    /// replace with a more fitting version
    /// </summary>
    public virtual String[] GetEntries(String dirName, String searchPattern)
    {
        return System.IO.Directory.GetFileSystemEntries(dirName, searchPattern);
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
        Assert.Empty(GetEntries(TestDirectory, strTempDir)); //Should not throw
    }

    [Fact]
    public void SearchPatternWithTrailingStar()
    {
        DirectoryInfo testDir = new DirectoryInfo(TestDirectory);
        testDir.CreateSubdirectory("TestDir1");
        testDir.CreateSubdirectory("TestDir2");
        testDir.CreateSubdirectory("TestDir3");
        using (File.Create(Path.Combine(TestDirectory, "TestFile1")))
        using (File.Create(Path.Combine(TestDirectory, "TestFile2")))
        using (File.Create(Path.Combine(TestDirectory, "Test1File2")))
        using (File.Create(Path.Combine(TestDirectory, "Test1Dir2")))
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
        using (File.Create(Path.Combine(TestDirectory, "TestFile1")))
        using (File.Create(Path.Combine(TestDirectory, "TestFile2")))
        using (File.Create(Path.Combine(TestDirectory, "Test1File2")))
        using (File.Create(Path.Combine(TestDirectory, "Test1Dir2")))
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
        using (File.Create(Path.Combine(TestDirectory, "AAABB")))
        {
            System.IO.Directory.CreateDirectory(Path.Combine(TestDirectory, "aaabbcc"));
            String[] strArr = GetEntries(TestDirectory, "*BB*");
            Assert.Contains(Path.Combine(TestDirectory, "AAABB"), strArr);
            if (Interop.IsWindows || Interop.IsOSX)
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
        using (File.Create(Path.Combine(TestDirectory, testDir1Str, GetTestFileName())))
        {
            Assert.Equal(1, GetEntries(TestDirectory, Path.Combine(testDir1Str, "*")).Length);
        }
    }

    [Fact]
    public void SearchPatternIgnoreSubDirectories()
    {
        String testDir = GetTestFileName();
        String testFile1 = Path.Combine(TestDirectory, GetTestFileName());
        String testFile2 = Path.Combine(TestDirectory, testDir, GetTestFileName());
        Directory.CreateDirectory(Path.Combine(TestDirectory, testDir));
        using (File.Create(testFile1))
        using (File.Create(testFile2))
        {
            String[] results = GetEntries(Directory.GetCurrentDirectory(), Path.Combine(new DirectoryInfo(TestDirectory).Name, "*"));
            Assert.Contains(testFile1, results);
            Assert.DoesNotContain(testFile2, results);
        }
    }

    #endregion

    #region WindowsOnly

    [Fact]
    [PlatformSpecific(PlatformID.Windows)]
    public void WindowsWildCharactersSearchPattern()
    {
        String strTempDir = Path.Combine("dls;d", "442349-0", "v443094(*)(+*$#$*") +
                            new string(Path.DirectorySeparatorChar, 3);
        Assert.Throws<ArgumentException>(() => GetEntries(TestDirectory, strTempDir));
    }

    [Fact]
    [PlatformSpecific(PlatformID.Windows)]
    public void WindowsSearchPatternWithSpaces()
    {
        Assert.Empty(GetEntries(TestDirectory, "           "));
        Assert.Empty(GetEntries(TestDirectory, "\n"));
        Assert.Empty(GetEntries(TestDirectory, " "));
        Assert.Empty(GetEntries(TestDirectory, ""));
        Assert.Throws<ArgumentException>(() => GetEntries(TestDirectory, "\0"));
        Assert.Throws<ArgumentException>(() => GetEntries(TestDirectory, ">"));
    }

    [Fact]
    [PlatformSpecific(PlatformID.Windows)]
    public void WindowsSearchPatternWithDoubleDots()
    {
        string strTempDir = Path.Combine("..ab ab.. .. abc..d", "abc..");
        Assert.Throws<ArgumentException>(() => GetEntries(TestDirectory, strTempDir));
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void UnixFilePathWithSpaces()
    {
        Assert.Throws<ArgumentException>(() => GetEntries("\0"));
        using (File.Create(Path.Combine(TestDirectory, " ")))
        using (File.Create(Path.Combine(TestDirectory, "          ")))
        using (File.Create(Path.Combine(TestDirectory, "\n")))
        {
            String[] results = GetEntries(TestDirectory);
            Assert.Contains(Path.Combine(TestDirectory, " "), results);
            Assert.Contains(Path.Combine(TestDirectory, "          "), results);
            Assert.Contains(Path.Combine(TestDirectory, "\n"), results);
        }
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void UnixSearchPatternWithSpaces()
    {
        Assert.Empty(GetEntries(TestDirectory, "           "));
        Assert.Empty(GetEntries(TestDirectory, "\n"));
        Assert.Empty(GetEntries(TestDirectory, " "));
        Assert.Empty(GetEntries(TestDirectory, ""));
        Assert.Throws<ArgumentException>(() => GetEntries(TestDirectory, "\0"));
    }

    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public void UnixSearchPatternWithDoubleDots()
    {
        // search pattern is valid but directory doesn't exist
        Assert.Throws<DirectoryNotFoundException>(() => GetEntries(TestDirectory, Path.Combine("..ab ab.. .. abc..d", "abc..")));

        // invalid search pattern trying to go up a directory with ..
        Assert.Throws<ArgumentException>(() => GetEntries(TestDirectory, Path.Combine("..ab ab.. .. abc..d", "abc", "..")));
        Assert.Throws<ArgumentException>(() => GetEntries(TestDirectory, Path.Combine("..ab ab.. .. abc..d", "..", "abc")));
        Assert.Throws<ArgumentException>(() => GetEntries(TestDirectory, Path.Combine("..", "..ab ab.. .. abc..d", "abc")));
        Assert.Throws<ArgumentException>(() => GetEntries(TestDirectory, Path.Combine("..", "..ab ab.. .. abc..d", "abc") + Path.DirectorySeparatorChar));
    }

    #endregion
}


