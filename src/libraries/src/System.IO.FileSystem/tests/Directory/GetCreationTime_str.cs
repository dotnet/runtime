// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;

public class Directory_GetCreationTime
{
    public static String s_strDtTmVer = "2009/02/18";
    public static String s_strClassMethod = "Directory.GetCreationTime()";
    public static String s_strTFName = "GetCreationTime.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
    [OuterLoop]
    public static void runTest()
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";

        try
        {
            DirectoryInfo dir = null;
            String strDirName = "";

            // [] Create a directory and check the creation time
            strLoc = "Loc_r8r7j";

            strDirName = Path.Combine(TestInfo.CurrentDirectory, "TestDir");
            dir = Directory.CreateDirectory(strDirName);
            iCountTestcases++;
            try
            {
                if (Math.Abs((Directory.GetCreationTime(strDirName) - DateTime.Now).TotalSeconds) > 3)
                {
                    iCountErrors++;
                    printerr("Error_20hjx! Creation time should be within 3 seconds of now");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_20fhd! Unexpected exceptiont thrown: " + exc.ToString());
            }
            dir.Delete(true);

            // remote directory test moved to RemoteIOTests.cs

            // [] Check the creation time for an existing file.

            strLoc = "Loc_20er";

            strDirName = Path.Combine(TestInfo.CurrentDirectory, "blah");
            dir = Directory.CreateDirectory(strDirName);
            Task.Delay(2000).Wait();
            iCountTestcases++;
            try
            {
                if ((DateTime.Now - Directory.GetCreationTime(strDirName)).Seconds > 3)
                {
                    iCountErrors++;
                    printerr("Eror_3123! Creation time is off");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_3543! Unexpected exception thrown: " + exc.ToString());
            }
            dir.Delete(true);

#if !TEST_WINRT
            //#469226 - DirectoryInfo.CreationTime throws System.ArgumentOutOfRangeException for directories on CDs
            //postponed but we will add the scenario
            try
            {
                IEnumerable<string> drives = IOServices.GetReadyDrives();
                foreach (string drive in drives)
                {
                    String[] dirs = Directory.GetDirectories(drive);
                    int count = 10;
                    if (dirs.Length < count)
                        count = dirs.Length;
                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            DateTime time = Directory.GetCreationTime(dirs[i]);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            Console.WriteLine("Info_9237tgfasd: #469226? drive: {0}, directory: {1}", drive, dirs[i]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ++iCountErrors;
                Console.WriteLine("Err_734g@! exception thrown: {0}", ex);
            }
#endif

        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            printerr("Error Err_8888yyy!  strLoc==" + strLoc + ", exc_general==" + exc_general.ToString());
        }
        if (iCountErrors != 0)
        {
            Console.WriteLine("FAiL! " + s_strTFName + " ,iCountErrors==" + iCountErrors.ToString());
        }

        Assert.Equal(0, iCountErrors);
    }

    private bool CompareDates(DateTime dt1, DateTime dt2)
    {
        Console.WriteLine(dt1);
        Console.WriteLine(dt2);
        if ((dt1.Year == dt2.Year) && (dt1.Month == dt2.Month) && (dt1.Day == dt2.Day))
            return true;
        else
            return false;
    }

    public static void printerr(String err, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine("ERROR: ({0}, {1}, {2}) {3}", memberName, filePath, lineNumber, err);
    }
}
