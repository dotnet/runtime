// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// Debuggee for cDAC <c>CallSiteLayoutDumpTests</c>. Builds a deep call chain
/// where every frame remains live on the stack at <see cref="Environment.FailFast"/>.
/// Each method exercises a different signature shape so the test can assert
/// <c>ICallingConvention.ComputeCallSiteLayout</c> produces the expected
/// <c>ArgLayout</c> per category:
///   varargs, byref, structs (by-value &amp; ABI-byref), managed objects, ByRefLike.
///
/// All methods are <see cref="MethodImplOptions.NoInlining"/> so each frame is
/// independently discoverable by name in the dump.
/// </summary>
internal static class Program
{
    private static void Main()
    {
        M_Varargs(1, __arglist(42, 43));
    }

    // ----- varargs -----

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_Varargs(int fixedArg, __arglist)
    {
        GC.KeepAlive(fixedArg);
        int local = 7;
        M_RefInt(ref local);
    }

    // ----- byref -----

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_RefInt(ref int x)
    {
        x++;
        M_OutObject(out object o);
        GC.KeepAlive(o);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_OutObject(out object o)
    {
        o = new object();
        Guid g = new Guid("11111111-2222-3333-4444-555555555555");
        M_RefGuid(ref g);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_RefGuid(ref Guid g)
    {
        GC.KeepAlive(g);
        StructWithRef s = new StructWithRef { Ref = "cDAC-CallSiteLayout-StringRef" };
        M_SmallStructWithRef(s);
    }

    // ----- structs (by-value) -----

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_SmallStructWithRef(StructWithRef s)
    {
        GC.KeepAlive(s.Ref);
        TwoInts p = new TwoInts { X = 1, Y = 2 };
        M_TwoInts(p);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_TwoInts(TwoInts p)
    {
        GC.KeepAlive(p.X + p.Y);
        Guid g = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        M_Guid(g);
    }

    // ----- struct >8 bytes -> ABI byref -----

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_Guid(Guid g)
    {
        GC.KeepAlive(g);
        KeyValuePair<string, string> kvp = new KeyValuePair<string, string>("k", "v");
        M_KvpStringString(kvp);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_KvpStringString(KeyValuePair<string, string> kvp)
    {
        GC.KeepAlive(kvp.Key);
        GC.KeepAlive(kvp.Value);
        M_String("cDAC-CallSiteLayout-StringArg");
    }

    // ----- managed objects -----

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_String(string s)
    {
        GC.KeepAlive(s);
        M_Object(new object());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_Object(object o)
    {
        GC.KeepAlive(o);
        M_IntArray(_intArrayArg);
    }

    private static readonly int[] _intArrayArg = [1, 2, 3];
    private static readonly int[] _spanBuffer = [10, 20, 30];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_IntArray(int[] a)
    {
        GC.KeepAlive(a);
        M_SpanInt(_spanBuffer.AsSpan());
    }

    // ----- ByRefLike -----

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_SpanInt(Span<int> s)
    {
        s[0]++;
        int local = 99;
        M_TinyRefStruct(new SmallRefStruct(ref local));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_TinyRefStruct(SmallRefStruct t)
    {
        t.Bump();
        OneByteStruct ob = new OneByteStruct { X = 0xAB };
        M_OneByteStruct(ob);
    }

    // ----- size-rule matrix: 1-byte struct enregistered by value -----

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_OneByteStruct(OneByteStruct s)
    {
        GC.KeepAlive(s.X);
        (int, int, int) v = (1, 2, 3);
        M_TwelveByteValueTuple(v);
    }

    // ----- 12-byte non-pow2 struct -> ABI byref -----

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_TwelveByteValueTuple((int, int, int) v)
    {
        GC.KeepAlive(v.Item1 + v.Item2 + v.Item3);
        M_DecimalArg(123.456m);
    }

    // ----- 16-byte BCL struct -> ABI byref -----

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_DecimalArg(decimal d)
    {
        GC.KeepAlive(d);
        M_ManyInts(1, 2, 3, 4, 5, 6);
    }

    // ----- 6 ints -> last two spill to the stack -----

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_ManyInts(int a, int b, int c, int d, int e, int f)
    {
        GC.KeepAlive(a + b + c + d + e + f);
        M_MixedIntDouble(7, 1.5, 8, 2.5);
    }

    // ----- alternating int/double -> exercises FP register lanes (XMM1, XMM3) -----

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_MixedIntDouble(int a, double b, int c, double d)
    {
        GC.KeepAlive(a + c);
        GC.KeepAlive(b + d);
        KeyValuePair<string, int> kvp = new KeyValuePair<string, int>("k2", 9);
        M_KvpStringInt(kvp);
    }

    // ----- heterogeneous generic type-args (ref + primitive) -----

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_KvpStringInt(KeyValuePair<string, int> kvp)
    {
        GC.KeepAlive(kvp.Key);
        GC.KeepAlive(kvp.Value);
        KeyValuePair<int, KeyValuePair<int, int>> nested =
            new KeyValuePair<int, KeyValuePair<int, int>>(1, new KeyValuePair<int, int>(2, 3));
        M_KvpIntKvpIntInt(nested);
    }

    // ----- nested generic: both levels are inline GENERICINST blobs, resolved
    // ----- by recursive GetGenericInstantiation (not via GetTypeFromSpecification).

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_KvpIntKvpIntInt(KeyValuePair<int, KeyValuePair<int, int>> nested)
    {
        GC.KeepAlive(nested.Key);
        GC.KeepAlive(nested.Value.Key + nested.Value.Value);
        M_EnumByte(SmallByteEnum.B);
    }

    // ----- enum collapses to its underlying primitive (U1) -----

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_EnumByte(SmallByteEnum e)
    {
        GC.KeepAlive((int)e);
        InstanceCallee callee = new InstanceCallee();
        callee.M_InstanceIntInt(11, 22);
    }
}

internal sealed class InstanceCallee
{
    private int _seed = 7;

    // ----- instance method: layout has a populated ThisOffset -----

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void M_InstanceIntInt(int x, int y)
    {
        GC.KeepAlive(_seed + x + y);
        Environment.FailFast("cDAC dump test: CallSiteLayout intentional crash");
    }
}

internal struct OneByteStruct
{
    public byte X;
}

internal enum SmallByteEnum : byte
{
    A = 1,
    B = 2,
}

internal struct StructWithRef
{
    public string Ref;
}

internal struct TwoInts
{
    public int X;
    public int Y;
}

/// <summary>
/// 8-byte ref struct used to exercise the <c>IsByRefLike</c> guard in
/// <c>ComputeValueTypeHandle</c>. A single <c>ref int</c> field keeps the
/// struct pointer-sized so the Win-x64 ABI passes it by value (not as an
/// implicit byref).
/// </summary>
internal ref struct SmallRefStruct
{
    public ref int Field;

    public SmallRefStruct(ref int r)
    {
        Field = ref r;
    }

    public void Bump() => Field++;
}
