// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class Directory_SetCurrentDirectory : FileSystemTest
    {
        public static String s_strDtTmVer = "2000/07/07 19:00";
        public static String s_strClassMethod = "Directory.SetCurrentDirectory(Directory)";
        public static String s_strTFName = "set_SetCurrentDirectory_dir.cs";
        public static String s_strTFPath = Directory.GetCurrentDirectory();

        [Fact]
        public void Null_Path_Throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Directory.SetCurrentDirectory(null));
        }

        [Fact]
        public void Empty_Path_Throws_ArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Directory.SetCurrentDirectory(string.Empty));
        }

        [Fact]
        [ActiveIssue(1220)] // SetCurrentDirectory
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

                DirectoryInfo dir = null;
                DirectoryInfo currentdir = new DirectoryInfo(Directory.GetCurrentDirectory());

                //Code coverage
                try
                {
                    Directory.SetCurrentDirectory(new String('a', IOInputs.MaxPath + 1));
                    iCountErrors++;
                    Console.WriteLine("Err_34tgs! No exception thrown");
                }
                catch (PathTooLongException)
                {
                }
                catch (Exception ex)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_34tgs! No exception thrown: {0}", ex);
                }

                // [] Vanilla case

                strLoc = "Loc_2g77b";

                Directory.SetCurrentDirectory("..");
                dir = new DirectoryInfo(".");
                iCountTestcases++;
                if (!Directory.GetCurrentDirectory().Equals(dir.FullName))
                {
                    iCountErrors++;
                    printerr("Error_38g8b! Directory Not set correctly");
                }


                // [] Another vanilla case

                strLoc = "Loc_9t8gt";

                string root = Path.GetPathRoot(Directory.GetCurrentDirectory());
                dir = new DirectoryInfo(root);
                Directory.SetCurrentDirectory(root);
                iCountTestcases++;
                if (!Directory.GetCurrentDirectory().Equals(dir.FullName))
                {
                    iCountErrors++;
                    printerr("Error_238g7! Directory not set correctly, dir==" + Directory.GetCurrentDirectory());
                }


                // [] Set back to original

                strLoc = "Loc_87ygv";

                Directory.SetCurrentDirectory(currentdir.FullName);
                iCountTestcases++;
                if (!Directory.GetCurrentDirectory().Equals(currentdir.FullName))
                {
                    iCountErrors++;
                    printerr("Error_2g7b7! Directory not set correctly, dir==" + Directory.GetCurrentDirectory());
                }

                ///////////////////////////////////////////////////////////////////
                /////////////////////////// END TESTS /////////////////////////////
            }
            catch (Exception exc_general)
            {
                ++iCountErrors;
                printerr("Error Err_8888yyy!  strLoc==" + strLoc + ", exc_general==" + exc_general.ToString());
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
}
