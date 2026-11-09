// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;

namespace SuperPmi.Replay;

internal static class Replay
{
    // Parallel mode parses these status lines from the child processes. They must all start with the same
    // fixed prefix, to quickly tell when to parse a line fully.
    public const string AllFormatStringFixedPrefix = "Loaded ";

    public static string Summary(int loaded, int jitted, int failed, int excluded, int missing)
        => $"Loaded {loaded}  Jitted {jitted}  FailedCompile {failed} Excluded {excluded} Missing {missing}";

    public static string AsmDiffsSummary(int loaded, int jitted, int failed, int excluded, int missing, int diffs)
        => $"{Summary(loaded, jitted, failed, excluded, missing)} Diffs {diffs}";

    private enum NearDifferResult
    {
        SuccessWithoutDiff,
        SuccessWithDiff,
        Failure,
    }

    // Runs SuperPMI, returning a SpmiResult.
    public static int Run(string[] args)
    {
        Stopwatch st2 = Stopwatch.StartNew();
        JitInstance jit = null;
        JitInstance jit2 = null;

        var o = new Options();
        if (!CommandLine.Parse(args, o))
            return (int)SpmiResult.GeneralFailure;

        if (o.parallel)
            return ParallelReplay.Run(o, args[0]);

        if (o.streamFile != null)
            return StreamingReplay.Run(o);

        SpmiUtil.BreakOnException = o.breakOnException;

        bool collectThroughput = o.methodStatsTypes != null && o.methodStatsTypes.AsSpan().IndexOfAny('*', 't', 'T') >= 0;

        Logger.LogVerbose($"Using jit({o.nameOfJit}) with input ({o.nameOfInputMethodContextFile})");
        Logger.LogVerbose($" indexCount={o.indexCount} ({string.Join(",", o.indexes ?? [])})");
        if (o.methodStatsTypes != null)
            Logger.LogVerbose($" EmitMethodStats-Types={o.methodStatsTypes}");
        if (o.hash != null)
            Logger.LogVerbose($" MD5Hash={o.hash}");
        if (o.mclFilename != null)
            Logger.LogVerbose($" failingMCList={o.mclFilename}");
        if (o.offset > 0 && o.increment > 0)
            Logger.LogVerbose($" offset={o.offset} increment={o.increment}");

        using MethodStatsEmitter methodStatsEmitter = o.methodStatsTypes != null ? new MethodStatsEmitter(o.nameOfInputMethodContextFile) : null;
        methodStatsEmitter?.SetStatsTypes(o.methodStatsTypes);

        var failingToReplayMCL = new MCList();
        if (o.mclFilename != null)
            failingToReplayMCL.InitializeMCL(o.mclFilename);

        using FileWriter detailsCsv = o.details != null ? FileWriter.CreateNew(o.details) : null;
        if (o.details != null && detailsCsv == null)
        {
            Logger.LogError($"Could not create file {o.details}");
            return (int)SpmiResult.GeneralFailure;
        }

        // The method context reader skips the method contexts that weren't asked for; with an index (.mct)
        // file, it does a lot less work.
        using var reader = new MethodContextReader(o.nameOfInputMethodContextFile, o.indexes, o.hash, o.offset, o.increment);
        if (!reader.isValid())
            return (int)SpmiResult.GeneralFailure;

        int loadedCount = 0;
        int jittedCount = 0;
        int failToReplayCount = 0;
        int errorCount = 0;
        int errorCount2 = 0;
        int diffsCount = 0;
        int missingCount = 0;
        int excludedCount = 0;

        Stopwatch st1 = Stopwatch.StartNew();
        using var nearDiffer = new NearDiffer(o.targetArchitecture != null ? SpmiUtil.GetSpmiTargetArchitecture() : null);

        if (o.applyDiff && !nearDiffer.InitAsmDiff())
            return (int)SpmiResult.GeneralFailure;

        if (detailsCsv != null)
        {
            if (o.applyDiff)
                PrintDiffsCsvHeader(detailsCsv);
            else
                PrintReplayCsvHeader(detailsCsv);
        }

        double totalWork = reader.TotalWork() * o.repeatCount;

        while (true)
        {
            // The progress before reading the method context, since the progress reported below adds the
            // per-repeatCount progress for the current one.
            double progressBeforeCurrentMC = reader.Progress() * o.repeatCount;

            MethodContextBuffer mcb = reader.GetNextMethodContext();
            if (mcb.Error())
                return (int)SpmiResult.GeneralFailure;
            if (mcb.allDone())
            {
                Logger.LogDebug("Done processing method contexts");
                break;
            }

            // This fails if the method context data is corrupt.
            loadedCount++;
            int mcIndex = reader.GetMethodContextIndex();
            if (!MethodContext.Initialize(mcIndex, mcb.buff, (uint)mcb.size, readCompileResults: false, out MethodContext mc))
                return (int)SpmiResult.GeneralFailure;

            if (o.ignoreStoredConfig)
                mc.ignoreStoredConfig = true;

            if (reader.IsMethodExcluded(mc))
            {
                excludedCount++;
                Logger.LogInfo($"main method {mcIndex} of size {mc.methodSize} with was excluded from the compilation.");
                continue;
            }

            // Compile the method context as many times as asked for (once by default).
            for (int iter = 0; iter < o.repeatCount; iter++)
            {
                if (jit == null)
                {
                    var stInitJit = new Stopwatch();
                    jit = JitInstance.InitJit(o.nameOfJit, o.breakOnAssert, stInitJit, mc, o.forceJitOptions, o.jitOptions);
                    if (jit == null)
                        return (int)SpmiResult.JitFailedToInit; // InitJit already printed a failure message.

                    if (o.nameOfJit2 != null)
                    {
                        jit2 = JitInstance.InitJit(o.nameOfJit2, o.breakOnAssert, stInitJit, mc, o.forceJit2Options, o.jit2Options);
                        if (jit2 == null)
                            return (int)SpmiResult.JitFailedToInit;

                        // A module shared by the two JIT instances would corrupt the JIT's global state (such as
                        // g_jitHost and JitConfig), with spurious diffs and intermittent crashes.
                        if (jit2.Module == jit.Module)
                        {
                            Logger.LogError($"The baseline JIT ('{o.nameOfJit}') and diff JIT ('{o.nameOfJit2}') resolve to the same loaded module. " +
                                "They must be distinct files; copy one JIT to a different path.");
                            return (int)SpmiResult.JitFailedToInit;
                        }
                    }
                }

                // For asm diffs, the compile result of the first JIT is kept while compiling with the second.
                CompileResult crl = null;

                if (mc.WasEnvironmentChanged(jit.getEnvironment()))
                {
                    if (!jit.resetConfig(mc))
                        Logger.LogError("JIT can't reset environment");
                    if (o.nameOfJit2 != null && !jit2.resetConfig(mc))
                        Logger.LogError("JIT2 can't reset environment");
                }

                jittedCount++;
                Stopwatch st3 = Stopwatch.StartNew();
                ReplayResults res = jit.CompileMethod(mc, mcIndex, collectThroughput);
                st3.Stop();
                Logger.LogDebug($"Method {mcIndex} compiled{(o.nameOfJit2 == null ? "" : " by JIT1")} in {st3.Elapsed.TotalMilliseconds:F6}ms, " +
                    $"result {(int)res.Result}");

                if (res.Result == ReplayResult.Success)
                {
                    if (Logger.IsEnabled(LogLevel.Debug))
                        mc.cr.dumpToConsole(); // Dump the compile results when debug logging.
                }
                else if (res.Result == ReplayResult.Error)
                {
                    errorCount++;
                    Logger.LogError($"Method {mcIndex} of size {mc.methodSize} failed to load and compile correctly{(o.nameOfJit2 == null ? "" : " by JIT1")} ({o.nameOfJit}).");
                    if (errorCount == o.failureLimit)
                    {
                        Logger.LogError($"More than {o.failureLimit} methods failed{(o.nameOfJit2 == null ? "" : " by JIT1")}. Skip compiling remaining methods.");
                        goto doneRepeatCount;
                    }
                    if (o.reproName != null && o.indexCount == -1 && !WriteRepro(o.reproName, mcIndex, mc))
                        return (int)SpmiResult.GeneralFailure;
                    if (o.breakOnError)
                    {
                        if (o.indexCount == -1)
                            Logger.LogInfo($"HINT: to repro add '-c {mcIndex}' to cmdline");
                        Debugger.Break();
                    }
                }

                var res2 = new ReplayResults { Result = ReplayResult.Success };
                if (o.nameOfJit2 != null)
                {
                    // Keep the first JIT's compile result, and compile with the second JIT.
                    crl = mc.cr;
                    mc.cr = new CompileResult();

                    Stopwatch st4 = Stopwatch.StartNew();
                    res2 = jit2.CompileMethod(mc, mcIndex, collectThroughput);
                    st4.Stop();
                    Logger.LogDebug($"Method {mcIndex} compiled by JIT2 in {st4.Elapsed.TotalMilliseconds:F6}ms, result {(int)res2.Result}");

                    if (res2.Result == ReplayResult.Success)
                    {
                        if (Logger.IsEnabled(LogLevel.Debug))
                            mc.cr.dumpToConsole();
                    }
                    else if (res2.Result == ReplayResult.Error)
                    {
                        errorCount2++;
                        Logger.LogError($"Method {mcIndex} of size {mc.methodSize} failed to load and compile correctly by JIT2 ({o.nameOfJit2}).");
                        if (errorCount2 == o.failureLimit)
                        {
                            Logger.LogError($"More than {o.failureLimit} methods compilation failed by JIT2. Skip compiling remaining methods.");
                            goto doneRepeatCount;
                        }
                    }
                }

                if (res.Result == ReplayResult.Success && res2.Result == ReplayResult.Success)
                {
                    if (collectThroughput)
                    {
                        if (o.nameOfJit2 != null)
                        {
                            (crl.clockCyclesToCompile, mc.cr.clockCyclesToCompile) = ClosestTimes(jit.times, jit2.times);
                            methodStatsEmitter?.Emit(mcIndex, mc, crl.clockCyclesToCompile, mc.cr.clockCyclesToCompile);
                        }
                        else
                        {
                            mc.cr.clockCyclesToCompile = Math.Min(jit.times[0], jit.times[1]);
                            methodStatsEmitter?.Emit(mcIndex, mc, mc.cr.clockCyclesToCompile, 0);
                        }
                    }
                    else
                    {
                        methodStatsEmitter?.Emit(mcIndex, mc, ulong.MaxValue, ulong.MaxValue);
                    }

                    if (o.applyDiff)
                    {
                        // Diffing needs two compile results: from two JIT invocations, or one from the method context file.
                        NearDifferResult diffResult = NearDifferResult.Failure;
                        if (crl.AllocMem == null || mc.cr.AllocMem == null)
                        {
                            Logger.LogError($"method {mcIndex} is missing a compileResult, cannot do diffing");
                        }
                        else
                        {
                            diffResult = InvokeNearDiffer(nearDiffer, mc, crl, mcIndex);
                            switch (diffResult)
                            {
                                case NearDifferResult.SuccessWithDiff:
                                    // This is a difference in the asm of the two JITs, not a replay failure. It's in the
                                    // details if there are any, and otherwise in the failing method context list.
                                    diffsCount++;
                                    if (o.details == null)
                                        failingToReplayMCL.AddMethodToMCL(mcIndex);
                                    break;
                                case NearDifferResult.Failure:
                                    if (o.mclFilename != null)
                                        failingToReplayMCL.AddMethodToMCL(mcIndex);
                                    break;
                            }
                        }

                        if (detailsCsv != null)
                            PrintDiffsCsvRow(detailsCsv, mcIndex, (uint)mcb.size, res, res2, hasDiff: diffResult != NearDifferResult.SuccessWithoutDiff);
                    }
                    else if (detailsCsv != null)
                    {
                        PrintReplayCsvRow(detailsCsv, mcIndex, (uint)mcb.size, res);
                    }
                }
                else
                {
                    failToReplayCount++;

                    // Methods that don't compile because of missing JIT-EE information are still added to the
                    // failing method context list, but without a repro.
                    if (o.mclFilename != null)
                        failingToReplayMCL.AddMethodToMCL(mcIndex);

                    if (res.Result == ReplayResult.Miss || res2.Result == ReplayResult.Miss)
                        missingCount++;

                    if (detailsCsv != null)
                    {
                        if (o.applyDiff)
                            PrintDiffsCsvRow(detailsCsv, mcIndex, (uint)mcb.size, res, res2, hasDiff: false);
                        else
                            PrintReplayCsvRow(detailsCsv, mcIndex, (uint)mcb.size, res);
                    }
                }

                // Write a progress message if necessary.
                const int progressInterval = 500;
                if (jittedCount > 0 && jittedCount % progressInterval == 0)
                {
                    // When the progress is in bytes of method contexts rather than in method contexts, adding
                    // 'iter' is wildly inaccurate. But then (without a TOC or index list) there's no idea of how
                    // many method contexts the file has, or of the "average size" of one.
                    double percentComplete = 100.0 * (progressBeforeCurrentMC + iter) / totalWork;
                    int perSecond = (int)(progressInterval / st1.Elapsed.TotalSeconds);
                    if (o.applyDiff)
                        Logger.LogVerbose($" {percentComplete,2:F1}% - Loaded {loadedCount}  Jitted {jittedCount}  Diffs {diffsCount}  " +
                            $"FailedCompile {failToReplayCount} at {perSecond} per second");
                    else
                        Logger.LogVerbose($" {percentComplete,2:F1}% - Loaded {loadedCount}  Jitted {jittedCount}  FailedCompile " +
                            $"{failToReplayCount} at {perSecond} per second");
                    st1.Restart();
                }

                if (iter + 1 < o.repeatCount)
                    mc.Reset();
            }
        }

    doneRepeatCount:
        if (o.applyDiff)
            Logger.LogInfo(AsmDiffsSummary(loadedCount, jittedCount, failToReplayCount, excludedCount, missingCount, diffsCount));
        else
            Logger.LogInfo(Summary(loadedCount, jittedCount, failToReplayCount, excludedCount, missingCount));

        Logger.LogVerbose($"Total time: {st2.Elapsed.TotalMilliseconds:F6}ms");

        if (o.mclFilename != null)
            failingToReplayMCL.CloseMCL();

        if (errorCount > 0 || errorCount2 > 0)
            return (int)SpmiResult.Error;
        if (o.applyDiff && diffsCount > 0)
            return (int)SpmiResult.Diffs;
        if (missingCount > 0)
            return (int)SpmiResult.Misses;
        return (int)SpmiResult.Success;
    }

