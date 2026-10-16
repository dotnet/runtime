// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Each victim throws before its GC ref local is first stored, so a GC while the throw
// helper runs or the exception is dispatched must not see DirtyStack's garbage in it.
public class Runtime_135966
{
    private class Holder
    {
        public object Value = new object();
    }

    [InlineArray(32)]
    private struct Garbage
    {
        private nint _element;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        int[] array = new int[1];

        for (int i = 0; i < 10; i++)
        {
            DirtyStack();

            bool caught = false;
            try
            {
                NullCheckVictim(null);
            }
            catch (NullReferenceException) when (Collect())
            {
                caught = true;
            }

            Assert.True(caught);

            DirtyStack();

            caught = false;
            try
            {
                BoundsCheckVictim(array, 5);
            }
            catch (IndexOutOfRangeException) when (Collect())
            {
                caught = true;
            }

            Assert.True(caught);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DirtyStack()
    {
        Garbage garbage = default;
        Span<nint> span = garbage;
        span.Fill(0x1000);
        Consume(span);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(Span<nint> span)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object NullCheckVictim(Holder holder)
    {
        object value = holder.Value;
        KeepAlive(value);
        return value;
    }

    // The local is address-exposed, so it is an untracked GC slot on every target.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object BoundsCheckVictim(int[] array, int index)
    {
        int element = array[index];
        object value = array;
        KeepAlive(ref value);
        return element == 0 ? value : null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void KeepAlive(object value)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void KeepAlive(ref object value)
    {
    }

    private static bool Collect()
    {
        GC.Collect();
        return true;
    }
}
