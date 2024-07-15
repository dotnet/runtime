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
    private readonly InterestingStringFinder _messageStringChecker = new(target, options.FormatFilter ?? [], options.FormatPrefixFilter ?? []);

    public async Task AnalyzeLogsAsync(TargetPointer logsPointer, CancellationToken token)
    {
        IEnumerable<ThreadStressLogData> logs = [.. stressLogContract.GetThreadStressLogs(logsPointer)];

        if (!options.ThreadFilter.HasAnyGCThreadFilter)
        {
            // If we don't have any GC thread filters, we can pre-filter on thread ID now
            logs = logs.Where(log => options.ThreadFilter.IncludeThread((uint)log.ThreadId));
        }

        ConcurrentDictionary<uint, (uint heap, bool background)> gcThreadMap = [];
        ConcurrentBag<(ThreadStressLogData thread, StressMsgData message)> messages = [];

        await Parallel.ForEachAsync(logs, token, (log, ct) =>
        {
            foreach (StressMsgData message in stressLogContract.GetStressMessages(log))
            {
                token.ThrowIfCancellationRequested();
                messages.Add((log, message));
            }
            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);
    }
}
