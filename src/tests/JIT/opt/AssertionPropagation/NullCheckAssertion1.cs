// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Unit test for null check assertion.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public class Sample3
{
    private sealed class Box
    {
        public int Value = 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ArrayRange(int[] array, int count)
    {
        // X64: cmp {{.*}}dword ptr
        if (array == null)
            return false;
        return !(0u > (uint)array.Length || (uint)count > (uint)array.Length);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool UnusedArrayLength(int[] array)
    {
        // ARM64: ldr{{s?}}b wzr,
        return 0u > (uint)array.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool UnusedStringLength(string text) => 0u > (uint)text.Length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool UnusedField(Box box) => 0u > (uint)box.Value;

    [Fact]
    public static void UnusedValues()
    {
        foreach (int count in new[] { -1, 0, 8, 9, int.MaxValue })
            Assert.Equal(count >= 0 && count <= 8, ArrayRange(new int[8], count));
        Assert.False(ArrayRange(null, 0));
        Assert.False(UnusedArrayLength(new int[1]));
        Assert.False(UnusedStringLength("x"));
        Assert.False(UnusedField(new Box()));
        Assert.Throws<NullReferenceException>(() => UnusedArrayLength(null));
        Assert.Throws<NullReferenceException>(() => UnusedStringLength(null));
        Assert.Throws<NullReferenceException>(() => UnusedField(null));
    }

    private struct Holder
    {
        public object Value;
    }

    private sealed class Record
    {
        public int Index;
    }

    private sealed class RecordList
    {
        public List<Record> Items = new List<Record>(8);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddWithRecordedIndex(RecordList list, Record record)
    {
        // ARM64-NOT: CORINFO_HELP_RNGCHKFAIL
        // X64-NOT: CORINFO_HELP_RNGCHKFAIL
        record.Index = list.Items.Count;
        list.Items.Add(record);
    }

    [Fact]
    public static void CseCopyBounds()
    {
        RecordList list = new RecordList();
        Record record = new Record();
        for (int i = 0; i < 16; i++)
        {
            AddWithRecordedIndex(list, record);
            Assert.Equal(i, record.Index);
            Assert.Same(record, list.Items[i]);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object CopyReceiver(ref Holder holder)
    {
        // ARM64: ldr{{s?}}b wzr,
        // ARM64-NOT: ldr{{s?}}b wzr,
        // X64: cmp byte ptr
        // X64-NOT: cmp byte ptr
        object first = holder.Value;
        first.GetType();
        object second = holder.Value;
        second.GetType();
        GC.KeepAlive(first);
        return second;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Change(ref Holder holder, object value) => holder.Value = value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object AfterMutation(ref Holder holder, object value)
    {
        object first = holder.Value;
        first.GetType();
        Change(ref holder, value);
        object second = holder.Value;
        second.GetType();
        GC.KeepAlive(first);
        return second;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object AfterMerge(ref Holder holder, object other, bool useOther)
    {
        object first = holder.Value;
        first.GetType();
        object second = useOther ? other : holder.Value;
        second.GetType();
        GC.KeepAlive(first);
        return second;
    }

    [Fact]
    public static void CseCopies()
    {
        object expected = new object();
        Holder holder = new Holder { Value = expected };
        Assert.Same(expected, CopyReceiver(ref holder));
        Assert.Same(expected, AfterMerge(ref holder, null, false));
        Assert.Throws<NullReferenceException>(() => AfterMerge(ref holder, null, true));
        object replacement = new object();
        Assert.Same(replacement, AfterMutation(ref holder, replacement));
        Assert.Throws<NullReferenceException>(() => AfterMutation(ref holder, null));
        Assert.Throws<NullReferenceException>(() => CopyReceiver(ref holder));
    }

    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void func(object o)
    {
        if (o == null)
            throw new Exception();
        o.GetType();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            func(new Object());
            Console.WriteLine("Passed");
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 666;
        }
    }
}
