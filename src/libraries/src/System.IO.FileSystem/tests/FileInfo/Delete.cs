// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Xunit;

public class FileInfo_Delete
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/05/01 16:27";
    public static String s_strClassMethod = "Directory.Root";
    public static String s_strTFName = "Delete.cs";
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
            FileStream fs2;
            FileInfo fil2;


            // COMMENT: Simple string parsing of existing fullpath


            // [] Delete a file that does not exist should just return
            //-----------------------------------------------------------------		   
            strLoc = "Loc_7198c";

            iCountTestcases++;
            fil2 = new FileInfo("AkkarBurger");
            fil2.Delete();
            //-----------------------------------------------------------------


            // [] Deleting a file in use should cause IOException
            //-----------------------------------------------------------------
            strLoc = "Loc_29yc7";

            fs2 = new FileStream(filName, FileMode.Create);
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            try
            {
                fil2.Delete();
#if !TEST_WINRT  // WINRT always sets FILE_SHARE_DELETE
                iCountErrors++;
                printerr("Error_1y678! Expected exception not thrown");
            }
            catch (IOException)
            {
#endif
            }
            catch (UnauthorizedAccessException iexc)
            {
                iCountErrors++;
                printerr("Error_1213! This excepton shouldn't occur on Win9X platforms" + iexc.Message);
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_16709! Incorrect exception thrown, exc==" + exc.ToString());
            }
            fs2.Dispose();
#if !TEST_WINRT
            iCountTestcases++;
            if (!fil2.Exists)
            {
                iCountErrors++;
                printerr("Error_768bc! File does not exist==" + fil2.FullName);
            }
            fil2.Delete();
#endif
            fil2.Refresh();
            iCountTestcases++;
            if (fil2.Exists)
            {
                iCountErrors++;
                printerr("Error_810x8! File not deleted==" + fil2.FullName);
            }

            //-----------------------------------------------------------------


            // [] Deleting a file should remove it
            //-----------------------------------------------------------------
            //-----------------------------------------------------------------

            // ][ Deleting a filename with wildcard should work



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

