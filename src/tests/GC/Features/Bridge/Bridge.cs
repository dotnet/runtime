// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

// False pinning cases are still possible. For example the thread can die
// and its stack reused by another thread. It also seems that a thread that
// does a GC can keep on the stack references to objects it encountered
// during the collection which are never released afterwards. This would
// be more likely to happen with the interpreter which reuses more stack.
public static class FinalizerHelpers
{
    private static IntPtr aptr;

    private static unsafe void NoPinActionHelper(int depth, Action act)
    {
        // Avoid tail calls
        int* values = stackalloc int[20];
        aptr = new IntPtr(values);

        if (depth <= 0)
        {
            //
            // When the action is called, this new thread might have not allocated
            // anything yet in the nursery. This means that the address of the first
            // object that would be allocated would be at the start of the tlab and
            // implicitly the end of the previous tlab (address which can be in use
            // when allocating on another thread, at checking if an object fits in
            // this other tlab). We allocate a new dummy object to avoid this type
            // of false pinning for most common cases.
            //
            new object();
            act();
            ClearStack();
        }
        else
        {
            NoPinActionHelper(depth - 1, act);
        }
    }

    private static unsafe void ClearStack()
    {
        int* values = stackalloc int[25000];
        for (int i = 0; i < 25000; i++)
            values[i] = 0;
    }

    public static void PerformNoPinAction(Action act)
    {
        Thread thr = new Thread(() => NoPinActionHelper (128, act));
        thr.Start();
        thr.Join();
    }
}

public class BridgeBase
{
    public static int fin_count;

    ~BridgeBase()
    {
        fin_count++;
    }
}

public class Bridge : BridgeBase
{
    public List<object> Links = new List<object>();
    public int __test;

    ~Bridge()
    {
        Links = null;
    }
}

public class Bridge1 : BridgeBase
{
    public object Link;
    ~Bridge1()
    {
        Link = null;
    }
}

// 128 size
public class Bridge14 : BridgeBase
{
    public object a,b,c,d,e,f,g,h,i,j,k,l,m,n;
}

public class NonBridge
{
    public object Link;
}

public class NonBridge2 : NonBridge
{
    public object Link2;
}

public class NonBridge14
{
    public object a,b,c,d,e,f,g,h,i,j,k,l,m,n;
}


public class BridgeTest
{
    const int OBJ_COUNT = 100 * 1000;
    const int LINK_COUNT = 2;
    const int EXTRAS_COUNT = 0;
    const double survival_rate = 0.1;

    // Pathological case for the original old algorithm.  Goes
    // away when merging is replaced by appending with flag
    // checking.
    static void SetupLinks()
    {
        var list = new List<Bridge>();
        for (int i = 0; i < OBJ_COUNT; ++i)
        {
            var bridge = new Bridge();
            list.Add(bridge);
        }

        var r = new Random(100);
        for (int i = 0; i < OBJ_COUNT; ++i)
        {
            var n = list[i];
            for (int j = 0; j < LINK_COUNT; ++j)
                n.Links.Add(list[r.Next (OBJ_COUNT)]);
            for (int j = 0; j < EXTRAS_COUNT; ++j)
                n.Links.Add(j);
            if (r.NextDouble() <= survival_rate)
                n.__test = 1;
        }
    }

    const int LIST_LENGTH = 10000;
    const int FAN_OUT = 10000;

    // Pathological case for the new algorithm.  Goes away with
    // the single-node elimination optimization, but will still
    // persist if modified by using a ladder instead of the single
    // list.
    static void SetupLinkedFan()
    {
        var head = new Bridge();
        var tail = new NonBridge();
        head.Links.Add(tail);
        for (int i = 0; i < LIST_LENGTH; ++i)
        {
            var obj = new NonBridge ();
            tail.Link = obj;
            tail = obj;
        }
        var list = new List<Bridge>();
        tail.Link = list;
        for (int i = 0; i < FAN_OUT; ++i)
            list.Add (new Bridge());
    }

    // Pathological case for the improved old algorithm.  Goes
    // away with copy-on-write DynArrays, but will still persist
    // if modified by using a ladder instead of the single list.
    static void SetupInverseFan()
    {
        var tail = new Bridge();
        object list = tail;
        for (int i = 0; i < LIST_LENGTH; ++i)
        {
            var obj = new NonBridge();
            obj.Link = list;
            list = obj;
        }
        var heads = new Bridge[FAN_OUT];
        for (int i = 0; i < FAN_OUT; ++i)
        {
            var obj = new Bridge();
            obj.Links.Add(list);
            heads[i] = obj;
        }
    }

    // Not necessarily a pathology, but a special case of where we
    // generate lots of "dead" SCCs.  A non-bridge object that
    // can't reach a bridge object can safely be removed from the
    // graph.  In this special case it's a linked list hanging off
    // a bridge object.  We can handle this by "forwarding" edges
    // going to non-bridge nodes that have only a single outgoing
    // edge.  That collapses the whole list into a single node.
    // We could remove that node, too, by removing non-bridge
    // nodes with no outgoing edges.
    static void SetupDeadList()
    {
        var head = new Bridge();
        var tail = new NonBridge();
        head.Links.Add(tail);
        for (int i = 0; i < LIST_LENGTH; ++i)
        {
            var obj = new NonBridge();
            tail.Link = obj;
            tail = obj;
        }
    }

    // Triggered a bug in the forwarding mechanic.
    static void SetupSelfLinks()
    {
        var head = new Bridge();
        var tail = new NonBridge();
        head.Links.Add(tail);
        tail.Link = tail;
    }

