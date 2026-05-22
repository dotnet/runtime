// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

internal static class Program
{
    private static void Main()
    {
        M_Empty();
    }

    // ===== Category A: register-bank fill / spill, no GC refs =====

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_Empty()
    {
        M_Int_Six(1, 2, 3, 4, 5, 6);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_Int_Six(int a, int b, int c, int d, int e, int f)
    {
        GC.KeepAlive(a + b + c + d + e + f);
        M_Int_Nine(1, 2, 3, 4, 5, 6, 7, 8, 9);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_Int_Nine(int a, int b, int c, int d, int e, int f, int g, int h, int i)
    {
        GC.KeepAlive(a + b + c + d + e + f + g + h + i);
        M_Double_Four(1.5, 2.5, 3.5, 4.5);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_Double_Four(double a, double b, double c, double d)
    {
        GC.KeepAlive(a + b + c + d);
        M_Double_Nine(1.5, 2.5, 3.5, 4.5, 5.5, 6.5, 7.5, 8.5, 9.5);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_Double_Nine(double a, double b, double c, double d, double e, double f, double g, double h, double i)
    {
        GC.KeepAlive(a + b + c + d + e + f + g + h + i);
        M_MixedID(1, 1.5, 2, 2.5, 3, 3.5);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_MixedID(int a, double b, int c, double d, int e, double f)
    {
        GC.KeepAlive(a + c + e);
        GC.KeepAlive(b + d + f);
        M_RefArgs_String("cDAC-CSL-String");
    }

    // ===== Category B: reference-typed args =====

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_RefArgs_String(string s)
    {
        GC.KeepAlive(s);
        M_RefArgs_Object(new object());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_RefArgs_Object(object o)
    {
        GC.KeepAlive(o);
        M_RefArgs_SzArray(_intArr);
    }

    private static readonly int[] _intArr = [1, 2, 3];
    private static readonly int[,] _intMdArr = new int[2, 2] { { 1, 2 }, { 3, 4 } };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_RefArgs_SzArray(int[] a)
    {
        GC.KeepAlive(a);
        M_RefArgs_MdArray(_intMdArr);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_RefArgs_MdArray(int[,] a)
    {
        GC.KeepAlive(a);
        int local = 42;
        M_RefArgs_RefInt(ref local);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_RefArgs_RefInt(ref int x)
    {
        x++;
        M_RefArgs_OutObject(out object obj);
        GC.KeepAlive(obj);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_RefArgs_OutObject(out object o)
    {
        o = new object();
        Guid g = new Guid("11111111-2222-3333-4444-555555555555");
        M_RefArgs_RefStruct(ref g);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_RefArgs_RefStruct(ref Guid g)
    {
        GC.KeepAlive(g);
        M_VT_Byte(new ByteStruct { X = 0xAB });
    }

    // ===== Category C: small by-value structs =====

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_Byte(ByteStruct s)
    {
        GC.KeepAlive(s.X);
        M_VT_Short(new ShortStruct { X = 0x1234 });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_Short(ShortStruct s)
    {
        GC.KeepAlive(s.X);
        M_VT_Int(new IntStruct { X = unchecked((int)0xDEADBEEF) });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_Int(IntStruct s)
    {
        GC.KeepAlive(s.X);
        M_VT_TwoInts(new TwoInts { X = 1, Y = 2 });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_TwoInts(TwoInts s)
    {
        GC.KeepAlive(s.X + s.Y);
        M_VT_ObjectOnly(new ObjectStruct { Ref = new object() });
    }

    // 8-byte by-value struct holding one object ref -- the canonical Win-x64 GCDesc-walk case.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_ObjectOnly(ObjectStruct s)
    {
        GC.KeepAlive(s.Ref);
        M_VT_StringOnly(new StringStruct { Ref = "cDAC-CSL-StringInStruct" });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_StringOnly(StringStruct s)
    {
        GC.KeepAlive(s.Ref);
        M_VT_ThreeByte(new ThreeByteStruct { A = 1, B = 2, C = 3 });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_ThreeByte(ThreeByteStruct s)
    {
        GC.KeepAlive(s.A + s.B + s.C);
        M_VT_FiveByte(new FiveByteStruct { I = 0x01020304, B = 5 });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_FiveByte(FiveByteStruct s)
    {
        GC.KeepAlive(s.I + s.B);
        M_VT_Twelve((1, 2, 3));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_Twelve((int X, int Y, int Z) v)
    {
        GC.KeepAlive(v.X + v.Y + v.Z);
        M_VT_Guid(new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_Guid(Guid g)
    {
        GC.KeepAlive(g);
        M_VT_Decimal(123.456m);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_Decimal(decimal d)
    {
        GC.KeepAlive(d);
        M_VT_KvpStrStr(new KeyValuePair<string, string>("k", "v"));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_KvpStrStr(KeyValuePair<string, string> kvp)
    {
        GC.KeepAlive(kvp.Key);
        GC.KeepAlive(kvp.Value);
        M_VT_KvpStrInt(new KeyValuePair<string, int>("k2", 99));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_KvpStrInt(KeyValuePair<string, int> kvp)
    {
        GC.KeepAlive(kvp.Key);
        GC.KeepAlive(kvp.Value);
        M_VT_TwoFloats(new TwoFloats { X = 1.5f, Y = 2.5f });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_TwoFloats(TwoFloats s)
    {
        GC.KeepAlive(s.X + s.Y);
        M_VT_TwoDoubles(new TwoDoubles { X = 1.5, Y = 2.5 });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_TwoDoubles(TwoDoubles s)
    {
        GC.KeepAlive(s.X + s.Y);
        M_VT_IntDouble(new IntDoubleStruct { I = 7, D = 8.5 });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_IntDouble(IntDoubleStruct s)
    {
        GC.KeepAlive(s.I);
        GC.KeepAlive(s.D);
        M_VT_Empty(default);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_Empty(EmptyStruct _)
    {
        M_VT_Big24(new Big24NoRef { A = 1, B = 2, C = 3 });
    }

    // ===== Category D: large stack-passed structs (>16) =====

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_Big24(Big24NoRef s)
    {
        GC.KeepAlive(s.A + s.B + s.C);
        M_VT_Big24WithRef(new Big24WithRef { Prefix = 0x11, Ref = new object(), Suffix = 0x22 });
    }

    // 24-byte struct with object ref at offset 8 -- the canonical SysV GCDesc-walk case.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_Big24WithRef(Big24WithRef s)
    {
        GC.KeepAlive(s.Prefix);
        GC.KeepAlive(s.Ref);
        GC.KeepAlive(s.Suffix);
        M_VT_Big48WithTwoRefs(new Big48TwoRefs { A = 1, R1 = "r1", B = 2, R2 = "r2", C = 3 });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_VT_Big48WithTwoRefs(Big48TwoRefs s)
    {
        GC.KeepAlive(s.A + s.B + s.C);
        GC.KeepAlive(s.R1);
        GC.KeepAlive(s.R2);
        M_BRL_SpanInt(_spanIntBuffer.AsSpan());
    }

    // ===== Category E: ByRefLike =====

    private static readonly int[] _spanIntBuffer = [10, 20, 30];
    private static readonly object[] _spanObjBuffer = [new object(), new object()];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_BRL_SpanInt(Span<int> s)
    {
        s[0]++;
        M_BRL_SpanObject(_spanObjBuffer.AsSpan());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_BRL_SpanObject(Span<object> s)
    {
        GC.KeepAlive(s[0]);
        int local = 100;
        M_BRL_SmallRefStruct(new SmallRefStruct(ref local));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_BRL_SmallRefStruct(SmallRefStruct t)
    {
        t.Bump();
        M_BRL_RefStructWithObject(new RefStructWithObject { Obj = new object(), Prim = 7 });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_BRL_RefStructWithObject(RefStructWithObject r)
    {
        GC.KeepAlive(r.Obj);
        GC.KeepAlive(r.Prim);
        new InstanceCallee().M_Special_Instance(11, 22);
    }

    // ===== Category F: special arg slots =====

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Big32Struct M_Special_RetBufSmall(int seed)
    {
        Big32Struct ret = default;
        ret.A = seed;
        ret.B = seed + 1;
        ret.C = seed + 2;
        ret.D = seed + 3;
        // Continue chain inside the retbuf method so the frame is live.
        M_Special_RetBufLargeWithDouble(1.5);
        return ret;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Big64Struct M_Special_RetBufLargeWithDouble(double firstUserArg)
    {
        Big64Struct ret = default;
        ret.A = (long)firstUserArg;
        ret.B = ret.C = ret.D = ret.E = ret.F = ret.G = ret.H = 0;
        M_Special_GenericRef("cDAC-generic-ref");
        return ret;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_Special_GenericRef<T>(T x) where T : class
    {
        GC.KeepAlive(x);
        M_Special_GenericVal(42);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_Special_GenericVal<T>(T x) where T : struct
    {
        GC.KeepAlive(x);
        M_Special_Varargs(1, __arglist(42, 43));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_Special_Varargs(int fixedArg, __arglist)
    {
        GC.KeepAlive(fixedArg);
        M_Special_NoVarargs(1, 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M_Special_NoVarargs(int a, int b)
    {
        GC.KeepAlive(a + b);
        new GenericContainer<string>().M_Special_InstanceGenericClassGenericMethod<int>("a", 7);
    }

    // ===== Category G: vectors =====

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void M_Vec_64(Vector64<int> v)
    {
        GC.KeepAlive(v);
        M_Vec_128(Vector128<int>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void M_Vec_128(Vector128<int> v)
    {
        GC.KeepAlive(v);
        M_Vec_256(Vector256<int>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void M_Vec_256(Vector256<int> v)
    {
        GC.KeepAlive(v);
        M_Vec_512(Vector512<int>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void M_Vec_512(Vector512<int> v)
    {
        GC.KeepAlive(v);
        M_Combo_InstanceManyArgs(new ComboReceiver(), 1, new object(),
            new KeyValuePair<string, int>("c", 2), _spanObjBuffer.AsSpan(), 5);
    }

    // ===== Category H: composites =====

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void M_Combo_InstanceManyArgs(ComboReceiver r, int ix, object o,
        KeyValuePair<string, int> kvp, Span<object> span, int tail)
    {
        r.M_Combo_InstanceMethodRun(ix, o, kvp, span, tail);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Big32Struct M_Combo_VarargsRetbuf(int seed, __arglist)
    {
        Big32Struct ret = default;
        ret.A = seed;
        M_Combo_RegBankExhaustion(
            1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5, 5, 5.5, 6, 6.5,
            new object(), "tail-string", _comboTailArr);
        return ret;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void M_Combo_RegBankExhaustion(
        int i1, double d1, int i2, double d2, int i3, double d3,
        int i4, double d4, int i5, double d5, int i6, double d6,
        object o, string s, int[] a)
    {
        GC.KeepAlive(i1 + i2 + i3 + i4 + i5 + i6);
        GC.KeepAlive(d1 + d2 + d3 + d4 + d5 + d6);
        GC.KeepAlive(o);
        GC.KeepAlive(s);
        GC.KeepAlive(a);
        M_Combo_DeepNestedGeneric(new KeyValuePair<KeyValuePair<string, Guid>, int>(
            new KeyValuePair<string, Guid>("nested", Guid.Empty), 99));
    }

    private static readonly int[] _comboTailArr = [1];

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void M_Combo_DeepNestedGeneric(KeyValuePair<KeyValuePair<string, Guid>, int> v)
    {
        GC.KeepAlive(v.Key.Key);
        GC.KeepAlive(v.Key.Value);
        GC.KeepAlive(v.Value);
        int local = 0;
        M_Combo_RefStructWithMultipleRefs(new MultiRefStruct(ref local, new object(), "s"));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void M_Combo_RefStructWithMultipleRefs(MultiRefStruct mrs)
    {
        mrs.Touch();
        Environment.FailFast("cDAC dump test: CallSiteLayout intentional crash");
    }
}

// ===== Instance / generic receiver classes =====

internal sealed class InstanceCallee
{
    private int _seed = 7;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void M_Special_Instance(int x, int y)
    {
        GC.KeepAlive(_seed + x + y);
        Program.M_Special_RetBufSmall(3);
    }
}

internal sealed class GenericContainer<T>
{
    private int _seed = 13;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void M_Special_InstanceGenericClassGenericMethod<U>(T classT, U methodU)
    {
        GC.KeepAlive(_seed);
        GC.KeepAlive(classT);
        GC.KeepAlive(methodU);
        Program.M_Vec_64(Vector64<int>.Zero);
    }
}

internal sealed class ComboReceiver
{
    private long _bias = 0x10000;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void M_Combo_InstanceMethodRun(int ix, object o, KeyValuePair<string, int> kvp, Span<object> span, int tail)
    {
        GC.KeepAlive(_bias + ix + tail);
        GC.KeepAlive(o);
        GC.KeepAlive(kvp.Key);
        GC.KeepAlive(span[0]);
        Program.M_Combo_VarargsRetbuf(123, __arglist("a", 1.5));
    }
}

// ===== Type definitions =====

internal struct EmptyStruct { }

internal struct ByteStruct { public byte X; }
internal struct ShortStruct { public short X; }
internal struct IntStruct { public int X; }
internal struct TwoInts { public int X; public int Y; }
internal struct ObjectStruct { public object Ref; }
internal struct StringStruct { public string Ref; }

internal struct ThreeByteStruct { public byte A; public byte B; public byte C; }

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
internal struct FiveByteStruct { public int I; public byte B; }

internal struct TwoFloats { public float X; public float Y; }
internal struct TwoDoubles { public double X; public double Y; }
internal struct IntDoubleStruct { public int I; public double D; }

internal struct Big24NoRef { public long A; public long B; public long C; }
internal struct Big24WithRef { public long Prefix; public object Ref; public long Suffix; }
internal struct Big48TwoRefs
{
    public long A; public string R1; public long B; public string R2; public long C;
}

internal struct Big32Struct { public long A; public long B; public long C; public long D; }
internal struct Big64Struct
{
    public long A; public long B; public long C; public long D;
    public long E; public long F; public long G; public long H;
}

internal ref struct SmallRefStruct
{
    public ref int Field;

    public SmallRefStruct(ref int r)
    {
        Field = ref r;
    }

    public void Bump() => Field++;
}

internal ref struct RefStructWithObject
{
    public object Obj;
    public int Prim;
}

internal ref struct MultiRefStruct
{
    public ref int Local;
    public object Obj;
    public string Str;

    public MultiRefStruct(ref int local, object obj, string str)
    {
        Local = ref local;
        Obj = obj;
        Str = str;
    }

    public void Touch()
    {
        Local++;
        GC.KeepAlive(Obj);
        GC.KeepAlive(Str);
    }
}
