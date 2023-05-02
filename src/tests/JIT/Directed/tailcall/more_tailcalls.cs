// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Note: This test file is the source of the more_tailcalls.il file. It requires
// InlineIL.Fody to compile. It is not used as anything but a reference of that
// IL file.

using InlineIL;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

struct S16
{
    public long A, B;
    public override string ToString() => $"{A}, {B}";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string InstanceMethod() => "Instance method";
}
struct S32
{
    public long A, B, C, D;
    public override string ToString() => $"{A}, {B}, {C}, {D}";
}
struct SGC
{
    public object A;
    public object B;
    public string C;
    public string D;
    public override string ToString() => $"{A}, {B}, {C}, {D}";
}
struct SGC2
{
    public int A;
    public SGC B;
    public object C;
    public int D;
    public override string ToString() => $"{A}, ({B}), {C}, {D}";
}

class HeapInt
{
    public int Value;
    public HeapInt(int val) => Value = val;
    public override string ToString() => $"{Value}";
}

public class Program
{
    private static readonly IntPtr s_calcStaticCalli;
    private static readonly IntPtr s_calcStaticCalliOther;
    private static readonly IntPtr s_calcStaticCalliRetbuf;
    private static readonly IntPtr s_calcStaticCalliRetbufOther;
    private static readonly IntPtr s_emptyCalliOther;
    private static readonly IntPtr s_instanceMethodOnValueType;

    static Program()
    {
        IL.Emit.Ldftn(new MethodRef(typeof(Program), nameof(CalcStaticCalli)));
        IL.Pop(out IntPtr calcStaticCalli);
        s_calcStaticCalli = calcStaticCalli;

        IL.Emit.Ldftn(new MethodRef(typeof(Program), nameof(CalcStaticCalliOther)));
        IL.Pop(out IntPtr calcStaticCalliOther);
        s_calcStaticCalliOther = calcStaticCalliOther;

        IL.Emit.Ldftn(new MethodRef(typeof(Program), nameof(CalcStaticCalliRetbuf)));
        IL.Pop(out IntPtr calcStaticCalliRetbuf);
        s_calcStaticCalliRetbuf = calcStaticCalliRetbuf;

        IL.Emit.Ldftn(new MethodRef(typeof(Program), nameof(CalcStaticCalliRetbufOther)));
        IL.Pop(out IntPtr calcStaticCalliRetbufOther);
        s_calcStaticCalliRetbufOther = calcStaticCalliRetbufOther;

        IL.Emit.Ldftn(new MethodRef(typeof(Program), nameof(EmptyCalliOther)));
        IL.Pop(out IntPtr emptyCalliOther);
        s_emptyCalliOther = emptyCalliOther;

        IL.Emit.Ldftn(new MethodRef(typeof(S16), nameof(S16.InstanceMethod)));
        IL.Pop(out IntPtr instanceMethodOnValueType);
        s_instanceMethodOnValueType = instanceMethodOnValueType;
    }

