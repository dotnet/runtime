
using System;
using Xunit.Abstractions;

internal class PrintTopHelper
{
    public static void PrintOutputOfTopCommand(ITestOutputHelper output)
    {
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            // grab the output of the `top` command to get the memory usage
            // and CPU usage of the process
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "top",
                    Arguments = "-b -n 1",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string outputText = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            output.WriteLine(outputText);
        }
    }
}