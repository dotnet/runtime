// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Victim's null check throws before its GC ref local is stored, so a GC while the
// exception is dispatched must not see DirtyStack's garbage in that local.
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
        for (int i = 0; i < 10; i++)
        {
            DirtyStack();

            bool caught = false;
            try
            {
                Victim(null);
            }
            catch (NullReferenceException) when (Collect())
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
    private static object Victim(Holder holder)
    {
        object value = holder.Value;
        KeepAlive(value);
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void KeepAlive(object value)
    {
    }

    private static bool Collect()
    {
        GC.Collect();
        return true;
    }
}
