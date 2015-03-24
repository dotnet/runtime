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

public class FileInfo_get_LastWriteTime
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/05/09 13:19";
    public static String s_strClassMethod = "File.Directory()";
    public static String s_strTFName = "get_LastWriteTime.cs";
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
            if ((DateTime.Now - fil2.LastWriteTime).TotalMilliseconds < 2000 ||
               (DateTime.Now - fil2.LastWriteTime).TotalMilliseconds > 5000)
            {
                iCountErrors++;
                Console.WriteLine(fil2.LastWriteTime);
                Console.WriteLine((DateTime.Now - fil2.LastWriteTime).TotalMilliseconds);
                printerr("Error_20hjx! Last Write Time time cannot be correct");
            }


            // [] 

            strLoc = "Loc_20yxc";

            stream = fil2.Open(FileMode.Open, FileAccess.Read);
            stream.Read(new Byte[1], 0, 1);
            stream.Dispose();
            fil2.Refresh();
            Task.Delay(2000).Wait();
            iCountTestcases++;
            if ((DateTime.Now - fil2.LastWriteTime).TotalMilliseconds < 4000 ||
               (DateTime.Now - fil2.LastWriteTime).TotalMilliseconds > 7000)
            {
                iCountErrors++;
                Console.WriteLine((DateTime.Now - fil2.LastWriteTime).TotalMilliseconds);
                printerr("Eror_209x9! LastWriteTime is way off");
            }

            stream = fil2.Open(FileMode.Open);
            stream.Write(new Byte[] { 10 }, 0, 1);
            stream.Dispose();
            Task.Delay(3000).Wait();
            fil2.Refresh();
            iCountTestcases++;
            if ((DateTime.Now - fil2.LastWriteTime).TotalMilliseconds < 1000 ||
               (DateTime.Now - fil2.LastWriteTime).TotalMilliseconds > 5000)
            {
                iCountErrors++;
                Console.WriteLine((DateTime.Now - fil2.LastWriteTime).TotalMilliseconds);
                printerr("Eror_f984f! LastWriteTime is way off");
            }




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

