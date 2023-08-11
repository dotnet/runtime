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

using GCListener informational = GCListener.Create(EventLevel.Informational);
using GCListener verbose = GCListener.Create(EventLevel.Verbose);
using GCListener logAlways = GCListener.Create(EventLevel.LogAlways);

Stopwatch stopwatch = Stopwatch.StartNew();

while (stopwatch.Elapsed.TotalSeconds < 3d)
{
    for (long i1 = 0, i2 = stopwatch.ElapsedMilliseconds; i1 < i2; i1++)
    {
        _ = new object();
    }
    GC.AddMemoryPressure(1L);
    GC.RemoveMemoryPressure(1L);

    if ((informational.Events.Count > 0) &&
        verbose.Events.Any(e => e.Level is EventLevel.Verbose))
    {
        break;
    }
}
informational.DumpEvents();
verbose.DumpEvents();
logAlways.DumpEvents();

Console.WriteLine($"\n{nameof(stopwatch.Elapsed.TotalSeconds)} {stopwatch.Elapsed.TotalSeconds}");

WarningIfNoEventsAtLevel(informational);
WarningIfNoEventsAtLevel(verbose);

ThrowIfEventsAtLevel(informational, EventLevel.Verbose);
ThrowIfNotSubsetOf(informational, verbose);
ThrowIfNotSubsetOf(verbose, logAlways);

return 100;

static void WarningIfNoEventsAtLevel(GCListener listener)
{
    if (!listener.Events.Any(e => e.Level == listener.Level))
    {
        Console.WriteLine($"\nWARNING! No {listener.Level} events in {listener}");
    }
}
static void ThrowIfEventsAtLevel(GCListener listener, EventLevel level)
{
    if (listener.Events.Any(e => e.Level == level))
    {
        throw new Exception($"{listener} contains {level} events");
    }
}
static void ThrowIfNotSubsetOf(GCListener listener, GCListener other)
{
    if (!listener.Events.IsSubsetOf(other.Events))
    {
        throw new Exception($"{listener} contains events not found in {other}");
    }
}

internal sealed class GCListener : EventListener
{
    public readonly EventLevel Level;
    public IReadOnlySet<GCEvent> Events
    {
        get
        {
            if (events.Count > keys.Count)
            {
                keys.UnionWith(events.Keys);
            }
            return keys;
        }
    }

    private const string sourceName = "Microsoft-Windows-DotNETRuntime";
    private static EventLevel nextLevel;
    private readonly ConcurrentDictionary<GCEvent, int> events = new();
    private readonly HashSet<GCEvent> keys = new();

    public readonly record struct GCEvent(EventLevel Level, long Keywords, string Name)
    {
        public override string ToString()
        {
            return $"{GetType().Name}({Level.GetType().Name}.{Level,-13}, 0x{Keywords:x16}L, \"{Name}\");";
        }
    }

    private GCListener()
    {
        Level = nextLevel;
    }

    public static GCListener Create(EventLevel level)
    {
        nextLevel = level;
        return new();
    }
    public void DumpEvents()
    {
        Console.WriteLine($"\n{this}\n\\");
        foreach (GCEvent e in Events)
        {
            Console.WriteLine(e);
        }
    }
    public override string ToString()
    {
        return $"{GetType().Name}({sourceName}, {Level.GetType().Name}.{Level,-13}, {Events.Count,3});";
    }
    public override void Dispose()
    {
        Console.WriteLine($"\n{this}\n\\\nDisposing...");
        base.Dispose();
        Console.WriteLine("Disposed");
    }

    protected override void OnEventSourceCreated(EventSource source)
    {
        if (source.Name == sourceName)
        {
            EnableEvents(source, nextLevel, (EventKeywords)1);
        }
    }
    protected override void OnEventWritten(EventWrittenEventArgs e)
    {
        events.TryAdd(new(e.Level, (long)e.Keywords, e.EventName ?? ""), e.EventId);
    }
}
