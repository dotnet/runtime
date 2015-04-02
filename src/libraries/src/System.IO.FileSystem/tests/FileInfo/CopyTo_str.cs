// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections;
using System.Globalization;
using Xunit;

public class FileInfo_CopyTo_str
{
    public static String s_strActiveBugNums = "16134";
    public static String s_strDtTmVer = "2000/03/11 14:37";
    public static String s_strClassMethod = "File.CopyTo(String)";
    public static String s_strTFName = "CopyTo_str.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
    public static void runTest()
    {
        //////////// Global Variables used for all tests
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;


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
            String filName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            String testFilName;

            try
            {
                new FileInfo(filName).Delete();
            }
            catch (Exception) { }
            // [] ArgumentNullException if null is passed in

            strLoc = "Loc_498yg";

            new FileStream(filName, FileMode.Create).Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            try
            {
                fil1 = fil2.CopyTo(null);
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


            // [] String.Empty should throw ArgumentException

            strLoc = "Loc_298vy";

            FileStream fs = new FileStream(filName, FileMode.Create);
            fs.Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            try
            {
                fil1 = fil2.CopyTo(String.Empty);
                iCountErrors++;
                printerr("Error_092u9! Expected exception not thrown, fil2==" + fil1.FullName);
                fil1.Delete();
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

            new FileStream(filName, FileMode.Create).Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            try
            {
                fil1 = fil2.CopyTo(TestInfo.CurrentDirectory);
                iCountErrors++;
                printerr("Error_301ju! Expected exception not thrown, fil2==" + fil1.FullName);
                fil1.Delete();
            }
            catch (IOException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_545345! Incorrect exception thrown, exc==" + exc.ToString());
            }
            fil2.Delete();


            // [] Copy onto itself should fail


            strLoc = "Loc_r7yd9";

            new FileStream(filName, FileMode.Create).Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            try
            {
                fil1 = fil2.CopyTo(fil2.FullName);
                iCountErrors++;
                printerr("Error_54534! Expected exception not thrown, fil2==" + fil1.FullName);
                fil1.Delete();
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
            fs2 = new FileStream(filName, FileMode.Create);
            fs2.Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            try
            {
                String tempFileName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());

                fil1 = fil2.CopyTo(tempFileName);
                if (!fil1.Exists)
                {
                    iCountErrors++;
                    printerr("Error_2978y! File not copied");
                }

                if (!fil2.Exists)
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
            if (fil2.Exists)
                fil2.Delete();

            // [] Filename with illiegal characters


            strLoc = "Loc_984hg";

            fs2 = new FileStream(filName, FileMode.Create);
            fs2.Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            try
            {
                fil1 = fil2.CopyTo("**");
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


            strLoc = "Loc_t98ck";

            try
            {
                testFilName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
                File.Delete(testFilName);
                FileStream fs4 = new FileStream(filName, FileMode.Append);
                sw2 = new StreamWriter(fs4);
                cWriteArr = new Char[26];
                int j = 0;
                for (Char i = 'A'; i <= 'Z'; i++)
                    cWriteArr[j++] = i;
                sw2.Write(cWriteArr, 0, cWriteArr.Length);
                sw2.Flush();
                sw2.Dispose();
                fs4.Dispose();

                fil2 = new FileInfo(filName);
                fil1 = fil2.CopyTo(testFilName);
                iCountTestcases++;
                if (!fil1.Exists)
                {
                    iCountErrors++;
                    printerr("Error_9u99s! File not copied correctly: " + fil1.FullName);
                }
                if (!fil2.Exists)
                {
                    iCountErrors++;
                    printerr("Error_29h7b! Source file gone");
                }
                FileStream fs3 = new FileStream(testFilName, FileMode.Open);
                sr2 = new StreamReader(fs3);
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
                fs3.Dispose();

                fil1.Delete();
                fil2.Delete();
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_28vc8! Unexpected exception, exc==" + exc.ToString());
            }

            // [] Unecessary long (over MAX_PATH) but valid directory name
            strLoc = "Loc_48ygv";
            testFilName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            fs2 = new FileStream(filName, FileMode.Create);
            fs2.Dispose();
            fil2 = new FileInfo(filName);
            File.Delete(testFilName);
            fil1 = fil2.CopyTo(Path.GetDirectoryName(testFilName) + string.Concat(Enumerable.Repeat(Path.DirectorySeparatorChar + ".", 90).ToArray()) + Path.DirectorySeparatorChar + Path.GetFileName(testFilName));
            iCountTestcases++;
            if (!fil1.FullName.Equals(testFilName))
            {
                Console.WriteLine(fil1.FullName);
                Console.WriteLine(fil2.FullName);
                iCountErrors++;
                printerr("Error_298gc! Incorrect fullname set during copy");
            }
            fil1.Delete();
            new FileInfo(filName).Delete();

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

