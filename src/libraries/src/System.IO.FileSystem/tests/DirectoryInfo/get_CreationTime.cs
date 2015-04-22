// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Globalization;
using Xunit;

public class DirectoryInfo_get_CreationTime
{
    public static String s_strActiveBugNums = "14952";
    public static String s_strClassMethod = "Directory.CreationTime";
    public static String s_strTFName = "get_CreationTime.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
    [OuterLoop]
    [PlatformSpecific(PlatformID.Windows | PlatformID.OSX)] // getting birthtime not supported on all Unix systems
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

            DirectoryInfo dir = null;




            // [] Create a directory and check the creation time

            strLoc = "Loc_r8r7j";


            dir = Directory.CreateDirectory(Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName()));
            iCountTestcases++;
            try
            {
                if (Math.Abs((dir.CreationTime - DateTime.Now).TotalSeconds) > 3)
                {
                    iCountErrors++;
                    Console.WriteLine("Error_20hjx! Creation time cannot be correct: <{0}>", dir.CreationTime);
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Bug 14952 Error_20fhd! Unexpected exceptiont thrown: " + exc.ToString());
            }
            dir.Delete(true);


            // [] Do some sleeps and check the creation time

            strLoc = "Loc_20yxc";

            dir = Directory.CreateDirectory(Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName()));
            Task.Delay(2000).Wait();
            iCountTestcases++;
            try
            {
                if ((DateTime.Now - dir.CreationTime).Seconds > 3)
                {
                    iCountErrors++;
                    printerr("Eror_209x9! Creation time is off");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_209jx! Unexpected exception thrown: " + exc.ToString());
            }
            dir.Delete(true);

            //See VSWhidbey # 92050
            if (Interop.IsWindows) // for back compat, rather than throwing, Windows returns default values when the file doesn't exist
            {
                String path = TestInfo.CurrentDirectory + Path.DirectorySeparatorChar;
                String tempPath;
                int count = 0;
                while (true)
                {
                    tempPath = path + "foo_" + count++;
                    if (!Directory.Exists(tempPath))
                        break;
                }
                dir = new DirectoryInfo(tempPath);
                try
                {
                    DateTime d1 = dir.CreationTime;
                    if (d1 != DateTime.FromFileTime(0))
                    {
                        iCountErrors++;
                        printerr("Error_20hjx! Creation time cannot be correct");
                    }
                }
                catch (Exception exc)
                {
                    iCountErrors++;
                    printerr("Error_20fhd! Unexpected exceptiont thrown: " + exc.ToString());
                }
            }

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

