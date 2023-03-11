// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;

namespace Microsoft.DotNet.Cli.Build
{
    public partial class DotNetCli
    {
        public string BinPath { get; }
        public string SharedFxPath { get; }
        public string GreatestVersionSharedFxPath { get; }
        public string GreatestVersionHostFxrPath { get; }
        public string GreatestVersionHostFxrFilePath => Path.Combine(GreatestVersionHostFxrPath, Binaries.HostFxr.FileName);
        public string DotnetExecutablePath => Path.Combine(BinPath, Binaries.DotNet.FileName);

        public DotNetCli(string binPath)
        {
            BinPath = binPath;

            SharedFxPath = Path.Combine(BinPath, "shared", Constants.MicrosoftNETCoreApp);
            if (Directory.Exists(SharedFxPath))
            {
                var sharedFxVersionDirectories = Directory.EnumerateDirectories(SharedFxPath);
                GreatestVersionSharedFxPath = sharedFxVersionDirectories
                    .OrderByDescending(p => p.ToLower())
                    .First();
            }

            var hostFxrBaseDirectory = Path.Combine(BinPath, "host", "fxr");
            if (Directory.Exists(hostFxrBaseDirectory))
            {
                var hostFxrVersionDirectories = Directory.EnumerateDirectories(hostFxrBaseDirectory);
                GreatestVersionHostFxrPath = hostFxrVersionDirectories
                    .OrderByDescending(p => p.ToLower())
                    .First();
            }
        }

        public Command Exec(string command, params string[] args)
        {
            var newArgs = args.ToList();
            newArgs.Insert(0, command);

            return Command.Create(DotnetExecutablePath, newArgs)
                .EnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1")
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0"); // Avoid looking at machine state by default
        }

        public Command Restore(params string[] args) => Exec("restore", args);
        public Command Build(params string[] args) => Exec("build", args);
        public Command Pack(params string[] args) => Exec("pack", args);
        public Command Test(params string[] args) => Exec("test", args);
        public Command Publish(params string[] args) => Exec("publish", args);

        public Command Store(params string[] args) => Exec("store", args);
    }
}
