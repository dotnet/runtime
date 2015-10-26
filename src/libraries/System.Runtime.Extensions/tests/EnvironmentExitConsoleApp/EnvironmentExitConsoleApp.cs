// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
[assembly: System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]

namespace EnvironmentExitConsoleApp
{
    /// <summary>
    /// Provides an entry point in a new process that will load a specified method and invoke it.
    /// </summary>
    internal static class Program
    {
        static int Main(string[] args)
        {
            // The program expects to be passed the exit code that needs to be sent to the Environment.Exit call.
            if (args.Length > 1)
            {
                Console.Error.WriteLine("Usage: {0} exitCode", typeof(Program).GetTypeInfo().Assembly.GetName().Name);
                return -1;
            }

            Environment.Exit(Convert.ToInt32(args[0]));

            return -100;
        }
    }
}
