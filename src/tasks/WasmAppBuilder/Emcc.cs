// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

#nullable enable

namespace Microsoft.WebAssembly.Build.Tasks
{
    // TODO: rsp files
    public class Emcc : Microsoft.Build.Utilities.Task
    {
        public ITaskItem[]? EnvironmentVariables { get; set; }

        [NotNull]
        [Required]
        public ITaskItem[]? SourceFiles { get; set; }

        public string? Arguments { get; set; } = string.Empty;

        public string? WorkingDirectory { get; set; }

        private string? _tempPath;

        public override bool Execute()
        {
            Dictionary<string, string> envVarsDict = new();
            if (EnvironmentVariables != null)
            {
                foreach (var item in EnvironmentVariables)
                {
                    var parts = item.ItemSpec.Split('=', 2, StringSplitOptions.None);
                    if (parts.Length == 0)
                        continue;

                    string key = parts[0];
                    string value = parts.Length > 1 ? parts[1] : string.Empty;

                    envVarsDict[key] = value;
                }
            }

            try
            {
                _tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(_tempPath);

                // FIXME: do preprocessing out of the parallel bit
                Parallel.ForEach(SourceFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, source =>
                {
                    var srcFile = source.ItemSpec;
                    var outFile = source.GetMetadata("ObjectFile");

                    if (string.IsNullOrEmpty(outFile))
                    {
                        Log.LogWarning($"Bug: No ObjectFile metadata found for {srcFile}");
                        return;
                    }

                    if (IsNewer(srcFile, outFile))
                        Log.LogMessage(MessageImportance.Low, $"Compiling {srcFile} because it is newer than {outFile}, or {outFile} doesn't exist");
                    else
                        Log.LogMessage(MessageImportance.Low, $"Skipping {srcFile} because it is newer than {outFile}");

                    string command = $"{Arguments} -c {srcFile} -o {outFile}";
                    string script = CreateTemporaryBatchFile(command);
                    Log.LogMessage(MessageImportance.Normal, $"Running {command}");
                    RunProcess("/bin/sh", script, envVarsDict);
                    Log.LogMessage(MessageImportance.Normal, $"Done compiling {srcFile}");
                });
            }
            finally
            {
                if (!string.IsNullOrEmpty(_tempPath))
                    Directory.Delete(_tempPath, true);
            }

            return true;
        }

        private string RunProcess(
            string path,
            string args = "",
            IDictionary<string, string>? envVars = null,
            string? workingDir = null,
            bool ignoreErrors = false,
            bool silent = true,
            MessageImportance outputMessageImportance=MessageImportance.Normal,
            MessageImportance debugMessageImportance=MessageImportance.Low)
        {
            if (envVars != null)
            {
                Log.LogMessage(debugMessageImportance, "Using additional environment variables:");
                foreach (var kvp in envVars)
                    Log.LogMessage(debugMessageImportance, $"\t{kvp.Key} = {kvp.Value}");
            }
            Log.LogMessage(debugMessageImportance, $"Running: {path} {args}");
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            var processStartInfo = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = args,
            };

            if (workingDir != null)
                processStartInfo.WorkingDirectory = workingDir;

            Log.LogMessage(debugMessageImportance, $"Using working directory: {workingDir ?? Environment.CurrentDirectory}");

            if (envVars != null)
            {
                if (envVars.Count > 0)
                    Log.LogMessage(debugMessageImportance, "Setting environment variables for execution:");

                foreach (KeyValuePair<string, string> envVar in envVars)
                {
                    processStartInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
                    Log.LogMessage(debugMessageImportance, $"\t{envVar.Key} = {envVar.Value}");
                }
            }

            Process? process = Process.Start(processStartInfo);
            if (process == null)
                throw new ArgumentException($"Process.Start({path} {args}) returned null process");

            process.ErrorDataReceived += (sender, e) =>
            {
                // lock (s_SyncObj)
                {
                    if (!silent)
                    {
                        Log.LogWarning(e.Data);
                        outputBuilder.AppendLine(e.Data);
                    }
                    errorBuilder.AppendLine(e.Data);
                }
            };
            process.OutputDataReceived += (sender, e) =>
            {
                // lock (s_SyncObj)
                {
                    if (!silent)
                    {
                        Log.LogMessage(outputMessageImportance, e.Data);
                        outputBuilder.AppendLine(e.Data);
                    }
                }
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Log.LogMessage(debugMessageImportance, $"Exit code: {process.ExitCode}");
                if (!ignoreErrors)
                    throw new Exception("Error: " + errorBuilder);
            }

            return outputBuilder.ToString().Trim('\r', '\n');
        }

        private string CreateTemporaryBatchFile(string command)
        {
            string extn = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : ".sh";
            string file = Path.Combine(_tempPath!, $"tmp{Guid.NewGuid():N}{extn}");

            using StreamWriter sw = new(file);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                    sw.WriteLine("setlocal");
                    sw.WriteLine("set errorlevel=dummy");
                    sw.WriteLine("set errorlevel=");
                    sw.WriteLine($"emcc.cmd {command}");
            }
            else
            {
                // Use sh rather than bash, as not all 'nix systems necessarily have Bash installed
                sw.WriteLine("#!/bin/sh");
                sw.WriteLine($"emcc {command}");
            }

            return file;
        }

        private static bool IsNewer(string fileA, string fileB)
        {
            if (!File.Exists(fileA))
                return true;
            if (!File.Exists(fileB))
                return true;

            DateTime lastWriteTimeA= File.GetLastWriteTimeUtc(fileA);
            DateTime lastWriteTimeB= File.GetLastWriteTimeUtc(fileB);

            return lastWriteTimeA > lastWriteTimeB;
        }
    }
}
