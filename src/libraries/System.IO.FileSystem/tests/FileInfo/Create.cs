// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using Xunit;

public class FileInfo_Create
{
    public static String s_strDtTmVer = "2009/02/18";
    public static String s_strClassMethod = "FileInfo.Create";
    public static String s_strTFName = "Create.cs";
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
            FileStream fs;
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

            iCountTestcases++;
            fileName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            file2 = new FileInfo(fileName);
            fs = file2.Create();
            fs.Dispose();
            file2 = new FileInfo(fileName);
            try
            {
                fs = file2.Create();
                fs.Dispose();
                file2.Delete();
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_6566! Incorrect exception thrown, exc==" + exc.ToString());
            }

            // [] Pass valid File name

            strLoc = "Loc_498vy";

            iCountTestcases++;
            fileName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName()); ;
            file2 = new FileInfo(fileName);
            try
            {
                fs = file2.Create();
                if (file2.FullName != fileName)
                {
                    iCountErrors++;
                    printerr("Error_0010! Unexpected File name :: " + file2.FullName);
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
            fileName = Path.Combine(TestInfo.CurrentDirectory, "abc", "..", Path.GetRandomFileName());
            file2 = new FileInfo(fileName);
            try
            {
                fs = file2.Create();
                fs.Dispose();
                file2.Delete();
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0980! Incorrect exception thrown, exc==" + exc.ToString());
            }

            // [] Create a file with symbols.

            strLoc = "Loc_87yg7";

            iCountTestcases++;
            fileName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName() + "!@#$%^&");
            file2 = new FileInfo(fileName);
            fs = file2.Create();
            fs.Dispose();
            if (!file2.FullName.Equals(fileName))
            {
                iCountErrors++;
                printerr("Error_0109x! Incorrect File name, file2==" + file2.FullName);
            }
            file2.Delete();

            // [] Create a file in nested sub directory

            strLoc = "Loc_209ud";

            iCountTestcases++;
            fileName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            file2 = new FileInfo(Path.Combine(TestInfo.CurrentDirectory, "Test", "Test", "Test", fileName));
            try
            {
                file2.Create();
                if (file2.FullName.IndexOf(Path.Combine("Test", "Test", "Test", fileName)) == -1)
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

            // [] Try invalid File name

            strLoc = "Loc_2089x";

            iCountTestcases++;
            try
            {
                file2 = new FileInfo("\0");
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

            // [] Pass nested level File structure.

            strLoc = "Loc_098gt";

            iCountTestcases++;
            fileName = Path.GetRandomFileName();
            file2 = new FileInfo(Path.Combine(TestInfo.CurrentDirectory, "abc", "xyz", "..", "..", fileName));
            try
            {
                fs = file2.Create();
                fs.Dispose();
                file2.Delete();
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_9092c! Unexpected exception thrown, exc==" + exc.ToString());
            }

            /* Test disabled because it modifies state outside the current working directory
            // [] Create a file in root
            strLoc = "Loc_89ytb";

            String CurrentDirectory2 = Directory.GetCurrentDirectory();
            fileName = Path.GetRandomFileName();
            String tempPath = CurrentDirectory2.Substring(0, CurrentDirectory2.IndexOf("\\") + 1) + fileName;
            if (!File.Exists(tempPath) && !Directory.Exists(tempPath))
            {
                file2 = new FileInfo(tempPath);
                fs = file2.Create();
                fs.Dispose();
                iCountTestcases++;
                if (!file2.Exists)
                {
                    iCountErrors++;
                    printerr("Error_t78g7! File not created, file==" + file2.FullName);
                }
                file2.Delete();
            }
            */

            if (!Interop.IsLinux) // testing case insensitivity
            {
                // [] Create file in current file2 by giving full File check casing as well
                strLoc = "loc_89tbh";
                fileName = Path.GetRandomFileName();
                file2 = new FileInfo(Path.Combine(TestInfo.CurrentDirectory.ToLowerInvariant(), fileName));
                fs = file2.Create();
                fs.Dispose();
                iCountTestcases++;
                if (!file2.Exists)
                {
                    iCountErrors++;
                    printerr("Error_t87gy! File not created, file==" + file2.FullName);
                }
                file2.Delete();

                strLoc = "loc_89mjd";
                fileName = Path.GetRandomFileName();
                file2 = new FileInfo(Path.Combine(TestInfo.CurrentDirectory.ToUpperInvariant(), fileName));
                fs = file2.Create();
                fs.Dispose();
                iCountTestcases++;
                if (!file2.Exists)
                {
                    iCountErrors++;
                    printerr("Error_hf3t4! File not created, file==" + file2.FullName);
                }
                file2.Delete();
            }
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

