// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Xunit;

public class Directory_GetFiles_str_str : Directory_GetFiles_str
{
    #region Utilities

    public override String[] GetFiles(String path)
    {
        return Directory.GetFiles(path, "*");
    }

    public virtual String[] GetFiles(String path, String searchPattern)
    {
        return Directory.GetFiles(path, searchPattern);
    }

    #endregion

    #region UniversalTests

    [Fact]
    public void NullSearchPattern()
    {
        Assert.Throws<ArgumentNullException>(() => GetFiles(".", null));
    }

    [Fact]
    public void EmptySearchPattern()
    {
        Assert.Empty(GetFiles(".", String.Empty));
    }

    [Fact]
    public void WhitespaceSearchPattern()
    {
        Assert.Empty(GetFiles(TestDirectory, "           "));
        Assert.Empty(GetFiles(TestDirectory, "\n"));
    }

    [Fact]
    public void InvalidSearchPattern()
    {
        // '<' and '>' are valid Unix filename chars
        if (Interop.IsWindows)
        {
            Assert.Throws<ArgumentException>(() => GetFiles(".", "<>"));
        }
        Assert.Throws<ArgumentException>(() => GetFiles(".", ".."));
        Assert.Throws<ArgumentException>(() => GetFiles(".", @".." + Path.DirectorySeparatorChar));

        Char[] invalidFileNames = Interop.IsWindows ? Path.GetInvalidFileNameChars() : new[] { '\0' };
        for (int i = 0; i < invalidFileNames.Length; i++)
        {
            switch (invalidFileNames[i])
            {
                case '\\':
                case '/':
                    Assert.Throws<DirectoryNotFoundException>(() => GetFiles(Directory.GetCurrentDirectory(), String.Format("te{0}st", invalidFileNames[i].ToString())));
                    break;
                //We dont throw in V1 too
                case ':':
                    //History:
                    // 1) we assumed that this will work in all non-9x machine
                    // 2) Then only in XP
                    // 3) NTFS?
                    if (Interop.IsWindows && FileSystemDebugInfo.IsCurrentDriveNTFS()) // testing NTFS
                        Assert.Throws<IOException>(() => GetFiles(Directory.GetCurrentDirectory(), String.Format("te{0}st", invalidFileNames[i].ToString())));
                    else
                        GetFiles(Directory.GetCurrentDirectory(), String.Format("te{0}st", invalidFileNames[i].ToString()));

                    break;
                case '*':
                case '?':
                    GetFiles(Directory.GetCurrentDirectory(), String.Format("te{0}st", invalidFileNames[i].ToString()));
                    break;
                default:
                    Assert.Throws<ArgumentException>(() => GetFiles(Directory.GetCurrentDirectory(), String.Format("te{0}st", invalidFileNames[i].ToString())));
                    break;
            }
        }
        //path too long
        Assert.Throws<PathTooLongException>(() => GetFiles(Path.Combine(new String('a', IOInputs.MaxPath), new String('b', IOInputs.MaxPath)), "*.*"));
        Assert.Throws<PathTooLongException>(() => GetFiles(new String('a', IOInputs.MaxPath), new String('b', IOInputs.MaxPath)));
    }

    [Fact]
    public void WildCardTests()
    {
        String testDirPath = Path.Combine(TestDirectory, GetTestFilePath());
        DirectoryInfo testDirInfo = new DirectoryInfo(testDirPath);
        testDirInfo.Create();
        using (File.Create(Path.Combine(testDirPath, "TestFile1")))
        using (File.Create(Path.Combine(testDirPath, "TestFile2")))
        using (File.Create(Path.Combine(testDirPath, "TestFile3")))
        using (File.Create(Path.Combine(testDirPath, "Test1File1")))
        using (File.Create(Path.Combine(testDirPath, "Test1File2")))
        {
            String[] results;

            results = GetFiles(testDirPath, "TestFile*");
            Assert.Equal(3, results.Length);
            Assert.Contains(Path.Combine(testDirPath, "TestFile1"), results);
            Assert.Contains(Path.Combine(testDirPath, "TestFile2"), results);
            Assert.Contains(Path.Combine(testDirPath, "TestFile3"), results);

            results = GetFiles(testDirPath, "*");
            Assert.Equal(5, results.Length);
            Assert.Contains(Path.Combine(testDirPath, "TestFile1"), results);
            Assert.Contains(Path.Combine(testDirPath, "TestFile2"), results);
            Assert.Contains(Path.Combine(testDirPath, "TestFile3"), results);
            Assert.Contains(Path.Combine(testDirPath, "Test1File1"), results);
            Assert.Contains(Path.Combine(testDirPath, "Test1File2"), results);

            results = GetFiles(testDirPath, "*File2");
            Assert.Equal(2, results.Length);
            Assert.Contains(Path.Combine(testDirPath, "TestFile2"), results);
            Assert.Contains(Path.Combine(testDirPath, "Test1File2"), results);
        }
    }

    [Fact]
    public void CaseSensitivity()
    {
        String testDirPath = Path.Combine(TestDirectory, GetTestFilePath());
        DirectoryInfo testDirInfo = new DirectoryInfo(testDirPath);
        testDirInfo.Create();
        String testFile1 = GetTestFileName() + "AAABB";
        String testFile2 = GetTestFileName() + "aaabb";
        using (File.Create(Path.Combine(testDirPath, testFile1)))
        using (File.Create(Path.Combine(testDirPath, testFile2)))
        {
            String[] results = GetFiles(testDirPath, "*BB*");
            if (Interop.IsWindows || Interop.IsOSX)
            {
                Assert.Equal(2, results.Length);
                Assert.Contains(Path.Combine(testDirPath, testFile1), results);
                Assert.Contains(Path.Combine(testDirPath, testFile2), results);
            }
            else
            {
                Assert.Equal(1, results.Length);
                Assert.Contains(Path.Combine(testDirPath, testFile1), results);
            }
        }
    }

