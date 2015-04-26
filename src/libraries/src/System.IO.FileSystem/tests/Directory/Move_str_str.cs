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

public class Directory_Move_str_str
{
    public static String s_strDtTmVer = "2000/07/12 10:42";
    public static String s_strClassMethod = "Directory.Move(String,String)";
    public static String s_strTFName = "Move_str_str.cs";
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

            String tempDirName = Path.Combine(TestInfo.CurrentDirectory, "TempDirectory");
            String dirName = Path.Combine(TestInfo.CurrentDirectory, "Move_str_str_test_Dir");
            DirectoryInfo dir2 = null;

            if (Directory.Exists(tempDirName))
                Directory.Delete(tempDirName, true);
            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);


            // [] Argumentnull exception for null arguments
            //-----------------------------------------------------------------
            strLoc = "Loc_00001";

            iCountTestcases++;
            try
            {
                Directory.Move(null, dirName);
                iCountErrors++;
                printerr("Error_00002! Expected exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_00004! Incorrect exception thrown, exc==" + exc.ToString());
            }

            iCountTestcases++;
            try
            {
                Directory.Move(dirName, null);
                iCountErrors++;
                printerr("Error_00005! Expected exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_00007! Incorrect exception thrown, exc==" + exc.ToString());
            }
            //-----------------------------------------------------------------

            // [] ArgumentException for zero length arguments
            //-----------------------------------------------------------------
            strLoc = "Loc_00008";

            iCountTestcases++;
            try
            {
                Directory.Move(String.Empty, dirName);
                iCountErrors++;
                printerr("Error_00008! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_00010! Incorrect exception thrown, exc==" + exc.ToString());
            }


            iCountTestcases++;
            try
            {
                Directory.Move(dirName, String.Empty);
                iCountErrors++;
                printerr("Error_00011! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_00013! Incorrect exception thrown, exc==" + exc.ToString());
            }
            //-----------------------------------------------------------------

            // [] Try to move a directory that does not exist
            //-----------------------------------------------------------------
            strLoc = "Loc_00014";

            iCountTestcases++;
            try
            {
                Directory.Move(Path.Combine(TestInfo.CurrentDirectory, "NonExistentDirectory"), dirName);
                iCountErrors++;
                printerr("Error_00015! Expected exception not thrown");
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_00017! Incorrect exception thrown, exc==" + exc.ToString());
            }
            //-----------------------------------------------------------------


            // [] AccessException when moving onto existing directory
            //-----------------------------------------------------------------
            strLoc = "Loc_00018";

            iCountTestcases++;
            try
            {
                Directory.Move(TestInfo.CurrentDirectory, TestInfo.CurrentDirectory);
                iCountErrors++;
                printerr("Error_00019! Expected exception not thrown");
            }
            catch (IOException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_00021! Incorrect exception thrown, exc==" + exc.ToString());
            }
            //-----------------------------------------------------------------

            // [] Move a directory and check that it is moved
            //-----------------------------------------------------------------
            strLoc = "Loc_00022";

            Directory.CreateDirectory(dirName);
            Directory.Move(dirName, tempDirName);
            iCountTestcases++;
            if (Directory.Exists(dirName))
            {
                iCountErrors++;
                printerr("Error_00023! Source directory still there");
            }
            if (!Directory.Exists(tempDirName))
            {
                iCountErrors++;
                printerr("Error_00024! destination directory missing");
            }
            Directory.Delete(tempDirName);

            //[]Move directories that end with directory separator
            //-----------------------------------------------------------------

            Directory.CreateDirectory(dirName);
            Directory.Move(dirName + Path.DirectorySeparatorChar, tempDirName + Path.DirectorySeparatorChar);
            iCountTestcases++;
            if (Directory.Exists(dirName))
            {
                iCountErrors++;
                printerr("Error_00023! Source directory still there");
            }
            if (!Directory.Exists(tempDirName))
            {
                iCountErrors++;
                printerr("Error_00024! destination directory missing");
            }
            Directory.Delete(tempDirName);

