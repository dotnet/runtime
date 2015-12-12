// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// GC.Collect in a handler might corrupt values in gc heap if gcinfo is not correct

using System;

namespace test2
{
    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    class Class1
    {
        private static TestUtil.TestLog testLog;

        static Class1()
        {
            // Create test writer object to hold expected output
            System.IO.StringWriter expectedOut = new System.IO.StringWriter();

            // Write expected output to string writer object
            expectedOut.WriteLine("In try");
            expectedOut.WriteLine("ar[0]=1");
            expectedOut.WriteLine("ar[1]=2");
            expectedOut.WriteLine("ar[2]=3");
            expectedOut.WriteLine("ar[3]=4");
            expectedOut.WriteLine("ar[4]=5");
            expectedOut.WriteLine("In catch");
            expectedOut.WriteLine("x = 0");
            expectedOut.WriteLine("ar[0]=1");
            expectedOut.WriteLine("ar[1]=2");
            expectedOut.WriteLine("ar[2]=3");
            expectedOut.WriteLine("ar[3]=4");
            expectedOut.WriteLine("ar[4]=5");

            // Create and initialize test log object
            testLog = new TestUtil.TestLog(expectedOut);
        }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static int Main(String[] args)
        {
            int[] ar = new int[] { 1, 2, 3, 4, 5 };

            //Start recording
            testLog.StartRecording();

            try
            {
                if (args.Length > 0)
                {
                    for (int i = 0; i < ar.Length; i++)
                    {
                        Console.WriteLine("ar[" + i + "]=" + ar[i]);
                    }
                }
                Console.WriteLine("In try");
                GC.Collect();
                GC.WaitForPendingFinalizers();
                for (int i = 0; i < ar.Length; i++)
                {
                    Console.WriteLine("ar[" + i + "]=" + ar[i]);
                }
                throw new Exception();
            }
            catch
            {

                Console.WriteLine("In catch");
                int x = new int();

                Console.WriteLine("x = {0}", x);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                for (int i = 0; i < ar.Length; i++)
                {
                    Console.WriteLine("ar[" + i + "]=" + ar[i]);
                }
                ar = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();

            }
            // stop recoding
            testLog.StopRecording();

            return testLog.VerifyOutput();
        }

    }
}
