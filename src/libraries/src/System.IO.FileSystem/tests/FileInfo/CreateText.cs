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

public class FileInfo_CreateText
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/05/03 20:43";
    public static String s_strClassMethod = "File.CreateText";
    public static String s_strTFName = "CreateText.cs";
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

            String filName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            FileInfo fil2;
            StreamWriter sw2;
            StreamReader sr2;
            String str2;
            FileStream stream;


            if (File.Exists(filName))
                File.Delete(filName);


            // [] CreateText on a file that does not exist should create it
            //-----------------------------------------------------------------
            strLoc = "Loc_2y78b";

            fil2 = new FileInfo(filName);
            iCountTestcases++;
            sw2 = fil2.CreateText();
            sw2.Write("HelloWorld");
            sw2.Dispose();
            stream = new FileStream(filName, FileMode.Open);
            sr2 = new StreamReader(stream);
            str2 = sr2.ReadToEnd();
            iCountTestcases++;
            if (!str2.Equals("HelloWorld"))
            {
                iCountErrors++;
                printerr("Error_21y77! Incorrect string written, str2==" + str2);
            }
            sr2.Dispose();
            stream.Dispose();

            // [] CreateText should overwrite existing file

            strLoc = "Loc_2gy7b";

            sw2 = fil2.CreateText();
            sw2.Write("You Big Globe");
            sw2.Dispose();
            stream = new FileStream(filName, FileMode.Open);
            sr2 = new StreamReader(stream);
            str2 = sr2.ReadToEnd();
            iCountTestcases++;
            if (!str2.Equals("You Big Globe"))
            {
                iCountErrors++;
                printerr("Error_12ytb! Incorrect string written, str2==" + str2);
            }
            sr2.Dispose();
            stream.Dispose();

            //-----------------------------------------------------------------


            // [] AccessException if file exists and is readonly

            strLoc = "Loc_498yv";

            fil2.Attributes = FileAttributes.ReadOnly;
            iCountTestcases++;
            try
            {
                sw2 = fil2.CreateText();
                iCountErrors++;
                printerr("Error_398fy! Expected exception not thrown");
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_8gy8g! Incorrect exception thrown, exc==" + exc.ToString());
            }
            fil2.Attributes = new FileAttributes();



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

