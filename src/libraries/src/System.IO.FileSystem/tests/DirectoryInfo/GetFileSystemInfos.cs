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

public class DirectoryInfo_GetFileSystemInfos
{
    public static String s_strActiveBugNums = "";
    public static String s_strClassMethod = "Directory.GetFiles()";
    public static String s_strTFName = "GetFileSystemInfos .cs";
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
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////

            DirectoryInfo dir2;
            String dirName = Path.GetRandomFileName();
            FileSystemInfo[] fsArr;

            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);

            // [] Should return zero length array for an empty directory
            //-----------------------------------------------------------------
            strLoc = "Loc_4y982";

            dir2 = Directory.CreateDirectory(dirName);
            fsArr = dir2.GetFileSystemInfos();
            iCountTestcases++;
            if (fsArr.Length != 0)
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
            new FileInfo(dir2.ToString() + "TestFile1");
            new FileInfo(dir2.ToString() + "TestFile2");
            new FileInfo(dir2.ToString() + "Test.bat");
            new FileInfo(dir2.ToString() + "Test.exe");

            FileStream[] fs = new FileStream[4];
            fs[0] = new FileInfo(dir2.FullName + "\\" + "TestFile1").Create();
            fs[1] = new FileInfo(dir2.FullName + "\\" + "TestFile2").Create();
            fs[2] = new FileInfo(dir2.FullName + "\\" + "Test.bat").Create();
            fs[3] = new FileInfo(dir2.FullName + "\\" + "Test.exe").Create();
            for (int iLoop = 0; iLoop < 4; iLoop++)
                fs[iLoop].Dispose();

            iCountTestcases++;
            fsArr = dir2.GetFileSystemInfos();
            iCountTestcases++;
            if (fsArr.Length != 7)
            {
                iCountErrors++;
                printerr("Error_1yt75! Incorrect number of directories returned");
            }

            String[] names = new String[7];
            int i = 0;
            foreach (FileSystemInfo fse in fsArr)
                names[i++] = fse.Name;

            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir1") < 0)
            {
                iCountErrors++;
                printerr("Error_4yg76! Incorrec tname==" + fsArr[0].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir2") < 0)
            {
                iCountErrors++;
                printerr("Error_1987y! Incorrect name==" + fsArr[1].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir3") < 0)
            {
                iCountErrors++;
                printerr("Error_4yt76! Incorrect name==" + fsArr[2].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "Test.bat") < 0)
            {
                iCountErrors++;
                printerr("Error_3y775! Incorrect name==" + fsArr[3].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "Test.exe") < 0)
            {
                iCountErrors++;
                printerr("Error_90885! Incorrect name==" + fsArr[4].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile1") < 0)
            {
                iCountErrors++;
                printerr("Error_879by! Incorrect name==" + fsArr[5].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile2") < 0)
            {
                iCountErrors++;
                printerr("Error_29894! Incorrect name==" + fsArr[6].Name);
            }



            //-----------------------------------------------------------------



            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);

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
}

