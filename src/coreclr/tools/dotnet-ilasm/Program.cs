// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.NETCore.ILAsm
{
    internal sealed class Program
    {
        public static int Main(string[] args)
        {
            // Determine the location of the native ilasm executable
            string ilasmPath = GetIlasmPath();
            if (ilasmPath is null || !File.Exists(ilasmPath))
            {
                Console.Error.WriteLine("Error: Could not find the native ilasm executable.");
                return 1;
            }

            // Create a process to run the native ilasm
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ilasmPath,
                UseShellExecute = false
            };

            // Pass through all arguments
            foreach (string arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            try
            {
                using Process process = Process.Start(startInfo);
                if (process is null)
                {
                    Console.Error.WriteLine("Error: Failed to start ilasm process.");
                    return 1;
                }

                process.WaitForExit();
                return process.ExitCode;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static string GetIlasmPath()
        {
            // The native ilasm binary should be in the runtimes/<rid>/native directory
            // relative to the location of this assembly
            string assemblyDir = AppContext.BaseDirectory;
            if (string.IsNullOrEmpty(assemblyDir))
            {
                return null;
            }

            // Determine the RID
            string rid = GetRuntimeIdentifier();
            string exeSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;

            // Look for ilasm in runtimes/<rid>/native/
            string ilasmPath = Path.Combine(assemblyDir, "runtimes", rid, "native", $"ilasm{exeSuffix}");
            if (File.Exists(ilasmPath))
            {
                return ilasmPath;
            }

            // Fallback: look in other RID directories (in case exact match doesn't exist)
            string runtimesDir = Path.Combine(assemblyDir, "runtimes");
            if (Directory.Exists(runtimesDir))
            {
                foreach (string ridDir in Directory.GetDirectories(runtimesDir))
                {
                    string candidatePath = Path.Combine(ridDir, "native", $"ilasm{exeSuffix}");
                    if (File.Exists(candidatePath))
                    {
                        return candidatePath;
                    }
                }
            }

            // Fallback: look for ilasm in the same directory as the assembly
            ilasmPath = Path.Combine(assemblyDir, $"ilasm{exeSuffix}");
            if (File.Exists(ilasmPath))
            {
                return ilasmPath;
            }

            return null;
        }

        private static string GetRuntimeIdentifier()
        {
            // Determine the current runtime identifier
            string os;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                os = "win";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                os = "linux";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                os = "osx";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            {
                os = "freebsd";
            }
            else
            {
                // Fallback to linux for unknown Unix-like platforms
                os = "linux";
            }

            // Map architecture to RID format
            string arch = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm => "arm",
                Architecture.Arm64 => "arm64",
                Architecture.Wasm => "wasm",
                Architecture.S390x => "s390x",
                Architecture.LoongArch64 => "loongarch64",
                Architecture.Armv6 => "armv6",
                Architecture.Ppc64le => "ppc64le",
                Architecture.RiscV64 => "riscv64",
                _ => RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()
            };

            return $"{os}-{arch}";
        }
    }
}
