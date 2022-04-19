// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// 119053
// rethrow in a handler will not work properly if the protected block is protected by other 
// clauses that catch the base class of the exception being rethrown
using System;
using System.IO;
using Xunit;

namespace hello_rethrowwithhandlerscatchingbase_rethrow_cs
{
    public class Class1
    {
        private static TestUtil.TestLog testLog;

        static Class1()
        {
            // Create test writer object to hold expected output
            System.IO.StringWriter expectedOut = new System.IO.StringWriter();

            // Write expected output to string writer object
            expectedOut.WriteLine("In catch 1 File x not found");
            expectedOut.WriteLine("In main's catch File x not found");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        static public void f2()
        {
            throw new System.IO.FileNotFoundException("File x not found");
        }

        static public void f1()
        {
            try
            {
                f2();
            }
            catch (System.IO.FileNotFoundException e)
            {
                Console.WriteLine("In catch 1 " + e.Message);
                throw;
            }
            catch (System.IO.IOException e)
            {
                Console.WriteLine("In catch 2 " + e.Message);
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine("In catch 3 " + e.Message);
                throw;
            }
        }

        [Fact]
        static public int TestEntryPoint()
        {
            //Start recording
            testLog.StartRecording();

            try
            {
                f1();
            }
            catch (Exception e)
            {
                Console.WriteLine("In main's catch " + e.Message);
            }

            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }
    }

}

