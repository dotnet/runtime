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

public class File_GetAttributes_str
{
    public static String s_strDtTmVer = "2006/11/10";
    public static String s_strClassMethod = "File.GetAttributes()";
    public static String s_strTFName = "GetAttributes_str.cs";
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
                FileAttributes fa = File.GetAttributes(strValue);
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
                FileAttributes fa = File.GetAttributes(null);
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
            //See #515184
            strLoc = "loc_0015";

            iCountTestcases++;
            String[] paths = { Path.Combine("Foo8235378", "Bar"), "Bar2342385", Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory()), "this is a invalid testing directory", "test", "test") };
            foreach (String path in paths)
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);

                try
                {
                    FileAttributes fa = File.GetAttributes(path);
                    Console.WriteLine(fa);
                    iCountErrors++;
                    printerr("Error_0016! Expected exception not thrown for path " + path);
                }
                catch (FileNotFoundException)
                {
                }
                catch (DirectoryNotFoundException)
                {
                }
                catch (Exception exc)
                {
                    iCountErrors++;
                    printerr("Error_0018! Incorrect exception thrown, exc==" + exc.ToString());
                }
            }

            // [] valid data
            //-----------------------------------------------------------------
            strLoc = "Loc_0009";

            file1 = new FileInfo(fileName);
            FileStream fs = file1.Create();

#if !TEST_WINRT
            iCountTestcases++;
            file1.Attributes = FileAttributes.Hidden;
            if ((file1.Attributes & FileAttributes.Hidden) == 0 && Interop.IsWindows) // setting Hidden not supported on Unix
            {
                iCountErrors++;
                printerr("Error_0010! Hidden not set");
            }

            iCountTestcases++;
            file1.Refresh();
            file1.Attributes = FileAttributes.System;
            if ((File.GetAttributes(fileName) & FileAttributes.System) == 0 && Interop.IsWindows) // setting System not supported on Unix
            {
                iCountErrors++;
                printerr("Error_0011! System not set");
            }
#endif

            iCountTestcases++;
            file1.Refresh();
            FileAttributes current = File.GetAttributes(fileName);

            file1.Attributes = FileAttributes.Normal;
            if ((File.GetAttributes(fileName) & FileAttributes.Normal) == 0)
            {
                //NOTE: Normal is only allowed on its own. So, if there is already another attribute, this will not work
                //@TODO!! This might have to be modified
                if ((File.GetAttributes(fileName) & FileAttributes.Compressed) == 0 && Interop.IsWindows) // setting Compressed not supported on Unix
#if TEST_WINRT
                if((File.GetAttributes(fileName) & FileAttributes.Archive) == 0)
#endif
                {
                    Console.WriteLine("Attributes before setting Normal: {0}", current);
                    iCountErrors++;
                    Console.WriteLine("Error_0012! Normal not set: {0}", File.GetAttributes(fileName));
                }
            }

            iCountTestcases++;
            file1.Refresh();
            file1.Attributes = FileAttributes.Temporary;
            if ((File.GetAttributes(fileName) & FileAttributes.Temporary) == 0 && Interop.IsWindows) // setting Temporary not supported on Unix
            {
                iCountErrors++;
                printerr("Error_0013! Temporary not set");
            }

            file1.Attributes = file1.Attributes | FileAttributes.SparseFile;
            file1.Attributes = file1.Attributes | FileAttributes.ReparsePoint;
            iCountTestcases++;
            if ((File.GetAttributes(fileName) & FileAttributes.Temporary) != FileAttributes.Temporary && Interop.IsWindows) // setting Temporary not supported on Unix
            {
                iCountErrors++;
                printerr("Error_0014! Temporary not set");
            }

            file1.Refresh();
            file1.Attributes = FileAttributes.Archive;
            file1.Attributes = FileAttributes.ReadOnly | FileAttributes.Archive; // setting Archive not supported on Unix
            iCountTestcases++;
            if ((File.GetAttributes(fileName) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
            {
                iCountErrors++;
                printerr("Error_0020! ReadOnly attribute not set");
            }

            iCountTestcases++;
            if ((File.GetAttributes(fileName) & FileAttributes.Archive) != FileAttributes.Archive && Interop.IsWindows)  // setting Archive not supported on Unix
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

