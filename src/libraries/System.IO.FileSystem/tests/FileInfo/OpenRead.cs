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

public class FileInfo_OpenRead
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/07/10 18:07";
    public static String s_strClassMethod = "File.Directory()";
    public static String s_strTFName = "OpenRead.cs";
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


            String filName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            Stream s2;
            FileInfo fil2;

            if (File.Exists(filName))
                File.Delete(filName);

            // [] File does not exist
            //-----------------------------------------------------------------
            strLoc = "Loc_00001";

            fil2 = new FileInfo(Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName()));
            iCountTestcases++;
            try
            {
                fil2.OpenRead();
                iCountErrors++;
                printerr("Error_00002! Expected exception not thrown");
            }
            catch (IOException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_00004! Incorrect exception caught, exc==" + exc.ToString());
            }

            //-----------------------------------------------------------------

            // [] Open file and check for canread
            //-----------------------------------------------------------------
            strLoc = "Loc_00005";

            new FileStream(filName, FileMode.Create).Dispose();
            iCountTestcases++;
            fil2 = new FileInfo(filName);
            s2 = fil2.OpenRead();
            iCountTestcases++;
            if (!s2.CanRead)
            {
                iCountErrors++;
                printerr("Error_00006! File not opened canread");
            }

            // [] Check file for canwrite

            iCountTestcases++;
            if (s2.CanWrite)
            {
                iCountErrors++;
                printerr("Error_00007! File was opened canWrite");
            }

            s2.Dispose();
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

