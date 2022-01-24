// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.NetworkInformation.Tests
{
    internal static class ProcessUtil
    {
        public static IEnumerable<string> GetProcessThreadsWithPsCommand(int pid)
        {
            ProcessStartInfo psi = new ProcessStartInfo("ps", $"-T --no-headers -p {pid}");
            psi.RedirectStandardOutput = true;

            using Process process = Process.Start(psi);
            if (process == null)
            {
                throw new Exception("Could not create process 'ps'");
            }

            string output = process.StandardOutput.ReadToEnd();
            if (process.ExitCode != 0)
            {
                throw new Exception($"Process 'ps' returned exit code {process.ExitCode}");
            }

            using StringReader sr = new StringReader(output);

            while (true)
            {
                string? line = sr.ReadLine();
                if (line == null)
                {
                    break;
                }

                yield return line;
            }
        }
    }
}