    [Fact]
    public static int Main()
    {
        const int numCalcIters = 1000000;
        const int countUpIters = 1000000;

        int x = numCalcIters;
        S32 s = default;
        int expected = 0;

        while (x != 0)
        {
            if (x % 2 == 0)
                s = default;

            Calc(ref x, ref s, ref expected);
        }

        bool result = true;
        void Test<T>(Func<T> f, T expected, string name)
        {
            Console.Write("{0}: ", name);
            Stopwatch timer = Stopwatch.StartNew();
            T val = f();
            timer.Stop();
            if (val.Equals(expected))
            {
                Console.WriteLine("OK in {1} ms", name, timer.ElapsedMilliseconds);
                return;
            }

            Console.WriteLine("FAIL (expected {1}, got {2})", name, expected, val);
            result = false;
        }

        void TestCalc<T>(Func<int, int, T> f, T expected, string name)
            => Test(() => f(numCalcIters, 0), expected, name);

        ClassImpl c = new ClassImpl();
        c.Other = c;

        InterfaceImpl i = new InterfaceImpl();
        i.Other = i;

        GenInstance<string, int> g = new GenInstance<string, int>();
        IGenInterface<string, int> ig = new GenInterfaceImpl<string, int>();
        IGenInterface<string, object> ig2 = new GenInterfaceImpl<string, object>();
        GenAbstractImpl<string> ga1 = new GenAbstractImpl<string>();
        GenAbstractImpl<int> ga2 = new GenAbstractImpl<int>();

        long expectedI8 = (long)(((ulong)(uint)expected << 32) | (uint)expected);
        S16 expectedS16 = new S16 { A = expected, B = expected, };
        S32 expectedS32 = new S32 { A = expected, B = expected, C = expected, D = expected, };
        int ten = 10;

        TestCalc(CalcStatic, expected, "Static non-generic");
        TestCalc(CalcStaticSmall, (byte)expected, "Static non-generic small");
        TestCalc(CalcStaticRetbuf, expectedS32, "Static non-generic retbuf");
        TestCalc(CalcStaticLong, expectedI8, "Static non-generic long");
        TestCalc(CalcStaticS16, expectedS16, "Static non-generic S16");
        TestCalc((x, s) => {CalcStaticVoid(x, s); return s_result;}, expected, "Static void");
        TestCalc(new Instance().CalcInstance, expected, "Instance non-generic");
        TestCalc(new Instance().CalcInstanceRetbuf, expectedS32, "Instance non-generic retbuf");
        TestCalc(c.CalcAbstract, expected, "Abstract class non-generic");
        TestCalc(c.CalcAbstractRetbuf, expectedS32, "Abstract class non-generic retbuf");
        TestCalc(i.CalcInterface, expected, "Interface non-generic");
        TestCalc(i.CalcInterfaceRetbuf, expectedS32, "Interface non-generic retbuf");
        TestCalc(CalcStaticCalli, expected, "Static calli");
        TestCalc(CalcStaticCalliRetbuf, expectedS32, "Static calli retbuf");
        TestCalc(new Instance().CalcInstanceCalli, expected, "Instance calli");
        TestCalc(new Instance().CalcInstanceCalliRetbuf, expectedS32, "Instance calli retbuf");
        Test(() => EmptyCalli(), "Empty calli", "Static calli without args");
        Test(() => ValueTypeInstanceMethodCalli(), "Instance method", "calli to an instance method on a value type");
        Test(() => ValueTypeExplicitThisInstanceMethodCalli(), "Instance method", "calli to an instance method on a value type with explicit this");
        Test(() => { var v = new InstanceValueType(); v.CountUp(countUpIters); return v.Count; }, countUpIters, "Value type instance call");
        Test(() => new Instance().GC("2", 3, "4", 5, "6", "7", "8", 9, ref ten), "2 3 4 5 6 7 8 9 10", "Instance with GC");
        Test(() => CountUpHeap(countUpIters, new HeapInt(0)), countUpIters, "Count up with heap int");
        Test(() => { int[] val = new int[1]; CountUpRef(countUpIters, ref val[0]); return val[0]; }, countUpIters, "Count up with byref to heap");
        Test(() => GenName1Forward("hello"), "System.String hello", "Static generic string");
        Test(() => GenName1Forward<object>("hello"), "System.Object hello", "Static generic object");
        Test(() => GenName1Forward(5), "System.Int32 5", "Static generic int");
        Test(() => GenName2ForwardBoth("hello", (object)"hello2"), "System.String System.Object hello hello2", "Static generic 2 string object");
        Test(() => GenName2ForwardBoth("hello", 5), "System.String System.Int32 hello 5", "Static generic 2 string int");
        Test(() => GenName2ForwardOne("hello", "hello2"), "System.String System.String hello hello2", "Static generic 1 string");
        Test(() => GenName2ForwardOne((object)"hello", "hello2"), "System.Object System.String hello hello2", "Static generic 1 object");
        Test(() => GenName2ForwardOne(5, "hello2"), "System.Int32 System.String 5 hello2", "Static generic 1 int");
        Test(() => GenName2ForwardNone("hello", "hello2"), "System.Object System.String hello hello2", "Static generic 0");
        Test(() => g.NonVirtForward<object, string>("a", 5, "b", "c"),
             "System.String System.Int32 System.Object System.String a 5 b c", "Instance generic 4");
        Test(() => g.VirtForward<object, string>("a", 5, "b", "c"),
             "System.String System.Int32 System.Object System.String a 5 b c", "Virtual instance generic 4");
        Test(() => GenInterfaceForwardF<string, int, string, object>("a", 5, "c", "d", ig),
            "System.String System.Int32 System.String System.Object a 5 c d", "Interface generic 4");
        Test(() => GenInterfaceForwardG<string, int>("a", 5, ig),
            "System.String System.Int32 a 5", "Interface generic forward G");
        Test(() => GenInterfaceForwardNone("a", "b", 5, "d", ig2),
             "System.String System.Object System.Int32 System.Object a b 5 d", "Interface generic 0");
        Test(() => GenInterfaceForward2("a", "b", ig2),
             "System.String System.Object a b", "Interface generic without generics on method");
        Test(() => GenAbstractFString(ga1), "System.String System.Object", "Abstract generic with generic on method 1");
        Test(() => GenAbstractFInt(ga2), "System.Int32 System.Object", "Abstract generic with generic on method 2");
        Test(() => GenAbstractGString(ga1), "System.String", "Abstract generic without generic on method 1");
        Test(() => GenAbstractGInt(ga2), "System.Int32", "Abstract generic without generic on method 2");

        int[] a = new int[1_000_000];
        a[99] = 1;
        Test(() => InstantiatingStub1(0, 0, "string", a), a.Length + 1, "Instantiating stub direct");

        Test(() => VirtCallThisHasSideEffects(), 1, "Virtual call where computing \"this\" has side effects");

        if (result)
            Console.WriteLine("All tailcall-via-help succeeded");
        else
            Console.WriteLine("One or more failures in tailcall-via-help test");

        return result ? 100 : 1;
    }

