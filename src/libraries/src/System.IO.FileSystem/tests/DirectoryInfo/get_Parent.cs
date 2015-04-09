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

public class DirectoryInfo_get_Parent
{
    public static String s_strActiveBugNums = "";
    public static String s_strClassMethod = "Directory.Parent";
    public static String s_strTFName = "get_Parent.cs";
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

            // COMMENT: Simple string parsing of existing fullpath


            // [] Get parent of root
            //-----------------------------------------------------------------

            strLoc = "Loc_49b78";

            dir2 = new DirectoryInfo(Path.GetPathRoot(Directory.GetCurrentDirectory())).Parent;
            iCountTestcases++;
            if (dir2 != null)
            {
                iCountErrors++;
                printerr("Error_69y7b! Unexpected parent==" + dir2.FullName);
            }

            // [] Get parent of \Directory

            strLoc = "Loc_98ygg";

            dir2 = new DirectoryInfo(Path.DirectorySeparatorChar + Path.Combine("Machine", "Test")).Parent;
            str2 = dir2.Name;
            iCountTestcases++;
            if (!str2.Equals("Machine"))
            {
                iCountErrors++;
                printerr("Error_91y7b! Unexpected parent==" + str2);
            }

            // [] Get parent of UNC share root

            strLoc = "Loc_yg7bk";

            dir2 = new DirectoryInfo(new string(Path.DirectorySeparatorChar, 2) + Path.Combine("Machine", "Test")).Parent;
            iCountTestcases++;
            if (dir2 != null)
            {
                iCountErrors++;
                printerr("Error_4y7gb! Unexpected parent==" + dir2.FullName);
            }


            // [] Get parent of directory string ending with \

            strLoc = "Loc_9876b";
            dir2 = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "a") + Path.DirectorySeparatorChar).Parent;
            str2 = dir2.FullName;
            iCountTestcases++;
            if (!str2.Equals(Directory.GetCurrentDirectory()))
            {
                iCountErrors++;
                printerr("Error_298yg! Unexpected parent==" + str2);
            }

            // [] Get parent of nested directories

            strLoc = "Loc_75y7b";
            dir2 = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "a")).Parent;
            str2 = dir2.FullName;
            iCountTestcases++;
            if (!str2.Equals(Directory.GetCurrentDirectory()))
                {
                iCountErrors++;
                printerr("Error_9887b! Unexpected parent==" + str2);
            }

            // [] Get parent of subdirectories on UNC share

            strLoc = "Loc_y7t98";

            dir2 = new DirectoryInfo(new string(Path.DirectorySeparatorChar, 2) + Path.Combine("Machine", "Test1", "Test2")).Parent;
            str2 = dir2.FullName;
            iCountTestcases++;
            if (!str2.Equals(new string(Path.DirectorySeparatorChar, 2) + Path.Combine("Machine", "Test1")))
            {
                iCountErrors++;
                printerr("Error_69929! Unexpected parent==" + str2);
            }

            // [] play with ".." and "." in the string

            strLoc = "Loc_2984y";

            dir2 = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "Test", "..", ".", "Test")).Parent;
            str2 = dir2.FullName;
            iCountTestcases++;
            if (!str2.Equals(Directory.GetCurrentDirectory()))
            {
                iCountErrors++;
                printerr("Error_20928! Unexpected parent==" + str2);
            }



            strLoc = "Loc_8y76y";

            dir2 = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "My Samples", "Hello To The World", "Test")).Parent;
            str2 = dir2.FullName;
            iCountTestcases++;
            if (!str2.Equals(Path.Combine(Directory.GetCurrentDirectory(), "My Samples", "Hello To The World")))
            {
                iCountErrors++;
                printerr("Error_9019c! Unexpected parent==" + str2);
            }


            //-----------------------------------------------------------------

            //problems with trailing slashes and stuff - #431574
            /**
             - We need to remove the trailing slash when we get the parent directory (we call Path.GetDirectoryName on the full path and having the slash will not work)
             - Root drive always have the trailing slash
             - MoveTo adds a trailing slash

            **/

            String parent = Directory.GetCurrentDirectory();
            String[] values = { "testDir", @"TestDir\" };
            foreach (String value in values)
            {
                dir2 = new DirectoryInfo(value);
                if (!dir2.Parent.FullName.Equals(parent))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_374g! wrong vlaue returned: {0}", dir2.Parent.FullName);
                }
            }

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

