// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Allocate nested objects of ~ 25 MB
// If memory is low, after every loop, the large objects should be collected
// and committed from the LargeObjectHeap
// The Finalizer makes sure that the GC is actually collecting the large objects


using System;

namespace LargeObjectTest
{
    public class OtherLargeObject
    {
        // disabling unused variable warning
#pragma warning disable 0414
        private int[] _otherarray;
#pragma warning restore 0414

        public OtherLargeObject()
        {
            _otherarray = new int[5000]; // 20 KB
        }
    }

    public class LargeObject
    {
        // disabling unused variable warning
#pragma warning disable 0414
        private int[] _array;
#pragma warning restore 0414
        private OtherLargeObject[] _olargeobj;

        public LargeObject()
        {
            _array = new int[1250000]; // 5 MB
            _olargeobj = new OtherLargeObject[1000];     //20 MB
            for (int i = 0; i < 1000; i++)
            {
                _olargeobj[i] = new OtherLargeObject();
            }
        }

        ~LargeObject()
        {
            TestLibrary.Logging.WriteLine("In finalizer");
            Test.ExitCode = 100;
        }
    }

    public class Test
    {
        public static int ExitCode = -1;

        public static int Main()
        {
            int loop = 0;
            LargeObject largeobj;

            TestLibrary.Logging.WriteLine("Test should pass with ExitCode 100\n");


            while (loop <= 200)
            {
                loop++;
                TestLibrary.Logging.Write(String.Format("LOOP: {0}\n", loop));
                try
                {
                    largeobj = new LargeObject();
                    TestLibrary.Logging.WriteLine("Allocated LargeObject");
                }
                catch (Exception e)
                {
                    TestLibrary.Logging.WriteLine("Failure to allocate at loop {0}\n", loop);
                    TestLibrary.Logging.WriteLine("Caught Exception: {0}", e);
                    return 1;
                }
                largeobj = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                TestLibrary.Logging.WriteLine("LargeObject Collected\n");
            }
            TestLibrary.Logging.WriteLine("Test Passed");
            GC.Collect();

            return ExitCode;
        }
    }
}
