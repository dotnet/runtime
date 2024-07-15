// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader;
using System.Buffers;

namespace StressLogAnalyzer;

internal class StressLogAnalyzer(Target target, IStressLog stressLogContract, Options options)
{
    private readonly InterestingStringFinder _messageStringChecker = new(target, options.FormatFilter ?? [], options.FormatPrefixFilter ?? [], options.IncludeDefaultMessages);

    public async Task AnalyzeLogsAsync(TargetPointer logsPointer, CancellationToken token)
    {
        IEnumerable<ThreadStressLogData> logs = [.. stressLogContract.GetThreadStressLogs(logsPointer)];

        if (!options.ThreadFilter.HasAnyGCThreadFilter)
        {
            // If we don't have any GC thread filters, we can pre-filter on thread ID now.
            // Otherwise, we need to wait until we identify which threads are GC threads before
            // we can filter.
            logs = logs.Where(log => options.ThreadFilter.IncludeThread(log.ThreadId));
        }

        GCHeapMap gcThreadMap = new();
        ConcurrentBag<(ThreadStressLogData thread, StressMsgData message)> messages = [];

        await Parallel.ForEachAsync(logs, token, (log, ct) =>
        {
            foreach (StressMsgData message in stressLogContract.GetStressMessages(log))
            {
                token.ThrowIfCancellationRequested();

                bool isInterestingMessage = _messageStringChecker.IsInteresting(message.FormatString, out InterestingStringFinder.WellKnownString? wellKnownString);

                if (wellKnownString.HasValue)
                {
                    switch (wellKnownString.Value)
                    {
                        case InterestingStringFinder.WellKnownString.THREAD_WAIT:
                        case InterestingStringFinder.WellKnownString.THREAD_WAIT_DONE:
                        case InterestingStringFinder.WellKnownString.MARK_START:
                        case InterestingStringFinder.WellKnownString.PLAN_START:
                        case InterestingStringFinder.WellKnownString.RELOCATE_START:
                        case InterestingStringFinder.WellKnownString.RELOCATE_END:
                        case InterestingStringFinder.WellKnownString.COMPACT_START:
                        case InterestingStringFinder.WellKnownString.COMPACT_END:
                            gcThreadMap.RememberHeapForThread(log.ThreadId, (ulong)message.Args[0], false);
                            break;

                        case InterestingStringFinder.WellKnownString.DESIRED_NEW_ALLOCATION:
                            if (message.Args[1] <= 1)
                            {
                                // do this only for gen 0 and 1, because otherwise it
                                // may be background GC
                                gcThreadMap.RememberHeapForThread(log.ThreadId, (ulong)message.Args[0], false);
                            }
                            break;

                        case InterestingStringFinder.WellKnownString.START_BGC_THREAD:
                            gcThreadMap.RememberHeapForThread(log.ThreadId, (ulong)message.Args[0], true);
                            break;
                    }
                }

                messages.Add((log, message));
            }
            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);
    }
}
