// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace StressLogAnalyzer;

internal sealed class TimeTracker(ulong startTimestamp, ulong tickFrequency, TimeRange timeRange, IntegerRange? interestingGCRange)
{
    private ulong _firstGCStart;
    private ulong _lastGCEnd = ulong.MaxValue;

    public double TicksToSecondsFromStart(ulong timestamp)
        => (timestamp - startTimestamp) / (double)tickFrequency;

    public void RecordGCStart(ulong gcIndex, ulong timestamp)
    {
        if (interestingGCRange is null)
        {
            // If we have no interesting GC range, then we don't need to track any GC times.
            return;
        }

        if (gcIndex < interestingGCRange.Value.Start || interestingGCRange.Value.End < gcIndex)
        {
            // If this gc is out of the interesting GC range, then we don't need to track it.
            return;
        }

        if (Volatile.Read(ref _firstGCStart) == 0)
        {
            // If we haven't recorded any GC start times yet, then this is the first one.
            // Use Interlocked.CompareExchange to ensure that only one thread initializes the first GC start time.
            Interlocked.CompareExchange(ref _firstGCStart, timestamp, 0);
        }

        // Record this GC start time as the first GC start time if it's earlier than the current first GC start time.
        ulong firstGCStart = Volatile.Read(ref _firstGCStart);
        while (timestamp < firstGCStart)
        {
            firstGCStart = Interlocked.CompareExchange(ref _firstGCStart, timestamp, firstGCStart);
        }
    }

    public void RecordGCEnd(ulong gcIndex, ulong timestamp)
    {
        if (interestingGCRange is null)
        {
            // If we have no interesting GC range, then we don't need to track any GC times.
            return;
        }

        if (gcIndex < interestingGCRange.Value.Start || interestingGCRange.Value.End < gcIndex)
        {
            // If this gc is out of the interesting GC range, then we don't need to track it.
            return;
        }

        if (Volatile.Read(ref _lastGCEnd) == ulong.MaxValue)
        {
            // If we haven't recorded any GC end times yet, or if the last GC end time is currently the end of the log,
            // then this is the last  interesting one.
            // Use Interlocked.CompareExchange to ensure that only one thread initializes the last GC end time.
            Interlocked.CompareExchange(ref _lastGCEnd, timestamp, ulong.MaxValue);
        }

        // Record this GC end time as the last GC end time if it's later than the current first GC start time.
        ulong lastGCEnd = Volatile.Read(ref _lastGCEnd);
        while (lastGCEnd < timestamp)
        {
            lastGCEnd = Interlocked.CompareExchange(ref _lastGCEnd, timestamp, lastGCEnd);
        }
    }

    public void SetEndTimestamp(ulong endTimestamp)
    {
        double recordedTime = TicksToSecondsFromStart(endTimestamp);
        if (timeRange is not null)
        {
            if (timeRange.StartTimestamp < 0)
            {
                timeRange = new TimeRange(Math.Max(recordedTime + timeRange.StartTimestamp, 0), recordedTime);
            }
            else if (timeRange.EndTimestamp == double.MaxValue)
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
        return timestamp >= _firstGCStart && timestamp <= _lastGCEnd;
    }
}
