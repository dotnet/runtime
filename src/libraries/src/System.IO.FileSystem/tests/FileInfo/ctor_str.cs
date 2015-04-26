// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections;
using System.Globalization;
using Xunit;

public class FileInfo_ctor_str
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/03/07 14:12";
    public static String s_strClassMethod = "Directory.ToString";
    public static String s_strTFName = "ctor_str.cs";
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

            FileInfo fil2;
            string filName;

            // [] ArgumentNullException if null is passed in

            strLoc = "Loc_498yg";


            iCountTestcases++;
            try
            {
                fil2 = new FileInfo(null);
                iCountErrors++;
                printerr("Error_209uz! Expected exception not thrown, fil2==" + fil2.FullName);
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_21x99! Incorrect exception thrown, exc==" + exc.ToString());
            }



            // [] Construct fil object

            strLoc = "Loc_7h7g7";

            iCountTestcases++;
            try
            {
                fil2 = new FileInfo("ThisFileDoesNotExist");
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_20g9u! Incorrect exception thrown, exc==" + exc.ToString());
            }



            // [] Try to get current directory with CurrentDirectory property

            strLoc = "Loc_289vy";

            iCountTestcases++;
            try
            {
                fil2 = new FileInfo(Directory.GetCurrentDirectory());
                fil2.Open(FileMode.Open);
                iCountErrors++;
                printerr("Error_301ju! Expected exception not thrown, fil2==" + fil2.FullName);
            }
            catch (UnauthorizedAccessException)
            {
#if TEST_WINRT // WinRT returns E_INVALIDARG instead of ACCESS_DENIED
            } catch (IOException) {
#endif
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_209us! Incorrect exception thrown, exc==" + exc.ToString());
            }



            // [] Try to get current directory with "."

            strLoc = "Loc_fd348";

            iCountTestcases++;
            try
            {
                fil2 = new FileInfo(".");
                fil2.Open(FileMode.Open);
                iCountErrors++;
                printerr("Error_398vh! Expected exception not thrown, fil2==" + fil2.FullName);
            }
            catch (UnauthorizedAccessException)
            {
#if TEST_WINRT // WinRT returns E_INVALIDARG instead of ACCESS_DENIED
            } catch (IOException) {
#endif
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_27h72! Incorrect exception thrown, exc==" + exc.ToString());
            }





            // [] Find a real file using full path

            strLoc = "Loc_r7yd9";
            filName = Path.Combine(TestInfo.CurrentDirectory, "MyTestFile");
            File.Create(filName).Dispose();
            iCountTestcases++;
            try
            {
                fil2 = new FileInfo(filName);
                fil2.Delete();
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_f588y! Unexpected exception thrown, exc==" + exc.ToString());
            }

#if !TEST_WINRT // WinRT cannot write to current dir
            // [] Find the same file using relative path;

            strLoc = "Loc_f548y";

            File.Create("MyTestFile").Dispose();
            iCountTestcases++;
            try
            {
                fil2 = new FileInfo("MyTestFile");
                fil2.Delete();
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_298gv! Unexpected exception thrown, exc==" + exc.ToString());
            }
#endif



            // [] File with bunch of periods

            strLoc = "Loc_298dy";
            filName = Path.Combine(TestInfo.CurrentDirectory, "Hello.there.you.have.an.extension");
            File.Create(filName).Dispose();
            iCountTestcases++;
            try
            {
                fil2 = new FileInfo(filName);
                fil2.Delete();
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_2901s! Unexpected exception thrown, exc==" + exc.ToString());
            }


            if (Interop.IsWindows)
            {
                // [] Filename with wildchar characters


                strLoc = "Loc_984hg";

                iCountTestcases++;
                try
                {
                    fil2 = new FileInfo("**");
                    iCountErrors++;
                    printerr("Error_298xh! Expected exception not thrown, fil2==" + fil2.FullName);
                }
                catch (ArgumentException)
                {
                }
                catch (Exception exc)
                {
                    iCountErrors++;
                    printerr("Error_2091s! Incorrect exception thrown, exc==" + exc.ToString());
                }
            }


            // [] String.Empty should throw exception

            strLoc = "Loc_948jk";


            iCountTestcases++;
            try
            {
                fil2 = new FileInfo(String.Empty);
                iCountErrors++;
                printerr("Error_0199z! Expected exception not thrown, fil2==" + fil2.FullName);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_20109! Incorrect exception thrown, exc==" + exc.ToString());
            }

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

