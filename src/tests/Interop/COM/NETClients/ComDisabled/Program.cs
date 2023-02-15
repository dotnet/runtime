// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace NetClient
{
    using System;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;

    class Program
    {
        static int Main()
        {
            // Force this test to timeout by having it sleep for an hour.
            Thread.Sleep(new TimeSpan(1, 0, 0));
            // RegFree COM is not supported on Windows Nano
            if (TestLibrary.Utilities.IsWindowsNanoServer)
            {
                return 100;
            }

            try
            {
                var server = (Server.Contract.Servers.NumericTesting)new Server.Contract.Servers.NumericTestingClass();
            }
            catch (NotSupportedException) when (OperatingSystem.IsWindows())
            {
                return 100;
            }
            catch (PlatformNotSupportedException) when (!OperatingSystem.IsWindows())
            {
                return 100;
            }

            return 101;
        }
    }
}
