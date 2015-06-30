// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
We test this API's functionality comprehensively via Directory.GetFiles(String, String, SearchOption). Here, we concentrate on the following
 - vanilla
 - parm validation, including non existent dir
 - security
**/

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class DirectoryInfo_GetFiles_str_so : Directory_GetFiles_str_str_so
{
    #region Utilities

    public override String[] GetFiles(String path)
    {
        return ((new DirectoryInfo(path).GetFiles("*", SearchOption.TopDirectoryOnly).Select(x => x.FullName)).ToArray());
    }

    public override String[] GetFiles(String path, String searchPattern)
    {
        return ((new DirectoryInfo(path).GetFiles(searchPattern, SearchOption.TopDirectoryOnly).Select(x => x.FullName)).ToArray());
    }

    public override String[] GetFiles(String path, String searchPattern, SearchOption option)
    {
        return ((new DirectoryInfo(path).GetFiles(searchPattern, option).Select(x => x.FullName)).ToArray());
    }

    #endregion

    [Fact]
    public void ParmValidation()
    {
        String dirName;
        DirectoryInfo dirInfo;
        String[] expectedFiles;
        FileInfo[] files;
        List<String> list;

        //Scenario 3: Parm Validation
        // dir not present and then after creating
        dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
        Assert.Throws<DirectoryNotFoundException>(() => GetFiles(dirName, "*.*", SearchOption.AllDirectories));

        // create the dir and then check that we dont cache this info
        using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
        {
            dirInfo = new DirectoryInfo(dirName);
            expectedFiles = fileManager.GetAllFiles();
            list = new List<String>(expectedFiles);
            files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);
            Eval(files.Length == list.Count, "Err_948kxt! wrong count");
            for (int i = 0; i < expectedFiles.Length; i++)
            {
                if (Eval(list.Contains(files[i].FullName), "Err_535xaj! No file found: {0}", files[i].FullName))
                    list.Remove(files[i].FullName);
            }
            if (!Eval(list.Count == 0, "Err_370pjl! wrong count: {0}", list.Count))
            {
                Console.WriteLine();
                foreach (String fileName in list)
                    Console.WriteLine(fileName);
            }
        }
    }

    [Fact]
    [ActiveIssue(0, PlatformID.Any)]
    public void CurrentDirectoryOfDrive()
    {
        //Scenario for bug #461014 - Getting files/directories of CurrentDirectory of drives are broken
        /* Test disabled while porting because it relies on state outside of its working directory not changing over time */
        string anotherDrive = IOServices.GetNtfsDriveOtherThanCurrent();
        String[] paths = null == anotherDrive ? new String[] { Directory.GetCurrentDirectory() } : new String[] { anotherDrive, Directory.GetCurrentDirectory() };
        String path;

        for (int i = 0; i < paths.Length; i++)
        {
            path = paths[i];
            if (path.Length > 1)
            {
                path = path.Substring(0, 2);

                FileInfo[] f1 = new DirectoryInfo(Path.GetFullPath(path)).GetFiles();
                FileInfo[] f2 = new DirectoryInfo(path).GetFiles();
                Eval<int>(f1.Length, f2.Length, "Err_2497gds! wrong value");
                for (int j = 0; j < f1.Length; j++)
                {
                    Eval<String>(f1[j].FullName, f2[j].FullName, "Err_03284t! wrong value");
                    Eval<String>(f1[j].Name, f2[j].Name, "Err_03284t! wrong value");
                }
            }
        }
    }
}




