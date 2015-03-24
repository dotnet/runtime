// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections;
using System.Globalization;
using Xunit;

public class FileInfo_Refresh
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/07/12 19:34";
    public static String s_strClassMethod = "File.Refresh()";
    public static String s_strTFName = "Refresh.cs";
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
            String filName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());

            if (File.Exists(filName))
                File.Delete(filName);


            // [] Delete File and refresh
            //----------------------------------------------------------------
            strLoc = "Loc_00001";

            File.Open(filName, FileMode.Create).Dispose();
            fil2 = new FileInfo(filName);
            File.Delete(filName);
            iCountTestcases++;
            fil2.Refresh();

            //----------------------------------------------------------------

            // [] Change name of File and refresh
            //----------------------------------------------------------------
            strLoc = "Loc_00005";

            string tempFilName = Path.Combine(TestInfo.CurrentDirectory, "Temp001");
            if (File.Exists(tempFilName))
                File.Delete(tempFilName);

            iCountTestcases++;

            File.Open(filName, FileMode.Create).Dispose();
            fil2 = new FileInfo(filName);
            fil2.MoveTo(tempFilName);
            fil2.Refresh();
            File.Delete(tempFilName);
            //----------------------------------------------------------------

            // [] Change Attributes and refresh
            //----------------------------------------------------------------
            strLoc = "Loc_00006";

            iCountTestcases++;
            File.Open(filName, FileMode.Create).Dispose();
            fil2 = new FileInfo(filName);
            if (((Int32)fil2.Attributes & (Int32)FileAttributes.ReadOnly) != 0)
            {
                Console.WriteLine(fil2.Attributes);
                iCountErrors++;
                printerr("Error_00007! Attribute set before refresh");
            }

            fil2.Attributes = FileAttributes.ReadOnly;
            fil2.Refresh();
            iCountTestcases++;
            if (((Int32)fil2.Attributes & (Int32)FileAttributes.ReadOnly) <= 0)
            {
                iCountErrors++;
                printerr("Error_00008! Object not refreshed after setting readonly");
            }

            fil2.Attributes = new FileAttributes();
            fil2.Refresh();
            if (((Int32)fil2.Attributes & (Int32)FileAttributes.ReadOnly) != 0)
            {
                iCountErrors++;
                printerr("Error_00009! Object not refreshed after removing readonly");
            }

            //----------------------------------------------------------------

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

