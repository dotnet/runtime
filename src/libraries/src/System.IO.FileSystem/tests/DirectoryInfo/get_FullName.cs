// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using Xunit;

public class DirectoryInfo_get_FullName
{
    public static String s_strActiveBugNums = "14866, 14849";
    public static String s_strClassMethod = "Directory.FullName";
    public static String s_strTFName = "Co5511get_FullName.cs";
    public static String s_strTFAbbrev = "Co5511";
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
            DirectoryInfo dir = null;
            strLoc = "Loc_2908x";

            iCountTestcases++;
            try
            {
                dir = new DirectoryInfo(null);
                iCountErrors++;
                printerr("Error_2908x! Expected exception not thrown, dir==" + dir.ToString());
            }
            catch (ArgumentNullException)
            {
                // Expected exception
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_209ux! Incorrect exception thrown, exc==" + exc.ToString());
            }

            // [] Do the current dir


            strLoc = "Loc_909d9";

            string currentDir = Directory.GetCurrentDirectory();
            dir = new DirectoryInfo(currentDir);
            iCountTestcases++;
            if (!dir.FullName.Equals(currentDir))
            {
                iCountErrors++;
                printerr("Error_209xu! Incorrect Directory returned , dir==" + dir.FullName);
            }

            // [] Give a non-existent directory

            strLoc = "Loc_2999s";

            iCountTestcases++;
            dir = new DirectoryInfo("This directory does not exist");
            if (!dir.FullName.Equals(Path.Combine(Directory.GetCurrentDirectory(), "This directory does not exist")))
            {
                iCountErrors++;
                printerr("Error_109z9! Incorrect directory name, dir==" + dir.ToString());
            }


            // [] Root drive

            strLoc = "Loc_20u9x";

            dir = new DirectoryInfo(Path.GetPathRoot(Directory.GetCurrentDirectory()));
            iCountTestcases++;
            if (!dir.FullName.Equals(Path.GetPathRoot(Directory.GetCurrentDirectory())))
            {
                iCountErrors++;
                printerr("Error_2098x! Incorrect dir returned==" + dir.FullName);
            }

            // [] Capital letter root drive

            strLoc = "Loc_099s8";

            dir = new DirectoryInfo(Path.GetPathRoot(Directory.GetCurrentDirectory()).ToUpper());
            DirectoryInfo dir2 = new DirectoryInfo(Path.GetPathRoot(Directory.GetCurrentDirectory()).ToUpper());

            iCountTestcases++;
            if (!dir.FullName.Equals(dir2.FullName))
            {
                iCountErrors++;
                printerr("Bug 14849 Error_2982y! dir==" + dir.FullName + " , dir2==" + dir2.FullName);
            }

            string dirPath = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            Directory.CreateDirectory(dirPath);
            dir = new DirectoryInfo(dirPath);
            iCountTestcases++;
            if (!dir.FullName.Equals(dirPath))
            {
                iCountErrors++;
                printerr("Error_298yx! Incorrect dir constructed, dir==" + dir.FullName);
            }
            dir.Delete();

            strLoc = "Loc_298yb";

            iCountTestcases++;
            try
            {
                dir = new DirectoryInfo("      ");
                dir.Create();
                iCountErrors++;
                printerr("Error_0919x! Expected exception not thrown, dir==" + dir.FullName);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_4577c! Incorrect exception thrown, exc==" + exc.ToString());
            }

            // [] Go for current dir with "."

            strLoc = "Loc_949gg";

            dir = new DirectoryInfo(".");
            iCountTestcases++;
            if (!dir.FullName.Equals(Directory.GetCurrentDirectory()))
            {
                iCountErrors++;
                printerr("Error_299xu! Incorrect dir constructed, dir==" + dir.FullName);
            }

            // [] go one dir up with ".."

            strLoc = "Loc_9937a";

            dir = new DirectoryInfo("..");
            iCountTestcases++;
            String strParent = Path.GetDirectoryName(Directory.GetCurrentDirectory());
            if (strParent.IndexOf(Path.DirectorySeparatorChar) == -1 && strParent.IndexOf(Path.AltDirectorySeparatorChar) == -1)
                strParent = strParent + Path.DirectorySeparatorChar;
            if (!dir.FullName.Equals(strParent))
            {
                iCountErrors++;
                Console.WriteLine(strParent);
                printerr("Error_298xy! Incorrect dir returned, dir==" + dir.FullName);
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

