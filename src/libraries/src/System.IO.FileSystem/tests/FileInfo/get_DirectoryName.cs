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

public class FileInfo_get_DirectoryName
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/05/08 20:00";
    public static String s_strClassMethod = "File.DirectoryName()";
    public static String s_strTFName = "get_DirectoryName.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
    public static void runTest()
    {
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;


        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////

            FileInfo fil2;



            // [] Vanilla case
            //-----------------------------------------------------------------
            strLoc = "Loc_2723d";

            fil2 = new FileInfo("Hello\\file.tmp");
            iCountTestcases++;
            if (!fil2.DirectoryName.Equals(Directory.GetCurrentDirectory() + "\\Hello"))
            {
                iCountErrors++;
                printerr("Error_5yb87! Incorrect name==" + fil2.DirectoryName);
            }

            // [] \Directory\File
            fil2 = new FileInfo("\\Directory\\File");
            iCountTestcases++;
            if (!fil2.DirectoryName.Equals(Directory.GetCurrentDirectory().Substring(0, 2) + "\\Directory"))
            {
                iCountErrors++;
                printerr("Error_78288! Incorrect name==" + fil2.DirectoryName);
            }

            // [] UNC share
            fil2 = new FileInfo("\\\\Machine\\Directory\\File");
            iCountTestcases++;
            if (!fil2.DirectoryName.Equals("\\\\Machine\\Directory"))
            {
                iCountErrors++;
                printerr("Error_67y8b! Incorrect name==" + fil2.DirectoryName);
            }

            // [] Names with spaces
            fil2 = new FileInfo("C:\\File.tmp hello.blah");
            iCountTestcases++;
            if (!fil2.DirectoryName.Equals("C:\\"))
            {
                iCountErrors++;
                printerr("Error_2987b! Incorrect name==" + fil2.DirectoryName);
            }

            // [] C:/Directory/File
            fil2 = new FileInfo("C:/Directory/File");
            iCountTestcases++;
            if (!fil2.DirectoryName.Equals("C:\\Directory"))
            {
                iCountErrors++;
                printerr("Error_2y78d! Incorrect name==" + fil2.DirectoryName);
            }
            /*
                        // [] Ending with \\
                        fil2 = new FileInfo("C:\\Dir1\\Dir2\\");
                        iCountTestcases++;
                        if(!fil2.DirectoryName.Equals("C:\\Dir1\\Dir2")) {
                            iCountErrors++;
                            printerr( "Error_287gy! Incorrect name=="+fil2.DirectoryName);
                        } 
            */

            // [] Multiple Subdirectories
            fil2 = new FileInfo("C:\\Dir1\\Dir2\\Dir3\\Dir4\\File1");
            iCountTestcases++;
            if (!fil2.DirectoryName.Equals("C:\\Dir1\\Dir2\\Dir3\\Dir4"))
            {
                iCountErrors++;
                printerr("Error_283fy! Incorrect name==" + fil2.DirectoryName);
            }


            fil2 = new FileInfo("Dir1");
            iCountTestcases++;
            if (!fil2.DirectoryName.Equals(Directory.GetCurrentDirectory()))
            {
                iCountErrors++;
                printerr("Error_758bb! Incorrect name==" + fil2.DirectoryName);
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

