// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text;
using Xunit;

public class File_AppendAll_all
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2002/09/30 19:25";
    public static String s_strClassMethod = "File.AppendAllText- All the methods";
    public static String s_strTFName = "AppendAll_all.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();


    [Fact]
    public static void runTest()
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";

        String path;
        Encoding encoding;
        String content;

        String all;
        StringBuilder builder;
        StringBuilder builder1;
        StreamReader reader;
        FileStream stream;


        try
        {
            //This TestCase tests the following methods that have been added to File for RAD purposes. These methods are thin wrappers for already 
            //defined methods in the IO namespace. We do not aim to test the underlying APIs here

            //void AppendAllText(String path, String contents)
            //void void AppendAllText(String path, String contents, Encoding encoding)

            //Argument 

            strLoc = "Loc_001oo";

            iCountTestcases++;

            path = null;
            content = "";
            try
            {
                File.AppendAllText(path, content);

                iCountErrors++;
                Console.WriteLine("Err_3947sgd! Expected Exception here.");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception ex)
            {
                iCountErrors++;
                Console.WriteLine("Err_9734sfg! Unexoected exception thrown; {0}", ex.GetType().Name);
            }

            path = null;
            encoding = Encoding.UTF8;
            try
            {
                File.AppendAllText(path, content, encoding);

                iCountErrors++;
                Console.WriteLine("Err_3947sgd! Expected Exception here.");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception ex)
            {
                iCountErrors++;
                Console.WriteLine("Err_9734sfg! Unexoected exception thrown; {0}", ex.GetType().Name);
            }

            path = Path.GetTempFileName();
            encoding = null;
            try
            {
                File.AppendAllText(path, content, encoding);

                iCountErrors++;
                Console.WriteLine("Err_3947sgd! Expected Exception here.");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception ex)
            {
                iCountErrors++;
                Console.WriteLine("Err_9734sfg! Unexoected exception thrown; {0}", ex.GetType().Name);
            }
            if (File.Exists(path))
                File.Delete(path);


            //vanilla: Non existent paths, writing null

            strLoc = "Loc_002oo";

            iCountTestcases++;

            path = Path.GetTempFileName();
            if (File.Exists(path))
                File.Delete(path);

            content = null;
            File.AppendAllText(path, content);
            //We know that the below API throws for non-existent file names
            all = File.ReadAllText(path);

            if (all != String.Empty)
            {
                iCountErrors++;
                Console.WriteLine("Err_3486sgd! Expected Exception here.");
            }
            if (File.Exists(path))
                File.Delete(path);

            encoding = Encoding.UTF8;
            File.AppendAllText(path, content, encoding);
            all = File.ReadAllText(path);

            if (all != String.Empty)
            {
                iCountErrors++;
                Console.WriteLine("Err_3486sgd! Expected Exception here.");
            }
            if (File.Exists(path))
                File.Delete(path);

            //Vanilla - make sure that normal behavior is seen for all the methods

            strLoc = "Loc_003oo";

            iCountTestcases++;

            path = Path.GetTempFileName();

            builder = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                builder.Append(i.ToString() + Environment.NewLine);
            }
            content = builder.ToString();
            File.AppendAllText(path, content);
            all = File.ReadAllText(path);

            if (all != content)
            {
                iCountErrors++;
                Console.WriteLine("Err_3486sgd! Expected Exception here.");
            }

            //Another AppendAllText should not overwrite 
            builder1 = new StringBuilder(all);
            builder = new StringBuilder();
            for (int i = 500; i < 600; i++)
            {
                builder.Append(i.ToString() + Environment.NewLine);
                builder1.Append(i.ToString() + Environment.NewLine);
            }
            content = builder.ToString();
            File.AppendAllText(path, content);
            all = File.ReadAllText(path);

            if (all != builder1.ToString())
            {
                iCountErrors++;
                Console.WriteLine("Err_2495sg! Wrong value returned: {0}", all);
            }

            builder = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                builder.Append(i.ToString() + Environment.NewLine);
                builder1.Append(i.ToString() + Environment.NewLine);
            }
            content = builder.ToString();
            encoding = Encoding.GetEncoding("US-ASCII");
            File.AppendAllText(path, content, encoding);
            all = File.ReadAllText(path);

            if (all != builder1.ToString())
            {
                iCountErrors++;
                Console.WriteLine("Err_234afd! Wrong value returned: {0}", all);
            }

            //Another writeall overwrite not appends
            builder = new StringBuilder();
            for (int i = 500; i < 600; i++)
            {
                builder.Append(i.ToString() + Environment.NewLine);
                builder1.Append(i.ToString() + Environment.NewLine);
            }
            content = builder.ToString();
            File.AppendAllText(path, content, encoding);
            all = File.ReadAllText(path);

            if (all != builder1.ToString())
            {
                iCountErrors++;
                Console.WriteLine("Err_4356sdg! Wrong value returned: {0}", all);
            }

            if (File.Exists(path))
                File.Delete(path);

            //Open issues - file already opened, We've closed the file after write etc
            //That we are closing the file after append is imlictly tested above

            path = Path.GetTempFileName();
            stream = new FileStream(path, FileMode.Open);
            reader = new StreamReader(stream);

            content = "";
            try
            {
                File.AppendAllText(path, content);

                iCountErrors++;
                Console.WriteLine("Err_3947sgd! Expected Exception here.");
            }
            catch (IOException)
            {
            }
            catch (Exception ex)
            {
                iCountErrors++;
                Console.WriteLine("Err_9734sfg! Unexoected exception thrown; {0}", ex.GetType().Name);
            }
            encoding = Encoding.UTF8;
            try
            {
                File.AppendAllText(path, content, encoding);

                iCountErrors++;
                Console.WriteLine("Err_3947sgd! Expected Exception here.");
            }
            catch (IOException)
            {
            }
            catch (Exception ex)
            {
                iCountErrors++;
                Console.WriteLine("Err_9734sfg! Unexoected exception thrown; {0}", ex.GetType().Name);
            }

            reader.Dispose();
            stream.Dispose();
            if (File.Exists(path))
                File.Delete(path);
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

