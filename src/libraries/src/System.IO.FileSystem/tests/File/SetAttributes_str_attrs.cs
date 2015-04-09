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

public class File_SetAttributes_str_attrs
{
    public static String s_strDtTmVer = "2001/01/31 5:30";
    public static String s_strClassMethod = "File.Directory()";
    public static String s_strTFName = "SetAttributes_str_attrs.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
    public static void runTest()
    {
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "";

        try
        {
            String fileName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            FileInfo file1;

            // [] With an empty string
            strLoc = "loc_0000";

            iCountTestcases++;
            try
            {
                File.SetAttributes(strValue, FileAttributes.Hidden);
                iCountErrors++;
                printerr("Error_0001! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0003! Incorrect exception thrown, exc==" + exc.ToString());
            }

            // [] By passing a null argument.
            strLoc = "loc_0004";

            iCountTestcases++;
            try
            {
                File.SetAttributes(null, FileAttributes.Hidden);
                iCountErrors++;
                printerr("Error_0005! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0007! Incorrect exception thrown, exc==" + exc.ToString());
            }

            // [] By passing an invalid path name.
            strLoc = "loc_0005";

            iCountTestcases++;
            try
            {
                File.SetAttributes(Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory()), "this is a invalid testing directory", "test", "test"), FileAttributes.Hidden);
                iCountErrors++;
                printerr("Error_0006! Expected exception not thrown");
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0008! Incorrect exception thrown, exc==" + exc.ToString());
            }


            // [] valid data
            //-----------------------------------------------------------------
            strLoc = "Loc_0009";

            file1 = new FileInfo(fileName);
            FileStream fs = file1.Create();

#if !TEST_WINRT
            iCountTestcases++;
            File.SetAttributes(fileName, FileAttributes.Hidden);
            if ((File.GetAttributes(fileName) & FileAttributes.Hidden) == 0 && Interop.IsWindows) // setting Hidden not supported on Unix
            {
                iCountErrors++;
                printerr("Error_0010! Hidden not set");
            }

            iCountTestcases++;
            file1.Refresh();
            File.SetAttributes(fileName, FileAttributes.System);
            if ((File.GetAttributes(fileName) & FileAttributes.System) == 0 && Interop.IsWindows) // setting System not supported on Unix
            {
                iCountErrors++;
                printerr("Error_0011! System not set");
            }
#endif

            iCountTestcases++;
            file1.Refresh();
            File.SetAttributes(fileName, FileAttributes.Normal);
            if ((File.GetAttributes(fileName) & FileAttributes.Normal) != FileAttributes.Normal)
            {
                //NOTE: Normal is only allowed on its own. So, if there is already another attribute, this will not work
                //@TODO!! This might have to be modified
                if ((File.GetAttributes(fileName) & FileAttributes.Compressed) == 0)
#if TEST_WINRT
                if((File.GetAttributes(fileName) & FileAttributes.Archive) == 0)
#endif
                {
                    iCountErrors++;
                    printerr("Error_0012! Normal not set");
                }
            }

            iCountTestcases++;
            file1.Refresh();
            File.SetAttributes(fileName, FileAttributes.Temporary);
            if ((File.GetAttributes(fileName) & FileAttributes.Temporary) == 0 && Interop.IsWindows) // setting Temporary not supported on Unix
            {
                iCountErrors++;
                printerr("Error_0013! Temporary not set");
            }

            File.SetAttributes(fileName, FileAttributes.Archive);
            file1.Refresh();
            File.SetAttributes(fileName, FileAttributes.ReadOnly | FileAttributes.Archive); // setting Archive not supported on Unix
            file1.Refresh();
            iCountTestcases++;
            if ((File.GetAttributes(fileName) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
            {
                iCountErrors++;
                printerr("Error_0020! ReadOnly attribute not set");
            }

            iCountTestcases++;
            if ((File.GetAttributes(fileName) & FileAttributes.Archive) != FileAttributes.Archive && Interop.IsWindows) // setting Archive not supported on Unix
            {
                iCountErrors++;
                printerr("Error_0021! Archive attribute not set");
            }
            File.SetAttributes(fileName, FileAttributes.Normal);
            fs.Dispose();
            file1.Delete();
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

