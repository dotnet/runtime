// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace NetClient
{
    using System;
    using System.Threading;
    using System.Runtime.InteropServices;
    using Xunit;

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
                RunTests();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test object interop failure: {e}");
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
