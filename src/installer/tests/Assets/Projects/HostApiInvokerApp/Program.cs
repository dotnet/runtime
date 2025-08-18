// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace HostApiInvokerApp
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            // write exception details to stdout so that they can be seen in test assertion failures.
            try
            {
                MainCore(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return -1;
            }

            return 0;
        }

        public static void MainCore(string[] args)
        {
            if (args.Length == 0)
                throw new Exception($"{nameof(HostApiInvokerApp)} requires at least one argument specifying the API to test.");

            Console.WriteLine("Arguments:");
            foreach (string arg in args)
                Console.WriteLine($"  {arg}");

            string hostfxrPath = AppContext.GetData("HOSTFXR_PATH") as string;
            if (hostfxrPath is not null)
            {
                Console.WriteLine($"Registering DLLImportResolver for {nameof(HostFXR.hostfxr)} -> {hostfxrPath}");
                NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, (libraryName, assembly, searchPath) =>
                {
                    return libraryName == nameof(HostFXR.hostfxr)
                        ? NativeLibrary.Load(hostfxrPath, assembly, searchPath)
                        : default;
                });
            }

            string apiToTest = args[0];
            if (HostFXR.RunTest(apiToTest, args[1..]))
                return;

            if (HostPolicy.RunTest(apiToTest, args[1..]))
                return;

            if (HostRuntimeContract.RunTest(apiToTest, args[1..]))
                return;

            throw new ArgumentException($"Invalid API to test passed as args[0]): {apiToTest}");
        }
    }
}
