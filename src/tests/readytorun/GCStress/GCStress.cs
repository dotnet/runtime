// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class GCStress
{
    private static int s_finallyValue;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void TestEntryPoint(bool generic)
    {
        object value = new object();
        GC.CollectionCount(0);
        int collections;
        object result = generic ? R2RGCStress<object>(value, out collections) : R2RGCStress(value, out collections);

        Console.WriteLine($"R2R GC stress (generic: {generic}): {collections} collections");
        Assert.Same(value, result);
        Assert.True(collections > 0, "R2R instruction stress did not collect.");

        if (generic)
        {
            string other = "shared generic code";
            Assert.Same(other, R2RGCStress<string>(other, out _));
        }
    }

    [Fact]
    public static void TestFinally()
    {
        object value = new object();
        Assert.Same(value, R2RGCStressFinally(value));
        Assert.Equal(42, s_finallyValue);
        Console.WriteLine("R2R GC stress finally: passed");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object R2RGCStressFinally(object value)
    {
        try
        {
            Poll();
            return value;
        }
        finally
        {
            // Nested handlers prevent finally cloning from eliminating the callfinally.
            try
            {
                Poll();
            }
            finally
            {
                // Leave a non-reference in EAX, unlike the parent method's return value.
                FinallyReturnNonGcValue();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int FinallyReturnNonGcValue()
    {
        s_finallyValue = 42;
        return 42;
    }

    public enum DispatchKind
    {
        Class,
        Interface,
        OpenDelegate
    }

    [Theory]
    [InlineData(DispatchKind.Class)]
    [InlineData(DispatchKind.Interface)]
    [InlineData(DispatchKind.OpenDelegate)]
    public static void TestVirtualCalls(DispatchKind kind)
    {
        VirtualCallTarget target = new DerivedVirtualCallTarget();
        Func<VirtualCallTarget, int[]> callback = typeof(VirtualCallTarget)
            .GetMethod(nameof(VirtualCallTarget.GetValue))
            .CreateDelegate<Func<VirtualCallTarget, int[]>>();
        WarmupVirtualCalls(target, target, callback);

        for (int i = 0; i < 10; i++)
        {
            int[] result = R2RGCStressVirtualCall(target, target, callback, kind);
            Assert.Equal(42, result[0]);
        }

        Assert.Throws<NullReferenceException>(() => R2RGCStressVirtualCall(null, null, callback, kind));
        Console.WriteLine($"R2R GC stress virtual calls ({kind}): passed");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WarmupVirtualCalls(
        VirtualCallTarget target, IVirtualCallTarget interfaceTarget, Func<VirtualCallTarget, int[]> callback)
    {
        // Resolve the shared R2R imports before the stressed caller consumes its call breakpoints.
        target.GetValue();
        interfaceTarget.GetValue();
        callback(target);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int[] R2RGCStressVirtualCall(
        VirtualCallTarget target, IVirtualCallTarget interfaceTarget,
        Func<VirtualCallTarget, int[]> callback, DispatchKind kind)
    {
        int[] result = kind switch
        {
            DispatchKind.Class => target.GetValue(),
            DispatchKind.Interface => interfaceTarget.GetValue(),
            DispatchKind.OpenDelegate => callback(target),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        Poll();
        return result;
    }

    private interface IVirtualCallTarget
    {
        int[] GetValue();
    }

    private class VirtualCallTarget : IVirtualCallTarget
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual int[] GetValue() => new[] { -1 };
    }

    private class DerivedVirtualCallTarget : VirtualCallTarget
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int[] GetValue() => new[] { 42 };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object R2RGCStress(object value, out int collections)
    {
        Poll();
        int before = GC.CollectionCount(0);
        for (int i = 0; i < 100; i++)
        {
            Poll();
        }

        collections = GC.CollectionCount(0) - before;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T R2RGCStress<T>(T value, out int collections) where T : class
    {
        Poll();
        int before = GC.CollectionCount(0);
        for (int i = 0; i < 100; i++)
        {
            Poll();
        }

        collections = GC.CollectionCount(0) - before;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Poll() { }
}
