// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.WebAssembly.Build.Tasks
{
    public class RunWithEmSdkEnv : Exec
    {
        [NotNull]
        [Required]
        public string? EmSdkPath { get; set; }

        public override bool Execute()
        {
            IgnoreStandardErrorWarningFormat = true;
            StandardOutputImportance = "Low";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string envScriptPath = Path.Combine(EmSdkPath, "emsdk_env.bat");
                if (!CheckEnvScript(envScriptPath))
                    return false;

                Command = $"@cmd /c \"call \"{envScriptPath}\" > nul 2>&1 && {Command}\"";
            }
            else
            {
                string envScriptPath = Path.Combine(EmSdkPath, "emsdk_env.sh");
                if (!CheckEnvScript(envScriptPath))
                    return false;

                Command = $"bash -c 'source {envScriptPath} > /dev/null 2>&1 && {Command}'";
            }

            var workingDir = string.IsNullOrEmpty(WorkingDirectory) ? Directory.GetCurrentDirectory() : WorkingDirectory;
            Log.LogMessage(MessageImportance.Low, $"Working directory: {workingDir}");
            Log.LogMessage(MessageImportance.Low, $"Using Command: {Command}");

            return base.Execute() && !Log.HasLoggedErrors;

            bool CheckEnvScript(string envScriptPath)
            {
                if (!File.Exists(envScriptPath))
                    Log.LogError($"Could not find '{envScriptPath}' required to run command: {Command}");

                return !Log.HasLoggedErrors;
            }
        }
    }
}
