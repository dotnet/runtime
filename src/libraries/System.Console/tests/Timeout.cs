// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

//
// System.Console BCL test cases
//
public class TimeOut
{
    [Fact]
    public static void OpenStandardXXX_WriteTimeOut()
    {
        Console.WriteLine("Testing WriteTimeout with OpenStandardXXX");
        Stream[] streams = new Stream[] { Console.OpenStandardOutput(), Console.OpenStandardError(), Console.OpenStandardInput() };
        int index = 0;
        foreach (Stream stream in streams)
        {
            Console.WriteLine(String.Format("Testing stream {0}({1})", stream, index++));
            TestConsoleStream_WriteTimeOut(stream);
        }
    }

    [Fact]
    public static void OpenStandardXXX_ReadTimeOut()
    {
        Console.WriteLine("Testing WriteTimeout with OpenStandardXXX");
        Stream[] streams = new Stream[] { Console.OpenStandardOutput(), Console.OpenStandardError(), Console.OpenStandardInput() };
        int index = 0;
        foreach (Stream stream in streams)
        {
            Console.WriteLine(String.Format("Testing stream {0}({1})", stream, index++));
            TestConsoleStream_ReadTimeOut(stream);
        }
    }

    [Fact]
    public static void OpenStandardXXX_CanTimeOut()
    {
        Console.WriteLine("Testing WriteTimeout with OpenStandardXXX");
        Stream[] streams = new Stream[] { Console.OpenStandardOutput(), Console.OpenStandardError(), Console.OpenStandardInput() };
        int index = 0;
        foreach (Stream stream in streams)
        {
            Console.WriteLine(String.Format("Testing stream {0}({1})", stream, index++));
            TestConsoleStream_CanTimeOut(stream);
        }
    }

    private static void TestConsoleStream_WriteTimeOut(Stream stream)
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;

        try
        {
            iCountTestcases++;

            try
            {
                int WriteTimeout = stream.WriteTimeout;

                iCountErrors++;
                Console.WriteLine("Err! Expected getting WriteTimeout to throw InvalidOperationException");
            }
            catch (InvalidOperationException) { }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine(String.Format("Err! Expected getting WriteTimeout to throw InvalidOperationException and the following exception was thrown:\n{0}",
                    e));
            }
            iCountTestcases++;

            try
            {
                stream.WriteTimeout = 500;

                iCountErrors++;
                Console.WriteLine("Err! Expected setting WriteTimeout to throw InvalidOperationException");
            }
            catch (InvalidOperationException) { }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine(String.Format("Err! Expected setting WriteTimeout to throw InvalidOperationException and the following exception was thrown:\n{0} stream",
                    e));
            }
        }
        catch (Exception ex)
        {
            ++iCountErrors;
            Console.WriteLine("Unexpected exception" + ex.ToString());
        }

        ////  Finish Diagnostics

        if (iCountErrors == 0)
        {
            Console.WriteLine("Test passed");
        }
        else
        {
            Console.WriteLine();
            Assert.True(false, String.Format("Failed test count:" + iCountErrors));
        }
    }

    private static void TestConsoleStream_ReadTimeOut(Stream stream)
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;
        try
        {
            iCountTestcases++;

            try
            {
                int readTimeout = stream.ReadTimeout;

                iCountErrors++;
                Console.WriteLine("Err! Expected getting ReadTimeout to throw InvalidOperationException");
            }
            catch (InvalidOperationException) { }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine(String.Format("Err! Expected getting ReadTimeout to throw InvalidOperationException and the following exception was thrown:\n{0",
                    e));
            }

            iCountTestcases++;

            try
            {
                stream.ReadTimeout = 500;

                iCountErrors++;
                Console.WriteLine("Err! Expected setting ReadTimeout to throw InvalidOperationException");
            }
            catch (InvalidOperationException) { }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine(String.Format("Err! Expected setting ReadTimeout to throw InvalidOperationException and the following exception was thrown:\n{0}",
                    e));
            }
        }
        catch (Exception ex)
        {
            ++iCountErrors;
            Console.WriteLine("Unexpected exception" + ex.ToString());
        }

        ////  Finish Diagnostics

        if (iCountErrors == 0)
        {
            Console.WriteLine("Test Passed");
        }
        else
        {
            Assert.True(false, String.Format("Test Failed with errorCount:" + iCountErrors));
        }
    }

    private static void TestConsoleStream_CanTimeOut(Stream stream)
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;
        try
        {
            iCountTestcases++;

            if (stream.CanTimeout)
            {
                iCountErrors++;
                Console.WriteLine("Err! Expected CanTimeout to return true");
            }
        }
        catch (Exception ex)
        {
            ++iCountErrors;
            Console.WriteLine("Unexpected exception" + ex);
        }
        if (iCountErrors == 0)
        {
            Console.WriteLine("Test passed");
        }
        else
        {
            Assert.True(false, String.Format("Test failed with errors:{0}", iCountErrors));
        }
    }
}
