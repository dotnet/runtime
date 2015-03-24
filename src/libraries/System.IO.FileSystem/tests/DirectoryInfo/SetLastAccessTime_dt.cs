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

public class DirectoryInfo_set_LastAccessTime_dt
{
    public static String s_strActiveBugNums = "";
    public static String s_strClassMethod = "DirectoryInfo.LastAccessTime";
    public static String s_strTFName = "set_LastAccessTime_dt.cs";
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
            String fileName = Path.GetRandomFileName();
            // [] Valid file name and datetime(Today)
            strLoc = "Loc_0006";
            DirectoryInfo dir2 = new DirectoryInfo(fileName);
            dir2.Create();
            iCountTestcases++;
            try
            {
                dir2.LastAccessTime = DateTime.Today;
                if ((dir2.LastAccessTime - DateTime.Now).Seconds > 0)
                {
                    iCountErrors++;
                    printerr("Error_0007! Creation time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0008! Unexpected exceptiont thrown: " + exc.ToString());
            }
            FailSafeDirectoryOperations.DeleteDirectoryInfo(dir2, false);

            //Add one year from DateTime.today.
            strLoc = "Loc_0009";

            dir2 = new DirectoryInfo(fileName);
            dir2.Create();
            iCountTestcases++;
            try
            {
                dir2.LastAccessTime = DateTime.Now.AddYears(1);
                if ((dir2.LastAccessTime - DateTime.Now.AddYears(1)).Seconds > 0)
                {
                    iCountErrors++;
                    printerr("Error_0010! Creation time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0011! Unexpected exceptiont thrown: " + exc.ToString());
            }
            FailSafeDirectoryOperations.DeleteDirectoryInfo(dir2, false);

            //Subtract one year from DateTime.today.
            strLoc = "Loc_0012";

            dir2 = new DirectoryInfo(fileName);
            dir2.Create();
            iCountTestcases++;
            try
            {
                DateTime dt = DateTime.Now.AddYears(-1);
                dir2.LastAccessTime = dt;
                if (!CompareDates(dt, dir2.LastAccessTime))
                {
                    iCountErrors++;
                    printerr("Error_0013! Creation time cannot be correct" + CompareDates(dt, dir2.LastAccessTime));
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0014! Unexpected exceptiont thrown: " + exc.ToString());
            }
            FailSafeDirectoryOperations.DeleteDirectoryInfo(dir2, false);

            //Add one month from DateTime.today.
            strLoc = "Loc_0015";

            dir2 = new DirectoryInfo(fileName);
            dir2.Create();
            iCountTestcases++;
            try
            {
                dir2.LastAccessTime = DateTime.Now.AddMonths(1);
                if ((dir2.LastAccessTime - DateTime.Now.AddMonths(1)).Seconds > 0)
                {
                    iCountErrors++;
                    printerr("Error_0016! Creation time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0017! Unexpected exceptiont thrown: " + exc.ToString());
            }
            FailSafeDirectoryOperations.DeleteDirectoryInfo(dir2, false);

            //Subtract one month from DateTime.today.
            strLoc = "Loc_0018";

            dir2 = new DirectoryInfo(fileName);
            dir2.Create();
            iCountTestcases++;
            try
            {
                dir2.LastAccessTime = DateTime.Now.AddMonths(-1);
                if ((dir2.LastAccessTime - DateTime.Now.AddMonths(-1)).Seconds > 0)
                {
                    iCountErrors++;
                    printerr("Error_0019! Creation time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0020! Unexpected exceptiont thrown: " + exc.ToString());
            }
            FailSafeDirectoryOperations.DeleteDirectoryInfo(dir2, false);

            //Add one day from DateTime.today.
            strLoc = "Loc_0021";

            dir2 = new DirectoryInfo(fileName);
            dir2.Create();
            iCountTestcases++;
            try
            {
                dir2.LastAccessTime = DateTime.Now.AddDays(1);
                if ((dir2.LastAccessTime - DateTime.Now.AddDays(1)).Seconds > 0)
                {
                    iCountErrors++;
                    printerr("Error_0022! Creation time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0023! Unexpected exceptiont thrown: " + exc.ToString());
            }
            FailSafeDirectoryOperations.DeleteDirectoryInfo(dir2, false);

            //Subtract one day from DateTime.today.
            strLoc = "Loc_0024";

            dir2 = new DirectoryInfo(fileName);
            dir2.Create();
            iCountTestcases++;
            try
            {
                dir2.LastAccessTime = DateTime.Now.AddDays(-1);
                if ((dir2.LastAccessTime - DateTime.Now.AddDays(-1)).Seconds > 0)
                {
                    iCountErrors++;
                    printerr("Error_0025! Creation time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0026! Unexpected exceptiont thrown: " + exc.ToString());
            }
            FailSafeDirectoryOperations.DeleteDirectoryInfo(dir2, false);

            //With invalid datetime object.
            strLoc = "Loc_0025";

            dir2 = new DirectoryInfo(fileName);
            dir2.Create();
            iCountTestcases++;
            try
            {
                dir2.LastAccessTime = new DateTime(2001, 332, 20, 50, 50, 50);
                iCountErrors++;
                printerr("Error_0026! Creation time cannot be correct");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0027! Unexpected exceptiont thrown: " + exc.ToString());
            }
            FailSafeDirectoryOperations.DeleteDirectoryInfo(dir2, false);

            //With valid date and time. 
            strLoc = "Loc_0028";

            dir2 = new DirectoryInfo(fileName);
            dir2.Create();
            iCountTestcases++;
            try
            {
                DateTime dt = new DateTime(2001, 2, 2, 20, 20, 20);
                dir2.LastAccessTime = dt;
                if ((dir2.LastAccessTime - dt).Seconds > 0)
                {
                    iCountErrors++;
                    printerr("Error_0029! Creation time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0030! Unexpected exceptiont thrown: " + exc.ToString());
            }
            FailSafeDirectoryOperations.DeleteDirectoryInfo(dir2, false);
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

    private static bool CompareDates(DateTime dt1, DateTime dt2)
    {
        if ((dt1.Year == dt2.Year) && (dt1.Month == dt2.Month) && (dt1.Day == dt2.Day))
            return true;
        else
            return false;
    }
}
