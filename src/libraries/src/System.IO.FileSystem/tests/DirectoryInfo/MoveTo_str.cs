// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.IO;
using System.Collections;
using System.Globalization;
using Xunit;

public class DirectoryInfo_Move_str
{
    public static String s_strActiveBugNums = "34383";
    public static String s_strClassMethod = "Directory.Move(String)";
    public static String s_strTFName = "Move_str.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    [Fact]
    public static void runTest()
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;


        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////

            FileInfo fil2 = null;
            DirectoryInfo dir2 = null;

            // [] Pass in null argument

            strLoc = "Loc_099u8";
            string testDir = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());

            dir2 = Directory.CreateDirectory(testDir);
            iCountTestcases++;
            try
            {
                dir2.MoveTo(null);
                iCountErrors++;
                printerr("Error_298dy! Expected exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_209xj! Incorrect exception thrown, exc==" + exc.ToString());
            }
            dir2.Delete(true);

            // [] Pass in empty String should throw ArgumentException

            strLoc = "Loc_098gt";

            dir2 = Directory.CreateDirectory(testDir);
            iCountTestcases++;
            try
            {
                dir2.MoveTo(String.Empty);
                iCountErrors++;
                printerr("Error_3987c! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_9092c! Incorrect exception thrown, exc==" + exc.ToString());
            }
            dir2.Delete(true);


            // [] Vanilla move to new name

            strLoc = "Loc_98hvc";

            dir2 = Directory.CreateDirectory(testDir);
            iCountTestcases++;
            dir2.MoveTo(Path.Combine(TestInfo.CurrentDirectory, "Test3"));
            try
            {
                dir2 = new DirectoryInfo(Path.Combine(TestInfo.CurrentDirectory, "Test3"));
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_2881s! Directory not moved, exc==" + exc.ToString());
            }
            dir2.Delete(true);


            // [] Try to move it on top of current dir

            strLoc = "Loc_2908x";

            dir2 = Directory.CreateDirectory(testDir);
            iCountTestcases++;
            try
            {
                dir2.MoveTo(Path.Combine(TestInfo.CurrentDirectory, "."));
                iCountErrors++;
                printerr("Error_2091z! Expected exception not thrown,");
            }
            catch (IOException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_2100s! Incorrect exception thrown, exc==" + exc.ToString());
            }
            dir2.Delete(true);

            // [] Try to move it on top of parent dir

            strLoc = "Loc_1999s";

            dir2 = Directory.CreateDirectory(testDir);
            iCountTestcases++;
            try
            {
                dir2.MoveTo(Path.Combine(TestInfo.CurrentDirectory, ".."));
                iCountErrors++;
                printerr("Error_2091b! Expected exception not thrown");
            }
            catch (IOException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_01990! Incorrect exception thrown, exc==" + exc.ToString());
            }
            dir2.Delete(true);



            // [] Pass in string with spaces should throw ArgumentException

            strLoc = "Loc_498vy";

            dir2 = Directory.CreateDirectory(testDir);
            iCountTestcases++;
            try
            {
                dir2.MoveTo("         ");
                iCountErrors++;
                printerr("Error_209uc! Expected exception not thrown");
                fil2.Delete();
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_28829! Incorrect exception thrown, exc==" + exc.ToString());
            }
            dir2.Delete(true);

            // [] Mvoe to the same directory

            strLoc = "Loc_498vy";

            dir2 = Directory.CreateDirectory(testDir);
            iCountTestcases++;
            try
            {
                dir2.MoveTo(testDir);
                iCountErrors++;
                printerr("Error_209uc! Expected exception not thrown");
                fil2.Delete();
            }
            catch (IOException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_28829! Incorrect exception thrown, exc==" + exc.ToString());
            }
            dir2.Delete(true);

