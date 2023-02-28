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
            Console.WriteLine("Hello World!");
            Console.WriteLine(string.Join(Environment.NewLine, args));

            // Enable tracing so that test assertion failures are easier to diagnose.
            Environment.SetEnvironmentVariable("COREHOST_TRACE", "1");

            // If requested, test multilevel lookup using fake Global SDK directories:
            //     1. using a fake ProgramFiles location
            //     2. using a fake SDK Self-Registered location
            // Note that this has to be set here and not in the calling test process because
            // %ProgramFiles% gets reset on process creation.
            string testMultilevelLookupProgramFiles = Environment.GetEnvironmentVariable("TEST_MULTILEVEL_LOOKUP_PROGRAM_FILES");
            string testMultilevelLookupSelfRegistered = Environment.GetEnvironmentVariable("TEST_MULTILEVEL_LOOKUP_SELF_REGISTERED");

            if (testMultilevelLookupProgramFiles != null && testMultilevelLookupSelfRegistered != null)
            {
                Environment.SetEnvironmentVariable("_DOTNET_TEST_GLOBALLY_REGISTERED_PATH", testMultilevelLookupSelfRegistered);
                Environment.SetEnvironmentVariable("ProgramFiles", testMultilevelLookupProgramFiles);
                Environment.SetEnvironmentVariable("ProgramFiles(x86)", testMultilevelLookupProgramFiles);
                Environment.SetEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1");
            }
            else
            {
                // never rely on machine state in test if we're not faking the multi-level lookup
                Environment.SetEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0");
            }

            if (args.Length == 0)
            {
                throw new Exception("Invalid number of arguments passed");
            }

            string apiToTest = args[0];
            if (HostFXR.RunTest(apiToTest, args))
                return;

            if (HostPolicy.RunTest(apiToTest, args))
                return;

            if (HostRuntimeContract.RunTest(apiToTest, args))
                return;

            throw new ArgumentException($"Invalid API to test passed as args[0]): {apiToTest}");
        }
    }
}
