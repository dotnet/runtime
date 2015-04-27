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

public class File_CreateText_str
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/05/03 20:41";
    public static String s_strClassMethod = "File.CreateText(String)";
    public static String s_strTFName = "CreateText_str.cs";
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
            StreamWriter sw2;
            StreamReader sr2;
            String str2;


            if (File.Exists(filName))
                File.Delete(filName);


            // [] ArgumentNullException if arg is null
            //-----------------------------------------------------------------
            strLoc = "Loc_789s9";

            iCountTestcases++;
            try
            {
                File.CreateText(null);
                iCountErrors++;
                printerr("Error_10198! Expected exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_19ygb! Incorrect exception thrown, exc==" + exc.ToString());
            }
            //-----------------------------------------------------------------


            // [] Exception if string.empty is passed
            //-----------------------------------------------------------------
            strLoc = "Loc_t768c";

            iCountTestcases++;
            try
            {
                File.CreateText(String.Empty);
                iCountErrors++;
                printerr("Error_y7g53! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_217tb! Incorrect exception thrown, exc==" + exc.ToString());
            }

            //-----------------------------------------------------------------




            // [] CreateText on a file that does not exist should create it
            //-----------------------------------------------------------------
            strLoc = "Loc_2y78b";

            sw2 = File.CreateText(filName);
            sw2.Write("HelloWorld");
            sw2.Dispose();
            FileStream stream = new FileStream(filName, FileMode.Open);
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

            // [] CreateText should overwrite exisiting file

            strLoc = "Loc_2gy7b";

            sw2 = File.CreateText(filName);
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


            if (Interop.IsWindows)
            {
                // [] Wildcard in filename should throw
                //-----------------------------------------------------------------
                strLoc = "Loc_48yv8";

                iCountTestcases++;
                try
                {
                    File.CreateText("Test*");
                    iCountErrors++;
                    printerr("Error_4y8vv! Expected exception not thrown");
                }
                catch (ArgumentException)
                {
                }
                catch (Exception exc)
                {
                    iCountErrors++;
                    printerr("Error_489v7! Incorrect exception thrown, exc==" + exc.ToString());
                }
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

