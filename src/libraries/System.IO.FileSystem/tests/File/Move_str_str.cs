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

public class File_Move_str_str
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/05/08 12:18";
    public static String s_strClassMethod = "File.OpenText(String)";
    public static String s_strTFName = "Move_str_str.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
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
            FileInfo fil1, fil2;
            StreamWriter sw2;
            Char[] cWriteArr, cReadArr;
            StreamReader sr2;

            if (File.Exists(filName))
                File.Delete(filName);


            // [] Exception for null arguments
            //-----------------------------------------------------------------

            strLoc = "Loc_498yg";

            File.Create(filName).Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            try
            {
                File.Move(null, fil2.FullName);
                iCountErrors++;
                printerr("Error_209uz! Expected exception not thrown, fil2==" + fil2.FullName);
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_21x99! Incorrect exception thrown, exc==" + exc.ToString());
            }
            fil2.Delete();

            // [] ArgumentNullException if null is passed in for destination file

            strLoc = "Loc_898gc";

            File.Create(filName).Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            try
            {
                File.Move(fil2.FullName, null);
                iCountErrors++;
                printerr("Error_8yt85! Expected exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_1298s! Incorrect exception thrown, exc==" + exc.ToString());
            }

            //-----------------------------------------------------------------


            // [] String.Empty should throw ArgumentException
            //-----------------------------------------------------------------

            strLoc = "Loc_298vy";

            File.Create(filName).Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            try
            {
                File.Move(fil2.FullName, String.Empty);
                iCountErrors++;
                printerr("Error_092u9! Expected exception not thrown, fil2==" + fil2.FullName);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_109uc! Incorrect exception thrown, exc==" + exc.ToString());
            }
            fil2.Delete();
            //-----------------------------------------------------------------


            // [] Try to Move onto directory
            //-----------------------------------------------------------------

            strLoc = "Loc_289vy";


            File.Create(filName).Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            try
            {
                File.Move(fil2.FullName, TestInfo.CurrentDirectory);
                iCountErrors++;
                printerr("Error_301ju! Expected exception not thrown, fil2==" + fil2.FullName);
            }
            catch (IOException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_209us! Incorrect exception thrown, exc==" + exc.ToString());
            }
            fil2.Delete();
            //-----------------------------------------------------------------


            // [] Move onto existing file should fail

            //-----------------------------------------------------------------

            strLoc = "Loc_r7yd9";


            File.Create(filName).Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            File.Move(fil2.FullName, fil2.FullName);
            if (!File.Exists(fil2.FullName))
            {
                iCountErrors++;
                printerr("Error_r8g7b! Expected exception not thrown");
            }
            fil2.Delete();
            //-----------------------------------------------------------------

            // [] Vanilla Move operation

            //-----------------------------------------------------------------
            strLoc = "Loc_f548y";

            File.Create(filName).Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            string destFile = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            File.Move(fil2.FullName, destFile);
            fil1 = new FileInfo(destFile);
            fil2.Refresh();
            if (!File.Exists(fil1.FullName))
            {
                iCountErrors++;
                printerr("Error_2978y! File not copied");
            }
            if (File.Exists(fil2.FullName))
            {
                iCountErrors++;
                printerr("Error_239vr! Source file still there");
            }
            fil1.Delete();
            fil2.Delete();
            //-----------------------------------------------------------------


            // [] Filename with illiegal characters
            //-----------------------------------------------------------------
            strLoc = "Loc_984hg";

            File.Create(filName).Dispose();
            fil2 = new FileInfo(filName);

            iCountTestcases++;
            try
            {
                File.Move(fil2.FullName, "**");
                iCountErrors++;
                printerr("Error_298xh! Expected exception not thrown, fil2==" + fil2.FullName);
                fil2.Delete();
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_2091s! Incorrect exception thrown, exc==" + exc.ToString());
            }
            fil2.Delete();
            //-----------------------------------------------------------------


            // [] Move a file containing data
            //-----------------------------------------------------------------

            strLoc = "Loc_f888m";

            destFile = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            File.Delete(destFile);
            try
            {
                sw2 = new StreamWriter(File.Create(filName));
                cWriteArr = new Char[26];
                int j = 0;
                for (Char i = 'A'; i <= 'Z'; i++)
                    cWriteArr[j++] = i;
                sw2.Write(cWriteArr, 0, cWriteArr.Length);
                sw2.Flush();
                sw2.Dispose();

                fil2 = new FileInfo(filName);
                File.Move(fil2.FullName, destFile);
                fil1 = new FileInfo(destFile);
                fil2.Refresh();
                iCountTestcases++;
                if (!File.Exists(fil1.FullName))
                {
                    iCountErrors++;
                    printerr("Error_9u99s! File not copied correctly: " + fil1.FullName);
                }
                if (File.Exists(fil2.FullName))
                {
                    iCountErrors++;
                    printerr("Error_29h7b! Source file gone");
                }

                FileStream stream = new FileStream(destFile, FileMode.Open);
                sr2 = new StreamReader(stream);
                cReadArr = new Char[cWriteArr.Length];
                sr2.Read(cReadArr, 0, cReadArr.Length);

                iCountTestcases++;
                for (int i = 0; i < cReadArr.Length; i++)
                {
                    iCountTestcases++;
                    if (cReadArr[i] != cWriteArr[i])
                    {
                        iCountErrors++;
                        printerr("Error_98yv7! Expected==" + cWriteArr[i] + ", got value==" + cReadArr[i]);
                    }
                }
                sr2.Dispose();
                stream.Dispose();

                fil1.Delete();
                fil2.Delete();
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_28vc8! Unexpected exception, exc==" + exc.ToString());
            }
            //-----------------------------------------------------------------


/* Scenario disabled when porting because it accesses the file system outside of the test's working directory
#if !TEST_WINRT
            // [] Move up to root dir
            //-----------------------------------------------------------------

            strLoc = "Loc_478yb";
            String strTmp = "..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\TestFile.tmp";
            File.Delete(strTmp);

            new FileStream(filName, FileMode.Create).Dispose();
            fil2 = new FileInfo(filName);

            File.Move(fil2.Name, strTmp);
            fil1 = new FileInfo(fil2.FullName.Substring(0, fil2.FullName.IndexOf("\\") + 1) + fil2.Name);
            iCountTestcases++;
            if (!fil1.FullName.Equals(fil2.FullName.Substring(0, fil2.FullName.IndexOf("\\") + 1) + fil2.Name))
            {
                Console.WriteLine(fil1.FullName);
                iCountErrors++;
                printerr("Error_298gc! Incorrect fullname set during Move");
            }
            new FileInfo(filName).Delete();
            fil1.Delete();
            //-----------------------------------------------------------------

            fil1.Delete();
            fil2.Delete();

            if (File.Exists(strTmp))
                File.Delete(strTmp);

            if (File.Exists(filName))
                File.Delete(filName);
#endif
*/

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

