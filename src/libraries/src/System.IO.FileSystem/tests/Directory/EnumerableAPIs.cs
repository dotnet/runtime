// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.FileSystem.Tests;
using System.Linq;
using EnumerableTests;
using Xunit;

#region EnumerateFiles

public class Directory_EnumerateFiles_Str : Directory_GetFiles_str
{
    public override String[] GetFiles(String path)
    {
        return Directory.EnumerateFiles(path).ToArray();
    }
}

public class Directory_EnumerateFiles_str_str : Directory_GetFiles_str_str
{
    public override String[] GetFiles(String path)
    {
        return Directory.EnumerateFiles(path, "*").ToArray();
    }

    public override String[] GetFiles(String path, String searchPattern)
    {
        return Directory.EnumerateFiles(path, searchPattern).ToArray();
    }
}

public class Directory_EnumerateFiles_str_str_so : Directory_GetFiles_str_str_so
{
    public override String[] GetFiles(String path)
    {
        return Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly).ToArray();
    }

    public override String[] GetFiles(String path, String searchPattern)
    {
        return Directory.EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly).ToArray();
    }

    public override String[] GetFiles(String path, String searchPattern, SearchOption option)
    {
        return Directory.EnumerateFiles(path, searchPattern, option).ToArray();
    }
}
#endregion

#region EnumerateFileSystemEntries

public class Directory_EnumerateFileSystemEntries_str : Directory_GetFileSystemEntries_str
{
    public override String[] GetEntries(String dirName)
    {
        return Directory.EnumerateFileSystemEntries(dirName).ToArray();
    }
}

public class Directory_EnumerateFileSystemEntries_str_str : Directory_GetFileSystemEntries_str_str
{
    public override String[] GetEntries(String dirName)
    {
        return Directory.EnumerateFileSystemEntries(dirName, "*").ToArray();
    }

    public override String[] GetEntries(String dirName, String searchPattern)
    {
        return Directory.EnumerateFileSystemEntries(dirName, searchPattern).ToArray();
    }

}

public class Directory_EnumerateFileSystemEntries_str_str_so : Directory_GetFileSystemEntries_str_str_so
{
    public override String[] GetEntries(String dirName)
    {
        return Directory.EnumerateFileSystemEntries(dirName, "*", SearchOption.TopDirectoryOnly).ToArray();
    }

    public override String[] GetEntries(String dirName, String searchPattern)
    {
        return Directory.EnumerateFileSystemEntries(dirName, searchPattern, SearchOption.TopDirectoryOnly).ToArray();
    }

    public override String[] GetEntries(String dirName, String searchPattern, SearchOption option)
    {
        return Directory.EnumerateFileSystemEntries(dirName, searchPattern, option).ToArray();
    }
}

#endregion

#region EnumerateDirectories

public class Directory_EnumerableAPIs : FileSystemTest
{
    private static EnumerableUtils s_utils;

    [Fact]
    public void runTest()
    {
        s_utils = new EnumerableUtils();

        s_utils.CreateTestDirs(TestDirectory);

        TestDirectoryAPIs();

        TestExceptions();

        if (Interop.IsWindows) // test relies on the explicit order being returned on Windows
        {
            TestWhileEnumerating();
        }

        s_utils.DeleteTestDirs();

        Assert.True(s_utils.Passed);
    }

    // Directory tests
    private static void TestDirectoryAPIs()
    {
        DoDirectoryGetXTests(s_utils.testDir);
    }

