// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using System;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static class CommandExtensions
    {
        public static Command EnableHostTracing(this Command command)
        {
            return command.EnvironmentVariable(Constants.HostTracing.TraceLevelEnvironmentVariable, "1");
        }

        public static Command EnableHostTracingToFile(this Command command, out string filePath)
        {
            filePath = Path.Combine(HostTestContext.TestArtifactsPath, "trace" + Guid.NewGuid().ToString() + ".log");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return command.EnableHostTracingToPath(filePath);
        }

        public static Command EnableHostTracingToPath(this Command command, string path)
        {
            return command
                .EnableHostTracing()
                .EnvironmentVariable(Constants.HostTracing.TraceFileEnvironmentVariable, path);
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
                return command.EnvironmentVariable(Constants.DotnetRoot.ArchitectureEnvironmentVariablePrefix + architecture.ToUpper(), dotNetRoot);

            // If we are clearing out the variable, make sure we clear out any architecture-specific one too
            if (string.IsNullOrEmpty(dotNetRoot))
                command = command.EnvironmentVariable($"{Constants.DotnetRoot.ArchitectureEnvironmentVariablePrefix}{HostTestContext.BuildArchitecture.ToUpperInvariant()}", dotNetRoot);

            return command
                    .EnvironmentVariable(Constants.DotnetRoot.EnvironmentVariable, dotNetRoot)
                    .EnvironmentVariable(Constants.DotnetRoot.WindowsX86EnvironmentVariable, dotNetRoot);
        }

        public static Command MultilevelLookup(this Command command, bool? enable)
        {
            if (enable.HasValue)
                return command.EnvironmentVariable(Constants.MultilevelLookup.EnvironmentVariable, enable.Value ? "1" : "0");

            return command.RemoveEnvironmentVariable(Constants.MultilevelLookup.EnvironmentVariable);
        }

        public static Command RuntimeId(this Command command, string rid)
        {
            return command.EnvironmentVariable(Constants.RuntimeId.EnvironmentVariable, rid);
        }
    }
}
