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
            NestedStructScenario();
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

    // ===== Scenario 5: Nested structs =====
    // Argument GC scanning for by-value structs has to walk the GCDesc
    // recursively when value-type fields contain (a) other value-type
    // fields that in turn carry GC refs, or (b) ref-fields buried inside
    // nested ByRefLike value types. The combinations below exercise the
    // ArgIterator + GCDesc / ByRefPointerOffsetsReporter paths:
    //   - Plain nested value type with no refs (encoder should emit
    //     nothing, runtime should emit nothing).
    //   - Nested value type with GC refs at non-zero offsets (GCDesc
    //     series must aggregate inner ref offsets relative to the outer
    //     argument start).
    //   - Three levels of nesting with refs at the deepest level.
    //   - Nested ByRefLike struct (Span<T> inside an outer ref struct):
    //     the encoder must walk the inner type's BYREF fields and emit
    //     INTERIOR at the correct offset within the outer struct.

    // Static sink so the JIT can't elide allocations / inline the
    // NoInlining methods below by proving the result is dead.
    static object? s_sink;

    struct InnerPlain
    {
        public int A;
    }

    struct OuterPlain
    {
        public InnerPlain Inner;
    }

    struct InnerWithRef
    {
        public int Pad;
        public object Ref;
    }

    struct OuterWithInnerRef
    {
        public int Header;
        public InnerWithRef Inner;
        public string Tail;
    }

    struct DeepLevel0
    {
        public object Ref;
    }

    struct DeepLevel1
    {
        public int Pad;
        public DeepLevel0 Inner;
    }

    struct DeepLevel2
    {
        public DeepLevel1 Inner;
        public int Trailer;
    }

    ref struct OuterRefStructWithSpan
    {
        public int Header;
        public Span<byte> Payload;
        public int Trailer;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ProcessNestedPlain(OuterPlain p)
    {
        // Burn allocations in a loop so the cdacstress allocation trigger
        // fires multiple times while this MD is live on the stack, and
        // route the results through a static sink so the JIT can't elide
        // them or inline this frame away.
        for (int i = 0; i < 16; i++)
        {
            s_sink = new object();
        }
        return p.Inner.A;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static object ProcessNestedRef(OuterWithInnerRef o)
    {
        for (int i = 0; i < 16; i++)
        {
            s_sink = new object();
        }
        GC.KeepAlive(o.Tail);
        return o.Inner.Ref;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static object ProcessDeeplyNested(DeepLevel2 d)
    {
        for (int i = 0; i < 16; i++)
        {
            s_sink = new object();
        }
        return d.Inner.Inner.Ref;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ProcessNestedSpan(OuterRefStructWithSpan o)
    {
        for (int i = 0; i < 16; i++)
        {
            s_sink = new object();
        }
        return o.Header + o.Payload.Length + o.Trailer;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void NestedStructScenario()
    {
        OuterPlain plain = new OuterPlain { Inner = new InnerPlain { A = 7 } };
        int v = ProcessNestedPlain(plain);
        GC.KeepAlive(v);

        OuterWithInnerRef withRef = new OuterWithInnerRef
        {
            Header = 1,
            Inner = new InnerWithRef { Pad = 2, Ref = new object() },
            Tail = "tail",
        };
        object inner = ProcessNestedRef(withRef);
        GC.KeepAlive(inner);
        GC.KeepAlive(withRef.Tail);

        DeepLevel2 deep = new DeepLevel2
        {
            Inner = new DeepLevel1
            {
                Pad = 3,
                Inner = new DeepLevel0 { Ref = new object() },
            },
            Trailer = 4,
        };
        object deepRef = ProcessDeeplyNested(deep);
        GC.KeepAlive(deepRef);

        byte[] buffer = new byte[16];
        OuterRefStructWithSpan refStruct = new OuterRefStructWithSpan
        {
            Header = 1,
            Payload = buffer,
            Trailer = 2,
        };
        int sum = ProcessNestedSpan(refStruct);
        GC.KeepAlive(sum);
        GC.KeepAlive(buffer);
    }
}
