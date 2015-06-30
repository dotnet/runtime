// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Threading;
using Xunit;

public class DirectoryInfo_GetFileSystemInfos_str
{
    public static String s_strActiveBugNums = "28509";
    public static String s_strClassMethod = "Directory.GetFiles()";
    public static String s_strTFName = "GetFileSystemInfos _str.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
    public static void runTest()
    {
        //////////// Global Variables used for all tests
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        String dirName = Path.GetRandomFileName();

        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////

            DirectoryInfo dir2;
            FileSystemInfo[] fsArr;

            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);


            // [] Should throw ArgumentNullException for null argument
            //-----------------------------------------------------------------
            strLoc = "Loc_477g8";

            dir2 = new DirectoryInfo(".");
            iCountTestcases++;
            try
            {
                dir2.GetFileSystemInfos(null);
                iCountErrors++;
                printerr("Error_2988b! Expected exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0707t! Incorrect exception thrown, exc==" + exc.ToString());
            }
            //-----------------------------------------------------------------


            // [] ArgumentException for String.Empty
            //-----------------------------------------------------------------
            strLoc = "Loc_4yg7b";

            dir2 = new DirectoryInfo(".");
            iCountTestcases++;
            try
            {
                FileSystemInfo[] strInfos = dir2.GetFileSystemInfos(String.Empty);
                if (strInfos.Length != 0)
                {
                    iCountErrors++;
                    printerr("Error_8ytbm! Unexpected number of file infos returned" + strInfos.Length);
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_2908y! Incorrect exception thrown, exc==" + exc.ToString());
            }
            //-----------------------------------------------------------------

            // [] ArgumentException for all whitespace
            //-----------------------------------------------------------------
            strLoc = "Loc_1190x";

            dir2 = new DirectoryInfo(".");
            iCountTestcases++;
            try
            {
                dir2.GetFileSystemInfos(Path.Combine("..ab ab.. .. abc..d", "abc.."));
                iCountErrors++;
                printerr("Error_2198y! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_17888! Incorrect exception thrown, exc==" + exc.ToString());
            }
            //-----------------------------------------------------------------



            // [] Should return zero length array for an empty directory
            //-----------------------------------------------------------------
            strLoc = "Loc_4y982";

            dir2 = Directory.CreateDirectory(dirName);
            fsArr = dir2.GetFileSystemInfos();
            iCountTestcases++;
            if (fsArr.Length != 0)
            {
                iCountErrors++;
                printerr("Error_207v7! Incorrect number of files returned");
            }
            //-----------------------------------------------------------------


            // [] Create a directorystructure and try different searchcriterias
            //-----------------------------------------------------------------
            strLoc = "Loc_2398c";

            dir2.CreateSubdirectory("TestDir1");
            dir2.CreateSubdirectory("TestDir2");
            dir2.CreateSubdirectory("TestDir3");
            dir2.CreateSubdirectory("Test1Dir1");
            dir2.CreateSubdirectory("Test1Dir2");
            FileStream f1 = new FileInfo(Path.Combine(dir2.FullName, "TestFile1")).Create();
            FileStream f2 = new FileInfo(Path.Combine(dir2.FullName, "TestFile2")).Create();
            FileStream f3 = new FileInfo(Path.Combine(dir2.FullName, "TestFile3")).Create();
            FileStream f4 = new FileInfo(Path.Combine(dir2.FullName, "Test1File1")).Create();
            FileStream f5 = new FileInfo(Path.Combine(dir2.FullName, "Test1File2")).Create();

            // [] Search criteria ending with '*'

            iCountTestcases++;
            fsArr = dir2.GetFileSystemInfos("TestFile*");
            iCountTestcases++;
            if (fsArr.Length != 3)
            {
                iCountErrors++;
                printerr("Error_1yt75! Incorrect number of files returned");
            }
            String[] names = new String[fsArr.Length];
            int i = 0;
            foreach (FileSystemInfo f in fsArr)
                names[i++] = f.Name;

            if (!Interop.IsWindows) // test is expecting sorted order as provided by Windows
            {
                Array.Sort(names);
            }

            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile1") < 0)
            {
                iCountErrors++;
                printerr("Error_3y775! Incorrect name==" + fsArr[0].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile2") < 0)
            {
                iCountErrors++;
                printerr("Error_90885! Incorrect name==" + fsArr[1].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile3") < 0)
            {
                iCountErrors++;
                printerr("Error_879by! Incorrect name==" + fsArr[2].Name);
            }


            // [] Search criteria is '*'

            fsArr = dir2.GetFileSystemInfos("*");
            iCountTestcases++;
            if (fsArr.Length != 10)
            {
                iCountErrors++;
                printerr("Error_t5792! Incorrect number of files==" + fsArr.Length);
            }
            names = new String[fsArr.Length];
            i = 0;
            foreach (FileSystemInfo f in fsArr)
                names[i++] = f.Name;

            if (!Interop.IsWindows) // test is expecting sorted order as provided by Windows
            {
                Array.Sort(names);
            }

            iCountTestcases++;
            if (Array.IndexOf(names, "Test1Dir1") < 0)
            {
                iCountErrors++;
                printerr("Error_4898v! Incorrect name==" + fsArr[0].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "Test1Dir2") < 0)
            {
                iCountErrors++;
                printerr("Error_4598c! Incorrect name==" + fsArr[1].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir1") < 0)
            {
                iCountErrors++;
                printerr("Error_209d8! Incorrect name==" + fsArr[2].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir2") < 0)
            {
                iCountErrors++;
                printerr("Error_10vtu! Incorrect name==" + fsArr[3].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir3") < 0)
            {
                iCountErrors++;
                printerr("Error_190vh! Incorrect name==" + fsArr[4].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "Test1File1") < 0)
            {
                iCountErrors++;
                printerr("Error_4898v! Incorrect name==" + fsArr[5].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "Test1File2") < 0)
            {
                iCountErrors++;
                printerr("Error_4598c! Incorrect name==" + fsArr[6].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile1") < 0)
            {
                iCountErrors++;
                printerr("Error_209d8! Incorrect name==" + fsArr[7].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile2") < 0)
            {
                iCountErrors++;
                printerr("Error_10vtu! Incorrect name==" + fsArr[8].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile3") < 0)
            {
                iCountErrors++;
                printerr("Error_190vh! Incorrect name==" + fsArr[9].Name);
            }

            // [] Search criteria beginning with '*'

            fsArr = dir2.GetFileSystemInfos("*2");
            iCountTestcases++;
            if (fsArr.Length != 4)
            {
                iCountErrors++;
                printerr("Error_8019x! Incorrect number of files==" + fsArr.Length);
            }
            names = new String[fsArr.Length];
            i = 0;
            foreach (FileSystemInfo fs in fsArr)
                names[i++] = fs.Name;
            iCountTestcases++;
            if (Array.IndexOf(names, "Test1Dir2") < 0)
            {
                iCountErrors++;
                printerr("Error_247yg! Incorrect name==" + fsArr[0].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir2") < 0)
            {
                iCountErrors++;
                printerr("Error_24gy7! Incorrect name==" + fsArr[1].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "Test1File2") < 0)
            {
                iCountErrors++;
                printerr("Error_167yb! Incorrect name==" + fsArr[2].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile2") < 0)
            {
                iCountErrors++;
                printerr("Error_49yb7! Incorrect name==" + fsArr[3].Name);
            }



            fsArr = dir2.GetFileSystemInfos("*Dir2");
            iCountTestcases++;
            if (fsArr.Length != 2)
            {
                iCountErrors++;
                printerr("Error_948yv! Incorrect number of files==" + fsArr.Length);
            }
            names = new String[fsArr.Length];
            i = 0;
            foreach (FileSystemInfo fs in fsArr)
                names[i++] = fs.Name;
            iCountTestcases++;
            if (Array.IndexOf(names, "Test1Dir2") < 0)
            {
                iCountErrors++;
                printerr("Error_247yg! Incorrect name==" + fsArr[0].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir2") < 0)
            {
                iCountErrors++;
                printerr("Error_24gy7! Incorrect name==" + fsArr[1].Name);
            }

            // [] Search criteria Beginning and ending with '*'

            using (new FileInfo(Path.Combine(dir2.FullName, "AAABB")).Create())
            {
                Directory.CreateDirectory(Path.Combine(dir2.FullName, "aaabbcc"));

                fsArr = dir2.GetFileSystemInfos("*BB*");
                iCountTestcases++;
                if (fsArr.Length != (Interop.IsWindows ? 2 : 1))
                {
                    iCountErrors++;
                    printerr("Error_4y190! Incorrect number of files==" + fsArr.Length);
                }
                names = new String[fsArr.Length];
                i = 0;
                foreach (FileSystemInfo fs in fsArr)
                    names[i++] = fs.Name;

                if (Interop.IsWindows)
                {
                    iCountTestcases++;
                    if (Array.IndexOf(names, "aaabbcc") < 0)
                    {
                        iCountErrors++;
                        printerr("Error_956yb! Incorrect name==" + fsArr[0]);
                        foreach (FileSystemInfo s in fsArr)
                            Console.WriteLine(s.Name);
                    }
                }
                if (Array.IndexOf(names, "AAABB") < 0)
                {
                    iCountErrors++;
                    printerr("Error_48yg7! Incorrect name==" + fsArr[1]);
                    foreach (FileSystemInfo s in fsArr)
                        Console.WriteLine(s.Name);
                }
                strLoc = "Loc_0001";
            }

            // [] Should not search on fullpath
            // [] Search Criteria without match should return empty array

            fsArr = dir2.GetFileSystemInfos("Directory");
            if (fsArr.Length != 0)
            {
                iCountErrors++;
                printerr("Error_209v7! Incorrect number of files==" + fsArr.Length);
            }

            using (new FileInfo(Path.Combine(dir2.FullName, "TestDir1", "Test.tmp")).Create())
            {
                fsArr = dir2.GetFileSystemInfos(Path.Combine("TestDir1", "*"));
                if (fsArr.Length != 1)
                {
                    printerr("Error_28gyb! Incorrect number of files");
                }
            }
            f1.Dispose();
            f2.Dispose();
            f3.Dispose();
            f4.Dispose();
            f5.Dispose();
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

        FailSafeDirectoryOperations.DeleteDirectory(dirName, true);
        Assert.Equal(0, iCountErrors);
    }

    public static void printerr(String err, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine("ERROR: ({0}, {1}, {2}) {3}", memberName, filePath, lineNumber, err);
    }
}