    public static void Calc(ref int x, ref S32 s, ref int acc)
    {
        if (x % 2 == 0)
            acc += (int)(x * 3 + s.A * 7 + s.B * 9 + s.C * -3 + s.D * 4);
        else
            acc += (int)(x * 1 + s.A * 9 + s.B * 3 + s.C * -4 + s.D * 5);

        x--;
        s.A = 11*x;
        s.B = 14*x;
        s.C = -14*x;
        s.D = 3*x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int CalcStatic(int x, int acc)
    {
        if (x == 0)
            return acc;

        S32 s = default;
        Calc(ref x, ref s, ref acc);

        IL.Push(x);
        IL.Push(s);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(CalcStaticOther)));
        return IL.Return<int>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CalcStaticOther(int x, S32 large, int acc)
    {
        Calc(ref x, ref large, ref acc);

        IL.Push(x);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(CalcStatic)));
        return IL.Return<int>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe byte CalcStaticSmall(int x, int acc)
    {
        if (x == 0)
            return (byte)acc;

        S32 s = default;
        Calc(ref x, ref s, ref acc);

        IL.Push(x);
        IL.Push(s);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(CalcStaticSmallOther)));
        return IL.Return<byte>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CalcStaticSmallOther(int x, S32 large, int acc)
    {
        Calc(ref x, ref large, ref acc);

        IL.Push(x);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(CalcStaticSmall)));
        return IL.Return<byte>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S32 CalcStaticRetbuf(int x, int acc)
    {
        if (x == 0)
            return new S32 { A = acc, B = acc, C = acc, D = acc, };

        S32 s = default;
        Calc(ref x, ref s, ref acc);

        IL.Push(x);
        IL.Push(s);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(CalcStaticRetbufOther)));
        return IL.Return<S32>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S32 CalcStaticRetbufOther(int x, S32 large, int acc)
    {
        Calc(ref x, ref large, ref acc);

        IL.Push(x);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(CalcStaticRetbuf)));
        return IL.Return<S32>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long CalcStaticLong(int x, int acc)
    {
        if (x == 0)
            return (long)(((ulong)(uint)acc << 32) | (uint)acc);

        S32 s = default;
        Calc(ref x, ref s, ref acc);

        IL.Push(x);
        IL.Push(s);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(CalcStaticLongOther)));
        return IL.Return<long>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long CalcStaticLongOther(int x, S32 large, int acc)
    {
        Calc(ref x, ref large, ref acc);

        IL.Push(x);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(CalcStaticLong)));
        return IL.Return<long>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S16 CalcStaticS16(int x, int acc)
    {
        if (x == 0)
            return new S16 { A = acc, B = acc };

        S32 s = default;
        Calc(ref x, ref s, ref acc);

        IL.Push(x);
        IL.Push(s);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(CalcStaticS16Other)));
        return IL.Return<S16>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S16 CalcStaticS16Other(int x, S32 large, int acc)
    {
        Calc(ref x, ref large, ref acc);

        IL.Push(x);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(CalcStaticS16)));
        return IL.Return<S16>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CalcStaticCalli(int x, int acc)
    {
        if (x == 0)
            return acc;

        S32 s = default;
        Calc(ref x, ref s, ref acc);

        IL.Push(x);
        IL.Push(s);
        IL.Push(acc);
        IL.Push(s_calcStaticCalliOther);
        IL.Emit.Tail();
        IL.Emit.Calli(new StandAloneMethodSig(CallingConventions.Standard, typeof(int), typeof(int), typeof(S32), typeof(int)));
        return IL.Return<int>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CalcStaticCalliOther(int x, S32 large, int acc)
    {
        Calc(ref x, ref large, ref acc);

        IL.Push(x);
        IL.Push(acc);
        IL.Push(s_calcStaticCalli);
        IL.Emit.Tail();
        IL.Emit.Calli(new StandAloneMethodSig(CallingConventions.Standard, typeof(int), typeof(int), typeof(int)));
        return IL.Return<int>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string EmptyCalli()
    {
        // Force helper-based tailcall out of this function by stackallocing
        Span<int> values = stackalloc int[Environment.TickCount < 0 ? 30 : 40];

        IL.Push(s_emptyCalliOther);
        IL.Emit.Tail();
        IL.Emit.Calli(new StandAloneMethodSig(CallingConventions.Standard, typeof(string)));
        return IL.Return<string>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string ValueTypeInstanceMethodCalli()
    {
        // Force helper-based tailcall out of this function by stackallocing
        Span<int> values = stackalloc int[Environment.TickCount < 0 ? 30 : 40];

        S16 s16 = new S16();

        IL.Push(ref s16);
        IL.Push(s_instanceMethodOnValueType);
        IL.Emit.Tail();
        IL.Emit.Calli(new StandAloneMethodSig(CallingConventions.Standard | CallingConventions.HasThis, typeof(string)));
        return IL.Return<string>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string ValueTypeExplicitThisInstanceMethodCalli()
    {
        // Force helper-based tailcall out of this function by stackallocing
        Span<int> values = stackalloc int[Environment.TickCount < 0 ? 30 : 40];

        S16 s16 = new S16();

        IL.Push(ref s16);
        IL.Push(s_instanceMethodOnValueType);
        IL.Emit.Tail();
        IL.Emit.Calli(new StandAloneMethodSig(CallingConventions.Standard | CallingConventions.HasThis | CallingConventions.ExplicitThis,
                      typeof(string), typeof(S16).MakeByRefType()));
        return IL.Return<string>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string EmptyCalliOther()
    {
        return "Empty calli";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S32 CalcStaticCalliRetbuf(int x, int acc)
    {
        if (x == 0)
            return new S32 { A = acc, B = acc, C = acc, D = acc, };

        S32 s = default;
        Calc(ref x, ref s, ref acc);

        IL.Push(x);
        IL.Push(s);
        IL.Push(acc);
        IL.Push(s_calcStaticCalliRetbufOther);
        IL.Emit.Tail();
        IL.Emit.Calli(new StandAloneMethodSig(CallingConventions.Standard, typeof(S32), typeof(int), typeof(S32), typeof(int)));
        return IL.Return<S32>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S32 CalcStaticCalliRetbufOther(int x, S32 large, int acc)
    {
        Calc(ref x, ref large, ref acc);

        IL.Push(x);
        IL.Push(acc);
        IL.Push(s_calcStaticCalliRetbuf);
        IL.Emit.Tail();
        IL.Emit.Calli(new StandAloneMethodSig(CallingConventions.Standard, typeof(S32), typeof(int), typeof(int)));
        return IL.Return<S32>();
    }

    internal static int s_result;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CalcStaticVoid(int x, int acc)
    {
        if (x == 0)
        {
            s_result = acc;
            return;
        }

        S32 s = default;
        Calc(ref x, ref s, ref acc);

        IL.Push(x);
        IL.Push(s);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(CalcStaticVoidOther)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CalcStaticVoidOther(int x, S32 large, int acc)
    {
        Calc(ref x, ref large, ref acc);

        IL.Push(x);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(CalcStaticVoid)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CountUpHeap(int left, HeapInt counter)
    {
        if (left == 0)
            return counter.Value;

        IL.Push(left - 1);
        IL.Push(new S32());
        IL.Push(new HeapInt(counter.Value + 1));
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(CountUpHeapOther)));
        return IL.Return<int>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CountUpHeapOther(int left, S32 s, HeapInt counter)
    {
        if (left == 0)
            return counter.Value;

        IL.Push(left - 1);
        IL.Push(new HeapInt(counter.Value + 1));
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(CountUpHeap)));
        return IL.Return<int>();
    }

    private static void CountUpRef(int left, ref int counter)
    {
        if (left == 0)
            return;

        counter++;
        IL.Push(left - 1);
        IL.Push(new S32());
        IL.Push(ref counter);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(CountUpRefOther)));
    }

    private static void CountUpRefOther(int left, S32 s, ref int counter)
    {
        if (left == 0)
            return;

        counter++;
        IL.Push(left - 1);
        IL.Push(ref counter);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(CountUpRef)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GenName1Forward<T>(T x)
    {
        S32 s = default;
        IL.Push(s);
        IL.Push(x);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(GenName1)).MakeGenericMethod(typeof(T)));
        return IL.Return<string>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GenName1<T>(S32 s, T x)
        => $"{typeof(T).FullName} {x}";

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GenName2ForwardBoth<T1, T2>(T1 x, T2 y)
    {
        S32 s = default;
        IL.Push(s);
        IL.Push(x);
        IL.Push(y);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(GenName2)).MakeGenericMethod(typeof(T1), typeof(T2)));
        return IL.Return<string>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GenName2ForwardOne<T>(T x, string y)
    {
        S32 s = default;
        IL.Push(s);
        IL.Push(x);
        IL.Push(y);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(GenName2)).MakeGenericMethod(typeof(T), typeof(string)));
        return IL.Return<string>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GenName2ForwardNone(object x, string y)
    {
        S32 s = default;
        IL.Push(s);
        IL.Push(x);
        IL.Push(y);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(GenName2)).MakeGenericMethod(typeof(object), typeof(string)));
        return IL.Return<string>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GenName2<T1, T2>(S32 s, T1 a, T2 b)
        => $"{typeof(T1).FullName} {typeof(T2).FullName} {a} {b}";

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GenInterfaceForwardF<T1, T2, T3, T4>(T1 a, T2 b, T3 c, T4 d, IGenInterface<T1, T2> igen)
    {
        IL.Push(igen);
        IL.Push(new S32());
        IL.Push(a);
        IL.Push(b);
        IL.Push(c);
        IL.Push(d);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(IGenInterface<T1, T2>), nameof(IGenInterface<T1, T2>.F)).MakeGenericMethod(typeof(T3), typeof(T4)));
        return IL.Return<string>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GenInterfaceForwardG<T1, T2>(T1 a, T2 b, IGenInterface<T1, T2> igen)
    {
        IL.Push(igen);
        IL.Push(new S32());
        IL.Push(a);
        IL.Push(b);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(IGenInterface<T1, T2>), nameof(IGenInterface<T1, T2>.G)));
        return IL.Return<string>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GenInterfaceForwardNone(string a, object b, int c, object d, IGenInterface<string, object> igen)
    {
        IL.Push(igen);
        IL.Push(new S32());
        IL.Push(a);
        IL.Push(b);
        IL.Push(c);
        IL.Push(d);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(IGenInterface<string, object>), nameof(IGenInterface<string, object>.F)).MakeGenericMethod(typeof(int), typeof(object)));
        return IL.Return<string>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GenInterfaceForward2(string a, object b, IGenInterface<string, object> igen)
    {
        IL.Push(igen);
        IL.Push(new S32());
        IL.Push(a);
        IL.Push(b);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(IGenInterface<string, object>), nameof(IGenInterface<string, object>.G)));
        return IL.Return<string>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GenAbstractFString(GenAbstract<string> ga)
    {
        IL.Push(ga);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(GenAbstract<string>), nameof(GenAbstract<string>.F)).MakeGenericMethod(typeof(object)));
        return IL.Return<string>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GenAbstractGString(GenAbstract<string> ga)
    {
        IL.Push(ga);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(GenAbstract<string>), nameof(GenAbstract<string>.G)));
        return IL.Return<string>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GenAbstractFInt(GenAbstract<int> ga)
    {
        IL.Push(ga);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(GenAbstract<int>), nameof(GenAbstract<int>.F)).MakeGenericMethod(typeof(object)));
        return IL.Return<string>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GenAbstractGInt(GenAbstract<int> ga)
    {
        IL.Push(ga);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(GenAbstract<int>), nameof(GenAbstract<int>.G)));
        return IL.Return<string>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int InstantiatingStub1<T>(int a, int r, T c, Span<int> d)
    {
        IL.Push(c);
        IL.Push(c);
        IL.Push(c);
        IL.Push(c);
        IL.Push(c);
        IL.Push(c);
        IL.Push(c);
        IL.Push(c);
        IL.Push(a);
        IL.Push(r);
        IL.Emit.Ldarg(nameof(d));
        IL.Push(r + d[99]);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Program), nameof(InstantiatingStub1Other)).MakeGenericMethod(typeof(T)));
        return IL.Return<int>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int InstantiatingStub1Other<T>(T c0, T c1, T c2, T c3, T c4, T c5, T c6, T c7, int a, int r, Span<int> d, int result)
    {
        if (a == d.Length) return result;
        else
        {
            IL.Push(a + 1);
            IL.Push(result);
            IL.Push(c0);
            IL.Emit.Ldarg(nameof(d));
            IL.Emit.Tail();
            IL.Emit.Call(new MethodRef(typeof(Program), nameof(InstantiatingStub1)).MakeGenericMethod(typeof(T)));
            return IL.Return<int>();
        }
    }

    class GenericInstance<T>
    {
        private GenericInstanceFactory factory;

        public GenericInstance(GenericInstanceFactory factory)
        {
            this.factory = factory;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual int NumberOfInstances()
        {
            return factory.counter;
        }
    }

    class GenericInstanceFactory
    {
        public int counter = 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public GenericInstance<string> CreateInstance()
        {
            counter++;
            return new GenericInstance<string>(this);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int VirtCallThisHasSideEffects()
    {
        IL.Push(1000);
        IL.Emit.Localloc();
        IL.Emit.Pop();
        GenericInstanceFactory fact = new GenericInstanceFactory();
        IL.Push(fact);
        IL.Emit.Call(new MethodRef(typeof(GenericInstanceFactory), nameof(GenericInstanceFactory.CreateInstance)));
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(GenericInstance<string>), nameof(GenericInstance<string>.NumberOfInstances)));
        return IL.Return<int>();
    }
}

