// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// Exercises generic method instantiations and interface dispatch.
/// </summary>
internal static class Program
{
    interface IKeepAlive
    {
        object GetRef();
    }

    class BoxHolder : IKeepAlive
    {
        object _value;
        public BoxHolder() { _value = new object(); }
        public BoxHolder(object v) { _value = v; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public object GetRef() => _value;
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
    static void GenericScenario()
    {
        var o = GenericAlloc<object>();
        var l = GenericAlloc<List<int>>();
        var s = GenericAlloc<BoxHolder>();
        GC.KeepAlive(o);
        GC.KeepAlive(l);
        GC.KeepAlive(s);
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
        Func<object> fn = () =>
        {
            GC.KeepAlive(captured);
            return new object();
        };
        object result = fn();
        GC.KeepAlive(result);
        GC.KeepAlive(fn);
    }

    static int Main()
    {
        for (int i = 0; i < 2; i++)
        {
            GenericScenario();
            InterfaceDispatchScenario();
            DelegateScenario();
        }
        return 100;
    }
}
