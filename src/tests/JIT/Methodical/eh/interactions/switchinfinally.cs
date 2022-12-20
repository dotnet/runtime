// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// switch statement in a finally 

using System;
using Xunit;

namespace test3
{
    class MyClass
    {
        int m_i;
        public MyClass(int i)
        {
            m_i = i;
            Console.WriteLine("In MyClass");
        }

        public int val
        {
            get
            {
                return m_i;
            }
            set
            {
                m_i = value;
            }
        }


        public void testit(int i)
        {
            try
            {

            }
            finally
            {
                switch (i)
                {
                    case 0:
                        Console.WriteLine("In finally");
                        break;
                    case 1:
                        Console.WriteLine("Wrong");
                        goto endfin;
                        break;
                    default:
                        Console.WriteLine("bad");
                        throw new Exception();
                }
                Console.WriteLine("Still in finally\n");
                endfin:
                Console.WriteLine("Exitting finally\n");
            }
            done:
            Console.WriteLine("done testing");
        }
    }
    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    public class Class1
    {
        private static TestUtil.TestLog testLog;

        static Class1()
        {
            // Create test writer object to hold expected output
            System.IO.StringWriter expectedOut = new System.IO.StringWriter();

            // Write expected output to string writer object
            expectedOut.WriteLine("In MyClass");
            expectedOut.WriteLine("In finally");
            expectedOut.WriteLine("Still in finally\n");

            expectedOut.WriteLine("Exitting finally\n");

            expectedOut.WriteLine("done testing");
            expectedOut.WriteLine("1234");
            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [Fact]
        public static int TestEntryPoint()
        {
            //Start recording
            testLog.StartRecording();
            MyClass x = new MyClass(1234);
            x.testit(0);
            Console.WriteLine(x.val);

            // stop recoding
            testLog.StopRecording();
            return testLog.VerifyOutput();
        }
    }
}
