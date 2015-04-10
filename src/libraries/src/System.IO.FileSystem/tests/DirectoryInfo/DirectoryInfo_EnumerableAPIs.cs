// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using Xunit;

namespace EnumerableTests
{
    public class DirectoryInfo_EnumerableTests
    {
        private static EnumerableUtils s_utils;

        [Fact]
        public static void RunTests()
        {
            s_utils = new EnumerableUtils();

            s_utils.CreateTestDirs();

            TestDirectoryInfoAPIs();

            TestExceptions();

            if (Interop.IsWindows) // test currently relies on the explicit order being returned on Windows
            {
                TestWhileEnumerating();
            }

            s_utils.DeleteTestDirs();

            Assert.True(s_utils.Passed);
        }


        // Directory tests
        private static void TestDirectoryInfoAPIs()
        {
            DoDirectoryInfoGetXTests(s_utils.testDir);
            DoDirectoryGetXTests(s_utils.testDir);
            TestDirectoryInfoSubdirs();
            TestSillySearchString();
        }

        private static void DoDirectoryGetXTests(String name)
        {
            DoDirectoryGetDirectoriesTest(name, "*", SearchOption.AllDirectories, s_utils.expected_Dirs_Deep_FSI);
            DoDirectoryGetDirectoriesTest(name, "*", SearchOption.TopDirectoryOnly, s_utils.expected_Dirs_Shallow_FSI);
            DoDirectoryGetDirectoriesTest(name, ".", SearchOption.TopDirectoryOnly, s_utils.expected_Dirs_Shallow_FSI);
            DoDirectoryGetDirectoriesTest(name, "", SearchOption.AllDirectories, new HashSet<FSIEntry>());

            DoDirectoryGetDirectoriesTest(name, @"lev1_a" + Path.DirectorySeparatorChar + "*", SearchOption.TopDirectoryOnly, s_utils.expected_Dirs_Subdir_FSI);

            DoDirectoryGetFilesTest(name, "*", SearchOption.AllDirectories, s_utils.expected_Files_Deep_FSI);
            DoDirectoryGetFilesTest(name, "*", SearchOption.TopDirectoryOnly, s_utils.expected_Files_Shallow_FSI);
            DoDirectoryGetFilesTest(name, ".", SearchOption.TopDirectoryOnly, s_utils.expected_Files_Shallow_FSI);
            DoDirectoryGetFilesTest(name, "", SearchOption.AllDirectories, new HashSet<FSIEntry>());
        }

        private static void DoDirectoryGetDirectoriesTest(String path, String searchPattern, SearchOption searchOption, HashSet<FSIEntry> expected)
        {
            String chkptFlag = "chkpt_dgd1_";
            int failCount = 0;

            // directoryinfo
            DirectoryInfo di = new DirectoryInfo(path);
            DirectoryInfo[] dis = di.GetDirectories(searchPattern, searchOption);
            HashSet<FSIEntry> disAsHS = new HashSet<FSIEntry>();
            foreach (DirectoryInfo d in dis)
            {
                disAsHS.Add(new FSIEntry(d.Name, d.FullName, null, d.ToString()));
            }

            if (!disAsHS.SetEquals(expected))
            {
                failCount++;
                Console.WriteLine(chkptFlag + "1: didn't get expected directories....");
                Console.WriteLine("Expected:");
                foreach (FSIEntry d in expected)
                {
                    Console.WriteLine("   Name:       " + d.Name);
                    Console.WriteLine("   FullName:   " + d.FullName);
                    Console.WriteLine("   ToString(): " + d.ToStr);
                }
                Console.WriteLine("But got:");
                foreach (FSIEntry d in disAsHS)
                {
                    Console.WriteLine("   Name        " + d.Name);
                    Console.WriteLine("   FullName:   " + d.FullName);
                    Console.WriteLine("   ToString(): " + d.ToStr);
                }
            }

            String testName = String.Format("SP={0}, SO={1}", searchPattern, searchOption);
            s_utils.PrintTestStatus(testName, "DirectoryInfo.GetDirectories", failCount);
        }

