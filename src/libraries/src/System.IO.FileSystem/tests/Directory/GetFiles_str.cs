// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;
using Xunit;

public class Directory_GetFiles_str
{
    public static String s_strDtTmVer = "2000/04/28 14:00";
    public static String s_strClassMethod = "Directory.GetFiles()";
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
            FileInfo[] filArr;

            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);


            // [] Should throw ArgumentNullException for null argument
            //-----------------------------------------------------------------
            strLoc = "Loc_477g8";

            dir2 = new DirectoryInfo(".");
            iCountTestcases++;
            try
            {
                dir2.GetFiles(null);
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


            // [] Pass String.Empty as the search pattern
            //-----------------------------------------------------------------
            strLoc = "Loc_4yg7b";

            dir2 = new DirectoryInfo(".");
            iCountTestcases++;
            try
            {
                FileInfo[] fInfos = dir2.GetFiles(String.Empty);
                // To avoid OS differences we have decided not to throw an argument exception when empty
                // string passed. But we should return 0 FileInfo objects.
                if (fInfos.Length != 0)
                {
                    iCountErrors++;
                    printerr("Error_8ytbm! Invalid number of file infos are returned");
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
                FileInfo[] strFiles = dir2.GetFiles("\n");
                if (strFiles.Length != 0)
                {
                    iCountErrors++;
                    printerr("Error_2198y!Unexpected files retrieved..");
                }
            }
            catch (IOException)
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
            filArr = dir2.GetFiles();
            iCountTestcases++;
            if (filArr.Length != 0)
            {
                iCountErrors++;
                printerr("Error_207v7! Incorrect number of files returned");
            }
            //-----------------------------------------------------------------


            // [] Create a directorystructure and check it
            //-----------------------------------------------------------------
            strLoc = "Loc_2398c";

            dir2.CreateSubdirectory("TestDir1");
            dir2.CreateSubdirectory("TestDir2");
            dir2.CreateSubdirectory("TestDir3");
            dir2.CreateSubdirectory("Test1Dir1");
            dir2.CreateSubdirectory("Test1Dir2");

            new FileInfo(Path.Combine(dir2.FullName, "TestFile1")).Create();
            new FileInfo(Path.Combine(dir2.FullName, "TestFile2")).Create();
            new FileInfo(Path.Combine(dir2.FullName, "TestFile3")).Create();
            new FileInfo(Path.Combine(dir2.FullName, "Test1File1")).Create();
            new FileInfo(Path.Combine(dir2.FullName, "Test1File2")).Create();

            // [] Searchstring ending with '*'

            iCountTestcases++;
            filArr = dir2.GetFiles("TestFile*");
            iCountTestcases++;
            if (filArr.Length != 3)
            {
                iCountErrors++;
                printerr("Error_1yt75! Incorrect number of files returned" + filArr.Length);
            }

            String[] names = new String[3];
            int i = 0;
            foreach (FileInfo f in filArr)
                names[i++] = f.Name;

            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile1") < 0)
            {
                iCountErrors++;
                printerr("Error_3y775! Incorrect name==" + filArr[0].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile2") < 0)
            {
                iCountErrors++;
                printerr("Error_90885! Incorrect name==" + filArr[1].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile3") < 0)
            {
                iCountErrors++;
                printerr("Error_879by! Incorrect name==" + filArr[2].Name);
            }


            // [] SearchString is '*'

            filArr = dir2.GetFiles("*");
            iCountTestcases++;
            if (filArr.Length != 5)
            {
                iCountErrors++;
                printerr("Error_t5792! Incorrect number of files==" + filArr.Length);
            }

            names = new String[5];
            i = 0;
            foreach (FileInfo f in filArr)
                names[i++] = f.Name;

            iCountTestcases++;
            if (Array.IndexOf(names, "Test1File1") < 0)
            {
                iCountErrors++;
                printerr("Error_4898v! Incorrect name==" + filArr[0].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "Test1File2") < 0)
            {
                iCountErrors++;
                printerr("Error_4598c! Incorrect name==" + filArr[1].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile1") < 0)
            {
                iCountErrors++;
                printerr("Error_209d8! Incorrect name==" + filArr[2].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile2") < 0)
            {
                iCountErrors++;
                printerr("Error_10vtu! Incorrect name==" + filArr[3].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile3") < 0)
            {
                iCountErrors++;
                printerr("Error_190vh! Incorrect name==" + filArr[4].Name);
            }

            // [] Searchstring starting with '*'

            filArr = dir2.GetFiles("*File2");
            iCountTestcases++;
            if (filArr.Length != 2)
            {
                iCountErrors++;
                printerr("Error_8019x! Incorrect number of files==" + filArr.Length);
            }

            names = new String[2];
            i = 0;
            foreach (FileInfo f in filArr)
                names[i++] = f.Name;

            iCountTestcases++;
            if (Array.IndexOf(names, "Test1File2") < 0)
            {
                iCountErrors++;
                printerr("Error_167yb! Incorrect name==" + filArr[0].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile2") < 0)
            {
                iCountErrors++;
                printerr("Error_49yb7! Incorrect name==" + filArr[1].Name);
            }

            // [] Multiple wildcards in searchstring

            strLoc = "Loc_9438y";
            filArr = dir2.GetFiles("*es*f*l*");
            iCountTestcases++;
            if (filArr.Length != 5)
            {
                iCountErrors++;
                printerr("Error_38fy3! Incorrect number of files returned, expected==5, got==" + filArr.Length);
            }


            // [] Searchstring beginning and ending with '*'
            // [] Search should be case insensitive
            dirName = Path.GetRandomFileName();
            Directory.CreateDirectory(dirName);
            dir2 = new DirectoryInfo(dirName);
            FileInfo fi1 = new FileInfo(Path.Combine(dirName, "AAABB"));
            FileInfo fi2 = new FileInfo(Path.Combine(dirName, "aaabbcc"));
            FileStream fs1 = fi1.Create();
            FileStream fs2 = fi2.Create();
            fs1.Dispose();
            fs2.Dispose();

            filArr = dir2.GetFiles("*BB*");
            iCountTestcases++;
            if (filArr.Length != 2)
            {
                iCountErrors++;
                printerr("Error_4y190! Incorrect number of files==" + filArr.Length);
            }
            names = new String[2];
            i = 0;
            foreach (FileInfo f in filArr)
            {
                names[i++] = f.Name;
            }

            iCountTestcases++;
            if (Array.IndexOf(names, "AAABB") < 0)
            {
                iCountErrors++;
                printerr("Error_956yb! Incorrect name==" + filArr[0].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "aaabbcc") < 0)
            {
                iCountErrors++;
                printerr("Error_48yg7! Incorrect name==" + filArr[1].Name);
            }

            // [] Exact match

            strLoc = "Loc_3y8cc";

            filArr = dir2.GetFiles("AAABB");
            iCountTestcases++;
            if (filArr.Length != 1)
            {
                iCountErrors++;
                printerr("Error_398s1! Incorrect number of files==" + filArr.Length);
            }
            iCountTestcases++;
            if (!filArr[0].Name.Equals("AAABB"))
            {
                iCountErrors++;
                printerr("Error_3298y! Incorrect file name==" + filArr[0].Name);
            }

            // [] Should not search on fullpath
            // [] Should return zero length array when no match
            strLoc = "Loc_5435";

            filArr = dir2.GetFiles("Directory");
            iCountTestcases++;
            if (filArr.Length != 0)
            {
                iCountErrors++;
                printerr("Error_209v7! Incorrect number of files==" + filArr.Length);
            }

            dirName = Path.GetRandomFileName();
            dir2 = new DirectoryInfo(dirName);
            dir2.Create();
            FileStream fs = new FileInfo(Path.Combine(dirName, Path.GetRandomFileName())).Create();
            fs.Dispose();
            filArr = dir2.GetFiles("*");
            iCountTestcases++;
            if (filArr.Length != 1)
            {
                iCountErrors++;
                printerr("Error_28gyb! Incorrect number of files");
            }

            //-----------------------------------------------------------------


            ///////////////////////////////////////////////////////////////////
            /////////////////////////// END TESTS /////////////////////////////
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            printerr("Err_8888yyy!  strLoc==" + strLoc + ", exc_general==" + exc_general.ToString());
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