    // The times of the two JITs that are closest to each other.
    // TODO-Bug?: this may not get the lowest cycle times.
    private static (ulong, ulong) ClosestTimes(ulong[] times1, ulong[] times2)
    {
        static ulong Square(ulong a, ulong b) => unchecked((a - b) * (a - b));

        ulong dif1 = Square(times1[0], times2[0]);
        ulong dif2 = Square(times1[0], times2[1]);
        ulong dif3 = Square(times1[1], times2[0]);
        ulong dif4 = Square(times1[1], times2[1]);

        if (dif1 < dif2)
        {
            if (dif3 < dif4)
                return dif1 < dif3 ? (times1[0], times2[0]) : (times1[1], times2[0]);
            return dif1 < dif4 ? (times1[0], times2[0]) : (times1[1], times2[1]);
        }
        if (dif3 < dif4)
            return dif2 < dif3 ? (times1[0], times2[1]) : (times1[1], times2[0]);
        return dif2 < dif4 ? (times1[0], times2[1]) : (times1[1], times2[1]);
    }

    private static bool WriteRepro(string reproName, int mcIndex, MethodContext mc)
    {
        string fileName = $"{reproName}-{mcIndex}.mc";
        try
        {
            using var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            mc.saveToFile(stream);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.LogError($"Failed to open output '{fileName}'. GetLastError()={ErrorHandling.LastError(e)}");
            return false;
        }
        Logger.LogInfo($"Wrote out repro to '{fileName}'");
        return true;
    }

