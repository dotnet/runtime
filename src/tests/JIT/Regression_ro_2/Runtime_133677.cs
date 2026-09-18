// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Emit;
using TestLibrary;
using Xunit;

public class Runtime_133677
{
    [ConditionalFact(typeof(Utilities), nameof(Utilities.IsReflectionEmitSupported))]
    public static void TestEntryPoint()
    {
        // Emit the cycle directly so the C# compiler cannot collapse it to a self-loop.
        var method = new DynamicMethod("SwitchCycle", typeof(int), new[] { typeof(int) });
        ILGenerator il = method.GetILGenerator();
        Label a = il.DefineLabel();
        Label b = il.DefineLabel();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Switch, new[] { a, b, a });
        il.Emit(OpCodes.Ldc_I4_7);
        il.Emit(OpCodes.Ret);

        il.MarkLabel(a);
        il.Emit(OpCodes.Br, b);
        il.MarkLabel(b);
        il.Emit(OpCodes.Br, a);

        Func<int, int> candidate = method.CreateDelegate<Func<int, int>>();
        Assert.Equal(7, candidate(99));
    }
}
