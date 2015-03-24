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

public class Directory_GetDirectoryRoot_str
{
    public static String s_strActiveBugNums = "28196";
    public static String s_strDtTmVer = "2000/04/27 16:55";
    public static String s_strClassMethod = "Directory.Root";
    public static String s_strTFName = "GetDirectoryRoot_str.cs";
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

            String str2;

            // [] COMMENT: Simple string parsing of existing fullpath
            iCountTestcases++; // To make maddog happy

            //Code coverage
            try
            {
                str2 = Directory.GetDirectoryRoot(null);
                iCountErrors++;
                Console.WriteLine("Err_34tgs! No exception thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception ex)
            {
                iCountErrors++;
                Console.WriteLine("Err_34tgs! No exception thrown: {0}", ex);
            }


            // [] Get root of root
            //-----------------------------------------------------------------

            strLoc = "Loc_49b78";

            str2 = Directory.GetDirectoryRoot("C:\\");
            iCountTestcases++;
            if (!str2.Equals("C:\\"))
            {
                iCountErrors++;
                printerr("Error_69y7b! Unexpected parent==" + str2);
            }

            // [] Get root of \Directory

            strLoc = "Loc_98ygg";

            str2 = Directory.GetDirectoryRoot("\\Machine\\Test");
            iCountTestcases++;
            String root = Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().IndexOf('\\') + 1);
            if (!str2.Equals(root))
            {
                iCountErrors++;
                printerr("Error_91y7b! Unexpected parent==" + str2);
            }

            // [] Get root of \\Machine\Directory

            strLoc = "Loc_yg7bk";

            str2 = Directory.GetDirectoryRoot("\\\\Machine\\Test");
            iCountTestcases++;
            if (!str2.Equals("\\\\Machine\\Test"))
            {
                iCountErrors++;
                printerr("Error_4y7gb! Unexpected parent==" + str2);
            }


            // [] Get root of many nested directories ending with \

            strLoc = "Loc_9876b";

            str2 = Directory.GetDirectoryRoot("X:\\a\\b\\c\\d\\");
            iCountTestcases++;
            if (!str2.Equals("X:\\"))
            {
                iCountErrors++;
                printerr("Error_298yg! Unexpected parent==" + str2);
            }

            // [] Get root of many nested directories

            strLoc = "Loc_75y7b";

            str2 = Directory.GetDirectoryRoot("X:\\a\\b\\c");
            iCountTestcases++;
            if (!str2.Equals("X:\\"))
            {
                iCountErrors++;
                printerr("Error_9887b! Unexpected parent==" + str2);
            }

            // [] Get root of many subdirs on UNC share

            strLoc = "Loc_y7t98";

            str2 = Directory.GetDirectoryRoot("\\\\Machine\\Test1\\Test2\\Test3");
            iCountTestcases++;
            if (!str2.Equals("\\\\Machine\\Test1"))
            {
                iCountErrors++;
                printerr("Error_69929! Unexpected parent==" + str2);
            }

            // [] Play with "." and ".." in the middle of the directory string

            strLoc = "Loc_2984y";

            str2 = Directory.GetDirectoryRoot("C:\\Test\\..\\.\\Test\\Test");
            iCountTestcases++;
            if (!str2.Equals("C:\\"))
            {
                iCountErrors++;
                printerr("Error_20928! Unexpected parent==" + str2);
            }


            // [] Use directory names that include spaces

            strLoc = "Loc_8y76y";

            str2 = Directory.GetDirectoryRoot("X:\\My Samples\\Hello To The World\\Test");
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
            Console.WriteLine(s_strTFName + " : Error Err_8888yyy!  strLoc==" + strLoc + ", exc_general==" + exc_general.ToString());
        }
        ////  Finish Diagnostics

        if (iCountErrors != 0)
        {
            Console.WriteLine("FAiL! " + s_strTFName + " ,iCountErrors==" + iCountErrors.ToString() + " , BugNums?: " + s_strActiveBugNums);
        }

        Assert.Equal(iCountErrors, 0);
    }

    public static void printerr(String err, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine("ERROR: ({0}, {1}, {2}) {3}", memberName, filePath, lineNumber, err);
    }
}


