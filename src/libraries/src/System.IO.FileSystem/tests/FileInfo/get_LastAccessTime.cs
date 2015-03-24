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

public class FileInfo_get_LastAccessTime
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/05/09 13:19";
    public static String s_strClassMethod = "File.Directory()";
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


            String filName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            FileInfo fil2;
            Stream stream;


            if (File.Exists(filName))
                File.Delete(filName);



            // [] Exception for null string
            //-----------------------------------------------------------

            // [] Create a directory and check the creation time

            strLoc = "Loc_r8r7j";

            new FileStream(filName, FileMode.Create).Dispose();
            fil2 = new FileInfo(filName);
            stream = fil2.OpenWrite();
            stream.Write(new Byte[] { 10 }, 0, 1);
            stream.Dispose();
            Task.Delay(4000).Wait();
            fil2.Refresh();
            iCountTestcases++;
            try
            {
                // Access time is always shows the midday. For example if you create the file on 3/5/2000. 
                // The access time should be some thing like... 3/5/2000 12:00:00 AM.
                DateTime d1 = DateTime.Now;
                DateTime d2 = fil2.LastAccessTime;
                if (d1.Year != d2.Year || d1.Month != d2.Month || d1.Month != d2.Month)
                {
                    iCountErrors++;
                    printerr("Error_20hjx! Creation time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Bug 14952 Error_20fhd! Unexpected exceptiont thrown: " + exc.ToString());
            }


            // [] 

            strLoc = "Loc_20yxc";

            stream = fil2.Open(FileMode.Open);
            stream.Read(new Byte[1], 0, 1);
            stream.Dispose();
            fil2.Refresh();
            Task.Delay(1000).Wait();
            iCountTestcases++;
            try
            {
                DateTime d1 = DateTime.Now;
                DateTime d2 = fil2.LastAccessTime;
                if (d1.Year != d2.Year || d1.Month != d2.Month || d1.Month != d2.Month)
                {
                    iCountErrors++;
                    Console.WriteLine((DateTime.Now - fil2.LastAccessTime).TotalMilliseconds);
                    printerr("Eror_209x9! LastAccessTime is way off");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_209jx! Unexpected exception thrown: " + exc.ToString());
            }
            fil2.Delete();


            //-----------------------------------------------------------


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

