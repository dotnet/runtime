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

public class DirectoryInfo_GetFiles_str
{
    public static String s_strActiveBugNums = "";
    public static String s_strClassMethod = "Directory.GetFiles(String)";
    public static String s_strTFName = "GetFiles_str.cs";
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
            String[] strArr;

            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);


            // [] Should throw ArgumentNullException for null argument
            //-----------------------------------------------------------------
            strLoc = "Loc_477g8";

            dir2 = new DirectoryInfo(".");
            iCountTestcases++;
            try
            {
                Directory.GetFiles(null);
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
                Directory.GetFiles(String.Empty);
                iCountErrors++;
                printerr("Error_8ytbm! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_2908y! Incorrect exception thrown, exc==" + exc.ToString());
            }
            //-----------------------------------------------------------------


            // [] ArgumentException for all whitespace
            //-----------------------------------------------------------------
            strLoc = "Loc_2g87y";

            iCountTestcases++;
            try
            {
                Directory.GetFiles("\n");
                iCountErrors++;
                printerr("Error_29019! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_9678g! Incorrect exception thrown, exc==" + exc.ToString());
            }
            //-----------------------------------------------------------------



            // [] Should return zero length array for an empty directory
            //-----------------------------------------------------------------
            strLoc = "Loc_4y982";

            dir2 = Directory.CreateDirectory(dirName);
            strArr = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\" + dirName);
            iCountTestcases++;
            if (strArr.Length != 0)
            {
                iCountErrors++;
                printerr("Error_207v7! Incorrect number of files returned");
            }
            //-----------------------------------------------------------------


            // [] Create a directorystructure and get all the files
            //-----------------------------------------------------------------
            strLoc = "Loc_2398c";
            FileStream[] fs = new FileStream[5];
            dir2.CreateSubdirectory("TestDir1");
            dir2.CreateSubdirectory("TestDir2");
            dir2.CreateSubdirectory("TestDir3");
            dir2.CreateSubdirectory("Test1Dir1");
            dir2.CreateSubdirectory("Test1Dir2");
            fs[0] = new FileInfo(dir2.FullName + "\\" + "TestFile1").Create();
            fs[1] = new FileInfo(dir2.FullName + "\\" + "TestFile2").Create();
            fs[2] = new FileInfo(dir2.FullName + "\\" + "TestFile3").Create();
            fs[3] = new FileInfo(dir2.FullName + "\\" + "Test1File1").Create();
            fs[4] = new FileInfo(dir2.FullName + "\\" + "Test1File2").Create();
            for (int iLoop = 0; iLoop < 5; iLoop++)
                fs[iLoop].Dispose();

            // Get all files
            strLoc = "Loc_4y7gb";

            strArr = Directory.GetFiles(".\\" + dirName);
            iCountTestcases++;
            if (strArr.Length != 5)
            {
                iCountErrors++;
                printerr("Error_t5792! Incorrect number of files==" + strArr.Length);
            }
            String[] names = new String[strArr.Length];
            int i = 0;
            foreach (String f in strArr)
            {
                names[i++] = f.Substring(f.LastIndexOf("\\") + 1);
            }

            iCountTestcases++;
            if (Array.IndexOf(names, "Test1File1") < 0)
            {
                iCountErrors++;
                printerr("Error_4898v! Incorrect name==" + strArr[0].ToString());
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "Test1File2") < 0)
            {
                iCountErrors++;
                printerr("Error_4598c! Incorrect name==" + strArr[1].ToString());
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile1") < 0)
            {
                iCountErrors++;
                printerr("Error_209d8! Incorrect name==" + strArr[2].ToString());
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile2") < 0)
            {
                iCountErrors++;
                printerr("Error_10vtu! Incorrect name==" + strArr[3].ToString());
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile3") < 0)
            {
                iCountErrors++;
                printerr("Error_190vh! Incorrect name==" + strArr[4].ToString());
            }

            strLoc = "Loc_45500";
            File.Delete(dirName + "\\TestFile1");
            File.Delete(dirName + "\\Test1File1");

            iCountTestcases++;
            strArr = Directory.GetFiles(".\\" + dirName);
            if (strArr.Length != 3)
            {
                iCountErrors++;
                printerr("Error_176yb! Incorrect number of files==" + strArr.Length);
            }
            names = new String[strArr.Length];
            i = 0;
            foreach (String f in strArr)
                names[i++] = f.Substring(f.LastIndexOf("\\") + 1);
            iCountTestcases++;
            if (Array.IndexOf(names, "Test1File2") < 0)
            {
                iCountErrors++;
                printerr("Error_78657! Incorrect name==" + strArr[0]);
            }

            strLoc = "Loc_455434";
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile2") < 0)
            {
                iCountErrors++;
                printerr("Error_9y1bh! Incorrect name==" + strArr[1]);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile3") < 0)
            {
                iCountErrors++;
                printerr("Error_2178d! Incorrect name==" + strArr[2]);
            }



            //-----------------------------------------------------------------


            // [] Some invalid directory testing
            //-----------------------------------------------------------------
            strLoc = "Loc_98yg5";

            iCountTestcases++;
            try
            {
                Directory.GetFiles(",");
                iCountErrors++;
                printerr("Error_2y675! Expected exception not thrown");
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_249y6! Incorrect exception thrown, exc==" + exc.ToString());
            }

            iCountTestcases++;
            try
            {
                Directory.GetFiles("DoesNotExist");
                iCountErrors++;
                printerr("Error_2y76b! Expected exception not thrown");
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_24y7g! Incorrect exception thrown, exc==" + exc.ToString());
            }

            //-----------------------------------------------------------------


            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);

            //See VSWhidbey #104091
            dir2 = new DirectoryInfo(".");
            try
            {
                dir2.GetFiles("<>");

                iCountErrors++;
                Console.WriteLine("Err_32497gs! No exception thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                iCountErrors++;
                Console.WriteLine("Err_234rd! Wrong exception thrown: {0}", ex);
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
}