    private static void DoDirectoryGetXTests(String name)
    {
        DoDirectoryGetDirectoriesTest(name, "*", SearchOption.AllDirectories, s_utils.expected_Dirs_Deep);
        DoDirectoryGetDirectoriesTest(name, "*", SearchOption.TopDirectoryOnly, s_utils.expected_Dirs_Shallow);
        DoDirectoryGetDirectoriesTest(name, ".", SearchOption.TopDirectoryOnly, s_utils.expected_Dirs_Shallow);
        DoDirectoryGetDirectoriesTest(name, "", SearchOption.AllDirectories, new HashSet<String>());

        DoDirectoryGetDirectoriesTest(name, "lev2_*", SearchOption.AllDirectories, s_utils.expected_Dirs_Lev2SearchPattern);
        DoDirectoryGetDirectoriesTest(name, "lev2_*", SearchOption.TopDirectoryOnly, new HashSet<String>());

        DoDirectoryGetDirectoriesTest(name, "lev2_f", SearchOption.AllDirectories, s_utils.expected_Dirs_ExactSearchPattern);
        DoDirectoryGetDirectoriesTest(name, "lev2_f", SearchOption.TopDirectoryOnly, new HashSet<String>());

        DoDirectoryGetDirectoriesTest(name, Path.Combine("lev1_a", "*"), SearchOption.TopDirectoryOnly, s_utils.expected_Dirs_Subdir);
    }

    private static void DoDirectoryGetDirectoriesTest(String path, String searchPattern, SearchOption searchOption, HashSet<String> expected)
    {
        String chkptFlag = "chkpt_dgd1_";

        IEnumerable<String> dirs = Directory.EnumerateDirectories(path, searchPattern, searchOption);
        HashSet<String> dirsAsHS = new HashSet<string>(dirs);
        int failCount = 0;
        if (!dirsAsHS.SetEquals(expected))
        {
            failCount++;
            Console.WriteLine(chkptFlag + "1: didn't get expected directories....");
            foreach (String d in dirs)
            {
                Console.WriteLine(d);
            }
        }

        String testName = String.Format("SP={0}, SO={1}", searchPattern, searchOption);
        s_utils.PrintTestStatus(testName, "Directory.EnumerateDirectories", failCount);
    }

    // Exception tests
    private static void TestExceptions()
    {
        TestSearchPatterns();
        TestSearchOptionOutOfRange();
        TestWeirdPaths();
    }


    private static void TestSearchPatterns()
    {
        TestSearchPatternIter(s_utils.testDir, null, "null SP", new ArgumentNullException());
        TestSearchPatternIter(s_utils.testDir, "..", "invalid SP", new ArgumentException());
    }

    private static void TestSearchOptionOutOfRange()
    {
        TestSearchOptionOutOfRange(Directory.GetDirectories, "Directory.GetDirectories");
        TestSearchOptionOutOfRange(Directory.GetFiles, "Directory.GetFiles");
        TestSearchOptionOutOfRangeFast(Directory.EnumerateDirectories, "Directory.EnumerateDirectories");
        TestSearchOptionOutOfRangeFast(Directory.EnumerateFiles, "Directory.EnumerateFiles");
    }

