// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using TestLibrary;
using Xunit;

namespace CommandLineTest
{
    class Program
    {
        int Main()
        {
            // Currently native command line is only implemented on CoreCLR Windows
            if (!TestLibrary.Utilities.IsMonoRuntime && !TestLibrary.Utilities.IsNativeAot)
            {
                // Clear the command line args set for managed entry point
                var field = typeof(Environment).GetField("s_commandLineArgs", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.NotNull(field);
                field.SetValue(null, null);

                string[] args = Environment.GetCommandLineArgs();
                if (OperatingSystem.IsWindows())
                {
                    // The command line should be "corerun assemblyname.dll" for coreclr test
                    Assert.Equal(2, args.Length);
                    Assert.Equal("corerun", Path.GetFileNameWithoutExtension(args[0]));
                    Assert.Equal(typeof(Program).Assembly.GetName().Name, Path.GetFileNameWithoutExtension(args[1]));
                }
            }

            return 100;
        }
    }
}