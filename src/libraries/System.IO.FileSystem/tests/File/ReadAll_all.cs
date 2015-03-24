// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text;
using System.Linq;
using Xunit;

public class File_ReadAll_all
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2002/09/30 11:31";
    public static String s_strClassMethod = "File.ReadAllText - All the methods";
    public static String s_strTFName = "ReadAll_all.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();


    [Fact]
    public static void runTest()
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";

        String path;
        Encoding encoding;
        String all;
        String[] allLines;
        StreamWriter writer;
        StringBuilder builder;
        BinaryWriter bin;
        FileStream stream;



        try
        {
            //This TestCase tests the following methods that have been added to File for RAD purposes. These methods are thin wrappers for already 
            //defined methods in the IO namespace. We do not aim to test the underlying APIs here

            //String ReadAllText(String path)
            //String ReadAllText(String path, Encoding encoding)
            //IEnumerable<String> ReadLines(String path)
            //IEnumerable<String> ReadLines(String path, Encoding encoding)

            //Argument 

            strLoc = "Loc_001oo";

            iCountTestcases++;

            path = null;
            try
            {
                all = File.ReadAllText(path);

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
                all = File.ReadAllText(path, encoding);

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
                all = File.ReadAllText(path, encoding);

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
            try
            {
                allLines = File.ReadLines(path).ToArray();

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
                allLines = File.ReadLines(path, encoding).ToArray();

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
                allLines = File.ReadLines(path, encoding).ToArray();

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


            //vanilla: Non existent paths

            strLoc = "Loc_002oo";

            iCountTestcases++;

            path = Path.GetTempFileName();
            if (File.Exists(path))
                File.Delete(path);

            try
            {
                all = File.ReadAllText(path);

                iCountErrors++;
                Console.WriteLine("Err_3486sgd! Expected Exception here.");
            }
            catch (FileNotFoundException)
            {
            }
            catch (Exception ex)
            {
                iCountErrors++;
                Console.WriteLine("Err_0347tsfg! Unexoected exception thrown; {0}", ex.GetType().Name);
            }

            encoding = Encoding.UTF8;
            try
            {
                all = File.ReadAllText(path, encoding);

                iCountErrors++;
                Console.WriteLine("Err_3486sgd! Expected Exception here.");
            }
            catch (FileNotFoundException)
            {
            }
            catch (Exception ex)
            {
                iCountErrors++;
                Console.WriteLine("Err_0347tsfg! Unexoected exception thrown; {0}", ex.GetType().Name);
            }

            try
            {
                allLines = File.ReadLines(path).ToArray();

                iCountErrors++;
                Console.WriteLine("Err_3486sgd! Expected Exception here.");
            }
            catch (FileNotFoundException)
            {
            }
            catch (Exception ex)
            {
                iCountErrors++;
                Console.WriteLine("Err_0347tsfg! Unexoected exception thrown; {0}", ex.GetType().Name);
            }

            encoding = Encoding.UTF8;
            try
            {
                allLines = File.ReadLines(path, encoding).ToArray();

                iCountErrors++;
                Console.WriteLine("Err_3486sgd! Expected Exception here.");
            }
            catch (FileNotFoundException)
            {
            }
            catch (Exception ex)
            {
                iCountErrors++;
                Console.WriteLine("Err_0347tsfg! Unexoected exception thrown; {0}", ex.GetType().Name);
            }



            //Vanilla - make sure that normal behavior is seen for all the methods

            strLoc = "Loc_003oo";

            iCountTestcases++;

            path = Path.GetTempFileName();
            stream = new FileStream(path, FileMode.Create);
            writer = new StreamWriter(stream);
            builder = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                writer.WriteLine(i.ToString());
                builder.Append(i.ToString() + Environment.NewLine);
            }
            writer.Dispose();
            stream.Dispose();

            all = File.ReadAllText(path);

            if (all != builder.ToString())
            {
                iCountErrors++;
                Console.WriteLine("Err_56sgf! Wrong result returned. {0}", all);
            }


            //This is an implicit test to ensure that the previous call released the file handle!
            all = File.ReadAllText(path, Encoding.GetEncoding("US-ASCII"));
            if (all != builder.ToString())
            {
                iCountErrors++;
                Console.WriteLine("Err_436sdg! Wrong result returned. {0}", all);
            }

            allLines = File.ReadLines(path).ToArray();
            for (int i = 0; i < 100; i++)
            {
                if (allLines[i] != i.ToString())
                {
                    iCountErrors++;
                    Console.WriteLine("Err_8364sg! Wrong result returned. {0}", allLines[i]);
                }
            }

            allLines = File.ReadLines(path, Encoding.GetEncoding("utf-7")).ToArray();
            for (int i = 0; i < 100; i++)
            {
                if (allLines[i] != i.ToString())
                {
                    iCountErrors++;
                    Console.WriteLine("Err_8364sg! Wrong result returned. {0}", allLines[i]);
                }
            }

            if (File.Exists(path))
                File.Delete(path);

            //Zero length, binary file tests

            strLoc = "Loc_03245oo";

            iCountTestcases++;

            path = Path.GetTempFileName();
            all = File.ReadAllText(path);
            if (all != String.Empty)
            {
                iCountErrors++;
                Console.WriteLine("Err_4328r6sgf! Wrong result returned. {0}", all);
            }


            all = File.ReadAllText(path, Encoding.GetEncoding("US-ASCII"));
            if (all != String.Empty)
            {
                iCountErrors++;
                Console.WriteLine("Err_4328r6sgf! Wrong result returned. {0}", all);
            }

            allLines = File.ReadLines(path).ToArray();
            if (allLines.Length != 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_3457tsg! Wrong result returned. {0}", allLines.Length);
            }

            allLines = File.ReadLines(path, Encoding.GetEncoding("US-ASCII")).ToArray();

            if (allLines.Length != 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_3457tsg! Wrong result returned. {0}", allLines.Length);
            }

            if (File.Exists(path))
                File.Delete(path);

            path = Path.GetTempFileName();

            bin = new BinaryWriter(new FileStream(path, FileMode.Open, FileAccess.Write));
            for (int i = 0; i < 50; i++)
            {
                bin.Write((byte)i);
            }
            bin.Dispose();

            all = File.ReadAllText(path);
            if (all.Length != 50)
            {
                iCountErrors++;
                Console.WriteLine("Err_3758tsg! Wrong value returned: {0}", all.Length);
            }

            all = File.ReadAllText(path, Encoding.Unicode);
            if (all.Length != 25)
            {
                iCountErrors++;
                Console.WriteLine("Err_3758tsg! Wrong value returned: {0}", all.Length);
            }

            allLines = File.ReadLines(path).ToArray();
            if (allLines.Length != 3)
            {
                iCountErrors++;
                Console.WriteLine("Err_3758tsg! Wrong value returned: {0}", allLines.Length);
            }

            if (File.Exists(path))
                File.Delete(path);

            //Open issues - file already opened, We've closed the file after read etc
            //That we are closing the file is imlictly tested above

            path = Path.GetTempFileName();
            stream = new FileStream(path, FileMode.Create);
            writer = new StreamWriter(stream);
            builder = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                writer.WriteLine(i.ToString());
            }

            try
            {
                all = File.ReadAllText(path);

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
                all = File.ReadAllText(path, encoding);

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
                allLines = File.ReadLines(path).ToArray();

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
                allLines = File.ReadLines(path, encoding).ToArray();


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

            writer.Dispose();
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

