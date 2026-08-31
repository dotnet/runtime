// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
/// All-in-one comprehensive debuggee that exercises every scenario
/// in a single run: allocations, exceptions, generics, P/Invoke, threading.
/// </summary>
internal static class Program
{
    interface IKeepAlive { object GetRef(); }
    class BoxHolder : IKeepAlive
    {
        object _value;
        public BoxHolder() { _value = new object(); }
        public BoxHolder(object v) { _value = v; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public object GetRef() => _value;
    }

    struct LargeStruct { public object A, B, C, D; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static object AllocAndHold()
    {
        object o = new object();
        string s = "hello world";
        int[] arr = new int[] { 1, 2, 3 };
        GC.KeepAlive(o);
        GC.KeepAlive(s);
        GC.KeepAlive(arr);
        return o;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void NestedCall(int depth)
    {
        object o = new object();
        if (depth > 0)
            NestedCall(depth - 1);
        GC.KeepAlive(o);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TryCatchScenario()
    {
        object before = new object();
        try
        {
            throw new InvalidOperationException("test");
        }
        catch (InvalidOperationException ex)
        {
            object inCatch = new object();
            GC.KeepAlive(ex);
            GC.KeepAlive(inCatch);
        }
        GC.KeepAlive(before);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TryFinallyScenario()
    {
        object outerRef = new object();
        try
        {
            object innerRef = new object();
            GC.KeepAlive(innerRef);
        }
        finally
        {
            object finallyRef = new object();
            GC.KeepAlive(finallyRef);
        }
        GC.KeepAlive(outerRef);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void NestedExceptionScenario()
    {
        object a = new object();
        try
        {
            try
            {
                throw new ArgumentException("inner");
            }
            catch (ArgumentException ex1)
            {
                GC.KeepAlive(ex1);
                throw new InvalidOperationException("outer", ex1);
            }
        }
        catch (InvalidOperationException ex2)
        {
            GC.KeepAlive(ex2);
        }
        GC.KeepAlive(a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void FilterExceptionScenario()
    {
        object holder = new object();
        try
        {
            throw new ArgumentException("filter-test");
        }
        catch (ArgumentException ex) when (FilterCheck(ex))
        {
            GC.KeepAlive(ex);
        }
        GC.KeepAlive(holder);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool FilterCheck(Exception ex)
    {
        object filterLocal = new object();
        GC.KeepAlive(filterLocal);
        return ex.Message.Contains("filter");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static T GenericAlloc<T>() where T : new()
    {
        T val = new T();
        object marker = new object();
        GC.KeepAlive(marker);
        return val;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void InterfaceDispatchScenario()
    {
        IKeepAlive holder = new BoxHolder(new int[] { 42, 43 });
        object r = holder.GetRef();
        GC.KeepAlive(holder);
        GC.KeepAlive(r);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DelegateScenario()
    {
        object captured = new object();
        Func<object> fn = () => { GC.KeepAlive(captured); return new object(); };
        object result = fn();
        GC.KeepAlive(result);
        GC.KeepAlive(fn);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void StructWithRefsScenario()
    {
        LargeStruct ls;
        ls.A = new object();
        ls.B = "struct-string";
        ls.C = new int[] { 10, 20 };
        ls.D = new BoxHolder(ls.A);
        GC.KeepAlive(ls.A);
        GC.KeepAlive(ls.B);
        GC.KeepAlive(ls.C);
        GC.KeepAlive(ls.D);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PinnedScenario()
    {
        byte[] buffer = new byte[64];
        GCHandle pin = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            object other = new object();
            GC.KeepAlive(other);
            GC.KeepAlive(buffer);
        }
        finally
        {
            pin.Free();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void MultiThreadScenario()
    {
        ManualResetEventSlim ready = new ManualResetEventSlim(false);
        ManualResetEventSlim go = new ManualResetEventSlim(false);
        Thread t = new Thread(() =>
        {
            object threadLocal = new object();
            ready.Set();
            go.Wait();
            NestedCall(5);
            GC.KeepAlive(threadLocal);
        });
        t.Start();
        ready.Wait();
        go.Set();
        NestedCall(3);
        t.Join();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RethrowScenario()
    {
        object outerRef = new object();
        try
        {
            try
            {
                throw new ApplicationException("rethrow-test");
            }
            catch (ApplicationException)
            {
                object catchRef = new object();
                GC.KeepAlive(catchRef);
                throw;
            }
        }
        catch (ApplicationException ex)
        {
            GC.KeepAlive(ex);
        }
        GC.KeepAlive(outerRef);
    }

    static int Main()
    {
        for (int i = 0; i < 2; i++)
        {
            AllocAndHold();
            NestedCall(5);
            TryCatchScenario();
            TryFinallyScenario();
            NestedExceptionScenario();
            FilterExceptionScenario();
            GenericAlloc<object>();
            GenericAlloc<List<int>>();
            InterfaceDispatchScenario();
            DelegateScenario();
            StructWithRefsScenario();
            PinnedScenario();
            MultiThreadScenario();
            RethrowScenario();
        }
        return 100;
    }
}
