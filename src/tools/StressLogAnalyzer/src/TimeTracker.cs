// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace StressLogAnalyzer;

internal class TimeTracker(ulong startTimestamp, ulong tickFrequency, TimeRange timeRange, IntegerRange? gcIndex)
{
    private readonly ConcurrentDictionary<ulong, (double start, double end)> _gcTimes = [];

    public double TicksToSecondsFromStart(ulong timestamp)
        => (timestamp - startTimestamp) / (double)tickFrequency;

    public void RecordGCStart(ulong gcIndex, ulong timestamp)
    {
        double recordedTime = TicksToSecondsFromStart(timestamp);
        _gcTimes.AddOrUpdate(
            gcIndex,
            (recordedTime, recordedTime),
            (newStart, current) => (newStart, current.end));
    }

    public void RecordGCEnd(ulong gcIndex, ulong timestamp)
    {
        double recordedTime = TicksToSecondsFromStart(timestamp);
        _gcTimes.AddOrUpdate(
            gcIndex,
            (0, recordedTime),
            (newEnd, current) => (current.start, newEnd));
    }

    public void SetEndTimestamp(ulong endTimestamp)
    {
        double recordedTime = TicksToSecondsFromStart(endTimestamp);
        if (timeRange is not null)
        {
            if (timeRange.StartTimestamp < 0)
            {
                timeRange = new TimeRange(Math.Max(recordedTime - timeRange.StartTimestamp, 0), endTimestamp);
            }
            else
            {
                timeRange = timeRange with { EndTimestamp = recordedTime };
            }
        }
    }

    public enum TimeQueryResult
    {
        BeforeRange,
        InRange,
        AfterRange
    }

    public TimeQueryResult IsInTimeRange(ulong timestamp)
    {
        if (timeRange is null)
        {
            // If we have no time range, then we'll treat all messages as in range.
            return TimeQueryResult.InRange;
        }

        double recordedTime = TicksToSecondsFromStart(timestamp);
        if (recordedTime < timeRange.StartTimestamp)
        {
            return TimeQueryResult.BeforeRange;
        }
        if (recordedTime > timeRange.EndTimestamp)
        {
            return TimeQueryResult.AfterRange;
        }

        return TimeQueryResult.InRange;
    }

    public bool IsInInterestingGCTimeRange(ulong timestamp)
    {
        if (gcIndex is null)
        {
            // If we have no interesting GC indices, then we'll treat all messages
            // as occuring during an interesting GC time.
            return true;
        }

        // If this timestamp occured during any GC that was recorded as interesting,
        // then it's an interesting GC time.
        double recordedTime = TicksToSecondsFromStart(timestamp);
        return _gcTimes.Any(
            times => gcIndex.Value.Start <= times.Key
                && times.Key <= gcIndex.Value.End
                && times.Value.start < recordedTime
                && recordedTime < times.Value.end);
    }
}
