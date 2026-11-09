// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperPmi.Replay;

// Parallel mode: runs SuperPMI child processes on interleaved method contexts ("-stride"), and merges their
// output, failing method context lists, and details.
internal static class ParallelReplay
{
    // CoreCLR's MAXIMUM_WAIT_OBJECTS.
    private const int MaximumWaitObjects = 64;

    private static volatile bool s_closeRequested;

    private sealed class PerWorkerData
    {
        public string failingMCListPath;
        public string detailsPath;
        public string stdOutputPath;
        public string stdErrorPath;
        public SpmiResult resultCode = SpmiResult.GeneralFailure;
    }

    // The arguments passed to the child processes. These aren't passed through: -parallel, -writeLogFile (the
    // parent writes the log, from the output of the children), -reproName, -coredistools, -skipCleanup,
    // -failingMCList, and -details.
    //
    // With a verbosity that includes info, a child outputs a "Loaded/Jitted/FailedCompile/Excluded/Missing[/Diffs]"
    // line, which the parent parses and sums up.
    private static List<string> ConstructChildProcessArgs(Options o)
    {
        var args = new List<string>();
        void AddBool(bool b, string arg)
        {
            if (b)
                args.Add(arg);
        }
        void AddString(string s, string arg)
        {
            if (s != null)
                args.AddRange([arg, s]);
        }
        // Only an integer that differs from the default, as a proxy for whether it was parsed.
        void AddInt(int i, string arg, int defaultValue)
        {
            if (i != defaultValue)
                args.AddRange([arg, i.ToString()]);
        }
        void AddJitOptions(LightWeightMap<uint, uint> jitOptions, params string[] optionName)
        {
            for (int i = 0; i < (jitOptions?.GetCount() ?? 0); i++)
                args.AddRange([.. optionName, $"{jitOptions.GetString(jitOptions.GetKey(i))}={jitOptions.GetString(jitOptions.GetItem(i))}"]);
        }

        AddBool(o.breakOnError, "-boe");
        AddBool(o.breakOnAssert, "-boa");
        AddBool(o.breakOnException, "-box");
        AddBool(o.ignoreStoredConfig, "-ignoreStoredConfig");
        AddBool(o.applyDiff, "-applyDiff");
        AddString(o.verbosity, "-verbosity");
        AddString(o.reproName, "-reproName");
        AddString(o.methodStatsTypes, "-emitMethodStats");
        AddString(o.hash, "-matchHash");
        AddString(o.targetArchitecture, "-target");
        AddString(o.compileList, "-compile");
        AddInt(o.failureLimit, "-failureLimit", -1);
        AddInt(o.repeatCount, "-repeatCount", 1);
        AddJitOptions(o.forceJitOptions, "-jitoption", "force");
        AddJitOptions(o.forceJit2Options, "-jit2option", "force");
        AddJitOptions(o.jitOptions, "-jitoption");
        AddJitOptions(o.jit2Options, "-jit2option");
        if (o.nameOfJit != null)
            args.Add(o.nameOfJit);
        if (o.nameOfJit2 != null)
            args.Add(o.nameOfJit2);
        if (o.nameOfInputMethodContextFile != null)
            args.Add(o.nameOfInputMethodContextFile);
        return args;
    }

