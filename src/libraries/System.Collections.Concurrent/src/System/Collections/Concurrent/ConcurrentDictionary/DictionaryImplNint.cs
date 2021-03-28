// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Collections.Concurrent
{
    internal sealed class DictionaryImplNint<TValue>
                : DictionaryImpl<nint, nint, TValue>
    {
        internal DictionaryImplNint(int capacity, ConcurrentDictionary<nint, TValue> topDict)
            : base(capacity, topDict)
        {
        }

        internal DictionaryImplNint(int capacity, DictionaryImplNint<TValue> other)
            : base(capacity, other)
        {
        }

        protected override bool TryClaimSlotForPut(ref nint entryKey, nint key)
        {
            return TryClaimSlot(ref entryKey, key);
        }

        protected override bool TryClaimSlotForCopy(ref nint entryKey, nint key)
        {
            return TryClaimSlot(ref entryKey, key);
        }

        private bool TryClaimSlot(ref nint entryKey, nint key)
        {
            var entryKeyValue = entryKey;
            //zero keys are claimed via hash
            if (entryKeyValue == 0 & key != 0)
            {
                entryKeyValue = Interlocked.CompareExchange(ref entryKey, key, (nint)0);
                if (entryKeyValue == 0)
                {
                    // claimed a new slot
                    this.allocatedSlotCount.Increment();
                    return true;
                }
            }

            return key == entryKeyValue || _keyComparer.Equals(key, entryKey);
        }

        protected override int hash(nint key)
        {
            if (key == 0)
            {
                return ZEROHASH;
            }

            return base.hash(key);
        }

        protected override bool keyEqual(nint key, nint entryKey)
        {
            return key == entryKey || _keyComparer.Equals(key, entryKey);
        }

        protected override DictionaryImpl<nint, nint, TValue> CreateNew(int capacity)
        {
            return new DictionaryImplNint<TValue>(capacity, this);
        }

        protected override nint keyFromEntry(nint entryKey)
        {
            return entryKey;
        }
    }

    internal sealed class DictionaryImplNintNoComparer<TValue>
            : DictionaryImpl<nint, nint, TValue>
    {
        internal DictionaryImplNintNoComparer(int capacity, ConcurrentDictionary<nint, TValue> topDict)
            : base(capacity, topDict)
        {
        }

        internal DictionaryImplNintNoComparer(int capacity, DictionaryImplNintNoComparer<TValue> other)
            : base(capacity, other)
        {
        }

        protected override bool TryClaimSlotForPut(ref nint entryKey, nint key)
        {
            return TryClaimSlot(ref entryKey, key);
        }

        protected override bool TryClaimSlotForCopy(ref nint entryKey, nint key)
        {
            return TryClaimSlot(ref entryKey, key);
        }

        private bool TryClaimSlot(ref nint entryKey, nint key)
        {
            var entryKeyValue = entryKey;
            //zero keys are claimed via hash
            if (entryKeyValue == 0 & key != 0)
            {
                entryKeyValue = Interlocked.CompareExchange(ref entryKey, key, (nint)0);
                if (entryKeyValue == 0)
                {
                    // claimed a new slot
                    this.allocatedSlotCount.Increment();
                    return true;
                }
            }

            return key == entryKeyValue;
        }

        internal override object TryGetValue(nint key)
        {
            return base.TryGetValue(key);
        }

        protected override int hash(nint key)
        {
            return (key == 0) ?
                ZEROHASH :
                key.GetHashCode() | SPECIAL_HASH_BITS;
        }

        protected override bool keyEqual(nint key, nint entryKey)
        {
            return key == entryKey;
        }

        protected override DictionaryImpl<nint, nint, TValue> CreateNew(int capacity)
        {
            return new DictionaryImplNintNoComparer<TValue>(capacity, this);
        }

        protected override nint keyFromEntry(nint entryKey)
        {
            return entryKey;
        }
    }
}
