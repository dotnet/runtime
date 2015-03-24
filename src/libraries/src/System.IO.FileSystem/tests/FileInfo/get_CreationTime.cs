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

public class FileInfo_get_CreationTime
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/05/09 13:19";
    public static String s_strClassMethod = "File.Directory()";
    public static String s_strTFName = "get_CreationTime.cs";
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
            String filName = s_strTFName.Substring(0, s_strTFName.IndexOf('.')) + "_test_" + "TestFile";
            FileInfo fil2;

            // [] Exception for null string
            //-----------------------------------------------------------
            iCountTestcases++;

            // This really isn't an issue. Just incrementing to keep the mad dog happy


            // [] Create a file and check the creation time - if the file does not exist,we should throw

            strLoc = "Loc_r8r7j";


            //We should make sure that the file doesn't exist
            if (File.Exists(filName))
                File.Delete(filName);

            DirectoryInfo newDirectory = new DirectoryInfo(".");
            fil2 = new FileInfo(newDirectory + filName);
            iCountTestcases++;
            try
            {
                DateTime d1 = fil2.CreationTime;
                //We threw in Whidbey upto 50606 but V1.1 behavior is as below
                if (d1 != DateTime.FromFileTime(0))
                {
                    iCountErrors++;
                    Console.WriteLine("Error_20hjx! Creation time cannot be correct: {0}", d1);
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Bug 14952 - Error_20fhd! Unexpected exceptiont thrown: " + exc.ToString());
            }
            fil2.Delete();


            // [] Create file, sleep for a while and check the creation time.

            strLoc = "Loc_20yxc";
            fil2 = new FileInfo(Path.GetTempFileName());
            FileStream fs = fil2.Create();
            Task.Delay(2000).Wait();
            iCountTestcases++;
            try
            {
                if ((DateTime.Now - fil2.CreationTime).Minutes > 1)
                {
                    Console.WriteLine(DateTime.Now);
                    Console.WriteLine(DateTime.Now - fil2.CreationTime);
                    Console.WriteLine(fil2.CreationTime);

                    iCountErrors++;
                    printerr("Eror_209x9! Creation time is off");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_209jx! Unexpected exception thrown: " + exc.ToString());
            }
            fs.Dispose();
            fil2.Delete();
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