    private static void TestWeirdPaths()
    {
        // null path
        String nullPath = null;
        TestWeirdPathIter(nullPath, "nullPath", new ArgumentNullException());

        // empty path
        String emptyPath = "";
        TestWeirdPathIter(emptyPath, "emptyPath", new ArgumentException());

        // whitespace-only path
        if (Interop.IsWindows) // whitespace-only names are valid on Unix
        {
            char[] whitespacePathChars = { (char)0x9, (char)0xA };
            String whitespacePath = new String(whitespacePathChars);
            TestWeirdPathIter(whitespacePath, "whitespacePath", new ArgumentException());
        }

        // try to test a path that doesn't exist. Skip if can't find an unused drive
        if (Interop.IsWindows)
        {
            String pathNotExists = null;
            String unusedDrive = EnumerableUtils.GetUnusedDrive();
            if (unusedDrive != null)
            {
                pathNotExists = Path.Combine(unusedDrive, "temp", "dir");
            }
            if (pathNotExists != null)
            {
                TestWeirdPathIter(pathNotExists, "pathNotExists", new DirectoryNotFoundException());
            }
        }

        // file (not dir) name. If we try to do GetFiles, GetDirs, etc in a file (not dir) we get IOException
        String filePath = null;
        foreach (String s in s_utils.expected_Files_Deep)
        {
            if (s != null)
            {
                filePath = s;
                break;
            }
        }
        TestWeirdPathIter(filePath, "pathIsFile", new IOException());

        // PathTooLong
        String longPath = Path.Combine(new String('a', IOInputs.MaxDirectory), "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        TestWeirdPathIter(longPath, "pathTooLong", new PathTooLongException());

        // path invalid chars
        String invalidCharPath = "temp" + Path.DirectorySeparatorChar + "fsd\0sdsds";
        TestWeirdPathIter(invalidCharPath, "invalidCharPath", new ArgumentException());
    }

    private static void TestWeirdPathIter(String path, String pathDescription, Exception expectedException)
    {
        TestWeirdPath(path, pathDescription, expectedException, Directory.GetDirectories, Directory.GetDirectories, Directory.GetDirectories, "Directory.GetDirectories");
        TestWeirdPathFast(path, pathDescription, expectedException, Directory.EnumerateDirectories, Directory.EnumerateDirectories, Directory.EnumerateDirectories, "Directory.EnumerateDirectories");
    }

    private static void TestSearchPatternIter(String path, String pattern, String patternDescription,
        Exception expectedException)
    {
        TestSearchPattern(path, pattern, patternDescription, expectedException, Directory.GetDirectories, Directory.GetDirectories, "Directory.GetDirectories");
        TestSearchPatternFast(path, pattern, patternDescription, expectedException, Directory.EnumerateDirectories, Directory.EnumerateDirectories, "Directory.EnumerateDirectories");
    }

    private static void TestSearchPattern(String path, String pattern, String patternDescription, Exception expectedException,
        EnumerableUtils.GetFSEs1 fseMethod1, EnumerableUtils.GetFSEs2 fseMethod2, String methodName)
    {
        int failCount = 0;
        String chkptFlag = "chkpt_nspat_";
        try
        {
            String[] dirs1 = fseMethod1(path, pattern);
            Console.WriteLine(chkptFlag + "1: didn't throw");
            failCount++;
        }
        catch (Exception e)
        {
            if (e.GetType() != expectedException.GetType())
            {
                failCount++;
                Console.WriteLine(chkptFlag + "2: threw wrong exception");
                Console.WriteLine(e);
            }
        }

        if (fseMethod2 != null)
        {
            try
            {
                String[] dirs2 = fseMethod2(path, pattern, SearchOption.AllDirectories);
                Console.WriteLine(chkptFlag + "3: didn't throw");
                failCount++;
            }
            catch (Exception e)
            {
                if (e.GetType() != expectedException.GetType())
                {
                    failCount++;
                    Console.WriteLine(chkptFlag + "4: threw wrong exception");
                    Console.WriteLine(e);
                }
            }
        }

        String testName = String.Format("TestSearchPattern({0})", patternDescription);
        s_utils.PrintTestStatus(testName, methodName, failCount);
    }

    private static void TestSearchPatternFast(String path, String pattern, String patternDescription, Exception expectedException, EnumerableUtils.GetFSEsFast1 fseMethod1, EnumerableUtils.GetFSEsFast2 fseMethod2, String methodName)
    {
        int failCount = 0;
        String chkptFlag = "chkpt_nspatf_";
        try
        {
            IEnumerable<String> dirs1 = fseMethod1(path, pattern);
            Console.WriteLine(chkptFlag + "1: didn't throw");
            failCount++;
        }
        catch (Exception e)
        {
            if (e.GetType() != expectedException.GetType())
            {
                failCount++;
                Console.WriteLine(chkptFlag + "2: threw wrong exception");
                Console.WriteLine(e);
            }
        }

        if (fseMethod2 != null)
        {
            try
            {
                IEnumerable<String> dirs2 = fseMethod2(path, pattern, SearchOption.AllDirectories);
                Console.WriteLine(chkptFlag + "3: didn't throw");
                failCount++;
            }
            catch (Exception e)
            {
                if (e.GetType() != expectedException.GetType())
                {
                    failCount++;
                    Console.WriteLine(chkptFlag + "4: threw wrong exception");
                    Console.WriteLine(e);
                }
            }
        }
        String testName = String.Format("TestSearchPatternFast({0})", patternDescription);
        s_utils.PrintTestStatus(testName, methodName, failCount);
    }

    private static void TestSearchOptionOutOfRange(EnumerableUtils.GetFSEs2 fseMethod, String methodName)
    {
        int failCount = 0;
        String chkptFlag = "chkpt_soor_";
        try
        {
            String[] dirs1 = fseMethod(s_utils.testDir, "*", (SearchOption)5);
            Console.WriteLine(chkptFlag + "1: didn't throw");
            failCount++;
        }
        catch (ArgumentOutOfRangeException)
        {
        } // expected
        catch (Exception e)
        {
            failCount++;
            Console.WriteLine(chkptFlag + "2: threw wrong exception");
            Console.WriteLine(e);
        }

        s_utils.PrintTestStatus("TestSearchOptionOutOfRange", methodName, failCount);
    }

    private static void TestSearchOptionOutOfRangeFast(EnumerableUtils.GetFSEsFast2 fseMethod, String methodName)
    {
        int failCount = 0;
        String chkptFlag = "chkpt_soorf_";
        try
        {
            IEnumerable<String> dirs1 = fseMethod(s_utils.testDir, "*", (SearchOption)5);
            Console.WriteLine(chkptFlag + "1: didn't throw");
            failCount++;
        }
        catch (ArgumentOutOfRangeException) { } // expected
        catch (Exception e)
        {
            failCount++;
            Console.WriteLine(chkptFlag + "2: threw wrong exception");
            Console.WriteLine(e);
        }

        s_utils.PrintTestStatus("TestSearchOptionOutOfRangeFast", methodName, failCount);
    }

    private static void TestWeirdPath(String path, String pathDescription, Exception expectedException, EnumerableUtils.GetFSEs0 fseMethod0, EnumerableUtils.GetFSEs1 fseMethod1, EnumerableUtils.GetFSEs2 fseMethod2, String methodName)
    {
        int failCount = 0;
        String chkptFlag = "chkpt_wp_";

        try
        {
            String[] dirs1 = fseMethod0(path);
            Console.WriteLine(chkptFlag + "1: didn't throw");
            failCount++;
        }
        catch (Exception e)
        {
            if (e.GetType() != expectedException.GetType())
            {
                failCount++;
                Console.WriteLine(chkptFlag + "2: threw wrong exception");
                Console.WriteLine(e);
            }
        }

        try
        {
            String[] dirs1 = fseMethod1(path, "*");
            Console.WriteLine(chkptFlag + "3: didn't throw");
            failCount++;
        }
        catch (Exception e)
        {
            if (e.GetType() != expectedException.GetType())
            {
                failCount++;
                Console.WriteLine(chkptFlag + "4: threw wrong exception");
                Console.WriteLine(e);
            }
        }

        if (fseMethod2 != null)
        {
            try
            {
                String[] dirs2 = fseMethod2(path, "*", SearchOption.AllDirectories);
                Console.WriteLine(chkptFlag + "5: didn't throw");
                failCount++;
            }
            catch (Exception e)
            {
                if (e.GetType() != expectedException.GetType())
                {
                    Console.WriteLine("e.GetType = " + e.GetType());
                    Console.WriteLine("expectedException.GetType = " + expectedException.GetType());
                    failCount++;
                    Console.WriteLine(chkptFlag + "6: threw wrong exception");
                    Console.WriteLine(e);
                }
            }
        }
        String testName = String.Format("TestWeirdPath({0})", pathDescription);
        s_utils.PrintTestStatus(testName, methodName, failCount);
    }

    private static void TestWeirdPathFast(String path, String pathDescription, Exception expectedException, EnumerableUtils.GetFSEsFast0 fseMethod0, EnumerableUtils.GetFSEsFast1 fseMethod1, EnumerableUtils.GetFSEsFast2 fseMethod2, String methodName)
    {
        int failCount = 0;
        String chkptFlag = "chkpt_wpf_";
        try
        {
            IEnumerable<String> dirs1 = fseMethod0(path);
            Console.WriteLine(chkptFlag + "1: didn't throw");
            failCount++;
        }
        catch (Exception e)
        {
            if (e.GetType() != expectedException.GetType())
            {
                failCount++;
                Console.WriteLine(chkptFlag + "2: threw wrong exception");
                Console.WriteLine(e);
            }
        }


        try
        {
            IEnumerable<String> dirs1 = fseMethod1(path, "*");
            Console.WriteLine(chkptFlag + "3: didn't throw");
            failCount++;
        }
        catch (Exception e)
        {
            if (e.GetType() != expectedException.GetType())
            {
                failCount++;
                Console.WriteLine(chkptFlag + "4: threw wrong exception");
                Console.WriteLine(e);
            }
        }

        if (fseMethod2 != null)
        {
            try
            {
                IEnumerable<String> dirs2 = fseMethod2(path, "*", SearchOption.AllDirectories);
                Console.WriteLine(chkptFlag + "5: didn't throw");
                failCount++;
            }
            catch (Exception e)
            {
                if (e.GetType() != expectedException.GetType())
                {
                    failCount++;
                    Console.WriteLine(chkptFlag + "6: threw wrong exception");
                    Console.WriteLine(e);
                }
            }
        }

        String testName = String.Format("TestWeirdPathFast({0})", pathDescription);
        s_utils.PrintTestStatus(testName, methodName, failCount);
    }

    private void TestWhileEnumerating()
    {
        TestChangeWhileEnumerating();
    }


    private void TestChangeWhileEnumerating()
    {
        DoGetDirectories_Add();
        DoGetDirectories_Delete();
    }

    private void DoGetDirectories_Add()
    {
        String chkptFlag = "chkpt_dgdm_";
        int failCount = 0;

        s_utils.CreateTestDirs(TestDirectory);

        IEnumerable<String> dis = Directory.EnumerateDirectories(s_utils.testDir, "*", SearchOption.AllDirectories);
        HashSet<String> disAsHS = new HashSet<string>();
        int count = 0;

        try
        {
            foreach (String d in dis)
            {
                disAsHS.Add(d);
                count++;
                if (count == 2)
                    s_utils.ChangeFSAdd();
            }
            if (!disAsHS.SetEquals(s_utils.expected_Dirs_Changed))
            {
                failCount++;
                Console.WriteLine(chkptFlag + "1: didn't get expected directories....");
                foreach (String d in dis)
                {
                    Console.WriteLine(d);
                }
            }
        }
        catch (Exception ex)
        {
            failCount++;
            Console.WriteLine(chkptFlag + "2: got wrong exception");
            Console.WriteLine(ex);
        }

        String testName = "addFilesWhileEnumerating";
        s_utils.PrintTestStatus(testName, "Directory.EnumerateDirectories", failCount);
    }

    private void DoGetDirectories_Delete()
    {
        String chkptFlag = "chkpt_dgdm_";
        int failCount = 0;

        s_utils.CreateTestDirs(TestDirectory);

        IEnumerable<String> dis = Directory.EnumerateDirectories(s_utils.testDir, "*", SearchOption.AllDirectories);
        HashSet<String> disAsHS = new HashSet<string>();
        int count = 0;

        try
        {
            foreach (String d in dis)
            {
                disAsHS.Add(d);
                count++;
                if (count == 2)
                    s_utils.ChangeFSDelete();
            }
            if (!disAsHS.SetEquals(s_utils.expected_Dirs_Changed))
            {
                failCount++;
                Console.WriteLine(chkptFlag + "1: didn't get expected directories....");
                foreach (String d in dis)
                {
                    Console.WriteLine(d);
                }
            }
        }
        catch (Exception ex)
        {
            failCount++;
            Console.WriteLine(chkptFlag + "2: got wrong exception");
            Console.WriteLine(ex);
        }

        String testName = "deleteFilesWhileEnumerating";
        ;
        s_utils.PrintTestStatus(testName, "Directory.EnumerateDirectories", failCount);
    }

}

#endregion