class Instance
{
    private static readonly IntPtr s_calcInstanceCalli;
    private static readonly IntPtr s_calcInstanceCalliOther;
    private static readonly IntPtr s_calcInstanceCalliRetbuf;
    private static readonly IntPtr s_calcInstanceCalliRetbufOther;

    static Instance()
    {
        IL.Emit.Ldftn(new MethodRef(typeof(Instance), nameof(CalcInstanceCalli)));
        IL.Pop(out IntPtr calcInstanceCalli);
        IL.Emit.Ldftn(new MethodRef(typeof(Instance), nameof(CalcInstanceCalliOther)));
        IL.Pop(out IntPtr calcInstanceCalliOther);
        IL.Emit.Ldftn(new MethodRef(typeof(Instance), nameof(CalcInstanceCalliRetbuf)));
        IL.Pop(out IntPtr calcInstanceCalliRetbuf);
        IL.Emit.Ldftn(new MethodRef(typeof(Instance), nameof(CalcInstanceCalliRetbufOther)));
        IL.Pop(out IntPtr calcInstanceCalliRetbufOther);

        s_calcInstanceCalli = calcInstanceCalli;
        s_calcInstanceCalliOther = calcInstanceCalliOther;
        s_calcInstanceCalliRetbuf = calcInstanceCalliRetbuf;
        s_calcInstanceCalliRetbufOther = calcInstanceCalliRetbufOther;
    }

