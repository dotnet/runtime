// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

/// <summary>
/// Exercises the MetaSig (non-GCRefMap) path by creating and invoking
/// DynamicMethod (LCG) methods. These methods use StoredSigMethodDesc
/// and don't have pre-computed GCRefMaps, forcing PromoteCallerStack
/// to walk the signature via MetaSig.
///
/// Scenarios:
/// - Simple object parameter (GcTypeKind.Ref)
/// - Multiple object parameters
/// - Byref parameter (GcTypeKind.Interior)
/// - Mixed ref and primitive parameters
/// - Method with 'this' (instance delegate)
/// - Method returning object (tests return type parsing)
/// </summary>
internal static class Program
{
    static int Main()
    {
        for (int i = 0; i < 50; i++)
        {
            SimpleObjectParam();
            MultipleObjectParams();
            MixedParams();
            ObjectReturn();
            KeepAliveInDynamic();
        }
        return 100;
    }

    // ===== Scenario 1: Single object parameter =====
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SimpleObjectParam()
    {
        // Create: void DynMethod(object o)
        DynamicMethod dm = new("DynSimple", typeof(void), new[] { typeof(object) });
        ILGenerator il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(GC).GetMethod(nameof(GC.KeepAlive))!);
        il.Emit(OpCodes.Ret);

        Action<object> del = dm.CreateDelegate<Action<object>>();
        object live = new object();
        del(live);
        GC.KeepAlive(live);
    }

    // ===== Scenario 2: Multiple object parameters =====
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void MultipleObjectParams()
    {
        // Create: void DynMulti(object a, string b, int[] c)
        DynamicMethod dm = new("DynMulti", typeof(void),
            new[] { typeof(object), typeof(string), typeof(int[]) });
        ILGenerator il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(GC).GetMethod(nameof(GC.KeepAlive))!);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Call, typeof(GC).GetMethod(nameof(GC.KeepAlive))!);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Call, typeof(GC).GetMethod(nameof(GC.KeepAlive))!);
        il.Emit(OpCodes.Ret);

        var del = dm.CreateDelegate<Action<object, string, int[]>>();
        object a = new object();
        string b = "hello";
        int[] c = new[] { 1, 2, 3 };
        del(a, b, c);
        GC.KeepAlive(a);
        GC.KeepAlive(b);
        GC.KeepAlive(c);
    }

    // ===== Scenario 3: Mixed ref and primitive parameters =====
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void MixedParams()
    {
        // Create: void DynMixed(object o, int x, string s, long y)
        DynamicMethod dm = new("DynMixed", typeof(void),
            new[] { typeof(object), typeof(int), typeof(string), typeof(long) });
        ILGenerator il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(GC).GetMethod(nameof(GC.KeepAlive))!);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Call, typeof(GC).GetMethod(nameof(GC.KeepAlive))!);
        il.Emit(OpCodes.Ret);

        var del = dm.CreateDelegate<Action<object, int, string, long>>();
        object o = new object();
        string s = "world";
        del(o, 42, s, 999L);
        GC.KeepAlive(o);
        GC.KeepAlive(s);
    }

    // ===== Scenario 4: Object return type =====
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ObjectReturn()
    {
        // Create: object DynReturn(object o)
        DynamicMethod dm = new("DynReturn", typeof(object), new[] { typeof(object) });
        ILGenerator il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ret);

        var del = dm.CreateDelegate<Func<object, object>>();
        object input = new object();
        object result = del(input);
        GC.KeepAlive(result);
        GC.KeepAlive(input);
    }

    // ===== Scenario 5: Multiple allocations inside dynamic method =====
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void KeepAliveInDynamic()
    {
        // Create: void DynAlloc(object a, object b, object c, object d)
        DynamicMethod dm = new("DynAlloc", typeof(void),
            new[] { typeof(object), typeof(object), typeof(object), typeof(object) });
        ILGenerator il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(GC).GetMethod(nameof(GC.KeepAlive))!);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Call, typeof(GC).GetMethod(nameof(GC.KeepAlive))!);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Call, typeof(GC).GetMethod(nameof(GC.KeepAlive))!);
        il.Emit(OpCodes.Ldarg_3);
        il.Emit(OpCodes.Call, typeof(GC).GetMethod(nameof(GC.KeepAlive))!);
        il.Emit(OpCodes.Ret);

        var del = dm.CreateDelegate<Action<object, object, object, object>>();
        object a = new object();
        object b = "str";
        object c = new int[] { 1 };
        object d = new byte[16];
        del(a, b, c, d);
        GC.KeepAlive(a);
        GC.KeepAlive(b);
        GC.KeepAlive(c);
        GC.KeepAlive(d);
    }
}