    [Fact]
    public void ExactSearchStringMatch()
    {
        String testDirPath = Path.Combine(TestDirectory, GetTestFilePath());
        DirectoryInfo testDirInfo = new DirectoryInfo(testDirPath);
        testDirInfo.Create();
        using (File.Create(Path.Combine(testDirPath, "AAABB")))
        using (File.Create(Path.Combine(testDirPath, "AAABBC")))
        using (File.Create(Path.Combine(testDirPath, "CAAABB")))
        {
            String[] results = GetFiles(testDirPath, "AAABB");
            Assert.Equal(1, results.Length);
            Assert.Contains(Path.Combine(testDirPath, "AAABB"), results);
        }
    }

    [Fact]
    public void SearchPatternNonFullPath()
    {
        //Shouldn't get files on full path by default
        String testDir1Str = GetTestFileName();
        String testFile1Str = GetTestFileName();
        DirectoryInfo testDir = new DirectoryInfo(TestDirectory);
        testDir.CreateSubdirectory(testDir1Str);
        using (File.Create(Path.Combine(TestDirectory, testDir1Str, testFile1Str)))
        {
            Assert.Equal(1, GetFiles(TestDirectory, Path.Combine(testDir1Str, "*")).Length);
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
            String[] results = GetFiles(Directory.GetCurrentDirectory(), Path.Combine(new DirectoryInfo(TestDirectory).Name, "*"));
            Assert.Contains(testFile1, results);
            Assert.DoesNotContain(testFile2, results);
        }
    }

    [Fact]
    public void SearchPatternSubset()
    {
        String testDir1Str = GetTestFileName();
        String testDir11Str = GetTestFileName();
        DirectoryInfo testDir = new DirectoryInfo(TestDirectory);
        DirectoryInfo testDir1 = testDir.CreateSubdirectory(testDir1Str);
        testDir1.CreateSubdirectory(testDir11Str);

        String testFile1 = GetTestFileName();
        String testFile2 = "ca1" + testFile1;
        testFile1 = "ca2" + testFile1;
        String testFile3 = GetTestFileName();
        String searchPattern = String.Format("???{0}", testFile1.Substring(3));

        using (File.Create(Path.Combine("\\\\?\\", TestDirectory, testDir1Str, testDir11Str, GetTestFileName())))
        using (File.Create(Path.Combine(TestDirectory, testDir1Str, testFile1)))
        using (File.Create(Path.Combine(TestDirectory, testDir1Str, testFile2)))
        using (File.Create(Path.Combine(TestDirectory, testDir1Str, testFile3)))
        {
            String[] results = GetFiles(Path.Combine(TestDirectory, testDir1Str), searchPattern);
            Assert.Equal(2, results.Length);
            Assert.Contains(Path.Combine(TestDirectory, testDir1Str, testFile1), results);
            Assert.Contains(Path.Combine(TestDirectory, testDir1Str, testFile2), results);

            //suffix only should return the empty array
            Assert.Empty(GetFiles(Path.Combine(TestDirectory, testDir1Str), testFile1.Substring(3)));
        }
    }

    [Fact]
    [ActiveIssue(846, PlatformID.AnyUnix)]
    public void CharacterTests()
    {
        //bug #417100 - not sure if this hard coded approach is safe in all 9x platforms!!!

        int[] validGreaterThan128ButLessThans160 = { 129, 133, 141, 143, 144, 157 };
        for (int i = 0; i < validGreaterThan128ButLessThans160.Length; i++)
        {
            GetFiles(".", ((Char)validGreaterThan128ButLessThans160[i]).ToString());
        }

        for (int i = 160; i < 256; i++)
        {
            GetFiles(".", ((Char)i).ToString());
        }
    }

    #endregion

    #region PlatformSpecific

    [Fact]
    [PlatformSpecific(PlatformID.Windows)]
    public void WildCharactersSearchPattern()
    {
        String strTempDir = Path.Combine("dls;d", "442349-0", "v443094(*)(+*$#$*") + new string(Path.DirectorySeparatorChar, 3);
        Assert.Throws<ArgumentException>(() => GetFiles(TestDirectory, strTempDir));
    }

    [Fact]
    [PlatformSpecific(PlatformID.Windows)]
    public void SearchPatternQuestionMarks()
    {
        String testDir1Str = GetTestFileName();
        String testDir11Str = GetTestFileName();
        DirectoryInfo testDir = new DirectoryInfo(TestDirectory);
        DirectoryInfo testDir1 = testDir.CreateSubdirectory(testDir1Str);
        testDir1.CreateSubdirectory(testDir11Str);

        using (File.Create(Path.Combine(TestDirectory, testDir1Str, testDir11Str, GetTestFileName())))
        using (File.Create(Path.Combine(TestDirectory, GetTestFileName())))
        using (File.Create(Path.Combine(TestDirectory, GetTestFileName())))
        {
            Assert.Equal(2, GetFiles(TestDirectory, String.Format("{0}.???", new String('?', GetTestFileName().Length))).Length);
        }
    }

    #endregion
}
