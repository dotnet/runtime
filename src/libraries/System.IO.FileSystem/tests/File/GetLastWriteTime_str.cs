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

public class File_GetLastWriteTime_str
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2001/1/31 19:00";
    public static String s_strClassMethod = "File.GetLastWriteTime()";
    public static String s_strTFName = "GetLastWriteTime_str.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
    public static void runTest()
    {
        String strLoc = "Loc_0001";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;

        try
        {
            String fileName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            FileInfo file2;
            Stream stream;

            if (File.Exists(fileName))
                File.Delete(fileName);

            // [] Test with null string
            iCountTestcases++;
            try
            {
                DateTime dt = File.GetLastWriteTime(null);
                iCountErrors++;
                printerr("Error_0002! Expected exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0003! Unexpected exceptiont thrown: " + exc.ToString());
            }

            // [] Test with an empty string
            iCountTestcases++;
            try
            {
                DateTime dt = File.GetLastWriteTime("");
                iCountErrors++;
                printerr("Error_0004! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0005! Unexpected exceptiont thrown: " + exc.ToString());
            }

            // [] Create a new file and verify the lastwrite time.
            strLoc = "Loc_0006";

            file2 = new FileInfo(fileName);
            FileStream fs = file2.Create();
            fs.Write(new Byte[] { 10 }, 0, 1);
            fs.Dispose();

            double lMilliSecs = (DateTime.Now - File.GetLastWriteTime(fileName)).TotalMilliseconds;

            //file2.Refresh();
            iCountTestcases++;

            if ((DateTime.Now - File.GetLastWriteTime(fileName)).Minutes > 6)
            {
                Console.WriteLine("Millis.. " + lMilliSecs);
                Console.WriteLine("FileName ... " + (DateTime.Now - File.GetLastWriteTime(fileName)).Minutes);
                Console.WriteLine(DateTime.Now);
                Console.WriteLine(File.GetLastWriteTime(fileName));

                iCountErrors++;
                Console.WriteLine((DateTime.Now - File.GetLastWriteTime(fileName)).TotalMilliseconds);
                printerr("Error_0007! Last Write Time time cannot be correct");
            }

            // Re-open the file and write some more info and verify.
            strLoc = "Loc_0008";

            stream = file2.Open(FileMode.Open, FileAccess.Read);
            stream.Read(new Byte[1], 0, 1);
            stream.Dispose();
            file2.Refresh();
            iCountTestcases++;
            if ((DateTime.Now - File.GetLastWriteTime(fileName)).TotalMinutes > 6)
            {
                iCountErrors++;
                Console.WriteLine((DateTime.Now - File.GetLastWriteTime(fileName)).TotalMilliseconds);
                printerr("Eror_0009! LastWriteTime is way off");
            }
            file2.Delete();

            // [] Verify lastwritetime for an old file.
            strLoc = "Loc_0010";
            iCountTestcases++;
            try
            {
                if (File.Exists(s_strTFName))
                {
                    //When you are running from Maddog it should work because it copies down the file just before running
                    //it. The creation time should be less than a minute. 
                    Console.WriteLine(File.GetLastWriteTime(s_strTFName));
                    if ((DateTime.Today - File.GetLastWriteTime(s_strTFName)).Seconds > 60)
                    {
                        iCountErrors++;
                        printerr("Eror_0011! LastWrite time is not correct");
                    }
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0012! Unexpected exception thrown: " + exc.ToString());
            }

            if (File.Exists(fileName))
                File.Delete(fileName);
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

