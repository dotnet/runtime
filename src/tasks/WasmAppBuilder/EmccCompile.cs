// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        public string       OutputMessageImportance{ get; set; } = "Low";

        [Output]
        public ITaskItem[]? OutputFiles            { get; private set; }

        private string? _tempPath;
        private int _totalFiles;
        private int _numCompiled;

        public override bool Execute()
        {
            try
            {
                return ExecuteActual();
            }
            catch (LogAsErrorException laee)
            {
                Log.LogError(laee.Message);
                return false;
            }
        }

        private bool ExecuteActual()
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

            if (!Enum.TryParse(OutputMessageImportance, ignoreCase: true, out MessageImportance messageImportance))
            {
                Log.LogError($"Invalid value for OutputMessageImportance={OutputMessageImportance}. Valid values: {string.Join(", ", Enum.GetNames(typeof(MessageImportance)))}");
                return false;
            }

            _totalFiles = SourceFiles.Length;
            IDictionary<string, string> envVarsDict = GetEnvironmentVariablesDict();
            ConcurrentBag<ITaskItem> outputItems = new();
            try
            {
                List<(string, string)> filesToCompile = new();
                foreach (ITaskItem srcItem in SourceFiles)
                {
                    string srcFile = srcItem.ItemSpec;
                    string objFile = srcItem.GetMetadata("ObjectFile");
                    string depMetadata = srcItem.GetMetadata("Dependencies");
                    string[] depFiles = string.IsNullOrEmpty(depMetadata)
                                            ? Array.Empty<string>()
                                            : depMetadata.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    if (!ShouldCompile(srcFile, objFile, depFiles, out string reason))
                    {
                        Log.LogMessage(MessageImportance.Low, $"Skipping {srcFile} because {reason}.");
                        outputItems.Add(CreateOutputItemFor(srcFile, objFile));
                    }
                    else
                    {
                        Log.LogMessage(MessageImportance.Low, $"Compiling {srcFile} because {reason}.");
                        filesToCompile.Add((srcFile, objFile));
                    }
                }

                _numCompiled = SourceFiles.Length - filesToCompile.Count;
                if (_numCompiled == _totalFiles)
                {
                    // nothing to do!
                    OutputFiles = outputItems.ToArray();
                    return !Log.HasLoggedErrors;
                }

                if (_numCompiled > 0)
                    Log.LogMessage(MessageImportance.High, $"[{_numCompiled}/{SourceFiles.Length}] skipped unchanged files");

                Log.LogMessage(MessageImportance.Low, "Using environment variables:");
                foreach (var kvp in envVarsDict)
                    Log.LogMessage(MessageImportance.Low, $"\t{kvp.Key} = {kvp.Value}");

                string workingDir = Environment.CurrentDirectory;
                Log.LogMessage(MessageImportance.Low, $"Using working directory: {workingDir}");

                _tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(_tempPath);

                int allowedParallelism = DisableParallelCompile ? 1 : Math.Min(SourceFiles.Length, Environment.ProcessorCount);
                if (BuildEngine is IBuildEngine9 be9)
                    allowedParallelism = be9.RequestCores(allowedParallelism);

                /*
                    From: https://github.com/dotnet/runtime/issues/46146#issuecomment-754021690

                    Stephen Toub:
                    "As such, by default ForEach works on a scheme whereby each
                    thread takes one item each time it goes back to the enumerator,
                    and then after a few times of this upgrades to taking two items
                    each time it goes back to the enumerator, and then four, and
                    then eight, and so on. This ammortizes the cost of taking and
                    releasing the lock across multiple items, while still enabling
                    parallelization for enumerables containing just a few items. It
                    does, however, mean that if you've got a case where the body
                    takes a really long time and the work for every item is
                    heterogeneous, you can end up with an imbalance."

                    The time taken by individual compile jobs here can vary a
                    lot, depending on various factors like file size. This can
                    create an imbalance, like mentioned above, and we can end up
                    in a situation where one of the partitions has a job that
                    takes very long to execute, by which time other partitions
                    have completed, so some cores are idle.  But the the idle
                    ones won't get any of the remaining jobs, because they are
                    all assigned to that one partition.

                    Instead, we want to use work-stealing so jobs can be run by any partition.
                */
                ParallelLoopResult result = Parallel.ForEach(
                                                Partitioner.Create(filesToCompile, EnumerablePartitionerOptions.NoBuffering),
                                                new ParallelOptions { MaxDegreeOfParallelism = allowedParallelism },
                                                (toCompile, state) =>
                {
                    if (!ProcessSourceFile(toCompile.Item1, toCompile.Item2))
                        state.Stop();
                });

                if (!result.IsCompleted && !Log.HasLoggedErrors)
                    Log.LogError("Unknown failure occured while compiling. Check logs to get more details.");

                if (!Log.HasLoggedErrors)
                {
                    int numUnchanged = _totalFiles - _numCompiled;
                    if (numUnchanged > 0)
                        Log.LogMessage(MessageImportance.High, $"[{numUnchanged}/{_totalFiles}] unchanged.");
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(_tempPath))
                    Directory.Delete(_tempPath, true);
            }

            OutputFiles = outputItems.ToArray();
            return !Log.HasLoggedErrors;

            bool ProcessSourceFile(string srcFile, string objFile)
            {
                string tmpObjFile = Path.GetTempFileName();
                try
                {
                    string command = $"emcc {Arguments} -c -o \"{tmpObjFile}\" \"{srcFile}\"";
                    var startTime = DateTime.Now;

                    // Log the command in a compact format which can be copy pasted
                    StringBuilder envStr = new StringBuilder(string.Empty);
                    foreach (var key in envVarsDict.Keys)
                        envStr.Append($"{key}={envVarsDict[key]} ");
                    Log.LogMessage(MessageImportance.Low, $"Exec: {envStr}{command}");
                    (int exitCode, string output) = Utils.RunShellCommand(
                                                            Log,
                                                            command,
                                                            envVarsDict,
                                                            workingDir: Environment.CurrentDirectory,
                                                            logStdErrAsMessage: true,
                                                            debugMessageImportance: messageImportance,
                                                            label: Path.GetFileName(srcFile));

                    var endTime = DateTime.Now;
                    var elapsedSecs = (endTime - startTime).TotalSeconds;
                    if (exitCode != 0)
                    {
                        Log.LogError($"Failed to compile {srcFile} -> {objFile}{Environment.NewLine}{output} [took {elapsedSecs:F}s]");
                        return false;
                    }

                    if (!Utils.CopyIfDifferent(tmpObjFile, objFile, useHash: true))
                        Log.LogMessage(MessageImportance.Low, $"Did not overwrite {objFile} as the contents are unchanged");
                    else
                        Log.LogMessage(MessageImportance.Low, $"Copied {tmpObjFile} to {objFile}");

                    outputItems.Add(CreateOutputItemFor(srcFile, objFile));

                    int count = Interlocked.Increment(ref _numCompiled);
                    Log.LogMessage(MessageImportance.High, $"[{count}/{_totalFiles}] {Path.GetFileName(srcFile)} -> {Path.GetFileName(objFile)} [took {elapsedSecs:F}s]");

                    return !Log.HasLoggedErrors;
                }
                catch (Exception ex)
                {
                    Log.LogError($"Failed to compile {srcFile} -> {objFile}{Environment.NewLine}{ex.Message}");
                    return false;
                }
                finally
                {
                    File.Delete(tmpObjFile);
                }
            }

            ITaskItem CreateOutputItemFor(string srcFile, string objFile)
            {
                ITaskItem newItem = new TaskItem(objFile);
                newItem.SetMetadata("SourceFile", srcFile);
                return newItem;
            }
        }

        private bool ShouldCompile(string srcFile, string objFile, string[] depFiles, out string reason)
        {
            if (!File.Exists(srcFile))
                throw new LogAsErrorException($"Could not find source file {srcFile}");

            if (!File.Exists(objFile))
            {
                reason = $"output file {objFile} doesn't exist";
                return true;
            }

            if (IsNewerThanOutput(srcFile, objFile, out reason))
                return true;

            foreach (string depFile in depFiles)
            {
                if (IsNewerThanOutput(depFile, objFile, out reason))
                    return true;
            }

            reason = "everything is up-to-date";
            return false;

            bool IsNewerThanOutput(string inFile, string outFile, out string reason)
            {
                if (!File.Exists(inFile))
                {
                    reason = $"Could not find dependency file {inFile} needed for compiling {srcFile} to {outFile}";
                    Log.LogWarning(reason);
                    return true;
                }

                DateTime lastWriteTimeSrc = File.GetLastWriteTimeUtc(inFile);
                DateTime lastWriteTimeDst = File.GetLastWriteTimeUtc(outFile);

                if (lastWriteTimeSrc > lastWriteTimeDst)
                {
                    reason = $"{inFile} is newer than {outFile}";
                    return true;
                }
                else
                {
                    reason = $"{inFile} is older than {outFile}";
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
