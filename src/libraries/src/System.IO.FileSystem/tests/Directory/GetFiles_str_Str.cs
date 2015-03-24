// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

public class Directory_GetFiles_str_str
{
    public static String s_strDtTmVer = "2000/04/28 14:00";
    public static String s_strClassMethod = "Directory.GetFiles()";
    public static String s_strTFName = "GetFiles_str_str.cs";
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
            String dirName = "GetFiles_str_str_test_TestDir";
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
                Directory.GetFiles(null, "*");
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

            iCountTestcases++;
            try
            {
                Directory.GetFiles(".", null);
                iCountErrors++;
                printerr("Error_2y867! Expected exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_39yb7! Incorrect exception thrown, exc==" + exc.ToString());
            }


            //-----------------------------------------------------------------





            // [] ArgumentException for String.Empty
            //-----------------------------------------------------------------
            strLoc = "Loc_4yg7b";

            dir2 = new DirectoryInfo(".");
            iCountTestcases++;
            try
            {
                Directory.GetFiles(String.Empty, "*");
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


            iCountTestcases++;
            try
            {
                String[] strFiles = Directory.GetFiles(".", String.Empty);
                if (strFiles.Length != 0)
                {
                    iCountErrors++;
                    printerr("Error_478b8! Incorrect number of files");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_21999! Incorrect exception thrown, exc==" + exc.ToString());
            }


            //-----------------------------------------------------------------

            // [] ArgumentException for all whitespace for filename and searchstring
            //-----------------------------------------------------------------
            strLoc = "Loc_1190x";

            iCountTestcases++;
            try
            {
                String[] str = Directory.GetFiles(".", "       ");
                if (str.Length != 0)
                {
                    Console.WriteLine("Number of files :: " + str.Length);
                    iCountErrors++;
                    printerr("Error_43432! Incorrect number of files");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_17888! Incorrect exception thrown, exc==" + exc.ToString());
            }

            iCountTestcases++;
            try
            {
                Directory.GetFiles("     ", ".");
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
            strArr = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\" + dirName, "*");
            iCountTestcases++;
            if (strArr.Length != 0)
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
            new FileInfo(dir2.FullName + "\\" + "TestFile1").Create();
            new FileInfo(dir2.FullName + "\\" + "TestFile2").Create();
            new FileInfo(dir2.FullName + "\\" + "TestFile3").Create();
            new FileInfo(dir2.FullName + "\\" + "Test1File1").Create();
            new FileInfo(dir2.FullName + "\\" + "Test1File2").Create();

            // [] SearchString ending with '*'

            iCountTestcases++;
            strArr = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\" + dirName, "TestFile*");
            iCountTestcases++;
            if (strArr.Length != 3)
            {
                iCountErrors++;
                printerr("Error_1yt75 Incorrect number of files returned==" + strArr.Length);
            }

            String[] names = new String[strArr.Length];
            int i = 0;
            foreach (String f in strArr)
                names[i++] = f.Substring(f.ToString().LastIndexOf("\\") + 1);

            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile1") < 0)
            {
                iCountErrors++;
                printerr("Error_3y775! Incorrect name==" + strArr[0].ToString());
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile2") < 0)
            {
                iCountErrors++;
                printerr("Error_90885! Incorrect name==" + strArr[1].ToString());
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile3") < 0)
            {
                iCountErrors++;
                printerr("Error_879by! Incorrect name==" + strArr[2].ToString());
            }


            // Search string is '*'
            strLoc = "Loc_4y7gb";

            strArr = Directory.GetFiles(".\\" + dirName, "*");
            iCountTestcases++;
            if (strArr.Length != 5)
            {
                iCountErrors++;
                printerr("Error_t5792! Incorrect number of files==" + strArr.Length);
            }
            names = new String[strArr.Length];
            i = 0;
            foreach (String f in strArr)
                names[i++] = f.Substring(f.ToString().LastIndexOf("\\") + 1);

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

            strLoc = "Loc_4yg7b";

            strArr = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\" + dirName, "*File2");
            iCountTestcases++;
            if (strArr.Length != 2)
            {
                iCountErrors++;
                printerr("Error_8019x! Incorrect number of files==" + strArr.Length);
            }
            names = new String[strArr.Length];
            i = 0;
            foreach (String f in strArr)
                names[i++] = f.Substring(f.ToString().LastIndexOf("\\") + 1);
            iCountTestcases++;
            if (Array.IndexOf(names, "Test1File2") < 0)
            {
                iCountErrors++;
                printerr("Error_167yb! Incorrect name==" + strArr[0].ToString());
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "TestFile2") < 0)
            {
                iCountErrors++;
                printerr("Error_49yb7! Incorrect name==" + strArr[1].ToString());
            }


            // [] Searchstring beginning and ending with '*'
            // [] Search should be case insensitive.
            strLoc = "Loc_767b7";

            new FileInfo(dir2.FullName + "\\" + "AAABB").Create();
            new FileInfo(dir2.FullName + "\\" + "aaabbcc").Create();

            strArr = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\" + dirName, "*BB*");
            iCountTestcases++;
            if (strArr.Length != 2)
            {
                iCountErrors++;
                printerr("Error_4y190! Incorrect number of files==" + strArr.Length);
            }
            names = new String[strArr.Length];
            i = 0;
            foreach (String f in strArr)
                names[i++] = f.Substring(f.ToString().LastIndexOf("\\") + 1);
            iCountTestcases++;
            if (Array.IndexOf(names, "AAABB") < 0)
            {
                iCountErrors++;
                printerr("Error_956yb! Incorrect name==" + strArr[0].ToString());
            }
            iCountTestcases++;
            if (Array.IndexOf(names, "aaabbcc") < 0)
            {
                iCountErrors++;
                printerr("Error_48yg7! Incorrect name==" + strArr[1].ToString());
            }

            // [] Exact match on searchstring

            strLoc = "Loc_38yf8";

            strArr = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\" + dirName, "AAABB");
            iCountTestcases++;
            if (strArr.Length != 1)
            {
                iCountErrors++;
                printerr("Error_20989! Incorrect number of files==" + strArr.Length);
            }
            if ((strArr[0].ToString().IndexOf("AAABB")) == -1)
            {
                iCountErrors++;
                printerr("Error_4y8v8! Incorrect name==" + strArr[0].ToString());
            }


            // [] Should not search on fullpath
            // [] No match returns zero length array
            strLoc = "Loc_yg78b";

            strArr = Directory.GetFiles(".\\" + dirName, "Directory");
            iCountTestcases++;
            if (strArr.Length != 0)
            {
                iCountErrors++;
                printerr("Error_209v7! Incorrect number of files==" + strArr.Length);
            }

            new FileInfo(dir2.FullName + "\\" + "TestDir1\\Test.tmp").Create();
            strArr = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\" + dirName, "TestDir1\\*");
            iCountTestcases++;
            if (strArr.Length != 1)
            {
                iCountErrors++;
                printerr("Error_28gyb! Incorrect number of files");
            }

            //-----------------------------------------------------------------


            // [] Some invalid directory testing
            //-----------------------------------------------------------------
            strLoc = "Loc_98yg5";

            iCountTestcases++;
            try
            {
                Directory.GetFiles(",", "*");
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

            strLoc = "Loc_98001";

            iCountTestcases++;
            try
            {
                Directory.GetFiles("DoesNotExist", "*");
                iCountErrors++;
                printerr("Error_2y76b! Expected exception not thrown");
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Err_24y7g! Incorrect exception thrown, exc==" + exc.ToString());
            }

            //bug #417100 - not sure if this hard coded approach is safe in all 9x platforms!!!
            //But RunAnotherScenario is probably more accurate
            try
            {
                int[] validGreaterThan128ButLessThans160 = { 129, 133, 141, 143, 144, 157 };
                for (i = 0; i < validGreaterThan128ButLessThans160.Length; i++)
                {
                    Directory.GetFiles(".", ((Char)validGreaterThan128ButLessThans160[i]).ToString());
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Err_247gdo! Incorrect exception thrown, exc==" + exc.ToString());
            }

            try
            {
                for (i = 160; i < 256; i++)
                {
                    Directory.GetFiles(".", ((Char)i).ToString());
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Err_960tli! Incorrect exception thrown, exc==" + exc.ToString());
            }

#if DESKTOP
            try {
                if(!RunAnotherScenario())
                {
                    iCountErrors++;
                    printerr( "Error_2937efg! RunAnotherScenario failed");
                }
            } catch (Exception exc) {
                iCountErrors++;
                printerr( "Err_960tli! Incorrect exception thrown, exc=="+exc.ToString());
            }
#endif
            //bug VSWhdibey #580357 - A customer ran into an issue with Directory.GetFiles and short file name
            //We search for files with "2006" and get some even though the directory doesn't contain any files with 2006
            try
            {
                String[] files = { "20050512_1600_ImpressionPart106_ClickSummary.DAT", "20050512_1600_ImpressionPart126_ClickSummary.DAT", "20050512_1600_ImpressionPart40_ClickSummary.DAT", "20050512_1600_ImpressionPart42_ClickSummary.DAT", "20050512_1600_ImpressionPart44_ClickSummary.DAT", "20050512_1600_ImpressionPart46_ClickSummary.DAT", "20050512_1600_ImpressionPart48_ClickSummary.DAT", "20050512_1600_ImpressionPart50_ClickSummary.DAT", "20050512_1600_ImpressionPart52_ClickSummary.DAT", "20050512_1600_ImpressionPart54_ClickSummary.DAT", "20050512_1600_ImpressionPart56_ClickSummary.DAT", "20050512_1600_ImpressionPart58_ClickSummary.DAT", "20050513_1400_ImpressionPart116_ClickSummary.DAT", "20050513_1400_ImpressionPart41_ClickSummary.DAT", "20050513_1400_ImpressionPart43_ClickSummary.DAT", "20050513_1400_ImpressionPart45_ClickSummary.DAT", "20050513_1400_ImpressionPart47_ClickSummary.DAT", "20050513_1400_ImpressionPart49_ClickSummary.DAT", "20050513_1400_ImpressionPart51_ClickSummary.DAT", "20050513_1400_ImpressionPart53_ClickSummary.DAT", "20050513_1400_ImpressionPart55_ClickSummary.DAT", "20050513_1400_ImpressionPart57_ClickSummary.DAT", "20050513_1400_ImpressionPart59_ClickSummary.DAT" };
                i = 0;
                String basePath = "laks";
                String path;
                do
                {
                    path = String.Format("{0}_{1}", basePath, i++);
                } while (Directory.Exists(path) || File.Exists(path));
                Directory.CreateDirectory(path);
                foreach (String file in files)
                {
                    File.CreateText(Path.Combine(path, file)).Dispose();
                }

                String[] filterList = Directory.GetFiles(path, "2006*");
                int expected = 0;
                if (filterList.Length != expected)
                {
                    iCountErrors++;
                    Console.WriteLine("Error_32497sdg! Wrong file count: returned: {0}, expeced: {1}", filterList.Length, expected);
                }

                Directory.Delete(path, true);
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Err_247gdo! Incorrect exception thrown, exc==" + exc.ToString());
            }
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




