// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text;
using Xunit;

public class FileInfo_Create_str
{
    public static String s_strDtTmVer = "2009/02/18";
    public static String s_strClassMethod = "FileInfo.Create";
    public static String s_strTFName = "Create_str.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
    public static void runTest()
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";

        try
        {
            FileInfo file2 = null;
            Stream fs2;
            String fileName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());

            try
            {
                new FileInfo(fileName).Delete();
            }
            catch (Exception) { }

            strLoc = "Loc_099u8";

            // [] Create a File with '.'.
            iCountTestcases++;
            file2 = new FileInfo(".");
            try
            {
                file2.Create();
                iCountErrors++;
                printerr("Error_298dy! Expected exception not thrown, file2==" + file2.FullName);
                file2.Delete();
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_209xj! Incorrect exception thrown, exc==" + exc.ToString());
            }

            // [] Try to create an existing file.
            strLoc = "Loc_209xc";

            //Create a file.
            String testDir = Path.Combine(TestInfo.CurrentDirectory, "TestDir");
            Directory.CreateDirectory(testDir);
            fileName = Path.Combine(testDir, "foobar.cs");
            FileStream fstream = File.Create(fileName);
            fstream.Dispose();

            iCountTestcases++;

            file2 = new FileInfo(fileName);
            try
            {
                FileStream fs = file2.Create();
                fs.Dispose();
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_6566! Incorrect exception thrown, exc==" + exc.ToString());
            }
            file2.Delete();
            Directory.Delete(testDir);

            // [] Pass valid File name

            strLoc = "Loc_498vy";

            iCountTestcases++;
            fileName = Path.Combine(TestInfo.CurrentDirectory, "UniqueFileName_28829");
            file2 = new FileInfo(fileName);
            try
            {
                FileStream fs = file2.Create();
                if (file2.Name != Path.GetFileName(fileName))
                {
                    iCountErrors++;
                    printerr("Error_0010! Unexpected File name :: " + file2.Name);
                }
                fs.Dispose();
                file2.Delete();
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_28829! Unexpected exception thrown, exc==" + exc.ToString());
            }


            // [] Try to create the file in the parent directory

            strLoc = "Loc_09t83";

            iCountTestcases++;
            file2 = new FileInfo(Path.Combine(TestInfo.CurrentDirectory, "abc", "..", "Test.txt"));
            try
            {
                FileStream fs = file2.Create();
                fs.Dispose();
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0980! Incorrect exception thrown, exc==" + exc.ToString());
            }
            file2.Delete();


            // [] Create a long filename File

            strLoc = "Loc_2908y";

            StringBuilder sb = new StringBuilder(TestInfo.CurrentDirectory);
            while (sb.Length < 260)
                sb.Append("a");

            iCountTestcases++;
            try
            {
                file2 = new FileInfo(sb.ToString());
                file2.Create();
                file2.Delete();
                iCountErrors++;
                printerr("Error_109ty! Expected exception not thrown, file2==" + file2.FullName);
            }
            catch (PathTooLongException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_109dv! Incorrect exception thrown, exc==" + exc.ToString());
            }

            // [] Do some symbols

            strLoc = "Loc_87yg7";

            fileName = Path.Combine(TestInfo.CurrentDirectory, "!@#$%^&");
            iCountTestcases++;
            try
            {
                file2 = new FileInfo(fileName);
                FileStream fs = file2.Create();
                if (!file2.FullName.Equals(fileName))
                {
                    iCountErrors++;
                    printerr("Error_0109x! Incorrect File name, file2==" + file2.FullName);
                }
                fs.Dispose();
                file2.Delete();
            }
            catch (Exception e)
            {
                iCountErrors++;
                printerr("Unexpected exception occured... " + e.ToString());
            }

            // [] specifying subfile2ectories should fail unless they exist

            strLoc = "Loc_209ud";

            iCountTestcases++;
            file2 = new FileInfo(Path.Combine(TestInfo.CurrentDirectory, "Test", "Test", "Test", "test.cs"));
            try
            {
                file2.Create();
                if (file2.FullName.IndexOf(Path.Combine("Test", "Test", "Test")) == -1)
                {
                    iCountErrors++;
                    printerr("Error_0010! Unexpected File name :: " + file2.FullName);
                }
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_2019u! Incorrect exception thrown, exc==" + exc.ToString());
            }

            // [] Try incorrect File name

            strLoc = "Loc_2089x";

            iCountTestcases++;
            try
            {
                file2 = new FileInfo(":");
                file2.Create();
                iCountErrors++;
                printerr("Error_19883! Expected exception not thrown, file2==" + file2.FullName);
                file2.Delete();
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0198xu! Incorrect exception thrown, exc==" + exc.ToString());
            }

            // network path test moved to RemoteIOTests.cs

            // [] Pass nested level File structure.

            strLoc = "Loc_098gt";

            iCountTestcases++;
            file2 = new FileInfo(Path.Combine(TestInfo.CurrentDirectory, "abc", "xyz", "..", "..", "test.txt"));
            try
            {
                FileStream fs = file2.Create();
                fs.Dispose();
                file2.Delete();
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_9092c! Incorrect exception thrown, exc==" + exc.ToString());
            }

            /* Test disabled because it modifies state outside the current working directory
            // [] Create a file in root
            strLoc = "Loc_89ytb";

            try
            {
                String CurrentDirectory2 = Directory.GetCurrentDirectory();
                String tempPath = CurrentDirectory2.Substring(0, CurrentDirectory2.IndexOf("\\") + 1) + Path.GetFileName(fileName);

                if (!File.Exists(tempPath) && !Directory.Exists(tempPath))
                {
                    file2 = new FileInfo(tempPath);
                    FileStream fs = file2.Create();
                    fs.Dispose();
                    iCountTestcases++;
                    if (!file2.Exists)
                    {
                        iCountErrors++;
                        printerr("Error_t78g7! File not created, file==" + file2.FullName);
                    }
                    file2.Delete();
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_h38d9! Unexpected exception, exc==" + exc.ToString());
            }
            */

            // [] Create file in current file2 by giving full File check casing as well
            strLoc = "loc_89tbh";

            string fileName2 = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            fs2 = File.Create(fileName2);
            file2 = new FileInfo(Path.Combine(TestInfo.CurrentDirectory.ToLowerInvariant(), Path.GetFileNameWithoutExtension(fileName2).ToLowerInvariant() + Path.GetExtension(fileName2).ToUpperInvariant()));
            iCountTestcases++;
            if (!file2.Exists)
            {
                iCountErrors++;
                printerr("Error_t87gy! File not created, file==" + file2.FullName);
            }
            fs2.Dispose();
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

