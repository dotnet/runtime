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
using System.Diagnostics;

namespace StressLogAnalyzer;

internal sealed class StressLogAnalyzer(
    Func<IStressLog> stressLogContractFactory,
    IInterestingStringFinder stringFinder,
    IMessageFilter messageFilter,
    ThreadFilter threadFilter,
    ThreadFilter? earliestMessageFilter)
{
    public async Task<(ulong messagesProcessed, ulong messagesPrinted)> AnalyzeLogsAsync(TargetPointer logsPointer, TimeTracker timeTracker, GCThreadMap gcThreadMap, IStressMessageOutput messageOutput, CancellationToken token)
    {
        IStressLog outerLogContract = stressLogContractFactory();
        IEnumerable<ThreadStressLogData> logs = [.. outerLogContract.GetThreadStressLogs(logsPointer)];

        // The "end" timestamp is the timestamp of the most recent message.
        timeTracker.SetEndTimestamp(
            logs.Select(
                log => outerLogContract.GetStressMessages(log).FirstOrDefault().Timestamp)
            .Max());

        if (!threadFilter.HasAnyGCThreadFilter)
        {
            // If we don't have any GC thread filters, we can pre-filter on thread ID now.
            // Otherwise, we need to wait until we identify which threads are GC threads before
            // we can filter.
            logs = logs.Where(log => threadFilter.IncludeThread(log.ThreadId));
        }

        ConcurrentBag<(ThreadStressLogData thread, StressMsgData message)> earliestMessages = [];
        ConcurrentBag<(ThreadStressLogData thread, StressMsgData message, int numMessageOnThread)> allMessages = [];

        using ThreadLocal<ulong> numMessagesProcessed = new(() => 0, trackAllValues: true);

        var parallelOptions = new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = /*Debugger.IsAttached ? 1 : */Environment.ProcessorCount };

        using (ThreadLocal<IStressLog> stressLogContract = new(() => stressLogContractFactory()))
        {
            await Parallel.ForEachAsync(logs, parallelOptions, (log, ct) =>
            {
                // Stress logs are traversed from newest message to oldest, so the last message we see is the earliest one.
                StressMsgData? earliestMessage = null;
                List<StressMsgData> localMessages = [];
                bool includeThreadMessages = true;
                foreach (StressMsgData message in stressLogContract.Value!.GetStressMessages(log))
                {
                    numMessagesProcessed.Value++;
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
                        localMessages.Add(message);
                    }

                    if (!stringFinder.IsWellKnown(message.FormatString, out WellKnownString wellKnown))
                    {
                        // If the string isn't a well-known string, we've finished the processing we need to do.
                        // Go to the next message.
                        continue;
                    }

                    if (wellKnown == WellKnownString.GCSTART)
                    {
                        timeTracker.RecordGCStart(message.Args[0], message.Timestamp);
                    }
                    else if (wellKnown == WellKnownString.GCEND)
                    {
                        timeTracker.RecordGCEnd(message.Args[0], message.Timestamp);
                    }

                    if (gcThreadMap.TryRememberHeapForThread(log.ThreadId, wellKnown, message.Args, out ulong heapNumber, out bool isBackground)
                        && threadFilter.HasAnyGCThreadFilter
                        && !threadFilter.IncludeHeapThread(heapNumber, isBackground)
                        && !threadFilter.IncludeThread(log.ThreadId))
                    {
                        // As soon as we know that this thread corresponds to a thread that we definitely don't care about,
                        // we can skip processing
                        // the rest of the messages on this thread log.
                        // We also won't push these messages up for later processing.
                        includeThreadMessages = false;
                        break;
                    }
                }

                // If we didn't determine that this thread should be filtered, add the messages to the bag now.
                if (includeThreadMessages)
                {
                    for (int i = 0; i < localMessages.Count; i++)
                    {
                        allMessages.Add((log, localMessages[i], i));
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
        }

        IEnumerable<(ThreadStressLogData thread, StressMsgData message, int numMessageOnThread)> messages = allMessages;

        if (threadFilter.HasAnyGCThreadFilter)
        {
            // If we have GC thread filters, we have filtered out threads that we know are GC threads we don't care about.
            // However, if the thread is not a GC thread at all, then we haven't filtered it out yet.
            // Filter out uninteresting threads that aren't GC threads now.
            messages = messages.Where(message => gcThreadMap.ThreadHasHeap(message.thread.ThreadId) || threadFilter.IncludeThread(message.thread.ThreadId));
        }

        // Now that we know all GC times, we can filter out messages that aren't in interesting GC time ranges.
        messages = messages.Where(message => timeTracker.IsInInterestingGCTimeRange(message.message.Timestamp));

        // Order by timestamp and then by thread id
        messages = messages.OrderByDescending(message => (message.message.Timestamp, message.thread.ThreadId))
            .ThenBy(message => message.numMessageOnThread);

        ulong messagesPrinted = 0;
        foreach (var message in messages)
        {
            messagesPrinted++;
            await messageOutput.OutputMessageAsync(message.thread, message.message).ConfigureAwait(false);
        }

        if (earliestMessageFilter is not null)
        {
            await messageOutput.OutputLineAsync("\nEarliest messages:").ConfigureAwait(false);

            // TODO: Sort messages by timestamp
            foreach ((ThreadStressLogData thread, StressMsgData message) in earliestMessages.OrderBy(message => (message.message.Timestamp, message.thread.ThreadId)))
            {
                await messageOutput.OutputMessageAsync(thread, message).ConfigureAwait(false);
            }
        }

        return (numMessagesProcessed.Values.Aggregate(0ul, (acc, val) => acc + val), messagesPrinted);
    }
}
