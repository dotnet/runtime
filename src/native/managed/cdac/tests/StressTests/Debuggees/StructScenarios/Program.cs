// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Exercises struct-related GC scanning scenarios that stress the MetaSig path:
/// - Value type 'this' (interior pointer for struct instance methods)
/// - Small struct returns (retbuf detection precision)
/// - Struct parameters containing embedded GC references
/// </summary>
internal static class Program
{
    static int Main()
    {
        for (int i = 0; i < 100; i++)
        {
            ValueTypeThisScenario();
            SmallStructReturnScenario();
            StructWithRefsScenario();
            InterfaceDispatchScenario();
        }
        return 100;
    }

    // ===== Scenario 1: Value type 'this' =====
    // When a struct instance method is called through interface dispatch,
    // 'this' is an interior pointer (pointing into the boxed struct, past
    // the MethodTable pointer). The GC needs GC_CALL_INTERIOR to handle it.

    interface IKeepAlive
    {
        object GetRef();
    }

    struct StructWithRef : IKeepAlive
    {
        public object Field;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public object GetRef() => Field;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ValueTypeThisScenario()
    {
        IKeepAlive s = new StructWithRef { Field = new object() };
        object r = s.GetRef();
        GC.KeepAlive(r);
        GC.KeepAlive(s);
    }

    // ===== Scenario 2: Small struct returns =====
    // Methods returning small structs (1/2/4/8 bytes, power-of-2) do NOT need
    // a return buffer on AMD64 Windows — the value is returned in RAX.
    // Conservative HasRetBuffArg=true shifts all parameter offsets by 1 slot.

    struct SmallResult
    {
        public int Value;
    }

    struct TinyResult
    {
        public byte Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static SmallResult MakeSmallResult(object keepAlive)
    {
        GC.KeepAlive(keepAlive);
        return new SmallResult { Value = 42 };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static TinyResult MakeTinyResult(object keepAlive)
    {
        GC.KeepAlive(keepAlive);
        return new TinyResult { Value = 1 };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SmallStructReturnScenario()
    {
        object live = new object();
        SmallResult sr = MakeSmallResult(live);
        TinyResult tr = MakeTinyResult(live);
        GC.KeepAlive(sr);
        GC.KeepAlive(tr);
        GC.KeepAlive(live);
    }

    // ===== Scenario 3: Struct parameters with embedded GC refs =====
    // Value type parameters containing object references require GCDesc
    // scanning to find the embedded refs. Without this, the refs inside
    // the struct are invisible to the GC.

    struct Holder
    {
        public object Ref1;
        public string Ref2;
        public int[] Array;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ProcessHolder(Holder h)
    {
        GC.KeepAlive(h.Ref1);
        GC.KeepAlive(h.Ref2);
        GC.KeepAlive(h.Array);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void StructWithRefsScenario()
    {
        Holder h = new Holder
        {
            Ref1 = new object(),
            Ref2 = "hello",
            Array = new int[] { 1, 2, 3 },
        };
        ProcessHolder(h);
        GC.KeepAlive(h.Ref1);
    }

    // ===== Scenario 4: Interface dispatch with generics =====
    // Shared generic methods going through stub dispatch combine
    // RequiresInstArg with value type 'this'.

    interface IGenericOp<T>
    {
        T Get();
    }

    struct GenericStruct<T> : IGenericOp<T>
    {
        public T Value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public T Get() => Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void InterfaceDispatchScenario()
    {
        IGenericOp<object> g = new GenericStruct<object> { Value = new object() };
        object r = g.Get();
        GC.KeepAlive(r);
        GC.KeepAlive(g);

        IGenericOp<string> gs = new GenericStruct<string> { Value = "test" };
        string s = gs.Get();
        GC.KeepAlive(s);
        GC.KeepAlive(gs);
    }
}
