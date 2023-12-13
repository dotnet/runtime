// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using BuildDriver;

var repoRoot = Paths.RepoRoot;

using (Process proc = new())
{
    proc.StartInfo = new ProcessStartInfo();
    proc.StartInfo.UseShellExecute = false;
    proc.StartInfo.RedirectStandardOutput = true;
    proc.StartInfo.RedirectStandardError = true;

    proc.StartInfo.FileName = "git";
    proc.StartInfo.Arguments = "status . --porcelain";
    proc.StartInfo.WorkingDirectory = repoRoot.Combine(".yamato");

    proc.Start();

    proc.WaitForExit();
    if (proc.ExitCode == 0)
    {
        var stdOut = proc.StandardOutput.ReadToEnd();
        if (!string.IsNullOrWhiteSpace(stdOut))
        {
            Console.WriteLine();
            Console.WriteLine($"Your repo is not clean - diff output:");
            Console.WriteLine();
            Console.WriteLine(stdOut);
            Console.WriteLine();
            Console.WriteLine("You must run recipe generation after updating recipes to update the generated YAML!");
            Console.WriteLine("Run <\"unity/CITools/generate-yamato\">");
            return 1;
        }
    }
    else
    {
        Console.WriteLine($"git status failed.");
        Console.WriteLine(proc.StandardOutput.ReadToEnd());
        Console.WriteLine(proc.StandardError.ReadToEnd());
        return 1;
    }
}

return 0;
