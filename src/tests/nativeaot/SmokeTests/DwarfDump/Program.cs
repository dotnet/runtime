using System;
using System.Diagnostics;

public class Program
{
    public static int Main(string[] args)
    {
        if (!OperatingSystem.IsLinux())
        {
            // Linux-only test
            return 100;
        }

        var proc = Process.Start(new ProcessStartInfo
        {
            FileName = "/bin/sh",
            Arguments = $"-c \"llvm-dwarfdump --verify {Environment.ProcessPath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });

        if (proc is null)
        {
            Console.WriteLine("llvm-dwarfdump could not run");
            return 1;
        }

        // Just count the number of warnings and errors. There are so many right now that it's not worth enumerating the list
        const int ExpectedCount = 23126;
        int count = 0;
        string line;
        while ((line = proc.StandardOutput.ReadLine()) != null)
        {
            if (line.Contains("warning:") || line.Contains("error:"))
            {
                count++;
            }
        }
        proc.WaitForExit();
        if (count != ExpectedCount)
        {
            Console.WriteLine($"Found {count} warnings and errors, expected {ExpectedCount}");
            Console.WriteLine("This is likely a result of debug info changes. To see the new output, run the following command:");
            Console.WriteLine("\tllvm-dwarfdump --verify " + Environment.ProcessPath);
            return 1;
        }
        return 100;
    }
}