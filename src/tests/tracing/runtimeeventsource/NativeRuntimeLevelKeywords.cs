using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;

using System.Buffers; // Copied from https://github.com/dotnet/runtime/pull/86370 by @davmason:
{
    // Access ArrayPool.Shared.Rent() before the test to avoid the deadlock reported
    // in https://github.com/dotnet/runtime/issues/86233. This is a real issue,
    // but only seen if you have a short lived EventListener and create EventSources
    // in your OnEventWritten callback so we don't expect customers to hit it.
    byte[] localBuffer = ArrayPool<byte>.Shared.Rent(10);
    Console.WriteLine($"buffer length={localBuffer.Length}");
}

using GCListener informational = new(EventLevel.Informational);
using GCListener verbose = new(EventLevel.Verbose);
using GCListener logAlways = new(EventLevel.LogAlways);

Stopwatch stopwatch = Stopwatch.StartNew();
do
{
    for (long i1 = 0, i2 = stopwatch.ElapsedMilliseconds; i1 < i2; i1++)
    {
        _ = new object();
    }
    GC.AddMemoryPressure(1L);
    GC.RemoveMemoryPressure(1L);
    GC.Collect();
}
while (stopwatch.Elapsed.TotalSeconds <= 0.25d);

GCListener.DumpEvents(informational, verbose, logAlways);
Console.WriteLine($"\n{nameof(stopwatch.Elapsed.TotalSeconds)} {stopwatch.Elapsed.TotalSeconds}");

AssertContains(("GCStart_V2", EventLevel.Informational), informational, verbose, logAlways);
AssertContains(("GCAllocationTick_V4", EventLevel.Verbose), informational, verbose, logAlways);

return 100;

static void AssertContains((string name, EventLevel level) e, params GCListener[] listeners)
{
    int eventLevel = (e.level is EventLevel.LogAlways) ? int.MinValue : (int)e.level;

    foreach (GCListener listener in listeners)
    {
        int listenerLevel = (listener.Level is EventLevel.LogAlways) ? int.MaxValue : (int)listener.Level;

        if ((eventLevel > listenerLevel) && listener.Contains(e.name))
        {
            throw new Exception($"{e} is in {listener}");
        }
        else if ((eventLevel <= listenerLevel) && !listener.Contains(e.name))
        {
            throw new Exception($"{e} is not in {listener}");
        }
    }
}

internal sealed class GCListener : EventListener
{
    public EventLevel Level { get; private set; }

    private const string sourceName = "Microsoft-Windows-DotNETRuntime";
    private static EventLevel nextLevel;
    private readonly ConcurrentDictionary<string, EventLevel> events = new();

    public GCListener(EventLevel level)
    {
        nextLevel = level;
    }

    public static void DumpEvents(params GCListener[] listeners)
    {
        foreach (GCListener listener in listeners)
        {
            Console.WriteLine($"\n{listener}\n\\");

            foreach (KeyValuePair<string, EventLevel> e in listener.events)
            {
                Console.WriteLine($"({$"\"{e.Key}\"",-24}, EventLevel.{e.Value,-13})");
            }
        }
    }
    public bool Contains(string eventName)
    {
        return events.ContainsKey(eventName);
    }
    public override string ToString()
    {
        return $"{nameof(GCListener)}({sourceName}, EventLevel.{Level}, {events.Count})";
    }
    public override void Dispose()
    {
        Console.WriteLine($"\n{this}\n\\\nDisposing...");
        base.Dispose();
        Console.WriteLine("Disposed");
    }

    protected override void OnEventSourceCreated(EventSource source)
    {
        if (source.Name is sourceName)
        {
            Level = nextLevel;
            EnableEvents(source, Level, (EventKeywords)1);
        }
    }
    protected override void OnEventWritten(EventWrittenEventArgs e)
    {
        events.TryAdd(e.EventName ?? "", e.Level);
    }
}
