// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Xunit;

public class Directory_GetLastWriteTime_str
{
    public static String s_strDtTmVer = "2001/02/12 17:29";
    public static String s_strClassMethod = "Directory.GetLastWriteTime()";
    public static String s_strTFName = "GetLastWriteTime_str.cs";
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
            String dirName = Path.Combine(TestInfo.CurrentDirectory, "GetLastWriteTime_str_TestDir");
            DirectoryInfo dir2;

            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);

            // [] Create a directory and check the last access time 

            strLoc = "Loc_r8r7j";

            dir2 = Directory.CreateDirectory(dirName);
            //dir2.GetFileSystemInfos (dirName);
            dir2.Refresh();
            iCountTestcases++;
            try
            {
                if ((DateTime.Now - Directory.GetLastWriteTime(dirName)).Minutes > 6)
                {
                    iCountErrors++;
                    Console.WriteLine(Directory.GetLastWriteTime(dirName));
                    Console.WriteLine("Now:" + DateTime.Now);
                    Console.WriteLine((DateTime.Now - Directory.GetLastWriteTime(dirName)).TotalMilliseconds);
                    printerr("Error_20hjx! GetLastWriteTime( dirName ) time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_20fhd! Unexpected exceptiont thrown: " + exc.ToString());
            }

            // [] access the directory and check the time

            strLoc = "Loc_20yxc";

            //dir2.GetFiles();
            dir2.CreateSubdirectory("Sub");
            dir2.Refresh();
            Task.Delay(1000).Wait();
            iCountTestcases++;
            try
            {
                if ((DateTime.Now - Directory.GetLastWriteTime(dirName)).Minutes > 6)
                {
                    iCountErrors++;
                    Console.WriteLine((DateTime.Now - Directory.GetLastWriteTime(dirName)).TotalMilliseconds);
                    printerr("Eror_209x9! GetLastWriteTime( dirName ) is way off");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_209jx! Unexpected exception thrown: " + exc.ToString());
            }
            dir2.Delete(true);

            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            printerr("Error Err_8888yyy!  strLoc==" + strLoc + ", exc_general==" + exc_general.ToString());
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
