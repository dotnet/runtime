//
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Microsoft.DotNet.CoreSetup
{
    public class Codesign
    {
        private const string CodesignPath = @"/usr/bin/codesign";

        public static bool IsAvailable() => File.Exists(CodesignPath);

        public static (int ExitCode, string StdErr) Run(string args, string appHostPath)
        {
            Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
            Debug.Assert(IsAvailable());

            var psi = new ProcessStartInfo()
            {
                Arguments = $"{args} \"{appHostPath}\"",
                FileName = CodesignPath,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using (var p = Process.Start(psi))
            {
                if (p == null)
                    return (-1, "Failed to start process");
                p.WaitForExit();
                return (p.ExitCode, p.StandardError.ReadToEnd());
            }
        }
    }
}
