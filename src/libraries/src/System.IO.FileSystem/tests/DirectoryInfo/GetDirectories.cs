// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

public class DirectoryInfo_GetDirectories
{
    public static String s_strActiveBugNums = "";
    public static String s_strClassMethod = "Directory.GetDirectories()";
    public static String s_strTFName = "GetDirectories.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    private static bool s_pass = true;

    [Fact]
    public static void runTest()
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;


        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////

            DirectoryInfo dir2;
            String dirName = Path.GetRandomFileName();

            DirectoryInfo[] dirArr;

            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);

            // [] Should return zero length array for an empty directory
            //-----------------------------------------------------------------
            strLoc = "Loc_4y982";

            dir2 = Directory.CreateDirectory(dirName);
            dirArr = dir2.GetDirectories();
            iCountTestcases++;
            if (dirArr.Length != 0)
            {
                iCountErrors++;
                printerr("Error_207v7! Incorrect number of directories returned");
            }
            //-----------------------------------------------------------------


            // [] Create a directorystructure and get all the directories
            //-----------------------------------------------------------------
            strLoc = "Loc_2398c";

            dir2.CreateSubdirectory("TestDir1");
            dir2.CreateSubdirectory("TestDir2");
            dir2.CreateSubdirectory("TestDir3");
            new FileInfo(dir2.ToString() + "TestFile1");
            new FileInfo(dir2.ToString() + "TestFile2");

            iCountTestcases++;
            dirArr = dir2.GetDirectories();
            iCountTestcases++;
            if (dirArr.Length != 3)
            {
                iCountErrors++;
                printerr("Error_1yt75! Incorrect number of directories returned");
            }

            if (!Interop.IsWindows) // test is expecting sorted order as provided by Windows
            {
                Array.Sort(dirArr, Comparer<DirectoryInfo>.Create((left, right) => left.FullName.CompareTo(right.FullName)));
            }

            iCountTestcases++;
            if (!dirArr[0].Name.Equals("TestDir1"))
            {
                iCountErrors++;
                printerr("Error_3y775! Incorrect name==" + dirArr[0].Name);
            }
            iCountTestcases++;
            if (!dirArr[1].Name.Equals("TestDir2"))
            {
                iCountErrors++;
                printerr("Error_90885! Incorrect name==" + dirArr[1].Name);
            }
            iCountTestcases++;
            if (!dirArr[2].Name.Equals("TestDir3"))
            {
                iCountErrors++;
                printerr("Error_879by! Incorrect name==" + dirArr[2].Name);
            }

            Directory.Delete(Path.Combine(dirName, "TestDir3"));
            Directory.Delete(Path.Combine(dirName, "TestDir2"));

            dirArr = dir2.GetDirectories();
            iCountTestcases++;
            if (dirArr.Length != 1)
            {
                iCountErrors++;
                printerr("Error_4y28x! Incorrect number of directories returned");
            }
            iCountTestcases++;
            if (!dirArr[0].Name.Equals("TestDir1"))
            {
                iCountErrors++;
                printerr("Error_0975b! Incorrect name==" + dirArr[0].Name);
            }


            //-----------------------------------------------------------------



            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);

            //bug ####### - DirectoryInfo.GetDirectories() uses relative path and thus the current directory rather than the full directory passed on 
            //DirectoryInfos internally constructed
            /**
For folders under the root DirectoryInfo everything is fine, but any subfolder of that turns into <workingdir>\foldername
             
So for example if my working dir is 
C:\wd
             
And Im looking at a directory structure
C:\foo\bar\abc
             
If I do repro c:\foo
It prints out something like
C:\foo\bar
C:\wd\bar\abc
            **/

            if (Interop.IsWindows) // test expects that '/' is not root
            {
                //@TODO!! use ManageFileSystem utility
                String rootTempDir = Path.DirectorySeparatorChar + "LaksTemp";
                int dirSuffixNo = 0;
                while (Directory.Exists(String.Format("{0}{1}", rootTempDir, dirSuffixNo++))) ;
                rootTempDir = String.Format("{0}{1}", rootTempDir, (dirSuffixNo - 1));
                String tempFullName = Path.Combine(rootTempDir, "laks1", "laks2");
                Directory.CreateDirectory(tempFullName);
                DirectoryInfo dir = new DirectoryInfo(rootTempDir);
                AddFolders(dir);
                Eval(s_directories.Count == 3, "Err_9347g! wrong count: {0}", s_directories.Count);
                Eval(s_directories.Contains(Path.GetFullPath(rootTempDir)), "Err_3407tgs! PAth not found: {0}", Path.GetFullPath(rootTempDir));
                Eval(s_directories.Contains(Path.Combine(Path.GetFullPath(rootTempDir), "laks1")), "Err_857sqi! PAth not found: {0}", Path.Combine(Path.GetFullPath(rootTempDir), "laks1"));
                Eval(s_directories.Contains(Path.Combine(Path.GetFullPath(rootTempDir), Path.Combine("laks1", "laks2"))), "Err_356slh! PAth not found: {0}", Path.Combine(Path.GetFullPath(rootTempDir), "laks1", "laks2"));
                Directory.Delete(rootTempDir, true);
            }

            if (!s_pass)
            {
                iCountErrors++;
                printerr("Error_32947eg! Relative directories not working");
            }

            ///////////////////////////////////////////////////////////////////
            /////////////////////////// END TESTS /////////////////////////////
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine("Error Err_8888yyy!  strLoc==" + strLoc + ", exc_general==" + exc_general.ToString());
        }
        ////  Finish Diagnostics

        if (iCountErrors != 0)
        {
            Console.WriteLine("FAiL! " + s_strTFName + " ,iCountErrors==" + iCountErrors.ToString());
        }

        Assert.Equal(0, iCountErrors);
    }

    public static void printerr(String err, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine("ERROR: ({0}, {1}, {2}) {3}", memberName, filePath, lineNumber, err);
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



    private static List<String> s_directories = new List<String>();
    public static void AddFolders(DirectoryInfo info)
    {
        s_directories.Add(info.FullName);
        //		Console.WriteLine("<{0}>", info.FullName);
        foreach (DirectoryInfo i in info.GetDirectories())
        {
            AddFolders(i);
        }
    }
}

