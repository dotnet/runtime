// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public class DirectoryInfo_get_LastAccessTime
{
    public static String s_strActiveBugNums = "";
    public static String s_strClassMethod = "Directory.LastAccessTime";
    public static String s_strTFName = "get_LastAccessTime.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
    [OuterLoop]
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


            String dirName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            DirectoryInfo dir2;


            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);



            // [] Create a directory and check the last access time 

            strLoc = "Loc_r8r7j";

            dir2 = Directory.CreateDirectory(dirName);
            dir2.Refresh();
            iCountTestcases++;
            try
            {
                if ((DateTime.Now - dir2.LastAccessTime).Days != 0)
                {
                    iCountErrors++;
                    Console.WriteLine(dir2.LastAccessTime);
                    Console.WriteLine((DateTime.Now - dir2.LastAccessTime).Days);
                    printerr("Error_20hjx! Access time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Bug 14952 Error_20fhd! Unexpected exceptiont thrown: " + exc.ToString());
            }

            // [] access the directory and check the time

            strLoc = "Loc_20yxc";

            dir2.Refresh();
            Task.Delay(1000).Wait();
            iCountTestcases++;
            try
            {
                if ((DateTime.Now - dir2.LastAccessTime).Days != 0)
                {
                    iCountErrors++;
                    Console.WriteLine((DateTime.Now - dir2.LastAccessTime).Days);
                    printerr("Eror_209x9! LastAccessTime is way off");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_209jx! Unexpected exception thrown: " + exc.ToString());
            }
            dir2.Delete();


            //-----------------------------------------------------------

            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);

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

