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

public class DirectoryInfo_GetFiles
{
    public static String s_strActiveBugNums = "";
    public static String s_strClassMethod = "Directory.GetFiles()";
    public static String s_strTFName = "GetFiles.cs";
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
            FileInfo[] filArr;

            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);

            // [] Should return zero length array for an empty directory
            //-----------------------------------------------------------------
            strLoc = "Loc_4y982";

            dir2 = Directory.CreateDirectory(dirName);
            filArr = dir2.GetFiles();
            iCountTestcases++;
            if (filArr.Length != 0)
            {
                iCountErrors++;
                printerr("Error_207v7! Incorrect number of directories returned");
            }
            //-----------------------------------------------------------------


            // [] Create a directorystructure and get all the files
            //-----------------------------------------------------------------
            strLoc = "Loc_2398c";

            dir2.CreateSubdirectory("TestDir1");
            dir2.CreateSubdirectory("TestDir2");
            dir2.CreateSubdirectory("TestDir3");
            FileStream fs1 = new FileInfo(dir2.FullName + "\\" + "TestFile1").Create();
            FileStream fs2 = new FileInfo(dir2.FullName + "\\" + "TestFile2").Create();
            FileStream fs3 = new FileInfo(dir2.FullName + "\\" + "Test.bat").Create();
            FileStream fs4 = new FileInfo(dir2.FullName + "\\" + "Test.exe").Create();
            fs1.Dispose();
            fs2.Dispose();
            fs3.Dispose();
            fs4.Dispose();

            iCountTestcases++;
            filArr = dir2.GetFiles();
            iCountTestcases++;
            if (filArr.Length != 4)
            {
                iCountErrors++;
                printerr("Error_1yt75! Incorrect number of directories returned" + filArr.Length);
            }

            String[] names = new String[4];
            int i = 0;
            foreach (FileInfo f in filArr)
                names[i++] = f.Name;
            iCountTestcases++;
            if (Array.IndexOf(names, "Test.bat") < 0)
            {
                iCountErrors++;
                printerr("Error_3y775! Incorrect name==" + filArr[0].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "Test.exe") < 0)
            {
                iCountErrors++;
                printerr("Error_90885! Incorrect name==" + filArr[1].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile1") < 0)
            {
                iCountErrors++;
                printerr("Error_879by! Incorrect name==" + filArr[2].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile2") < 0)
            {
                iCountErrors++;
                printerr("Error_29894! Incorrect name==" + filArr[3].Name);
            }

            File.Delete(dirName + "\\TestFile1");
            File.Delete(dirName + "\\TestFile2");

            filArr = dir2.GetFiles();
            iCountTestcases++;
            if (filArr.Length != 2)
            {
                iCountErrors++;
                printerr("Error_4y28x! Incorrect number of directories returned");
            }

            names = new String[2];
            i = 0;
            foreach (FileInfo f in filArr)
                names[i++] = f.Name;

            iCountTestcases++;
            if (Array.IndexOf(names, "Test.bat") < 0)
            {
                iCountErrors++;
                printerr("Error_0975b! Incorrect name==" + filArr[0].Name);
            }
            if (Array.IndexOf(names, "Test.exe") < 0)
            {
                iCountErrors++;
                printerr("Error_928yb! Incorrect name==" + filArr[1].Name);
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

