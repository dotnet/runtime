// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Runtime.CompilerServices;
using Xunit;

public class Directory_GetFileSystemEntries_str
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2001/02/12 21:00";
    public static String s_strClassMethod = "Directory.GetFileSystemEntries()";
    public static String s_strTFName = "GetFileSystemEntries_str.cs";

    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
    public static void runTest()
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;

        try
        {
            DirectoryInfo dir2;
            String dirName = "GetFileSystemEntries_str_TestDir";
            String[] strArr;

            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);

            strLoc = "Loc_4y982";

            // [] With null string
            iCountTestcases++;
            try
            {
                Directory.GetFileSystemEntries(null);
                iCountErrors++;
                printerr("Error_0002! Expected exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0003! Unexpected exceptiont thrown: " + exc.ToString());
            }

            // [] With an empty string.
            iCountTestcases++;
            try
            {
                Directory.GetFileSystemEntries("");
                iCountErrors++;
                printerr("Error_0004! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0005! Unexpected exceptiont thrown: " + exc.ToString());
            }

            // [] With white spaces....
            iCountTestcases++;
            try
            {
                Directory.GetFileSystemEntries("            ");
                iCountErrors++;
                printerr("Error_0008! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0009! Unexpected exceptiont thrown: " + exc.ToString());
            }

            //Directory that doesn't exist
            iCountTestcases++;
            try
            {
                Directory.GetFileSystemEntries(dirName);
                iCountErrors++;
                printerr("Error_1001! Expected exception not thrown");
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_1002! Unexpected exceptiont thrown: " + exc.ToString());
            }

            //TODO:: Add UNC path testcase.

            if (Interop.IsWindows)
            {
                //With wild character's
                iCountTestcases++;
                try
                {
                    String strTempDir = Path.Combine("dls;d", "442349-0", "v443094(*)(+*$#$*") + new string(Path.DirectorySeparatorChar, 2);
                    Directory.GetFileSystemEntries(strTempDir);
                    iCountErrors++;
                    printerr("Error_1003! Expected exception not thrown");
                }
                catch (ArgumentException)
                {
                }
                catch (Exception exc)
                {
                    iCountErrors++;
                    printerr("Error_1004! Unexpected exceptiont thrown: " + exc.ToString());
                }
            }

            //With lot's of \'s at the end
            iCountTestcases++;
            try
            {
                String strTempDir = Directory.GetCurrentDirectory() + new string(Path.DirectorySeparatorChar, 5);
                strArr = Directory.GetFileSystemEntries(strTempDir);
                if (strArr == null || strArr.Length == 0)
                {
                    printerr("Error_1234!!! INvalid number of file system entries count :: " + strArr.Length);
                    iCountErrors++;
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_1006! Unexpected exceptiont thrown: " + exc.ToString());
            }

            //With the current directory
            iCountTestcases++;
            try
            {
                strArr = Directory.GetFileSystemEntries(s_strTFPath);
                if (strArr == null || strArr.Length == 0)
                {
                    printerr("Error_2434!!! INvalid number of file system entries count :: " + strArr.Length);
                    iCountErrors++;
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_12321!!! Unexpected exceptiont thrown: " + exc.ToString());
            }

            //With valid directory
            dir2 = new DirectoryInfo(dirName);
            dir2.Create();

            strArr = Directory.GetFileSystemEntries(dirName);
            iCountTestcases++;
            if (strArr.Length != 0)
            {
                iCountErrors++;
                printerr("Error_207v7! Incorrect number of directories returned");
            }

            // [] Create a directorystructure get all the filesystementries
            //-----------------------------------------------------------------
            strLoc = "Loc_2398c";

            dir2.CreateSubdirectory("TestDir1");
            dir2.CreateSubdirectory("TestDir2");
            dir2.CreateSubdirectory("TestDir3");
            FileStream fs1 = new FileInfo(Path.Combine(dir2.ToString(), "TestFile1")).Create();
            FileStream fs2 = new FileInfo(Path.Combine(dir2.ToString(), "TestFile2")).Create();
            FileStream fs3 = new FileInfo(Path.Combine(dir2.ToString(), "Test.bat")).Create();
            FileStream fs4 = new FileInfo(Path.Combine(dir2.ToString(), "Test.exe")).Create();

            iCountTestcases++;
            strArr = Directory.GetFileSystemEntries(dir2.Name);
            iCountTestcases++;
            if (strArr.Length != 7)
            {
                iCountErrors++;
                printerr("Error_1yt75! Incorrect number of directories returned" + strArr.Length);
            }

            for (int iLoop = 0; iLoop < strArr.Length; iLoop++)
                strArr[iLoop] = Path.GetFileName(strArr[iLoop]);

            iCountTestcases++;
            if (Array.IndexOf(strArr, "TestDir1") < 0)
            {
                iCountErrors++;
                printerr("Error_4yg76! Incorrect name==" + strArr[0]);
            }
            iCountTestcases++;
            if (Array.IndexOf(strArr, "TestDir2") < 0)
            {
                iCountErrors++;
                printerr("Error_1987y! Incorrect name==" + strArr[1]);
            }
            iCountTestcases++;
            if (Array.IndexOf(strArr, "TestDir3") < 0)
            {
                iCountErrors++;
                printerr("Error_4yt76! Incorrect name==" + strArr[2]);
            }
            iCountTestcases++;
            if (Array.IndexOf(strArr, "Test.bat") < 0)
            {
                iCountErrors++;
                printerr("Error_3y775! Incorrect name==" + strArr[3]);
            }
            iCountTestcases++;
            if (Array.IndexOf(strArr, "Test.exe") < 0)
            {
                iCountErrors++;
                printerr("Error_90885! Incorrect name==" + strArr[4]);
            }
            iCountTestcases++;
            if (Array.IndexOf(strArr, "TestFile1") < 0)
            {
                iCountErrors++;
                printerr("Error_879by! Incorrect name==" + strArr[5]);
            }
            iCountTestcases++;
            if (Array.IndexOf(strArr, "TestFile2") < 0)
            {
                iCountErrors++;
                printerr("Error_29894! Incorrect name==" + strArr[6]);
            }

            fs1.Dispose();
            fs2.Dispose();
            fs3.Dispose();
            fs4.Dispose();
            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            printerr("Error Err_8888yyy!  strLoc==" + strLoc + ", exc_general==" + exc_general.ToString());
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
}




