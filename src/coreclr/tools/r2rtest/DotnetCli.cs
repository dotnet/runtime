// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace R2RTest
{
    /// <summary>
    /// Helpers to call dotnet CLI via Process.Start
    /// </summary>
    static class DotnetCli
    {
        // Default 30s timeout for CLI commands
        private const int DotnetCliTimeout = 30 * 1000;

        public static int New(string workingDirectory, string projectType, StreamWriter logWriter)
        {
            return RunProcess("dotnet", $"new {projectType}", workingDirectory, DotnetCliTimeout, logWriter);
        }

        public static int AddPackage(string workingDirectory, string packageName, StreamWriter logWriter)
        {
            return RunProcess("dotnet", $"add package {packageName}", workingDirectory, DotnetCliTimeout, logWriter);
        }

        public static int Publish(string workingDirectory, StreamWriter logWriter)
        {
            return RunProcess("dotnet", "publish", workingDirectory, DotnetCliTimeout, logWriter);
        }

        private static int RunProcess(string processPath, string arguments, string workingDirectory, int timeout, StreamWriter logWriter)
        {
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = processPath,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process p = new Process();
            p.StartInfo = psi;
            p.OutputDataReceived += new DataReceivedEventHandler((sender, eventArgs) =>
            {
                if (!string.IsNullOrEmpty(eventArgs.Data))
                {
                    logWriter.WriteLine(eventArgs.Data);
                }
            });

            p.ErrorDataReceived += new DataReceivedEventHandler((sender, eventArgs) =>
            {
                if (!string.IsNullOrEmpty(eventArgs.Data))
                {
                    logWriter.WriteLine(eventArgs.Data);
                }
            });

            p.Start();
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();

            if (p.WaitForExit(timeout))
            {
                return p.ExitCode;
            }
            else
            {
                try
                {
                    p.Kill();
                }
                catch (Exception)
                {
                    // Silently ignore exceptions during this call to Kill as
                    // the process may have exited in the meantime.
                }
                logWriter.WriteLine($"{processPath} {arguments} timed out after {timeout}ms.");
                return 1;
            }
        }
    }
}
