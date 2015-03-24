// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
We test this API's functionality comprehensively via Directory.GetFiles(String, String, SearchOption). Here, we concentrate on the following
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

public class DirectoryInfo_FetFiles_str_so
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
            String[] expectedFiles;
            FileInfo[] files;
            List<String> list;

            //Scenario 1: Vanilla
            try
            {
                dirName = ManageFileSystem.GetNonExistingDir(Directory.GetCurrentDirectory(), ManageFileSystem.DirPrefixName);

                using (ManageFileSystem fileManager = new ManageFileSystem(dirName, 3, 100))
                {
                    dirInfo = new DirectoryInfo(dirName);
                    expectedFiles = fileManager.GetAllFiles();
                    list = new List<String>(expectedFiles);
                    files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);
                    Eval(files.Length == list.Count, "Err_415mbz! wrong count");
                    for (int i = 0; i < expectedFiles.Length; i++)
                    {
                        if (Eval(list.Contains(files[i].FullName), "Err_287kkm! No file found: {0}", files[i].FullName))
                            list.Remove(files[i].FullName);
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
                CheckException<DirectoryNotFoundException>(delegate { files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories); }, "Err_326pgt! worng exception thrown");

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
                    CheckException<ArgumentNullException>(delegate { files = dirInfo.GetFiles(null, SearchOption.TopDirectoryOnly); }, "Err_751mwu! worng exception thrown");
                    CheckException<ArgumentOutOfRangeException>(delegate { files = dirInfo.GetFiles("*.*", (SearchOption)100); }, "Err_589kvu! worng exception thrown - see bug #386545");
                    CheckException<ArgumentOutOfRangeException>(delegate { files = dirInfo.GetFiles("*.*", (SearchOption)(-1)); }, "Err_359vcj! worng exception thrown - see bug #386545");
                    String[] invalidValuesForSearch = { "..", @"..\" };
                    for (int i = 0; i < invalidValuesForSearch.Length; i++)
                    {
                        CheckException<ArgumentException>(delegate { files = dirInfo.GetFiles(invalidValuesForSearch[i], SearchOption.TopDirectoryOnly); }, String.Format("Err_631bwy! worng exception thrown: {1}", i, invalidValuesForSearch[i]));
                    }
                    Char[] invalidFileNames = Path.GetInvalidFileNameChars();
                    for (int i = 0; i < invalidFileNames.Length; i++)
                    {
                        switch (invalidFileNames[i])
                        {
                            case '\\':
                            case '/':
                                CheckException<DirectoryNotFoundException>(delegate { files = dirInfo.GetFiles(String.Format("te{0}st", invalidFileNames[i].ToString()), SearchOption.TopDirectoryOnly); }, String.Format("Err_631bwy_{0}! worng exception thrown: {1} - bug#387196", i, (int)invalidFileNames[i]));
                                break;
                            case ':':
                                //History:
                                // 1) we assumed that this will work in all non-9x machine
                                // 2) Then only in XP
                                // 3) NTFS?
                                if (FileSystemDebugInfo.IsCurrentDriveNTFS())
                                    CheckException<IOException>(delegate { files = dirInfo.GetFiles(String.Format("te{0}st", invalidFileNames[i].ToString()), SearchOption.TopDirectoryOnly); }, String.Format("Err_997gqs! worng exception thrown: {1} - bug#387196", i, (int)invalidFileNames[i]));
                                else
                                {
                                    try
                                    {
                                        files = dirInfo.GetFiles(String.Format("te{0}st", invalidFileNames[i].ToString()), SearchOption.TopDirectoryOnly);
                                    }
                                    catch (IOException)
                                    {
                                        Console.WriteLine(FileSystemDebugInfo.MachineInfo());
                                        Eval(false, "Err_3947g! Another OS throwing for DI.GetFiles(). modify the above check after confirming the v1.x behavior in that machine");
                                        /**
                                                try
                                                {
                                                      DirectoryInfo dirInfo = new DirectoryInfo(".");
                                                    FileInfo[] files = dirInfo.GetFiles("te:st"); 
                                                pass=false;
                                                Console.WriteLine("No exception thrown");
                                                }
                                            catch(IOException){}
                                                catch (Exception ex)
                                                {
                                                pass=false;
                                                Console.WriteLine("Err_723jvl! Different Exception caught in scenario: {0}", ex);
                                                }

                                        **/
                                    }
                                }
                                break;
                            case '*':
                            case '?':
                                files = dirInfo.GetFiles(String.Format("te{0}st", invalidFileNames[i].ToString()), SearchOption.TopDirectoryOnly);
                                break;
                            default:
                                CheckException<ArgumentException>(delegate { files = dirInfo.GetFiles(String.Format("te{0}st", invalidFileNames[i].ToString()), SearchOption.TopDirectoryOnly); }, String.Format("Err_036gza! worng exception thrown: {1} - bug#387196", i, (int)invalidFileNames[i]));
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_006dwq! Exception caught in scenario: {0}", ex);
            }

            //Scenario for bug #461014 - Getting files/directories of CurrentDirectory of drives are broken
            /* Test disabled while porting because it relies on state outside of its working directory not changing over time
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




