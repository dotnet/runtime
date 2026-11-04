// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

// CSE rewrites a use's conservative value number to its reaching definition's while
// copies and method-table loads keep the original. Facts about the value must still apply.
public class CseValueIdentity
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

    private sealed class Buffer
    {
        public int[] Items;
        public int Index;
        public int Count;
    }

    // A reloaded field gets a fresh conservative value number, and CSE rewrites the reload
    // but not the arithmetic above it. Facts about "Items.Length - 1" or "Count" from the
    // guard must still reach the index built from the reload.

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int LastOfNonEmpty(Buffer b)
    {
        // ARM64-NOT: CORINFO_HELP_RNGCHKFAIL
        // X64-NOT: CORINFO_HELP_RNGCHKFAIL
        if (b.Items.Length == 0)
            return -1;
        return b.Items[b.Items.Length - 1];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int LastGuardedBySub(Buffer b)
    {
        // ARM64-NOT: CORINFO_HELP_RNGCHKFAIL
        // X64-NOT: CORINFO_HELP_RNGCHKFAIL
        int last = b.Items.Length - 1;
        if (last < 0)
            return -1;
        return b.Items[b.Items.Length - 1];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CountAsIndex(Buffer b)
    {
        // ARM64-NOT: CORINFO_HELP_RNGCHKFAIL
        // X64-NOT: CORINFO_HELP_RNGCHKFAIL
        if ((uint)b.Count >= (uint)b.Items.Length)
            return -1;
        return b.Items[b.Count];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NextOfIndex(Buffer b)
    {
        // ARM64-NOT: CORINFO_HELP_RNGCHKFAIL
        // X64-NOT: CORINFO_HELP_RNGCHKFAIL
        if ((uint)(b.Index + 1) >= (uint)b.Items.Length)
            return -1;
        return b.Items[b.Index + 1];
    }

    // The reload's length has its own number, so the difference is a function over two
    // numbers that only the representative shows to be equal. Constant propagation asks for it.

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int LengthDelta(Buffer b)
    {
        // ARM64-NOT: sub {{w[0-9]+, w[0-9]+, w[0-9]+}}
        // X64-NOT: sub {{e[a-z0-9]+, e[a-z0-9]+}}
        int[] items = b.Items;
        if (items.Length == 0)
            return -1;
        return b.Items.Length - items.Length;
    }

    [Fact]
    public static void ReloadedFieldArithmetic()
    {
        Buffer b = new Buffer { Items = new int[] { 1, 2, 3, 4 }, Index = 1, Count = 2 };
        Assert.Equal(4, LastOfNonEmpty(b));
        Assert.Equal(4, LastGuardedBySub(b));
        Assert.Equal(3, CountAsIndex(b));
        Assert.Equal(3, NextOfIndex(b));
        Assert.Equal(0, LengthDelta(b));

        Buffer empty = new Buffer { Items = new int[0], Index = -1, Count = 0 };
        Assert.Equal(-1, LastOfNonEmpty(empty));
        Assert.Equal(-1, LastGuardedBySub(empty));
        Assert.Equal(-1, CountAsIndex(empty));
        Assert.Equal(-1, NextOfIndex(empty));
        Assert.Equal(-1, LengthDelta(empty));

        Buffer edge = new Buffer { Items = new int[] { 7 }, Index = 0, Count = 1 };
        Assert.Equal(-1, CountAsIndex(edge));
        Assert.Equal(-1, NextOfIndex(edge));
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CapturedType(ref Holder holder)
    {
        if (holder.Value is bool)
            return (bool)holder.Value;
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TypeAfterMutation(ref Holder holder, object value)
    {
        if (holder.Value is bool)
        {
            Change(ref holder, value);
            return (bool)holder.Value;
        }
        return false;
    }

    [Fact]
    public static void TypeCopies()
    {
        Holder holder = new Holder { Value = true };
        Assert.True(CapturedType(ref holder));
        Assert.False(TypeAfterMutation(ref holder, false));
        Assert.False(CapturedType(ref holder));
        holder.Value = true;
        Assert.Throws<InvalidCastException>(() => TypeAfterMutation(ref holder, "text"));
        Assert.False(CapturedType(ref holder));
        holder.Value = true;
        Assert.Throws<NullReferenceException>(() => TypeAfterMutation(ref holder, null));
        Assert.False(CapturedType(ref holder));
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T GetMetadataValue<T>(IDictionary<string, object> metadata)
    {
        return metadata.TryGetValue("flag", out object value) && value is T result ? result : default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CountFlags(IEnumerable<IDictionary<string, object>> records)
    {
        // ARM64-NOT: CORINFO_HELP_UNBOX
        // X64-NOT: CORINFO_HELP_UNBOX
        int count = 0;
        foreach (var record in records)
        {
            if (GetMetadataValue<bool>(record))
                count++;
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CastAfterMutation(ref object value, object replacement)
    {
        if (value is bool)
        {
            value = replacement;
            return (bool)value;
        }
        return false;
    }

    [Fact]
    public static void CapturedTypeChecks()
    {
        var records = new List<IDictionary<string, object>>();
        foreach (object value in new object[] { true, false, null, "text", 1 })
            records.Add(new Dictionary<string, object> { ["flag"] = value });
        records.Add(new Dictionary<string, object>());
        Assert.Equal(1, CountFlags(records));

        object boxed = true;
        Assert.False(CastAfterMutation(ref boxed, false));
        Assert.Throws<InvalidCastException>(() => CastAfterMutation(ref boxed, "text"));
        boxed = true;
        Assert.Throws<NullReferenceException>(() => CastAfterMutation(ref boxed, null));
    }
}
