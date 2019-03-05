﻿using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;

namespace Microsoft.DotNet.Cli.Build
{
    public partial class DotNetCli
    {
        public string BinPath { get; }
        public string GreatestVersionSharedFxPath { get; }
        public string GreatestVersionHostFxrPath { get; } 
        public string DotnetExecutablePath
        {
            get
            {
                return Path.Combine(BinPath, RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("dotnet"));
            }
        }

        public DotNetCli(string binPath)
        {
            BinPath = binPath;

            var sharedFxBaseDirectory = Path.Combine(BinPath, "shared", "Microsoft.NETCore.App");
            if (!Directory.Exists(sharedFxBaseDirectory))
            {
                GreatestVersionSharedFxPath = null;
                return;
            }

            var hostFxrBaseDirectory = Path.Combine(BinPath, "host", "fxr");

            if (!Directory.Exists(hostFxrBaseDirectory))
            {
                GreatestVersionHostFxrPath = null;
                return;
            }

            var sharedFxVersionDirectories = Directory.EnumerateDirectories(sharedFxBaseDirectory);

            GreatestVersionSharedFxPath = sharedFxVersionDirectories
                .OrderByDescending(p => p.ToLower())
                .First();

            var hostFxrVersionDirectories = Directory.EnumerateDirectories(hostFxrBaseDirectory);
            GreatestVersionHostFxrPath = hostFxrVersionDirectories
                .OrderByDescending(p => p.ToLower())
                .First();
        }

        public Command Exec(string command, params string[] args)
        {
            var newArgs = args.ToList();
            newArgs.Insert(0, command);

            if (EnvVars.Verbose)
            {
                newArgs.Insert(0, "-v");
            }

            return Command.Create(DotnetExecutablePath, newArgs)
                .EnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1");
        }

        public Command Restore(params string[] args) => Exec("restore", args);
        public Command Build(params string[] args) => Exec("build", args);
        public Command Pack(params string[] args) => Exec("pack", args);
        public Command Test(params string[] args) => Exec("test", args);
        public Command Publish(params string[] args) => Exec("publish", args);

        public Command Store(params string[] args) => Exec("store", args);
    }
}
