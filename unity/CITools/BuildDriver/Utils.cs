// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using NiceIO;

namespace BuildDriver;

public class Utils
{
    public static string UnityEmbedHostTfmDirectoryName(GlobalConfig gConfig)
        => Paths.UnityEmbedHost.Combine("bin", gConfig.Configuration).Directories().Single().FileName;

    public static NPath RuntimeArtifactDirectory(GlobalConfig gConfig)
        => Paths.RepoRoot.Combine("artifacts", "bin",
            $"microsoft.netcore.app.runtime.{Paths.ShortPlatformNameInPaths}-{gConfig.Architecture}", gConfig.Configuration, "runtimes",
            $"{Paths.ShortPlatformNameInPaths}-{gConfig.Architecture}");

    public static NPath UnityTestHostDotNetRoot(GlobalConfig gConfig)
        // Find the directory "net7.0-windows-Release-x64" without hard coding the tfm
        => Paths.RepoRoot.Combine("artifacts/bin/testhost")
            .DirectoryMustExist()
            .Directories($"*-{Paths.FullPlatformNameInPaths}-{gConfig.Configuration}-{gConfig.Architecture}")
            .Single();

    public static NPath UnityTestHostDotNetAppDirectory(GlobalConfig gConfig)
        => UnityTestHostDotNetRoot(gConfig).Combine("shared/Microsoft.NETCore.App")
            .DirectoryMustExist()
            .Directories()
            // Ex: 7.0.0
            .Single();

    public static string WinArchitecture(string arch) => arch.Equals("x86") ? "Win32" : "x64";

    public static string DotNetVerbosity(Verbosity val) =>
        val == Verbosity.Silent ? Verbosity.Quiet.ToString().ToLower() : val.ToString().ToLower();

    public static void RunProcess(ProcessStartInfo psi, GlobalConfig config)
        => RunProcess(psi, config.VerbosityLevel == Verbosity.Silent);

    public static void RunProcess(ProcessStartInfo psi, bool silent = false)
    {

        Console.WriteLine($"Running: {psi.FileName} {psi.Arguments}");
        Console.WriteLine($"Working Directory: {psi.WorkingDirectory}");

        using (Process proc = new())
        {
            proc.StartInfo = psi;
            proc.StartInfo.UseShellExecute = false;
            if (silent)
            {
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
            }

            proc.Start();

            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                if (silent)
                {
                    Console.WriteLine(proc.StandardOutput.ReadToEnd());
                    Console.WriteLine(proc.StandardError.ReadToEnd());
                }

                throw new Exception($"Running {psi.FileName} {psi.Arguments} failed!");
            }
        }
    }

}
