// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.DotNet.CoreSetup
{
    public class Codesign
    {
        private const string CodesignPath = "/usr/bin/codesign";

        public static bool IsAvailable => File.Exists(CodesignPath);

        public static (int ExitCode, string StdErr) Run(string args, string binaryPath)
        {
            Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
            Debug.Assert(IsAvailable);

            ProcessStartInfo psi = new()
            {
                Arguments = $"{args} \"{binaryPath}\"",
                FileName = CodesignPath,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using (var p = Process.Start(psi))
            {
                if (p == null)
                    return (-1, "Failed to start process");

                var stderrRead = p.StandardError.ReadToEndAsync();
                var processWait = p.WaitForExitAsync();
                Task.WaitAll(stderrRead, processWait);
                return (p.ExitCode, stderrRead.Result);
            }
        }
    }
}
