// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class SsaCopyNonNull
{
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
        public RecordBuffer Items = new RecordBuffer();
    }

    private sealed class RecordBuffer
    {
        public Record[] Entries = new Record[16];
        public int Count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddWithRecordedIndex(RecordList list, Record record)
    {
        // ARM64-NOT: CORINFO_HELP_RNGCHKFAIL
        // X64-NOT: CORINFO_HELP_RNGCHKFAIL
        record.Index = list.Items.Count;
        RecordBuffer receiver = list.Items;
        Record[] entries = receiver.Entries;
        int index = receiver.Count;
        if ((uint)index < (uint)entries.Length)
        {
            receiver.Count = index + 1;
            entries[index] = record;
        }
        else
        {
            throw new InvalidOperationException();
        }
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
            Assert.Same(record, list.Items.Entries[i]);
            Assert.Equal(i + 1, list.Items.Count);
        }
        Assert.Throws<InvalidOperationException>(() => AddWithRecordedIndex(list, record));
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
}
