// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// Re-throwing an exception from finally block enclosed within a catch block.
// NDPWhidbey 10965
//
// 06/18/03 - SB
// Due to compiler error CS0724: A throw statement with no arguments is not allowed inside of a finally clause nested inside of the innermost catch clause,
// I'm modifying the test to explicitly throw 'eo'. An IL test will be added to test the original functionality.
//

using System;
using Xunit;

namespace Test_rethrowinfinallyaftercatch_cs
{
    public class Class1
    {
        private static TestUtil.TestLog testLog;

        static Class1()
        {
            // Create test writer object to hold expected output
            System.IO.StringWriter expectedOut = new System.IO.StringWriter();

            // Write expected output to string writer object
            expectedOut.WriteLine("try");
            expectedOut.WriteLine("\ttry - throwing outer exception");
            expectedOut.WriteLine("\tcatch - Outer Exception");
            expectedOut.WriteLine("\t\ttry - throwing inner exception");
            expectedOut.WriteLine("\t\tcatch - Inner Exception");
            expectedOut.WriteLine("\t\tfinally - Rethrowing Outer Exception");
            expectedOut.WriteLine("catch - Outer Exception");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        [Fact]
        static public int TestEntryPoint()
        {
            //Start recording
            testLog.StartRecording();

            try
            {
                Console.WriteLine("try");
                try
                {
                    Console.WriteLine("\ttry - throwing outer exception");
                    throw new Exception("Outer Exception");
                }
                catch (System.Exception eo)
                {
                    Console.WriteLine("\tcatch - " + eo.Message);
                    try
                    {
                        Console.WriteLine("\t\ttry - throwing inner exception");
                        throw new Exception("Inner Exception");
                    }
                    catch (System.Exception ei)
                    {
                        Console.WriteLine("\t\tcatch - " + ei.Message);
                    }
                    finally
                    {
                        Console.WriteLine("\t\tfinally - Rethrowing Outer Exception");
                        // excplicitly added 'eo' so that the CS compiler wouldn't complain.
                        throw eo;
                    }
                }
            }
            catch (System.Exception e)
            {
                Console.WriteLine("catch - " + e.Message);
            }

            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }
    }
}
