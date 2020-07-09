// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.Deb.Tool
{
    public class Program
    {
        public static int Main(string[] args)
        {
            string packageToolPath = Path.Combine(Path.GetDirectoryName(typeof(Program).GetTypeInfo().Assembly.Location), "tool", "package_tool");
            EnsureExecutable(packageToolPath);

            var processInfo = new ProcessStartInfo()
            {
                FileName = packageToolPath,
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

        private static void EnsureExecutable(string filePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }
            // Executable files don't get the 'x' permission when restored from
            // NuGet packages.
            // See (https://github.com/NuGet/Home/issues/4424)
            // On .NET Core 1.x, all extracted files had default permissions of 766.
            // The default on .NET Core 2.x has changed to 666.
            // We force the .NET Core 1.x default permissions, to ensure we can execute the files.
            try
            {
                chmod(filePath, 0x1f6); // 0766
            }
            catch {} // if we can't set the permssion, just ignore it and try to run the file
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);
    }
}
