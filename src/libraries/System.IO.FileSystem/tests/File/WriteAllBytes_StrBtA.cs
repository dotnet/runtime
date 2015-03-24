// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text;
using Xunit;

public class File_WriteAllBytes_StrBtA
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer = "2002/10/01 08:59";
    public static String s_strClassMethod = "File.WriteAllBytes(String path, Byte[])";
    public static String s_strTFName = "WriteAllBytes_StrBtA.cs";
    public static String s_strTFPath = Directory.GetCurrentDirectory();


    [Fact]
    public static void runTest()
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";

        String path;
        Byte[] bits;
        Byte[] rbits;
        int count;
        //		FileStream stream;
        //		BinaryReader reader;


        try
        {
            //Argument 

            strLoc = "Loc_001oo";

            iCountTestcases++;

            path = null;
            bits = new Byte[0];
            try
            {
                File.WriteAllBytes(path, bits);

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
            bits = null;
            try
            {
                File.WriteAllBytes(path, bits);

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

            //Vanilla - make sure that the normal works!

            strLoc = "Loc_002oo";

            iCountTestcases++;

            path = Path.GetTempFileName();
            if (File.Exists(path))
                File.Delete(path);
            bits = new Byte[0];
            File.WriteAllBytes(path, bits);
            rbits = File.ReadAllBytes(path);
            if (rbits.Length != 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_3456gsd! Wrong value returned. {0}", rbits.Length);
            }

            if (File.Exists(path))
                File.Delete(path);

            path = Path.GetTempFileName();
            bits = new Byte[0];
            File.WriteAllBytes(path, bits);
            rbits = File.ReadAllBytes(path);
            if (rbits.Length != 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_3456gsd! Wrong value returned. {0}", rbits.Length);
            }
            count = 200;
            bits = new Byte[count];
            for (int i = 0; i < count; i++)
                bits[i] = (byte)i;
            File.WriteAllBytes(path, bits);
            rbits = File.ReadAllBytes(path);
            for (int i = 0; i < count; i++)
            {
                if (rbits[i] != i)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_93457gs_{0}! Wrong value returned. {1}", i, rbits[i]);
                }
            }

            //Writing again overwrites
            count = 20;
            bits = new Byte[count];
            for (int i = 100; i < 100 + count; i++)
                bits[i - 100] = (byte)i;
            File.WriteAllBytes(path, bits);
            rbits = File.ReadAllBytes(path);
            if (rbits.Length != count)
            {
                iCountErrors++;
                Console.WriteLine("Err_34sg! Wrong value returned. {0}", rbits.Length);
            }
            for (int i = 100; i < 100 + count; i++)
            {
                if (rbits[i - 100] != i)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_93457gs_{0}! Wrong value returned. {1}", i, rbits[i]);
                }
            }

            if (File.Exists(path))
                File.Delete(path);

            //large values
            path = Path.GetTempFileName();
            count = 1 << 16;
            while (count > 1000)
            {
                try
                {
                    bits = new Byte[count];
                    break;
                }
                catch (OutOfMemoryException)
                {
                    count /= 10;
                }
            }
            File.WriteAllBytes(path, bits);
            rbits = File.ReadAllBytes(path);
            if (rbits.Length != count)
            {
                iCountErrors++;
                Console.WriteLine("Err_34sg! Wrong value returned. {0}", rbits.Length);
            }
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

