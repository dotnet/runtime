// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
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
        using (new FileInfo(Path.Combine(testDirPath, "TestFile1")).Create())
        using (new FileInfo(Path.Combine(testDirPath, "TestFile2")).Create())
        using (new FileInfo(Path.Combine(testDirPath, "TestFile3")).Create())
        using (new FileInfo(Path.Combine(testDirPath, "Test1File1")).Create())
        using (new FileInfo(Path.Combine(testDirPath, "Test1File2")).Create())
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
        using (new FileInfo(Path.Combine(testDirPath, testFile1)).Create())
        using (new FileInfo(Path.Combine(testDirPath, testFile2)).Create())
        {
            String[] results = GetFiles(testDirPath, "*BB*");
            if (Interop.IsWindows)
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
        using (new FileInfo(Path.Combine(testDirPath, "AAABB")).Create())
        using (new FileInfo(Path.Combine(testDirPath, "AAABBC")).Create())
        using (new FileInfo(Path.Combine(testDirPath, "CAAABB")).Create())
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
        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, testFile1Str)).Create())
        {
            Assert.Equal(1, GetFiles(TestDirectory, Path.Combine(testDir1Str, "*")).Length);
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
            Assert.Equal(1, GetFiles(TestDirectory, Path.Combine(testDir1Str, "*")).Length);
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

        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, testDir11Str, GetTestFileName())).Create())
        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, testFile1)).Create())
        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, testFile2)).Create())
        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, testFile3)).Create())
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
    public void SearchPatternSuperset()
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

        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, testDir11Str, GetTestFileName())).Create())
        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, testFile1)).Create())
        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, testFile2)).Create())
        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, testFile3)).Create())
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
        // TODO: [ActiveIssue(846)] Globalization on Unix
        //bug #417100 - not sure if this hard coded approach is safe in all 9x platforms!!!
        //But RunAnotherScenario is probably more accurate

        int[] validGreaterThan128ButLessThans160 = { 129, 133, 141, 143, 144, 157 };
        for (int i = 0; i < validGreaterThan128ButLessThans160.Length; i++)
        {
            GetFiles(".", ((Char)validGreaterThan128ButLessThans160[i]).ToString());
        }

        for (int i = 160; i < 256; i++)
        {
            GetFiles(".", ((Char)i).ToString());
        }

#if DESKTOP
            try {
                if(!RunAnotherScenario())
                {
                    iCountErrors++;
                    printerr( "Error_2937efg! RunAnotherScenario failed");
                }
            } catch (Exception exc) {
                iCountErrors++;
                printerr( "Err_960tli! Incorrect exception thrown, exc=="+exc.ToString());
            }
#endif
    }

    [Fact]
    public void ShortFileNames()
    {
        //bug VSWhdibey #580357 - A customer ran into an issue with Directory.GetFiles and short file name
        //We search for files with "2006" and get some even though the directory doesn't contain any files with 2006

        String[] files = { "20050512_1600_ImpressionPart106_ClickSummary.DAT", "20050512_1600_ImpressionPart126_ClickSummary.DAT", "20050512_1600_ImpressionPart40_ClickSummary.DAT", "20050512_1600_ImpressionPart42_ClickSummary.DAT", "20050512_1600_ImpressionPart44_ClickSummary.DAT", "20050512_1600_ImpressionPart46_ClickSummary.DAT", "20050512_1600_ImpressionPart48_ClickSummary.DAT", "20050512_1600_ImpressionPart50_ClickSummary.DAT", "20050512_1600_ImpressionPart52_ClickSummary.DAT", "20050512_1600_ImpressionPart54_ClickSummary.DAT", "20050512_1600_ImpressionPart56_ClickSummary.DAT", "20050512_1600_ImpressionPart58_ClickSummary.DAT", "20050513_1400_ImpressionPart116_ClickSummary.DAT", "20050513_1400_ImpressionPart41_ClickSummary.DAT", "20050513_1400_ImpressionPart43_ClickSummary.DAT", "20050513_1400_ImpressionPart45_ClickSummary.DAT", "20050513_1400_ImpressionPart47_ClickSummary.DAT", "20050513_1400_ImpressionPart49_ClickSummary.DAT", "20050513_1400_ImpressionPart51_ClickSummary.DAT", "20050513_1400_ImpressionPart53_ClickSummary.DAT", "20050513_1400_ImpressionPart55_ClickSummary.DAT", "20050513_1400_ImpressionPart57_ClickSummary.DAT", "20050513_1400_ImpressionPart59_ClickSummary.DAT" };
        int i = 0;
        String basePath = "laks";
        String path;
        do
        {
            path = String.Format("{0}_{1}", basePath, i++);
        } while (Directory.Exists(path) || File.Exists(path));

        Directory.CreateDirectory(path);
        foreach (String file in files)
        {
            File.CreateText(Path.Combine(path, file)).Dispose();
        }

        Assert.Empty(GetFiles(path, "2006*"));

        Directory.Delete(path, true);
    }

    #endregion

    #region WindowsOnly

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

        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, testDir11Str, GetTestFileName())).Create())
        using (new FileInfo(Path.Combine(TestDirectory, GetTestFileName())).Create())
        using (new FileInfo(Path.Combine(TestDirectory, GetTestFileName())).Create())
        {
            Assert.Equal(2, GetFiles(TestDirectory, String.Format("{0}.???", new String('?', GetTestFileName().Length))).Length);
        }
    }

    #endregion
}




