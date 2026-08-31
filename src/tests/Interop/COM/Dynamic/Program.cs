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
        [SkipOnCoreClr("This test is very slow under some GC stress variations, especially with DOTNET_HeapVerify=1, and can time out in CI. See https://github.com/dotnet/runtime/issues/39584.", RuntimeTestModes.AnyGCStress)]
        [Fact]
        [Xunit.SkipOnCoreClrAttribute("Depends on COM behavior that is not correct in interpreter", RuntimeTestModes.InterpreterActive)]
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
