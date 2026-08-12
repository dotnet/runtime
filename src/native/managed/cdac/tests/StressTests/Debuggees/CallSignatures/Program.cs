// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

/// <summary>
/// Exhaustive cdacstress ArgIterator debuggee. Covers a wide variety of
/// argument shapes that exercise different paths through the runtime
/// ComputeCallRefMap encoder and the cDAC CallingConventionGCRefMapBuilder:
///   - Register vs stack-passed parameters; long signatures that spill
///   - ByRef / in / out parameters (managed pointers -> INTERIOR)
///   - Native pointer / function pointer parameters (no token)
///   - Single- and multi-dimensional arrays (REF)
///   - Empty / tiny / pointer-sized / multi-field structs by value
///   - Structs containing object refs at the start, middle, end
///   - Nested structs (refs at deep offsets); deep nesting
///   - Value-type 'this' (interior pointer); ByRef return value
///   - ByRefLike value types: Span / ReadOnlySpan
///   - ByRefLike with only PTR fields (no INTERIOR expected)
///   - ByRefLike with multiple BYREF fields
///   - Nested ByRefLike (ref struct containing Span)
///   - Generic methods: T as ref / value type; multiple type params
///   - Generic value-type instance methods (interface dispatch)
///   - Enum arguments (Int32-, Int64-, byte-backed)
///   - Large-struct return (HasRetBuffArg)
///   - HFA / HVA: float, double, Vector64<T>, Vector128<T>, System.Numerics.Vector<T>
///     plus negative shapes (too many fields, mixed FP types)
///   - Mutually-recursive deep stack
///
/// __arglist / vararg coverage lives in the dedicated VarArgs debuggee
/// because the native varargs calling convention is only supported on
/// Windows x86/x64/ARM64 (see src/coreclr/jit/target.h::compFeatureVarArg).
/// Building this debuggee with vararg methods would fail to JIT on
/// Linux/macOS, Windows ARM32, RISC-V, LoongArch64, and WASM.
/// Every test method begins with AllocBurst() so the cdacstress allocation
/// trigger fires while the frame is on the stack and per-MD dedup actually
/// produces an ARG_PASS / ARG_FAIL log line for it.
/// </summary>
internal static unsafe class Program
{
    // Static sink to keep allocations from being elided by the JIT.
    private static object? s_sink;

    // Each test method calls this at entry. AllocBurst itself is also
    // NoInlining so it shows up as a distinct frame, but the important
    // thing is that the CALLER (the test method we want verified) is
    // still on the stack at the moment of allocation.
    //
    // 32 allocations is intentional: the cdacstress allocation trigger
    // serializes verifications on an internal lock, and other threads /
    // helper allocations may swallow the trigger for a given alloc call.
    // A bigger burst maximizes the chance that at least one fires while
    // the caller's frame is live, which is what per-MD dedup needs to
    // record an [ARG_PASS] / [ARG_FAIL] line for the caller.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AllocBurst()
    {
        for (int i = 0; i < 32; i++)
        {
            s_sink = new object();
        }
    }

    private static int Main()
    {
        for (int iter = 0; iter < 50; iter++)
        {
            Drive();
        }
        // Suppress unused warning for stack-allocated buffers in test methods.
        GC.KeepAlive(s_sink);
        return 100;
    }

    // ---- Driver: invokes every test method. NoInlining so it stays its own
    //      frame; the test methods themselves are NoInlining (see attribute on
    //      each). Wrapped categories live in helpers below.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Drive()
    {
        ArgCountCategory();
        ByRefCategory();
        PointerCategory();
        ArrayCategory();
        StructByValueCategory();
        NestedStructCategory();
        ValueTypeThisCategory();
        ByRefLikeCategory();
        GenericCategory();
        EnumCategory();
        ReturnCategory();
        HfaCategory();
        SysVCategory();
        DeepStackCategory();
    }

