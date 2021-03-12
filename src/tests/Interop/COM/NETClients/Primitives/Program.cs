// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                Console.WriteLine(
@"=========================
Testing COM object interop.
=========================");
                RunTests();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test object interop failure: {e}");
                return 101;
            }

            try
            {
                Console.WriteLine(
@"
=========================
Testing COM object lifetime control methods.
=========================");
                Thread.CurrentThread.DisableComObjectEagerCleanup();
                RunTests();

                // Force GC to ensure all RCWs have been considered and are ready to be cleaned up.
                ForceGC();
                Marshal.CleanupUnusedObjectsInCurrentContext();
                Assert.IsFalse(Marshal.AreComObjectsAvailableForCleanup());
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test lifetime control failure: {e}");
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