    const int L0_COUNT = 50000;
    const int L1_COUNT = 50000;
    const int EXTRA_LEVELS = 4;

    // Set a complex graph from one bridge to a couple.
    // The graph is designed to expose naive coloring on
    // tarjan and SCC explosion on classic.
    static void Spider()
    {
        Bridge a = new Bridge();
        Bridge b = new Bridge();

        var l1 = new List<object>();
        for (int i = 0; i < L0_COUNT; ++i) {
            var l0 = new List<object>();
            l0.Add(a);
            l0.Add(b);
            l1.Add(l0);
        }
        var last_level = l1;
        for (int l = 0; l < EXTRA_LEVELS; ++l) {
            int j = 0;
            var l2 = new List<object>();
            for (int i = 0; i < L1_COUNT; ++i) {
                var tmp = new List<object>();
                tmp.Add(last_level [j++ % last_level.Count]);
                tmp.Add(last_level [j++ % last_level.Count]);
                l2.Add(tmp);
            }
            last_level = l2;
        }
        Bridge c = new Bridge();
        c.Links.Add(last_level);
    }

    // Simulates a graph with two nested cycles that is produces by
    // the async state machine when `async Task M()` method gets its
    // continuation rooted by an Action held by RunnableImplementor
    // (ie. the task continuation is hooked through the SynchronizationContext
    // implentation and rooted only by Android bridge objects).
    static void NestedCycles()
    {
        Bridge runnableImplementor = new Bridge ();
        Bridge byteArrayOutputStream = new Bridge ();
        NonBridge2 action = new NonBridge2 ();
        NonBridge displayClass = new NonBridge ();
        NonBridge2 asyncStateMachineBox = new NonBridge2 ();
        NonBridge2 asyncStreamWriter = new NonBridge2 ();

        runnableImplementor.Links.Add(action);
        action.Link = displayClass;
        action.Link2 = asyncStateMachineBox;
        displayClass.Link = action;
        asyncStateMachineBox.Link = asyncStreamWriter;
        asyncStateMachineBox.Link2 = action;
        asyncStreamWriter.Link = byteArrayOutputStream;
        asyncStreamWriter.Link2 = asyncStateMachineBox;
    }

    static void RunGraphTest(Action test)
    {
        Console.WriteLine("Start test {0}", test.Method.Name);
        FinalizerHelpers.PerformNoPinAction(test);
        Console.WriteLine("-graph built-");
        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine("-GC {0}/5-", i);
            GC.Collect ();
            GC.WaitForPendingFinalizers();
        }

        Console.WriteLine("Finished test {0}, finalized {1}", test.Method.Name, Bridge.fin_count);
    }

    static void TestLinkedList()
    {
        int count = Environment.ProcessorCount + 2;
        var th = new Thread [count];
        for (int i = 0; i < count; ++i)
        {
            th [i] = new Thread( _ =>
            {
                var lst = new ArrayList();
                for (var j = 0; j < 500 * 1000; j++)
                {
                    lst.Add (new object());
                    if ((j % 999) == 0)
                        lst.Add (new BridgeBase());
                    if ((j % 1000) == 0)
                        new BridgeBase();
                    if ((j % 50000) == 0)
                        lst = new ArrayList();
                }
            });

            th [i].Start();
        }

        for (int i = 0; i < count; ++i)
            th [i].Join();

        GC.Collect(2);
        Console.WriteLine("Finished test LinkedTest, finalized {0}", BridgeBase.fin_count);
    }

    //we fill 16Mb worth of stuff, eg, 256k objects
    const int major_fill = 1024 * 256;

    //4mb nursery with 64 bytes objects -> alloc half
    const int nursery_obj_count = 16 * 1024;

    static void SetupFragmentation<TBridge, TNonBridge>()
            where TBridge : new()
            where TNonBridge : new()
    {
        const int loops = 4;
        for (int k = 0; k < loops; k++)
        {
            Console.WriteLine("[{0}] CrashLoop {1}/{2}", DateTime.Now, k + 1, loops);
            var arr = new object[major_fill];
            for (int i = 0; i < major_fill; i++)
                arr[i] = new TNonBridge();
            GC.Collect(1);
            Console.WriteLine("[{0}] major fill done", DateTime.Now);

            //induce massive fragmentation
            for (int i = 0; i < major_fill; i += 4)
            {
                arr[i + 1] = null;
                arr[i + 2] = null;
                arr[i + 3] = null;
            }
            GC.Collect (1);
            Console.WriteLine("[{0}] fragmentation done", DateTime.Now);

            //since 50% is garbage, do 2 fill passes
            for (int j = 0; j < 2; ++j)
            {
                for (int i = 0; i < major_fill; i++)
                {
                    if ((i % 1000) == 0)
                        new TBridge();
                    else
                        arr[i] = new TBridge();
                }
            }
            Console.WriteLine("[{0}] done spewing bridges", DateTime.Now);

            for (int i = 0; i < major_fill; i++)
                arr[i] = null;
            GC.Collect ();
        }
    }

    public static int Main(string[] args)
    {
//        TestLinkedList(); // Crashes, but only in this multithreaded variant
        RunGraphTest(SetupFragmentation<Bridge14, NonBridge14>); // This passes but the following crashes ??
//        RunGraphTest(SetupFragmentation<Bridge, NonBridge>);
        RunGraphTest(SetupLinks);
        RunGraphTest(SetupLinkedFan);
        RunGraphTest(SetupInverseFan);

        RunGraphTest(SetupDeadList);
        RunGraphTest(SetupSelfLinks);
        RunGraphTest(NestedCycles);
//        RunGraphTest(Spider); // Crashes
        return 100;
    }
}
