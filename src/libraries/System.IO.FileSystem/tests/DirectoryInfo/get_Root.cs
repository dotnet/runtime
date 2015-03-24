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

public class DirectoryInfo_get_Root
{
    public static String s_strActiveBugNums = "28196";
    public static String s_strClassMethod = "Directory.Root";
    public static String s_strTFName = "get_Root.cs";
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
            String str2;

            // [] COMMENT: Simple string parsing of existing fullpath
            iCountTestcases++; // To make maddog happy 

            // [] get root of root 
            //-----------------------------------------------------------------

            strLoc = "Loc_49b78";

            dir2 = new DirectoryInfo("C:\\").Root;
            str2 = dir2.FullName;
            iCountTestcases++;
            if (!str2.Equals("C:\\"))
            {
                iCountErrors++;
                printerr("Error_69y7b! Unexpected parent==" + dir2.FullName);
            }

            // [] Get root of \Directory

            strLoc = "Loc_98ygg";

            dir2 = new DirectoryInfo("\\Machine\\Test").Root;
            str2 = dir2.FullName;
            iCountTestcases++;
            String root = Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().IndexOf('\\') + 1);
            if (!str2.Equals(root))
            {
                iCountErrors++;
                printerr("Error_91y7b! Unexpected parent==" + str2);
            }

            // [] Get root of UNC share

            strLoc = "Loc_yg7bk";

            dir2 = new DirectoryInfo("\\\\Machine\\Test").Root;
            str2 = dir2.FullName;
            iCountTestcases++;
            if (!str2.Equals("\\\\Machine\\Test"))
            {
                iCountErrors++;
                printerr("Error_4y7gb! Unexpected parent==" + dir2.FullName);
            }

            // [] get root of subdirectories endning with \

            strLoc = "Loc_9876b";
            dir2 = new DirectoryInfo("X:\\a\\b\\c\\d\\").Root;
            str2 = dir2.FullName;
            iCountTestcases++;
            if (!str2.Equals("X:\\"))
            {
                iCountErrors++;
                printerr("Error_298yg! Unexpected parent==" + str2);
            }

            // [] get root of subdirectories

            strLoc = "Loc_75y7b";
            dir2 = new DirectoryInfo("X:\\a\\b\\c").Root;
            str2 = dir2.FullName;
            iCountTestcases++;
            if (!str2.Equals("X:\\"))
            {
                iCountErrors++;
                printerr("Error_9887b! Unexpected parent==" + str2);
            }


            // [] Get root of nested subdirs on UNC share

            strLoc = "Loc_y7t98";

            dir2 = new DirectoryInfo("\\\\Machine\\Test1\\Test2\\Test3").Root;
            str2 = dir2.FullName;
            iCountTestcases++;
            if (!str2.Equals("\\\\Machine\\Test1"))
            {
                iCountErrors++;
                printerr("Error_69929! Unexpected parent==" + str2);
            }

            // [] Include ".." and "." in directory string

            strLoc = "Loc_2984y";

            dir2 = new DirectoryInfo("X:\\Test\\..\\.\\Test\\Test").Root;
            str2 = dir2.FullName;
            iCountTestcases++;
            if (!str2.Equals("X:\\"))
            {
                iCountErrors++;
                printerr("Error_20928! Unexpected parent==" + str2);
            }


            // [] Include spaces in directory string.

            strLoc = "Loc_8y76y";

            dir2 = new DirectoryInfo("X:\\My Samples\\Hello To The World\\Test").Root;
            str2 = dir2.FullName;
            iCountTestcases++;
            if (!str2.Equals("X:\\"))
            {
                iCountErrors++;
                printerr("Error_9019c! Unexpected parent==" + str2);
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

