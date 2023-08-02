using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tracing.Tests.Common;


{// Copied from https://github.com/dotnet/runtime/pull/86370 by @davmason:

    // Access ArrayPool.Shared.Rent() before the test to avoid the deadlock reported
    // in https://github.com/dotnet/runtime/issues/86233. This is a real issue,
    // but only seen if you have a short lived EventListener and create EventSources
    // in your OnEventWritten callback so we don't expect customers to hit it.
    byte[] localBuffer = ArrayPool<byte>.Shared.Rent(10);
    Console.WriteLine($"buffer length={localBuffer.Length}");
}
const string SourceName = "Microsoft-Windows-DotNETRuntime";

using Listener most = Listener.Create(SourceName, EventLevel.Verbose, EventKeywords.All);
using Listener all1 = Listener.Create(SourceName, EventLevel.LogAlways, EventKeywords.All);
using Listener all2 = Listener.Create(SourceName, EventLevel.LogAlways, EventKeywords.None);
Stopwatch stopwatch = Stopwatch.StartNew();
HashSet<Task> tasks = new();

while (stopwatch.Elapsed.TotalSeconds < (39d / 10d))
{
    tasks.RemoveWhere(t => t.IsCompleted);
    tasks.Add(Task.Run(() =>
    {
        Thread.Sleep(Random.Shared.Next().ToString().GetHashCode() & sbyte.MaxValue);
    }));

    string str = stopwatch.Elapsed.TotalNanoseconds.ToString();
    GC.KeepAlive(str);
    Thread.Sleep(str.GetHashCode() & sbyte.MaxValue);
}
GC.Collect();

foreach (Task task in tasks)
{
    task.Wait();
}
Thread.Sleep(1000);

bool LevelsPassed = most.IsSubsetOf(all1);
bool KeywordsPassed = all1.SetEquals(all2);

most.DumpEvents();
all1.DumpEvents();
all2.DumpEvents();
Console.WriteLine($"\n{nameof(stopwatch.Elapsed.TotalSeconds)}\t{stopwatch.Elapsed.TotalSeconds}");

Assert.True(nameof(LevelsPassed), LevelsPassed);
Assert.True(nameof(KeywordsPassed), KeywordsPassed);

return 100;



internal sealed class Listener : EventListener, IReadOnlySet<string>
{
    public int Count => Invoke(_ => events.Count, 0);
    public readonly string? SourceName = null;
    public readonly EventLevel? Level = nextLevel;
    public readonly EventKeywords? Keywords = nextKeywords;

    private static string nextSourceName = "";
    private static EventLevel nextLevel = default;
    private static EventKeywords nextKeywords = default;
    private readonly object eventsLock = new();
    private readonly HashSet<string> events = new();

    private Listener() => SourceName = nextSourceName;
    public static Listener Create(string sourceName, EventLevel level, EventKeywords keywords)
    {
        nextSourceName = sourceName;
        nextLevel = level;
        nextKeywords = keywords;
        return new();
    }

    public void DumpEvents()
    {
        Console.WriteLine($"\n{nameof(SourceName)}\t{SourceName}");
        Console.WriteLine($"{nameof(Level)}\t\t{Level}");
        Console.WriteLine($"{nameof(Keywords)}\t{Keywords}");
        Console.WriteLine($"{nameof(Count)}\t\t{Count}");

        foreach (string e in this)
        {
            Console.WriteLine(e);
        }
    }
    public bool Contains(string item) => Invoke(events.Contains, item);
    public bool IsProperSubsetOf(IEnumerable<string> other) => Invoke(events.IsProperSubsetOf, other);
    public bool IsProperSupersetOf(IEnumerable<string> other) => Invoke(events.IsProperSupersetOf, other);
    public bool IsSubsetOf(IEnumerable<string> other) => Invoke(events.IsSubsetOf, other);
    public bool IsSupersetOf(IEnumerable<string> other) => Invoke(events.IsSupersetOf, other);
    public bool Overlaps(IEnumerable<string> other) => Invoke(events.Overlaps, other);
    public bool SetEquals(IEnumerable<string> other) => Invoke(events.SetEquals, other);
    public IEnumerator<string> GetEnumerator() => Invoke(events.ToHashSet, EqualityComparer<string>.Default).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    protected override void OnEventSourceCreated(EventSource source)
    {
        if (source.Name == (SourceName ?? nextSourceName))
        {
            EnableEvents(source, Level ?? nextLevel, Keywords ?? nextKeywords);
        }
    }
    protected override void OnEventWritten(EventWrittenEventArgs e) => Invoke(events.Add, $"{e.EventId}\t\t{e.EventName}");

    private TResult Invoke<T, TResult>(Func<T, TResult> func, T t)
    {
        lock (eventsLock)
        {
            return func(t);
        }
    }
}
