// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

/// <summary>
/// Execute a given number of mutually independent build subprocesses represented by an array of
/// command lines with a given degree of parallelization.
/// </summary>
public sealed class ParallelRunner
{
    /// <summary>
    /// Helper class for launching mutually independent build subprocesses in parallel.
    /// It supports launching the processes and optionally redirecting their standard and
    /// error output streams to prevent them from interleaving in the final build output log.
    /// Multiple instances of this class representing the individual running processes
    /// can exist at the same time.
    /// </summary>
    class ProcessSlot
    {
        /// <summary>
        /// Process slot index (used for diagnostic purposes)
        /// </summary>
        private readonly int _slotIndex;

        /// <summary>
        /// Event used to report that a process has exited
        /// </summary>
        private readonly AutoResetEvent _processExitEvent;

        /// <summary>
        /// Process object
        /// </summary>
        private ProcessRunner _processRunner;

        /// <summary>
        /// Constructor stores global slot parameters and initializes the slot state machine
        /// </summary>
        /// <param name="slotIndex">Process slot index used for diagnostic purposes</param>
        /// <param name="processExitEvent">Event used to report process exit</param>
        public ProcessSlot(int slotIndex, AutoResetEvent processExitEvent)
        {
            _slotIndex = slotIndex;
            _processExitEvent = processExitEvent;
        }

        /// <summary>
        /// Launch a new process.
        /// </summary>
        /// <param name="processInfo">application to execute</param>
        /// <param name="jittedMethods">Jitted method collector</param>
        /// <param name="processIndex">Numeric index used to prefix messages pertaining to this process in the console output</param>
        /// <param name="processCount">Total number of processes being executed (used for displaying progress)</param>
        /// <param name="progressIndex">Number of processes that have already finished (for displaying progress)</param>
        /// <param name="failureCount">Number of pre-existing failures in this parallel build step (for displaying progress)</param>
        public void Launch(ProcessInfo processInfo, ReadyToRunJittedMethods jittedMethods, int processIndex, int processCount, int progressIndex, int failureCount)
        {
            Debug.Assert(_processRunner == null);
            Console.WriteLine($"{processIndex} / {processCount} ({(progressIndex * 100 / processCount)}%, {failureCount} failed): " +
                $"launching: {processInfo.Parameters.ProcessPath} {processInfo.Parameters.Arguments}");

            _processRunner = new ProcessRunner(processInfo, processIndex, processCount, jittedMethods, _processExitEvent);
        }

        public bool IsAvailable(ref int progressIndex, ref int failureCount)
        {
            if (_processRunner == null)
            {
                return true;
            }
            if (!_processRunner.IsAvailable(ref progressIndex, ref failureCount))
            {
                return false;
            }
            _processRunner.Dispose();
            _processRunner = null;
            return true;
        }
    }

    /// <summary>
    /// Execute a given set of mutually independent build commands with given degree of
    /// parallelism.
    /// </summary>
    /// <param name="processesToRun">Processes to execute in parallel</param>
    /// <param name="degreeOfParallelism">Maximum number of processes to execute in parallel, 0 = logical processor count</param>
    public static void Run(IEnumerable<ProcessInfo> processesToRun, int degreeOfParallelism = 0, bool measurePerf = false)
    {
        if (degreeOfParallelism == 0)
        {
            degreeOfParallelism = Environment.ProcessorCount;
        }

        List<ProcessInfo> processList = new List<ProcessInfo>();
        bool collectEtwTraces = false;
        collectEtwTraces |= measurePerf;
        foreach (ProcessInfo process in processesToRun)
        {
            if (process.Construct())
            {
                processList.Add(process);
                collectEtwTraces |= process.Parameters.CollectJittedMethods;
            }
        }

        processList.Sort((a, b) => b.Parameters.CompilationCostHeuristic.CompareTo(a.Parameters.CompilationCostHeuristic));

        int processCount = processList.Count;
        if (processCount < degreeOfParallelism)
        {
            // We never need a higher DOP than the number of process to execute
            degreeOfParallelism = processCount;
        }

        if (collectEtwTraces)
        {
            // In ETW collection mode, separate the processes to run into smaller batches as we need to keep
            // the process objects alive for the entire duration of the parallel execution, otherwise PID's
            // may get recycled by the OS and we can no longer back-translate PIDs in events to the logical
            // process executions. Without parallelization, we simply run the processes one by one.
            int etwCollectionBatching = (degreeOfParallelism == 1 ? 1 : 10);
            int failureCount = 0;

            for (int batchStartIndex = 0; batchStartIndex < processCount; batchStartIndex += etwCollectionBatching)
            {
                int batchEndIndex = Math.Min(batchStartIndex + etwCollectionBatching, processCount);
                BuildEtwProcesses(
                    startIndex: batchStartIndex,
                    endIndex: batchEndIndex,
                    totalCount: processCount,
                    failureCount: failureCount,
                    processList,
                    degreeOfParallelism,
                    measurePerf);

                for (int processIndex = batchStartIndex; processIndex < batchEndIndex; processIndex++)
                {
                    if (!processList[processIndex].Succeeded)
                    {
                        failureCount++;
                    }
                }
            }
        }
        else
        {
            BuildProjects(startIndex: 0, endIndex: processCount, totalCount: processCount, failureCount: 0, processList, null, degreeOfParallelism);
        }
    }

