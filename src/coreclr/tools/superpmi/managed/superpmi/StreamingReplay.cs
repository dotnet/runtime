// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;

namespace SuperPmi.Replay;

// Streaming mode: compiles the method contexts asked for by the lines of a file (or stdin), each a method context
// number optionally followed by "!key=value" force JIT options. Lines starting with '#' are ignored, and
// "quit", a 0 number, or the end of the file stop it.
internal static class StreamingReplay
{
    private const int CacheSize = 100;

    private struct CacheEntry
    {
        public MethodContext mc;
        public int age;
    }

    private static readonly CacheEntry[] s_cache = new CacheEntry[CacheSize];
    private static int s_count;
    private static int s_age;

    private static MethodContext getMethodContext(int index, MethodContextReader reader)
    {
        int i = 0;
        for (; i < s_count; i++)
        {
            if (s_cache[i].mc.index == index)
                break;
        }

        if (i == s_count)
        {
            Logger.LogDebug($"[streaming] loading MC {index} from file");
            if (i == CacheSize)
            {
                // The cache is full: evict the oldest entry.
                int oldestAge = s_age;
                int oldestEntry = -1;
                for (int j = 0; j < CacheSize; j++)
                {
                    if (s_cache[j].age < oldestAge)
                    {
                        oldestEntry = j;
                        oldestAge = s_cache[j].age;
                    }
                }

                Logger.LogDebug($"[streaming] evicting MC {s_cache[oldestEntry].mc.index} from cache");
                s_cache[oldestEntry].mc = null;
                i = oldestEntry;
            }
            else
            {
                s_count++;
            }

            reader.Reset([index]);
            MethodContextBuffer mcb = reader.GetNextMethodContext();
            if (mcb.Error() || !MethodContext.Initialize(index, mcb.buff, (uint)mcb.size, readCompileResults: false, out MethodContext mc))
                return null;

            s_cache[i].mc = mc;
        }
        else
        {
            Logger.LogDebug($"[streaming] found MC {index} in cache");
        }

        // Move it to the front.
        (s_cache[0], s_cache[i]) = (s_cache[i], s_cache[0]);
        s_cache[0].age = s_age++;
        return s_cache[0].mc;
    }

    public static int Run(Options o)
    {
        TextReader streamFile;
        try
        {
            streamFile = string.Equals(o.streamFile, "stdin", StringComparison.OrdinalIgnoreCase) ? Console.In : new StreamReader(o.streamFile);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.LogError($"Failed to open file '{o.streamFile}'. GetLastError()={ErrorHandling.LastError(e)}");
            return 1;
        }

        // Just one worker for now: all method selection is done with the stream file.
        using var reader = new MethodContextReader(o.nameOfInputMethodContextFile);
        if (!reader.isValid())
            return (int)SpmiResult.GeneralFailure;

        JitInstance jit = null;

        while (streamFile.ReadLine() is string line)
        {
            Logger.LogDebug($"[streaming] Request: '{line}'");

            if (line.StartsWith('#'))
                continue;

            if (line.StartsWith("quit", StringComparison.Ordinal))
            {
                Logger.LogDebug("[streaming] Quitting");
                break;
            }

            string[] tokens = line.Split('!', StringSplitOptions.RemoveEmptyEntries);
            int index = tokens.Length > 0 ? CommandLine.Atoi(tokens[0]) : 0;
            if (index == 0)
            {
                Logger.LogDebug("[streaming] Stopping");
                break;
            }

            Logger.LogDebug($"[streaming] Method {index}");

            LightWeightMap<uint, uint> forceJitOptions = null;
            bool skip = false;
            foreach (string option in tokens.AsSpan(1))
            {
                forceJitOptions ??= o.forceJitOptions?.Clone() ?? new LightWeightMap<uint, uint>();
                if (!CommandLine.ParseJitOption(option, out string key, out string value))
                {
                    Logger.LogInfo($"[streaming] unable to parse option '{option}'");
                    skip = true;
                    break;
                }
                CommandLine.AddJitOption(forceJitOptions, key, value);
            }

            if (skip)
                continue;

            Logger.LogDebug("[streaming] Launching...");
            MethodContext mc = getMethodContext(index, reader);
            if (mc == null)
                return (int)SpmiResult.GeneralFailure;

            if (mc.index != index)
            {
                Logger.LogDebug($"MC cache lookup failure, wanted index {index}, got index {mc.index}\n");
                return (int)SpmiResult.GeneralFailure;
            }

            if (jit == null)
            {
                Logger.LogDebug($"[streaming] loading jit {o.nameOfJit}");
                jit = JitInstance.InitJit(o.nameOfJit, o.breakOnAssert, new Stopwatch(), mc, forceJitOptions, o.jitOptions);
                if (jit == null)
                    return (int)SpmiResult.JitFailedToInit; // InitJit already printed a failure message.
            }
            else
            {
                jit.updateForceOptions(forceJitOptions);
                jit.resetConfig(mc);
            }

            Logger.LogDebug("[streaming] invoking jit");
            Logger.Flush();

            ReplayResults res = jit.CompileMethod(mc, reader.GetMethodContextIndex(), collectThroughput: false);
            if (res.Result == ReplayResult.Success)
            {
                if (Logger.IsEnabled(LogLevel.Debug))
                    mc.cr.dumpToConsole(); // Dump the compile results when debug logging.
            }
            else if (res.Result == ReplayResult.Error)
            {
                Logger.LogDebug("[streaming] jit compilation failed");
                Logger.LogError($"Method {reader.GetMethodContextIndex()} of size {mc.methodSize} failed to load and compile correctly ({o.nameOfJit}).");
            }

            // The protocol with clients is for them to read stdout, so let them know this is done.
            Logger.Out.Write($"[streaming] Done. Status={(int)res.Result}\n");
            Logger.Flush();

            mc.Reset();
        }

        return (int)SpmiResult.Success;
    }
}
