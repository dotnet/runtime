// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;
using Xunit;

public class Directory_SetLastWriteTime_str_dt
{
    public static String s_strDtTmVer = "2001/02/12 17.04";
    public static String s_strClassMethod = "Directory.SetLastWriteTime()";
    public static String s_strTFName = "SetLastWriteTime_str_dt.cs";
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
            String fileName = "SetLastWriteTime_str_dt_test_TestDirectory";

            // [] With null string
            iCountTestcases++;
            try
            {
                Directory.SetLastWriteTime(null, DateTime.Today);
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

            // [] With an empty string.
            iCountTestcases++;
            try
            {
                Directory.SetLastWriteTime("", DateTime.Today);
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

            // [] Valid file name and datetime(Today)
            strLoc = "Loc_0006";
            DirectoryInfo dir2 = new DirectoryInfo(fileName);
            dir2.Create();
            iCountTestcases++;
            try
            {
                Directory.SetLastWriteTime(fileName, DateTime.Today);
                if ((Directory.GetLastWriteTime(fileName) - DateTime.Now).Seconds > 0)
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
                Directory.SetLastWriteTime(fileName, DateTime.Now.AddYears(1));
                int iSeconds = (Directory.GetLastWriteTime(fileName) - DateTime.Now.AddYears(1)).Seconds;
                if (iSeconds > 2)
                {
                    iCountErrors++;
                    Console.WriteLine("Error_0010! Creation time cannot be correct, Expected....(<2), Actual....{0}", iSeconds);
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
                DateTime now = DateTime.Now;
                now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
                Directory.SetLastWriteTime(fileName, now.AddYears(-1));
                if (Directory.GetLastWriteTime(fileName) != now.AddYears(-1))
                {
                    iCountErrors++;
                    printerr("Error_0013! Creation time cannot be correct");
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
                Directory.SetLastWriteTime(fileName, DateTime.Now.AddMonths(1));
                int iSeconds = (Directory.GetLastWriteTime(fileName) - DateTime.Now.AddMonths(1)).Seconds;
                if (iSeconds > 2)
                {
                    iCountErrors++;
                    Console.WriteLine("Error_0016! Creation time cannot be correct, Expected....(<2), Actual....{0}", iSeconds);
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
                Directory.SetLastWriteTime(fileName, DateTime.Now.AddMonths(-1));
                int iSeconds = (Directory.GetLastAccessTime(fileName) - DateTime.Now.AddMonths(-1)).Seconds;
                if (iSeconds > 60)
                {
                    iCountErrors++;
                    printerr("Error_0019! Creation time cannot be correct" + iSeconds);
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
                Directory.SetLastWriteTime(fileName, DateTime.Now.AddDays(1));
                int iSeconds = (Directory.GetLastWriteTime(fileName) - DateTime.Now.AddDays(1)).Seconds;
                if (iSeconds > 2)
                {
                    iCountErrors++;
                    Console.WriteLine("Error_0022! Creation time cannot be correct, Expected....(<2), Actual....{0}", iSeconds);
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
                Directory.SetLastWriteTime(fileName, DateTime.Now.AddDays(-1));
                int iSeconds = (Directory.GetLastAccessTime(fileName) - DateTime.Now.AddMonths(-1)).Seconds;
                if (iSeconds > 60)
                {
                    iCountErrors++;
                    printerr("Error_0025! Creation time cannot be correct" + iSeconds);
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
                Directory.SetLastWriteTime(fileName, new DateTime(2001, 332, 20, 50, 50, 50));
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
                Directory.SetLastWriteTime(fileName, dt);
                if ((Directory.GetLastWriteTime(fileName) - dt).Seconds > 60)
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
            printerr("Error Err_0100!  strLoc==" + strLoc + ", exc_general==" + exc_general.ToString());
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