// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public static class CommandExtensions
    {
        public static Command EnableHostTracing(this Command command)
        {
            return command.EnvironmentVariable(Constants.HostTracing.TraceLevelEnvironmentVariable, "1");
        }

        public static Command EnableHostTracingToFile(this Command command, out string filePath)
        {
            filePath = Path.Combine(TestArtifact.TestArtifactsPath, "trace" + Guid.NewGuid().ToString() + ".log");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return command
                .EnableHostTracing()
                .EnvironmentVariable(Constants.HostTracing.TraceFileEnvironmentVariable, filePath);
        }

        public static Command EnableTracingAndCaptureOutputs(this Command command)
        {
            return command
                .EnableHostTracing()
                .CaptureStdOut()
                .CaptureStdErr();
        }

        public static Command DotNetRoot(this Command command, string dotNetRoot, string architecture = null)
        {
            if (!string.IsNullOrEmpty(architecture))
                return command.EnvironmentVariable($"DOTNET_ROOT_{architecture.ToUpper()}", dotNetRoot);

            return command
                .EnvironmentVariable("DOTNET_ROOT", dotNetRoot)
                .EnvironmentVariable("DOTNET_ROOT(x86)", dotNetRoot);
        }

        public static Command MultilevelLookup(this Command command, bool enable)
        {
            return command.EnvironmentVariable(Constants.MultilevelLookup.EnvironmentVariable, enable ? "1" : "0");
        }

        public static Command RuntimeId(this Command command, string rid)
        {
            return command.EnvironmentVariable(Constants.RuntimeId.EnvironmentVariable, rid);
        }
    }
}
