// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;

internal static class Program
{
    private static int Main()
    {
        DynamicMethod dm = new DynamicMethod("TailCaller", typeof(void), new Type[] { typeof(Span<int>) });
        ILGenerator il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Tailcall);
        il.Emit(OpCodes.Call, typeof(Program).GetMethod("TailCallee", BindingFlags.Static | BindingFlags.NonPublic));
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
            return 1; // fail
        }

        return 100; // pass
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