    private int _x;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int CalcInstance(int x, int acc)
    {
        if (x != -1234567)
            _x = x;

        if (_x == 0)
            return acc;

        S32 s = default;
        Program.Calc(ref _x, ref s, ref acc);

        IL.Push(this);
        IL.Push(-1234567);
        IL.Push(s);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(Instance), nameof(CalcInstanceOther)));
        return IL.Return<int>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int CalcInstanceOther(int x, S32 large, int acc)
    {
        Program.Calc(ref _x, ref large, ref acc);

        IL.Push(this);
        IL.Push(-1234567);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(Instance), nameof(CalcInstance)));
        return IL.Return<int>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public S32 CalcInstanceRetbuf(int x, int acc)
    {
        if (x != -1234567)
            _x = x;

        if (_x == 0)
            return new S32 { A = acc, B = acc, C = acc, D = acc, };

        S32 s = default;
        Program.Calc(ref _x, ref s, ref acc);

        IL.Push(this);
        IL.Push(-1234567);
        IL.Push(s);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(Instance), nameof(CalcInstanceRetbufOther)));
        return IL.Return<S32>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public S32 CalcInstanceRetbufOther(int x, S32 large, int acc)
    {
        Program.Calc(ref _x, ref large, ref acc);

        IL.Push(this);
        IL.Push(-1234567);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(Instance), nameof(CalcInstanceRetbuf)));
        return IL.Return<S32>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int CalcInstanceCalli(int x, int acc)
    {
        if (x != -1234567)
            _x = x;

        if (_x == 0)
            return acc;

        S32 s = default;
        Program.Calc(ref _x, ref s, ref acc);

        IL.Push(this);
        IL.Push(-1234567);
        IL.Push(s);
        IL.Push(acc);
        IL.Push(s_calcInstanceCalliOther);
        IL.Emit.Tail();
        IL.Emit.Calli(new StandAloneMethodSig(CallingConventions.Standard | CallingConventions.HasThis, typeof(int), typeof(int), typeof(S32), typeof(int)));
        return IL.Return<int>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int CalcInstanceCalliOther(int x, S32 large, int acc)
    {
        Program.Calc(ref _x, ref large, ref acc);

        IL.Push(this);
        IL.Push(-1234567);
        IL.Push(acc);
        IL.Push(s_calcInstanceCalli);
        IL.Emit.Tail();
        IL.Emit.Calli(new StandAloneMethodSig(CallingConventions.Standard | CallingConventions.HasThis, typeof(int), typeof(int), typeof(int)));
        return IL.Return<int>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public S32 CalcInstanceCalliRetbuf(int x, int acc)
    {
        if (x != -1234567)
            _x = x;

        if (_x == 0)
            return new S32 { A = acc, B = acc, C = acc, D = acc, };

        S32 s = default;
        Program.Calc(ref _x, ref s, ref acc);

        IL.Push(this);
        IL.Push(-1234567);
        IL.Push(s);
        IL.Push(acc);
        IL.Push(s_calcInstanceCalliRetbufOther);
        IL.Emit.Tail();
        IL.Emit.Calli(new StandAloneMethodSig(CallingConventions.Standard | CallingConventions.HasThis, typeof(S32), typeof(int), typeof(S32), typeof(int)));
        return IL.Return<S32>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public S32 CalcInstanceCalliRetbufOther(int x, S32 large, int acc)
    {
        Program.Calc(ref _x, ref large, ref acc);

        IL.Push(this);
        IL.Push(-1234567);
        IL.Push(acc);
        IL.Push(s_calcInstanceCalliRetbuf);
        IL.Emit.Tail();
        IL.Emit.Calli(new StandAloneMethodSig(CallingConventions.Standard | CallingConventions.HasThis, typeof(S32), typeof(int), typeof(int)));
        return IL.Return<S32>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string GC(object a, int b, object c, object d, string e, string f, object g, int h, ref int interior)
    {
        IL.Push(this);

        IL.Push(a);

        S32 s = new S32();
        IL.Push(s);

        SGC2 sgc = new SGC2
        {
            A = b,
            B =
            {
                A = c,
                B = d,
                C = e,
                D = f,
            },
            C = g,
            D = h
        };
        IL.Push(sgc);
        IL.Push(ref interior);

        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(Instance), nameof(GCOther)));
        return IL.Return<string>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private string GCOther(object a, S32 s, SGC2 gc, ref int interior)
        => $"{a} {gc.A} {gc.B.A} {gc.B.B} {gc.B.C} {gc.B.D} {gc.C} {gc.D} {interior}";
}

struct InstanceValueType
{
    public int Count;

    public void CountUp(int left)
    {
        if (left == 0)
            return;

        Count++;

        IL.Push(ref this);
        IL.Push(left - 1);
        IL.Push(new S32());
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(InstanceValueType), nameof(CountUpOther)));
    }

    private void CountUpOther(int left, S32 s)
    {
        if (left == 0)
            return;

        Count++;
        IL.Push(ref this);
        IL.Push(left - 1);
        IL.Emit.Tail();
        IL.Emit.Call(new MethodRef(typeof(InstanceValueType), nameof(CountUp)));
    }
}

