// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace BuildDriver;

public class BuildDriver
{
    public static void RunProcess(ProcessStartInfo psi) => RunProcess(psi,
        new GlobalConfig() { Configuration = "Release", Architecture = "x64", Silent = false });
    protected static void RunProcess(ProcessStartInfo psi, GlobalConfig gConfig)
    {
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
                Console.WriteLine(proc.StandardOutput.ReadToEnd());
                Console.WriteLine(proc.StandardError.ReadToEnd());
                throw new Exception("Building Null GC failed!");
            }
        }
    }

}
