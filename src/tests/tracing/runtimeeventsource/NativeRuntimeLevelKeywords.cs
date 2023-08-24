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
Console.WriteLine();

using GCListener informational = GCListener.StartNew(EventLevel.Informational);
using GCListener verbose = GCListener.StartNew(EventLevel.Verbose);
using GCListener logAlways = GCListener.StartNew(EventLevel.LogAlways);

Stopwatch stopwatch = Stopwatch.StartNew();
do
{
    int nanoseconds = (int)double.Min(stopwatch.Elapsed.TotalNanoseconds, 1000000d);

    for (int i = 0; i < nanoseconds; i++)
    {
        _ = new object();
    }
    GC.Collect();

    if (informational.Contains("GCStart_V2") && verbose.Contains("GCAllocationTick_V4"))
    {
        break;
    }
}
while (stopwatch.Elapsed.TotalSeconds <= 0.25d);

GCListener.DumpEvents(informational, verbose, logAlways);

Console.WriteLine($"\nElapsed Seconds: {stopwatch.Elapsed.TotalSeconds}\n");

AssertContains("GCStart_V2", EventLevel.Informational, informational, verbose, logAlways);
AssertContains("GCAllocationTick_V4", EventLevel.Verbose, informational, verbose, logAlways);

return 100;

static void AssertContains(string eventName, EventLevel level, params GCListener[] listeners)
{
    int eventLevel = (level is EventLevel.LogAlways) ? int.MinValue : (int)level;

    foreach (GCListener listener in listeners)
    {
        int listenerLevel = (listener.Level is EventLevel.LogAlways) ? int.MaxValue : (int)listener.Level;

        if ((eventLevel > listenerLevel) && listener.Contains(eventName))
        {
            throw new Exception($"{eventName} is in {listener}");
        }
        else if ((eventLevel <= listenerLevel) && !listener.Contains(eventName))
        {
            throw new Exception($"{eventName} is not in {listener}");
        }
    }
}

internal sealed class GCListener : EventListener
{
    public EventLevel Level { get; private set; }

    private static EventLevel nextLevel;
    private readonly ConcurrentDictionary<string, (int id, EventLevel level)> events = new();

    private GCListener()
    {
        Console.WriteLine($"{this} Listening...");
    }

    public static GCListener StartNew(EventLevel level)
    {
        nextLevel = level;
        return new();
    }

    public static void DumpEvents(params GCListener[] listeners)
    {
        foreach (GCListener listener in listeners)
        {
            Console.WriteLine($"\n{listener} Dump:\n\\");

            foreach (KeyValuePair<string, (int id, EventLevel level)> e in listener.events.OrderBy(e =>
            {
                return e.Value.id;
            }))
            {
                Console.WriteLine($"{e.Value.id,3}: {$"\"{e.Key}\"",-24}, EventLevel.{e.Value.level,-13}");
            }
        }
    }
    public bool Contains(string eventName)
    {
        return events.ContainsKey(eventName);
    }
    public override string ToString()
    {
        return $"{nameof(GCListener)}({Level,-13}, {events.Count,2})";
    }
    public override void Dispose()
    {
        Console.WriteLine($"{this} Disposing... ");
        base.Dispose();
    }

    protected override void OnEventSourceCreated(EventSource source)
    {
        if (source.Name is "Microsoft-Windows-DotNETRuntime")
        {
            Level = nextLevel;
            EnableEvents(source, Level, (EventKeywords)1);
        }
    }
    protected override void OnEventWritten(EventWrittenEventArgs e)
    {
        events.TryAdd(e.EventName ?? "", (e.EventId, e.Level));
    }
}
