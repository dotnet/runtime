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

public class File_ChangeExtension_str_Str
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/05/03 20:26";
    public static String s_strClassMethod = "Path.AppendText(String)";
    public static String s_strTFName = "ChangeExtension_str_Str.cs";
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

            String filName = s_strTFName.Substring(0, s_strTFName.IndexOf('.')) + "_test_" + "TestFile";
            String str2;


            if (File.Exists(filName))
                File.Delete(filName);

            iCountTestcases++;
            str2 = Path.ChangeExtension(String.Empty, ".tmp");
            if (!str2.Equals(String.Empty))
            {
                iCountErrors++;
                printerr("Error_18v88! Expected exception not thrown, str2==" + str2);
            }

            //-----------------------------------------------------------------

            // [] Some trivial test cases.
            //-----------------------------------------------------------------	
            strLoc = "Loc_2y9x8";

            // [] Change extension to ...
            str2 = Path.ChangeExtension("Path.tmp", "...");
            iCountTestcases++;
            if (!str2.Equals("Path..."))
            {
                iCountErrors++;
                printerr("Error_98ygf7! Incorrect string returned, str2==" + str2);
            }

            // [] Vanilla change from .tmp to .doc
            str2 = Path.ChangeExtension("Path.temporary.tmp", ".doc");
            iCountTestcases++;
            if (!str2.Equals("Path.temporary.doc"))
            {
                iCountErrors++;
                printerr("Error_187yg! Incorrect string returned, str2==" + str2);
            }

            // [] Change to null (nothing)
            str2 = Path.ChangeExtension("File.tmp", null);
            iCountTestcases++;
            if (!str2.Equals("File"))
            {
                iCountErrors++;
                printerr("Error_198cf! Incorrect string returned, str2==" + str2);
            }

            // [] String away everything after . by passing in Empty
            str2 = Path.ChangeExtension("File.tmp", String.Empty);
            iCountTestcases++;
            if (!str2.Equals("File."))
            {
                iCountErrors++;
                printerr("Error_290wd! Incorrect string returned, str2==" + str2);
            }


            // [] Add extension to a filename that doesn't have one
            str2 = Path.ChangeExtension("File", ".tmp");
            iCountTestcases++;
            if (!str2.Equals("File.tmp"))
            {
                iCountErrors++;
                printerr("Error_19yg7! Incorrect string returned, str2==" + str2);
            }


            //-----------------------------------------------------------------



            if (File.Exists(filName))
                File.Delete(filName);

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

