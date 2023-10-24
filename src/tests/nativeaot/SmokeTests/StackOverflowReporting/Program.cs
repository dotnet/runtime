using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class StackOverflowReporting
{
    public static int Main(string[] args)
    {
        if (args.Length == 1 && args[0].ToLowerInvariant() == "recurse")
        {
            Recursion(0);
            return 0;
        }
        else
        {
            var directory = System.AppContext.BaseDirectory;
            var exe = System.IO.Path.Combine(directory, "StackOverflowReporting");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                exe += ".exe";
            }
            exe = Process.GetCurrentProcess().MainModule.FileName;
            using (Process p = new Process())
            {
                p.StartInfo.FileName = exe;
                p.StartInfo.Arguments = "recurse";
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.Start();
                p.WaitForExit();
                var output = p.StandardOutput.ReadToEnd();
                var error = p.StandardError.ReadToEnd();
                if (output.Contains("StackOverflowException") || error.Contains("StackOverflowException"))
                {
                    return 100;
                }
                else
                {
                    return 1;
                }
            }
        }
    }

    public static void Recursion(int i)
    {
        Recursion(i + 1);
        Recursion(i + 1);
    }
}