    private static NearDifferResult InvokeNearDiffer(NearDiffer nearDiffer, MethodContext mc, CompileResult crl, int mcIndex)
    {
        try
        {
            if (nearDiffer.compare(mc, crl, mc.cr))
                return NearDifferResult.SuccessWithoutDiff;

            Logger.LogIssue($"<ASM_DIFF> main method {mcIndex} of size {mc.methodSize} differs");
            return NearDifferResult.SuccessWithDiff;
        }
        catch (Exception e)
        {
            Logger.LogError($"main method {mcIndex} of size {mc.methodSize} failed to load and compile correctly.");
            Logger.LogError($"Exception thrown: {(e is SpmiException ? e.Message : e.ToString())}");
            return NearDifferResult.Failure;
        }
    }

    private static string ResultToString(ReplayResult result) => result switch
    {
        ReplayResult.Success => "Success",
        ReplayResult.Error => "Error",
        ReplayResult.Miss => "Miss",
        _ => "Unknown",
    };

    private static void PrintDiffsCsvHeader(FileWriter fw)
    {
        fw.Print("Context,Context size,Method full name,Tier name,Base result,Diff result,MinOpts,Has diff,Base instructions,Diff instructions");
        foreach (JitMetrics.Metric metric in JitMetrics.All)
            fw.Print($",Base {metric.Name},Diff {metric.Name}");
        fw.Print("\n");
    }

