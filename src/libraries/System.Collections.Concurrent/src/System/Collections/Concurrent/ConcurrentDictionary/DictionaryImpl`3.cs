// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Core algorithms are based on NonBlockingHashMap,
// written and released to the public domain by Dr.Cliff Click.
// A good overview is here https://www.youtube.com/watch?v=HJ-719EGIts
//

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Internal.Runtime.CompilerServices;

namespace System.Collections.Concurrent
{
    internal abstract partial class DictionaryImpl<TKey, TKeyStore, TValue>
        : DictionaryImpl<TKey, TValue>
    {
        private readonly Entry[] _entries;
        internal DictionaryImpl<TKey, TKeyStore, TValue> _newTable;

        protected readonly ConcurrentDictionary<TKey, TValue> _topDict;
        protected readonly Counter32 allocatedSlotCount = new Counter32();
        private Counter32 _size;

        internal static readonly bool valueIsAtomic = IsValueAtomicPrimitive();

        // Sometimes many threads race to create a new very large table.  Only 1
        // wins the race, but the losers all allocate a junk large table with
        // hefty allocation costs.  Attempt to control the overkill here by
        // throttling attempts to create a new table.  I cannot really block here
        // (lest I lose the non-blocking property) but late-arriving threads can
        // give the initial resizing thread a little time to allocate the initial
        // new table.
        //
        // count of threads attempting an initial resize
        private int _resizers;

        // The next part of the table to copy.  It monotonically transits from zero
        // to table.length.  Visitors to the table can claim 'work chunks' by
        // CAS'ing this field up, then copying the indicated indices from the old
        // table to the new table.  Workers are not required to finish any chunk;
        // the counter simply wraps and work is copied duplicately until somebody
        // somewhere completes the count.
        private int _claimedChunk;

        // Work-done reporting.  Used to efficiently signal when we can move to
        // the new table.  From 0 to length of old table refers to copying from the old
        // table to the new.
        private int _copyDone;

        [DebuggerDisplay("key = {key}; hash = {hash}; value = {value};")]
        [StructLayout(LayoutKind.Sequential)]
        public struct Entry
        {
            internal int hash;
            internal TKeyStore key;
            internal object value;
        }

        private const int MIN_SIZE = 8;

        // targeted time span between resizes.
        // if resizing more often than this, try expanding.
        private const uint RESIZE_MILLIS_TARGET = (uint)1000;

        // create an empty dictionary
        protected abstract DictionaryImpl<TKey, TKeyStore, TValue> CreateNew(int capacity);

        // convert key from its storage form (noop or unboxing) used in Key enumarators
        protected abstract TKey keyFromEntry(TKeyStore entryKey);

        // compares key with another in its storage form
        protected abstract bool keyEqual(TKey key, TKeyStore entryKey);

        // claiming (by writing atomically to the entryKey location)
        // or getting existing slot suitable for storing a given key.
        protected abstract bool TryClaimSlotForPut(ref TKeyStore entryKey, TKey key);

        // claiming (by writing atomically to the entryKey location)
        // or getting existing slot suitable for storing a given key in its store form (could be boxed).
        protected abstract bool TryClaimSlotForCopy(ref TKeyStore entryKey, TKeyStore key);

        internal DictionaryImpl(int capacity, ConcurrentDictionary<TKey, TValue> topDict)
        {
            capacity = Math.Max(capacity, MIN_SIZE);

            capacity = HashHelpers.AlignToPowerOfTwo(capacity);
            this._entries = new Entry[capacity];
            this._size = new Counter32();
            this._topDict = topDict;

            if (!typeof(TKeyStore).IsValueType)
            {
                // do not create a real sweeper just yet. Often it is not needed.
                topDict._sweeperInstance = NULLVALUE;
            }

            _ = valueIsAtomic;
        }

        protected DictionaryImpl(int capacity, DictionaryImpl<TKey, TKeyStore, TValue> other)
        {
            capacity = HashHelpers.AlignToPowerOfTwo(capacity);
            this._entries = new Entry[capacity];
            this._size = other._size;
            this._topDict = other._topDict;
            this._keyComparer = other._keyComparer;
        }

        /// <summary>
        /// Determines whether type TValue can be written atomically
        /// </summary>
        private static bool IsValueAtomicPrimitive()
        {
            // only intereste in primitive value types here.
            if (default(TValue) == null)
            {
                return false;
            }

            //
            // Section 12.6.6 of ECMA CLI explains which types can be read and written atomically without
            // the risk of tearing.
            //
            // See http://www.ecma-international.org/publications/files/ECMA-ST/Ecma-335.pdf
            //
            if (typeof(TValue) == typeof(bool) ||
                typeof(TValue) == typeof(byte) ||
                typeof(TValue) == typeof(char) ||
                typeof(TValue) == typeof(short) ||
                typeof(TValue) == typeof(int) ||
                typeof(TValue) == typeof(sbyte) ||
                typeof(TValue) == typeof(float) ||
                typeof(TValue) == typeof(ushort) ||
                typeof(TValue) == typeof(uint) ||
                typeof(TValue) == typeof(IntPtr) ||
                typeof(TValue) == typeof(UIntPtr))
            {
                return true;
            }

            if (typeof(TValue) == typeof(long) ||
                typeof(TValue) == typeof(double) ||
                typeof(TValue) == typeof(ulong))
            {
                return IntPtr.Size == 8;
            }

            return false;
        }

        private static uint CurrentTickMillis()
        {
            return (uint)Environment.TickCount;
        }

        protected virtual int hash(TKey key)
        {
            Debug.Assert(!(key is null));

            int h = _keyComparer.GetHashCode(key);

            // ensure that hash never matches 0, TOMBPRIMEHASH, ZEROHASH or REGULAR_HASH_BITS
            return h | (SPECIAL_HASH_BITS | 1);
        }

        internal sealed override int Count
        {
            get
            {
                return this.Size;
            }
        }

        internal sealed override void Clear()
        {
            var newTable = CreateNew(MIN_SIZE);
            newTable._size = new Counter32();
            _topDict._table = newTable;
        }

        /// <summary>
        /// returns null if value is not present in the table
        /// otherwise returns the actual value or NULLVALUE if null is the actual value
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override object TryGetValue(TKey key)
        {
            int fullHash = this.hash(key);
            var curTable = this;

        tryWithNewTable:

            var lenMask = curTable._entries.Length - 1;
            int idx = ReduceHashToIndex(fullHash, lenMask);

            // Main spin/reprobe loop
            int reprobeCount = 0;
            while (true)
            {
                ref var entry = ref curTable._entries[idx];

                // an entry is never reused for a different key
                // key/value/hash all read atomically and order of reads is unimportant

                // is this our slot?
                if (fullHash == entry.hash && keyEqual(key, entry.key))
                {
                    // read the value before the _newTable.
                    // if the new table is null later, then the value cannot be forwarded
                    // this also orders reads from the table.
                    var entryValue = Volatile.Read(ref entry.value);
                    if (EntryValueNullOrDead(entryValue))
                    {
                        break;
                    }

                    // "READ BARRIER", if copying has started, we must help with copying and
                    // read from the new table.

                    // if no new table, no need to check for primed value,
                    // but TOMBPRIME is possible when sweeping, check for that
                    if ((curTable._newTable == null && entryValue != TOMBPRIME) ||
                        entryValue.GetType() != typeof(Prime))
                    {
                        return entryValue;
                    }

                    // found a prime, that means the copying or sweeping has started
                    // help and retry in the new table
                    curTable = curTable.CopySlotAndGetNewTable(ref entry, shouldHelp: true);
                    goto tryWithNewTable;
                }

                if (entry.hash == 0)
                {
                    // the slot has not been claimed - the rest of the bucket is empty
                    break;
                }

                // get, put and remove must have the same key lookup logic.
                // But only 'put' needs to force a table-resize for a too-long key-reprobe sequence
                // hitting reprobe limit or finding TOMBPRIMEHASH here means that the key is not in this table,
                // but there could be more in the new table
                if (entry.hash == TOMBPRIMEHASH || reprobeCount >= ReprobeLimit(lenMask))
                {
                    if (curTable._newTable != null)
                    {
                        curTable.HelpCopy();
                        curTable = curTable._newTable;
                        goto tryWithNewTable;
                    }

                    // no new table, so this is a miss
                    break;
                }

                // quadratic reprobe
                reprobeCount++;
                curTable.ResizeOnReprobeCheck(reprobeCount);
                idx = (idx + reprobeCount) & lenMask;
            }

            return null;
        }

        /// <summary>
        /// returns true if value was removed from the table.
        /// oldVal contains original value or default(TValue), if it was not present in the table
        /// </summary>
        internal sealed override bool RemoveIfMatch(TKey key, ref TValue oldVal, ValueMatch match)
        {
            Debug.Assert(
                match == ValueMatch.NotNullOrDead ||
                match == ValueMatch.OldValue ||
                match == ValueMatch.Any);   // same as NotNullOrDead, but not reporting the old value

            var curTable = this;
            int fullHash = curTable.hash(key);

        tryWithNewTable:

            var lenMask = curTable._entries.Length - 1;
            int idx = ReduceHashToIndex(fullHash, lenMask);
            ref Entry entry = ref curTable._entries[idx];

            // Main spin/reprobe loop
            int reprobeCount = 0;
            while (true)
            {
                // an entry is never reused for a different key
                // key/value/hash all read atomically and order of reads is unimportant
                var entryHash = entry.hash;

                // is this our slot?
                if (fullHash == entryHash && curTable.keyEqual(key, entry.key))
                {
                    break;
                }

                if (entryHash == 0)
                {
                    // Found an unassigned slot - which means this
                    // key has never been in this table.
                    oldVal = default;
                    goto FAILED;
                }

                // get, put and remove must have the same key lookup logic.
                // But only 'put' needs to force a table-resize for a too-long key-reprobe sequence
                // hitting reprobe limit or finding TOMBPRIMEHASH here means that the key is not in this table,
                // but there could be more in the new table
                if (entryHash == TOMBPRIMEHASH || reprobeCount >= ReprobeLimit(lenMask))
                {
                    if (curTable._newTable != null)
                    {
                        curTable.HelpCopy();
                        curTable = curTable._newTable;
                        goto tryWithNewTable;
                    }

                    // no new table, so this is a miss
                    break;
                }

                // quadratic reprobing
                reprobeCount++;
                curTable.ResizeOnReprobeCheck(reprobeCount);
                idx = (idx + reprobeCount) & lenMask;
                entry = ref curTable._entries[idx];
            }

            // Found the proper Key slot, now update the Value.
            // We never put a null, so Value slots monotonically move from null to
            // not-null (deleted Values use Tombstone).

            // volatile read to make sure we read the element before we read the _newTable
            // that would guarantee that as long as _newTable == null, entryValue cannot be forwarded.
            var entryValue = Volatile.Read(ref entry.value);
            var newTable = curTable._newTable;

            // See if we are moving to a new table.
            // If so, copy our slot and retry in the new table.
            // Seeing TOMBPRIME entry while no newTable means the slot is in a process of being deleted
            // Let CopySlotAndGetNewTable handle that case too.
            if (newTable != null || entryValue == TOMBPRIME)
            {
                var newTable1 = curTable.CopySlotAndGetNewTable(ref entry, shouldHelp: true);
                Debug.Assert(newTable == newTable1 || (newTable == null && newTable1 == this));
                curTable = newTable1;
                goto tryWithNewTable;
            }

            // We are finally prepared to update the existing table
            while (true)
            {
                Debug.Assert(!(entryValue is Prime));

                // can't remove if nothing is there
                if (EntryValueNullOrDead(entryValue))
                {
                    oldVal = default;
                    goto FAILED;
                }

                if (ValueIsAtomicPrimitive() && match != ValueMatch.Any)
                {
                    // must freeze before removing or before checking for value match
                    // unless it is "Any" case where we have no witnesses of the old value
                    // and can assume all writes in the current box "happened before" the remove.
                    Unsafe.As<Boxed<TValue>>(entryValue).Freeze();
                }

                if (match == ValueMatch.OldValue)
                {
                    TValue unboxedEntryValue = FromObjectValue(entryValue);
                    if (!EqualityComparer<TValue>.Default.Equals(oldVal, unboxedEntryValue))
                    {
                        oldVal = unboxedEntryValue;
                        goto FAILED;
                    }
                }

                // Actually change the Value
                var prev = Interlocked.CompareExchange(ref entry.value, TOMBSTONE, entryValue);
                if (prev == entryValue)
                {
                    // CAS succeeded - we removed!
                    if (match == ValueMatch.NotNullOrDead)
                    {
                        oldVal = FromObjectValue(prev);
                    }

                    // Adjust the size
                    curTable._size.Decrement();
                    SweepCheck();

                    return true;
                }

                // If a Prime'd value got installed, we need to re-run on the new table.
                Debug.Assert(prev != null);
                if (prev.GetType() == typeof(Prime))
                {
                    curTable = curTable.CopySlotAndGetNewTable(ref entry, shouldHelp: true);
                    goto tryWithNewTable;
                }

                // Otherwise we lost the CAS to another racing put/remove.
                // Simply retry from the start.
                entryValue = prev;
            }

        FAILED:
            return false;
        }

        // 1) finds or creates a slot for the key
        // 2) sets the slot value to the newVal if original value meets oldVal and match condition
        // 3) returns true if the value was actually changed
        // Note that pre-existence of the slot is irrelevant
        // since slot without a value is as good as no slot at all
        internal sealed override bool PutIfMatch(TKey key, TValue newVal, ref TValue oldVal, ValueMatch match)
        {
            Debug.Assert(
                match == ValueMatch.NullOrDead ||
                match == ValueMatch.OldValue ||
                match == ValueMatch.Any);

            var curTable = this;
            int fullHash = curTable.hash(key);

        tryWithNewTable:

            var lenMask = curTable._entries.Length - 1;
            int idx = ReduceHashToIndex(fullHash, lenMask);
            ref Entry entry = ref curTable._entries[idx];

            // Spin till we get a slot for the key or force a resizing.
            int reprobeCount = 0;
            while (true)
            {
                // an entry is never reused for a different key
                // key/value/hash all read atomically and order of reads is unimportant
                var entryHash = entry.hash;
                if (entryHash == 0)
                {
                    // Found an unassigned slot - which means this key has never been in this table.
                    // claim the hash first
                    Debug.Assert(fullHash != 0);
                    entryHash = Interlocked.CompareExchange(ref entry.hash, fullHash, 0);
                    if (entryHash == 0)
                    {
                        entryHash = fullHash;
                        if (entryHash == ZEROHASH)
                        {
                            // "added" entry for zero key
                            curTable.allocatedSlotCount.Increment();
                            break;
                        }
                    }
                }

                if (entryHash == fullHash)
                {
                    // hash is good, one way or another,
                    // try claiming the slot for the key
                    if (curTable.TryClaimSlotForPut(ref entry.key, key))
                    {
                        break;
                    }
                }

                // here we know that this slot does not map to our key and must reprobe or resize
                // hitting reprobe limit or finding TOMBPRIMEHASH here means that the key is not in this table,
                // but there could be more in the new table
                if (entryHash == TOMBPRIMEHASH || reprobeCount >= ReprobeLimit(lenMask))
                {
                    // start resize or get new table if resize is already in progress
                    var newTable1 = curTable.Resize();
                    // help along an existing copy
                    curTable.HelpCopy();
                    curTable = newTable1;
                    goto tryWithNewTable;
                }

                // quadratic reprobing
                reprobeCount++;
                curTable.ResizeOnReprobeCheck(reprobeCount);
                idx = (idx + reprobeCount) & lenMask;
                entry = ref curTable._entries[idx];
            }

            // Found the proper Key slot, now update the Value.
            // We never put a null, so Value slots monotonically move from null to
            // not-null (deleted Values use Tombstone).

            // volatile read to make sure we read the element before we read the _newTable
            // that would guarantee that as long as _newTable == null, entryValue cannot be forwarded.
            var entryValue = Volatile.Read(ref entry.value);

            // See if we want to move to a new table (to avoid high average re-probe counts).
            // We only check on the initial set of a Value from null to
            // not-null (i.e., once per key-insert).
            var newTable = curTable._newTable;

            // newTable == entryValue only when both are nulls
                if ((object)newTable == (object)entryValue &&
                curTable.TableIsCrowded())
            {
                // Force the new table copy to start
                newTable = curTable.Resize();
                Debug.Assert(curTable._newTable != null && newTable == curTable._newTable);
            }

            // See if we are moving to a new table.
            // If so, copy our slot and retry in the new table.
            // Seeing TOMBPRIME entry while no newTable means the slot is in a process of being deleted
            // Let CopySlotAndGetNewTable handle that case too.
            if (newTable != null || entryValue == TOMBPRIME)
            {
                var newTable1 = curTable.CopySlotAndGetNewTable(ref entry, shouldHelp: true);
                Debug.Assert(newTable == newTable1 || (newTable == null && newTable1 == this));
                curTable = newTable1;
                goto tryWithNewTable;
            }

            // We are finally prepared to update the existing table
            while (true)
            {
                Debug.Assert(!(entryValue is Prime));
                var entryValueNullOrDead = EntryValueNullOrDead(entryValue);

                switch (match)
                {
                    case ValueMatch.Any:
                        if (ValueIsAtomicPrimitive() &&
                            !entryValueNullOrDead &&
                            Unsafe.As<Boxed<TValue>>(entryValue).TryVolatileWrite(newVal))
                        {
                            return true;
                        }
                        break;

                    case ValueMatch.OldValue:
                        if (entryValueNullOrDead)
                        {
                            goto FAILED;
                        }

                        if (ValueIsAtomicPrimitive() &&
                            Unsafe.As<Boxed<TValue>>(entryValue).TryCompareExchange(oldVal, newVal, out var changed))
                        {
                            return changed;
                        }

                        if (ValueIsAtomicPrimitive())
                        {
                            // we could not change the value in place (this is rare), fallback to replacing the whole box.
                            Unsafe.As<Boxed<TValue>>(entryValue).Freeze();
                        }

                        TValue unboxedEntryValue = FromObjectValue(entryValue);
                        if (!EqualityComparer<TValue>.Default.Equals(oldVal, unboxedEntryValue))
                        {
                            goto FAILED;
                        }
                        break;

                    default:
                        Debug.Assert(match == ValueMatch.NullOrDead);
                        if (!entryValueNullOrDead)
                        {
                            // this is the only case where caller expects to see oldVal
                            // NB: No need to freeze here. This is keyed on mere presence of the value.
                            oldVal = FromObjectValue(entryValue);
                            goto FAILED;
                        }
                        break;
                }

                // Actually change the Value
                object newValObj = ToObjectValue(newVal);
                var prev = Interlocked.CompareExchange(ref entry.value, newValObj, entryValue);
                if (prev == entryValue)
                {
                    // CAS succeeded - we did the update!
                    // Adjust sizes
                    if (entryValueNullOrDead)
                    {
                        curTable._size.Increment();
                    }

                    return true;
                }
                // Else CAS failed

                // If a Prime'd value got installed, we need to re-run the put on the new table.
                Debug.Assert(prev != null);
                if (prev.GetType() == typeof(Prime))
                {
                    curTable = curTable.CopySlotAndGetNewTable(ref entry, shouldHelp: true);
                    goto tryWithNewTable;
                }

                // Otherwise we lost the CAS to another racing put.
                // Simply retry from the start.
                entryValue = prev;
            }

        FAILED:
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ValueIsAtomicPrimitive()
        {
            return default(TValue) != null && valueIsAtomic;
        }

        internal sealed override TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            object newValObj = null;
            TValue result = default(TValue);

            var curTable = this;
            int fullHash = curTable.hash(key);

        TRY_WITH_NEW_TABLE:

            var lenMask = curTable._entries.Length - 1;
            int idx = ReduceHashToIndex(fullHash, lenMask);
            ref Entry entry = ref curTable._entries[idx];

            // Spin till we get a slot for the key or force a resizing.
            int reprobeCount = 0;
            while (true)
            {
                // hash and key are CAS-ed down and follow a specific sequence of states.
                // hence the order of their reads is irrelevant and they do not need to be volatile
                var entryHash = entry.hash;
                if (entryHash == 0)
                {
                    // Found an unassigned slot - which means this
                    // key has never been in this table.
                    // Slot is completely clean, claim the hash first
                    Debug.Assert(fullHash != 0);
                    entryHash = Interlocked.CompareExchange(ref entry.hash, fullHash, 0);
                    if (entryHash == 0)
                    {
                        entryHash = fullHash;
                        if (entryHash == ZEROHASH)
                        {
                            // "added" entry for zero key
                            curTable.allocatedSlotCount.Increment();
                            break;
                        }
                    }
                }

                if (entryHash == fullHash)
                {
                    // hash is good, one way or another,
                    // try claiming the slot for the key
                    if (curTable.TryClaimSlotForPut(ref entry.key, key))
                    {
                        break;
                    }
                }

                // here we know that this slot does not map to our key
                // and must reprobe or resize
                // hitting reprobe limit or finding TOMBPRIMEHASH here means that the key is not in this table,
                // but there could be more in the new table
                if (entryHash == TOMBPRIMEHASH || reprobeCount >= ReprobeLimit(lenMask))
                {
                    // start resize or get new table if resize is already in progress
                    var newTable1 = curTable.Resize();
                    // help along an existing copy
                    curTable.HelpCopy();
                    curTable = newTable1;
                    goto TRY_WITH_NEW_TABLE;
                }

                // quadratic reprobing
                reprobeCount++;
                curTable.ResizeOnReprobeCheck(reprobeCount);
                idx = (idx + reprobeCount) & lenMask;
                entry = ref curTable._entries[idx];
            }

            // Found the proper Key slot, now update the Value.
            // We never put a null, so Value slots monotonically move from null to
            // not-null (deleted Values use Tombstone).

            // volatile read to make sure we read the element before we read the _newTable
            // that would guarantee that as long as _newTable == null, entryValue cannot be forwarded.
            var entryValue = Volatile.Read(ref entry.value);

            // See if we want to move to a new table (to avoid high average re-probe counts).
            // We only check on the initial set of a Value from null to
            // not-null (i.e., once per key-insert).
            var newTable = curTable._newTable;

            // newTable == entryValue only when both are nulls
            if ((object)newTable == (object)entryValue &&
                curTable.TableIsCrowded())
            {
                // Force the new table copy to start
                newTable = curTable.Resize();
                Debug.Assert(curTable._newTable != null && curTable._newTable == newTable);
            }

            // See if we are moving to a new table.
            // If so, copy our slot and retry in the new table.
            // Seeing TOMBPRIME entry while no newTable means the slot is in a process of being deleted
            // Let CopySlotAndGetNewTable handle that case too.
            if (newTable != null || entryValue == TOMBPRIME)
            {
                var newTable1 = curTable.CopySlotAndGetNewTable(ref entry, shouldHelp: true);
                Debug.Assert(newTable == newTable1);
                curTable = newTable;
                goto TRY_WITH_NEW_TABLE;
            }

            if (!EntryValueNullOrDead(entryValue))
            {
                goto GOT_PREV_VALUE;
            }

            // prev value is null or dead.
            // let's try install new value
            newValObj = newValObj ?? ToObjectValue(result = valueFactory(key));
            while (true)
            {
                Debug.Assert(!(entryValue is Prime));

                // Actually change the Value
                var prev = Interlocked.CompareExchange(ref entry.value, newValObj, entryValue);
                if (prev == entryValue)
                {
                    // CAS succeeded - we did the update!
                    // Adjust sizes
                    curTable._size.Increment();
                    goto DONE;
                }
                // Else CAS failed

                // If a Prime'd value got installed, we need to re-run on the new table.
                Debug.Assert(prev != null);
                if (prev.GetType() == typeof(Prime))
                {
                    curTable = curTable.CopySlotAndGetNewTable(ref entry, shouldHelp: true);
                    goto TRY_WITH_NEW_TABLE;
                }

                // Otherwise we lost the CAS to another racing put.
                entryValue = prev;
                if (entryValue != TOMBSTONE)
                {
                    goto GOT_PREV_VALUE;
                }
            }

        GOT_PREV_VALUE:
            result = FromObjectValue(entryValue);

        DONE:
            return result;
        }

        private bool PutSlotCopy(TKeyStore key, object value, int fullHash)
        {
            Debug.Assert(key != null);
            Debug.Assert(value != TOMBSTONE);
            Debug.Assert(value != null);
            Debug.Assert(!(value is Prime));

            var curTable = this;

        TRY_WITH_NEW_TABLE:

            var lenMask = curTable._entries.Length - 1;
            int idx = ReduceHashToIndex(fullHash, lenMask);
            ref Entry entry = ref curTable._entries[idx];

            // Spin till we get a slot for the key or force a resizing.
            int reprobeCount = 0;
            while (true)
            {
                var entryHash = entry.hash;
                if (entryHash == 0)
                {
                    // Slot is completely clean, claim the hash
                    Debug.Assert(fullHash != 0);
                    entryHash = Interlocked.CompareExchange(ref entry.hash, fullHash, 0);
                    if (entryHash == 0)
                    {
                        entryHash = fullHash;
                        if (entryHash == ZEROHASH)
                        {
                            // "added" entry for zero key
                            curTable.allocatedSlotCount.Increment();
                            break;
                        }
                    }
                }

                if (entryHash == fullHash)
                {
                    // hash is good, one way or another, claim the key
                    if (curTable.TryClaimSlotForCopy(ref entry.key, key))
                    {
                        break;
                    }
                }

                // this slot contains a different key

                // here we know that this slot does not map to our key
                // and must reprobe or resize
                // hitting reprobe limit or finding TOMBPRIMEHASH here means that
                // we will not find an appropriate slot in this table
                // but there could be more in the new one
                if (entryHash == TOMBPRIMEHASH || reprobeCount >= ReprobeLimit(lenMask))
                {
                    var resized = curTable.Resize();
                    curTable = resized;
                    goto TRY_WITH_NEW_TABLE;
                }

                // quadratic reprobing
                reprobeCount++;
                // no resize check on reprobe needed.
                // we always insert a new value (or somebody else inserts)
                idx = (idx + reprobeCount) & lenMask;
                entry = ref curTable._entries[idx];
            }

            // Found the proper Key slot, now update the Value.

            // volatile read to make sure we read the element before we read the _newTable
            // that would guarantee that as long as _newTable == null, entryValue cannot be forwarded.
            var entryValue = Volatile.Read(ref entry.value);

            // See if we want to move to a new table (to avoid high average re-probe counts).
            // We only check on the initial set of a Value from null to
            // not-null (i.e., once per key-insert).
            var newTable = curTable._newTable;

            // newTable == entryValue only when both are nulls
            if ((object)newTable == (object)entryValue &&
                curTable.TableIsCrowded())
            {
                // Force the new table copy to start
                newTable = curTable.Resize();
                Debug.Assert(curTable._newTable != null && curTable._newTable == newTable);
            }

            // See if we are moving to a new table.
            // If so, copy our slot and retry in the new table.
            // Seeing TOMBPRIME entry while no newTable means the slot is in a process of being deleted
            // Let CopySlotAndGetNewTable handle that case too.
            if (newTable != null || entryValue == TOMBPRIME)
            {
                var newTable1 = curTable.CopySlotAndGetNewTable(ref entry, shouldHelp: false);
                Debug.Assert(newTable == newTable1);
                curTable = newTable;
                goto TRY_WITH_NEW_TABLE;
            }

            // We are finally prepared to update the existing table
            // if entry value is null and our CAS succeeds - we did the update!
            // otherwise someone else copied the value.
            // table-copy does not (effectively) increase the number of live k/v pairs
            // so no need to update size
            return entry.value == null &&
                   Interlocked.CompareExchange(ref entry.value, value, null) == null;
        }

        // check once in a while if a table might benefit from resizing.
        // one reason for this is that crowdedness check uses estimated counts
        // so we do not always catch this on key inserts.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResizeOnReprobeCheck(int reprobeCount)
        {
            // must be ^2 - 1
            const int reprobeCheckPeriod = 16 - 1;

            // once per reprobeCheckPeriod, check if the table is crowded
            // and initiate a resize
            if ((reprobeCount & reprobeCheckPeriod) == 0)
            {
                ReprobeResizeCheckSlow();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ReprobeResizeCheckSlow()
        {
            if (this.TableIsCrowded())
            {
                this.Resize();
                this.HelpCopy();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TValue FromObjectValue(object obj)
        {
            // regular value type
            if (default(TValue) != null)
            {
                return Unsafe.As<Boxed<TValue>>(obj).Value;
            }

            // null
            if (obj == NULLVALUE)
            {
                return default(TValue);
            }

            // ref type
            if (!typeof(TValue).IsValueType)
            {
                return Unsafe.As<object, TValue>(ref obj);
            }

            // nullable
            return (TValue)obj;
        }

        ///////////////////////////////////////////////////////////
        // Resize support
        ///////////////////////////////////////////////////////////

        internal int Size
        {
            get
            {
                // counter does not lose counts, but reports of increments/decrements can be delayed
                // it might be confusing if we ever report negative size.
                var size = _size.Value;
                var negMask = ~(size >> 31);
                return size & negMask;
            }
        }

        internal int EstimatedSlotsUsed
        {
            get
            {
                return (int)allocatedSlotCount.EstimatedValue;
            }
        }

        internal bool TableIsCrowded()
        {
            // 80% utilization, switch to a bigger table
            return EstimatedSlotsUsed > (_entries.Length >> 2) * 3;
        }

        // Help along an existing resize operation.  This is just a fast cut-out
        // wrapper, to encourage inlining for the fast no-copy-in-progress case.
        private void HelpCopyIfNeeded()
        {
            if (this._newTable != null)
            {
                this.HelpCopy(copy_all: false);
            }
        }

        // Help along an existing resize operation.
        internal void HelpCopy(bool copy_all = false)
        {
            var newTable = this._newTable;
            var oldEntries = this._entries;
            int toCopy = oldEntries.Length;

#if DEBUG
            const int CHUNK_SIZE = 16;
#else
            const int CHUNK_SIZE = 1024;
#endif
            int MIN_COPY_WORK = Math.Min(toCopy, CHUNK_SIZE); // Limit per-thread work

            bool panic = false;
            int claimedChunk = -1;

            while (this._copyDone < toCopy)
            {
                // Still needing to copy?
                // Carve out a chunk of work.
                if (!panic)
                {
                    claimedChunk = this._claimedChunk;

                    while (true)
                    {
                        // panic check
                        // We "panic" if we have tried TWICE to copy every slot - and it still
                        // has not happened.  i.e., twice some thread somewhere claimed they
                        // would copy 'slot X' (by bumping _copyIdx) but they never claimed to
                        // have finished (by bumping _copyDone).  Our choices become limited:
                        // we can wait for the work-claimers to finish (and become a blocking
                        // algorithm) or do the copy work ourselves.  Tiny tables with huge
                        // thread counts trying to copy the table often 'panic'.
                        if (claimedChunk > (toCopy / (CHUNK_SIZE / 2)))
                        {
                            panic = true;
                            break;
                        }

                        var alreadyClaimed = Interlocked.CompareExchange(ref this._claimedChunk, claimedChunk + 1, claimedChunk);
                        if (alreadyClaimed == claimedChunk)
                        {
                            break;
                        }

                        claimedChunk = alreadyClaimed;
                    }
                }
                else
                {
                    // we went through the whole table in panic mode
                    // there cannot be possibly anything left to copy.
                    if (claimedChunk > ((toCopy / (CHUNK_SIZE / 2)) + toCopy / CHUNK_SIZE))
                    {
                        _copyDone = toCopy;
                        PromoteNewTable();
                        return;
                    }

                    claimedChunk++;
                }

                // We now know what to copy.  Try to copy.
                int workdone = 0;
                int copyStart = claimedChunk * CHUNK_SIZE;
                for (int i = 0; i < MIN_COPY_WORK; i++)
                {
                    if (this._copyDone >= toCopy)
                    {
                        PromoteNewTable();
                        return;
                    }

                    if (CopySlot(ref oldEntries[(copyStart + i) & (toCopy - 1)], newTable))
                    {
                        workdone++;
                    }
                }

                if (workdone > 0)
                {
                    // See if we can promote
                    var copyDone = Interlocked.Add(ref this._copyDone, workdone);

                    // Check for copy being ALL done, and promote.
                    if (copyDone >= toCopy)
                    {
                        PromoteNewTable();
                    }
                }

                if (!(copy_all | panic))
                {
                    return;
                }
            }

            // Extra promotion check, in case another thread finished all copying
            // then got stalled before promoting.
            PromoteNewTable();
        }

        private void PromoteNewTable()
        {
            // Looking at the top-level table?
            // Note that we might have
            // nested in-progress copies and manage to finish a nested copy before
            // finishing the top-level copy.  We only promote top-level copies.
            if (_topDict._table == this)
            {
                // Attempt to promote
                if (Interlocked.CompareExchange(ref _topDict._table, this._newTable, this) == this)
                {
                    _topDict._lastResizeTickMillis = CurrentTickMillis();
                }
            }
        }

        // Copy slot 'idx' from the old table to the new table.  If this thread
        // confirmed the copy, update the counters and check for promotion.
        //
        // Returns the result of reading the new table, mostly as a
        // convenience to callers.  We come here with 1-shot copy requests
        // typically because the caller has found a Prime, and has not yet read
        // the new table - which must have changed from null-to-not-null
        // before any Prime appears.  So the caller needs to read the new table
        // field to retry his operation in the new table, but probably has not
        // read it yet.
        internal DictionaryImpl<TKey, TKeyStore, TValue> CopySlotAndGetNewTable(ref Entry entry, bool shouldHelp)
        {
            // However this could be just an entry partially swept.
            // In such case treat the value as being copied (into oblivion) and return the same table back to retry.
            var newTable = this._newTable;

            // We're here because the caller saw a Prime or new table, which implies copying or sweeping is in progress.
            Debug.Assert(entry.value is Prime || newTable != null);

            if (newTable == null)
            {
                Debug.Assert(!typeof(TKeyStore).GetTypeInfo().IsValueType);
                // help with sweeping in case the sweeper thread is stuck. We do not want to come here again.
                // we write unconditionally though, since this is a relatively rare case.
                entry.hash = SPECIAL_HASH_BITS;
                entry.key = default(TKeyStore);

                return this;
            }

            if (CopySlot(ref entry, newTable))
            {
                // Record the slot copied
                var copyDone = Interlocked.Increment(ref this._copyDone);

                // Check for copy being ALL done, and promote.
                if (copyDone >= this._entries.Length)
                {
                    PromoteNewTable();
                    return newTable;
                }
            }

            // Generically help along any copy (except if called recursively from a helper)
            if (shouldHelp)
            {
                this.HelpCopy();
            }

            return newTable;
        }

        // Copy one K/V pair from old table to new table.
        // Returns true if we actually did the copy.
        // Regardless, once this returns, the copy is available in the new table and
        // slot in the old table is no longer usable.
        private static bool CopySlot(ref Entry oldEntry, DictionaryImpl<TKey, TKeyStore, TValue> newTable)
        {
            Debug.Assert(newTable != null);

            // Blindly set the hash from 0 to TOMBPRIMEHASH, to eagerly stop
            // fresh put's from claiming new slots in the old table when the old
            // table is mid-resize.
            var hash = oldEntry.hash;
            if (hash == 0)
            {
                hash = Interlocked.CompareExchange(ref oldEntry.hash, TOMBPRIMEHASH, 0);
                if (hash == 0)
                {
                    // slot was not claimed, copy is done here
                    return true;
                }
            }

            if (hash == TOMBPRIMEHASH)
            {
                // slot was trivially copied, but not by us
                return false;
            }

            // Prevent new values from appearing in the old table.
            // Put a forwarding entry, to prevent further updates.
            // NOTE: Read of the value below must happen before reading of the key,
            // however this read does not need to be volatile since we will have
            // some fences in between reads.
            object oldval = oldEntry.value;

            // already boxed?
            Prime box = oldval as Prime;
            if (box != null)
            {
                // volatile read here since we need to make sure
                // that the key read below happens after we have read oldval above
                // (this read is a dependednt read after oldval, and reading the key happens-after)
                Volatile.Read(ref box.originalValue);
            }
            else
            {
                do
                {
                    box = EntryValueNullOrDead(oldval) ?
                        TOMBPRIME :
                        new Prime(oldval);

                    // CAS down a box'd version of oldval
                    // also works as a complete fence between reading the value and the key
                    object prev = Interlocked.CompareExchange(ref oldEntry.value, box, oldval);

                    if (prev == oldval)
                    {
                        // If we made the Value slot hold a TOMBPRIME, then we both
                        // prevented further updates here but also the (absent)
                        // oldval is vacuously available in the new table.  We
                        // return with true here: any thread looking for a value for
                        // this key can correctly go straight to the new table and
                        // skip looking in the old table.
                        if (box == TOMBPRIME)
                        {
                            return true;
                        }

                        // Break loop; oldval is now boxed by us
                        // it still needs to be copied into the new table.
                        break;
                    }

                    oldval = prev;
                    box = oldval as Prime;
                }
                while (box == null);
            }

            if (box == TOMBPRIME)
            {
                // Copy already complete here, but not by us.
                return false;
            }

            // Copy the value into the new table, but only if we overwrite a null.
            // If another value is already in the new table, then somebody else
            // wrote something there and that write is happens-after any value that
            // appears in the old table.  If putIfMatch does not find a null in the
            // new table - somebody else should have recorded the null-not_null
            // transition in this copy.
            object originalValue = box.originalValue;
            Debug.Assert(originalValue != TOMBSTONE);

            // since we have a real value, there must be a nontrivial key in the table.
            // regular read is ok because value is always CASed down after the key
            // and we ensured that we read the key after the value with fences above
            var key = oldEntry.key;
            bool copiedIntoNew = newTable.PutSlotCopy(key, originalValue, hash);

            // Finally, now that any old value is exposed in the new table, we can
            // forever hide the old-table value by gently inserting TOMBPRIME value.
            // This will stop other threads from uselessly attempting to copy this slot
            // (i.e., it's a speed optimization not a correctness issue).
            if (oldEntry.value != TOMBPRIME)
            {
                oldEntry.value = TOMBPRIME;
            }

            // if we failed to copy, it means something has already appeared in
            // the new table and old value should have been copied before that (not by us).
            return copiedIntoNew;
        }

        // kick off resizing, if not started already, and return the new table.
        private DictionaryImpl<TKey, TKeyStore, TValue> Resize()
        {
            // Check for resize already in progress, probably triggered by another thread.
            // reads of this._newTable in Resize are not volatile
            // we are just opportunistically checking if a new table has arrived.
            return this._newTable ?? ResizeImpl();
        }

        // Resizing after too many probes.  "How Big???" heuristics are here.
        // Callers will (not this routine) help any in-progress copy.
        // Since this routine has a fast cutout for copy-already-started, callers
        // MUST 'help_copy' lest we have a path which forever runs through
        // 'resize' only to discover a copy-in-progress which never progresses.
        private DictionaryImpl<TKey, TKeyStore, TValue> ResizeImpl()
        {
            // No copy in-progress, so start one.
            // First up: compute new table size.
            int oldlen = this._entries.Length;

            const int MAX_SIZE = 1 << 30;
            const int MAX_CHURN_SIZE = 1 << 15;

            // First size estimate is roughly inverse of ProbeLimit
            int sz = Size + (MIN_SIZE >> REPROBE_LIMIT_SHIFT);
            int newsz = sz < (MAX_SIZE >> REPROBE_LIMIT_SHIFT) ?
                                            sz << REPROBE_LIMIT_SHIFT :
                                            sz;

            // if new table would shrink or hold steady,
            // we must be resizing because of churn.
            // target churn based resize rate to be about 1 per RESIZE_TICKS_TARGET
            if (newsz <= oldlen)
            {
                var resizeSpan = CurrentTickMillis() - _topDict._lastResizeTickMillis;

                // note that CurrentTicks() will wrap around every 50 days.
                // For our purposes that is tolerable since it just
                // adds a possibility that in some rare cases a churning resize will not be
                // considered a churning one.
                if (resizeSpan < RESIZE_MILLIS_TARGET)
                {
                    // last resize too recent, expand
                    newsz = oldlen < MAX_CHURN_SIZE ? oldlen << 1 : oldlen;
                }
                else
                {
                    // do not allow shrink too fast
                    newsz = Math.Max(newsz, (int)((long)oldlen * RESIZE_MILLIS_TARGET / resizeSpan));
                }
            }

            // Align up to a power of 2
            newsz = HashHelpers.AlignToPowerOfTwo(newsz);

            // Size calculation: 2 words (K+V) per table entry, plus a handful.  We
            // guess at 32-bit pointers; 64-bit pointers screws up the size calc by
            // 2x but does not screw up the heuristic very much.
            int kBs4 = (((newsz << 1) + 4) << 3/*word to bytes*/) >> 12/*kBs4*/;

            var newTable = this._newTable;

            // Now, if allocation is big enough,
            // limit the number of threads actually allocating memory to a
            // handful - lest we have 750 threads all trying to allocate a giant
            // resized array.
            // conveniently, Increment is also a full fence
            if (kBs4 > 0 && Interlocked.Increment(ref _resizers) >= 2)
            {
                // Already 2 guys trying; wait and see
                // See if resize is already in progress
                if (newTable != null)
                {
                    return newTable;         // Use the new table already
                }

                SpinWait.SpinUntil(() => this._newTable != null, 8 * kBs4);
            }

            // Last check, since the 'new' below is expensive and there is a chance
            // that another thread slipped in a new table while we ran the heuristic.
            newTable = this._newTable;
            // See if resize is already in progress
            if (newTable != null)
            {
                return newTable;          // Use the new table already
            }

            newTable = this.CreateNew(newsz);

            // The new table must be CAS'd in to ensure only 1 winner
            var prev = this._newTable ??
                        Interlocked.CompareExchange(ref this._newTable, newTable, null);

            if (prev != null)
            {
                return prev;
            }
            else
            {
                return newTable;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SweepCheck()
        {
            if (_topDict._sweepRequests == 0)
            {
                _topDict._sweepRequests = 1;
                Sweeper.TryRearm(this);
            }
        }

        private class Sweeper
        {
            private DictionaryImpl<TKey, TKeyStore, TValue> _dict;

            public static void TryRearm(DictionaryImpl<TKey, TKeyStore, TValue> dict)
            {
                ref var sweeperLocation = ref dict._topDict._sweeperInstance;
                var obj = sweeperLocation;
                if (obj != null && Interlocked.CompareExchange(ref sweeperLocation, null, obj) == obj)
                {
                    Sweeper sweeper = obj as Sweeper ?? new Sweeper();
                    sweeper._dict = dict;
                    GC.ReRegisterForFinalize(sweeper);
                }
            }

            ~Sweeper()
            {
                Task.Run(() => Sweep());
            }

            private void Sweep()
            {
                var dict = _dict;
                this._dict = null;

                if (dict == null)
                {
                    // this could happen when table is destroyed
                    return;
                }

                // any key removed after we start sweeping may not be swept
                // signal to future removers that they need another request.
                // note that remove is a CAS, so remover will see this value
                // after removing.
                Interlocked.Exchange(ref dict._topDict._sweepRequests, 0);

                var entries = dict._entries;
                for (int i = 0; i < entries.Length; i++)
                {
                    // if resizing, just help to resize instead
                    if (dict._newTable != null)
                    {
                        do
                        {
                            dict.HelpCopy(copy_all: true);
                            dict = dict._newTable;
                        } while (dict._newTable != null);
                        break;
                    }

                    ref var e = ref entries[i];
                    if (e.value == TOMBSTONE)
                    {
                        if (Interlocked.CompareExchange(ref e.value, TOMBPRIME, TOMBSTONE) == TOMBSTONE)
                        {
                            e.hash = SPECIAL_HASH_BITS;
                            e.key = default(TKeyStore);
                            Interlocked.Increment(ref dict._copyDone);
                        }
                    }

                }

                // got new requests while sweeping. revisit after next GC.
                dict._topDict._sweeperInstance = this;
                if (dict._topDict._sweepRequests != 0)
                {
                    TryRearm(dict);
                }
            }
        }
    }
}