#if !TEST_WINRT // Can't access other root drives
            // [] Move to different drive will throw AccessException
            //-----------------------------------------------------------------
            if (Interop.IsWindows) // drive labels

            {
                strLoc = "Loc_00025";

                dir2 = Directory.CreateDirectory(testDir);
                iCountTestcases++;
                try
                {
                    if (dir2.FullName.Substring(0, 3) == @"d:\" || dir2.FullName.Substring(0, 3) == @"D:\")
                        dir2.MoveTo("C:\\TempDirectory");
                    else
                        dir2.MoveTo("D:\\TempDirectory");
                    Console.WriteLine("Root directory..." + dir2.FullName.Substring(0, 3));
                    iCountErrors++;
                    printerr("Error_00078! Expected exception not thrown");
                }
                catch (IOException)
                {
                }
                catch (Exception exc)
                {
                    iCountErrors++;
                    printerr("Error_23r0g! Incorrect exception thrown, exc==" + exc.ToString());
                }
                dir2.Delete(true);
            }
#endif

            // [] Move non-existent directory
            dir2 = new DirectoryInfo(testDir);
            try
            {
                dir2.MoveTo(Path.Combine(TestInfo.CurrentDirectory, "Test5526"));
                iCountErrors++;
                Console.WriteLine("Err_34gs! Exception not thrown");
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (Exception ex)
            {
                iCountErrors++;
                Console.WriteLine("Err_34gs! Wrong Exception thrown: {0}", ex);
            }


            // [] Pass in string with tabs 

            strLoc = "Loc_98399";

            dir2 = Directory.CreateDirectory(testDir);
            iCountTestcases++;
            try
            {
                dir2.MoveTo("\t");
                iCountErrors++;
                printerr("Error_2091c! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_8374v! Incorrect exception thrown, exc==" + exc.ToString());
            }
            dir2.Delete(true);




            // [] Create a long filename directory


            strLoc = "Loc_2908y";

            StringBuilder sb = new StringBuilder(TestInfo.CurrentDirectory);
            while (sb.Length < IOInputs.MaxPath + 1)
                sb.Append("a");

            iCountTestcases++;
            dir2 = Directory.CreateDirectory(testDir);
            try
            {
                dir2.MoveTo(sb.ToString());
                iCountErrors++;
                printerr("Error_109ty! Expected exception not thrown");
            }
            catch (PathTooLongException)
            { // This should really be PathTooLongException
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_109dv! Incorrect exception thrown, exc==" + exc.ToString());
            }
            dir2.Delete(true);


            // [] Too long filename

            strLoc = "Loc_48fyf";

            sb = new StringBuilder();
            for (int i = 0; i < IOInputs.MaxPath + 1; i++)
                sb.Append("a");

            iCountTestcases++;
            dir2 = Directory.CreateDirectory(testDir);
            try
            {
                dir2.MoveTo(sb.ToString());
                iCountErrors++;
                printerr("Error_109ty! Expected exception not thrown");
            }
            catch (PathTooLongException)
            { // This should really be PathTooLongException
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_109dv! Incorrect exception thrown, exc==" + exc.ToString());
            }
            dir2.Delete(true);


            // [] specifying subdirectories should fail unless they exist

            strLoc = "Loc_209ud";

            dir2 = Directory.CreateDirectory(testDir);
            iCountTestcases++;
            try
            {
                dir2.MoveTo(Path.Combine(testDir, "Test", "Test", "Test"));
                iCountErrors++;
                printerr("Error_1039s! Expected exception not thrown");
                fil2.Delete();
            }
            catch (IOException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_2019u! Incorrect exception thrown, exc==" + exc.ToString());
            }
            dir2.Delete(true);

            // [] Exception if directory already exists

            strLoc = "Loc_2498x";

            iCountTestcases++;
            Directory.CreateDirectory(testDir + "a");
            if (!Interop.IsWindows) // exception on Unix would only happen if the directory isn't empty
            {
                File.Create(Path.Combine(testDir + "a", "temp.txt")).Dispose(); 
            }
            dir2 = Directory.CreateDirectory(testDir);
            try
            {
                dir2.MoveTo(testDir + "a");
                iCountErrors++;
                printerr("Error_2498h! Expected exception not thrown");
            }
            catch (IOException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_289vt! Incorrect exception thrown, exc==" + exc.ToString());
            }
            dir2.Delete(true);
            new DirectoryInfo(testDir + "a").Delete(true);



            // [] Illiegal chars in new DirectoryInfo name

            strLoc = "Loc_2798r";

            dir2 = Directory.CreateDirectory(testDir);
            iCountTestcases++;
            try
            {
                dir2.MoveTo("******.***");
                iCountErrors++;
                printerr("Error_298hv! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_2199d! Incorrect exception thrown, exc==" + exc.ToString());
            }
            dir2.Delete(true);


            // [] Move a directory with subdirs

            strLoc = "Loc_209ux";

            dir2 = Directory.CreateDirectory(testDir);
            DirectoryInfo subdir = dir2.CreateSubdirectory("Test5525");

            //			dir2.MoveTo("NewTest5525");
            FailSafeDirectoryOperations.MoveDirectoryInfo(dir2, Path.Combine(TestInfo.CurrentDirectory, "NewTest5525"));

            iCountTestcases++;
            try
            {
                subdir = new DirectoryInfo(Path.Combine(TestInfo.CurrentDirectory, "NewTest5525", "Test5525"));
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_290u1! Failed to move Folder, exc==" + exc.ToString());
            }
            subdir.Delete(true);
            dir2.Delete(true);


            // [] 


            //problems with trailing slashes and stuff - #431574
            /**
             - We need to remove the trailing slash when we get the parent directory (we call Path.GetDirectoryName on the full path and having the slash will not work)
             - Root drive always have the trailing slash
             - MoveTo adds a trailing slash
            **/

            String subDir = Path.Combine(TestInfo.CurrentDirectory, "LaksTemp");
            DeleteFileDir(subDir);
            String[] values = { "TestDir", "TestDir" + Path.DirectorySeparatorChar };
            String moveDir = Path.Combine(TestInfo.CurrentDirectory, values[1]);
            foreach (String value in values)
            {
                dir2 = new DirectoryInfo(subDir);
                dir2.Create();
                dir2.MoveTo(Path.Combine(TestInfo.CurrentDirectory, value));
                if (!dir2.FullName.Equals(moveDir))
                {
                    Console.WriteLine("moveDir: <{0}>", moveDir);
                    iCountErrors++;
                    Console.WriteLine("Err_374g! wrong vlaue returned: {0}", dir2.FullName);
                }
                dir2.Delete();
            }

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

    private static void DeleteFileDir(String path)
    {
        if (File.Exists(path))
            File.Delete(path);
        if (Directory.Exists(path))
            Directory.Delete(path);
    }

    public static void printerr(String err, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine("ERROR: ({0}, {1}, {2}) {3}", memberName, filePath, lineNumber, err);
    }
}