abstract class BaseClass
{
    public abstract int CalcAbstract(int x, int acc);
    public abstract int CalcAbstractOther(int x, S32 large, int acc);

    public abstract S32 CalcAbstractRetbuf(int x, int acc);
    public abstract S32 CalcAbstractRetbufOther(int x, S32 large, int acc);
}
class ClassImpl : BaseClass
{
    private int _x;
    public BaseClass Other { get; set; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int CalcAbstract(int x, int acc)
    {
        if (x != -1234567)
            _x = x;

        if (_x == 0)
            return acc;

        S32 s = default;
        Program.Calc(ref _x, ref s, ref acc);

        IL.Push(Other);
        IL.Push(-1234567);
        IL.Push(s);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(ClassImpl), nameof(CalcAbstractOther)));
        return IL.Return<int>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int CalcAbstractOther(int x, S32 large, int acc)
    {
        Program.Calc(ref _x, ref large, ref acc);

        IL.Push(Other);
        IL.Push(-1234567);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(ClassImpl), nameof(CalcAbstract)));
        return IL.Return<int>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override S32 CalcAbstractRetbuf(int x, int acc)
    {
        if (x != -1234567)
            _x = x;

        if (_x == 0)
            return new S32 { A = acc, B = acc, C = acc, D = acc, };

        S32 s = default;
        Program.Calc(ref _x, ref s, ref acc);

        IL.Push(Other);
        IL.Push(-1234567);
        IL.Push(s);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(ClassImpl), nameof(CalcAbstractRetbufOther)));
        return IL.Return<S32>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override S32 CalcAbstractRetbufOther(int x, S32 large, int acc)
    {
        Program.Calc(ref _x, ref large, ref acc);

        IL.Push(Other);
        IL.Push(-1234567);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(ClassImpl), nameof(CalcAbstractRetbuf)));
        return IL.Return<S32>();
    }
}

