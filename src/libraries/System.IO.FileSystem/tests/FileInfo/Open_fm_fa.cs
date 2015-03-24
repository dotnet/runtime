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

public class FileInfo_Open_fm_fa
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/05/08 12:18";
    public static String s_strClassMethod = "File.OpenText(String)";
    public static String s_strTFName = "Open_fm_fa.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();
    private static String s_strLoc = "Loc_000oo";
    private static int s_iCountErrors = 0;
    private static int s_iCountTestcases = 0;

    [Fact]
    public static void runTest()
    {
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////



            // CreateNew
            // Create
            // Open
            // OpenOrCreate
            // Truncate
            // Append

            // [] FileMode.CreateNew and FileAccess.Read
            // [][] File does not exist
            // [][] File already exists
            // [] FileMode.CreateNew and FileAccess.Write
            // [][] File does not exist
            // [][] File already exists
            // [] FileMode.CreateNew and FileAccess.ReadWrite
            // [][] File does not exist
            // [][] File already exists
            // [] FileMode.Create and FileAccess.Read
            // [][] File does not exist
            // [][] File already exists
            // [] FileMode.Create and FileAccess.Write
            // [][] File does not exist
            // [][] File already exists
            // [] FileMode.Create and FileAccess.ReadWrite
            // [][] File does not exist
            // [][] File already exists
            // [] FileMode.Open and FileAccess.Read
            // [][] File does not exist
            // [][] File already exists
            // [] FileMode.Open and FileAccess.Write
            // [][] File does not exist
            // [][] File already exists
            // [] FileMode.Open and FileAccess.ReadWrite
            // [][] File does not exist
            // [][] File already exists
            // [] FileMode.OpenOrCreate and FileAccess.Read
            // [][] File does not exist
            // [][] File already exists
            // [] FileMode.OpenOrCreate and FileAccess.Write
            // [][] File does not exist
            // [][] File already exists
            // [] FileMode.OpenOrCreate and FileAccess.ReadWrite
            // [][] File does not exist
            // [][] File already exists
            // [] FileMode.Truncate and FileAccess.Read
            // [][] File does not exist
            // [][] File already exists
            // [] FileMode.Truncate and FileAccess.Write
            // [][] File does not exist
            // [][] File already exists
            // [] FileMode.Truncate and FileAccess.ReadWrite
            // [][] File does not exist
            // [][] File already exists
            // [] FileMode.Append and FileAccess.Read
            // [][] File does not exist
            // [][] File already exists
            // [] FileMode.Append and FileAccess.Write
            // [][] File does not exist
            // [][] File already exists
            // [] FileMode.Append and FileAccess.ReadWrite
            // [][] File does not exist
            // [][] File already exists

            // Simple call throughs to FileStream, just test functionality

            TestMethod(FileMode.CreateNew, FileAccess.Read);
            TestMethod(FileMode.CreateNew, FileAccess.Write);
            TestMethod(FileMode.CreateNew, FileAccess.ReadWrite);
            TestMethod(FileMode.Create, FileAccess.Read);
            TestMethod(FileMode.Create, FileAccess.Write);
            TestMethod(FileMode.Create, FileAccess.ReadWrite);
            TestMethod(FileMode.Open, FileAccess.Read);
            TestMethod(FileMode.Open, FileAccess.Write);
            TestMethod(FileMode.Open, FileAccess.ReadWrite);
            TestMethod(FileMode.OpenOrCreate, FileAccess.Read);
            TestMethod(FileMode.OpenOrCreate, FileAccess.Write);
            TestMethod(FileMode.OpenOrCreate, FileAccess.ReadWrite);
            TestMethod(FileMode.Truncate, FileAccess.Read);
            TestMethod(FileMode.Truncate, FileAccess.Write);
            TestMethod(FileMode.Truncate, FileAccess.ReadWrite);
            TestMethod(FileMode.Append, FileAccess.Read);
            TestMethod(FileMode.Append, FileAccess.Write);
            TestMethod(FileMode.Append, FileAccess.ReadWrite);

            ///////////////////////////////////////////////////////////////////
            /////////////////////////// END TESTS /////////////////////////////
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



    public static void TestMethod(FileMode fm, FileAccess fa)
    {
        String filName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
        FileInfo fil2;
        StreamWriter sw2;
        Stream fs2;
        String str2;

        //			Console.WriteLine("FileMode: "+Enum.ToString(typeof(FileMode), fm, "G"));
        //			Console.WriteLine("FileAccess: "+Enum.ToString(typeof(FileAccess), fa, "G"));

        if (File.Exists(filName))
            File.Delete(filName);

        // File does not exist
        //------------------------------------------------------------------
        s_strLoc = "Loc_234yg";

        fil2 = new FileInfo(filName);
        switch (fm)
        {
            case FileMode.CreateNew:
            case FileMode.Create:
            case FileMode.OpenOrCreate:
                try
                {
                    fs2 = fil2.Open(fm, fa);
                    s_iCountTestcases++;
                    if (!File.Exists(filName))
                    {
                        s_iCountErrors++;
                        //TODO:
                        //printerr( "Error_48gb7! File not created, FileMode=="+Enum.ToString(typeof(FileMode), fm, "G"));
                    }
                    fs2.Dispose();
                }
                catch (ArgumentException aexc)
                {
                    if (!((fm == FileMode.Create && fa == FileAccess.Read) || (fm == FileMode.CreateNew && fa == FileAccess.Read)))
                    {
                        s_iCountErrors++;
                        printerr("Error_478v8! Unexpected exception, aexc==" + aexc);
                    }
                }
                catch (Exception exc)
                {
                    s_iCountErrors++;
                    printerr("Error_4879v! Incorrect exception thrown, exc==" + exc);
                }
                break;
            case FileMode.Open:
            case FileMode.Truncate:
                s_iCountTestcases++;
                try
                {
                    fs2 = fil2.Open(fm, fa);
                    s_iCountErrors++;
                    printerr("Error_2yg8b! Expected exception not thrown");
                    fs2.Dispose();
                }
                catch (IOException)
                {
                }
                catch (ArgumentException aexc)
                {
                    if (fa != FileAccess.Read)
                    {
                        s_iCountErrors++;
                        printerr("Error_v48y8! Unexpected exception thrown, aexc==" + aexc);
                    }
                }
                catch (Exception exc)
                {
                    s_iCountErrors++;
                    printerr("Error_2y7gf! Incorrect exception thrown, exc==" + exc.ToString());
                }
                break;
            case FileMode.Append:
                if (fa == FileAccess.Write)
                {
                    fs2 = fil2.Open(fm, fa);
                    s_iCountTestcases++;
                    if (!File.Exists(filName))
                    {
                        s_iCountErrors++;
                        printerr("Error_2498y! File not created");
                    }
                    fs2.Dispose();
                }
                else
                {
                    s_iCountTestcases++;
                    try
                    {
                        fs2 = fil2.Open(fm, fa);
                        s_iCountErrors++;
                        printerr("Error_2g78b! Expected exception not thrown");
                        fs2.Dispose();
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (Exception exc)
                    {
                        s_iCountErrors++;
                        printerr("Error_g77b7! Incorrect exception thrown, exc==" + exc.ToString());
                    }
                }
                break;
            default:
                s_iCountErrors++;
                printerr("Error_27tbv! This should not be....");
                break;
        }
        if (File.Exists(filName))
            File.Delete(filName);

        //------------------------------------------------------------------


        //  File already exists
        //------------------------------------------------------------------
        s_strLoc = "Loc_4yg7b";

        FileStream stream = new FileStream(filName, FileMode.Create);
        sw2 = new StreamWriter(stream);
        str2 = "Du er en ape";
        sw2.Write(str2);
        sw2.Dispose();
        stream.Dispose();
        fil2 = new FileInfo(filName);
        switch (fm)
        {
            case FileMode.CreateNew:
                s_iCountTestcases++;
                try
                {
                    fs2 = fil2.Open(fm, fa);
                    s_iCountErrors++;
                    printerr("Error_27b98! Expected exception not thrown");
                    fs2.Dispose();
                }
                catch (ArgumentException aexc)
                {
                    if (fa != FileAccess.Read)
                    {
                        s_iCountErrors++;
                        printerr("Error_4387v! Unexpected exception, aexc==" + aexc);
                    }
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
                try
                {
                    fs2 = fil2.Open(fm, fa);
                    if (fs2.Length != 0)
                    {
                        s_iCountErrors++;
                        printerr("Error_287vb! Incorrect length of file==" + fil2.Length);
                    }
                    fs2.Dispose();
                }
                catch (ArgumentException aexc)
                {
                    if (fa != FileAccess.Read)
                    {
                        s_iCountErrors++;
                        printerr("Error_48vy7! Unexpected exception, aexc==" + aexc);
                    }
                }
                catch (Exception exc)
                {
                    s_iCountErrors++;
                    printerr("Error_47yv3! Incorrect exception thrown, exc==" + exc.ToString());
                }
                break;
            case FileMode.OpenOrCreate:
            case FileMode.Open:
                fs2 = fil2.Open(fm, fa);
                if (fs2.Length != str2.Length)
                {
                    s_iCountErrors++;
                    printerr("Error_2gy78! Incorrect length on file==" + fil2.Length);
                }
                fs2.Dispose();
                break;
            case FileMode.Truncate:
                if (fa == FileAccess.Read)
                {
                    s_iCountTestcases++;
                    try
                    {
                        fs2 = fil2.Open(fm, fa);
                        s_iCountErrors++;
                        printerr("Error_g95y8! Expected exception not thrown");
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (Exception exc)
                    {
                        s_iCountErrors++;
                        printerr("Error_98y4v! Incorrect exception thrown, exc==" + exc.ToString());
                    }
                }
                else
                {
                    fs2 = fil2.Open(fm, fa);
                    if (fs2.Length != 0)
                    {
                        s_iCountErrors++;
                        printerr("Error_29gv9! Incorrect length on file==" + fil2.Length);
                    }
                    fs2.Dispose();
                }
                break;
            case FileMode.Append:
                if (fa == FileAccess.Write)
                {
                    fs2 = fil2.Open(fm, fa);
                    s_iCountTestcases++;
                    if (!File.Exists(filName))
                    {
                        s_iCountErrors++;
                        printerr("Error_4089v! File not created");
                    }
                    fs2.Dispose();
                }
                else
                {
                    s_iCountTestcases++;
                    try
                    {
                        fs2 = fil2.Open(fm, fa);
                        s_iCountErrors++;
                        printerr("Error_287yb! Expected exception not thrown");
                        fs2.Dispose();
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (Exception exc)
                    {
                        s_iCountErrors++;
                        printerr("Error_27878! Incorrect exception thrown, exc==" + exc.ToString());
                    }
                }
                break;
            default:
                s_iCountErrors++;
                printerr("Error_587yb! This should not be...");
                break;
        }


        //------------------------------------------------------------------



        if (File.Exists(filName))
            File.Delete(filName);
    }


    public static void printerr(String err, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine("ERROR: ({0}, {1}, {2}) {3}", memberName, filePath, lineNumber, err);
    }
}

