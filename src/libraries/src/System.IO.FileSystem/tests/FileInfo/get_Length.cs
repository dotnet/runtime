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

public class FileInfo_get_Length
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/05/09 11:28";
    public static String s_strClassMethod = "File.Directory()";
    public static String s_strTFName = "get_Length.cs";
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
            FileInfo fil2;
            FileStream fs2;
            StreamWriter sw2;



            if (File.Exists(filName))
                File.Delete(filName);

            // [] Set negative length
            //--------------------------------------------------------
            strLoc = "Loc_98yc8";

            fs2 = new FileStream(filName, FileMode.Create);
            fs2.Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            if (fil2.Length != 0)
            {
                iCountErrors++;
                printerr("Error_2898t! Incorrect length==" + fs2.Length);
            }
            //--------------------------------------------------------


            // [] Set position after end of stream and write a bit
            //--------------------------------------------------------
            strLoc = "Loc_27yxc";

            fs2 = new FileStream(filName, FileMode.Create);
            fs2.SetLength(50);
            fs2.Position = 50;
            sw2 = new StreamWriter(fs2);
            for (char c = 'a'; c < 'f'; c++)
                sw2.Write(c);
            sw2.Flush();
            iCountTestcases++;
            fil2 = new FileInfo(filName);
            fs2.Dispose();
            if (fil2.Length != 55)
            {
                iCountErrors++;
                printerr("Error_389xd! Incorrect stream length==" + fs2.Length + ", filelength==" + fil2.Length);
            }

            // [] Truncate file by setting length in middle of file
            strLoc = "Loc_f47yv";

            fs2 = new FileStream(filName, FileMode.Open);
            fs2.SetLength(30);
            fs2.Flush();
            iCountTestcases++;
            fs2.Dispose();
            fil2.Refresh();
            if (fil2.Length != 30)
            {
                iCountErrors++;
                printerr("Error_28xye! Incorrect, filelength==" + fil2.Length);
            }

            // [] Increase the length by setting the length after end

            strLoc = "Loc_487yv";
            fs2 = new FileStream(filName, FileMode.Open);
            fs2.SetLength(100);
            fs2.Flush();
            fs2.Dispose();
            fil2.Refresh();
            iCountTestcases++;
            if (fil2.Length != 100)
            {
                iCountErrors++;
                printerr("Error_2090x! Incorrect filelength==" + fil2.Length);
            }
            fs2.Dispose();
            fil2.Refresh();
            iCountTestcases++;
            if (fil2.Length != 100)
            {
                iCountErrors++;
                printerr("Error_297ty! Incorrect length==" + fs2.Length);
            }
            //--------------------------------------------------------




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

