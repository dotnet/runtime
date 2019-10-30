// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace NetClient
{
    using System;
    using System.Threading;
    using System.Runtime.InteropServices;
    using TestLibrary;

    class Program
    {
        static int Main(string[] doNotUse)
        {
            // RegFree COM is not supported on Windows Nano
            if (TestLibrary.Utilities.IsWindowsNanoServer)
            {
                return 100;
            }

            try
            {
                RunTests();
                Console.WriteLine("Testing COM object lifetime control methods.");
                Thread.CurrentThread.DisableComObjectEagerCleanup();
                RunTests();
                Marshal.CleanupUnusedObjectsInCurrentContext();
                Assert.IsFalse(Marshal.AreComObjectsAvailableForCleanup());
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test Failure: {e}");
                return 101;
            }

            return 100;
        }

        private static void RunTests()
        {
            new NumericTests().Run();
            new ArrayTests().Run();
            new StringTests().Run();
            new ErrorTests().Run();
            new ColorTests().Run();
        }
    }
}
