// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel
{
    internal static class HostModelUtils
    {
        private const string CodesignPath = @"/usr/bin/codesign";

        public static bool IsCodesignAvailable() => File.Exists(CodesignPath);

        public static (int ExitCode, string StdErr) RunCodesign(string args, string appHostPath)
        {
            Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
            Debug.Assert(IsCodesignAvailable());

            var psi = new ProcessStartInfo()
            {
                Arguments = $"{args} \"{appHostPath}\"",
                FileName = CodesignPath,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using (var p = Process.Start(psi))
            {
                p.WaitForExit();
                return (p.ExitCode, p.StandardError.ReadToEnd());
            }
        }
    }
}