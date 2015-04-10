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

public class File_SetLastWriteTime_str_dt
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer       = "2001/01/31 18.45";
    public static String s_strClassMethod   = "File.GetLastAccessTime()";
    public static String s_strTFName        = "SetLastWriteTime_str_dt.cs";
    public static String s_strTFPath        = Directory.GetCurrentDirectory();

    [Fact]
    public static void runTest()
    {
        String strLoc = "Loc_0001";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;

        try
        {
            String fileName = s_strTFName.Substring(0, s_strTFName.IndexOf('.')) + "_test_" +"TestFile";			

            // [] With null string
            iCountTestcases++;
            try
            {
                File.SetLastWriteTime(null, DateTime.Today);
                iCountErrors++;
                printerr( "Error_0002! Expected exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0003! Unexpected exceptiont thrown: "+exc.ToString());
            }
                        
            // [] With an empty string.
            iCountTestcases++;
            try
            {
                File.SetLastWriteTime("", DateTime.Today);
                iCountErrors++;
                printerr("Error_0004! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr( "Error_0005! Unexpected exceptiont thrown: "+exc.ToString());
            }                      
            
            // [] Valid file name and datetime(Today)
            strLoc = "Loc_0006";
            FileInfo file2 = new FileInfo(fileName);
            FileStream fs2 = file2.Create();
            fs2.Dispose();                        
                        iCountTestcases++;
            try {
                DateTime today = DateTime.Today;
                File.SetLastWriteTime(fileName, today) ;
                if (File.GetLastWriteTime(fileName) != today) {
                    iCountErrors++;
                    printerr( "Error_0007! Creation time cannot be correct");
                }
            } catch (Exception exc) {
                iCountErrors++;
                printerr( "Error_0008! Unexpected exceptiont thrown: "+exc.ToString());
            }
            file2.Delete();

            //Add one year from DateTime.today.
            strLoc = "Loc_0009";
                        
            file2 = new FileInfo(fileName);
            fs2 = file2.Create();
            fs2.Dispose();                        
            iCountTestcases++;
            try
            {
                DateTime now = DateTime.Now;
                now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
                File.SetLastWriteTime(fileName , now.AddYears(1)) ;
                if (File.GetLastWriteTime(fileName) != now.AddYears(1))
                {
                    iCountErrors++;
                    Console.WriteLine(" POINTTOBREAK !!!Error_0013! Creation time cannot be correct.. Expected...{0}, ...Actual...{1}...Seconds difference...{2}",File.GetLastWriteTime(fileName),DateTime.Now.AddYears(1),(File.GetLastWriteTime(fileName) - DateTime.Now.AddYears(1)).Seconds  );
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0011! Unexpected exceptiont thrown: " + exc.ToString());
            }
            file2.Delete();

            //Subtract one year from DateTime.today.
            strLoc = "Loc_0012";
                        
            file2 = new FileInfo(fileName);
            fs2 = file2.Create();
            fs2.Dispose();                        
            iCountTestcases++;
            try
            {
                DateTime now = DateTime.Now;
                now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
                File.SetLastWriteTime(fileName, now.AddYears(-1));
                if (File.GetLastWriteTime(fileName) != now.AddYears(-1))
                {
                    iCountErrors++;
                    Console.WriteLine(" POINTTOBREAK !!!Error_0013! Creation time cannot be correct.. Expected...{0}, ...Actual...{1}...Seconds difference...{2}",File.GetLastWriteTime(fileName),DateTime.Now.AddYears(-1),(File.GetLastWriteTime(fileName) - DateTime.Now.AddYears(-1)).Seconds  );
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0014! Unexpected exceptiont thrown: " + exc.ToString());
            }
            file2.Delete();

            //Add one month from DateTime.today.
            strLoc = "Loc_0015";
                        
            file2 = new FileInfo(fileName);
            fs2 = file2.Create();
            fs2.Dispose();                        
            iCountTestcases++;
            try
            {
                DateTime now = DateTime.Now;
                now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
                File.SetLastWriteTime(fileName, now.AddMonths(1));
                if (File.GetLastWriteTime(fileName) != now.AddMonths(1))
                {
                    Console.WriteLine(DateTime.Now.AddMonths(1));
                
                    Console.WriteLine(" POINTTOBREAK !!!Error_0013! Creation time cannot be correct.. Expected...{0}, ...Actual...{1}...Seconds difference...{2}",File.GetLastWriteTime(fileName),DateTime.Now.AddMonths(1),(File.GetLastWriteTime(fileName) - DateTime.Now.AddMonths(1)).Seconds  );
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0017! Unexpected exceptiont thrown: " + exc.ToString());
            }
            file2.Delete();

            //Subtract one month from DateTime.today.
            strLoc = "Loc_0018";
                        
            file2 = new FileInfo(fileName);
            fs2 = file2.Create();
            fs2.Dispose();                        
            iCountTestcases++;
            try
            {
                DateTime now = DateTime.Now;
                now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
                File.SetLastWriteTime(fileName, now.AddMonths(-1));
                if (File.GetLastWriteTime(fileName) != now.AddMonths(-1))
                {
                    iCountErrors++;
                    Console.WriteLine(" POINTTOBREAK !!!Error_0013! Creation time cannot be correct.. Expected...{0}, ...Actual...{1}...Seconds difference...{2}",File.GetLastWriteTime(fileName),DateTime.Now.AddMonths(-1),(File.GetLastWriteTime(fileName) - DateTime.Now.AddMonths(-1)).Seconds  );
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0020! Unexpected exceptiont thrown: " + exc.ToString());
            }
            file2.Delete();

            //Add one day from DateTime.today.
            strLoc = "Loc_0021";
                        
            file2 = new FileInfo(fileName);
            fs2 = file2.Create();
            fs2.Dispose();                        
            iCountTestcases++;
            try
            {
                DateTime now = DateTime.Now;
                now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
                File.SetLastWriteTime(fileName, now.AddDays(1));
                if (File.GetLastWriteTime(fileName) != now.AddDays(1))
                {
                    iCountErrors++;
                    Console.WriteLine(" POINTTOBREAK !!!Error_0013! Creation time cannot be correct.. Expected...{0}, ...Actual...{1}...Seconds difference...{2}",File.GetLastWriteTime(fileName),DateTime.Now.AddDays(1),(File.GetLastWriteTime(fileName) - DateTime.Now.AddDays(-1)).Seconds  );
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0023! Unexpected exceptiont thrown: " + exc.ToString());
            }
            file2.Delete();

            //Subtract one day from DateTime.today.
            strLoc = "Loc_0024";
                        
            file2 = new FileInfo(fileName);
            fs2 = file2.Create();
            fs2.Dispose();                        
            iCountTestcases++;
            try
            {
                DateTime now = DateTime.Now;
                now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
                File.SetLastWriteTime(fileName, now.AddDays(-1));
                if (File.GetLastWriteTime(fileName) != now.AddDays(-1))
                {
                    iCountErrors++;
                    Console.WriteLine(" POINTTOBREAK !!!Error_0013! Creation time cannot be correct.. Expected...{0}, ...Actual...{1}...Seconds difference...{2}",File.GetLastWriteTime(fileName),DateTime.Now.AddDays(-1),(File.GetLastWriteTime(fileName) - DateTime.Now.AddDays(-1)).Seconds  );
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0026! Unexpected exceptiont thrown: " + exc.ToString());
            }
            file2.Delete();
            
            //With invalid datetime object.
            strLoc = "Loc_0025";
                        
            file2 = new FileInfo(fileName);
            fs2 = file2.Create();
            fs2.Dispose();                        
            iCountTestcases++;
            try
            {
                File.SetLastWriteTime(fileName, new DateTime(2001, 332, 20, 50, 50, 50));
                iCountErrors++;
                printerr( "Error_0026! Creation time cannot be correct");
            }
            catch (ArgumentOutOfRangeException)
            {
            } catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0027! Unexpected exceptiont thrown: " + exc.ToString());
            }
            file2.Delete();

            //With valid date and time. 
            strLoc = "Loc_0028";
                        
            file2 = new FileInfo(fileName);
            fs2 = file2.Create();
            fs2.Dispose();                        
            iCountTestcases++;
            try
            {
                DateTime dt = new DateTime(2001, 2, 2, 20, 20, 20);
                File.SetLastWriteTime(fileName, dt );
                if((File.GetLastWriteTime(fileName) - dt ).Seconds > 0)
                {
                    iCountErrors++;
                    printerr( "Error_0029! Creation time cannot be correct");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0030! Unexpected exceptiont thrown: " + exc.ToString());
            }
            file2.Delete();
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

