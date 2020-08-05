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

            // COM is not supported off Windows.
            // However, we want to test non-Windows crossgen on vtable gap members
            // so we still build and run this test on non-Windows so the crossgen test
            // runs exercise that path.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return 100;
            }

            try
            {
                RunTests();
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
            new ErrorTests().Run();
        }
    }
}
