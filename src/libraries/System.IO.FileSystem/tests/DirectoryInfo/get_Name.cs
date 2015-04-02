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

public class DirectoryInfo_get_Name
{
    public static String s_strActiveBugNums = "";
    public static String s_strClassMethod = "Directory.Exists(String)";
    public static String s_strTFName = "get_Name.cs";
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

            DirectoryInfo dir2 = null;


            // [] Current Directory
            //-----------------------------------------------------------------
            strLoc = "Loc_276t8";

            iCountTestcases++;
            dir2 = new DirectoryInfo(".");
            if (!dir2.Name.Equals(Path.GetFileName(Directory.GetCurrentDirectory())))
            {
                iCountErrors++;
                printerr("Error_69v8j! Incorrect Name on directory, dir2.Name==" + dir2.Name);
            }

            iCountTestcases++;
            dir2 = new DirectoryInfo(Directory.GetCurrentDirectory());
            if (!dir2.Name.Equals(Path.GetFileName(Directory.GetCurrentDirectory())))
            {
                iCountErrors++;
                printerr("Error_97t67! Incorrect Name on directory, dir2.Name==" + dir2.Name);
            }
            //-----------------------------------------------------------------



            // [] UNC share
            //-----------------------------------------------------------------
            strLoc = "Loc_99084";

            dir2 = new DirectoryInfo(new string(Path.DirectorySeparatorChar, 2) + Path.Combine("contoso", "amusement", "device"));
            if (!dir2.Name.Equals("device"))
            {
                iCountErrors++;
                printerr("Error_02099! Incorrect name==" + dir2.Name);
            }

            // [] Root directory

            iCountTestcases++;
            dir2 = new DirectoryInfo(Path.GetPathRoot(Directory.GetCurrentDirectory()));

            if (!dir2.Name.Equals(Path.GetPathRoot(Directory.GetCurrentDirectory())))
            {
                iCountErrors++;
                printerr("Error_50210! Incorrect name==" + dir2.Name);
            }
            //-----------------------------------------------------------------

            // [] Case insensitivity
            //-----------------------------------------------------------------
            strLoc = "Loc_t97g8";

            string currentDir = Directory.GetCurrentDirectory();
            dir2 = new DirectoryInfo(currentDir.ToUpper());
            iCountTestcases++;
            if (!dir2.Name.Equals(Path.GetFileName(currentDir).ToUpper()))
            {
                iCountErrors++;
                Console.WriteLine(dir2.Name);
                printerr("Error_15787! Incorrect return");
            }

            dir2 = new DirectoryInfo(currentDir.ToLower());
            iCountTestcases++;
            if (!dir2.Name.Equals(Path.GetFileName(currentDir).ToLower()))
            {
                iCountErrors++;
                Console.WriteLine(dir2.Name);
                printerr("Error_2yg77! Incorrect return");
            }

            //-----------------------------------------------------------------









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



