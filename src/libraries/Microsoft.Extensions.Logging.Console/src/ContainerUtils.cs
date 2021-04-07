// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Logging.Console
{
    internal static class ContainerUtils
    {
        private const string RunningInContainerVariableName = "DOTNET_RUNNING_IN_CONTAINER";
        private const string DeprecatedRunningInContainerVariableName = "DOTNET_RUNNING_IN_CONTAINERS";

        public static bool IsContainer { get; } = IsProcessRunningInContainer();

        public static bool IsProcessRunningInContainer()
        {
            // Official .NET Core images (Windows and Linux) set this. So trust it if it's there.
            // We check both DOTNET_RUNNING_IN_CONTAINER (the current name) and DOTNET_RUNNING_IN_CONTAINERS (a deprecated name used in some images).
            if (GetBooleanEnvVar(RunningInContainerVariableName) || GetBooleanEnvVar(DeprecatedRunningInContainerVariableName))
            {
                return true;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // we currently don't have a good way to detect if running in a Windows container
                return false;
            }

            // Try to detect docker using the cgroups process 1 is in.
            const string procFile = "/proc/1/cgroup";
            if (!File.Exists(procFile))
            {
                return false;
            }

            // typically the last line in the file is "1:name=openrc:/docker"
            string[] lines = File.ReadAllLines(procFile);
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (lines[i].EndsWith("name=openrc:/docker", StringComparison.Ordinal))
                {
                    return true;
                }
            }
            
            return false;
        }

        private static bool GetBooleanEnvVar(string envVarName)
        {
            var value = Environment.GetEnvironmentVariable(envVarName);
            return string.Equals(value, "1", StringComparison.Ordinal) ||
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
