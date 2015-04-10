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

public class File_Open_fm_fa_fs
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2000/05/08 12:18";
    public static String s_strClassMethod = "File.OpenText(String)";
    public static String s_strTFName = "Open_fm_fa_fs.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();

    private delegate void ExceptionCode();
    private static bool s_pass = true;

    [Fact]
    public static void runTest()
    {
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        try
        {
            String fileName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());
            Stream fs2 = null, fs3;

            if (File.Exists(fileName))
                File.Delete(fileName);

            // [] FileSharing.None -- Should not be able to access the file
            //-----------------------------------------------------------------
            strLoc = "Loc_2498V";

            fs2 = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            iCountTestcases++;
            try
            {
                fs3 = File.Open(fileName, FileMode.Open);
                iCountErrors++;
                printerr("Error_209uv! Shouldn't be able to open an open file");
                fs3.Dispose();
#if TEST_WINRT
            } catch (UnauthorizedAccessException) {
#endif
            }
            catch (IOException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_287gv! Incorrect exception thrown, exc==" + exc.ToString());
            }
            fs2.Dispose();

            // [] FileSharing.Read
            //-----------------------------------------------------------------
            strLoc = "Loc_f5498";

            // create test file
            fs2 = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            fs2.Write(new Byte[] { 10 }, 0, 1);
            fs2.Flush();
            fs2.Dispose();

            // FileShare is not supported by WINRT, sharing is controlled by access.
            // It allows concurrent readers, and write after read, but not vice-versa.
            // Reopen file as only Read to test concurrent read behavior.
            fs2 = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            iCountTestcases++;
            fs3 = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fs2.Read(new Byte[1], 0, 1);
            fs3.Read(new Byte[1], 0, 1);
            iCountTestcases++;
            try
            {
                fs3.Write(new Byte[] { 10 }, 0, 1);
                iCountErrors++;
                printerr("Error_958vc! Expected exception not thrown");
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_20939! Incorrect exception thrown, exc==" + exc.ToString());
            }

            fs2.Dispose();
            fs3.Dispose();

            //-----------------------------------------------------------------

            // [] With null file name
            //-----------------------------------------------------------------
            strLoc = "Loc_2498x";

            iCountTestcases++;
            try
            {
                fs3 = File.Open(null, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                iCountErrors++;
                printerr("Error_0001! Expected exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_0003! Unexpected exception thrown, exc==" + exc.ToString());
            }
            fs3.Dispose();

            // [] empty string file name
            //-----------------------------------------------------------------
            strLoc = "Loc_2498x";

            iCountTestcases++;
            try
            {
                fs3 = File.Open("", FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                iCountErrors++;
                printerr("Error_1001! Expected exception not thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_2003! Unexpected exception thrown, exc==" + exc.ToString());
            }
            fs3.Dispose();

            // [] FileSharing.Write
            //-----------------------------------------------------------------
            strLoc = "Loc_2498x";

            fs2 = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Write);
            iCountTestcases++;
            try
            {
                fs3 = File.Open(fileName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                fs3.Write(new Byte[] { 1, 2 }, 0, 2);
#if TEST_WINRT // WinRT's sharing model does not support concurrent write
                printerr( "Error_209uv! Shouldn't be able to open concurrent writers");
            } catch (UnauthorizedAccessException) {
#endif
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_2980x! Unexpected exception thrown, exc==" + exc.ToString());
            }
            fs2.Dispose();
            fs3.Dispose();

#if !TEST_WINRT // WinRT's sharing model doesn't support concurrent write
            // [] FileSharing.ReadWrite
            //-----------------------------------------------------------------
            strLoc = "Loc_4897y";

            fs2 = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            iCountTestcases++;
            try
            {
                fs3 = File.Open(fileName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                fs2.Write(new Byte[] { 1 }, 0, 1);
                fs3.Dispose();
                fs3 = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fs2.Write(new Byte[] { 2 }, 0, 1);
                fs3.Dispose();
                fs3 = File.Open(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                fs2.Write(new Byte[] { 3 }, 0, 1);
                fs3.Dispose();
            }
            catch (Exception exc)
            {
                iCountErrors++;
                printerr("Error_287g9! Unexpected exception thrown, exc==" + exc.ToString());
            }
            fs2.Dispose();
#endif

            if (File.Exists(fileName))
                File.Delete(fileName);

            const String sourceString = "This is the source File";

            //First we look at the drives of this machine to be sure that we can proceed
            String sourceFileName = Path.Combine(TestInfo.CurrentDirectory, Path.GetRandomFileName());

            //Scenario 1: Vanilla - create a filestream with this flag in FileShare parameter and then delete the file (using File.Delete or FileInfo.Delete) before closing the 
            //FileStream. Ensure that the delete operation succeeds but the file is not deleted till the FileStream is closed
            try
            {
                Byte[] outbits = Encoding.Unicode.GetBytes(sourceString);

                FileStream stream = File.Open(sourceFileName, FileMode.Create, FileAccess.Write, FileShare.Delete);

                stream.Write(outbits, 0, outbits.Length);

                //This should succeed
                File.Delete(sourceFileName);

                if (Interop.IsWindows) // file will be deleted on Unix
                {
                    //But we shold still be able to call the file
                    Eval(File.Exists(sourceFileName), "Err_3947sg! File doesn't exists");
                }

                stream.Write(outbits, 0, outbits.Length);

                stream.Dispose();

                //Now it shouldn't exists - is there any OS delay
                Eval(!File.Exists(sourceFileName), "Err_2397g! File doesn't exists");

                if (!s_pass)
                    iCountErrors++;
            }
            catch (Exception ex)
            {
                s_pass = false;
                Console.WriteLine("Err_349t7g! Exception caught in scenario: {0}", ex);
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



    //Checks for error
    private static void Eval(bool expression, String msg, params Object[] values)
    {
        Eval(expression, String.Format(msg, values));
    }

    private static void Eval(bool expression, String msg)
    {
        if (!expression)
        {
            s_pass = false;
            Console.WriteLine(msg);
        }
    }

    //Checks for a particular type of exception
    private static void CheckException<E>(ExceptionCode test, string error)
    {
        CheckException<E>(test, error, null);
    }

    //Checks for a particular type of exception and an Exception msg in the English locale
    private static void CheckException<E>(ExceptionCode test, string error, String msgExpected)
    {
        bool exception = false;
        try
        {
            test();
            error = String.Format("{0} Exception NOT thrown ", error);
        }
        catch (Exception e)
        {
            if (e.GetType() == typeof(E))
            {
                exception = true;
                if (msgExpected != null && System.Globalization.CultureInfo.CurrentUICulture.Name == "en-US" && e.Message != msgExpected)
                {
                    exception = false;
                    error = String.Format("{0} Message Different: <{1}>", error, e.Message);
                }
            }
            else
                error = String.Format("{0} Exception type: {1}", error, e.GetType().Name);
        }
        Eval(exception, error);
    }


    public static void printerr(String err, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine("ERROR: ({0}, {1}, {2}) {3}", memberName, filePath, lineNumber, err);
    }
}