        private static void DoDirectoryGetFilesTest(String path, String searchPattern, SearchOption searchOption, HashSet<FSIEntry> expected)
        {
            /*
             * FileInfo check:
             * Name
             * FullName
             * DirectoryName
             * ToString
             * Directory (properties of that)
             */
            String chkptFlag = "chkpt_dgf1_";
            int failCount = 0;

            // directoryinfo
            DirectoryInfo di = new DirectoryInfo(path);
            FileInfo[] fis = di.GetFiles(searchPattern, searchOption);
            HashSet<FSIEntry> disAsHS = new HashSet<FSIEntry>();
            foreach (FileInfo f in fis)
            {
                disAsHS.Add(new FSIEntry(f.Name, f.FullName, f.DirectoryName, f.ToString()));
            }

            if (!disAsHS.SetEquals(expected))
            {
                failCount++;
                Console.WriteLine(chkptFlag + "1: didn't get expected files....");
                Console.WriteLine("Expected:");
                foreach (FSIEntry f in expected)
                {
                    Console.WriteLine("   Name:          " + f.Name);
                    Console.WriteLine("   FullName:      " + f.FullName);
                    Console.WriteLine("   DirectoryName: " + f.DirectoryName);
                    Console.WriteLine("   ToString():    " + f.ToStr);
                }
                Console.WriteLine("But got:");
                foreach (FSIEntry f in disAsHS)
                {
                    Console.WriteLine("   Name:          " + f.Name);
                    Console.WriteLine("   FullName:      " + f.FullName);
                    Console.WriteLine("   DirectoryName: " + f.DirectoryName);
                    Console.WriteLine("   ToString():    " + f.ToStr);
                }
            }

            String testName = String.Format("SP={0}, SO={1}", searchPattern, searchOption);
            s_utils.PrintTestStatus(testName, "DirectoryInfo.GetFiles", failCount);
        }


        private static void DoDirectoryInfoGetXTests(String name)
        {
            DoDirectoryInfoGetDirectoriesTest(name, "*", SearchOption.AllDirectories, s_utils.expected_Dirs_Deep);
            DoDirectoryInfoGetDirectoriesTest(name, "*", SearchOption.TopDirectoryOnly, s_utils.expected_Dirs_Shallow);
            DoDirectoryInfoGetDirectoriesTest(name, ".", SearchOption.TopDirectoryOnly, s_utils.expected_Dirs_Shallow);
            DoDirectoryInfoGetDirectoriesTest(name, "", SearchOption.AllDirectories, new HashSet<String>());

            DoDirectoryInfoGetDirectoriesTest(name, "lev2_*", SearchOption.AllDirectories, s_utils.expected_Dirs_Lev2SearchPattern);
            DoDirectoryInfoGetDirectoriesTest(name, "lev2_*", SearchOption.TopDirectoryOnly, new HashSet<String>());

            DoDirectoryInfoGetDirectoriesTest(name, "lev2_f", SearchOption.AllDirectories, s_utils.expected_Dirs_ExactSearchPattern);
            DoDirectoryInfoGetDirectoriesTest(name, "lev2_f", SearchOption.TopDirectoryOnly, new HashSet<String>());

            DoDirectoryInfoGetDirectoriesTest(name, @"lev1_a" + Path.DirectorySeparatorChar + "*", SearchOption.TopDirectoryOnly, s_utils.expected_Dirs_Subdir);

            DoDirectoryInfoGetDirectoriesTest(Path.Combine(name, "lev1_c"), ".", SearchOption.TopDirectoryOnly, new HashSet<String>());

            DoDirectoryInfoGetFilesTest(name, "*", SearchOption.AllDirectories, s_utils.expected_Files_Deep);
            DoDirectoryInfoGetFilesTest(name, "*", SearchOption.TopDirectoryOnly, s_utils.expected_Files_Shallow);
            DoDirectoryInfoGetFilesTest(name, ".", SearchOption.TopDirectoryOnly, s_utils.expected_Files_Shallow);
            DoDirectoryInfoGetFilesTest(name, "", SearchOption.AllDirectories, new HashSet<String>());
            DoDirectoryInfoGetFilesTest(name, "lev2_f", SearchOption.AllDirectories, new HashSet<String>());
            DoDirectoryInfoGetFilesTest(name, "lev2_f", SearchOption.TopDirectoryOnly, new HashSet<String>());

            DoDirectoryInfoGetFileSystemInfosTest(name, "*", SearchOption.AllDirectories, new HashSet<String>(s_utils.expected_Files_Deep.Union(s_utils.expected_Dirs_Deep)));
            DoDirectoryInfoGetFileSystemInfosTest(name, "*", SearchOption.TopDirectoryOnly, new HashSet<String>(s_utils.expected_Files_Shallow.Union(s_utils.expected_Dirs_Shallow)));
            DoDirectoryInfoGetFileSystemInfosTest(name, ".", SearchOption.TopDirectoryOnly, new HashSet<String>(s_utils.expected_Files_Shallow.Union(s_utils.expected_Dirs_Shallow)));
            DoDirectoryInfoGetFileSystemInfosTest(name, "", SearchOption.AllDirectories, new HashSet<String>());
            DoDirectoryInfoGetFileSystemInfosTest(name, "lev2_f", SearchOption.AllDirectories, s_utils.expected_Dirs_ExactSearchPattern);
            DoDirectoryInfoGetFileSystemInfosTest(name, "lev2_f", SearchOption.TopDirectoryOnly, new HashSet<String>());
            DoDirectoryInfoGetFileSystemInfosTest(name, "file1", SearchOption.AllDirectories, s_utils.expected_Files_Shallow);
        }

