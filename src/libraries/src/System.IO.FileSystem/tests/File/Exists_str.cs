// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text;
using Xunit;

public class File_Exists_str
{
    public static String s_strDtTmVer = "2009/02/18";
    public static String s_strClassMethod = "File.Exists(String)";
    public static String s_strTFName = "FileExists_str.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
    public static void runTest()
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";

        try
        {
            String filName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());

            if (File.Exists(filName))
                File.Delete(filName);


            // [] Exception for null argument
            strLoc = "Loc_t987b";

            iCountTestcases++;
            try
            {
                if (File.Exists(null))
                {
                    iCountErrors++;
                    printerr("Error_49939! Expected exception not thrown");
                }
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_6019b! Incorrect exception thrown, exc==" + exc.ToString());
            }

            // [] ArgumentException for empty path
            strLoc = "Loc_199gb";

            iCountTestcases++;
            try
            {
                if (File.Exists(String.Empty))
                {
                    iCountErrors++;
                    printerr("Error_109tg! Expected exception not thrown");
                }
                if (File.Exists("\0"))
                {
                    iCountErrors++;
                    printerr("Error_123af! Expected exception not thrown");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_6t69b! Incorrect exception thrown, exc==" + exc.ToString());
            }

            // [] Current Directory
            strLoc = "Loc_276t8";

            iCountTestcases++;
            if (File.Exists("."))
            {
                iCountErrors++;
                printerr("Error_95428! Incorrect return value");
            }

            iCountTestcases++;
            if (File.Exists(Directory.GetCurrentDirectory()))
            {
                iCountErrors++;
                printerr("Error_97t67! Incorrect return value");
            }

            // [] Parent Directory
            strLoc = "Loc_y7t03";

            iCountTestcases++;
            if (File.Exists(".."))
            {
                iCountErrors++;
                printerr("Error_290bb! Incorrect return value");
            }

            /* Scenario disabled when porting because it modifies global process state and can not be run in parallel with other tests
#if !TEST_WINRT  // SetCurrentDirectory is not allowed in AppX
            String tmpDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory("C:\\");
            iCountTestcases++;
            if (File.Exists(".."))
            {
                iCountErrors++;
                printerr("Error_t987y! Incorrect return value");
            }
            Directory.SetCurrentDirectory(tmpDir);
#endif
            */

            // network path scenario moved to RemoteIOTests.cs

            // [] Non-existent directories
            strLoc = "Loc_t993c";

            iCountTestcases++;
            if (File.Exists("Da drar vi til fjells"))
            {
                iCountErrors++;
                printerr("Error_6895b! Incorrect return value");
            }

            // [] Path too long
            strLoc = "Loc_t899t";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < IOInputs.MaxPath + 1; i++)
                sb.Append(i);

            iCountTestcases++;
            try
            {
                if (File.Exists(sb.ToString()))
                {
                    iCountErrors++;
                    printerr("Error_20937! Expected exception not thrown");
                }
            }
            catch (PathTooLongException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_7159s! Incorrect exception thrown, exc==" + exc.ToString());
            }

            // [] File that does exist
            strLoc = "Loc_2y78g";

            string shortFilName = Path.GetFileName(filName);
#if !TEST_WINRT  // Current dir is not writable
            new FileStream(shortFilName, FileMode.Create).Dispose();
            iCountTestcases++;
            if (!File.Exists(shortFilName))
            {
                iCountErrors++;
                printerr("Error_9t821! Returned false for existing file");
            }
            File.Delete(shortFilName);
#endif
            string fullFilName = Path.Combine(TestInfo.CurrentDirectory, shortFilName);
            new FileStream(fullFilName, FileMode.Create).Dispose();
            iCountTestcases++;
            if (!File.Exists(fullFilName))
            {
                iCountErrors++;
                printerr("Error_9198v! Returned false for existing file");
            }

            File.Delete(fullFilName);


            iCountTestcases++;
            if (File.Exists(fullFilName))
            {
                iCountErrors++;
                printerr("Errro_197bb! Returned true for deleted file");
            }

            // [] Filename with spaces
            strLoc = "Loc_298g7";

            String tmp = filName + "   " + Path.GetFileName(filName);
            new FileStream(tmp, FileMode.Create).Dispose();
            iCountTestcases++;
            if (!File.Exists(tmp))
            {
                iCountErrors++;
                printerr("Error_01y8v! Returned incorrect value");
            }
            File.Delete(tmp);

            // [] Wildcards in filename should return false

            strLoc = "Loc_398vy8";
            iCountTestcases++;
            try
            {
                if (File.Exists("*"))
                {
                    iCountErrors++;
                    printerr("Error_4979c! File with wildcard exist");
                }
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_498u9! Unexpected exception thrown, exc==" + exc.ToString());
            }

            if (File.Exists(filName))
                File.Delete(filName);
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

