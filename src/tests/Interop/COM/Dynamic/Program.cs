// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace Dynamic
{
    using System;
    using TestLibrary;
    using Xunit;

    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            // RegFree COM is not supported on Windows Nano
            if (Utilities.IsWindowsNanoServer)
            {
                return 100;
            }

            try
            {
                new BasicTest().Run();
                new CollectionTest().Run();
                new EventTest().Run();
                new ParametersTest().Run();
                new NETServerTest().Run();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test Failure: {e}");
                return 101;
            }

            return 100;
        }
    }
}
