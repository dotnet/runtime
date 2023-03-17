// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace BuildDriver;

public class Utils
{
    public static string WinArchitecture(string arch) => arch.Equals("x86") ? "Win32" : "x64";
    public static void RunProcess(ProcessStartInfo psi) => RunProcess(psi,
        new GlobalConfig() { Configuration = "Release", Architecture = "x64", Silent = false });
    public static void RunProcess(ProcessStartInfo psi, GlobalConfig gConfig)
    {
        if (!gConfig.Silent)
            Console.WriteLine($"Running: {psi.FileName} {psi.Arguments}");
        using (Process proc = new())
        {
            proc.StartInfo = psi;
            proc.StartInfo.UseShellExecute = false;
            if (gConfig.Silent)
            {
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
            }

            proc.Start();

            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                if (gConfig.Silent)
                {
                    Console.WriteLine(proc.StandardOutput.ReadToEnd());
                    Console.WriteLine(proc.StandardError.ReadToEnd());
                }

                throw new Exception($"Running {psi.FileName} {psi.Arguments} failed!");
            }
        }
    }

}
