// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text;
using Xunit;

public class File_WriteAll_all
{
    public static String s_strActiveBugNums = "184664";
    public static String s_strClassMethod = "File.WriteAllText- All the methods";
    public static String s_strTFName = "WriteAll_all.cs";
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
        String[] contents;

        String all;
        StringBuilder builder;
        int count;
        StreamReader reader;
        FileStream stream;



        try
        {
            //This TestCase tests the following methods that have been added to File for RAD purposes. These methods are thin wrappers for already 
            //defined methods in the IO namespace. We do not aim to test the underlying APIs here

            //void WriteAllText(String path, String contents)
            //void WriteAllText(String path, String contents, Encoding encoding)
            //void WriteAllLines(String path, String[] contents)
            //void WriteAllLines(String path, String[] contents, Encoding encoding)

            //Argument 

            strLoc = "Loc_001oo";

            iCountTestcases++;

            path = null;
            content = "";
            try
            {
                File.WriteAllText(path, content);

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
                File.WriteAllText(path, content, encoding);

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
                File.WriteAllText(path, content, encoding);

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

            path = null;
            contents = new String[0];
            try
            {
                File.WriteAllLines(path, contents);

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
            contents = null;
            try
            {
                File.WriteAllLines(path, contents);

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


            path = null;
            encoding = Encoding.UTF8;
            try
            {
                File.WriteAllLines(path, contents, encoding);

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
                File.WriteAllLines(path, contents, encoding);

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


            // Non existent paths, writing null

            strLoc = "Loc_002oo";

            iCountTestcases++;

            path = Path.GetTempFileName();
            if (File.Exists(path))
                File.Delete(path);

            content = null;
            File.WriteAllText(path, content);
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
            File.WriteAllText(path, content, encoding);
            all = File.ReadAllText(path);

            if (all != String.Empty)
            {
                iCountErrors++;
                Console.WriteLine("Err_3486sgd! Expected Exception here.");
            }
            if (File.Exists(path))
                File.Delete(path);

            contents = new String[0];
            File.WriteAllLines(path, contents);
            all = File.ReadAllText(path);

            if (all != String.Empty)
            {
                iCountErrors++;
                Console.WriteLine("Err_3486sgd! Expected Exception here.");
            }
            if (File.Exists(path))
                File.Delete(path);

            File.WriteAllLines(path, contents, encoding);
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
            File.WriteAllText(path, content);
            all = File.ReadAllText(path);

            if (all != content)
            {
                iCountErrors++;
                Console.WriteLine("Err_3486sgd! Expected Exception here.");
            }

            //Another writeall overwrite not appends
            builder = new StringBuilder();
            for (int i = 500; i < 600; i++)
            {
                builder.Append(i.ToString() + Environment.NewLine);
            }
            content = builder.ToString();
            File.WriteAllText(path, content);
            all = File.ReadAllText(path);

            if (all != content)
            {
                iCountErrors++;
                Console.WriteLine("Err_3486sgd! Expected Exception here.");
            }

            builder = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                builder.Append(i.ToString() + Environment.NewLine);
            }
            content = builder.ToString();
            encoding = Encoding.GetEncoding("US-ASCII");
            File.WriteAllText(path, content, encoding);
            all = File.ReadAllText(path);

            if (all != content)
            {
                iCountErrors++;
                Console.WriteLine("Err_3486sgd! Expected Exception here.");
            }

            //Another writeall overwrite not appends
            builder = new StringBuilder();
            for (int i = 500; i < 600; i++)
            {
                builder.Append(i.ToString() + Environment.NewLine);
            }
            content = builder.ToString();
            File.WriteAllText(path, content, encoding);
            all = File.ReadAllText(path);

            if (all != content)
            {
                iCountErrors++;
                Console.WriteLine("Err_3486sgd! Expected Exception here.");
            }

            count = 100;
            contents = new String[count];
            builder = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                builder.Append(i.ToString() + Environment.NewLine);
                contents[i] = i.ToString();
            }
            File.WriteAllLines(path, contents);
            all = File.ReadAllText(path);

            if (all != builder.ToString())
            {
                iCountErrors++;
                Console.WriteLine("Err_3486sgd! Expected Exception here.");
            }

            //Another writeall overwrite not appends
            builder = new StringBuilder();
            contents = new String[count];
            for (int i = 500; i < (500 + count); i++)
            {
                builder.Append(i.ToString() + Environment.NewLine);
                contents[i - 500] = i.ToString();
            }
            File.WriteAllLines(path, contents);
            all = File.ReadAllText(path);

            if (all != builder.ToString())
            {
                iCountErrors++;
                Console.WriteLine("Err_3486sgd! Expected Exception here.");
            }


            encoding = Encoding.UTF8;
            count = 100;
            contents = new String[count];
            builder = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                builder.Append(i.ToString() + Environment.NewLine);
                contents[i] = i.ToString();
            }
            File.WriteAllLines(path, contents, encoding);
            all = File.ReadAllText(path);

            if (all != builder.ToString())
            {
                iCountErrors++;
                Console.WriteLine("Err_3486sgd! Expected Exception here.");
            }

            //Another writeall overwrite not appends
            builder = new StringBuilder();
            contents = new String[count];
            for (int i = 500; i < (500 + count); i++)
            {
                builder.Append(i.ToString() + Environment.NewLine);
                contents[i - 500] = i.ToString();
            }
            File.WriteAllLines(path, contents, encoding);
            all = File.ReadAllText(path);

            if (all != builder.ToString())
            {
                iCountErrors++;
                Console.WriteLine("Err_3486sgd! Expected Exception here.");
            }

            if (File.Exists(path))
                File.Delete(path);

            //Open issues - file already opened, We've closed the file after write etc
            //That we are closing the file after write is imlictly tested above

            path = Path.GetTempFileName();
            stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            reader = new StreamReader(stream);

            content = "";
            try
            {
                File.WriteAllText(path, content);

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
                File.WriteAllText(path, content, encoding);

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
            contents = new String[0];
            try
            {
                File.WriteAllLines(path, contents);

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
            try
            {
                File.WriteAllLines(path, contents, encoding);


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

            //See VSwhidbey # 104086
            string strFile1 = Path.GetTempFileName();
            String strFile2 = Path.GetTempFileName();
            string[] strArr = new String[10];
            for (int i = 0; i < 10; i++)
                strArr[i] = "Line number...:" + i; ;

            TextWriter w = File.CreateText(strFile1);
            for (int i = 0; i < strArr.Length; i++)
                w.WriteLine(strArr[i]);
            w.Dispose();

            File.WriteAllLines(strFile2, strArr);

            if (new FileInfo(strFile1).Length != new FileInfo(strFile2).Length)
            {
                iCountErrors++;
                Console.WriteLine("Err_23rf! File length not equal possible BOM issue");
            }
            if (File.Exists(strFile1))
                File.Delete(strFile1);
            if (File.Exists(strFile2))
                File.Delete(strFile2);


            strFile1 = Path.GetTempFileName();
            strFile2 = Path.GetTempFileName();
            String str1 = "Line number...:1";

            w = File.CreateText(strFile1);
            w.Write(str1);
            w.Dispose();

            File.WriteAllText(strFile2, str1);

            if (new FileInfo(strFile1).Length != new FileInfo(strFile2).Length)
            {
                iCountErrors++;
                Console.WriteLine("Err_23rf! File length not equal possible BOM issue {0} {1}", new FileInfo(strFile1).Length, new FileInfo(strFile2).Length);
            }
            if (File.Exists(strFile1))
                File.Delete(strFile1);
            if (File.Exists(strFile2))
                File.Delete(strFile2);
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

