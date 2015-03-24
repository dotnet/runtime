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

public class File_Open_str_fm
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2001/02/02 10;00";
    public static String s_strClassMethod = "File.Open(String,FileMode)";
    public static String s_strTFName = "Open_str_fm.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();
    private static String s_strLoc = "Loc_0001";
    private static int s_iCountErrors = 0;
    private static int s_iCountTestcases = 0;

    [Fact]
    public static void runTest()
    {
        try
        {
            // CreateNew
            // Create
            // Open
            // OpenOrCreate
            // Truncate
            // Append

            // Simple call throughs to FileStream, just test functionality
            // [] FileMode.CreateNew
            // [][] File Exists
            // [][] File does not exist
            // [] FileMode.Create
            // [][] File Exists
            // [][] File does not exist
            // [] FileMode.Open
            // [][] File Exists
            // [][] File does not exist
            // [] FileMode.OpenOrCreate
            // [][] File Exists
            // [][] File does not exist
            // [] FileMode.Truncate
            // [][] File Exists
            // [][] File does not exist
            // [] FileMode.Append
            // [][] File Exists
            // [][] File does not exist

            TestMethod(FileMode.CreateNew);
            TestMethod(FileMode.Create);
            TestMethod(FileMode.Open);
            TestMethod(FileMode.OpenOrCreate);
            TestMethod(FileMode.Truncate);
            TestMethod(FileMode.Append);
        }
        catch (Exception exc_general)
        {
            ++s_iCountErrors;
            Console.WriteLine("Error Err_8888yyy!  strLoc==" + s_strLoc + ", exc_general==" + exc_general.ToString());
        }
        ////  Finish Diagnostics

        if (s_iCountErrors != 0)
        {
            Console.WriteLine("FAiL! " + s_strTFName + " ,iCountErrors==" + s_iCountErrors.ToString());
        }

        Assert.Equal(0, s_iCountErrors);
    }

    public static void TestMethod(FileMode fm)
    {
        String fileName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
        FileInfo fil2;
        StreamWriter sw2;
        Stream fs2 = null;
        String str2;


        if (File.Exists(fileName))
            File.Delete(fileName);

        // [] File does not exist
        //------------------------------------------------------------------
        s_strLoc = "Loc_0001";

        fil2 = new FileInfo(fileName);
        switch (fm)
        {
            case FileMode.CreateNew:
            case FileMode.Create:
            case FileMode.OpenOrCreate:

                //With a null string
                s_iCountTestcases++;
                try
                {
                    fs2 = File.Open(null, fm);
                    if (!File.Exists(fileName))
                    {
                        s_iCountErrors++;
                        printerr("Error_0002! File not created, FileMode==" + fm.ToString());
                    }
                }
                catch (ArgumentNullException)
                {
                }
                catch (Exception ex)
                {
                    s_iCountErrors++;
                    printerr("Error_0003! Unexpected exception thrown :: " + ex.ToString());
                }

                //with anempty string
                s_iCountTestcases++;
                try
                {
                    fs2 = File.Open("", fm);
                    if (!File.Exists(fileName))
                    {
                        s_iCountErrors++;
                        printerr("Error_0004! File not created, FileMode==" + fm.ToString());
                    }
                }
                catch (ArgumentException)
                {
                }
                catch (Exception ex)
                {
                    s_iCountErrors++;
                    printerr("Error_0005! Unexpected exception thrown :: " + ex.ToString());
                }

                fs2 = File.Open(fileName, fm);
                s_iCountTestcases++;
                if (!File.Exists(fileName))
                {
                    s_iCountErrors++;
                    printerr("Error_0006! File not created, FileMode==" + fm.ToString());
                }
                fs2.Dispose();
                break;
            case FileMode.Open:
            case FileMode.Truncate:
                s_iCountTestcases++;
                try
                {
                    fs2 = File.Open(fileName, fm);
                    s_iCountErrors++;
                    printerr("Error_0007! Expected exception not thrown");
                    fs2.Dispose();
                }
                catch (FileNotFoundException)
                {
                }
                catch (Exception exc)
                {
                    s_iCountErrors++;
                    printerr("Error_0009! Incorrect exception thrown, exc==" + exc.ToString());
                }
                break;
            case FileMode.Append:
                s_iCountTestcases++;
                try
                {
                    fs2 = File.Open(fileName, fm);
                    fs2.Write(new Byte[] { 54, 65, 54, 90 }, 0, 4);
                    if (fs2.Length != 4)
                    {
                        s_iCountErrors++;
                        Console.WriteLine("Unexpected file length .... " + fs2.Length);
                    }
                    fs2.Dispose();
                }
                catch (Exception exc)
                {
                    s_iCountErrors++;
                    printerr("Error_0012! Incorrect exception thrown, exc==" + exc.ToString());
                }
                break;
            default:
                s_iCountErrors++;
                printerr("Error_27tbv! This should not be....");
                break;
        }
        if (File.Exists(fileName))
            File.Delete(fileName);

        //------------------------------------------------------------------


        // [] File already exists
        //------------------------------------------------------------------
        s_strLoc = "Loc_4yg7b";

        FileStream stream = new FileStream(fileName, FileMode.Create);
        sw2 = new StreamWriter(stream);
        str2 = "Du er en ape";
        sw2.Write(str2);
        sw2.Dispose();
        stream.Dispose();
        switch (fm)
        {
            case FileMode.CreateNew:
                s_iCountTestcases++;
                try
                {
                    fs2 = File.Open(fileName, fm);
                    s_iCountErrors++;
                    printerr("Error_27b98! Expected exception not thrown");
                    fs2.Dispose();
                }
                catch (IOException)
                {
                }
                catch (Exception exc)
                {
                    s_iCountErrors++;
                    printerr("Error_g8782! Incorrect exception thrown, exc==" + exc.ToString());
                }
                break;
            case FileMode.Create:
                fs2 = File.Open(fileName, fm);
                if (fs2.Length != 0)
                {
                    s_iCountErrors++;
                    printerr("Error_287vb! Incorrect length of file==" + fil2.Length);
                }
                fs2.Dispose();
                break;
            case FileMode.OpenOrCreate:
            case FileMode.Open:
                fs2 = File.Open(fileName, fm);
                if (fs2.Length != str2.Length)
                {
                    s_iCountErrors++;
                    printerr("Error_2gy78! Incorrect length on file==" + fil2.Length);
                }
                fs2.Dispose();
                break;
            case FileMode.Truncate:
                fs2 = File.Open(fileName, fm);
                if (fs2.Length != 0)
                {
                    s_iCountErrors++;
                    printerr("Error_29gv9! Incorrect length on file==" + fil2.Length);
                }
                fs2.Dispose();
                break;
            case FileMode.Append:
                s_iCountTestcases++;
                try
                {
                    fs2 = File.Open(fileName, fm);
                    fs2.Write(new Byte[] { 54, 65, 54, 90 }, 0, 4);
                    if (fs2.Length != 16)
                    {  // already 12 characters are written to the file.
                        s_iCountErrors++;
                        Console.WriteLine("Unexpected file length .... " + fs2.Length);
                    }
                    fs2.Dispose();
                }
                catch (Exception exc)
                {
                    s_iCountErrors++;
                    printerr("Error_27878! Incorrect exception thrown, exc==" + exc.ToString());
                }
                break;
            default:
                s_iCountErrors++;
                printerr("Error_587yb! This should not be...");
                break;
        }

        if (File.Exists(fileName))
            File.Delete(fileName);
    }

    public static void printerr(String err, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine("ERROR: ({0}, {1}, {2}) {3}", memberName, filePath, lineNumber, err);
    }
}