    public static int Run(Options o, string program)
    {
        Stopwatch st = Stopwatch.StartNew();

        // The child processes share the console, so they also get Ctrl+C.
        Console.CancelKeyPress += (_, e) =>
        {
            s_closeRequested = true;
            e.Cancel = true;
        };

        string tempPath = Path.GetTempPath();

        if (o.workerCount <= 0)
            o.workerCount = OperatingSystem.IsWindows() ? Math.Min(Environment.ProcessorCount, MaximumWaitObjects) : Environment.ProcessorCount;

        // The child processes are this same program.
        string spmiFilename = Environment.ProcessPath;
        List<string> spmiArgs = ConstructChildProcessArgs(o);

        Logger.LogVerbose($"Using child ({spmiFilename}) with args ({string.Concat(spmiArgs.Select(a => " " + a))})");
        if (o.mclFilename != null)
            Logger.LogVerbose($" failingMCList={o.mclFilename}");
        if (o.details != null)
            Logger.LogVerbose($" details={o.details}");
        Logger.LogVerbose($" workerCount={o.workerCount}, skipCleanup={(o.skipCleanup ? 1 : 0)}.");

        // A random number in the temporary file names allows running multiple parallel SuperPMI at once.
        uint randNumber = (uint)Random.Shared.NextInt64(uint.MaxValue + 1L);

        var perWorkerData = new PerWorkerData[o.workerCount];
        for (int i = 0; i < o.workerCount; i++)
        {
            perWorkerData[i] = new PerWorkerData
            {
                failingMCListPath = o.mclFilename != null ? $"{tempPath}ParallelSuperPMI-{randNumber}-{i}.mcl" : null,
                detailsPath = o.details != null ? $"{tempPath}ParallelSuperPMI-Details-{randNumber}-{i}.csv" : null,
                stdOutputPath = $"{tempPath}ParallelSuperPMI-stdout-{randNumber}-{i}.txt",
                stdErrorPath = $"{tempPath}ParallelSuperPMI-stderr-{randNumber}-{i}.txt",
            };
        }

        var processes = new Process[o.workerCount];
        var outputCopies = new List<Task>();
        for (int i = 0; i < o.workerCount; i++)
        {
            PerWorkerData wd = perWorkerData[i];
            var startInfo = new ProcessStartInfo(spmiFilename)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            startInfo.ArgumentList.Add("-stride");
            startInfo.ArgumentList.Add((i + 1).ToString());
            startInfo.ArgumentList.Add(o.workerCount.ToString());
            if (wd.failingMCListPath != null)
            {
                startInfo.ArgumentList.Add("-failingMCList");
                startInfo.ArgumentList.Add(wd.failingMCListPath);
            }
            if (wd.detailsPath != null)
            {
                startInfo.ArgumentList.Add("-details");
                startInfo.ArgumentList.Add(wd.detailsPath);
            }
            startInfo.ArgumentList.Add("-v");
            startInfo.ArgumentList.Add("ewmin");
            foreach (string arg in spmiArgs)
                startInfo.ArgumentList.Add(arg);

            Logger.LogDebug($"stdout {i}={wd.stdOutputPath}");
            Logger.LogDebug($"stderr {i}={wd.stdErrorPath}");
            Logger.LogDebug($"StartProcess commandLine={spmiFilename}{string.Concat(startInfo.ArgumentList.Select(a => " " + a))}");

            Stream stdout, stderr;
            try
            {
                stdout = new FileStream(wd.stdOutputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                stderr = new FileStream(wd.stdErrorPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                processes[i] = Process.Start(startInfo);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            {
                Logger.LogError($"Unable to start the SuperPMI worker process. {e.Message}");
                return -1;
            }

            outputCopies.Add(CopyAndClose(processes[i].StandardOutput.BaseStream, stdout));
            outputCopies.Add(CopyAndClose(processes[i].StandardError.BaseStream, stderr));
        }

        foreach (Process process in processes)
            process.WaitForExit();
        Task.WaitAll(outputCopies);

        SpmiResult result = SpmiResult.Success;

        if (!s_closeRequested)
        {
            // If any child returns non-zero, return non-zero, to indicate failure.
            for (int i = 0; i < o.workerCount; i++)
            {
                var childResult = (SpmiResult)processes[i].ExitCode;
                if (childResult is not (SpmiResult.Success or SpmiResult.Diffs or SpmiResult.Misses or SpmiResult.Error or SpmiResult.JitFailedToInit))
                {
                    // For example, if the child was OOM-killed.
                    Logger.LogError($"Child process {i} exited with code {(uint)processes[i].ExitCode}");
                    childResult = SpmiResult.GeneralFailure;
                }

                perWorkerData[i].resultCode = childResult;

                // In priority: the first failures are the most important to propagate.
                if (childResult != result)
                {
                    foreach (SpmiResult r in (ReadOnlySpan<SpmiResult>)[SpmiResult.GeneralFailure, SpmiResult.JitFailedToInit, SpmiResult.Error, SpmiResult.Diffs, SpmiResult.Misses])
                    {
                        if (result == r || childResult == r)
                        {
                            result = r;
                            break;
                        }
                    }
                }
            }

            bool usageError = false;
            int loaded = 0, jitted = 0, failed = 0, excluded = 0, missing = 0, diffs = 0;

            // Log the stderr of the children as errors, and parse their stdout for the counts.
            for (int i = 0; i < o.workerCount; i++)
            {
                PerWorkerData wd = perWorkerData[i];
                ProcessChildStdErr(wd.stdErrorPath);
                ProcessChildStdOut(o, wd.stdOutputPath, ref loaded, ref jitted, ref failed, ref excluded, ref missing, ref diffs, ref usageError);
                if (usageError)
                    break;
            }

            if (o.mclFilename != null && !usageError)
                MergeWorkerMCLs(o.mclFilename, perWorkerData.Select(wd => wd.failingMCListPath));

            if (o.details != null && !usageError)
                MergeWorkerCsvs(o.details, perWorkerData);

            if (!usageError)
            {
                if (o.applyDiff)
                    Logger.LogInfo(Replay.AsmDiffsSummary(loaded, jitted, failed, excluded, missing, diffs));
                else
                    Logger.LogInfo(Replay.Summary(loaded, jitted, failed, excluded, missing));
            }

            Logger.LogVerbose($"Total time: {st.Elapsed.TotalMilliseconds:F6}ms");
        }

        if (!o.skipCleanup)
        {
            // Delete all the temporary files.
            foreach (PerWorkerData wd in perWorkerData)
            {
                foreach (string path in (ReadOnlySpan<string>)[wd.failingMCListPath, wd.detailsPath, wd.stdOutputPath, wd.stdErrorPath])
                {
                    if (path != null)
                        File.Delete(path);
                }
            }
        }

        return (int)result;
    }

    private static async Task CopyAndClose(Stream source, Stream destination)
    {
        await source.CopyToAsync(destination);
        await destination.DisposeAsync();
    }

    private static IEnumerable<string> ReadLines(string path)
    {
        try
        {
            return File.ReadLines(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.LogError($"Unable to open '{path}'.");
            return [];
        }
    }

    private static void ProcessChildStdErr(string stderrFilename)
    {
        foreach (string line in ReadLines(stderrFilename))
            Logger.LogPassThroughStderr(line);
    }

    private static void ProcessChildStdOut(Options o, string stdoutFilename, ref int loaded, ref int jitted, ref int failed, ref int excluded,
        ref int missing, ref int diffs, ref bool usageError)
    {
        foreach (string line in ReadLines(stdoutFilename))
        {
            if (line.StartsWith(CommandLine.UsageFirstLine, StringComparison.Ordinal))
            {
                // A SuperPMI command line usage error.
                usageError = true;
                Logger.LogPassThroughStdout(line);
            }
            else if (line.StartsWith(Replay.AllFormatStringFixedPrefix, StringComparison.Ordinal))
            {
                int[] counts = ScanSummary(line, o.applyDiff ? 6 : 5);
                if (counts == null)
                {
                    Logger.LogError($"Couldn't parse status message: \"{line}\"");
                    continue;
                }

                loaded += counts[0];
                jitted += counts[1];
                failed += counts[2];
                excluded += counts[3];
                missing += counts[4];
                if (o.applyDiff)
                    diffs += counts[5];
                else
                    diffs = -1;
            }
            else
            {
                // Pass the output through; the children have the same verbosity.
                Logger.LogPassThroughStdout(line);
            }
        }
    }

    // Parses "Loaded %d  Jitted %d  FailedCompile %d Excluded %d Missing %d[ Diffs %d]" as sscanf would.
    private static int[] ScanSummary(string line, int count)
    {
        ReadOnlySpan<string> labels = ["Loaded", "Jitted", "FailedCompile", "Excluded", "Missing", "Diffs"];
        string[] tokens = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        var counts = new int[count];
        for (int i = 0; i < count; i++)
        {
            if (2 * i + 1 >= tokens.Length || tokens[2 * i] != labels[i] || !int.TryParse(tokens[2 * i + 1], out counts[i]))
                return null;
        }
        return counts;
    }

    // This counts the lines by their carriage returns, as MCList writes them.
    private static List<int> ReadMCLToArray(string mclFilename)
    {
        var result = new List<int>();
        byte[] buff;
        try
        {
            buff = File.ReadAllBytes(mclFilename);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.LogError($"Unable to open '{mclFilename}'. GetLastError()={ErrorHandling.LastError(e)}");
            return result;
        }

        int count = buff.Count(b => b == '\r');
        for (int j = 0; result.Count < count && j < buff.Length;)
        {
            // Seek the first number on the line, read it, and seek to the start of the next line.
            while (j < buff.Length && !char.IsAsciiDigit((char)buff[j]))
                j++;
            int start = j;
            while (j < buff.Length && char.IsAsciiDigit((char)buff[j]))
                j++;
            result.Add(CommandLine.Atoi(Encoding.ASCII.GetString(buff, start, j - start)));
            while (j < buff.Length && buff[j] != '\n')
                j++;
            j++;
        }
        return result;
    }

    private static void MergeWorkerMCLs(string mclFilename, IEnumerable<string> workerMCLs)
    {
        List<int> mergedMCL = workerMCLs.SelectMany(ReadMCLToArray).Order().ToList();
        try
        {
            File.WriteAllText(mclFilename, string.Concat(mergedMCL.Select(i => $"{i}\r\n")));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.LogError($"Failed to open output file '{mclFilename}'. GetLastError()={ErrorHandling.LastError(e)}");
            Logger.LogError($"Unable to write to MCL file {mclFilename}.");
        }
    }

    // Concatenates the CSV files of the children, with one header.
    private static void MergeWorkerCsvs(string csvFilename, PerWorkerData[] workerData)
    {
        using FileWriter fw = FileWriter.CreateNew(csvFilename);
        if (fw == null)
        {
            Logger.LogError($"Could not create file {csvFilename}");
            return;
        }

        bool hasHeader = false;
        for (int i = 0; i < workerData.Length; i++)
        {
            if (workerData[i].resultCode is not (SpmiResult.Success or SpmiResult.Diffs or SpmiResult.Misses or SpmiResult.Error))
            {
                Logger.LogWarning($"Skipping merging CSV from child {i} due to result code {(int)workerData[i].resultCode}");
                continue;
            }

            IEnumerable<string> lines;
            try
            {
                lines = File.ReadLines(workerData[i].detailsPath);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Logger.LogError($"Could not open child CSV file {workerData[i].detailsPath}");
                continue;
            }

            foreach (string line in hasHeader ? lines.Skip(1) : lines)
                fw.Print(line + "\n");
            hasHeader = true;
        }
    }
}
