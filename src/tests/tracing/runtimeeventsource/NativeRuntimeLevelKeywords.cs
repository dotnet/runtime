using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;

using System.Buffers; // Copied from https://github.com/dotnet/runtime/pull/86370 by @davmason:
{
    // Access ArrayPool.Shared.Rent() before the test to avoid the deadlock reported
    // in https://github.com/dotnet/runtime/issues/86233. This is a real issue,
    // but only seen if you have a short lived EventListener and create EventSources
    // in your OnEventWritten callback so we don't expect customers to hit it.
    byte[] localBuffer = ArrayPool<byte>.Shared.Rent(10);
    Console.WriteLine($"buffer length={localBuffer.Length}");
}

Listener.NextLevel = EventLevel.Informational;
using Listener informational = new();

Listener.NextLevel = EventLevel.Verbose;
using Listener verbose = new();

Listener.NextLevel = EventLevel.LogAlways;
using Listener logAlways = new();

Listener.Event gCStart_V2 = new(1, EventLevel.Informational, 0xf00000000001L, "GCStart_V2");
Listener.Event gCAllocationTick_V4 = new(10, EventLevel.Verbose, 0xf00000000001L, "GCAllocationTick_V4");

for (Stopwatch stopwatch = Stopwatch.StartNew(); stopwatch.Elapsed.TotalSeconds < 10d;)
{
    for (int i = 0; i < 1000; i++)
    {
        GC.KeepAlive(new());
    }
    GC.Collect();

    if (informational.Contains(gCStart_V2) &&
        verbose.Contains(gCAllocationTick_V4))
    {
        Console.WriteLine($"\n{nameof(stopwatch.Elapsed.TotalSeconds)} {stopwatch.Elapsed.TotalSeconds}");
        break;
    }
}
try
{
    ThrowIfEvent(informational, gCStart_V2, false);
    ThrowIfEvent(informational, gCAllocationTick_V4);
    ThrowIfEvent(verbose, gCStart_V2, false);
    ThrowIfEvent(verbose, gCAllocationTick_V4, false);
    ThrowIfEvent(logAlways, gCStart_V2, false);
    ThrowIfEvent(logAlways, gCAllocationTick_V4, false);
}
finally
{
    informational.DumpEvents();
    verbose.DumpEvents();
    logAlways.DumpEvents();
}
return 100;

static void ThrowIfEvent(Listener listener, Listener.Event e, bool contains = true)
{
    if (listener.Contains(e) == contains)
    {
        throw new Exception($"{listener} {(contains ? "contains" : "doesn't contain")} {e}");
    }
}

internal sealed class Listener : EventListener
{
    public static string NextSourceName = "Microsoft-Windows-DotNETRuntime";
    public static EventLevel NextLevel = EventLevel.LogAlways;
    public static long NextKeywords = 1L;

    private readonly ConcurrentDictionary<Event, int> events = new();
    private string sourceName = "";
    private EventLevel level;
    private long keywords;

    public readonly record struct Event(int Id, EventLevel Level, long Keywords, string Name)
    {
        public override string ToString()
        {
            return $"new({Id,3}, {nameof(EventLevel)}.{Level,-13}, 0x{Keywords:x12}L, \"{Name}\");";
        }
    }

    public bool Contains(Event e)
    {
        return events.ContainsKey(e);
    }
    public void DumpEvents()
    {
        Console.WriteLine($"\n{this}\n\\");
        foreach (KeyValuePair<Event, int> e in events)
        {
            Console.WriteLine($"{e.Key} // {e.Value}");
        }
    }
    public override string ToString()
    {
        return $"\"{sourceName}\": [{nameof(EventLevel)}.{level,-13}, 0x{keywords:x12}L] ({events.Count,3}, {events.Values.Sum()})";
    }

    protected override void OnEventSourceCreated(EventSource source)
    {
        if (string.IsNullOrEmpty(sourceName) && (source.Name == NextSourceName))
        {
            sourceName = source.Name;
            level = NextLevel;
            keywords = NextKeywords;
            EnableEvents(source, level, (EventKeywords)keywords);
        }
    }
    protected override void OnEventWritten(EventWrittenEventArgs e)
    {
        events.AddOrUpdate(new(e.EventId, e.Level, (long)e.Keywords, e.EventName ?? ""), 1, (_, i) =>
        {
            return i + 1;
        });
    }
}
