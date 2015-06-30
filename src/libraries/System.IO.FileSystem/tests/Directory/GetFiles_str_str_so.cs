// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Security;
using System.Globalization;
using Xunit;

public class Directory_GetFiles_str_str_so : Directory_GetFiles_str_str
{
    private delegate void ExceptionCode();
    private static bool s_pass = true;

    #region Utilities

    public override String[] GetFiles(String dirName)
    {
        return Directory.GetFiles(dirName, "*", SearchOption.TopDirectoryOnly);
    }

    public override String[] GetFiles(String dirName, String searchPattern)
    {
        return Directory.GetFiles(dirName, searchPattern, SearchOption.TopDirectoryOnly);
    }

    public virtual String[] GetFiles(String dirName, String searchPattern, SearchOption option)
    {
        return Directory.GetFiles(dirName, searchPattern, option);
    }

    #endregion

    #region UniversalTests

    [Fact]
    public void IncludeSubDirectoryFiles()
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
            Assert.Equal(3, GetFiles(Path.Combine(TestDirectory, testDir1Str), "*", SearchOption.AllDirectories).Length);
        }
    }

    [Fact]
    public void SearchPatternIncludeSubDirectories()
    {
        String testDir1Str = GetTestFileName();
        String testDir11Str = GetTestFileName();
        DirectoryInfo testDir = new DirectoryInfo(TestDirectory);
        DirectoryInfo testDir1 = testDir.CreateSubdirectory(testDir1Str);
        testDir1.CreateSubdirectory(testDir11Str);

        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, testDir11Str, GetTestFileName())).Create())
        using (new FileInfo(Path.Combine(TestDirectory, testDir1Str, GetTestFileName())).Create())
        {
            Assert.Equal(2, GetFiles(TestDirectory, Path.Combine(testDir1Str, "*"), SearchOption.AllDirectories).Length);
        }
    }

    [Fact]
    public void InvalidSearchOption()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GetFiles(".", "*", (SearchOption)100));
        Assert.Throws<ArgumentOutOfRangeException>(() => GetFiles(".", "*", (SearchOption)(-1)));
    }


    [Fact]
    public void Scenario1()
    {
        //Scenario 1: Vanilla - create a directory with some files and then add a couple of directories with some files and check with searchPattern *.*
        String dirName;
        String[] expectedFiles;
        String[] files;
        List<String> list;

        dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
        {
            expectedFiles = fileManager.GetAllFiles();
            list = new List<String>(expectedFiles);
            files = Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories);
            Eval(files.Length, list.Count, "Err_415mbz! wrong file count.");
            for (int i = 0; i < files.Length; i++)
            {
                if (Eval(list.Contains(files[i]), "Err_287kkm! unexpected file found: {0}", files[i]))
                    list.Remove(files[i]);
            }
            if (!Eval(list.Count == 0, "Err_921mhs! {0} expected files not found.", list.Count))
            {
                foreach (String fileName in list)
                    Console.WriteLine(fileName);
            }
        }
    }

    [Fact]
    public void Scenario2()
    {
        //Scenario 2: create a directory only top level files and directories and check
        String dirName;
        String[] expectedFiles;
        String[] files;
        List<String> list;

        dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 1, 10))
        {
            expectedFiles = fileManager.GetAllFiles();
            list = new List<String>(expectedFiles);
            files = Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories);
            Eval(files.Length, list.Count, "Err_202wur! wrong file count.");
            for (int i = 0; i < files.Length; i++)
            {
                if (Eval(list.Contains(files[i]), "Err_611lgv! unexpected file found: {0}", files[i]))
                    list.Remove(files[i]);
            }
            if (!Eval(list.Count == 0, "Err_648ibm! {0} expected files not found.", list.Count))
            {
                foreach (String fileName in list)
                    Console.WriteLine(fileName);
            }
        }
    }

    [Fact]
    [ActiveIssue(0, PlatformID.Any)]
    public void Scenario3()
    {
        // Path is not in the current directory (same drive)
        //Scenario 3: Ensure that the path contains subdirectories and call this API and ensure that only the top directory files are returned
        /* Scenario disabled when porting because it modifies the filesystem outside of the test's working directory */
        String dirName;
        String[] expectedFiles;
        String[] files;
        List<String> list;

        dirName = ManageFileSystem.GetNonExistingDir(Path.DirectorySeparatorChar.ToString(), ManageFileSystem.DirPrefixName);
        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
        {
            expectedFiles = fileManager.GetAllFiles();
            list = new List<String>(expectedFiles);
            files = Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories);
            Eval(files.Length, list.Count, "Err_123rcm! wrong file count.");
            for (int i = 0; i < files.Length; i++)
            {
                //This will return as \<dirName>\<fileName> whereas our utility will return as <drive>:\<dirName>\<fileName>
                String fileFullName = Path.GetFullPath(files[i]);
                if (Eval(list.Contains(fileFullName), "Err_242yur! unexpected file found: {0}", fileFullName))
                    list.Remove(fileFullName);
            }
            if (!Eval(list.Count == 0, "Err_477xiv! {0} expected files not found.", list.Count))
            {
                foreach (String fileName in list)
                    Console.WriteLine(fileName);
            }
        }
    }

    [Fact]
    public void Scenario4()
    {
        //Scenario 4: searchPattern variations - valid search characters, file match exactly the searchPattern, searchPattern is a subset of existing files, superset, no match, 
        String dirName;
        String[] expectedFiles;
        String[] files;
        List<String> list;

        dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
        String searchPattern;
        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
        {
            expectedFiles = fileManager.GetAllFiles();
            //?
            int maxLen = 0;
            foreach (String file in expectedFiles)
            {
                String realFile = Path.GetFileNameWithoutExtension(file);
                if (realFile.Length > maxLen)
                    maxLen = realFile.Length;
            }
            searchPattern = String.Format("{0}.???", new String('?', maxLen));
            list = new List<String>(expectedFiles);
            files = Directory.GetFiles(dirName, searchPattern, SearchOption.AllDirectories);
            Eval(files.Length, list.Count, "Err_654wlf! wrong file count.");
            for (int i = 0; i < files.Length; i++)
            {
                if (Eval(list.Contains(files[i]), "Err_792olh! unexpected file found: {0}", files[i]))
                    list.Remove(files[i]);
            }
            if (!Eval(list.Count == 0, "Err_434gew! {0} expected files not found.", list.Count))
            {
                foreach (String fileName in list)
                    Console.WriteLine(fileName);
            }

            //file match exactly 
            searchPattern = Path.GetFileName(expectedFiles[0]);
            list = new List<String>(new String[] { expectedFiles[0] });
            files = Directory.GetFiles(dirName, searchPattern, SearchOption.AllDirectories);
            Eval(files.Length, list.Count, "Err_427fug! wrong file count.");
            for (int i = 0; i < files.Length; i++)
            {
                if (Eval(list.Contains(files[i]), "Err_382bzl! unexpected file found: {0}", files[i]))
                    list.Remove(files[i]);
            }
            if (!Eval(list.Count == 0, "Err_008xan! {0} expected files not found.", list.Count))
            {
                foreach (String fileName in list)
                    Console.WriteLine(fileName);
            }

            //subset
            String tempSearchPattern = Path.GetFileName(expectedFiles[0]).Substring(2);
            List<String> newFiles = new List<String>();
            foreach (String file in expectedFiles)
            {
                String realFile = Path.GetFileName(file);
                if (realFile.Substring(2).Equals(tempSearchPattern))
                    newFiles.Add(file);
            }
            searchPattern = String.Format("??{0}", tempSearchPattern);

            list = newFiles;
            files = Directory.GetFiles(dirName, searchPattern, SearchOption.AllDirectories);
            Eval(files.Length, list.Count, "Err_030bfw! wrong file count.");
            for (int i = 0; i < files.Length; i++)
            {
                if (Eval(list.Contains(files[i]), "Err_393mly! unexpected file found: {0}", files[i]))
                    list.Remove(files[i]);
            }
            if (!Eval(list.Count == 0, "Err_328gse! {0} expected files not found.", list.Count))
            {
                foreach (String fileName in list)
                    Console.WriteLine(fileName);
            }

            //there shouldn't be any with just the suffix
            searchPattern = tempSearchPattern;
            list = new List<String>();
            files = Directory.GetFiles(dirName, searchPattern, SearchOption.AllDirectories);
            Eval(files.Length, list.Count, "Err_747fnq! wrong file count.");

            //superset
            searchPattern = String.Format("blah{0}", Path.GetFileName(expectedFiles[0]));
            newFiles = new List<String>();
            foreach (String file in expectedFiles)
            {
                String realFile = Path.GetFileName(file);
                if (realFile.Equals(searchPattern))
                    newFiles.Add(file);
            }

            list = newFiles;
            files = Directory.GetFiles(dirName, searchPattern, SearchOption.AllDirectories);
            Eval(files.Length, list.Count, "Err_969vnk! wrong file count.");
            for (int i = 0; i < files.Length; i++)
            {
                if (Eval(list.Contains(files[i]), "Err_353ygu! unexpected file found: {0}", files[i]))
                    list.Remove(files[i]);
            }
            if (!Eval(list.Count == 0, "Err_830vvw! {0} expected files not found.", list.Count))
            {
                foreach (String fileName in list)
                    Console.WriteLine(fileName);
            }
        }
    }

    [Fact]
    public void Scenario7()
    {
        //Scenario 7: dir is readonly
        String dirName;
        String[] expectedFiles;
        String[] files;
        List<String> list;

        //Readonly
        dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
        {
            //now lets make this directory readonly?
            new DirectoryInfo(dirName).Attributes = new DirectoryInfo(dirName).Attributes | FileAttributes.ReadOnly;

            expectedFiles = fileManager.GetAllFiles();
            list = new List<String>(expectedFiles);
            files = Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories);
            Eval(files.Length, list.Count, "Err_862vhr! wrong file count.");

            for (int i = 0; i < files.Length; i++)
            {
                if (Eval(list.Contains(files[i]), "Err_556ioj! unexpected file found: {0}", files[i]))
                    list.Remove(files[i]);
            }
            if (!Eval(list.Count == 0, "Err_562xwh! {0} expected files not found.", list.Count))
            {
                foreach (String fileName in list)
                    Console.WriteLine(fileName);
            }
            if (Directory.Exists(dirName) && (new DirectoryInfo(dirName).Attributes & FileAttributes.ReadOnly) != 0)
                new DirectoryInfo(dirName).Attributes = new DirectoryInfo(dirName).Attributes ^ FileAttributes.ReadOnly;
        }
    }

    #endregion

    //Checks for error
    protected static bool Eval(bool expression, String msg, params Object[] values)
    {
        return Eval(expression, String.Format(msg, values));
    }

    protected static bool Eval<T>(T actual, T expected, String errorMsg)
    {
        bool retValue = expected == null ? actual == null : expected.Equals(actual);

        if (!retValue)
            Eval(retValue, errorMsg +
            " Expected:" + (null == expected ? "<null>" : expected.ToString()) +
            " Actual:" + (null == actual ? "<null>" : actual.ToString()));

        return retValue;
    }

    protected static bool Eval(bool expression, String msg)
    {
        if (!expression)
        {
            s_pass = false;
            Console.WriteLine(msg);
        }
        return expression;
    }

    //Checks for a particular type of exception
    private static void CheckException<E>(ExceptionCode test, string error)
    {
        CheckException<E>(test, error, null);
    }

    //Checks for a particular type of exception and an Exception msg
    private static void CheckException<E>(ExceptionCode test, string error, String msgExpected)
    {
        bool exception = false;
        try
        {
            test();
            error = String.Format("{0} Exception NOT thrown ", error);
        }
        catch (Exception e)
        {
            if (e.GetType() == typeof(E))
            {
                exception = true;
                if (System.Globalization.CultureInfo.CurrentUICulture.Name == "en-US" && msgExpected != null && e.Message != msgExpected)
                {
                    exception = false;
                    error = String.Format("{0} Message Different: <{1}>", error, e.Message);
                }
            }
            else
                error = String.Format("{0} Exception type: {1}, expected {2}", error, e.GetType().ToString(), typeof(E).ToString());
        }
        Eval(exception, error);
    }
}