    private static void BuildEtwProcesses(
        int startIndex,
        int endIndex,
        int totalCount,
        int failureCount,
        List<ProcessInfo> processList,
        int degreeOfParallelism,
        bool measurePerf)
    {
        using (TraceEventSession traceEventSession = new TraceEventSession("ReadyToRunTestSession"))
        {
            traceEventSession.EnableProvider(ClrTraceEventParser.ProviderGuid, TraceEventLevel.Verbose, (ulong)(ClrTraceEventParser.Keywords.Jit | ClrTraceEventParser.Keywords.Loader));
            int warmupRuns = 0;
            int realRuns = 1;
            PerfEventSourceListener perfMeasurer = null;
            if (measurePerf)
            {
                Debug.Assert(processList.Count == 1);
                warmupRuns = 1;
                realRuns = 5;
                perfMeasurer = new PerfEventSourceListener(traceEventSession, warmupRuns, realRuns);
            }
            using (ReadyToRunJittedMethods jittedMethods = new ReadyToRunJittedMethods(traceEventSession, processList, startIndex, endIndex))
            {
                Task.Run(() =>
                {
                    // Warmup runs
                    if (measurePerf)
                    {
                        Console.WriteLine("Warmup runs:");
                        for (int run = 0; run < warmupRuns; ++run)
                        {
                            BuildProjects(startIndex, endIndex, totalCount, failureCount, processList, jittedMethods, degreeOfParallelism);
                        }
                        // Wait for all the warmup events to come in before starting the real run so there is no interference
                        perfMeasurer.WaitForWarmupFinished();
                        Console.WriteLine("Real runs:");
                    }

                    for (int run = 0; run < realRuns; ++run)
                    {
                        BuildProjects(startIndex, endIndex, totalCount, failureCount, processList, jittedMethods, degreeOfParallelism);
                    }
                    if (measurePerf)
                    {
                        perfMeasurer.PrintPerfResults();
                    }
                    traceEventSession.Stop();
                });
            }
            traceEventSession.Source.Process();
        }

        // Append jitted method info to the logs
        for (int index = startIndex; index < endIndex; index++)
        {
            ProcessInfo processInfo = processList[index];
            if (processInfo.Parameters.CollectJittedMethods)
            {
                using (StreamWriter processLogWriter = new StreamWriter(processInfo.Parameters.LogPath, append: true))
                {
                    if (processInfo.JittedMethods != null)
                    {
                        processLogWriter.WriteLine($"Jitted methods ({processInfo.JittedMethods.Sum(moduleMethodsKvp => moduleMethodsKvp.Value.Count)} total):");
                        foreach (KeyValuePair<string, HashSet<string>> jittedMethodsPerModule in processInfo.JittedMethods)
                        {
                            foreach (string method in jittedMethodsPerModule.Value)
                            {
                                processLogWriter.WriteLine(jittedMethodsPerModule.Key + " -> " + method);
                            }
                        }
                    }
                    else
                    {
                        processLogWriter.WriteLine("Jitted method info not available");
                    }
                }
            }
        }
    }

    private static void BuildProjects(int startIndex, int endIndex, int totalCount, int failureCount, List<ProcessInfo> processList, ReadyToRunJittedMethods jittedMethods, int degreeOfParallelism)
    {
        using (AutoResetEvent processExitEvent = new AutoResetEvent(initialState: false))
        {
            ProcessSlot[] processSlots = new ProcessSlot[degreeOfParallelism];
            for (int index = 0; index < degreeOfParallelism; index++)
            {
                processSlots[index] = new ProcessSlot(index, processExitEvent);
            }

            int progressIndex = startIndex;
            for (int index = startIndex; index < endIndex; index++)
            {
                ProcessInfo processInfo = processList[index];

                // Allocate a process slot, potentially waiting on the exit event
                // when all slots are busy (running)
                ProcessSlot freeSlot = null;
                do
                {
                    foreach (ProcessSlot slot in processSlots)
                    {
                        if (slot.IsAvailable(ref progressIndex, ref failureCount))
                        {
                            freeSlot = slot;
                            break;
                        }
                    }
                    if (freeSlot == null)
                    {
                        // All slots are busy - wait for a process to finish
                        processExitEvent.WaitOne(200);
                    }
                }
                while (freeSlot == null);

                freeSlot.Launch(processInfo, jittedMethods, index + 1, totalCount, progressIndex, failureCount);
            }

            // We have launched all the commands, now wait for all processes to finish
            bool activeProcessesExist;
            do
            {
                activeProcessesExist = false;
                foreach (ProcessSlot slot in processSlots)
                {
                    if (!slot.IsAvailable(ref progressIndex, ref failureCount))
                    {
                        activeProcessesExist = true;
                    }
                }
                if (activeProcessesExist)
                {
                    processExitEvent.WaitOne();
                }
            }
            while (activeProcessesExist);
        }
    }
}
