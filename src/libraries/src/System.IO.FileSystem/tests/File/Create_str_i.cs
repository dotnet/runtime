// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using Xunit;

public class File_Create_str_i
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2009/02/18";
    public static String s_strClassMethod = "File.Create(String,Int32)";
    public static String s_strTFName = "Create_str_i.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
    public static void runTest()
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";
        String filName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());

        try
        {
            Stream fs2;

            // [] ArgumentNullException for null argument
            //----------------------------------------------------------------
            strLoc = "Loc_89y8b";

            iCountTestcases++;
            try
            {
                fs2 = File.Create(null, 1);
                iCountErrors++;
                printerr("Error_298yr! Expected exception not thrown");
                fs2.Dispose();
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_987b7! Incorrect exception thrown, exc==" + exc.ToString());
            }
            //----------------------------------------------------------------

            // [] AccessException if passing in currentdirectory
            //----------------------------------------------------------------
            strLoc = "Loc_t7y84";

            iCountTestcases++;
            try
            {
                fs2 = File.Create(".", 1);
                iCountErrors++;
                printerr("Error_7858c! Expected exception not thrown");
                fs2.Dispose();
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_958vy! Incorrect exception thrown, ex==" + exc.ToString());
            }
            //----------------------------------------------------------------

            // [] ArgumentException if zero length string is passed in
            //----------------------------------------------------------------
            strLoc = "Loc_8t87b";

            iCountTestcases++;
            try
            {
                fs2 = File.Create(String.Empty, 1);
                iCountErrors++;
                printerr("Error_598yb! Expected exception not thrown");
                fs2.Dispose();
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_9t5y! Incorrect exception thrown, exc==" + exc.ToString());
            }

            // [] ArgumentOutOfRangeException if buffer is negative
            strLoc = "Loc_t98x9";

            iCountTestcases++;
            try
            {
                fs2 = File.Create(filName, -10);
                iCountErrors++;
                printerr("Error_598cy! Expected exception not thrown");
                fs2.Dispose();
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_928xy! Incorrect exception thrown, exc==" + exc.ToString());
            }

            // [] Create a file
            strLoc = "Loc_887th";

            fs2 = File.Create(filName, 1000);
            iCountTestcases++;
            if (!File.Exists(filName))
            {
                iCountErrors++;
                printerr("Error_2876g! File not created, file==" + filName);
            }
            iCountTestcases++;
            if (fs2.Length != 0)
            {
                iCountErrors++;
                printerr("Error_t598v! Incorrect file length==" + fs2.Length);
            }
            if (fs2.Position != 0)
            {
                iCountErrors++;
                printerr("Error_958bh! Incorrect file position==" + fs2.Position);
            }
            fs2.Dispose();
            File.Delete(filName);

            // [] Create a file in root
            /* Test disabled because it modifies state outside the current working directory
            strLoc = "Loc_89ytb";

            iCountTestcases++;
            bool expectSuccess = true;
#if TEST_WINRT
            expectSuccess = false;  // We will not have access to root
#endif
            String currentdir = TestInfo.CurrentDirectory;
            filName = currentdir.Substring(0, currentdir.IndexOf('\\') + 1) + Path.GetFileName(filName);
            try
            {
                fs2 = File.Create(filName, 1000);
                if (expectSuccess)
                {
                    if (!File.Exists(filName))
                    {
                        iCountErrors++;
                        printerr("Error_t78g7! File not created, file==" + filName);
                    }
                }
                else
                {
                    iCountErrors++;
                    printerr("Error_254d!  User is not Administrator but no exception thrown, file==" + filName);
                }
                fs2.Dispose();
                if (File.Exists(filName))
                    File.Delete(filName);
            }
            catch (UnauthorizedAccessException ex)
            {
                if (expectSuccess)
                {
                    iCountErrors++;
                    printerr("Error_409t!  User is Administrator but exception was thrown:" + ex.ToString());
                }
            }
            */

            // [] Create file in current dir by giving full directory check casing as well
            strLoc = "loc_89tbh";

            filName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            fs2 = File.Create(filName, 100);
            iCountTestcases++;
            if (!File.Exists(filName))
            {
                iCountErrors++;
                printerr("Error_t87gy! File not created, file==" + filName);
            }
            fs2.Dispose();
            File.Delete(filName);

            strLoc = "loc_89tbh_1";

            //see VSWhidbey bug 103341
            filName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            if (File.Exists(filName))
                File.Delete(filName);
            fs2 = File.Create(String.Format(" {0}", filName), 100);
            iCountTestcases++;
            if (!File.Exists(filName))
            {
                iCountErrors++;
                printerr("Error_t87gy_1! File not created, file==" + filName);
            }
            fs2.Dispose();
            File.Delete(filName);

            strLoc = "loc_89tbh_3";

            //see VSWhidbey bug 103341
            filName = Path.Combine(" " + TestInfo.CurrentDirectory, " " + Path.GetRandomFileName());
            if (File.Exists(filName))
                File.Delete(filName);
            fs2 = File.Create(filName, 100);
            iCountTestcases++;
            if (!File.Exists(filName))
            {
                iCountErrors++;
                printerr("Error_t87gy_3! File not created, file==" + filName);
            }
            fs2.Dispose();
            File.Delete(filName);

            if (File.Exists(filName))
                File.Delete(filName);
#if !TEST_WINRT  // Leading spaces in a relative path: relative path will not actually go through WinRT & we'll get access denied
            strLoc = "loc_89tbh_2";
            filName = String.Format(" {0}", Path.GetRandomFileName());
            if (File.Exists(filName))
                File.Delete(filName);
            fs2 = File.Create(filName, 100);
            iCountTestcases++;
            filName = Path.Combine(Directory.GetCurrentDirectory(), filName);
            if (!File.Exists(filName))
            {
                iCountErrors++;
                Console.WriteLine("Error_t87gy_2! File not created, file==<{0}>", filName);
            }
            fs2.Dispose();
            if (File.Exists(filName))
                File.Delete(filName);
#endif
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

