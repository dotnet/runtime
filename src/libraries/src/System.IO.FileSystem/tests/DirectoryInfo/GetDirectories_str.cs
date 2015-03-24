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

public class DirectoryInfo_GetDirectories_str
{
    public static String s_strActiveBugNums = "";
    public static String s_strClassMethod = "DirectoryInfo.GetDirectories(String searchPattern)";
    public static String s_strTFName = "GetDirectories_str.cs";
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

            DirectoryInfo dir1, dir2;
            String dirName = Path.GetRandomFileName();
            DirectoryInfo[] dirA;

            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);


            // [] Should throw ArgumentNullException for null argument
            //-----------------------------------------------------------------
            strLoc = "Loc_477g8";

            dir2 = new DirectoryInfo(".");
            iCountTestcases++;
            try
            {
                dir2.GetDirectories(null);
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
                dirA = dir2.GetDirectories(String.Empty);
                if (dirA.Length != 0)
                {
                    iCountErrors++;
                    printerr("Error_8ytbm! Expected exception not thrown");
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
                dirA = dir2.GetDirectories("\n");
                if (dirA.Length != 0)
                {
                    iCountErrors++;
                    printerr("Error_2198y! Expected exception not thrown");
                }
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
            dirA = dir2.GetDirectories("*");
            iCountTestcases++;
            if (dirA.Length != 0)
            {
                iCountErrors++;
                printerr("Error_207v7! Incorrect number of directories returned");
            }
            //-----------------------------------------------------------------


            // Regression test for Dev10 #540781 - passing "."
            // [] Should return zero length array for an empty directory
            //-----------------------------------------------------------------
            strLoc = "Loc_540781";

            dirA = dir2.GetDirectories(".");
            iCountTestcases++;
            if (dirA.Length != 0)
            {
                iCountErrors++;
                printerr("Error_540781! Incorrect number of directories returned");
                foreach (DirectoryInfo di in dirA)
                {
                    Console.WriteLine(di.FullName);
                }
            }
            //-----------------------------------------------------------------


            // [] Create a directorystructure and get all directories
            //-----------------------------------------------------------------
            strLoc = "Loc_2398c";

            dir2.CreateSubdirectory("TestDir1");
            dir2.CreateSubdirectory("TestDir2");
            dir2.CreateSubdirectory("TestDir3");
            dir2.CreateSubdirectory("Test1Dir1");
            dir2.CreateSubdirectory("Test1Dir2");
            FileStream[] fs = new FileStream[2];
            fs[0] = new FileInfo(dir2.FullName + "\\" + "TestFile1").Create();
            fs[1] = new FileInfo(dir2.FullName + "\\" + "TestFile2").Create();
            for (int iLoop = 0; iLoop < 2; iLoop++)
                fs[iLoop].Dispose();


            // Get all directories
            strLoc = "Loc_249yv";

            dirA = dir2.GetDirectories("*");
            iCountTestcases++;
            if (dirA.Length != 5)
            {
                iCountErrors++;
                printerr("Error_t5792! Incorrect number of directories==" + dirA.Length);
            }
            String[] names = new String[dirA.Length];
            int i = 0;
            foreach (DirectoryInfo f in dirA)
                names[i++] = f.FullName.Substring(f.FullName.LastIndexOf("\\") + 1);
            iCountTestcases++;
            if (Array.IndexOf(names, "Test1Dir1") < 0)
            {
                iCountErrors++;
                printerr("Error_4898v! Incorrect name==" + dirA[0].ToString());
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "Test1Dir2") < 0)
            {
                iCountErrors++;
                printerr("Error_4598c! Incorrect name==" + dirA[1].ToString());
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir1") < 0)
            {
                iCountErrors++;
                printerr("Error_209d8! Incorrect name==" + dirA[2].ToString());
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir2") < 0)
            {
                iCountErrors++;
                printerr("Error_10vtu! Incorrect name==" + dirA[3].ToString());
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir3") < 0)
            {
                iCountErrors++;
                printerr("Error_190vh! Incorrect name==" + dirA[4].ToString());
            }

            Directory.Delete(dirName + "\\TestDir2");
            Directory.Delete(dirName + "\\TestDir3");

            dirA = dir2.GetDirectories("*");
            iCountTestcases++;
            if (dirA.Length != 3)
            {
                iCountErrors++;
                printerr("Error_0989b! Incorrec tnumber of directories==" + dirA.Length);
            }
            names = new String[dirA.Length];
            i = 0;
            foreach (DirectoryInfo f in dirA)
                names[i++] = f.FullName.Substring(f.FullName.LastIndexOf("\\") + 1);
            iCountTestcases++;
            if (Array.IndexOf(names, "Test1Dir1") < 0)
            {
                iCountErrors++;
                printerr("Error_10938! Incorrect name==" + dirA[0]);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "Test1Dir2") < 0)
            {
                iCountErrors++;
                printerr("Error_01c99! Incorrect name==" + dirA[1].ToString());
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir1") < 0)
            {
                iCountErrors++;
                printerr("Error_176y7! Incorrect name==" + dirA[2].ToString());
            }

            dir1 = new DirectoryInfo(".");

            dirA = dir1.GetDirectories(String.Format("{0}\\*", dirName));

            if (dirA.Length != 3)
            {
                iCountErrors++;
                printerr("Error_0989b! Incorrec tnumber of directories==" + dirA.Length);
            }
            names = new String[dirA.Length];
            i = 0;
            foreach (DirectoryInfo f in dirA)
                names[i++] = f.FullName.Substring(f.FullName.LastIndexOf("\\") + 1);
            iCountTestcases++;
            if (Array.IndexOf(names, "Test1Dir1") < 0)
            {
                iCountErrors++;
                printerr("Error_10938! Incorrect name==" + dirA[0]);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "Test1Dir2") < 0)
            {
                iCountErrors++;
                printerr("Error_01c99! Incorrect name==" + dirA[1].ToString());
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir1") < 0)
            {
                iCountErrors++;
                printerr("Error_176y7! Incorrect name==" + dirA[2].ToString());
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

