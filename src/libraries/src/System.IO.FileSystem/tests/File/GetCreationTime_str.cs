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

public class File_GetCreationTime_str
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2001/01/31 13:19";
    public static String s_strClassMethod = "File.GetCreationTime()";
    public static String s_strTFName = "GetCreationTime_str.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
    [OuterLoop]
    public static void runTest()
    {
        String strLoc = "Loc_0001";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;

        try
        {
            String fileName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());

            // [] Exception for null string
            //-----------------------------------------------------------
            iCountTestcases++;
            try
            {
                DateTime dt = File.GetCreationTime(null);
                iCountErrors++;
                printerr("Error_0009! Expected exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0010! Unexpected exceptiont thrown: " + exc.ToString());
            }

            // [] Create a file and check the creation time

            strLoc = "Loc_0002";

            FileInfo file1 = new FileInfo(fileName);
            FileStream fs = file1.Create();
            fs.Dispose();
            iCountTestcases++;
            try
            {
                if (Math.Abs((File.GetCreationTime(fileName) - DateTime.Now).TotalSeconds) > 3)
                {
                    Console.WriteLine("{0}, {1}", File.GetCreationTime(fileName), DateTime.Now);

                    iCountErrors++;
                    printerr("Error_0003! Creation time should be within 3 seconds of now");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0004! Unexpected exceptiont thrown: " + exc.ToString());
            }
            file1.Delete();


            // [] Create file, sleep for a while and check the creation time.
            // Update: 2005/09/07. This fails when run repeatedly. It looks like the the creation time reported is the one at first time run creation.
            // I suspect the OS caches this information. Modifying to get a random file

            strLoc = "Loc_0005";
            fileName = Path.GetTempFileName();
            FileInfo file2 = new FileInfo(fileName);
            FileStream fs2 = file2.Create();
            fs2.Dispose();
            Task.Delay(2000).Wait();
            iCountTestcases++;
            try
            {
                if ((DateTime.Now - File.GetCreationTime(fileName)).Seconds > 3)
                {
                    Console.WriteLine(DateTime.Now);
                    Console.WriteLine((DateTime.Now - File.GetCreationTime(fileName)).Seconds.ToString());
                    Console.WriteLine(file2.CreationTime);

                    iCountErrors++;
                    printerr("Eror_0006! Creation time is off");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0007! Unexpected exception thrown: " + exc.ToString());
            }
            file2.Delete();
            file2.Refresh();

            strLoc = "Loc_0012";
            iCountTestcases++;
            try
            {
                if (File.Exists(s_strTFName))
                {
                    Console.WriteLine("File date :: " + File.GetCreationTime(s_strTFName));
                    Console.WriteLine("Today :: " + DateTime.Today);
                    //When you are running from Maddog it should work because it copies down the file just before running
                    //it. The creation time should be less than a minute. 
                    if ((DateTime.Today - File.GetCreationTime(s_strTFName)).Seconds > 60)
                    {
                        iCountErrors++;
                        printerr("Eror_0013! Creation time is off");
                    }
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0014! Unexpected exception thrown: " + exc.ToString());
            }
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

