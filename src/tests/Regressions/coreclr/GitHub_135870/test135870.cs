// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Xunit;

public class Test135870
{
    [Fact]
    public static void TestEntryPoint()
    {
        // Keep the heap alive so subsequent methods can reuse freed code blocks.
        Func<int> keepAlive = CreateDynamicMethod();
        for (int iteration = 0; iteration < 3; iteration++)
        {
            WeakReference method = CreateAndInvoke();

            // The resolver finalizer creates a scout that destroys the native code after another GC.
            for (int collection = 0; collection < 10; collection++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Assert.False(method.IsAlive);
        }

        GC.KeepAlive(keepAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndInvoke()
    {
        Func<int> method = CreateDynamicMethod();
        Assert.Equal(42, method());
        return new WeakReference(method, trackResurrection: true);
    }

    private static Func<int> CreateDynamicMethod()
    {
        var method = new DynamicMethod("Return42", typeof(int), Type.EmptyTypes);
        ILGenerator il = method.GetILGenerator();
        il.Emit(OpCodes.Ldc_I4, 42);
        il.Emit(OpCodes.Ret);
        return method.CreateDelegate<Func<int>>();
    }
}
