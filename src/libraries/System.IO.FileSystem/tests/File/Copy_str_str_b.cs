// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections;
using System.Globalization;
using Xunit;

public class File_Copy_str_str_b
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/03/11 14:37";
    public static String s_strClassMethod = "File.CopyTo(String, Boolean)";
    public static String s_strTFName = "Copy_str_str_b.cool";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
    public static void runTest()
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        String filName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());

        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////

            FileInfo fil2 = null;
            FileInfo fil1 = null;
            Char[] cWriteArr, cReadArr;
            StreamWriter sw2;
            StreamReader sr2;
            FileStream fs2;

            try
            {
                new FileInfo(filName).Delete();
            }
            catch (Exception) { }
            // [] ArgumentNullException if null is passed in for source file

            strLoc = "Loc_498yg";



            File.Create(filName).Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            try
            {
                File.Copy(null, fil2.FullName, false);
                iCountErrors++;
                printerr("Error_209uz! Expected exception not thrown, fil2==" + fil1.FullName);
                fil1.Delete();
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
                File.Copy(fil2.FullName, null, false);
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


            // [] String.Empty should throw ArgumentException

            strLoc = "Loc_298vy";

            File.Create(filName).Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            try
            {
                File.Copy(fil2.FullName, String.Empty, false);
                iCountErrors++;
                printerr("Error_092u9! Expected exception not thrown, fil2==" + fil1.FullName);
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


            // [] Try to copy onto directory

            strLoc = "Loc_289vy";


            File.Create(filName).Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            try
            {
                File.Copy(fil2.FullName, TestInfo.CurrentDirectory, false);
                iCountErrors++;
                printerr("Error_301ju! Expected exception not thrown, fil2==" + fil1.FullName);
            }
            catch (IOException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_23r78af! Incorrect exception thrown, exc==" + exc.ToString());
            }
            fil2.Delete();


            // [] Copy onto itself should fail


            strLoc = "Loc_r7yd9";


            File.Create(filName).Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            try
            {
                File.Copy(fil2.FullName, fil2.FullName, false);
                iCountErrors++;
                printerr("Error_2387ag! Expected exception not thrown, fil2==" + fil1.FullName);
            }
            catch (IOException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_f588y! Unexpected exception thrown, exc==" + exc.ToString());
            }
            fil2.Delete();



            // [] Vanilla copy operation

            strLoc = "Loc_f548y";

            File.Create(filName).Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            string destFile = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            try
            {
                File.Copy(fil2.FullName, destFile, false);
                fil1 = new FileInfo(destFile);
                if (!File.Exists(fil1.FullName))
                {
                    iCountErrors++;
                    printerr("Error_2978y! File not copied");
                }
                if (!File.Exists(fil2.FullName))
                {
                    iCountErrors++;
                    printerr("Error_239vr! Source file gone");
                }
                fil1.Delete();
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_2987v! Unexpected exception, exc==" + exc.ToString());
            }
            fil2.Delete();
            fil1.Delete();

            // [] Filename with illiegal characters


            strLoc = "Loc_984hg";

            File.Create(filName).Dispose();
            fil2 = new FileInfo(filName);

            iCountTestcases++;
            try
            {
                File.Copy(fil2.FullName, "*\0*", false);
                iCountErrors++;
                printerr("Error_298xh! Expected exception not thrown, fil2==" + fil1.FullName);
                fil1.Delete();
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


            // [] Move a file containing data


            strLoc = "Loc_f888m";

            try
            {
                new FileInfo(destFile).Delete();
            }
            catch (Exception) { }
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
                File.Copy(fil2.FullName, destFile, false);
                fil1 = new FileInfo(destFile);
                iCountTestcases++;
                if (!File.Exists(fil1.FullName))
                {
                    iCountErrors++;
                    printerr("Error_9u99s! File not copied correctly: " + fil1.FullName);
                }
                if (!File.Exists(fil2.FullName))
                {
                    iCountErrors++;
                    printerr("Error_29h7b! Source file gone");
                }

                FileStream fs = new FileStream(destFile, FileMode.Open);
                sr2 = new StreamReader(fs);
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
                fs.Dispose();

                fil1.Delete();
                fil2.Delete();
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_28vc8! Unexpected exception, exc==" + exc.ToString());
            }

            /* Scenario disabled while porting because it accesses a file outside the test's working directory
            #if !TEST_WINRT  // Cannot access root
                        // [] Unecessary long but valid string

                        strLoc = "Loc_478yb";

                        File.Delete("\\TestFile.tmp");

                        File.Create(filName).Dispose();
                        fil2 = new FileInfo(filName);

                        File.Copy(fil2.Name, "..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\TestFile.tmp", false);
                        fil1 = new FileInfo(fil2.FullName.Substring(0, fil2.FullName.IndexOf("\\") + 1) + fil2.Name);
                        iCountTestcases++;
                        if (!fil1.FullName.Equals(fil2.FullName.Substring(0, fil2.FullName.IndexOf("\\") + 1) + fil2.Name))
                        {
                            Console.WriteLine(fil1.FullName);
                            iCountErrors++;
                            printerr("Error_298gc! Incorrect fullname set during copy");
                        }
                        new FileInfo(filName).Delete();
                        fil1.Delete();
                        File.Delete("\\TestFile.tmp");
            #endif
            */

            // [] Copy over a file that already exists

            strLoc = "Loc_37tgy";
            destFile = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());

            fs2 = new FileStream(filName, FileMode.Create);
            sw2 = new StreamWriter(fs2);
            for (Char i = (Char)65; i < (Char)75; i++)
                sw2.Write(i);
            sw2.Flush();
            sw2.Dispose();

            fs2 = new FileStream(destFile, FileMode.Create);
            sw2 = new StreamWriter(fs2);
            for (Char i = (Char)74; i >= (Char)65; i--)
                sw2.Write(i);
            sw2.Flush();
            sw2.Dispose();
            fil2 = new FileInfo(destFile);
            File.Copy(fil2.FullName, Path.ChangeExtension(destFile, ".tmp"), true);
            fil1 = new FileInfo(Path.ChangeExtension(destFile, ".tmp"));
            fil2.Delete();

            FileStream fs3 = new FileStream(filName, FileMode.Open);
            sr2 = new StreamReader(fs3);
            int tmp = 0;
            for (Char i = (Char)65; i < (Char)75; i++)
            {
                iCountTestcases++;
                if ((tmp = sr2.Read()) != i)
                {
                    iCountErrors++;
                    printerr("Error_498vy! Expected==" + i + ", got==" + tmp);
                }
            }
            sr2.Dispose();
            fs3.Dispose();
            fil2.Delete();
            fil1.Delete();
            File.Delete(destFile);
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

        FailSafeDirectoryOperations.DeleteDirectory(filName, true);
        Assert.Equal(0, iCountErrors);
    }

    public static void printerr(String err, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine("ERROR: ({0}, {1}, {2}) {3}", memberName, filePath, lineNumber, err);
    }
}

