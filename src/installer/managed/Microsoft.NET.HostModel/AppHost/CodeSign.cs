// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.AppHost
{
    public static class CodeSign
    {
        private const string CodeSignPath = @"/usr/bin/codesign";

        /// <summary>
        /// Sign the app binary using codesign with an anonymous certificate.
        /// </summary>
        public static void CodeSignAppHost(string appHostPath)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return;
            }

            if (!File.Exists(CodeSignPath))
                return;

            var psi = new ProcessStartInfo()
            {
                Arguments = $"-s - \"{appHostPath}\"",
                FileName = CodeSignPath,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using (var p = Process.Start(psi))
            {
                p.WaitForExit();
                if (p.ExitCode != 0)
                    throw new AppHostSigningException(p.ExitCode, p.StandardError.ReadToEnd());
            }
        }
    }
}
