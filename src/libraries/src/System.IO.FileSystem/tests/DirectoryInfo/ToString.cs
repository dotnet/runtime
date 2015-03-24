// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections;
using System.Globalization;
using Xunit;

public class DirectoryInfo_ToString
{
    public static String s_strActiveBugNums = "14866";
    public static String s_strClassMethod = "Directory.ToString";
    public static String s_strTFName = "ToString.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
    public static void runTest()
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        string dirName = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());
        Directory.CreateDirectory(dirName);


        try
        {
            DirectoryInfo dir = null;


            // [] Do the current dir


            strLoc = "Loc_909d9";

            dir = new DirectoryInfo(dirName);
            iCountTestcases++;
            if (!dir.ToString().Equals(Path.GetFileName(dirName)))
            {
                iCountErrors++;
                printerr("Error_209xu! Incorrect Directory returned , dir==" + dir.ToString());
                Console.WriteLine(dirName);
                Console.WriteLine(dir == null);
                Console.WriteLine(dir);
            }
            // [] Root drive

            strLoc = "Loc_20u9x";

            dir = new DirectoryInfo("c:\\");
            iCountTestcases++;
            if (!dir.ToString().Equals("c:\\"))
            {
                iCountErrors++;
                printerr("Error_2098x! Incorrect dir returned==" + dir.ToString());
            }

            string testDir = Path.Combine(dirName, "TestDir");
            Directory.CreateDirectory(testDir);
            dir = new DirectoryInfo(testDir);
            iCountTestcases++;

            if (!dir.FullName.Equals(testDir))
            {
                iCountErrors++;
                printerr("Error_298yx! Incorrect dir constructed, dir==" + dir.ToString());
            }
            dir.Delete();

            // [] Whitespace string

            strLoc = "Loc_298yb";

            iCountTestcases++;
            try
            {
                dir = new DirectoryInfo("      ");
                dir.Create();
                iCountErrors++;
                printerr("Error_09rux! Expected exception not thrown, dir==" + dir.ToString());
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Bug 14866 Error_4577c! Incorrect exception thrown, exc==" + exc.ToString());
            }

            // [] Go for same dir

            strLoc = "Loc_949gg";

            dir = new DirectoryInfo(".");
            iCountTestcases++;
            if (!dir.FullName.Equals(Directory.GetCurrentDirectory()))
            {
                iCountErrors++;
                printerr("Error_299xu! Incorrect dir constructed, dir==" + dir.ToString());
            }

            // [] go one dir up

            strLoc = "Loc_9937a";

            dir = new DirectoryInfo("..");
            iCountTestcases++;
            if (!dir.FullName.Equals(Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().LastIndexOf("\\"))))
            {
                iCountErrors++;
                printerr("Error_298xy! Incorrect dir returned, dir==" + dir.ToString());
            }
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

