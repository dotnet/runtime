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
using System.Collections;
using StressLogAnalyzer.Filters;
using StressLogAnalyzer.Output;

namespace StressLogAnalyzer;

internal sealed class StressLogAnalyzer(
    IStressLog stressLogContract,
    IInterestingStringFinder stringFinder,
    IMessageFilter messageFilter,
    ThreadFilter threadFilter,
    ThreadFilter? earliestMessageFilter)
{
    public async Task AnalyzeLogsAsync(TargetPointer logsPointer, TimeTracker timeTracker, GCThreadMap gcThreadMap, IStressMessageOutput messageOutput, CancellationToken token)
    {
        IEnumerable<ThreadStressLogData> logs = [.. stressLogContract.GetThreadStressLogs(logsPointer)];

        // The "end" timestamp is the timestamp of the most recent message.
        timeTracker.SetEndTimestamp(
            logs.Select(
                log => stressLogContract.GetStressMessages(log).FirstOrDefault().Timestamp)
            .Max());

        if (!threadFilter.HasAnyGCThreadFilter)
        {
            // If we don't have any GC thread filters, we can pre-filter on thread ID now.
            // Otherwise, we need to wait until we identify which threads are GC threads before
            // we can filter.
            logs = logs.Where(log => threadFilter.IncludeThread(log.ThreadId));
        }

        ConcurrentBag<(ThreadStressLogData thread, StressMsgData message)> earliestMessages = [];
        ConcurrentBag<(ThreadStressLogData thread, StressMsgData message)> allMessages = [];

        await Parallel.ForEachAsync(logs, token, (log, ct) =>
        {
            // Stress logs are traversed from newest message to oldest, so the last message we see is the earliest one.
            StressMsgData? earliestMessage = null;
            foreach (StressMsgData message in stressLogContract.GetStressMessages(log))
            {
                token.ThrowIfCancellationRequested();

                earliestMessage = message;

                TimeTracker.TimeQueryResult time = timeTracker.IsInTimeRange(message.Timestamp);

                if (time == TimeTracker.TimeQueryResult.AfterRange)
                {
                    // We still haven't reached the interesting time range.
                    // Skip this message.
                    continue;
                }
                else if (time == TimeTracker.TimeQueryResult.BeforeRange)
                {
                    // We've passed the interesting time range.
                    // We can stop processing this thread.
                    break;
                }
                // Otherwise, we're in the interesting time range.

                if (messageFilter.IncludeMessage(message))
                {
                    allMessages.Add((log, message));
                }

                if (stringFinder.IsWellKnown(out WellKnownString? wellKnown))
                {
                    gcThreadMap.ProcessInterestingMessage(log.ThreadId, wellKnown.Value, message.Args);
                    if (wellKnown.Value == WellKnownString.GCSTART)
                    {
                        timeTracker.RecordGCStart(message.Args[0], message.Timestamp);
                    }
                    else if (wellKnown.Value == WellKnownString.GCEND)
                    {
                        timeTracker.RecordGCEnd(message.Args[0], message.Timestamp);
                    }
                }

                if (threadFilter.HasAnyGCThreadFilter
                    && gcThreadMap.ThreadHasHeap(log.ThreadId)
                    && !gcThreadMap.IncludeThread(log.ThreadId, threadFilter))
                {
                    // As soon as we know that this thread corresponds a GC heap that we don't want, we can skip the rest of the messages.
                    break;
                }
            }

            // If we're recording the earliest messages for this thread, do so now.
            if (earliestMessage is not null
                && earliestMessageFilter is not null
                && gcThreadMap.IncludeThread(log.ThreadId, earliestMessageFilter))
            {
                earliestMessages.Add((log, earliestMessage.Value));
            }
            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);

        IEnumerable<(ThreadStressLogData thread, StressMsgData message)> messages = allMessages;

        if (threadFilter.HasAnyGCThreadFilter)
        {
            // Re-filter out the messages we added before we knew a thread was a filtered-out GC thread
            messages = messages.Where(message => gcThreadMap.IncludeThread(message.thread.ThreadId, threadFilter));
        }

        // Now that we know all GC times, we can filter out messages that aren't in interesting GC time ranges.
        messages = messages.Where(message => timeTracker.IsInInterestingGCTimeRange(message.message.Timestamp));

        // Order by timestamp and then by thread id
        messages = messages.OrderBy(message => (message.message.Timestamp, message.thread.ThreadId));

        foreach (var message in messages)
        {
            await messageOutput.OutputMessageAsync(message.thread, message.message).ConfigureAwait(false);
        }

        await messageOutput.OutputLineAsync("\nEarliest messages:").ConfigureAwait(false);

        // TODO: Sort messages by timestamp
        foreach ((ThreadStressLogData thread, StressMsgData message) in earliestMessages.OrderBy(message => (message.message.Timestamp, message.thread.ThreadId)))
        {
            await messageOutput.OutputMessageAsync(thread, message).ConfigureAwait(false);
        }
    }
}
