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

public class FileInfo_set_Attributes
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/05/09 11:28";
    public static String s_strClassMethod = "File.Directory()";
    public static String s_strTFName = "set_Attributes.cs";
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
            FileInfo fil2;

            /*
              ReadOnly = 0x1,
              Hidden = 0x2,
              System = 0x4,
              Directory = 0x10,
              Archive = 0x20,
              Encrypted = 0x40,
              Normal = 0x80,
              Temporary = 0x100,
              SparseFile = 0x200,
              ReparsePoint = 0x400,
              Compressed = 0x800,
              Offline = 0x1000,
              NotContentIndexed = 0x2000
            */

            if (File.Exists(filName))
            {
                fil2 = new FileInfo(filName);
                fil2.Attributes = new FileAttributes();
                File.Delete(filName);
            }


            // [] File does not exist
            //--------------------------------------------------------
            strLoc = "loc_2yg8c";

            fil2 = new FileInfo("FileDoesNotExist");
            iCountTestcases++;
            try
            {
                fil2.Attributes = new FileAttributes();
                iCountErrors++;
                printerr("Error_27t8b! Expected exception not thrown");
            }
            catch (FileNotFoundException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_21409! Incorrect exception thrown, exc==" + exc.ToString());
            }

            //-----------------------------------------------------------------




            File.Create(filName).Dispose();

            /*
                        // [] Invalid enum value
                        //-----------------------------------------------------------------
                        strLoc = "Loc_298g8";

                        fil2 = new FileInfo(filName);
                        iCountTestcases++;
                        try {
                            fil2.Attributes = (FileAttributes)0x4000;
                            iCountErrors++;
                            printerr( "Error_28g8b! Expected exception not thrown");
                        } catch (ArgumentException aexc) {
                        } catch (Exception exc) {
                            iCountErrors++;
                            printerr( "Error_t94u9! Incorrect exception thrown, exc=="+exc.ToString());
                        }

                        //-----------------------------------------------------------------
            */

            try
            {
                // [] valid data
                //-----------------------------------------------------------------
                strLoc = "Loc_48yx9";

                fil2 = new FileInfo(filName);
                fil2.Attributes = FileAttributes.Hidden;
                iCountTestcases++;
#if TEST_WINRT  // WinRT doesn't support hidden
                if ((fil2.Attributes & FileAttributes.Hidden)!=0) {
#else
                if ((fil2.Attributes & FileAttributes.Hidden) == 0 && Interop.IsWindows) // setting Hidden not supported on Unix
                {
#endif
                    iCountErrors++;
                    printerr("ERror_2g985! Hidden not set");
                }
                fil2.Attributes = FileAttributes.System | FileAttributes.Normal;
                fil2.Refresh();
                iCountTestcases++;
#if TEST_WINRT  // WinRT doesn't support system
                if((fil2.Attributes & FileAttributes.System) == FileAttributes.System) {
#else
                if ((fil2.Attributes & FileAttributes.System) != FileAttributes.System && Interop.IsWindows) // setting System not supported on Unix
                {
#endif
                    iCountErrors++;
                    printerr("Error_298g7! System not set");
                }
                fil2.Attributes = FileAttributes.Temporary;
                fil2.Refresh();
                iCountTestcases++;
                if ((fil2.Attributes & FileAttributes.Temporary) == 0 && Interop.IsWindows) // setting Temporary not supported on Unix
                {
                    iCountErrors++;
                    printerr("Error_87tg8! Temporary not set");
                }
                //-----------------------------------------------------------------



                // [] 
                //-----------------------------------------------------------------
                strLoc = "Loc_29gy7";
                fil2 = new FileInfo(filName);

                fil2.Attributes = FileAttributes.ReadOnly;
                fil2.Refresh();
                fil2.Attributes = fil2.Attributes | FileAttributes.Encrypted | FileAttributes.Archive;
                fil2.Refresh();
                iCountTestcases++;
                if ((fil2.Attributes & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
                {
                    iCountErrors++;
                    printerr("Error_g58y8! ReadOnly attribute not set");
                }

                iCountTestcases++;
                if ((fil2.Attributes & FileAttributes.Archive) != FileAttributes.Archive && Interop.IsWindows) // setting Archive not supported on Unix
                {
                    iCountErrors++;
                    printerr("Error_2g78b! Archive attribute not set");
                }
                fil2.Attributes = new FileAttributes();
                //-----------------------------------------------------------------
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_284y8! Unexpected exception thrown, bug 29808, exc==" + exc.ToString());
            }

            if (File.Exists(filName))
            {
                fil2 = new FileInfo(filName);
                fil2.Attributes = new FileAttributes();
                File.Delete(filName);
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

    public static void printerr(String err, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine("ERROR: ({0}, {1}, {2}) {3}", memberName, filePath, lineNumber, err);
    }
}