interface IInterface
{
    int CalcInterface(int x, int acc);
    int CalcInterfaceOther(int x, S32 large, int acc);

    S32 CalcInterfaceRetbuf(int x, int acc);
    S32 CalcInterfaceRetbufOther(int x, S32 large, int acc);
}

class InterfaceImpl : IInterface
{
    private int _x;
    public IInterface Other { get; set; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int CalcInterface(int x, int acc)
    {
        if (x != -1234567)
            _x = x;

        if (_x == 0)
            return acc;

        S32 s = default;
        Program.Calc(ref _x, ref s, ref acc);

        IL.Push(Other);
        IL.Push(-1234567);
        IL.Push(s);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(IInterface), nameof(CalcInterfaceOther)));
        return IL.Return<int>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int CalcInterfaceOther(int x, S32 large, int acc)
    {
        Program.Calc(ref _x, ref large, ref acc);

        IL.Push(Other);
        IL.Push(-1234567);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(IInterface), nameof(CalcInterface)));
        return IL.Return<int>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public S32 CalcInterfaceRetbuf(int x, int acc)
    {
        if (x != -1234567)
            _x = x;

        if (_x == 0)
            return new S32 { A = acc, B = acc, C = acc, D = acc, };

        S32 s = default;
        Program.Calc(ref _x, ref s, ref acc);

        IL.Push(Other);
        IL.Push(-1234567);
        IL.Push(s);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(IInterface), nameof(CalcInterfaceRetbufOther)));
        return IL.Return<S32>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public S32 CalcInterfaceRetbufOther(int x, S32 large, int acc)
    {
        Program.Calc(ref _x, ref large, ref acc);

        IL.Push(Other);
        IL.Push(-1234567);
        IL.Push(acc);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(IInterface), nameof(CalcInterfaceRetbuf)));
        return IL.Return<S32>();
    }
}

