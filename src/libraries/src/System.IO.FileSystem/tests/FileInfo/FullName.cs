// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text;
using Xunit;

public class FileInfo_FullName
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/05/08 12:18";
    public static String s_strClassMethod = "File.OpenText(String)";
    public static String s_strTFName = "FullName .cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();
    private static String s_strLoc = "Loc_000oo";
    private static int s_iCountErrors = 0;
    private static int s_iCountTestcases = 0;

    [Fact]
    public static void runTest()
    {
        try
        {
            FileInfo fil2;
            String strFileName = "";

            // [] Vanilla case
            //-----------------------------------------------------------------
            s_strLoc = "Loc_2723d";

            strFileName = "Hello\\file.tmp";
            fil2 = new FileInfo(strFileName);
            s_iCountTestcases++;
            if (!fil2.FullName.Equals(s_strTFPath + "\\" + strFileName))
            {
                s_iCountErrors++;
                printerr("Error_5yb87! Incorrect name==" + fil2.FullName);
            }

            // [] \Directory\File
            strFileName = "\\Directory\\File";
            fil2 = new FileInfo(strFileName);
            s_iCountTestcases++;
            if (fil2.FullName.IndexOf(strFileName) != 2)
            {
                s_iCountErrors++;
                printerr("Error_78288! Incorrect name==" + fil2.FullName);
            }

            // [] UNC share
            strFileName = "\\\\Machine\\Directory\\File";
            fil2 = new FileInfo(strFileName);
            s_iCountTestcases++;
            if (!fil2.FullName.Equals(strFileName))
            {
                s_iCountErrors++;
                printerr("Error_67y8b! Incorrect name==" + fil2.FullName);
            }

            // [] Multiple spaces and dots in filename
            strFileName = "C:\\File.tmp hello.blah";
            fil2 = new FileInfo(strFileName);
            s_iCountTestcases++;
            if (!fil2.FullName.Equals(strFileName))
            {
                s_iCountErrors++;
                printerr("Error_2987b! Incorrect name==" + fil2.FullName);
            }

            strFileName = "C://Directory//File";
            fil2 = new FileInfo(strFileName);
            s_iCountTestcases++;

            if (!fil2.FullName.Equals("C:\\Directory\\File"))
            {
                s_iCountErrors++;
                Console.WriteLine("strFileName: " + strFileName);
                printerr("Error_2y78d! Incorrect name==" + fil2.FullName);
            }
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

