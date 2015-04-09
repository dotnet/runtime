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

public class FileInfo_ToString
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/05/08 12:18";
    public static String s_strClassMethod = "File.OpenText(String)";
    public static String s_strTFName = "ToString.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();
    private static String s_strLoc = "Loc_000oo";
    private static int s_iCountErrors = 0;
    private static int s_iCountTestcases = 0;

    [Fact]
    public static void runTest()
    {
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////

            FileInfo fil2;

            // [] Check commonly used file formats
            //-----------------------------------------------------------------
            s_strLoc = "Loc_2723d";

            fil2 = new FileInfo(Path.Combine("Hello", "file.tmp"));
            s_iCountTestcases++;
            if (!fil2.ToString().Equals(Path.Combine("Hello", "file.tmp")))
            {
                s_iCountErrors++;
                printerr("Error_5yb87! Incorrect name==" + fil2.ToString());
            }


            fil2 = new FileInfo(Path.DirectorySeparatorChar + Path.Combine("Directory", "File"));
            s_iCountTestcases++;
            if (!fil2.ToString().Equals(Path.DirectorySeparatorChar + Path.Combine("Directory", "File")))
            {
                s_iCountErrors++;
                printerr("Error_78288! Incorrect name==" + fil2.ToString());
            }

            fil2 = new FileInfo(new string(Path.DirectorySeparatorChar, 2) + Path.Combine("Machine", "Directory", "File"));
            s_iCountTestcases++;
            if (!fil2.ToString().Equals(new string(Path.DirectorySeparatorChar, 2) + Path.Combine("Machine", "Directory", "File")))
            {
                s_iCountErrors++;
                printerr("Error_67y8b! Incorrect name==" + fil2.ToString());
            }

            fil2 = new FileInfo(Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory()), "File.tmp hello.blah"));
            s_iCountTestcases++;
            if (!fil2.ToString().Equals(Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory()), "File.tmp hello.blah")))
            {
                s_iCountErrors++;
                printerr("Error_2987b! Incorrect name==" + fil2.ToString());
            }
            fil2 = new FileInfo(Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory()), "Directory", "File").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            s_iCountTestcases++;
            if (!fil2.ToString().Equals(Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory()), "Directory", "File").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)))
            {
                s_iCountErrors++;
                printerr("Error_2y78d! Incorrect name==" + fil2.ToString());
            }
            //-----------------------------------------------------------------




            ///////////////////////////////////////////////////////////////////
            /////////////////////////// END TESTS /////////////////////////////

        }
        catch (Exception exc_general)
        {
            ++s_iCountErrors;
            Console.WriteLine("Error Err_8888yyy!  strLoc==" + s_strLoc + ", exc_general==" + exc_general.ToString());
        }
        ////  Finish Diagnostics
        if (s_iCountErrors != 0)
        {
            Console.WriteLine("FAiL! " + s_strTFName + " ,iCountErrors==" + s_iCountErrors.ToString());
        }

        Assert.Equal(0, s_iCountErrors);
    }

    public static void printerr(String err, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine("ERROR: ({0}, {1}, {2}) {3}", memberName, filePath, lineNumber, err);
    }
}

