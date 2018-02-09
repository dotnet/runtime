// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

internal static class Program
{
    private static int Main()
    {
        bool allPassed = true;
        bool passed;

        Console.Write("    SpanTest: ");
        passed = SpanTest.Run();
        Console.WriteLine(passed ? "pass" : "fail");
        allPassed &= passed;

        Console.Write("    ByRefLikeTest: ");
        passed = ByRefLikeTest.Run();
        Console.WriteLine(passed ? "pass" : "fail");
        allPassed &= passed;

        return allPassed ? 100 : 1;
    }
}

internal static class SpanTest
{
    public static bool Run()
    {
        DynamicMethod dm = new DynamicMethod("TailCaller", typeof(void), new Type[] { typeof(Span<int>) });
        ILGenerator il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Tailcall);
        il.Emit(OpCodes.Call, typeof(SpanTest).GetMethod("TailCallee", BindingFlags.Static | BindingFlags.NonPublic));
        il.Emit(OpCodes.Ret);

        var tailCaller = (ActionOfSpanOfInt)dm.CreateDelegate(typeof(ActionOfSpanOfInt));

        try
        {
            for (int i = 0; i < 1000; ++i)
            {
                GC.KeepAlive(new object());
                tailCaller(new Span<int>(new int[] { 42 }));
                GC.KeepAlive(new object());
            }
        }
        catch (ArgumentException)
        {
            return false; // fail
        }
        return true;
    }

    private static void TailCallee(Span<int> a, Span<int> b, Span<int> c, Span<int> d, Span<int> e)
    {
        GC.Collect();
        for (int i = 0; i < 10000; i++)
            GC.KeepAlive(new object());
        if (a[0] != 42 || b[0] != 42 || c[0] != 42 || d[0] != 42 || e[0] != 42)
            throw new ArgumentException();
    }

    private delegate void ActionOfSpanOfInt(Span<int> x);
}

internal static class ByRefLikeTest
{
    public static bool Run()
    {
        DynamicMethod dm = new DynamicMethod("TailCaller", typeof(void), new Type[] { typeof(TestByRefLike) });
        ILGenerator il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Tailcall);
        il.Emit(OpCodes.Call, typeof(ByRefLikeTest).GetMethod("TailCallee", BindingFlags.Static | BindingFlags.NonPublic));
        il.Emit(OpCodes.Ret);

        var tailCaller = (ActionOfTestByRefLike)dm.CreateDelegate(typeof(ActionOfTestByRefLike));

        try
        {
            for (int i = 0; i < 1000; ++i)
            {
                GC.KeepAlive(new object());
                tailCaller(new TestByRefLike(new int[] { 42 }));
                GC.KeepAlive(new object());
            }
        }
        catch (ArgumentException)
        {
            return false; // fail
        }
        return true;
    }

    private static void TailCallee(TestByRefLike a, TestByRefLike b, TestByRefLike c, TestByRefLike d, TestByRefLike e)
    {
        GC.Collect();
        for (int i = 0; i < 10000; i++)
            GC.KeepAlive(new object());
        if (a.span[0] != 42 || b.span[0] != 42 || c.span[0] != 42 || d.span[0] != 42 || e.span[0] != 42 ||
            a.span2[0] != 42 || b.span2[0] != 42 || c.span2[0] != 42 || d.span2[0] != 42 || e.span2[0] != 42)
        {
            throw new ArgumentException();
        }
    }

    private delegate void ActionOfTestByRefLike(TestByRefLike x);

    [StructLayout(LayoutKind.Explicit)]
    private ref struct TestByRefLike
    {
        [FieldOffset(8 * 0)]
        private object obj;
        [FieldOffset(8 * 0)]
        private object obj2;
        [FieldOffset(8 * 1)]
        public Span<int> span;
        [FieldOffset(8 * 1)]
        public Span<int> span2;
        [FieldOffset(8 * 3)]
        private object obj3;

        public TestByRefLike(int[] values)
        {
            obj = null;
            if (obj != null)
                obj = null;
            obj2 = null;
            if (obj2 != null)
                obj2 = null;
            obj3 = null;
            if (obj3 != null)
                obj3 = null;
            span = new Span<int>(values);
            span2 = span;
        }
    }
}