class GenInstance<T1, T2>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public string NonVirtForward<T3, T4>(T1 a, T2 b, T3 c, T4 d)
    {
        IL.Push(this);
        IL.Push(new S32());
        IL.Push(a);
        IL.Push(b);
        IL.Push(c);
        IL.Push(d);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(GenInstance<T1, T2>), nameof(NonVirt)).MakeGenericMethod(typeof(T3), typeof(T4)));
        return IL.Return<string>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string VirtForward<T3, T4>(T1 a, T2 b, T3 c, T4 d)
    {
        IL.Push(this);
        IL.Push(new S32());
        IL.Push(a);
        IL.Push(b);
        IL.Push(c);
        IL.Push(d);
        IL.Emit.Tail();
        IL.Emit.Callvirt(new MethodRef(typeof(GenInstance<T1, T2>), nameof(Virt)).MakeGenericMethod(typeof(T3), typeof(T4)));
        return IL.Return<string>();
    }

    public string NonVirt<T3, T4>(S32 s, T1 a, T2 b, T3 c, T4 d)
        => $"{typeof(T1).FullName} {typeof(T2).FullName} {typeof(T3).FullName} {typeof(T4).FullName} {a} {b} {c} {d}";

    public virtual string Virt<T3, T4>(S32 s, T1 a, T2 b, T3 c, T4 d)
        => $"{typeof(T1).FullName} {typeof(T2).FullName} {typeof(T3).FullName} {typeof(T4).FullName} {a} {b} {c} {d}";
}

interface IGenInterface<T1, T2>
{
    string F<T3, T4>(S32 s, T1 a, T2 b, T3 c, T4 d);
    string G(S32 s, T1 a, T2 b);
}

class GenInterfaceImpl<T1, T2> : IGenInterface<T1, T2>
{
    public string F<T3, T4>(S32 s, T1 a, T2 b, T3 c, T4 d)
        => $"{typeof(T1).FullName} {typeof(T2).FullName} {typeof(T3).FullName} {typeof(T4).FullName} {a} {b} {c} {d}";

    public string G(S32 s, T1 a, T2 b)
        => $"{typeof(T1).FullName} {typeof(T2).FullName} {a} {b}";
}

abstract class GenAbstract<T1>
{
    public abstract string F<T2>();
    public abstract string G();
}

class GenAbstractImpl<T1> : GenAbstract<T1>
{
    public override string F<T2>()
        => $"{typeof(T1).FullName} {typeof(T2).FullName}";

    public override string G()
        => $"{typeof(T1).FullName}";
}
