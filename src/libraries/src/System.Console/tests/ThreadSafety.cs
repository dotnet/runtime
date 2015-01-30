// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using Xunit;

public class ThreadSafety
{
    static TextWriter writerOut;
    static TextWriter writerErr;
    static TextReader readerIn;

    static TextWriter orConOut = Console.Out;
    static TextWriter orConErr = Console.Error;
    static TextReader orConIn = Console.In;

    const char ReadValue = '5';
    const string ReadLineValue = "This is a test";

    // Suppress warning that stream1 is assigned but not used
    static Stream stream1;


    [Fact]
    public static void ConsoleStream_ThreadSafety()
    {
        try
        {
            OpenStandardXXX_Threading();
        }
        catch (Exception)
        {
            Console.SetIn(orConIn);
            Console.SetOut(orConOut);
            Console.SetError(orConErr);
        }
    }

    private static void OpenStandardXXX_Threading()
    {
        const int numberOfThreads = 20;

        int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";

        ThreadStart ts1;
        Thread[] pool1;

        try
        {
            TextWriter ConErr = orConErr;
            TextWriter ConOut = orConOut;
            TextReader ConIn = orConIn;

            strLoc = "Loc_002ab";

            iCountTestcases++;

            pool1 = new Thread[numberOfThreads];
            for (int i = 0; i < numberOfThreads; i++)
            {
                ts1 = new ThreadStart(WorkOnStdOpen);
                pool1[i] = new Thread(ts1);
            }

            //Start the threads and then wait for completion
            for (int i = 0; i < numberOfThreads; i++)
                pool1[i].Start();
            for (int i = 0; i < numberOfThreads; i++)
                pool1[i].Join();

            //We'll change the Set methods
            String outfileName = "output.txt";
            String errfileName = "error.txt";
            String infileName = "in.txt";

            StreamWriter tempWriter = File.CreateText(infileName);
            for (int i = 0; i < numberOfThreads; i++)
                tempWriter.Write(ReadValue);
            for (int i = 0; i < numberOfThreads; i++)
                tempWriter.WriteLine(ReadLineValue);
            tempWriter.Dispose();


            writerOut = File.CreateText(outfileName);
            writerErr = File.CreateText(errfileName);
            readerIn = File.OpenText(infileName);

            pool1 = new Thread[numberOfThreads];
            for (int i = 0; i < numberOfThreads; i++)
            {
                ts1 = new ThreadStart(SetStream);
                pool1[i] = new Thread(ts1);
            }

            for (int i = 0; i < numberOfThreads; i++)
                pool1[i].Start();

            for (int i = 0; i < numberOfThreads; i++)
                pool1[i].Join();

            //We'll test the Read method
            pool1 = new Thread[numberOfThreads];
            for (int i = 0; i < numberOfThreads; i++)
            {
                ts1 = new ThreadStart(ReadConsole);
                pool1[i] = new Thread(ts1);
            }

            for (int i = 0; i < numberOfThreads; i++)
                pool1[i].Start();

            for (int i = 0; i < numberOfThreads; i++)
                pool1[i].Join();


            writerOut.Dispose();
            writerErr.Dispose();
            readerIn.Dispose();

            if (File.Exists(outfileName))
                File.Delete(outfileName);
            if (File.Exists(errfileName))
                File.Delete(errfileName);
            if (File.Exists(infileName))
                File.Delete(infileName);

            Console.SetIn(ConIn);
            Console.SetOut(ConOut);
            Console.SetError(ConErr);
        }
        catch (Exception ex)
        {
            ++iCountErrors;
            Console.WriteLine(" Error!  strLoc==" + strLoc + ", exception==" + ex);
        }
        Assert.Equal(iCountErrors, 0);
    }

    private static void WorkOnStdOpen()
    {
        stream1 = Console.OpenStandardInput();
        stream1 = Console.OpenStandardOutput();
        stream1 = Console.OpenStandardError();
    }

    private static void SetStream()
    {
        Console.SetIn(readerIn);
        Console.SetOut(writerOut);
        Console.SetError(writerErr);
    }

    private static void ReadConsole()
    {
        Char ch1 = (Char)Console.Read();
        if (ch1 != ReadValue)
        {
            throw new Exception(String.Format("Err! Wrong value: <{0}><{1}>", ch1, ReadValue));
        }
    }
}