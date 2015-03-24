// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Linq;
using Xunit;

public class FileInfo_MoveTo_str
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/05/10 11:30";
    public static String s_strClassMethod = "File.MoveTo(String)";
    public static String s_strTFName = "MoveTo_str.cs";
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
                fil2.MoveTo(null);
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

            //-----------------------------------------------------------------


            // [] String.Empty should throw ArgumentException
            //-----------------------------------------------------------------

            strLoc = "Loc_298vy";

            File.Create(filName).Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            try
            {
                fil2.MoveTo(String.Empty);
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
                fil2.MoveTo(TestInfo.CurrentDirectory);
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


            // [] Move onto itself should fail

            //-----------------------------------------------------------------

            strLoc = "Loc_r7yd9";


            File.Create(filName).Dispose();
            fil2 = new FileInfo(filName);
            iCountTestcases++;
            fil2.MoveTo(fil2.FullName);
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
            String filNameDest = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            fil2.MoveTo(filNameDest);
            fil1 = new FileInfo(filNameDest);
            fil2.Refresh();
            if (File.Exists(filName))
            {
                iCountErrors++;
                printerr("Error_2978y! File not copied");
            }
            if (!File.Exists(fil2.FullName))
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
                fil2.MoveTo("**");
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
            filName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            filNameDest = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            File.Delete(filNameDest);
            sw2 = new StreamWriter(File.Create(filName));
            cWriteArr = new Char[26];
            int j = 0;
            for (Char i = 'A'; i <= 'Z'; i++)
                cWriteArr[j++] = i;
            sw2.Write(cWriteArr, 0, cWriteArr.Length);
            sw2.Flush();
            sw2.Dispose();

            fil2 = new FileInfo(filName);
            fil2.MoveTo(filNameDest);
            fil1 = new FileInfo(filName);
            iCountTestcases++;
            if (fil1.Exists)
            {
                iCountErrors++;
                printerr("Error_9u99s! File not moved: " + fil1.FullName);
            }
            iCountTestcases++;
            if (!fil2.FullName.Equals(filNameDest))
            {
                iCountErrors++;
                printerr("Error_2488g! Incorrect name==" + fil2.FullName);
            }
            if (!File.Exists(fil2.FullName))
            {
                iCountErrors++;
                printerr("Error_29h7b! Source still there");
            }

            FileStream stream = new FileStream(filNameDest, FileMode.Open);
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

            //-----------------------------------------------------------------




            // [] Interesting case:
            //-----------------------------------------------------------------

            strLoc = "Loc_478yb";
            filName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            filNameDest = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            if (File.Exists(filNameDest))
                File.Delete(filNameDest);

            new FileStream(filName, FileMode.Create).Dispose();
            fil2 = new FileInfo(filName);
            string pathDest = Path.Combine(Path.GetDirectoryName(filNameDest), Path.Combine(Enumerable.Repeat(".", 125).Concat(new string[] { Path.GetFileName(filNameDest) }).ToArray()));

            fil2.MoveTo(pathDest);

            fil1 = new FileInfo(filNameDest);
            iCountTestcases++;
            if (!fil1.Exists)
            {
                iCountErrors++;
                printerr("Error_hs84fs! File does not exist in expected destination after move");
            }
            if (!fil2.FullName.Equals(fil1.FullName))
            {
                iCountErrors++;
                printerr(string.Format("Error_298gc! Incorrect fullname set during Move (expected {0}, actual {1}",
                    fil1.FullName, fil2.FullName));
            }
            new FileInfo(filName).Delete();
            File.Delete(filNameDest);
            //-----------------------------------------------------------------

            fil1.Delete();
            fil2.Delete();





            if (File.Exists(filName))
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

        Assert.Equal(0, iCountErrors);
    }

    public static void printerr(String err, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine("ERROR: ({0}, {1}, {2}) {3}", memberName, filePath, lineNumber, err);
    }
}