    private static void PrintDiffsCsvRow(FileWriter fw, int context, uint contextSize, in ReplayResults baseRes, in ReplayResults diffRes, bool hasDiff)
    {
        fw.Print($"{context},{contextSize},");
        fw.PrintQuotedCsvField(diffRes.CompileResults.MethodFullName ?? "");
        fw.Print($",{diffRes.CompileResults.TieringName ?? ""},{ResultToString(baseRes.Result)},{ResultToString(diffRes.Result)}," +
            $"{(diffRes.IsMinOpts ? "True" : "False")},{(hasDiff ? "True" : "False")},{baseRes.NumExecutedInstructions},{diffRes.NumExecutedInstructions}");
        for (int i = 0; i < JitMetrics.All.Length; i++)
            fw.Print($",{baseRes.CompileResults.Metrics.Format(i)},{diffRes.CompileResults.Metrics.Format(i)}");
        fw.Print("\n");
    }

    private static void PrintReplayCsvHeader(FileWriter fw)
    {
        fw.Print("Context,Context size,Method full name,Tier name,Result,MinOpts,Instructions");
        foreach (JitMetrics.Metric metric in JitMetrics.All)
            fw.Print($",{metric.Name}");
        fw.Print("\n");
    }

    private static void PrintReplayCsvRow(FileWriter fw, int context, uint contextSize, in ReplayResults res)
    {
        fw.Print($"{context},{contextSize},");
        fw.PrintQuotedCsvField(res.CompileResults.MethodFullName ?? "");
        fw.Print($",{res.CompileResults.TieringName ?? ""},{ResultToString(res.Result)},{(res.IsMinOpts ? "True" : "False")},{res.NumExecutedInstructions}");
        for (int i = 0; i < JitMetrics.All.Length; i++)
            fw.Print($",{res.CompileResults.Metrics.Format(i)}");
        fw.Print("\n");
    }
}
