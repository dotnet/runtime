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

            // If requested, test multilevel lookup using fake Global SDK directories:
            //     1. using a fake ProgramFiles location
            //     2. using a fake SDK Self-Registered location
            // Note that this has to be set here and not in the calling test process because
            // %ProgramFiles% gets reset on process creation.
            string testMultilevelLookupProgramFiles = Environment.GetEnvironmentVariable("TEST_MULTILEVEL_LOOKUP_PROGRAM_FILES");
            string testMultilevelLookupSelfRegistered = Environment.GetEnvironmentVariable("TEST_MULTILEVEL_LOOKUP_SELF_REGISTERED");

            string hostfxrPath;
            if (testMultilevelLookupProgramFiles != null && testMultilevelLookupSelfRegistered != null)
            {
                Environment.SetEnvironmentVariable("_DOTNET_TEST_GLOBALLY_REGISTERED_PATH", testMultilevelLookupSelfRegistered);
                Environment.SetEnvironmentVariable("ProgramFiles", testMultilevelLookupProgramFiles);
                Environment.SetEnvironmentVariable("ProgramFiles(x86)", testMultilevelLookupProgramFiles);
                Environment.SetEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1");
                hostfxrPath = AppContext.GetData("HOSTFXR_PATH_TEST_BEHAVIOR") as string;
            }
            else
            {
                // never rely on machine state in test if we're not faking the multi-level lookup
                Environment.SetEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0");
                hostfxrPath = AppContext.GetData("HOSTFXR_PATH") as string;
            }

            if (hostfxrPath is not null)
            {
                NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, (libraryName, assembly, searchPath) =>
                {
                    return libraryName == nameof(HostFXR.hostfxr)
                        ? NativeLibrary.Load(libraryName, assembly, searchPath)
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