    // ===== Category 1: argument count / register vs stack =====

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ArgCountCategory()
    {
        OneRef("a");
        TwoRefs("a", "b");
        ThreeRefs("a", "b", "c");
        FourRefs("a", "b", "c", "d");
        EightRefs("a", "b", "c", "d", "e", "f", "g", "h");
        ManyPrimitives(1, 2, 3, 4, 5, 6, 7, 8);
        ManyLongs(1, 2, 3, 4);
        MixedSizes(1, 2L, "a", 3, "b", 4L);
        MixedRefAndPrimitive("x", 1, "y", 2, "z", 3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)] private static void OneRef(string a) { AllocBurst(); GC.KeepAlive(a); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void TwoRefs(string a, string b) { AllocBurst(); GC.KeepAlive(a); GC.KeepAlive(b); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void ThreeRefs(string a, string b, string c) { AllocBurst(); GC.KeepAlive(a); GC.KeepAlive(b); GC.KeepAlive(c); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void FourRefs(string a, string b, string c, string d) { AllocBurst(); GC.KeepAlive(a); GC.KeepAlive(b); GC.KeepAlive(c); GC.KeepAlive(d); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void EightRefs(string a, string b, string c, string d, string e, string f, string g, string h)
    {
        AllocBurst();
        GC.KeepAlive(a); GC.KeepAlive(b); GC.KeepAlive(c); GC.KeepAlive(d);
        GC.KeepAlive(e); GC.KeepAlive(f); GC.KeepAlive(g); GC.KeepAlive(h);
    }

    [MethodImpl(MethodImplOptions.NoInlining)] private static int ManyPrimitives(int a, int b, int c, int d, int e, int f, int g, int h) { AllocBurst(); return a + b + c + d + e + f + g + h; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static long ManyLongs(long a, long b, long c, long d) { AllocBurst(); return a + b + c + d; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void MixedSizes(int a, long b, string c, int d, string e, long f) { AllocBurst(); GC.KeepAlive(c); GC.KeepAlive(e); GC.KeepAlive((object)(a + b + d + f)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void MixedRefAndPrimitive(string a, int b, string c, int d, string e, int f) { AllocBurst(); GC.KeepAlive(a); GC.KeepAlive(c); GC.KeepAlive(e); GC.KeepAlive((object)(b + d + f)); }

    // ===== Category 2: by-ref / in / out =====

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ByRefCategory()
    {
        int x = 1;
        ByRefInt(ref x);
        InInt(in x);
        OutInt(out _);

        string s = "a";
        ByRefRef(ref s);

        ByRefMixed(1, ref x, "lit", ref s);

        Holder h = default;
        ByRefStruct(ref h);
    }

    [MethodImpl(MethodImplOptions.NoInlining)] private static void ByRefInt(ref int x) { AllocBurst(); x++; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void InInt(in int x) { AllocBurst(); GC.KeepAlive((object)x); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void OutInt(out int x) { AllocBurst(); x = 1; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void ByRefRef(ref string s) { AllocBurst(); GC.KeepAlive(s); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void ByRefMixed(int a, ref int b, string c, ref string d) { AllocBurst(); b += a; GC.KeepAlive(c); GC.KeepAlive(d); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void ByRefStruct(ref Holder h) { AllocBurst(); GC.KeepAlive(h.Ref); }

    private struct Holder
    {
        public int Pad;
        public object? Ref;
    }

    // ===== Category 3: pointers (native, function) =====

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PointerCategory()
    {
        int v = 1;
        PtrInt(&v);
        Ptr2Int(&v, &v);
        PtrMix("a", &v, "b");
        VoidPtr((void*)1);
        FnPtrArg(&HelperForFnPtr);
    }

    [MethodImpl(MethodImplOptions.NoInlining)] private static void PtrInt(int* p) { AllocBurst(); GC.KeepAlive((object)(*p)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Ptr2Int(int* a, int* b) { AllocBurst(); GC.KeepAlive((object)(*a + *b)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void PtrMix(string a, int* b, string c) { AllocBurst(); GC.KeepAlive(a); GC.KeepAlive((object)(*b)); GC.KeepAlive(c); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void VoidPtr(void* p) { AllocBurst(); GC.KeepAlive((object)(nint)p); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void FnPtrArg(delegate*<int, int> f) { AllocBurst(); GC.KeepAlive((object)f(1)); }
    private static int HelperForFnPtr(int x) => x + 1;

    // ===== Category 4: arrays =====

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ArrayCategory()
    {
        ArrayOne(new int[3]);
        Array2D(new int[3, 3]);
        ArrayJagged(new int[3][]);
        ArrayObj(new object[3]);
        ArrayMix(new int[3], "a", new object[3]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)] private static void ArrayOne(int[] a) { AllocBurst(); GC.KeepAlive(a); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Array2D(int[,] a) { AllocBurst(); GC.KeepAlive(a); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void ArrayJagged(int[][] a) { AllocBurst(); GC.KeepAlive(a); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void ArrayObj(object[] a) { AllocBurst(); GC.KeepAlive(a); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void ArrayMix(int[] a, string b, object[] c) { AllocBurst(); GC.KeepAlive(a); GC.KeepAlive(b); GC.KeepAlive(c); }

    // ===== Category 5: structs by value =====

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StructByValueCategory()
    {
        Empty();
        Tiny(new TinyStruct { B = 1 });
        IntSized(new IntStruct { I = 1 });
        TwoInts(new TwoIntStruct { A = 1, B = 2 });
        DoubleSized(new DoubleStruct { D = 1.0 });
        Big(new BigStruct { A = 1, B = 2, C = 3, D = 4 });
        RefAtStart(new RefAtStartStruct { R = "a", Trailer = 1 });
        RefAtEnd(new RefAtEndStruct { Header = 1, R = "a" });
        RefInMiddle(new RefInMiddleStruct { Header = 1, R = "a", Trailer = 2 });
        TwoRefStructArg(new TwoRefStruct { A = "a", B = "b" });
        AlternatingRefs(new AlternatingRefsStruct { I1 = 1, R1 = "a", I2 = 2, R2 = "b" });
        RefAndArray(new RefAndArrayStruct { R = "a", Arr = new int[3] });
    }

    [MethodImpl(MethodImplOptions.NoInlining)] private static void Empty() { AllocBurst(); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Tiny(TinyStruct s) { AllocBurst(); GC.KeepAlive((object)s.B); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void IntSized(IntStruct s) { AllocBurst(); GC.KeepAlive((object)s.I); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void TwoInts(TwoIntStruct s) { AllocBurst(); GC.KeepAlive((object)(s.A + s.B)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void DoubleSized(DoubleStruct s) { AllocBurst(); GC.KeepAlive((object)s.D); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Big(BigStruct s) { AllocBurst(); GC.KeepAlive((object)(s.A + s.B + s.C + s.D)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void RefAtStart(RefAtStartStruct s) { AllocBurst(); GC.KeepAlive(s.R); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void RefAtEnd(RefAtEndStruct s) { AllocBurst(); GC.KeepAlive(s.R); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void RefInMiddle(RefInMiddleStruct s) { AllocBurst(); GC.KeepAlive(s.R); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void TwoRefStructArg(TwoRefStruct s) { AllocBurst(); GC.KeepAlive(s.A); GC.KeepAlive(s.B); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void AlternatingRefs(AlternatingRefsStruct s) { AllocBurst(); GC.KeepAlive(s.R1); GC.KeepAlive(s.R2); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void RefAndArray(RefAndArrayStruct s) { AllocBurst(); GC.KeepAlive(s.R); GC.KeepAlive(s.Arr); }

    private struct TinyStruct { public byte B; }
    private struct IntStruct { public int I; }
    private struct TwoIntStruct { public int A; public int B; }
    private struct DoubleStruct { public double D; }
    private struct BigStruct { public long A, B, C, D; }
    private struct RefAtStartStruct { public object R; public int Trailer; }
    private struct RefAtEndStruct { public int Header; public object R; }
    private struct RefInMiddleStruct { public int Header; public object R; public int Trailer; }
    private struct TwoRefStruct { public object A; public object B; }
    private struct AlternatingRefsStruct { public int I1; public object R1; public int I2; public object R2; }
    private struct RefAndArrayStruct { public object R; public int[] Arr; }

    // ===== Category 6: nested structs =====

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void NestedStructCategory()
    {
        NestedPlain(new OuterPlain { I = new InnerPlain { A = 1 } });
        NestedRef(new OuterWithRef { H = 1, I = new InnerWithRef { Pad = 2, R = "a" }, T = "b" });
        DoublyNested(new Doubly { L0 = new L0 { L1 = new L1 { L2 = new L2 { R = "deep" } } } });
        NestedTwoLevelMixed(new MixedOuter
        {
            Pre = 1,
            Mid = new MixedInner { A = "a", I = 2, B = "b" },
            Post = 3,
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)] private static void NestedPlain(OuterPlain o) { AllocBurst(); GC.KeepAlive((object)o.I.A); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void NestedRef(OuterWithRef o) { AllocBurst(); GC.KeepAlive(o.I.R); GC.KeepAlive(o.T); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void DoublyNested(Doubly d) { AllocBurst(); GC.KeepAlive(d.L0.L1.L2.R); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void NestedTwoLevelMixed(MixedOuter m) { AllocBurst(); GC.KeepAlive(m.Mid.A); GC.KeepAlive(m.Mid.B); }

    private struct InnerPlain { public int A; }
    private struct OuterPlain { public InnerPlain I; }
    private struct InnerWithRef { public int Pad; public object R; }
    private struct OuterWithRef { public int H; public InnerWithRef I; public string T; }
    private struct L2 { public object R; }
    private struct L1 { public L2 L2; }
    private struct L0 { public L1 L1; }
    private struct Doubly { public L0 L0; }
    private struct MixedInner { public string A; public int I; public string B; }
    private struct MixedOuter { public int Pre; public MixedInner Mid; public int Post; }

    // ===== Category 7: value-type 'this' (interior) =====

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ValueTypeThisCategory()
    {
        // Instance method on a struct receives 'this' as a managed
        // pointer interior to the struct -> INTERIOR.
        var ws = new WithRefStructInstance { R = "a" };
        ws.Instance();

        var gs = new GenericStructInstance<string> { V = "a" };
        gs.Instance();

        IDispatch d = new DispatchStruct { R = "b" };
        d.Method();
    }

    private struct WithRefStructInstance
    {
        public object R;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Instance() { AllocBurst(); GC.KeepAlive(R); }
    }

    private struct GenericStructInstance<T>
    {
        public T V;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public T Instance() { AllocBurst(); return V; }
    }

    private interface IDispatch
    {
        void Method();
    }

    private struct DispatchStruct : IDispatch
    {
        public object R;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Method() { AllocBurst(); GC.KeepAlive(R); }
    }

    // ===== Category 8: ByRefLike (ref structs) =====

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ByRefLikeCategory()
    {
        Span<byte> sp = stackalloc byte[16];
        ProcessSpan(sp);
        ProcessReadOnlySpan(sp);

        // Two Span args next to each other.
        Span<byte> sp2 = stackalloc byte[8];
        ProcessTwoSpans(sp, sp2);

        // Span + reference + Span mix.
        ProcessSpanMix(sp, "x", sp2);

        // ByRefLike whose only field is a void* (no INTERIOR expected).
        var ptrOnly = new PtrOnlyRefStruct { P = (void*)1 };
        ProcessPtrOnlyRefStruct(ptrOnly);

        // ByRefLike with two ref fields.
        int a1 = 1, a2 = 2;
        var multi = new TwoByRefStruct(ref a1, ref a2);
        ProcessTwoByRefStruct(multi);

        // Ref struct containing a Span (nested ByRefLike).
        Span<byte> nested = stackalloc byte[16];
        var nestedRef = new OuterRefWithSpan { Header = 1, Payload = nested, Trailer = 2 };
        ProcessOuterRefWithSpan(nestedRef);
    }

    [MethodImpl(MethodImplOptions.NoInlining)] private static int ProcessSpan(Span<byte> s) { AllocBurst(); return s.Length; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ProcessReadOnlySpan(ReadOnlySpan<byte> s) { AllocBurst(); return s.Length; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ProcessTwoSpans(Span<byte> a, Span<byte> b) { AllocBurst(); return a.Length + b.Length; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ProcessSpanMix(Span<byte> a, string b, Span<byte> c) { AllocBurst(); GC.KeepAlive(b); return a.Length + c.Length; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static nint ProcessPtrOnlyRefStruct(PtrOnlyRefStruct s) { AllocBurst(); return (nint)s.P; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ProcessTwoByRefStruct(TwoByRefStruct s) { AllocBurst(); return s.A + s.B; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ProcessOuterRefWithSpan(OuterRefWithSpan o) { AllocBurst(); return o.Header + o.Payload.Length + o.Trailer; }

    private unsafe ref struct PtrOnlyRefStruct { public void* P; }

    private ref struct TwoByRefStruct
    {
        public ref int A;
        public ref int B;
        public TwoByRefStruct(ref int a, ref int b) { A = ref a; B = ref b; }
    }

    private ref struct OuterRefWithSpan
    {
        public int Header;
        public Span<byte> Payload;
        public int Trailer;
    }

    // ===== Category 9: generics =====

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void GenericCategory()
    {
        GenericRef<string>("a");
        GenericRef<object>(new object());
        GenericVT<int>(42);
        GenericVT<double>(3.14);
        GenericMulti<string, object>("a", new object());
        GenericConstrained(new MemoryStream());

        // Generic instance method on a generic value type (shared
        // canonical impl pulls in the param type via HasParamType).
        var c1 = new Container<string> { V = "v" };
        c1.Get();
        var c2 = new Container<object> { V = new object() };
        c2.Get();
    }

    [MethodImpl(MethodImplOptions.NoInlining)] private static T GenericRef<T>(T v) where T : class { AllocBurst(); return v; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static T GenericVT<T>(T v) where T : struct { AllocBurst(); return v; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void GenericMulti<TA, TB>(TA a, TB b) { AllocBurst(); GC.KeepAlive(a); GC.KeepAlive(b); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void GenericConstrained<T>(T v) where T : class, IDisposable { AllocBurst(); GC.KeepAlive(v); }

    private struct Container<T>
    {
        public T V;
        [MethodImpl(MethodImplOptions.NoInlining)] public T Get() { AllocBurst(); return V; }
    }

    // ===== Category 10: enums =====

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void EnumCategory()
    {
        EnumInt(SomeEnum.A);
        EnumLong(SomeLongEnum.A);
        EnumByte(SomeByteEnum.A);
        EnumInStruct(new EnumWrapper { E = SomeEnum.A, R = "a" });
    }

    [MethodImpl(MethodImplOptions.NoInlining)] private static SomeEnum EnumInt(SomeEnum e) { AllocBurst(); return e; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static SomeLongEnum EnumLong(SomeLongEnum e) { AllocBurst(); return e; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static SomeByteEnum EnumByte(SomeByteEnum e) { AllocBurst(); return e; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void EnumInStruct(EnumWrapper w) { AllocBurst(); GC.KeepAlive(w.R); }

    private enum SomeEnum { A, B, C }
    private enum SomeLongEnum : long { A, B, C }
    private enum SomeByteEnum : byte { A, B, C }
    private struct EnumWrapper { public SomeEnum E; public object R; }

    // ===== Category 11: returns =====

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ReturnCategory()
    {
        _ = ReturnRef();
        _ = ReturnLarge();
        Span<byte> sp = stackalloc byte[16];
        _ = ReturnSpan(sp);
    }

    [MethodImpl(MethodImplOptions.NoInlining)] private static string ReturnRef() { AllocBurst(); return "x"; }
    // Large struct on Windows AMD64 (> 8 bytes, not power-of-2) -> HasRetBuffArg shifts arg offsets.
    [MethodImpl(MethodImplOptions.NoInlining)] private static BigStruct ReturnLarge() { AllocBurst(); return new BigStruct { A = 1 }; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static Span<byte> ReturnSpan(Span<byte> s) { AllocBurst(); return s; }

    // ===== Category 12: HFA / HVA =====
    // HFAs (Homogeneous Floating-point Aggregates) and HVAs (Homogeneous Vector
    // Aggregates) are classified at MethodTable layout time on FEATURE_HFA
    // targets (ARM / ARM64). The ArgIterator code path for HFA/HVA args is
    // distinct from regular structs, so these debuggee methods exist primarily
    // to exercise the cDAC's TryGetHFAElementSize walker on ARM/ARM64. On
    // Windows x86/x64 the runtime does not classify these types as HFAs, so
    // they go through the regular struct path -- the cDAC matches that, and
    // the ARGITER stress assertion still passes.
    //
    // Negative shapes (Float5, MixedR4R8) are included so the walker correctly
    // returns "not an HFA" -- the runtime never sets the IsHFA flag on these.

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void HfaCategory()
    {
        // Float HFAs of legal arity (1..4 elements).
        PassFloat2(default);
        PassFloat3(default);
        PassFloat4(default);

        // Double HFAs of legal arity.
        PassDouble2(default);
        PassDouble3(default);
        PassDouble4(default);

        // Nested HFAs: still classified as HFAs because the recursive
        // first-field walker reaches R4/R8.
        PassNestedFloat3(default);
        PassNestedDouble4(default);

        // Mixed-in args: HFA in register, refs elsewhere, ensures
        // ArgIterator correctly accounts for FP register consumption
        // when emitting GCRefMap tokens for the trailing INTEGER args.
        HfaThenRef(default, "x");
        RefThenHfa("x", default);
        TwoFloatHfas(default, default);

        // Returns: HFA returns use FP-register return paths on ARM/ARM64,
        // not the integer-return path, so HasRetBuffArg should be false.
        _ = ReturnFloat3();
        _ = ReturnDouble2();

        // HVAs (ARM64 only at the runtime level; classification falls
        // through to "not HFA" on other targets). All passed as default.
        Vec64FloatArg(default);
        Vec128FloatArg(default);
        VecNumericsFloatArg(default);

        // Negative cases -- runtime does not flag these as HFAs.
        PassFloat5(default);
        PassMixedR4R8(default);
    }

    // --- HFA struct shapes ---
    private struct Float2 { public float A, B; }
    private struct Float3 { public float A, B, C; }
    private struct Float4 { public float A, B, C, D; }
    private struct Float5 { public float A, B, C, D, E; }       // >4 fields: not HFA
    private struct Double2 { public double A, B; }
    private struct Double3 { public double A, B, C; }
    private struct Double4 { public double A, B, C, D; }
    private struct MixedR4R8 { public float A; public double B; } // mixed FP: not HFA

    // Nested HFA: first field is itself an HFA, total still <=4 R4/R8 elements.
    private struct NestedFloat3 { public Float2 Inner; public float C; }
    private struct NestedDouble4 { public Double2 First; public Double2 Second; }

    // --- HFA arg / return helpers ---
    [MethodImpl(MethodImplOptions.NoInlining)] private static void PassFloat2(Float2 s) { AllocBurst(); GC.KeepAlive((object)(s.A + s.B)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void PassFloat3(Float3 s) { AllocBurst(); GC.KeepAlive((object)(s.A + s.B + s.C)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void PassFloat4(Float4 s) { AllocBurst(); GC.KeepAlive((object)(s.A + s.B + s.C + s.D)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void PassFloat5(Float5 s) { AllocBurst(); GC.KeepAlive((object)(s.A + s.E)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void PassDouble2(Double2 s) { AllocBurst(); GC.KeepAlive((object)(s.A + s.B)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void PassDouble3(Double3 s) { AllocBurst(); GC.KeepAlive((object)(s.A + s.B + s.C)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void PassDouble4(Double4 s) { AllocBurst(); GC.KeepAlive((object)(s.A + s.B + s.C + s.D)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void PassMixedR4R8(MixedR4R8 s) { AllocBurst(); GC.KeepAlive((object)(s.A + s.B)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void PassNestedFloat3(NestedFloat3 s) { AllocBurst(); GC.KeepAlive((object)(s.Inner.A + s.C)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void PassNestedDouble4(NestedDouble4 s) { AllocBurst(); GC.KeepAlive((object)(s.First.A + s.Second.B)); }

    [MethodImpl(MethodImplOptions.NoInlining)] private static void HfaThenRef(Float3 s, string r) { AllocBurst(); GC.KeepAlive((object)(s.A)); GC.KeepAlive(r); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void RefThenHfa(string r, Float3 s) { AllocBurst(); GC.KeepAlive(r); GC.KeepAlive((object)(s.A)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void TwoFloatHfas(Float2 a, Float2 b) { AllocBurst(); GC.KeepAlive((object)(a.A + b.A)); }

    [MethodImpl(MethodImplOptions.NoInlining)] private static Float3 ReturnFloat3() { AllocBurst(); return default; }
    [MethodImpl(MethodImplOptions.NoInlining)] private static Double2 ReturnDouble2() { AllocBurst(); return default; }

    // --- HVA shapes (intrinsic Vector types). On non-FEATURE_HFA targets
    //     these go through the regular struct path; on ARM64 they hit the
    //     GetVectorHFAElementSize TypeDef-name match (Vector64/128 in
    //     System.Runtime.Intrinsics, Vector<T> in System.Numerics). ---
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Vec64FloatArg(Vector64<float> v) { AllocBurst(); GC.KeepAlive((object)v.GetElement(0)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Vec128FloatArg(Vector128<float> v) { AllocBurst(); GC.KeepAlive((object)v.GetElement(0)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void VecNumericsFloatArg(Vector<float> v) { AllocBurst(); GC.KeepAlive((object)v[0]); }

    // ===== Category 13: SystemV-AMD64 struct-in-register shapes =====
    // On non-Unix-x64 targets the cDAC's descriptor is absent so classification
    // returns "not in registers" and the runtime path agrees trivially. Linux/
    // macOS x64 read the cached SystemVEightByteRegistersInfo and produce
    // different GCRefMap tokens per eightbyte classification.

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SysVCategory()
    {
        // Single-eightbyte shapes.
        SysVIntPair(default);           // Integer
        SysVFloatPair(default);         // SSE
        SysVIntFloat(default);          // mixed -> Integer (Integer wins)
        SysVSingleRef(default);         // IntegerReference

        // Two-eightbyte shapes.
        SysVLongLong(default);          // Integer + Integer
        SysVLongDouble(default);        // Integer + SSE
        SysVDoubleDouble(default);      // SSE + SSE
        SysVRefInt(default);            // IntegerReference + Integer

        SysVNested(default);            // nested value type

        // Span<byte>: IntegerByRef + Integer.
        Span<byte> sp = stackalloc byte[8];
        SysVSpanByte(sp);

        SysVThreeRefs(default);         // 24 bytes -> stack
        SysVVector128Arg(default);      // intrinsic Vector -> stack
        SysVInt128Arg(default);         // 2 Integer eightbytes (runtime doesn't reject Int128 by name)
        SysVEmpty(default);             // 1-byte empty -> 1 Integer eightbyte (NoClass normalized to Integer)
        // SSE + IntegerReference: ref goes in RDI, not RSI -- SSE eightbytes
        // don't advance the general-register cursor.
        SysVDoubleRef(default);

        // Unions (LayoutKind.Explicit) -- exercises the classifier's merge path.
        SysVIntFloatUnion(default);     // int + float @ 0 -> Integer
        SysVIntIntUnion(default);       // int + int @ 0 -> Integer

        SysVSmall(default);             // { int a; byte b; } -> Integer (unaligned tail)
        SysVWrapInt(default);           // { int a; } single-field wrapper
    }

    // ---- SystemV struct shapes ----
    private struct IntPair { public int A, B; }
    private struct FloatPair { public float A, B; }
    private struct IntFloat { public int A; public float B; }
    private struct SingleRef { public object? R; }
    private struct LongLong { public long A, B; }
    private struct LongDouble { public long A; public double B; }
    private struct DoubleDouble { public double A, B; }
    private struct RefInt { public object? R; public long Padding; }
    private struct DoubleRef { public double D; public object? R; }
    private struct SysVNestedStruct { public IntPair Inner; public int Trailing; }
    private struct SysVThreeRefsStruct { public object? R1, R2, R3; }
    private struct Int128Wrapper { public System.Int128 V; }
    private struct EmptyStruct { }
    private struct SmallUnaligned { public int A; public byte B; }
    private struct WrapInt { public int A; }

    // Union: int and float at the same offset. Classifier sees both as
    // unique-offset fields and merges via ReClassifyField (Integer+SSE = Integer).
    [StructLayout(LayoutKind.Explicit)]
    private struct IntFloatUnion
    {
        [FieldOffset(0)] public int I;
        [FieldOffset(0)] public float F;
    }

    // Union: two ints at the same offset. Merge collapses to Integer.
    [StructLayout(LayoutKind.Explicit)]
    private struct IntIntUnion
    {
        [FieldOffset(0)] public int A;
        [FieldOffset(0)] public int B;
    }

    // ---- SystemV arg helpers ----
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVIntPair(IntPair s) { AllocBurst(); GC.KeepAlive((object)(s.A + s.B)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVFloatPair(FloatPair s) { AllocBurst(); GC.KeepAlive((object)(s.A + s.B)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVIntFloat(IntFloat s) { AllocBurst(); GC.KeepAlive((object)(s.A + s.B)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVSingleRef(SingleRef s) { AllocBurst(); GC.KeepAlive(s.R); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVLongLong(LongLong s) { AllocBurst(); GC.KeepAlive((object)(s.A + s.B)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVLongDouble(LongDouble s) { AllocBurst(); GC.KeepAlive((object)(s.A + s.B)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVDoubleDouble(DoubleDouble s) { AllocBurst(); GC.KeepAlive((object)(s.A + s.B)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVRefInt(RefInt s) { AllocBurst(); GC.KeepAlive(s.R); GC.KeepAlive((object)s.Padding); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVDoubleRef(DoubleRef s) { AllocBurst(); GC.KeepAlive(s.R); GC.KeepAlive((object)s.D); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVNested(SysVNestedStruct s) { AllocBurst(); GC.KeepAlive((object)(s.Inner.A + s.Trailing)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVSpanByte(Span<byte> s) { AllocBurst(); GC.KeepAlive((object)s.Length); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVThreeRefs(SysVThreeRefsStruct s) { AllocBurst(); GC.KeepAlive(s.R1); GC.KeepAlive(s.R2); GC.KeepAlive(s.R3); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVVector128Arg(Vector128<int> v) { AllocBurst(); GC.KeepAlive((object)v.GetElement(0)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVInt128Arg(Int128Wrapper s) { AllocBurst(); GC.KeepAlive((object)s.V.ToString()); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVEmpty(EmptyStruct s) { AllocBurst(); GC.KeepAlive((object)s); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVSmall(SmallUnaligned s) { AllocBurst(); GC.KeepAlive((object)(s.A + s.B)); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVWrapInt(WrapInt s) { AllocBurst(); GC.KeepAlive((object)s.A); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVIntFloatUnion(IntFloatUnion s) { AllocBurst(); GC.KeepAlive((object)s.I); }
    [MethodImpl(MethodImplOptions.NoInlining)] private static void SysVIntIntUnion(IntIntUnion s) { AllocBurst(); GC.KeepAlive((object)s.A); }

    // ===== Category 14: deep stack =====
    // Mutually-recursive chains of methods with mixed signatures. At any
    // given allocation trigger many frames are simultaneously live, so a
    // single stack-walk verification run touches multiple MDs across
    // diverse signature shapes.

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DeepStackCategory()
    {
        DeepA("a", 1, 2L);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DeepA(string a, int b, long c)
    {
        AllocBurst();
        DeepB(b, a, new RefAtStartStruct { R = a, Trailer = (int)c });
        GC.KeepAlive(a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DeepB(int b, string a, RefAtStartStruct s)
    {
        AllocBurst();
        DeepC(s, a, b);
        GC.KeepAlive(s.R);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DeepC(RefAtStartStruct s, string a, int b)
    {
        AllocBurst();
        Span<byte> sp = stackalloc byte[8];
        DeepD(sp, a, s, b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DeepD(Span<byte> sp, string a, RefAtStartStruct s, int b)
    {
        AllocBurst();
        DeepE("inner", a, s);
        GC.KeepAlive(sp.Length);
        GC.KeepAlive((object)b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DeepE(string label, string a, RefAtStartStruct s)
    {
        AllocBurst();
        GC.KeepAlive(label);
        GC.KeepAlive(a);
        GC.KeepAlive(s.R);
    }
}
