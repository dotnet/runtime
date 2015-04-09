// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Runtime.CompilerServices;
using Xunit;

public class Directory_GetFileSystemEntries_str_str
{
    public static String s_strDtTmVer = "2001/02/12 22:00";
    public static String s_strClassMethod = "Directory.GetFileSystemEntries()";
    public static String s_strTFName = "GetFileSystemEntries_str_str.cs";
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
            String dirName = "GetFileSystemEntries_str_str_TestDir";
            String[] strArr;

            FailSafeDirectoryOperations.DeleteDirectory(dirName, true);

            strLoc = "Loc_4y982";

            // [] With null file name
            iCountTestcases++;
            try
            {
                Directory.GetFileSystemEntries(null, "*");
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

            // [] With empty file name
            iCountTestcases++;
            try
            {
                Directory.GetFileSystemEntries("", "*");
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

            //Directory that doesn't exist
            iCountTestcases++;
            try
            {
                Directory.GetFileSystemEntries(dirName, "*");
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

            //With wild character's as file name
            iCountTestcases++;
            try
            {
                String strTempDir = Path.Combine("dls;d", "442349-0", "v443094(*)(+*$#$*") + new string(Path.DirectorySeparatorChar, 3);
                Directory.GetFileSystemEntries(strTempDir, "*");
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

            //With spaces as file name
            iCountTestcases++;
            try
            {
                String strTempDir = "             ";
                Directory.GetFileSystemEntries(strTempDir, "*");
                iCountErrors++;
                printerr("Error_1044! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_1023! Unexpected exceptiont thrown: " + exc.ToString());
            }

            dir2 = new DirectoryInfo(dirName);
            dir2.Create();

            // [] With null search pattern 
            iCountTestcases++;
            try
            {
                Directory.GetFileSystemEntries(dirName, null);
                iCountErrors++;
                printerr("Error_02002! Expected exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_00023! Unexpected exceptiont thrown: " + exc.ToString());
            }

            // [] With empty search pattern
            iCountTestcases++;
            try
            {
                String[] strInfos = Directory.GetFileSystemEntries(dirName, "");
                // To avoid OS differences we have decided not to throw an argument exception when empty
                // string passed. But we should return 0 items.
                if (strInfos.Length != 0)
                {
                    iCountErrors++;
                    printerr("Error_9004! Invalid number of directories returned");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_9005! Unexpected exceptiont thrown: " + exc.ToString());
            }

            //With wild character's as search pattern
            iCountTestcases++;
            try
            {
                String strTempDir = Path.Combine("dls;d", "442349-0", "v443094(*)(+*$#$*") + new string(Path.DirectorySeparatorChar, 3);
                Directory.GetFileSystemEntries(dirName, strTempDir);
                iCountErrors++;
                printerr("Error_3003! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_3004! Unexpected exceptiont thrown: " + exc.ToString());
            }

            //Valid characters for search pattern
            iCountTestcases++;
            try
            {
                String strTempDir = "a..b abc..d";
                Directory.GetFileSystemEntries(dirName, strTempDir);
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_4004! Unexpected exceptiont thrown: " + exc.ToString());
            }

            //Invalid characters for search pattern
            iCountTestcases++;
            try
            {
                String strTempDir = Path.Combine("..ab ab.. .. abc..d", "abc..");
                Directory.GetFileSystemEntries(dirName, strTempDir);
                iCountErrors++;
                printerr("Error_144003! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_10404! Unexpected exceptiont thrown: " + exc.ToString());
            }

            strLoc = "Loc_0001";
            //Path that doesn't exist 
            iCountTestcases++;
            try
            {
                strArr = Directory.GetFileSystemEntries("ThisDirectoryShouldNotExist", "*");
                iCountErrors++;
                printerr("Error_14443! Expected exception not thrown");
            }
            catch (IOException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_10433! Unexpected exceptiont thrown: " + exc.ToString());
            }

            //With lot's of \'s at the end of file name
            iCountTestcases++;
            try
            {
                String strTempDir = Directory.GetCurrentDirectory() + new string(Path.DirectorySeparatorChar, 5);
                strArr = Directory.GetFileSystemEntries(strTempDir, "*");
                if (strArr == null || strArr.Length == 0)
                {
                    printerr("Error_1234!!! INvalid number of file system entries count :: " + strArr.Length);
                    iCountErrors++;
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_10406! Unexpected exceptiont thrown: " + exc.ToString());
            }

            //With the current directory
            iCountTestcases++;
            try
            {
                strArr = Directory.GetFileSystemEntries(s_strTFPath, "*");
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

            strArr = Directory.GetFileSystemEntries(dirName, "*");
            iCountTestcases++;
            if (strArr.Length != 0)
            {
                iCountErrors++;
                printerr("Error_207v7! Incorrect number of directories returned");
            }
            //-----------------------------------------------------------------


            // [] Create a directorystructure get all the filesystementries
            //-----------------------------------------------------------------
            strLoc = "Loc_2398c";

            dir2.CreateSubdirectory("TestDir1");
            dir2.CreateSubdirectory("TestDir2");
            dir2.CreateSubdirectory("TestDir3");
            FileStream fs1 = new FileInfo(Path.Combine(dir2.ToString(), "TestFile1")).Create();
            FileStream fs2 = new FileInfo(Path.Combine(dir2.ToString(), "TestFile2")).Create();
            FileStream fs3 = new FileInfo(Path.Combine(dir2.ToString(), "Test1File2")).Create();
            FileStream fs4 = new FileInfo(Path.Combine(dir2.ToString(), "Test1Dir2")).Create();

            //white spaces for search pattern
            strArr = Directory.GetFileSystemEntries(dirName, "           ");
            iCountTestcases++;
            if (strArr.Length != 0)
            {
                Console.WriteLine(dir2.ToString());
                iCountErrors++;
                printerr("Error_24324! Incorrect number of directories returned" + strArr.Length);
            }

            //Search pattern with '?'.
            strArr = Directory.GetFileSystemEntries(dirName, "Test1*");
            iCountTestcases++;
            if (strArr.Length != 2)
            {
                iCountErrors++;
                printerr("Error_24343! Incorrect number of directories returned" + strArr.Length);
            }

            strArr = Directory.GetFileSystemEntries(dir2.Name, "*");
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
            if (Array.IndexOf(strArr, "Test1File2") < 0)
            {
                iCountErrors++;
                printerr("Error_3y775! Incorrect name==" + strArr[3]);
            }
            iCountTestcases++;
            if (Array.IndexOf(strArr, "Test1Dir2") < 0)
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

            // [] Search criteria beginning with '*'

            strArr = Directory.GetFileSystemEntries(dir2.Name, "*2");
            iCountTestcases++;
            if (strArr.Length != 4)
            {
                iCountErrors++;
                printerr("Error_8019x! Incorrect number of files==" + strArr.Length);
            }

            for (int iLoop = 0; iLoop < strArr.Length; iLoop++)
                strArr[iLoop] = Path.GetFileName(strArr[iLoop]);

            iCountTestcases++;
            if (Array.IndexOf(strArr, "Test1Dir2") < 0)
            {
                iCountErrors++;
                printerr("Error_247yg! Incorrect name==" + strArr[0]);
            }
            iCountTestcases++;
            if (Array.IndexOf(strArr, "TestDir2") < 0)
            {
                iCountErrors++;
                printerr("Error_24gy7! Incorrect name==" + strArr[1]);
            }
            iCountTestcases++;
            if (Array.IndexOf(strArr, "Test1File2") < 0)
            {
                iCountErrors++;
                printerr("Error_167yb! Incorrect name==" + strArr[2]);
            }
            iCountTestcases++;
            if (Array.IndexOf(strArr, "TestFile2") < 0)
            {
                iCountErrors++;
                printerr("Error_49yb7! Incorrect name==" + strArr[3]);
            }

            strArr = Directory.GetFileSystemEntries(dir2.Name, "*Dir2");
            iCountTestcases++;
            if (strArr.Length != 2)
            {
                iCountErrors++;
                printerr("Error_948yv! Incorrect number of files==" + strArr.Length);
            }
            for (int iLoop = 0; iLoop < strArr.Length; iLoop++)
                strArr[iLoop] = Path.GetFileName(strArr[iLoop]);

            iCountTestcases++;
            if (Array.IndexOf(strArr, "Test1Dir2") < 0)
            {
                iCountErrors++;
                printerr("Error_247yg! Incorrect name==" + strArr[0]);
            }
            iCountTestcases++;
            if (Array.IndexOf(strArr, "TestDir2") < 0)
            {
                iCountErrors++;
                printerr("Error_24gy7! Incorrect name==" + strArr[1]);
            }

            // [] Search criteria Beginning and ending with '*'

            new FileInfo(Path.Combine(dir2.FullName, "AAABB")).Create();
            Directory.CreateDirectory(Path.Combine(dir2.FullName, "aaabbcc"));

            strArr = Directory.GetFileSystemEntries(dir2.Name, "*BB*");

            iCountTestcases++;
            if (strArr.Length != (Interop.IsLinux ? 1 : 2)) // Linux is case-sensitive
            {
                iCountErrors++;
                printerr("Error_4y190! Incorrect number of files==" + strArr.Length);
            }
            for (int iLoop = 0; iLoop < strArr.Length; iLoop++)
                strArr[iLoop] = Path.GetFileName(strArr[iLoop]);

            if (!Interop.IsLinux) // Linux is case-sensitive
            {
                iCountTestcases++;
                if (Array.IndexOf(strArr, "aaabbcc") < 0)
                {
                    iCountErrors++;
                    printerr("Error_956yb! Incorrect name==" + strArr[0]);
                }
            }
            iCountTestcases++;
            if (Array.IndexOf(strArr, "AAABB") < 0)
            {
                iCountErrors++;
                printerr("Error_48yg7! Incorrect name==" + strArr[1]);
            }

            // [] Should not search on fullpath
            // [] Search Criteria without match should return empty array

            strLoc = "Loc_2301";
            FileInfo f = new FileInfo(Path.Combine(dir2.Name, "TestDir1", "Test.tmp"));
            FileStream fs6 = f.Create();
            strArr = Directory.GetFileSystemEntries(dir2.Name, Path.Combine("TestDir1", "*"));
            iCountTestcases++;
            if (strArr.Length != 1)
            {
                iCountErrors++;
                printerr("Error_28gyb! Incorrect number of files");
            }
            fs6.Dispose();

            //if(Directory.Exists(dirName))
            //	Directory.Delete(dirName, true);
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



