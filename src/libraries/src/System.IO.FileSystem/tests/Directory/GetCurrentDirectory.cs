// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using Xunit;

public class Directory_GetCurrentDirectory
{
    public static String s_strDtTmVer = "2000/07/07 19:00";
    public static String s_strClassMethod = "Directory.GetCurrentDirectory()";
    public static String s_strTFName = "GetCurrentDirectory.cs";
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

            DirectoryInfo currentdir = new DirectoryInfo(Directory.GetCurrentDirectory());

            // [] ArgumentNullException for null argument

            strLoc = "Loc_23849";

            iCountTestcases++;
            try
            {
                Directory.SetCurrentDirectory(null);
                iCountErrors++;
                printerr("Error_388rf! Expected exception not thrown, dir==" + Directory.GetCurrentDirectory());
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_28t7b! Incorrect exception thrown, exc==" + exc.ToString());
            }


            // [] Vanilla case
            /* Scenario disabled when porting because it modifies global process state and can not be run in parallel with other tests
            DirectoryInfo dir = null;
            strLoc = "Loc_2g77b";

            dir = new DirectoryInfo("..");
            Directory.SetCurrentDirectory(dir.FullName);

            iCountTestcases++;
            if (!Directory.GetCurrentDirectory().Equals(dir.FullName))
            {
                Console.WriteLine(Directory.GetCurrentDirectory());
                iCountErrors++;
                printerr("Error_38g8b! Directory Not set correctly");
            }
            */

            // [] Another vanilla case
            /* Scenario disabled when porting because it modifies global process state and can not be run in parallel with other tests
            strLoc = "Loc_9t8gt";

            dir = new DirectoryInfo("C:\\");
            Directory.SetCurrentDirectory(dir.FullName);
            iCountTestcases++;
            if (!Directory.GetCurrentDirectory().Equals(dir.FullName))
            {
                iCountErrors++;
                printerr("Error_238g7! Directory not set correctly, dir==" + Directory.GetCurrentDirectory());
            }
            */

            // [] Set back to original
            /* Scenario disabled when porting because it modifies global process state and can not be run in parallel with other tests
            strLoc = "Loc_87ygv";
            
            Directory.SetCurrentDirectory(currentdir.FullName);
            iCountTestcases++;
            if (!Directory.GetCurrentDirectory().Equals(currentdir.FullName))
            {
                iCountErrors++;
                printerr("Error_2g7b7! Directory not set correctly, dir==" + Directory.GetCurrentDirectory());
            }
            */

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