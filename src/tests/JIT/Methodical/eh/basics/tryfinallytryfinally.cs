// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace hello_tryfinallytryfinally_basics_cs
{
    public class Class1
    {
        private static TestUtil.TestLog testLog;

        static Class1()
        {
            // Create test writer object to hold expected output
            System.IO.StringWriter expectedOut = new System.IO.StringWriter();

            // Write expected output to string writer object
            expectedOut.WriteLine("In try");
            expectedOut.WriteLine("In finally");
            expectedOut.WriteLine("In try");
            expectedOut.WriteLine("In finally");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }

        [Fact]
        [OuterLoop]
        static public int TestEntryPoint()
        {
            //Start recording
            testLog.StartRecording();

            try
            {
                Console.WriteLine("In try");
            }
            finally
            {
                Console.WriteLine("In finally");
            }
            try
            {
                Console.WriteLine("In try");
            }
            finally
            {
                Console.WriteLine("In finally");
            }

            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }
    }

    public class NestedFinallyGc
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Test(bool throwException)
        {
            int[] values = new int[1];
            bool caught = false;
            try
            {
                Run(values, throwException);
            }
            catch (InvalidOperationException)
            {
                caught = true;
            }

            Assert.Equal(throwException, caught);
            Assert.Equal(111, values[0]);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Run(int[] values, bool throwException)
        {
            try
            {
                values[0] = 1;
                if (throwException)
                    throw new InvalidOperationException();
            }
            finally
            {
                try
                {
                    Update(values);
                }
                finally
                {
                    // This finally is called normally, including during exceptional entry to the outer finally.
                    GC.Collect();
                    values[0] += 100;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Update(int[] values)
        {
            values[0] += 10;
        }
    }
}
