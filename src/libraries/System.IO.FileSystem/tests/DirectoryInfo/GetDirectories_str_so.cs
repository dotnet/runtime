// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
We test this API's functionality comprehensively via Directory.GetDirectories(String, String, SearchOption). Here, we concentrate on the following
 - vanilla
 - parm validation, including non existent dir
 - security
**/
using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections.Generic;
using System.Security;
using System.Globalization;
using Xunit;

public class DirectoryInfo_GetDirectories_str_so
{
    private delegate void ExceptionCode();
    private static bool s_pass = true;

    [Fact]
    public static void RunTest()
    {
        try
        {
            String dirName;
            DirectoryInfo dirInfo;
            String[] expectedDirs;
            DirectoryInfo[] dirs;
            List<String> list;

            //Scenario 1: Vanilla
            try
            {
                dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    dirInfo = new DirectoryInfo(dirName);
                    expectedDirs = fileManager.GetAllDirectories();
                    list = new List<String>(expectedDirs);
                    dirs = dirInfo.GetDirectories("*", SearchOption.AllDirectories);
                    Eval(dirs.Length == list.Count, "Err_415mbz! wrong count {0} {1}", dirs.Length, list.Count);
                    for (int i = 0; i < expectedDirs.Length; i++)
                    {
                        if (Eval(list.Contains(dirs[i].FullName), "Err_287kkm! No file found: {0}", dirs[i].FullName))
                            list.Remove(dirs[i].FullName);
                    }
                    if (!Eval(list.Count == 0, "Err_921mhs! wrong count: {0}", list.Count))
                    {
                        Console.WriteLine();
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_349t7g! Exception caught in scenario: {0}", ex);
            }

            //Scenario 3: Parm Validation
            try
            {
                // dir not present and then after creating
                dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);
                dirInfo = new DirectoryInfo(dirName);
                CheckException<DirectoryNotFoundException>(delegate { dirs = dirInfo.GetDirectories("*", SearchOption.AllDirectories); }, "Err_326pgt! worng exception thrown");

                // create the dir and then check that we dont cache this info
                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    dirInfo = new DirectoryInfo(dirName);
                    expectedDirs = fileManager.GetAllDirectories();
                    list = new List<String>(expectedDirs);
                    dirs = dirInfo.GetDirectories("*", SearchOption.AllDirectories);
                    Eval(dirs.Length == list.Count, "Err_948kxt! wrong count");
                    for (int i = 0; i < expectedDirs.Length; i++)
                    {
                        if (Eval(list.Contains(dirs[i].FullName), "Err_535xaj! No file found: {0}", dirs[i].FullName))
                            list.Remove(dirs[i].FullName);
                    }
                    if (!Eval(list.Count == 0, "Err_370pjl! wrong count: {0}", list.Count))
                    {
                        Console.WriteLine();
                        foreach (String fileName in list)
                            Console.WriteLine(fileName);
                    }
                    CheckException<ArgumentNullException>(delegate { dirs = dirInfo.GetDirectories(null, SearchOption.TopDirectoryOnly); }, "Err_751mwu! worng exception thrown");
                    CheckException<ArgumentOutOfRangeException>(delegate { dirs = dirInfo.GetDirectories("*", (SearchOption)100); }, "Err_589kvu! worng exception thrown - see bug #386545");
                    CheckException<ArgumentOutOfRangeException>(delegate { dirs = dirInfo.GetDirectories("*", (SearchOption)(-1)); }, "Err_359vcj! worng exception thrown - see bug #386545");
                    String[] invalidValuesForSearch = { "..", @"..\" };
                    for (int i = 0; i < invalidValuesForSearch.Length; i++)
                    {
                        CheckException<ArgumentException>(delegate { dirs = dirInfo.GetDirectories(invalidValuesForSearch[i], SearchOption.TopDirectoryOnly); }, String.Format("Err_631bwy! worng exception thrown: {1}", i, invalidValuesForSearch[i]));
                    }
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_006dwq! Exception caught in scenario: {0}", ex);
            }



            //Scenario for bug #461014 - Getting files/directories of CurrentDirectory of drives are broken
            /* Scenario disabled when porting because it relies on the contents of the current directory not changing during execution
            try
            {
                string anotherDrive = IOServices.GetNtfsDriveOtherThanCurrent();
                String[] paths = null == anotherDrive ? new String[] { Directory.GetCurrentDirectory() } : new String[] { anotherDrive, Directory.GetCurrentDirectory() };
                String path;
                for (int i = 0; i < paths.Length; i++)
                {
                    path = paths[i];
                    if (path.Length > 1)
                    {
                        path = path.Substring(0, 2);

                        DirectoryInfo[] f1 = new DirectoryInfo(Path.GetFullPath(path)).GetDirectories();
                        DirectoryInfo[] f2 = new DirectoryInfo(path).GetDirectories();
                        Eval<int>(f1.Length, f2.Length, "Err_2497gds! wrong value");
                        for (int j = 0; j < f1.Length; j++)
                        {
                            Eval<String>(f1[j].FullName, f2[j].FullName, "Err_03284t! wrong value");
                            Eval<String>(f1[j].Name, f2[j].Name, "Err_03284t! wrong value");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_349t7g! Exception caught in scenario: {0}", ex);
            }
            */
        }
        catch (Exception ex)
        {
            s_pass = false;
            Console.WriteLine("Err_234rsgf! Uncaught exception in RunTest: {0}", ex);
        }

        Assert.True(s_pass);
    }

    private void DeleteFile(String fileName)
    {
        if (File.Exists(fileName))
            File.Delete(fileName);
    }

    private void DeleteDir(String dirName)
    {
        if (Directory.Exists(dirName))
            Directory.Delete(dirName);
    }

    //Checks for error
    private static bool Eval(bool expression, String msg, params Object[] values)
    {
        return Eval(expression, String.Format(msg, values));
    }

    private static bool Eval<T>(T actual, T expected, String errorMsg)
    {
        bool retValue = expected == null ? actual == null : expected.Equals(actual);

        if (!retValue)
            Eval(retValue, errorMsg +
            " Expected:" + (null == expected ? "<null>" : expected.ToString()) +
            " Actual:" + (null == actual ? "<null>" : actual.ToString()));

        return retValue;
    }

    private static bool Eval(bool expression, String msg)
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

    //Checks for a particular type of exception and an Exception msg in the English locale
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
                if (msgExpected != null && System.Globalization.CultureInfo.CurrentUICulture.Name == "en-US" && e.Message != msgExpected)
                {
                    exception = false;
                    error = String.Format("{0} Message Different: <{1}>", error, e.Message);
                }
            }
            else
                error = String.Format("{0} Exception type: {1}", error, e.GetType().Name);
        }
        Eval(exception, error);
    }
}