#if !TEST_WINRT
            if (Interop.IsWindows) // moving between drive labels
            {
                // [] Move to different drive will throw AccessException
                //-----------------------------------------------------------------
                strLoc = "Loc_00025";
                string fullDirName = Path.GetFullPath(dirName);
                Directory.CreateDirectory(dirName);
                iCountTestcases++;
                try
                {
                    if (fullDirName.Substring(0, 3) == @"d:\" || fullDirName.Substring(0, 3) == @"D:\")
                        Directory.Move(fullDirName, "C:\\TempDirectory");
                    else
                        Directory.Move(fullDirName, "D:\\TempDirectory");
                    Console.WriteLine("Root directory..." + fullDirName.Substring(0, 3));
                    iCountErrors++;
                    printerr("Error_00026! Expected exception not thrown");
                }
                catch (IOException)
                {
                }
                catch (Exception exc)
                {
                    iCountErrors++;
                    printerr("Error_00000! Incorrect exception thrown, exc==" + exc.ToString());
                }
                if (Directory.Exists(fullDirName))
                    Directory.Delete(fullDirName, true);
                //-----------------------------------------------------------------
            }
#endif

            // [] Moving Directory with subdirectories
            //-----------------------------------------------------------------
            strLoc = "Loc_00028";

            dir2 = Directory.CreateDirectory(dirName);
            dir2.CreateSubdirectory(Path.Combine("SubDir", "SubSubDir"));
            FailSafeDirectoryOperations.MoveDirectory(dirName, tempDirName);
            //			Directory.Move(dirName, tempDirName);
            iCountTestcases++;
            if (Directory.Exists(dirName))
            {
                iCountErrors++;
                printerr("Error_00029! Source directory still there");
            }
            dir2 = new DirectoryInfo(tempDirName);
            iCountTestcases++;
            if (!Directory.Exists(dir2.FullName))
            {
                iCountErrors++;
                printerr("Error_00030! Destination directory missing");
            }
            iCountTestcases++;
            if (!Directory.Exists(Path.Combine(dir2.FullName, "SubDir", "SubSubDir")))
            {
                iCountErrors++;
                printerr("Error_00031! Subdirectories not moved");
            }
            dir2.Delete(true);

            //-----------------------------------------------------------------

            if (Interop.IsWindows)
            {
                // [] wildchars in src directory
                //-----------------------------------------------------------------
                strLoc = "Loc_00032";

                iCountTestcases++;
                try
                {
                    Directory.Move("*", tempDirName);
                    iCountErrors++;
                    printerr("Error_00033! Expected exception not thrown");
                }
                catch (ArgumentException)
                {
                }
                catch (Exception exc)
                {
                    iCountErrors++;
                    printerr("Error_00035! Incorrect exception thrown, exc==" + exc.ToString());
                }
            }
            //-----------------------------------------------------------------

            if (Interop.IsWindows)
            {
                // [] wildchars in dest directory
                //-----------------------------------------------------------------
                strLoc = "Loc_00036";

                iCountTestcases++;
                try
                {
                    Directory.Move(TestInfo.CurrentDirectory, "Temp*");
                    iCountErrors++;
                    printerr("Error_00037! Expected exception not thrown");
                }
                catch (ArgumentException)
                {
                }
                catch (Exception exc)
                {
                    iCountErrors++;
                    printerr("Error_00039! Incorrect exception thrown, exc==" + exc.ToString());
                }
            }
            //-----------------------------------------------------------------

            // [] InvalidPathChars
            //-----------------------------------------------------------------
            strLoc = "Loc_00040";

            iCountTestcases++;
            try
            {
                Directory.Move(TestInfo.CurrentDirectory, "<MyDirectory\0");
                iCountErrors++;
                printerr("Error_00041! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_00043 Incorret exception thrown, exc==" + exc.ToString());
            }
            //-----------------------------------------------------------------

            // [] PathTooLongException if destination name is too long
            //-----------------------------------------------------------------
            strLoc = "Loc_00044";

            String str = new string('a', IOInputs.MaxPath);
            iCountTestcases++;
            try
            {
                Directory.Move(TestInfo.CurrentDirectory, str);
                iCountErrors++;
                printerr("Error_00045! Expected exception not thrown");
            }
            catch (PathTooLongException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_00047! Incorrect exception thrown, exc==" + exc.ToString());
            }

            //-----------------------------------------------------------------

            // [] Non-existent drive specified for destination
            //-----------------------------------------------------------------
            strLoc = "Loc_00048";

            if (Interop.IsWindows) // drive labels
            {
                iCountTestcases++;
                Directory.CreateDirectory(dirName);
                try
                {
                    Directory.Move(dirName, "X:\\Temp");
                    iCountErrors++;
                    printerr("Error_00049! Expected exception not thrown");
                }
                catch (IOException)
                {
                }
                catch (Exception exc)
                {
                    iCountErrors++;
                    printerr("Error_00051! Incorrect exception thrown, exc==" + exc.ToString());
                }
                Directory.Delete(dirName);
            }
            //-----------------------------------------------------------------

            // [] Use directory names with spaces
            //-----------------------------------------------------------------
            strLoc = "Loc_00052";
            string destDirName = Path.Combine(TestInfo.CurrentDirectory, "This is my directory");

            Directory.CreateDirectory(dirName);
            Directory.Move(dirName, destDirName);
            iCountTestcases++;
            if (!Directory.Exists(destDirName))
            {
                iCountErrors++;
                printerr("Error_00053! Destination directory missing");
            }
            Directory.Delete(destDirName);
            //-----------------------------------------------------------------


            // ][ Directory names in different cultures
            //-----------------------------------------------------------------
            //-----------------------------------------------------------------

            if (Directory.Exists(tempDirName))
                Directory.Delete(tempDirName, true);

            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);

            ///////////////////////////////////////////////////////////////////
            /////////////////////////// END TESTS /////////////////////////////
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