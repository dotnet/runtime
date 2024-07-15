// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace StressLogAnalyzer;

public record struct IntegerRange(ulong Start, ulong End);

public record struct TimeRange(double StartTimestamp, double EndTimestamp);

public sealed class ThreadFilter
{
    // Filter includes all threads
    private bool _allThreads;
    private readonly HashSet<ulong> _backgroundGCThreads = [];
    private readonly HashSet<ulong> _foregroundGCThreads = [];
    private readonly HashSet<ulong> _threads = [];

    public void AddThread(ulong threadId)
    {
        _threads.Add(threadId);
    }

    public void AddBackgroundGCThread(ulong heapNumber)
    {
        _backgroundGCThreads.Add(heapNumber);
    }

    public void AddForegroundGCThread(ulong heapNumber)
    {
        _foregroundGCThreads.Add(heapNumber);
    }

    public bool HasAnyFilter => !_allThreads;

    public bool HasAnyGCThreadFilter => _backgroundGCThreads.Count > 0 || _foregroundGCThreads.Count > 0;

    public bool IncludeThread(ulong threadId)
    {
        if (_allThreads)
        {
            return true;
        }

        return _threads.Contains(threadId);
    }

    public bool IncludeHeapThread(ulong heapNumber, bool isBackground)
    {
        if (_allThreads)
        {
            return true;
        }

        return isBackground ? _backgroundGCThreads.Contains(heapNumber) : _foregroundGCThreads.Contains(heapNumber);
    }

    public ThreadFilter(IEnumerable<string> threadIds)
    {
        bool any = false;
        foreach (string threadIdSet in threadIds)
        {
            foreach (string threadId in threadIdSet.Split(','))
            {
                any = true;
                if (threadId.StartsWith("GC", StringComparison.OrdinalIgnoreCase))
                {
                    _foregroundGCThreads.Add(ulong.Parse(threadId.AsSpan()[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                }
                else if (threadId.StartsWith("BG", StringComparison.OrdinalIgnoreCase))
                {
                    _backgroundGCThreads.Add(ulong.Parse(threadId.AsSpan()[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                }
                else
                {
                    _threads.Add(ulong.Parse(threadId, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                }
            }
        }

        if (!any)
        {
            _allThreads = true;
        }
    }
}

public sealed record Options(
    FileInfo InputFile,
    FileInfo? OutputFile,
    IntegerRange[]? ValueRanges,
    TimeRange Time,
    bool IncludeAllMessages,
    bool IncludeDefaultMessages,
    IReadOnlyList<IntegerRange>? LevelFilter,
    IntegerRange? GCIndex,
    ulong[]? IncludeFacility,
    ThreadFilter? EarliestMessageThreads,
    bool PrintHexThreadIds,
    ThreadFilter ThreadFilter,
    bool PrintFormatStrings,
    string[]? FormatPrefixFilter,
    string[]? FormatFilter);
