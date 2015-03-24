// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text;
using Xunit;

public class FileInfo_Exists
{
    public static String s_strDtTmVer = "2001/02/03 19:50";
    public static String s_strClassMethod = "FileInfo.Exists()";
    public static String s_strTFName = "Exists.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
    public static void runTest()
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";

        try
        {
            String filName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            FileInfo fil2;


            if (File.Exists(filName))
                File.Delete(filName);


            // [] Current Directory
            strLoc = "Loc_276t8";

            iCountTestcases++;
            fil2 = new FileInfo(".");
            if (fil2.Exists)
            {
                iCountErrors++;
                printerr("Error_95428! Incorrect return value");
            }

            iCountTestcases++;
            fil2 = new FileInfo(Directory.GetCurrentDirectory());
            if (fil2.Exists)
            {
                iCountErrors++;
                printerr("Error_97t67! Incorrect return value");
            }

            // [] Non-existent file
            strLoc = "Loc_t993c";
            try
            {
                iCountTestcases++;
                fil2 = new FileInfo(Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName()));
                if (fil2.Exists)
                {
                    iCountErrors++;
                    printerr("Error_6895b! Incorrect return value");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_1y908! Unexpected exception, exc==" + exc.ToString());
            }

            // [] File that does exist
            strLoc = "Loc_2y78g";

            new FileStream(filName, FileMode.Create).Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            if (!fil2.Exists)
            {
                iCountErrors++;
                printerr("Error_9t821! Returned false for existing file");
            }

            File.Delete(filName);

            // [] Filename with spaces
            strLoc = "Loc_298g7";

            String tmp = Path.ChangeExtension(filName, "   space space space" + Path.GetExtension(filName));
            new FileStream(tmp, FileMode.Create).Dispose();
            fil2 = new FileInfo(tmp);
            iCountTestcases++;
            if (!fil2.Exists)
            {
                iCountErrors++;
                printerr("Error_01y8v! Returned incorrect value");
            }
            fil2.Delete();

            // [] Wildcards in filename should return false

            strLoc = "Loc_398vy8";
            iCountTestcases++;
            try
            {
                fil2 = new FileInfo("*");
                if (fil2.Exists)
                {
                    iCountErrors++;
                    printerr("Error_4979c! File with wildcard exists: " + fil2.FullName);
                }
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_498u9! Unexpected exception thrown, exc==" + exc.ToString());
            }

            if (File.Exists(filName))
                File.Delete(filName);
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

