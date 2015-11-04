// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

public class Environment_Exit
{
    [Fact]
    public static void CheckExitCode()
    {
        RunRemote(0);
        RunRemote(1);
        RunRemote(42);
        RunRemote(-1);
        RunRemote(-45);
    }

    private static void RunRemote(int arg)
    {
        using (Process p = new Process())
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "corerun";
                psi.Arguments = "EnvironmentExitConsoleApp.exe " + Convert.ToString(arg);
                psi.UseShellExecute = false;

                // Profilers / code coverage tools doing coverage of the test process set environment
                // variables to tell the targeted process what profiler to load.  We don't want the child process 
                // to be profiled / have code coverage, so we remove these environment variables for that process 
                // before it's started.
                psi.Environment.Remove("Cor_Profiler");
                psi.Environment.Remove("Cor_Enable_Profiling");
                psi.Environment.Remove("CoreClr_Profiler");
                psi.Environment.Remove("CoreClr_Enable_Profiling");

                p.StartInfo = psi;
                p.Start();
                Assert.True(p.WaitForExit(30 * 1000));

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Assert.Equal(arg, p.ExitCode);
                }
                else
                {
                    Assert.True(arg == p.ExitCode || arg == Convert.ToInt32(unchecked((sbyte)(p.ExitCode))));
                }

            }
            finally
            {
                // Cleanup
                try { p.Kill(); }
                catch { } // ignore all cleanup errors
            }
        }
    }
}
