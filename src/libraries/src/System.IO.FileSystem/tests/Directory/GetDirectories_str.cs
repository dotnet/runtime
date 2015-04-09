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

public class Directory_GetDirectories_str
{
    public static String s_strDtTmVer = "2000/04/27 17:00";
    public static String s_strClassMethod = "Directory.GetDirectories()";
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

            DirectoryInfo dir2;
            String dirName = "GetDirectories_str_TestDir";
            DirectoryInfo[] dirArr;

            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);


            // [] Should throw ArgumentNullException for null argument
            //-----------------------------------------------------------------
            strLoc = "Loc_477g8";

            iCountTestcases++;
            try
            {
                Directory.GetDirectories(null);
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

            iCountTestcases++;
            try
            {
                Directory.GetDirectories(String.Empty);
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

            // [] ArgumentException for invalid path
            //-----------------------------------------------------------------
            strLoc = "Loc_1190x";

            iCountTestcases++;
            try
            {
                Directory.GetDirectories("\0");
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
            dirArr = dir2.GetDirectories();
            iCountTestcases++;
            if (dirArr.Length != 0)
            {
                iCountErrors++;
                printerr("Error_207v7! Incorrect number of directories returned");
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
            new FileInfo(dir2.ToString() + "TestFile1");
            new FileInfo(dir2.ToString() + "TestFile2");

            // [] Searchstring ending with '*'

            iCountTestcases++;
            dirArr = dir2.GetDirectories("TestDir*");
            iCountTestcases++;
            if (dirArr.Length != 3)
            {
                iCountErrors++;
                printerr("Error_1yt75! Incorrect number of directories returned");
            }


            String[] names = new String[3];
            int i = 0;
            foreach (DirectoryInfo d in dirArr)
                names[i++] = d.Name;

            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir1") < 0)
            {
                iCountErrors++;
                printerr("Error_3y775! Incorrect name==" + dirArr[0].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir2") < 0)
            {
                iCountErrors++;
                printerr("Error_90885! Incorrect name==" + dirArr[1].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir3") < 0)
            {
                iCountErrors++;
                printerr("Error_879by! Incorrect name==" + dirArr[2].Name);
            }


            // [] Searchstring is '*'

            dirArr = dir2.GetDirectories("*");
            iCountTestcases++;
            if (dirArr.Length != 5)
            {
                iCountErrors++;
                printerr("Error_t5792! Incorrect number of directories==" + dirArr.Length);
            }
            names = new String[5];
            i = 0;
            foreach (DirectoryInfo d in dirArr)
                names[i++] = d.Name;

            iCountTestcases++;
            if (Array.IndexOf(names, "Test1Dir1") < 0)
            {
                iCountErrors++;
                printerr("Error_4898v! Incorrect name==" + dirArr[0].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "Test1Dir2") < 0)
            {
                iCountErrors++;
                printerr("Error_4598c! Incorrect name==" + dirArr[1].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir1") < 0)
            {
                iCountErrors++;
                printerr("Error_209d8! Incorrect name==" + dirArr[2].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir2") < 0)
            {
                iCountErrors++;
                printerr("Error_10vtu! Incorrect name==" + dirArr[3].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir3") < 0)
            {
                iCountErrors++;
                printerr("Error_190vh! Incorrect name==" + dirArr[4].Name);
            }

            // [] Searchstring starting with '*'

            dirArr = dir2.GetDirectories("*Dir2");
            iCountTestcases++;
            if (dirArr.Length != 2)
            {
                iCountErrors++;
                printerr("Error_8019x! Incorrect number of directories==" + dirArr.Length);
            }

            names = new String[2];
            i = 0;
            foreach (DirectoryInfo d in dirArr)
                names[i++] = d.Name;
            iCountTestcases++;
            if (Array.IndexOf(names, "Test1Dir2") < 0)
            {
                iCountErrors++;
                printerr("Error_167yb! Incorrect name==" + dirArr[0].Name);
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestDir2") < 0)
            {
                iCountErrors++;
                printerr("Error_49yb7! Incorrect name==" + dirArr[1].Name);
            }

            // [] Exact match

            strLoc = "Loc_fy87e";

            dirArr = dir2.GetDirectories("Test1Dir2");
            iCountTestcases++;
            if (dirArr.Length != 1)
            {
                iCountErrors++;
                printerr("Error_2398v! Incorrect number of directories returned, expected==1, got==" + dirArr.Length);
            }
            iCountTestcases++;
            if (!dirArr[0].Name.Equals("Test1Dir2"))
            {
                iCountErrors++;
                printerr("Error_88gbb! Incorrect directory returned==" + dirArr[0]);
            }

            // [] Multiple wildcards

            strLoc = "Loc_48yg7";

            dirArr = dir2.GetDirectories("T*st*D*2");
            iCountTestcases++;
            if (dirArr.Length != 2)
            {
                iCountErrors++;
                printerr("Error_39g8b! Incorrect number of directories returned, expected==2, got==" + dirArr.Length);
            }


            // [] Searchstring starting and ending with '*'
            // [] Search is case insensitive
            dir2.CreateSubdirectory("AAABB");
            dir2.CreateSubdirectory("aaabbcc");

            dirArr = dir2.GetDirectories("*BB*");
            iCountTestcases++;
            if (dirArr.Length != (Interop.IsLinux ? 1 : 2)) // Linux is case-sensitive
            {
                iCountErrors++;
                printerr("Error_4y190! Incorrect number of directories==" + dirArr.Length);
            }
            names = new String[2];
            i = 0;
            foreach (DirectoryInfo d in dirArr)
                names[i++] = d.Name;

            iCountTestcases++;
            if (Array.IndexOf(names, "AAABB") < 0)
            {
                iCountErrors++;
                printerr("Error_956yb! Incorrect name==" + dirArr[0]);
            }

            if (!Interop.IsLinux) // linux is case-sensitive
            {
                iCountTestcases++;
                if (Array.IndexOf(names, "aaabbcc") < 0)
                {
                    iCountErrors++;
                    printerr("Error_48yg7! Incorrect name==" + dirArr[1]);
                }
            }

            // [] Should not search on fullpath
            // [] No match should return zero length array

            dirArr = dir2.GetDirectories("Directory");
            iCountTestcases++;
            if (dirArr.Length != 0)
            {
                iCountErrors++;
                printerr("Error_209v7! Incorrect number of directories==" + dirArr.Length);
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
            Console.WriteLine(s_strTFName + " : Error Err_8888yyy!  strLoc==" + strLoc + ", exc_general==" + exc_general.ToString());
        }
        ////  Finish Diagnostics

        if (iCountErrors != 0)
        {
            Console.WriteLine("FAiL! " + s_strTFName + " ,iCountErrors==" + iCountErrors.ToString());
        }

        Assert.Equal(iCountErrors, 0);
    }

    public static void printerr(String err, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine("ERROR: ({0}, {1}, {2}) {3}", memberName, filePath, lineNumber, err);
    }
}


