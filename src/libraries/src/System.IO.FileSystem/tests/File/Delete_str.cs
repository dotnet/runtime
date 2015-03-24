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

public class File_Delete_str
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/05/08 11:38";
    public static String s_strClassMethod = "File.CreateText";
    public static String s_strTFName = "Delete_str.cs";
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

            String filName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            FileInfo fil2;
            StreamWriter sw2;


            if (File.Exists(filName))
                File.Delete(filName);


            // [] Exception if argument is null
            //-----------------------------------------------------------------
            strLoc = "Loc_2yc81";

            iCountTestcases++;
            try
            {
                File.Delete(null);
                iCountErrors++;
                printerr("Error_1u9by! Expected exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_19d4b! Incorrect exception thrown, exc==" + exc.ToString());
            }

            // [] ArgumentException if argument is *.*
            //-----------------------------------------------------------------
            strLoc = "Loc_32453";

            iCountTestcases++;
            try
            {
                File.Delete("*.*");
                iCountErrors++;
                printerr("Error_4342! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_7777! Incorrect exception thrown, exc==" + exc.ToString());
            }

            // [] Exception for "."
            //-----------------------------------------------------------------
            strLoc = "Loc_90187";

            iCountTestcases++;
            try
            {
                File.Delete(".");
                iCountErrors++;
                printerr("Error_19yb7! Expected exception not thrown");
#if TEST_WINRT // WinRT returns E_INVALIDARG instead of Access denied, BUG: TODO
            } catch (IOException) {
#endif
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_19gyb! Incorrect exception thrown, exc==" + exc.ToString());
            }
            //-----------------------------------------------------------------


            // [] Delete an existing file
            //-----------------------------------------------------------------
            strLoc = "Loc_2g79b";

            string shortFilName = Path.GetFileName(filName);
#if !TEST_WINRT  // Cannot access current directory
            new FileStream(shortFilName, FileMode.Create).Dispose();
            File.Delete(shortFilName);
            iCountTestcases++;
            if (File.Exists(shortFilName))
            {
                iCountErrors++;
                printerr("Error_y7gbs! File not deleted");
            }
#endif
            string fullFilName = Path.Combine(TestInfo.CurrentDirectory, shortFilName);
            new FileStream(fullFilName, FileMode.Create).Dispose();
            File.Delete(fullFilName);
            iCountTestcases++;
            if (File.Exists(fullFilName))
            {
                iCountErrors++;
                printerr("Error_94t7b! File not deleted");
            }

            //-----------------------------------------------------------------

            // []Try to delete a non-existing file
            //-----------------------------------------------------------------
            strLoc = "Loc_278yb";

            iCountTestcases++;
            try
            {
                File.Delete("FileDoesNotExist");
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_218b9! Unexpected exception thrown, exc==" + exc.ToString());
            }
            try
            {
                File.Delete(Path.Combine(TestInfo.CurrentDirectory, "This file doesnt exist - supercalifragilisticexpialodoscious.txt"));
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_ewsdf! Unexpected exception thrown, exc==" + exc.ToString());
            }

            //-----------------------------------------------------------------

            // [] Delete a file with contents
            //-----------------------------------------------------------------
            strLoc = "Loc_298c8";

            FileStream stream = new FileStream(filName, FileMode.Create);
            sw2 = new StreamWriter(stream);
            sw2.Write("HelloWorld");
            sw2.Dispose();
            stream.Dispose();

            File.Delete(filName);
            iCountTestcases++;
            if (File.Exists(filName))
            {
                iCountErrors++;
                printerr("Error_4017v! File not deleted");
            }

            //-----------------------------------------------------------------


#if !TEST_WINRT  // TODO: Enable once we bring up file attributes
            // [] Deleting a ReadOnly file should not work
            //-----------------------------------------------------------------
            strLoc = "Loc_298b7";
            fil2 = new FileInfo(filName);
            new FileStream(filName, FileMode.Create).Dispose();
            fil2.Attributes = FileAttributes.ReadOnly;
            iCountTestcases++;
            try
            {
                File.Delete(filName);
                iCountErrors++;
                printerr("Error_487bg! Expected exception not thrown");
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_2467y! Incorrect exception thrown, exc==" + exc.ToString());
            }
            fil2.Attributes = new FileAttributes();
            fil2.Delete();

            //-----------------------------------------------------------------
#endif

            // ][ Filename starting with wildcard
            // ][ Filename ending with wildcard
            // ][ Filename with internal wildcard





            if (File.Exists(filName))
                File.Delete(filName);

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

