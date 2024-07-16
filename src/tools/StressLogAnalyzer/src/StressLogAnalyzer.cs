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

namespace StressLogAnalyzer;

internal class StressLogAnalyzer(Target target, IStressLog stressLogContract, Options options, TextWriter output)
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
        ConcurrentBag<(ThreadStressLogData thread, StressMsgData message)> earliestMessages = [];
        ConcurrentBag<(ThreadStressLogData thread, StressMsgData message)> messages = [];

        await Parallel.ForEachAsync(logs, token, (log, ct) =>
        {
            // Stress logs are traversed from newest message to oldest, so the last message we see is the earliest one.
            StressMsgData? earliestMessage = null;
            foreach (StressMsgData message in stressLogContract.GetStressMessages(log))
            {
                token.ThrowIfCancellationRequested();

                InterestingStringFinder.WellKnownString? wellKnownString = null;
                bool shouldPrintMessage = options.IncludeAllMessages || FilterMessage(message, out wellKnownString);

                if (wellKnownString.HasValue)
                {
                    gcThreadMap.ProcessInterestingMessage(log.ThreadId, wellKnownString.Value, message.Args);
                    // TODO: Record GC Times
                }

                if (!shouldPrintMessage && options.ValueRanges is [_, ..] interestingValueRanges)
                {
                    shouldPrintMessage = interestingValueRanges.Any(range => message.Args.Any(arg => range.Start <= arg && arg <= range.End));
                }

                if (shouldPrintMessage)
                {
                    messages.Add((log, message));
                }

                earliestMessage = message;

                if (options.ThreadFilter.HasAnyGCThreadFilter
                    && gcThreadMap.ThreadHasHeap(log.ThreadId)
                    && !gcThreadMap.IncludeThread(log.ThreadId, options.ThreadFilter))
                {
                    // As soon as we know that this thread corresponds a GC heap that we don't want, we can skip the rest of the messages.
                    break;
                }
            }

            // If we're recording the earliest messages for this thread, do so now.
            if (earliestMessage is not null
                && options.EarliestMessageThreads is not null
                && gcThreadMap.IncludeThread(log.ThreadId, options.EarliestMessageThreads))
            {
                earliestMessages.Add((log, earliestMessage.Value));
            }
            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);

        // TODO: Re-filter out the messages we added before we knew a thread was a filtered-out GC thread

        StressMessageFormatter formatter = new(target, new DefaultSpecialPointerFormatter());

        // TODO: Sort messages by timestamp
        foreach (var message in messages)
        {
            // TODO: Write out thread id
            await output.WriteLineAsync(formatter.GetFormattedMessage(message.message)).ConfigureAwait(false);
        }

        await output.WriteLineAsync("\nEarliest messages:").ConfigureAwait(false);

        // TODO: Sort messages by timestamp
        foreach (var message in earliestMessages)
        {
            await output.WriteLineAsync(formatter.GetFormattedMessage(message.message)).ConfigureAwait(false);
        }
    }

    private bool CheckWellKnownMessageRanges(InterestingStringFinder.WellKnownString wellKnownString, IReadOnlyList<TargetPointer> args)
    {
        switch (wellKnownString)
        {
            case InterestingStringFinder.WellKnownString.PLAN_PLUG:
            case InterestingStringFinder.WellKnownString.PLAN_PINNED_PLUG:
            {
                ulong gapSize = args[0];
                ulong plugStart = args[1];
                ulong gapStart = plugStart - gapSize;
                ulong plugEnd = args[2];
                return RangeIsInteresting(gapStart, plugEnd);
            }
            case InterestingStringFinder.WellKnownString.GCMEMCOPY:
                return RangeIsInteresting(args[0], args[2]) || RangeIsInteresting(args[1], args[3]);
            case InterestingStringFinder.WellKnownString.MAKE_UNUSED_ARRAY:
                return RangeIsInteresting(args[0], args[1]);
            case InterestingStringFinder.WellKnownString.RELOCATE_REFERENCE:
            {
                ulong src = args[0];
                ulong destFrom = args[1];
                ulong destTo = args[2];

                foreach (IntegerRange filter in options.ValueRanges)
                {
                    if ((filter.End < src || src > filter.Start)
                        && (filter.End < destFrom || destFrom > filter.Start)
                        && (filter.End < destTo || destTo > filter.Start))
                    {
                        continue;
                    }
                    return true;
                }

                return false;
            }
        }
        return false;

        bool RangeIsInteresting(ulong start, ulong end)
        {
            foreach (IntegerRange filter in options.ValueRanges)
            {
                if (filter.End < start || end > filter.Start)
                {
                    continue;
                }
                return true;
            }

            return false;
        }
    }

    private static uint GcLogLevel(uint facility)
    {
        if ((facility & ((uint)LogFacility.LF_ALWAYS | 0xfffeu | (uint)LogFacility.LF_GC)) == (uint)(LogFacility.LF_ALWAYS | LogFacility.LF_GC))
        {
            return (facility >> 16) & 0x7fff;
        }
        return 0;
    }

    private bool FilterMessage(StressMsgData message, out InterestingStringFinder.WellKnownString? wellKnownString)
    {
        wellKnownString = null;
        // Filter out messages by log facility immediately.
        if (options.IgnoreFacility != 0)
        {
            if ((message.Facility & ((uint)LogFacility.LF_ALWAYS | 0xfffe | (uint)LogFacility.LF_GC)) == ((uint)LogFacility.LF_ALWAYS | (uint)LogFacility.LF_GC))
            {
                // specially encoded GC message including dprintf level
                if ((options.IgnoreFacility & (uint)LogFacility.LF_GC) != 0)
                {
                    return false;
                }
            }
            else if ((options.IgnoreFacility & message.Facility) != 0)
            {
                return false;
            }
        }

        bool shouldPrintMessage = _messageStringChecker.IsInteresting(message.FormatString, out wellKnownString);

        if (wellKnownString.HasValue)
        {
            if (CheckWellKnownMessageRanges(wellKnownString.Value, message.Args))
            {
                return true;
            }
        }

        if (options.LevelFilter is not [])
        {
            // Check level and facility
            uint level = GcLogLevel(message.Facility);
            shouldPrintMessage |= options.LevelFilter.Any(filter => filter.Start <= level && level <= filter.End);
        }

        return shouldPrintMessage;
    }

    [Flags]
    private enum LogFacility : uint
    {
        LF_GC = 0x00000001,
        LF_ALWAYS = 0x80000000,
    }
}
