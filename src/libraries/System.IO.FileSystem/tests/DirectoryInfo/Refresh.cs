// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections;
using System.Globalization;
using Xunit;

public class DirectoryInfo_Refresh
{
    public static String s_strActiveBugNums = "";
    public static String s_strClassMethod = "Directory.Refresh()";
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


            DirectoryInfo dir2;
            String dirName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());

            if (Directory.Exists(dirName))
                Directory.Delete(dirName);


            // [] Delete directory and refresh
            //----------------------------------------------------------------
            strLoc = "Loc_00001";

            Directory.CreateDirectory(dirName);
            dir2 = new DirectoryInfo(dirName);
            Directory.Delete(dirName);
            iCountTestcases++;
            dir2.Refresh();

            //----------------------------------------------------------------

            // [] Change name of directory and refresh
            //----------------------------------------------------------------
            strLoc = "Loc_00005";

            string newDirName = Path.Combine(TestInfo.CurrentDirectory, "Temp001");
            if (Directory.Exists(newDirName))
                Directory.Delete(newDirName);

            iCountTestcases++;
            Directory.CreateDirectory(dirName);
            dir2 = new DirectoryInfo(dirName);
            dir2.MoveTo(newDirName);
            dir2.Refresh();
            Directory.Delete(newDirName);
            //----------------------------------------------------------------

            // [] Change Attributes and refresh
            //----------------------------------------------------------------
            strLoc = "Loc_00006";
            iCountTestcases++;

            Directory.CreateDirectory(dirName);
            dir2 = new DirectoryInfo(dirName);

            if (((Int32)dir2.Attributes & (Int32)FileAttributes.ReadOnly) != 0)
            {
                Console.WriteLine(dir2.Attributes);
                iCountErrors++;
                printerr("Error_00007! Attribute set before refresh");
            }
            dir2.Attributes = FileAttributes.ReadOnly;
            dir2.Refresh();
            iCountTestcases++;
            if (((Int32)dir2.Attributes & (Int32)FileAttributes.ReadOnly) <= 0)
            {
                iCountErrors++;
                printerr("Error_00008! Object not refreshed after setting readonly");
            }

            dir2.Attributes = new FileAttributes();
            dir2.Refresh();
            if (((Int32)dir2.Attributes & (Int32)FileAttributes.ReadOnly) != 0)
            {
                iCountErrors++;
                printerr("Error_00009! Object not refreshed after removing readonly");
            }

            //----------------------------------------------------------------

            if (Directory.Exists(dirName))
                Directory.Delete(dirName);

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

