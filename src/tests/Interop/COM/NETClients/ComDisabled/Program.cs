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
        static int Main(string[] doNotUse)
        {
            // RegFree COM is not supported on Windows Nano
            if (TestLibrary.Utilities.IsWindowsNanoServer)
            {
                return 100;
            }

            try
            {
                ActivateServer();
            }
            catch (PlatformNotSupportedException ex)
            {
                return 100;
            }

            return 101;
        }

        // Mark as NoInlining to make sure the failure is observed while running Main,
        // not while JITing Main and trying to resolve the target of the constructor call.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ActivateServer()
        {
            var server = (Server.Contract.Servers.NumericTesting)new Server.Contract.Servers.NumericTestingClass();
        }
    }
}
