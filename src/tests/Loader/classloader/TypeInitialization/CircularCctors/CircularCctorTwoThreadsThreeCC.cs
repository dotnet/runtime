// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

using Xunit;

// Regression test for https://github.com/dotnet/runtime/issues/93778
namespace CircularCctorTwoThreadsThreeCC;

[Flags]
public enum SlotConstants : int
{
    ZInited = 1,
    YInitedFromX = 2,
    XInitedFromY = 4,

    Thread1 = 1 << 16,
    Thread2 = 2 << 16,
}

/// X and Y both try to use each other, and also both use Z.
/// We expect to see exactly one thread initialize Z and
/// either X inited from Y or Y inited from X.
public class X
{
    public static X Singleton = new();
    private X() {
        Z.Singleton.Ping();
        Y.Singleton?.Pong();
    }

    public void Pong() => Coordinator.Note(SlotConstants.XInitedFromY);
}

public class Y
{
    public static Y Singleton = new();
    private Y() {
            Z.Singleton.Ping();
            X.Singleton?.Pong();
        }

    public void Pong() => Coordinator.Note(SlotConstants.YInitedFromX);
}

public class Z
{
        public static Z Singleton = new();

    private Z() {
        Coordinator.Note(SlotConstants.ZInited);
    }

    public void Ping() { }
        
}

public class Coordinator
{
    [ThreadStatic]
    private static SlotConstants t_threadTag;

    private static int s_NextNote;
    private static readonly SlotConstants[] Notes = new SlotConstants[12];

    private static SlotConstants DecorateWithThread(SlotConstants c)
    {
        return c | t_threadTag;
    }

    public static void Note(SlotConstants s) {
        int idx = Interlocked.Increment(ref s_NextNote);
        idx--;
        Notes[idx] = DecorateWithThread (s);
    }

    public static Coordinator CreateThread(bool xThenY, SlotConstants threadTag)
    {
        return new Coordinator(xThenY, threadTag);
    }

    public readonly Thread Thread;
    private static readonly Barrier s_barrier = new (3);

    private Coordinator(bool xThenY, SlotConstants threadTag)
    {
        var t = new Thread(() => {
            t_threadTag = threadTag;
            // Log("started");
            NextPhase();
            // Log("racing");
            DoConstructions(xThenY);
            NextPhase();
            // Log("done");
        });
        Thread = t;
        t.Start();
    }

    public static void NextPhase() { s_barrier.SignalAndWait(); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void DoConstructions(bool xThenY)
    {
        if (xThenY) {
            XCreate();
        } else {
            YCreate();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void XCreate()
    {
        var _ = X.Singleton;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void YCreate()
    {
        var _ = Y.Singleton;
    }

    public static void Log(string msg)
    {
        Console.WriteLine ($"{Thread.CurrentThread.ManagedThreadId}: {msg}");
    }

    [Fact]
    public static void RunTestCase()
    {
        var c1 = CreateThread(xThenY: true, threadTag: SlotConstants.Thread1);
        var c2 = CreateThread(xThenY: false, threadTag: SlotConstants.Thread2);
        // created all threads
        NextPhase();
        // racing
        NextPhase();
        // done

        // one second should be plenty for all these threads, but it's arbitrary
        int threadJoinTimeoutMS = 1000;
        var j1 = c1.Thread.Join(threadJoinTimeoutMS);
        var j2 = c2.Thread.Join(threadJoinTimeoutMS);
        Assert.True(j1);
        Assert.True(j2);
        // all joined

        // exactly one thread inited Z
        Assert.Equal(1, CountNotes(SlotConstants.ZInited));
        // either X was inited or Y, not both.
        Assert.Equal(1, Count2Notes(SlotConstants.XInitedFromY, SlotConstants.YInitedFromX));
    }

    private static int CountNotes(SlotConstants mask)
    {
        int found = 0;
        foreach (var note in Notes) {
            if ((note & mask) != (SlotConstants)0) {
                found++;
            }
        }
        return found;
    }

    private static int Count2Notes(SlotConstants mask1, SlotConstants mask2)
    {
        int found = 0;
        foreach (var note in Notes) {
            if ((note & mask1) != (SlotConstants)0) {
                found++;
            }
            if ((note & mask2) != (SlotConstants)0) {
                found++;
            }
        }
        return found;
    }
    
}