        private static void DoDirectoryInfoGetDirectoriesTest(String path, String searchPattern, SearchOption searchOption, HashSet<String> expected)
        {
            String chkptFlag = "chkpt_dgd1_";
            int failCount = 0;

            // directoryinfo
            DirectoryInfo di = new DirectoryInfo(path);
            IEnumerable<DirectoryInfo> dis = di.EnumerateDirectories(searchPattern, searchOption);
            HashSet<String> disAsHS = new HashSet<string>();
            foreach (DirectoryInfo d in dis)
            {
                disAsHS.Add(d.FullName);
            }

            if (!disAsHS.SetEquals(expected))
            {
                failCount++;
                Console.WriteLine(chkptFlag + "1: didn't get expected directories....");
                foreach (DirectoryInfo d in dis)
                {
                    Console.WriteLine(d.Name);
                }
            }

            String testName = String.Format("SP={0}, SO={1}", searchPattern, searchOption);
            s_utils.PrintTestStatus(testName, "DirectoryInfo.EnumerateDirectories", failCount);
        }

        private static void DoDirectoryInfoGetFilesTest(String path, String searchPattern, SearchOption searchOption, HashSet<String> expected)
        {
            String chkptFlag = "chkpt_dgf1_";
            int failCount = 0;

            // directoryinfo
            DirectoryInfo di = new DirectoryInfo(path);
            IEnumerable<FileInfo> fis = di.EnumerateFiles(searchPattern, searchOption);
            HashSet<String> disAsHS = new HashSet<string>();
            foreach (FileInfo d in fis)
            {
                disAsHS.Add(d.FullName);
            }

            if (!disAsHS.SetEquals(expected))
            {
                failCount++;
                Console.WriteLine(chkptFlag + "1: didn't get expected files....");
                foreach (FileInfo f in fis)
                {
                    Console.WriteLine(f.Name);
                }
            }

            String testName = String.Format("SP={0}, SO={1}", searchPattern, searchOption);
            s_utils.PrintTestStatus(testName, "DirectoryInfo.EnumerateFiles", failCount);
        }

        private static void DoDirectoryInfoGetFileSystemInfosTest(String path, String searchPattern, SearchOption searchOption, HashSet<String> expected)
        {
            String chkptFlag = "chkpt_dgi1_";
            int failCount = 0;

            // directoryinfo
            DirectoryInfo di = new DirectoryInfo(path);
            IEnumerable<FileSystemInfo> fis = di.EnumerateFileSystemInfos(searchPattern, searchOption);
            HashSet<String> disAsHS = new HashSet<string>();
            foreach (FileSystemInfo d in fis)
            {
                disAsHS.Add(d.FullName);
            }

            if (!disAsHS.SetEquals(expected))
            {
                failCount++;
                Console.WriteLine(chkptFlag + "1: didn't get expected files....");
                foreach (FileSystemInfo f in fis)
                {
                    Console.WriteLine(f.Name);
                }
            }

            String testName = String.Format("SP={0}, SO={1}", searchPattern, searchOption);
            s_utils.PrintTestStatus(testName, "DirectoryInfo.EnumerateFileSystemInfos", failCount);
        }


