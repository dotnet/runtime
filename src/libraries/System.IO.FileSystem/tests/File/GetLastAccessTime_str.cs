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

public class File_GetLastAccessTime_str
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2001/01/31 18:25";
    public static String s_strClassMethod = "File.GetLastAccessTime()";
    public static String s_strTFName = "GetLastAccessTime_str.cs";
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
            String filName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            FileInfo fil2;
            Stream stream;

            if (File.Exists(filName))
                File.Delete(filName);

            // [] Test with null string
            iCountTestcases++;
            try
            {
                DateTime dt = File.GetLastAccessTime(null);
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
                DateTime dt = File.GetLastAccessTime("");
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

            // [] Create a file and check the creation time
            strLoc = "Loc_0006";

            FileStream fs = new FileStream(filName, FileMode.Create);
            fs.Dispose();
            fil2 = new FileInfo(filName);
            stream = fil2.OpenWrite();
            stream.Write(new Byte[] { 10 }, 0, 1);
            stream.Dispose();
            Task.Delay(4000).Wait();
            fil2.Refresh();
            iCountTestcases++;
            try
            {
                DateTime d1 = fil2.LastAccessTime;
                DateTime d2 = DateTime.Today;
                if (d1.Year != d2.Year || d1.Month != d2.Month || d1.Day != d2.Day)
                {
                    iCountErrors++;
                    printerr("Error_0007! Creation time cannot be correct.. Expected:::" + DateTime.Today.ToString() + ", Actual:::" + fil2.LastAccessTime.ToString());
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0008! Unexpected exceptiont thrown: " + exc.ToString());
            }

            // [] Verify lastaccesstime for an old file.
            strLoc = "Loc_0009";
            iCountTestcases++;
            try
            {
                //When you are running from Maddog it should work because it copies down the file just before running
                //it. The creation time should be less than a minute. 
                if (File.Exists(s_strTFName))
                {
                    if ((DateTime.Today - File.GetLastAccessTime(s_strTFName)).Seconds > 60)
                    {
                        iCountErrors++;
                        printerr("Eror_0010! LastAccess time is not correct");
                    }
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0011! Unexpected exception thrown: " + exc.ToString());
            }
            fil2.Delete();

            if (File.Exists(filName))
                File.Delete(filName);
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

