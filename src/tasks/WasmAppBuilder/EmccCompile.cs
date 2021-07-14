// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

#nullable enable

namespace Microsoft.WebAssembly.Build.Tasks
{
    /// <summary>
    /// This is meant to *compile* source files only. It is *not* a general purpose
    /// `emcc` invocation task.
    ///
    /// It runs `emcc` for each source file, and with output to `%(SourceFiles.ObjectFile)`
    ///
    /// </summary>
    public class EmccCompile : Microsoft.Build.Utilities.Task
    {
        [NotNull]
        [Required]
        public ITaskItem[]? SourceFiles            { get; set; }

        public ITaskItem[]? EnvironmentVariables   { get; set; }
        public bool         DisableParallelCompile { get; set; }
        public string       Arguments              { get; set; } = string.Empty;
        public string?      WorkingDirectory       { get; set; }

        [Output]
        public ITaskItem[]? OutputFiles            { get; private set; }

        private string? _tempPath;

        public override bool Execute()
        {
            if (SourceFiles.Length == 0)
            {
                Log.LogError($"No SourceFiles to compile");
                return false;
            }

            ITaskItem? badItem = SourceFiles.FirstOrDefault(sf => string.IsNullOrEmpty(sf.GetMetadata("ObjectFile")));
            if (badItem != null)
            {
                Log.LogError($"Source file {badItem.ItemSpec} is missing ObjectFile metadata.");
                return false;
            }

            IDictionary<string, string> envVarsDict = GetEnvironmentVariablesDict();
            ConcurrentBag<ITaskItem> outputItems = new();
            try
            {
                Log.LogMessage(MessageImportance.Low, "Using environment variables:");
                foreach (var kvp in envVarsDict)
                    Log.LogMessage(MessageImportance.Low, $"\t{kvp.Key} = {kvp.Value}");

                string workingDir = Environment.CurrentDirectory;
                Log.LogMessage(MessageImportance.Low, $"Using working directory: {workingDir}");

                _tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(_tempPath);

                int allowedParallelism = Math.Min(SourceFiles.Length, Environment.ProcessorCount);
#if false // Enable this when we bump msbuild to 16.1.0
                if (BuildEngine is IBuildEngine9 be9)
                    allowedParallelism = be9.RequestCores(allowedParallelism);
#endif

                if (DisableParallelCompile || allowedParallelism == 1)
                {
                    foreach (ITaskItem srcItem in SourceFiles)
                    {
                        if (!ProcessSourceFile(srcItem))
                            return false;
                    }
                }
                else
                {
                    ParallelLoopResult result = Parallel.ForEach(SourceFiles,
                                                    new ParallelOptions { MaxDegreeOfParallelism = allowedParallelism },
                                                    (srcItem, state) =>
                    {
                        if (!ProcessSourceFile(srcItem))
                            state.Stop();
                    });

                    if (!result.IsCompleted && !Log.HasLoggedErrors)
                        Log.LogError("Unknown failed occured while compiling");
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(_tempPath))
                    Directory.Delete(_tempPath, true);
            }

            OutputFiles = outputItems.ToArray();
            return !Log.HasLoggedErrors;

            bool ProcessSourceFile(ITaskItem srcItem)
            {
                string srcFile = srcItem.ItemSpec;
                string objFile = srcItem.GetMetadata("ObjectFile");

                try
                {
                    string command = $"emcc {Arguments} -c -o {objFile} {srcFile}";

                    // Log the command in a compact format which can be copy pasted
                    StringBuilder envStr = new StringBuilder(string.Empty);
                    foreach (var key in envVarsDict.Keys)
                        envStr.Append($"{key}={envVarsDict[key]} ");
                    Log.LogMessage(MessageImportance.Low, $"Exec: {envStr}{command}");
                    (int exitCode, string output) = Utils.RunShellCommand(command, envVarsDict, workingDir: Environment.CurrentDirectory);

                    if (exitCode != 0)
                    {
                        Log.LogError($"Failed to compile {srcFile} -> {objFile}{Environment.NewLine}{output}");
                        return false;
                    }

                    ITaskItem newItem = new TaskItem(objFile);
                    newItem.SetMetadata("SourceFile", srcFile);
                    outputItems.Add(newItem);

                    return true;
                }
                catch (Exception ex)
                {
                    Log.LogError($"Failed to compile {srcFile} -> {objFile}{Environment.NewLine}{ex.Message}");
                    return false;
                }
            }
        }

        private IDictionary<string, string> GetEnvironmentVariablesDict()
        {
            Dictionary<string, string> envVarsDict = new();
            if (EnvironmentVariables == null)
                return envVarsDict;

            foreach (var item in EnvironmentVariables)
            {
                var parts = item.ItemSpec.Split(new char[] {'='}, 2, StringSplitOptions.None);
                if (parts.Length == 0)
                    continue;

                string key = parts[0];
                string value = parts.Length > 1 ? parts[1] : string.Empty;

                envVarsDict[key] = value;
            }

            return envVarsDict;
        }
    }
}
