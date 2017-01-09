// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.Deb.Tool
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var processInfo = new ProcessStartInfo()
            {
                FileName = Path.Combine(Path.GetDirectoryName(typeof(Program).GetTypeInfo().Assembly.Location), "tool", "package_tool"),
                Arguments = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(args),
                UseShellExecute = false
            };

            Console.WriteLine($"dotnet-deb-tool: executing - {processInfo.FileName} {processInfo.Arguments}");

            var process = new Process()
            {
                StartInfo = processInfo
            };

            process.Start();
            process.WaitForExit();

            return process.ExitCode;
        }
    }
}
