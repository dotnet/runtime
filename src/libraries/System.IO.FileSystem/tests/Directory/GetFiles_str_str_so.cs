// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections.Generic;
using Xunit;

public class Directory_GetFiles_str_str_so : Directory_GetFiles_str_str
{
    private delegate void ExceptionCode();

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
        String testDir = GetTestFileName();
        String testFile1 = Path.Combine(TestDirectory, GetTestFileName());
        String testFile2 = Path.Combine(TestDirectory, testDir, GetTestFileName());
        Directory.CreateDirectory(Path.Combine(TestDirectory, testDir));
        using (File.Create(testFile1))
        using (File.Create(testFile2))
        {
            String[] results = GetFiles(TestDirectory, "*", SearchOption.AllDirectories);
            Assert.Contains(testFile1, results);
            Assert.Contains(testFile2, results);
        }
    }

    [Fact]
    public void SearchPatternIncludeSubDirectories()
    {
        String testDir = GetTestFileName();
        String testFile1 = Path.Combine(TestDirectory, GetTestFileName());
        String testFile2 = Path.Combine(TestDirectory, testDir, GetTestFileName());
        Directory.CreateDirectory(Path.Combine(TestDirectory, testDir));
        using (File.Create(testFile1))
        using (File.Create(testFile2))
        {
            String[] results = GetFiles(Directory.GetCurrentDirectory(), Path.Combine(new DirectoryInfo(TestDirectory).Name, "*"), SearchOption.AllDirectories);
            Assert.Contains(testFile1, results);
            Assert.Contains(testFile2, results);
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
        String dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
        {
            String[] expectedFiles = fileManager.GetAllFiles();
            List<String> list = new List<String>(expectedFiles);
            String[] files = Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories);
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
        String dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 1, 10))
        {
            String[] expectedFiles = fileManager.GetAllFiles();
            List<String> list = new List<String>(expectedFiles);
            String[] files = Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories);
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
    public void Scenario4()
    {
        //Scenario 4: searchPattern variations - valid search characters, file match exactly the searchPattern, searchPattern is a subset of existing files, superset, no match, 
        String dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
        {
            String[] expectedFiles = fileManager.GetAllFiles();
            //?
            int maxLen = 0;
            foreach (String file in expectedFiles)
            {
                String realFile = Path.GetFileNameWithoutExtension(file);
                if (realFile.Length > maxLen)
                    maxLen = realFile.Length;
            }
            String searchPattern = String.Format("{0}.???", new String('?', maxLen));
            List<String> list = new List<String>(expectedFiles);
            String[] files = Directory.GetFiles(dirName, searchPattern, SearchOption.AllDirectories);
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
        String dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
        {
            //now lets make this directory readonly?
            new DirectoryInfo(dirName).Attributes = new DirectoryInfo(dirName).Attributes | FileAttributes.ReadOnly;

            String[] expectedFiles = fileManager.GetAllFiles();
            List<String> list = new List<String>(expectedFiles);
            String[] files = Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories);
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
            Console.WriteLine(msg);
        }
        return expression;
    }
}
