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

public class Directory_SetCreationTime_str_dt
{
    public static String s_strDtTmVer = "2001/02/12 17.04";
    public static String s_strClassMethod = "Directory.GetCreationTime()";
    public static String s_strTFName = "SetCreationTime_str_dt.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    public static void EnsureDirectoryDeletion(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
            Directory.Delete(directoryPath, true);
        int i = 5000;
        while (Directory.Exists(directoryPath) && i > 0)
        {
            Task.Delay(500).Wait(); // Allow some time for file system to settle down
            i -= 500;
        }
        if (Directory.Exists(directoryPath))
            throw new Exception("Directory still exists");
    }
    public static void EnsureDirectoryCreation(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);
        int i = 5000;
        while (!Directory.Exists(directoryPath) && i > 0)
        {
            Task.Delay(500).Wait(); // Allow some time for file system to settle down
            i -= 500;
        }
        if (!Directory.Exists(directoryPath))
            throw new Exception("Directory does not exists");
    }

    [Fact]
    [OuterLoop]
    [PlatformSpecific(PlatformID.Windows)] // roundtripping birthtime not supported on Unix
    public static void runTest()
    {
        String strLoc = "Loc_0001";
        int iCountErrors = 0;
        int iCountTestcases = 0;

        try
        {
            String directoryName = "SetCreationTime_str_dt_TestDirectory";

            // [] With null string
            iCountTestcases++;
            try
            {
                Directory.SetCreationTime(null, DateTime.Today);
                iCountErrors++;
                printerr("Error_0002! Expected exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0003! Unexpected exception thrown: " + exc.ToString());
            }

            // [] With an empty string.
            iCountTestcases++;
            try
            {
                Directory.SetCreationTime("", DateTime.Today);
                iCountErrors++;
                printerr("Error_0004! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0005! Unexpected exception thrown: " + exc.ToString());
            }

            // [] Valid file name and datetime(Today)
            strLoc = "Loc_0006";
            EnsureDirectoryCreation(directoryName);
            iCountTestcases++;
            try
            {
                Directory.SetCreationTime(directoryName, DateTime.Today);
                if ((Directory.GetCreationTime(directoryName) - DateTime.Now).Seconds > 0)
                {
                    iCountErrors++;
                    printerr("Error_0007! Creation time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0008! Unexpected exception thrown: " + exc.ToString());
            }
            EnsureDirectoryDeletion(directoryName);

            //Add one year from DateTime.today.
            strLoc = "Loc_0009";

            EnsureDirectoryCreation(directoryName);
            iCountTestcases++;
            try
            {
                Directory.SetCreationTime(directoryName, DateTime.Now.AddYears(1));
                if ((Directory.GetCreationTime(directoryName) - DateTime.Now.AddYears(1)).Seconds > 0)
                {
                    iCountErrors++;
                    printerr("Error_0010! Creation time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0011! Unexpected exception thrown: " + exc.ToString());
            }
            EnsureDirectoryDeletion(directoryName);

            //Subtract one year from DateTime.today.
            strLoc = "Loc_0012";

            EnsureDirectoryCreation(directoryName);
            iCountTestcases++;
            try
            {
                Directory.SetCreationTime(directoryName, DateTime.Now.AddYears(-1));
                if ((Directory.GetCreationTime(directoryName) - DateTime.Now.AddYears(-1)).Seconds > 0)
                {
                    iCountErrors++;
                    printerr("Error_0013! Creation time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0014! Unexpected exception thrown: " + exc.ToString());
            }
            EnsureDirectoryDeletion(directoryName);

            //Add one month from DateTime.today.
            strLoc = "Loc_0015";

            EnsureDirectoryCreation(directoryName);
            iCountTestcases++;
            try
            {
                Directory.SetCreationTime(directoryName, DateTime.Now.AddMonths(1));
                if ((Directory.GetCreationTime(directoryName) - DateTime.Now.AddMonths(1)).Seconds > 0)
                {
                    iCountErrors++;
                    printerr("Error_0016! Creation time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0017! Unexpected exception thrown: " + exc.ToString());
            }
            EnsureDirectoryDeletion(directoryName);

            //Subtract one month from DateTime.today.
            strLoc = "Loc_0018";

            EnsureDirectoryCreation(directoryName);
            iCountTestcases++;
            try
            {
                Directory.SetCreationTime(directoryName, DateTime.Now.AddMonths(-1));
                if ((Directory.GetCreationTime(directoryName) - DateTime.Now.AddMonths(-1)).Seconds > 0)
                {
                    iCountErrors++;
                    printerr("Error_0019! Creation time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0020! Unexpected exception thrown: " + exc.ToString());
            }
            EnsureDirectoryDeletion(directoryName);

            //Add one day from DateTime.today.
            strLoc = "Loc_0021";

            EnsureDirectoryCreation(directoryName);
            iCountTestcases++;
            try
            {
                Directory.SetCreationTime(directoryName, DateTime.Now.AddDays(1));
                if ((Directory.GetCreationTime(directoryName) - DateTime.Now.AddDays(1)).Seconds > 0)
                {
                    iCountErrors++;
                    printerr("Error_0022! Creation time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0023! Unexpected exception thrown: " + exc.ToString());
            }
            EnsureDirectoryDeletion(directoryName);

            //Subtract one day from DateTime.today.
            strLoc = "Loc_0024";

            EnsureDirectoryCreation(directoryName);
            iCountTestcases++;
            try
            {
                Directory.SetCreationTime(directoryName, DateTime.Now.AddDays(-1));
                if ((Directory.GetCreationTime(directoryName) - DateTime.Now.AddDays(-1)).Seconds > 0)
                {
                    iCountErrors++;
                    printerr("Error_0025! Creation time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0026! Unexpected exception thrown: " + exc.ToString());
            }
            EnsureDirectoryDeletion(directoryName);

            //With invalid datetime object.
            strLoc = "Loc_0025";

            EnsureDirectoryCreation(directoryName);
            iCountTestcases++;
            try
            {
                Directory.SetCreationTime(directoryName, new DateTime(2001, 332, 20, 50, 50, 50));
                iCountErrors++;
                printerr("Error_0026! Creation time cannot be correct");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0027! Unexpected exception thrown: " + exc.ToString());
            }
            EnsureDirectoryDeletion(directoryName);

            //With valid date and time. 
            strLoc = "Loc_0028";

            EnsureDirectoryCreation(directoryName);
            iCountTestcases++;
            try
            {
                DateTime dt = new DateTime(2001, 2, 2, 20, 20, 20);
                Directory.SetCreationTime(directoryName, dt);
                if ((Directory.GetCreationTime(directoryName) - dt).Seconds > 0)
                {
                    iCountErrors++;
                    printerr("Error_0029! Creation time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0030! Unexpected exception thrown: " + exc.ToString());
            }
            EnsureDirectoryDeletion(directoryName);
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            printerr("Error Err_0100!  strLoc==" + strLoc + ", exc_general==" + exc_general.ToString());
        }

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