        private static void TestDirectoryInfoSubdirs()
        {
            String chkptFlag = "chkpt_disub_";
            int failCount = 0;

            DirectoryInfo di = new DirectoryInfo(s_utils.testDir);

            di.CreateSubdirectory("TestDir1");
            di.CreateSubdirectory("TestDir2");
            di.CreateSubdirectory("TestDir3");
            di.CreateSubdirectory("Test1Dir1");
            di.CreateSubdirectory("Test1Dir2");

            DirectoryInfo[] dis = di.GetDirectories("TestDir*");
            if (dis.Length != 3)
            {
                failCount++;
                Console.WriteLine(chkptFlag + "1: didn't get expected dirs....");
                foreach (DirectoryInfo d in dis)
                {
                    Console.WriteLine(d.Name);
                }
            }

            s_utils.PrintTestStatus("DirectoryInfo subdirs", "GetDirs*", failCount);
        }

        private static void TestSillySearchString()
        {
            String chkptFlag = "chkpt_sss_";
            int failCount = 0;

            DirectoryInfo di = new DirectoryInfo(s_utils.testDir);
            try
            {
                DirectoryInfo[] dis = di.GetDirectories("\n");
                if (dis.Length != 0)
                {
                    failCount++;
                    Console.WriteLine(chkptFlag + "1: got results, didn't expect any....");
                    foreach (DirectoryInfo d in dis)
                    {
                        Console.WriteLine(d.Name);
                    }
                }
            }
            catch (Exception e)
            {
                failCount++;
                Console.WriteLine(chkptFlag + "2: got unexpected exception");
                Console.WriteLine(e);
            }

            s_utils.PrintTestStatus("silly search string", "GetDirectories", failCount);
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

        private static void TestSearchPatternIter(String path, String pattern, String patternDescription, Exception expectedException)
        {
            TestGetDirectories(path, pattern, patternDescription, expectedException);
            TestGetFiles(path, pattern, patternDescription, expectedException);
            TestGetFileSystemInfos(path, pattern, patternDescription, expectedException);
            TestGetDirectoriesFast(path, pattern, patternDescription, expectedException);
            TestGetFilesFast(path, pattern, patternDescription, expectedException);
            TestGetFileSystemInfosFast(path, pattern, patternDescription, expectedException);
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
            char[] whitespacePathChars = { (char)0x9, (char)0xA };
            String whitespacePath = new String(whitespacePathChars);
            TestWeirdPathIter(whitespacePath, "whitespacePath", new ArgumentException());

            if (Interop.IsWindows) // drive labels
            {
                // try to test a path that doesn't exist. Skip if can't find an unused drive
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
            String longPath = Path.Combine(new String('a', IOInputs.MaxPath), "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
            TestWeirdPathIter(longPath, "pathTooLong", new PathTooLongException());

            // path invalid chars
            String invalidCharPath = "temp" + Path.DirectorySeparatorChar + "fsd\0sdsds";
            TestWeirdPathIter(invalidCharPath, "invalidCharPath", new ArgumentException());
        }

        private static void TestWeirdPathIter(String path, String pathDescription, Exception expectedException)
        {
            String pattern = "*";
            TestGetDirectories(path, pattern, pathDescription, expectedException);
            TestGetFiles(path, pattern, pathDescription, expectedException);
            TestGetFileSystemInfos(path, pattern, pathDescription, expectedException);
            TestGetDirectoriesFast(path, pattern, pathDescription, expectedException);
            TestGetFilesFast(path, pattern, pathDescription, expectedException);
            TestGetFileSystemInfosFast(path, pattern, pathDescription, expectedException);
        }


        private static void TestGetDirectories(String path, String pattern, String testDescription, Exception expectedException)
        {
            int failCount = 0;
            String chkptFlag = "chkpt_spgd_";
            String methodName = "DirectoryInfo.GetDirectories";
            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                DirectoryInfo[] dirs1 = di.GetDirectories(pattern);

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
                DirectoryInfo di = new DirectoryInfo(path);
                DirectoryInfo[] dirs2 = di.GetDirectories(pattern, SearchOption.AllDirectories);

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

            s_utils.PrintTestStatus(testDescription, methodName, failCount);
        }

        private static void TestGetFiles(String path, String pattern, String testDescription, Exception expectedException)
        {
            int failCount = 0;
            String chkptFlag = "chkpt_spgf_";
            String methodName = "DirectoryInfo.GetFiles";

            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                FileInfo[] dirs1 = di.GetFiles(pattern);

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
                DirectoryInfo di = new DirectoryInfo(path);
                FileInfo[] dirs2 = di.GetFiles(pattern, SearchOption.AllDirectories);

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

            s_utils.PrintTestStatus(testDescription, methodName, failCount);
        }

        private static void TestGetFileSystemInfos(String path, String pattern, String testDescription, Exception expectedException)
        {
            int failCount = 0;
            String chkptFlag = "chkpt_spgfse_";
            String methodName = "DirectoryInfo.GetFileSystemInfos";

            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                FileSystemInfo[] dirs1 = di.GetFileSystemInfos(pattern);

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
                DirectoryInfo di = new DirectoryInfo(path);
                FileSystemInfo[] dirs2 = di.GetFileSystemInfos(pattern, SearchOption.AllDirectories);

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


            s_utils.PrintTestStatus(testDescription, methodName, failCount);
        }


        private static void TestGetDirectoriesFast(String path, String pattern, String testDescription, Exception expectedException)
        {
            int failCount = 0;
            String chkptFlag = "chkpt_spgdf_";
            String methodName = "DirectoryInfo.EnumerateDirectories";

            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                IEnumerable<DirectoryInfo> dirs1 = di.EnumerateDirectories(pattern);

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
                DirectoryInfo di = new DirectoryInfo(path);
                IEnumerable<DirectoryInfo> dirs2 = di.EnumerateDirectories(pattern, SearchOption.AllDirectories);

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

            s_utils.PrintTestStatus(testDescription, methodName, failCount);
        }

        private static void TestGetFilesFast(String path, String pattern, String testDescription, Exception expectedException)
        {
            int failCount = 0;
            String chkptFlag = "chkpt_spgff_";
            String methodName = "DirectoryInfo.EnumerateFiles";

            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                IEnumerable<FileInfo> dirs1 = di.EnumerateFiles(pattern);

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
                DirectoryInfo di = new DirectoryInfo(path);
                IEnumerable<FileInfo> dirs2 = di.EnumerateFiles(pattern, SearchOption.AllDirectories);

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

            s_utils.PrintTestStatus(testDescription, methodName, failCount);
        }

        private static void TestGetFileSystemInfosFast(String path, String pattern, String testDescription, Exception expectedException)
        {
            int failCount = 0;
            String chkptFlag = "chkpt_spgfse_";
            String methodName = "DirectoryInfo.EnumerateFileSystemInfos";

            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                IEnumerable<FileSystemInfo> dirs1 = di.EnumerateFileSystemInfos(pattern);

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
                DirectoryInfo di = new DirectoryInfo(path);
                IEnumerable<FileSystemInfo> dirs2 = di.EnumerateFileSystemInfos(pattern, SearchOption.AllDirectories);

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

            s_utils.PrintTestStatus(testDescription, methodName, failCount);
        }

        private static void TestSearchOptionOutOfRange()
        {
            TestSearchOptionOutOfRangeDirectoryInfo();
            TestSearchOptionOutOfRangeDirectoryInfoFast();
        }

        private static void TestSearchOptionOutOfRangeDirectoryInfo()
        {
            int failCount = 0;
            String chkptFlag = "chkpt_soordi_";
            DirectoryInfo di = new DirectoryInfo(s_utils.testDir);

            // get files
            try
            {
                FileInfo[] s1 = di.GetFiles(s_utils.testDir, (SearchOption)5);
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

            // get directories
            try
            {
                DirectoryInfo[] s1 = di.GetDirectories(s_utils.testDir, (SearchOption)5);
                Console.WriteLine(chkptFlag + "3: didn't throw");
                failCount++;
            }
            catch (ArgumentOutOfRangeException) { } // expected
            catch (Exception e)
            {
                failCount++;
                Console.WriteLine(chkptFlag + "4: threw wrong exception");
                Console.WriteLine(e);
            }

            s_utils.PrintTestStatus("TestSearchOptionOutOfRangeDirectoryInfo", "DirectoryInfo*", failCount);
        }

        private static void TestSearchOptionOutOfRangeDirectoryInfoFast()
        {
            int failCount = 0;
            String chkptFlag = "chkpt_soordif_";
            DirectoryInfo di = new DirectoryInfo(s_utils.testDir);

            // get files
            try
            {
                IEnumerable<FileInfo> s1 = di.EnumerateFiles(s_utils.testDir, (SearchOption)5);
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

            // get directories
            try
            {
                IEnumerable<DirectoryInfo> s1 = di.EnumerateDirectories(s_utils.testDir, (SearchOption)5);
                Console.WriteLine(chkptFlag + "3: didn't throw");
                failCount++;
            }
            catch (ArgumentOutOfRangeException) { } // expected
            catch (Exception e)
            {
                failCount++;
                Console.WriteLine(chkptFlag + "4: threw wrong exception");
                Console.WriteLine(e);
            }

            s_utils.PrintTestStatus("TestSearchOptionOutOfRangeDirectoryInfoFast", "DirectoryInfo*", failCount);
        }

        private static void TestWhileEnumerating()
        {
            TestChangeWhileEnumerating();
        }

        private static void TestChangeWhileEnumerating()
        {
            DoGetDirectories_Add();
            DoGetFiles_Add();
            DoGetFileSystemInfos_Add();
            DoGetDirectories_Delete();
            DoGetFiles_Delete();
            DoGetFileSystemInfos_Delete();
        }

        private static void DoGetDirectories_Add()
        {
            String chkptFlag = "chkpt_dgdm_";
            int failCount = 0;

            s_utils.CreateTestDirs();

            // directoryinfo
            DirectoryInfo di = new DirectoryInfo(s_utils.testDir);
            IEnumerable<DirectoryInfo> dis = di.EnumerateDirectories("*", SearchOption.AllDirectories);
            HashSet<String> disAsHS = new HashSet<string>();
            int count = 0;

            try
            {
                foreach (DirectoryInfo d in dis)
                {
                    disAsHS.Add(d.FullName);
                    count++;
                    if (count == 2)
                        s_utils.ChangeFSAdd();
                }
                if (!disAsHS.SetEquals(s_utils.expected_Dirs_Changed))
                {
                    failCount++;
                    Console.WriteLine(chkptFlag + "1: didn't get expected directories....");
                    foreach (DirectoryInfo d in dis)
                    {
                        Console.WriteLine(d.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                failCount++;
                Console.WriteLine(chkptFlag + "2: got wrong exception");
                Console.WriteLine(ex);
            }

            String testName = "addFilesWhileEnumerating"; ;
            s_utils.PrintTestStatus(testName, "DirectoryInfo.EnumerateDirectories", failCount);
        }

        private static void DoGetDirectories_Delete()
        {
            String chkptFlag = "chkpt_dgdm_";
            int failCount = 0;

            s_utils.CreateTestDirs();

            // directoryinfo
            DirectoryInfo di = new DirectoryInfo(s_utils.testDir);
            IEnumerable<DirectoryInfo> dis = di.EnumerateDirectories("*", SearchOption.AllDirectories);
            HashSet<String> disAsHS = new HashSet<string>();
            int count = 0;

            try
            {
                foreach (DirectoryInfo d in dis)
                {
                    disAsHS.Add(d.FullName);
                    count++;
                    if (count == 2)
                        s_utils.ChangeFSDelete();
                }
                if (!disAsHS.SetEquals(s_utils.expected_Dirs_Changed))
                {
                    failCount++;
                    Console.WriteLine(chkptFlag + "1: didn't get expected directories....");
                    foreach (DirectoryInfo d in dis)
                    {
                        Console.WriteLine(d.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                failCount++;
                Console.WriteLine(chkptFlag + "2: got wrong exception");
                Console.WriteLine(ex);
            }

            String testName = "deleteFilesWhileEnumerating"; ;
            s_utils.PrintTestStatus(testName, "DirectoryInfo.EnumerateDirectories", failCount);
        }

        private static void DoGetFiles_Add()
        {
            String chkptFlag = "chkpt_dgfm_";
            int failCount = 0;

            s_utils.CreateTestDirs();

            // directoryinfo
            DirectoryInfo di = new DirectoryInfo(s_utils.testDir);
            IEnumerable<FileInfo> fis = di.EnumerateFiles("*", SearchOption.AllDirectories);
            HashSet<String> disAsHS = new HashSet<string>();
            int count = 0;

            try
            {
                foreach (FileInfo d in fis)
                {
                    disAsHS.Add(d.FullName);
                    count++;
                    if (count == 2)
                        s_utils.ChangeFSAdd();
                }
                if (!disAsHS.SetEquals(s_utils.expected_Files_Changed))
                {
                    failCount++;
                    Console.WriteLine(chkptFlag + "1: didn't get expected files....");
                    foreach (FileInfo f in fis)
                    {
                        Console.WriteLine(f.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                failCount++;
                Console.WriteLine(chkptFlag + "2: got wrong exception");
                Console.WriteLine(ex);
            }

            String testName = "addFilesWhileEnumerating"; ;
            s_utils.PrintTestStatus(testName, "DirectoryInfo.EnumerateFiles", failCount);
        }

        private static void DoGetFiles_Delete()
        {
            String chkptFlag = "chkpt_dgfm_";
            int failCount = 0;

            s_utils.CreateTestDirs();

            // directoryinfo
            DirectoryInfo di = new DirectoryInfo(s_utils.testDir);
            IEnumerable<FileInfo> fis = di.EnumerateFiles("*", SearchOption.AllDirectories);
            HashSet<String> disAsHS = new HashSet<string>();
            int count = 0;

            try
            {
                foreach (FileInfo d in fis)
                {
                    disAsHS.Add(d.FullName);
                    count++;
                    if (count == 2)
                        s_utils.ChangeFSDelete();
                }
                if (!disAsHS.SetEquals(s_utils.expected_Files_Changed))
                {
                    failCount++;
                    Console.WriteLine(chkptFlag + "1: didn't get expected files....");
                    foreach (FileInfo f in fis)
                    {
                        Console.WriteLine(f.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                failCount++;
                Console.WriteLine(chkptFlag + "2: got wrong exception");
                Console.WriteLine(ex);
            }

            String testName = "deleteFilesWhileEnumerating"; ;
            s_utils.PrintTestStatus(testName, "DirectoryInfo.EnumerateFiles", failCount);
        }

        private static void DoGetFileSystemInfos_Add()
        {
            String chkptFlag = "chkpt_dgim_";
            int failCount = 0;

            s_utils.CreateTestDirs();

            // directoryinfo
            DirectoryInfo di = new DirectoryInfo(s_utils.testDir);
            IEnumerable<FileSystemInfo> fis = di.EnumerateFileSystemInfos("*", SearchOption.AllDirectories);
            HashSet<String> disAsHS = new HashSet<string>();
            int count = 0;

            try
            {
                foreach (FileSystemInfo d in fis)
                {
                    disAsHS.Add(d.FullName);
                    count++;
                    if (count == 2)
                        s_utils.ChangeFSAdd();
                }
                if (!disAsHS.SetEquals(new HashSet<String>(s_utils.expected_Dirs_Changed.Union(s_utils.expected_Files_Changed))))
                {
                    failCount++;
                    Console.WriteLine(chkptFlag + "1: didn't get expected files....");
                    foreach (FileSystemInfo f in fis)
                    {
                        Console.WriteLine(f.Name);
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
            s_utils.PrintTestStatus(testName, "DirectoryInfo.EnumerateFileSystemInfos", failCount);
        }

        private static void DoGetFileSystemInfos_Delete()
        {
            String chkptFlag = "chkpt_dgim_";
            int failCount = 0;

            s_utils.CreateTestDirs();

            // directoryinfo
            DirectoryInfo di = new DirectoryInfo(s_utils.testDir);
            IEnumerable<FileSystemInfo> fis = di.EnumerateFileSystemInfos("*", SearchOption.AllDirectories);
            HashSet<String> disAsHS = new HashSet<string>();
            int count = 0;

            try
            {
                foreach (FileSystemInfo d in fis)
                {
                    disAsHS.Add(d.FullName);
                    count++;
                    if (count == 2)
                        s_utils.ChangeFSDelete();
                }
                if (!disAsHS.SetEquals(new HashSet<String>(s_utils.expected_Dirs_Changed.Union(s_utils.expected_Files_Changed))))
                {
                    failCount++;
                    Console.WriteLine(chkptFlag + "1: didn't get expected files....");
                    foreach (FileSystemInfo f in fis)
                    {
                        Console.WriteLine(f.Name);
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
            s_utils.PrintTestStatus(testName, "DirectoryInfo.EnumerateFileSystemInfos", failCount);
        }
    }
}
