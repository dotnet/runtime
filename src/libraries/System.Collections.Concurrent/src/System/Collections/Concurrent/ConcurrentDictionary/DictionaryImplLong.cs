// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Collections.Concurrent
{
    internal sealed class DictionaryImplLong<TValue>
                : DictionaryImpl<long, long, TValue>
    {
        internal DictionaryImplLong(int capacity, ConcurrentDictionary<long, TValue> topDict)
            : base(capacity, topDict)
        {
        }

        internal DictionaryImplLong(int capacity, DictionaryImplLong<TValue> other)
            : base(capacity, other)
        {
        }

        protected override bool TryClaimSlotForPut(ref long entryKey, long key)
        {
            return TryClaimSlot(ref entryKey, key);
        }

        protected override bool TryClaimSlotForCopy(ref long entryKey, long key)
        {
            return TryClaimSlot(ref entryKey, key);
        }

        private bool TryClaimSlot(ref long entryKey, long key)
        {
            var entryKeyValue = entryKey;
            //zero keys are claimed via hash
            if (entryKeyValue == 0 & key != 0)
            {
                entryKeyValue = Interlocked.CompareExchange(ref entryKey, key, 0);
                if (entryKeyValue == 0)
                {
                    // claimed a new slot
                    this.allocatedSlotCount.Increment();
                    return true;
                }
            }

            return key == entryKeyValue || _keyComparer.Equals(key, entryKey);
        }

        protected override int hash(long key)
        {
            if (key == 0)
            {
                return ZEROHASH;
            }

            return base.hash(key);
        }

        protected override bool keyEqual(long key, long entryKey)
        {
            return key == entryKey || _keyComparer.Equals(key, entryKey);
        }

        protected override DictionaryImpl<long, long, TValue> CreateNew(int capacity)
        {
            return new DictionaryImplLong<TValue>(capacity, this);
        }

        protected override long keyFromEntry(long entryKey)
        {
            return entryKey;
        }
    }

    internal sealed class DictionaryImplLongNoComparer<TValue>
            : DictionaryImpl<long, long, TValue>
    {
        internal DictionaryImplLongNoComparer(int capacity, ConcurrentDictionary<long, TValue> topDict)
            : base(capacity, topDict)
        {
        }

        internal DictionaryImplLongNoComparer(int capacity, DictionaryImplLongNoComparer<TValue> other)
            : base(capacity, other)
        {
        }

        protected override bool TryClaimSlotForPut(ref long entryKey, long key)
        {
            return TryClaimSlot(ref entryKey, key);
        }

        protected override bool TryClaimSlotForCopy(ref long entryKey, long key)
        {
            return TryClaimSlot(ref entryKey, key);
        }

        private bool TryClaimSlot(ref long entryKey, long key)
        {
            var entryKeyValue = entryKey;
            //zero keys are claimed via hash
            if (entryKeyValue == 0 & key != 0)
            {
                entryKeyValue = Interlocked.CompareExchange(ref entryKey, key, 0);
                if (entryKeyValue == 0)
                {
                    // claimed a new slot
                    this.allocatedSlotCount.Increment();
                    return true;
                }
            }

            return key == entryKeyValue;
        }

        // inline the base implementation to devirtualize calls to hash and keyEqual
        internal override object TryGetValue(long key)
        {
            return base.TryGetValue(key);
        }

        protected override int hash(long key)
        {
            return (key == 0) ?
                ZEROHASH :
                key.GetHashCode() | SPECIAL_HASH_BITS;
        }

        protected override bool keyEqual(long key, long entryKey)
        {
            return key == entryKey;
        }

        protected override DictionaryImpl<long, long, TValue> CreateNew(int capacity)
        {
            return new DictionaryImplLongNoComparer<TValue>(capacity, this);
        }

        protected override long keyFromEntry(long entryKey)
        {
            return entryKey;
        }
    }
}
