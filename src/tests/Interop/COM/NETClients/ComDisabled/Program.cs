// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace NetClient
{
    using System;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;

    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            // RegFree COM is not supported on Windows Nano
            if (TestLibrary.Utilities.IsWindowsNanoServer)
            {
                return 100;
            }

            try
            {
                var server = new Server.Contract.Servers.NumericTesting();
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
