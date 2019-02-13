// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;

namespace System.Runtime.Caching
{
    internal struct ExpiresEntryRef
    {
        static internal readonly ExpiresEntryRef INVALID = new ExpiresEntryRef(0, 0);

        private const uint ENTRY_MASK = 0x000000ffu;
        private const uint PAGE_MASK = 0xffffff00u;
        private const int PAGE_SHIFT = 8;

        private uint _ref;

        internal ExpiresEntryRef(int pageIndex, int entryIndex)
        {
            Debug.Assert((pageIndex & 0x00ffffff) == pageIndex, "(pageIndex & 0x00ffffff) == pageIndex");
            Debug.Assert((entryIndex & ENTRY_MASK) == entryIndex, "(entryIndex & ENTRY_MASK) == entryIndex");
            Debug.Assert(entryIndex != 0 || pageIndex == 0, "entryIndex != 0 || pageIndex == 0");

            _ref = ((((uint)pageIndex) << PAGE_SHIFT) | (((uint)(entryIndex)) & ENTRY_MASK));
        }

        public override bool Equals(object value)
        {
            if (value is ExpiresEntryRef)
            {
                return _ref == ((ExpiresEntryRef)value)._ref;
            }

            return false;
        }

        public static bool operator !=(ExpiresEntryRef r1, ExpiresEntryRef r2)
        {
            return r1._ref != r2._ref;
        }
        public static bool operator ==(ExpiresEntryRef r1, ExpiresEntryRef r2)
        {
            return r1._ref == r2._ref;
        }

        public override int GetHashCode()
        {
            return (int)_ref;
        }

        internal int PageIndex
        {
            get
            {
                int result = (int)(_ref >> PAGE_SHIFT);
                return result;
            }
        }

        internal int Index
        {
            get
            {
                int result = (int)(_ref & ENTRY_MASK);
                return result;
            }
        }

        internal bool IsInvalid
        {
            get
            {
                return _ref == 0;
            }
        }
    }

    [SuppressMessage("Microsoft.Portability", "CA1900:ValueTypeFieldsShouldBePortable", Justification = "Grandfathered suppression from original caching code checkin")]
    [StructLayout(LayoutKind.Explicit)]
    internal struct ExpiresEntry
    {
        [FieldOffset(0)]
        internal DateTime _utcExpires;

        [FieldOffset(0)]
        internal ExpiresEntryRef _next;

        [FieldOffset(4)]
        internal int _cFree;

        [FieldOffset(8)]
        internal MemoryCacheEntry _cacheEntry;
    }

    internal struct ExpiresPage
    {
        internal ExpiresEntry[] _entries;
        internal int _pageNext;
        internal int _pagePrev;
    }

    internal struct ExpiresPageList
    {
        internal int _head;
        internal int _tail;
    }

    internal sealed class ExpiresBucket
    {
        private const int NUM_ENTRIES = 127;
        private const int LENGTH_ENTRIES = 128;

        private const int MIN_PAGES_INCREMENT = 10;
        private const int MAX_PAGES_INCREMENT = 340;
        private const double MIN_LOAD_FACTOR = 0.5;

        private const int COUNTS_LENGTH = 4;

        private static readonly TimeSpan s_COUNT_INTERVAL = new TimeSpan(CacheExpires._tsPerBucket.Ticks / COUNTS_LENGTH);

        private readonly CacheExpires _cacheExpires;
        private readonly byte _bucket;

        private ExpiresPage[] _pages;

        private int _cEntriesInUse;
        private int _cPagesInUse;
        private int _cEntriesInFlush;
        private int _minEntriesInUse;

        private ExpiresPageList _freePageList;
        private ExpiresPageList _freeEntryList;
        private bool _blockReduce;
        private DateTime _utcMinExpires;
        private int[] _counts;
        private DateTime _utcLastCountReset;

        internal ExpiresBucket(CacheExpires cacheExpires, byte bucket, DateTime utcNow)
        {
            _cacheExpires = cacheExpires;
            _bucket = bucket;
            _counts = new int[COUNTS_LENGTH];
            ResetCounts(utcNow);
            InitZeroPages();
        }

        private void InitZeroPages()
        {
            Debug.Assert(_cPagesInUse == 0, "_cPagesInUse == 0");
            Debug.Assert(_cEntriesInUse == 0, "_cEntriesInUse == 0");
            Debug.Assert(_cEntriesInFlush == 0, "_cEntriesInFlush == 0");

            _pages = null;
            _minEntriesInUse = -1;
            _freePageList._head = -1;
            _freePageList._tail = -1;
            _freeEntryList._head = -1;
            _freeEntryList._tail = -1;
        }
        private void ResetCounts(DateTime utcNow)
        {
            _utcLastCountReset = utcNow;
            _utcMinExpires = DateTime.MaxValue;

            for (int i = 0; i < _counts.Length; i++)
            {
                _counts[i] = 0;
            }
        }

        private int GetCountIndex(DateTime utcExpires)
        {
            return Math.Max(0, (int)((utcExpires - _utcLastCountReset).Ticks / s_COUNT_INTERVAL.Ticks));
        }

        private void AddCount(DateTime utcExpires)
        {
            int ci = GetCountIndex(utcExpires);
            for (int i = _counts.Length - 1; i >= ci; i--)
            {
                _counts[i]++;
            }

            if (utcExpires < _utcMinExpires)
            {
                _utcMinExpires = utcExpires;
            }
        }

        private void RemoveCount(DateTime utcExpires)
        {
            int ci = GetCountIndex(utcExpires);
            for (int i = _counts.Length - 1; i >= ci; i--)
            {
                _counts[i]--;
            }
        }

        private int GetExpiresCount(DateTime utcExpires)
        {
            if (utcExpires < _utcMinExpires)
                return 0;

            int ci = GetCountIndex(utcExpires);
            if (ci >= _counts.Length)
                return _cEntriesInUse;

            return _counts[ci];
        }

        private void AddToListHead(int pageIndex, ref ExpiresPageList list)
        {
            Debug.Assert((list._head == -1) == (list._tail == -1), "(list._head == -1) == (list._tail == -1)");

            (_pages[(pageIndex)]._pagePrev) = -1;
            (_pages[(pageIndex)]._pageNext) = list._head;
            if (list._head != -1)
            {
                Debug.Assert((_pages[(list._head)]._pagePrev) == -1, "PagePrev(list._head) == -1");
                (_pages[(list._head)]._pagePrev) = pageIndex;
            }
            else
            {
                list._tail = pageIndex;
            }

            list._head = pageIndex;
        }

        private void AddToListTail(int pageIndex, ref ExpiresPageList list)
        {
            Debug.Assert((list._head == -1) == (list._tail == -1), "(list._head == -1) == (list._tail == -1)");

            (_pages[(pageIndex)]._pageNext) = -1;
            (_pages[(pageIndex)]._pagePrev) = list._tail;
            if (list._tail != -1)
            {
                Debug.Assert((_pages[(list._tail)]._pageNext) == -1, "PageNext(list._tail) == -1");
                (_pages[(list._tail)]._pageNext) = pageIndex;
            }
            else
            {
                list._head = pageIndex;
            }

            list._tail = pageIndex;
        }

        private int RemoveFromListHead(ref ExpiresPageList list)
        {
            Debug.Assert(list._head != -1, "list._head != -1");

            int oldHead = list._head;
            RemoveFromList(oldHead, ref list);
            return oldHead;
        }

        private void RemoveFromList(int pageIndex, ref ExpiresPageList list)
        {
            Debug.Assert((list._head == -1) == (list._tail == -1), "(list._head == -1) == (list._tail == -1)");

            if ((_pages[(pageIndex)]._pagePrev) != -1)
            {
                Debug.Assert((_pages[((_pages[(pageIndex)]._pagePrev))]._pageNext) == pageIndex, "PageNext(PagePrev(pageIndex)) == pageIndex");
                (_pages[((_pages[(pageIndex)]._pagePrev))]._pageNext) = (_pages[(pageIndex)]._pageNext);
            }
            else
            {
                Debug.Assert(list._head == pageIndex, "list._head == pageIndex");
                list._head = (_pages[(pageIndex)]._pageNext);
            }

            if ((_pages[(pageIndex)]._pageNext) != -1)
            {
                Debug.Assert((_pages[((_pages[(pageIndex)]._pageNext))]._pagePrev) == pageIndex, "PagePrev(PageNext(pageIndex)) == pageIndex");
                (_pages[((_pages[(pageIndex)]._pageNext))]._pagePrev) = (_pages[(pageIndex)]._pagePrev);
            }
            else
            {
                Debug.Assert(list._tail == pageIndex, "list._tail == pageIndex");
                list._tail = (_pages[(pageIndex)]._pagePrev);
            }

            (_pages[(pageIndex)]._pagePrev) = -1;
            (_pages[(pageIndex)]._pageNext) = -1;
        }

        private void MoveToListHead(int pageIndex, ref ExpiresPageList list)
        {
            Debug.Assert(list._head != -1, "list._head != -1");
            Debug.Assert(list._tail != -1, "list._tail != -1");

            if (list._head == pageIndex)
                return;

            RemoveFromList(pageIndex, ref list);

            AddToListHead(pageIndex, ref list);
        }

        private void MoveToListTail(int pageIndex, ref ExpiresPageList list)
        {
            Debug.Assert(list._head != -1, "list._head != -1");
            Debug.Assert(list._tail != -1, "list._tail != -1");

            if (list._tail == pageIndex)
            {
                return;
            }

            RemoveFromList(pageIndex, ref list);
            AddToListTail(pageIndex, ref list);
        }

        private void UpdateMinEntries()
        {
            if (_cPagesInUse <= 1)
            {
                _minEntriesInUse = -1;
            }
            else
            {
                int capacity = _cPagesInUse * NUM_ENTRIES;
                Debug.Assert(capacity > 0, "capacity > 0");
                Debug.Assert(MIN_LOAD_FACTOR < 1.0, "MIN_LOAD_FACTOR < 1.0");

                _minEntriesInUse = (int)(capacity * MIN_LOAD_FACTOR);

                if ((_minEntriesInUse - 1) > ((_cPagesInUse - 1) * NUM_ENTRIES))
                {
                    _minEntriesInUse = -1;
                }
            }
        }

        private void RemovePage(int pageIndex)
        {
            Debug.Assert((((_pages[(pageIndex)]._entries))[0]._cFree) == NUM_ENTRIES, "FreeEntryCount(EntriesI(pageIndex)) == NUM_ENTRIES");

            RemoveFromList(pageIndex, ref _freeEntryList);
            AddToListHead(pageIndex, ref _freePageList);

            Debug.Assert((_pages[(pageIndex)]._entries) != null, "EntriesI(pageIndex) != null");
            (_pages[(pageIndex)]._entries) = null;

            _cPagesInUse--;
            if (_cPagesInUse == 0)
            {
                InitZeroPages();
            }
            else
            {
                UpdateMinEntries();
            }
        }

        private ExpiresEntryRef GetFreeExpiresEntry()
        {
            Debug.Assert(_freeEntryList._head >= 0, "_freeEntryList._head >= 0");
            int pageIndex = _freeEntryList._head;

            ExpiresEntry[] entries = (_pages[(pageIndex)]._entries);
            int entryIndex = ((entries)[0]._next).Index;

            ((entries)[0]._next) = entries[entryIndex]._next;
            ((entries)[0]._cFree)--;
            if (((entries)[0]._cFree) == 0)
            {
                Debug.Assert(((entries)[0]._next).IsInvalid, "FreeEntryHead(entries).IsInvalid");
                RemoveFromList(pageIndex, ref _freeEntryList);
            }
            return new ExpiresEntryRef(pageIndex, entryIndex);
        }

        private void AddExpiresEntryToFreeList(ExpiresEntryRef entryRef)
        {
            ExpiresEntry[] entries = (_pages[(entryRef.PageIndex)]._entries);
            int entryIndex = entryRef.Index;

            Debug.Assert(entries[entryIndex]._cacheEntry == null, "entries[entryIndex]._cacheEntry == null");
            entries[entryIndex]._cFree = 0;

            entries[entryIndex]._next = ((entries)[0]._next);
            ((entries)[0]._next) = entryRef;

            _cEntriesInUse--;
            int pageIndex = entryRef.PageIndex;
            ((entries)[0]._cFree)++;
            if (((entries)[0]._cFree) == 1)
            {
                AddToListHead(pageIndex, ref _freeEntryList);
            }
            else if (((entries)[0]._cFree) == NUM_ENTRIES)
            {
                RemovePage(pageIndex);
            }
        }

        private void Expand()
        {
            Debug.Assert(_cPagesInUse * NUM_ENTRIES == _cEntriesInUse, "_cPagesInUse * NUM_ENTRIES == _cEntriesInUse");
            Debug.Assert(_freeEntryList._head == -1, "_freeEntryList._head == -1");
            Debug.Assert(_freeEntryList._tail == -1, "_freeEntryList._tail == -1");

            if (_freePageList._head == -1)
            {
                int oldLength;
                if (_pages == null)
                {
                    oldLength = 0;
                }
                else
                {
                    oldLength = _pages.Length;
                }

                Debug.Assert(_cPagesInUse == oldLength, "_cPagesInUse == oldLength");
                Debug.Assert(_cEntriesInUse == oldLength * NUM_ENTRIES, "_cEntriesInUse == oldLength * ExpiresEntryRef.NUM_ENTRIES");

                int newLength = oldLength * 2;
                newLength = Math.Max(oldLength + MIN_PAGES_INCREMENT, newLength);
                newLength = Math.Min(newLength, oldLength + MAX_PAGES_INCREMENT);
                Debug.Assert(newLength > oldLength, "newLength > oldLength");

                ExpiresPage[] newPages = new ExpiresPage[newLength];

                for (int i = 0; i < oldLength; i++)
                {
                    newPages[i] = _pages[i];
                }

                for (int i = oldLength; i < newPages.Length; i++)
                {
                    newPages[i]._pagePrev = i - 1;
                    newPages[i]._pageNext = i + 1;
                }

                newPages[oldLength]._pagePrev = -1;
                newPages[newPages.Length - 1]._pageNext = -1;

                _freePageList._head = oldLength;
                _freePageList._tail = newPages.Length - 1;

                _pages = newPages;
            }

            int pageIndex = RemoveFromListHead(ref _freePageList);
            AddToListHead(pageIndex, ref _freeEntryList);

            ExpiresEntry[] entries = new ExpiresEntry[LENGTH_ENTRIES];
            ((entries)[0]._cFree) = NUM_ENTRIES;

            for (int i = 0; i < entries.Length - 1; i++)
            {
                entries[i]._next = new ExpiresEntryRef(pageIndex, i + 1);
            }
            entries[entries.Length - 1]._next = ExpiresEntryRef.INVALID;

            (_pages[(pageIndex)]._entries) = entries;

            _cPagesInUse++;
            UpdateMinEntries();
        }

        private void Reduce()
        {
            if (_cEntriesInUse >= _minEntriesInUse || _blockReduce)
                return;

            Debug.Assert(_freeEntryList._head != -1, "_freeEntryList._head != -1");
            Debug.Assert(_freeEntryList._tail != -1, "_freeEntryList._tail != -1");
            Debug.Assert(_freeEntryList._head != _freeEntryList._tail, "_freeEntryList._head != _freeEntryList._tail");

            int meanFree = (int)(NUM_ENTRIES - (NUM_ENTRIES * MIN_LOAD_FACTOR));
            int pageIndexLast = _freeEntryList._tail;
            int pageIndexCurrent = _freeEntryList._head;
            int pageIndexNext;
            ExpiresEntry[] entries;

            for (; ;)
            {
                pageIndexNext = (_pages[(pageIndexCurrent)]._pageNext);

                if ((((_pages[(pageIndexCurrent)]._entries))[0]._cFree) > meanFree)
                {
                    MoveToListTail(pageIndexCurrent, ref _freeEntryList);
                }
                else
                {
                    MoveToListHead(pageIndexCurrent, ref _freeEntryList);
                }

                if (pageIndexCurrent == pageIndexLast)
                {
                    break;
                }

                pageIndexCurrent = pageIndexNext;
            }

            for (; ;)
            {
                if (_freeEntryList._tail == -1)
                    break;

                entries = (_pages[(_freeEntryList._tail)]._entries);
                Debug.Assert(((entries)[0]._cFree) > 0, "FreeEntryCount(entries) > 0");
                int availableFreeEntries = (_cPagesInUse * NUM_ENTRIES) - ((entries)[0]._cFree) - _cEntriesInUse;
                if (availableFreeEntries < (NUM_ENTRIES - ((entries)[0]._cFree)))
                    break;

                for (int i = 1; i < entries.Length; i++)
                {
                    if (entries[i]._cacheEntry == null)
                        continue;

                    Debug.Assert(_freeEntryList._head != _freeEntryList._tail, "_freeEntryList._head != _freeEntryList._tail");
                    ExpiresEntryRef newRef = GetFreeExpiresEntry();
                    Debug.Assert(newRef.PageIndex != _freeEntryList._tail, "newRef.PageIndex != _freeEntryList._tail");

                    MemoryCacheEntry cacheEntry = entries[i]._cacheEntry;

                    cacheEntry.ExpiresEntryRef = newRef;

                    ExpiresEntry[] newEntries = (_pages[(newRef.PageIndex)]._entries);
                    newEntries[newRef.Index] = entries[i];

                    ((entries)[0]._cFree)++;
                }

                RemovePage(_freeEntryList._tail);
            }
        }

        internal void AddCacheEntry(MemoryCacheEntry cacheEntry)
        {
            lock (this)
            {
                if ((cacheEntry.State & (EntryState.AddedToCache | EntryState.AddingToCache)) == 0)
                    return;

                ExpiresEntryRef entryRef = cacheEntry.ExpiresEntryRef;
                Debug.Assert((cacheEntry.ExpiresBucket == 0xff) == entryRef.IsInvalid, "(cacheEntry.ExpiresBucket == 0xff) == entryRef.IsInvalid");
                if (cacheEntry.ExpiresBucket != 0xff || !entryRef.IsInvalid)
                    return;

                if (_freeEntryList._head == -1)
                {
                    Expand();
                }

                ExpiresEntryRef freeRef = GetFreeExpiresEntry();
                Debug.Assert(cacheEntry.ExpiresBucket == 0xff, "cacheEntry.ExpiresBucket == 0xff");
                Debug.Assert(cacheEntry.ExpiresEntryRef.IsInvalid, "cacheEntry.ExpiresEntryRef.IsInvalid");
                cacheEntry.ExpiresBucket = _bucket;
                cacheEntry.ExpiresEntryRef = freeRef;

                ExpiresEntry[] entries = (_pages[(freeRef.PageIndex)]._entries);
                int entryIndex = freeRef.Index;
                entries[entryIndex]._cacheEntry = cacheEntry;
                entries[entryIndex]._utcExpires = cacheEntry.UtcAbsExp;

                AddCount(cacheEntry.UtcAbsExp);

                _cEntriesInUse++;

                if ((cacheEntry.State & (EntryState.AddedToCache | EntryState.AddingToCache)) == 0)
                {
                    RemoveCacheEntryNoLock(cacheEntry);
                }
            }
        }

        private void RemoveCacheEntryNoLock(MemoryCacheEntry cacheEntry)
        {
            ExpiresEntryRef entryRef = cacheEntry.ExpiresEntryRef;
            if (cacheEntry.ExpiresBucket != _bucket || entryRef.IsInvalid)
                return;

            ExpiresEntry[] entries = (_pages[(entryRef.PageIndex)]._entries);
            int entryIndex = entryRef.Index;

            RemoveCount(entries[entryIndex]._utcExpires);

            cacheEntry.ExpiresBucket = 0xff;
            cacheEntry.ExpiresEntryRef = ExpiresEntryRef.INVALID;
            entries[entryIndex]._cacheEntry = null;

            AddExpiresEntryToFreeList(entryRef);

            if (_cEntriesInUse == 0)
            {
                ResetCounts(DateTime.UtcNow);
            }

            Reduce();

            Debug.WriteLine("CacheExpiresRemove",
                        "Removed item=" + cacheEntry.Key +
                        ",_bucket=" + _bucket +
                        ",ref=" + entryRef +
                        ",now=" + DateTime.Now.ToString("o", CultureInfo.InvariantCulture) +
                        ",expires=" + cacheEntry.UtcAbsExp.ToLocalTime());
        }

        internal void RemoveCacheEntry(MemoryCacheEntry cacheEntry)
        {
            lock (this)
            {
                RemoveCacheEntryNoLock(cacheEntry);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Grandfathered suppression from original caching code checkin")]
        internal void UtcUpdateCacheEntry(MemoryCacheEntry cacheEntry, DateTime utcExpires)
        {
            lock (this)
            {
                ExpiresEntryRef entryRef = cacheEntry.ExpiresEntryRef;
                if (cacheEntry.ExpiresBucket != _bucket || entryRef.IsInvalid)
                    return;

                ExpiresEntry[] entries = (_pages[(entryRef.PageIndex)]._entries);
                int entryIndex = entryRef.Index;

                Debug.Assert(cacheEntry == entries[entryIndex]._cacheEntry);

                RemoveCount(entries[entryIndex]._utcExpires);
                AddCount(utcExpires);

                entries[entryIndex]._utcExpires = utcExpires;

                cacheEntry.UtcAbsExp = utcExpires;
            }
        }

        internal int FlushExpiredItems(DateTime utcNow, bool useInsertBlock)
        {
            if (_cEntriesInUse == 0 || GetExpiresCount(utcNow) == 0)
                return 0;

            Debug.Assert(_cEntriesInFlush == 0, "_cEntriesInFlush == 0");

            ExpiresEntryRef inFlushHead = ExpiresEntryRef.INVALID;

            ExpiresEntry[] entries;
            int entryIndex;
            MemoryCacheEntry cacheEntry;
            int flushed = 0;

            try
            {
                if (useInsertBlock)
                {
                    _cacheExpires.MemoryCacheStore.BlockInsert();
                }

                lock (this)
                {
                    Debug.Assert(_blockReduce == false, "_blockReduce == false");

                    if (_cEntriesInUse == 0 || GetExpiresCount(utcNow) == 0)
                        return 0;

                    ResetCounts(utcNow);
                    int cPages = _cPagesInUse;
                    for (int i = 0; i < _pages.Length; i++)
                    {
                        entries = _pages[i]._entries;
                        if (entries != null)
                        {
                            int cEntries = NUM_ENTRIES - ((entries)[0]._cFree);
                            for (int j = 1; j < entries.Length; j++)
                            {
                                cacheEntry = entries[j]._cacheEntry;
                                if (cacheEntry != null)
                                {
                                    if (entries[j]._utcExpires > utcNow)
                                    {
                                        AddCount(entries[j]._utcExpires);
                                    }
                                    else
                                    {
                                        cacheEntry.ExpiresBucket = 0xff;
                                        cacheEntry.ExpiresEntryRef = ExpiresEntryRef.INVALID;

                                        entries[j]._cFree = 1;

                                        entries[j]._next = inFlushHead;
                                        inFlushHead = new ExpiresEntryRef(i, j);

                                        flushed++;
                                        _cEntriesInFlush++;
                                    }

                                    cEntries--;
                                    if (cEntries == 0)
                                        break;
                                }
                            }

                            cPages--;
                            if (cPages == 0)
                                break;
                        }
                    }

                    if (flushed == 0)
                    {
                        Dbg.Trace("CacheExpiresFlushTotal", "FlushExpiredItems flushed " + flushed +
                                    " expired items, bucket=" + _bucket + "; Time=" + DateTime.Now.ToString("o", CultureInfo.InvariantCulture));

                        return 0;
                    }

                    _blockReduce = true;
                }
            }
            finally
            {
                if (useInsertBlock)
                {
                    _cacheExpires.MemoryCacheStore.UnblockInsert();
                }
            }

            Debug.Assert(!inFlushHead.IsInvalid, "!inFlushHead.IsInvalid");

            MemoryCacheStore cacheStore = _cacheExpires.MemoryCacheStore;
            ExpiresEntryRef current = inFlushHead;
            ExpiresEntryRef next;
            while (!current.IsInvalid)
            {
                entries = (_pages[(current.PageIndex)]._entries);
                entryIndex = current.Index;

                next = entries[entryIndex]._next;

                cacheEntry = entries[entryIndex]._cacheEntry;
                entries[entryIndex]._cacheEntry = null;
                Debug.Assert(cacheEntry.ExpiresEntryRef.IsInvalid, "cacheEntry.ExpiresEntryRef.IsInvalid");
                cacheStore.Remove(cacheEntry, cacheEntry, CacheEntryRemovedReason.Expired);

                current = next;
            }

            try
            {
                if (useInsertBlock)
                {
                    _cacheExpires.MemoryCacheStore.BlockInsert();
                }

                lock (this)
                {
                    current = inFlushHead;
                    while (!current.IsInvalid)
                    {
                        entries = (_pages[(current.PageIndex)]._entries);
                        entryIndex = current.Index;

                        next = entries[entryIndex]._next;

                        _cEntriesInFlush--;
                        AddExpiresEntryToFreeList(current);

                        current = next;
                    }

                    Debug.Assert(_cEntriesInFlush == 0, "_cEntriesInFlush == 0");
                    _blockReduce = false;
                    Reduce();

                    Dbg.Trace("CacheExpiresFlushTotal", "FlushExpiredItems flushed " + flushed +
                                " expired items, bucket=" + _bucket + "; Time=" + DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
                }
            }
            finally
            {
                if (useInsertBlock)
                {
                    _cacheExpires.MemoryCacheStore.UnblockInsert();
                }
            }

            return flushed;
        }
    }

    internal sealed class CacheExpires
    {
        internal static readonly TimeSpan MIN_UPDATE_DELTA = new TimeSpan(0, 0, 1);
        internal static readonly TimeSpan MIN_FLUSH_INTERVAL = new TimeSpan(0, 0, 1);
        internal static readonly TimeSpan _tsPerBucket = new TimeSpan(0, 0, 20);

        private const int NUMBUCKETS = 30;
        private static readonly TimeSpan s_tsPerCycle = new TimeSpan(NUMBUCKETS * _tsPerBucket.Ticks);

        private readonly MemoryCacheStore _cacheStore;
        private readonly ExpiresBucket[] _buckets;
        private GCHandleRef<Timer> _timerHandleRef;
        private DateTime _utcLastFlush;
        private int _inFlush;

        internal CacheExpires(MemoryCacheStore cacheStore)
        {
            Debug.Assert(NUMBUCKETS < byte.MaxValue);

            DateTime utcNow = DateTime.UtcNow;

            _cacheStore = cacheStore;
            _buckets = new ExpiresBucket[NUMBUCKETS];
            for (byte b = 0; b < _buckets.Length; b++)
            {
                _buckets[b] = new ExpiresBucket(this, b, utcNow);
            }
        }

        private int UtcCalcExpiresBucket(DateTime utcDate)
        {
            long ticksFromCycleStart = utcDate.Ticks % s_tsPerCycle.Ticks;
            int bucket = (int)(((ticksFromCycleStart / _tsPerBucket.Ticks) + 1) % NUMBUCKETS);

            return bucket;
        }

        private int FlushExpiredItems(bool checkDelta, bool useInsertBlock)
        {
            int flushed = 0;

            if (Interlocked.Exchange(ref _inFlush, 1) == 0)
            {
                try
                {
                    if (_timerHandleRef == null)
                    {
                        return 0;
                    }
                    DateTime utcNow = DateTime.UtcNow;
                    if (!checkDelta || utcNow - _utcLastFlush >= MIN_FLUSH_INTERVAL || utcNow < _utcLastFlush)
                    {
                        _utcLastFlush = utcNow;
                        foreach (ExpiresBucket bucket in _buckets)
                        {
                            flushed += bucket.FlushExpiredItems(utcNow, useInsertBlock);
                        }

                        Dbg.Trace("CacheExpiresFlushTotal", "FlushExpiredItems flushed a total of " + flushed + " items; Time=" + DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _inFlush, 0);
                }
            }

            return flushed;
        }

        internal int FlushExpiredItems(bool useInsertBlock)
        {
            return FlushExpiredItems(true, useInsertBlock);
        }

        private void TimerCallback(object state)
        {
            FlushExpiredItems(false, false);
        }

        internal void EnableExpirationTimer(bool enable)
        {
            if (enable)
            {
                if (_timerHandleRef == null)
                {
                    DateTime utcNow = DateTime.UtcNow;
                    TimeSpan due = _tsPerBucket - (new TimeSpan(utcNow.Ticks % _tsPerBucket.Ticks));
                    Timer timer;
                    // Don't capture the current ExecutionContext and its AsyncLocals onto the timer causing them to live forever
                    bool restoreFlow = false;
                    try
                    {
                        if (!ExecutionContext.IsFlowSuppressed())
                        {
                            ExecutionContext.SuppressFlow();
                            restoreFlow = true;
                        }

                        timer = new Timer(new TimerCallback(this.TimerCallback), null,
                            due.Ticks / TimeSpan.TicksPerMillisecond, _tsPerBucket.Ticks / TimeSpan.TicksPerMillisecond);
                    }
                    finally
                    {
                        // Restore the current ExecutionContext
                        if (restoreFlow)
                            ExecutionContext.RestoreFlow();
                    }
                    _timerHandleRef = new GCHandleRef<Timer>(timer);

                    Dbg.Trace("Cache", "Cache expiration timer created.");
                }
            }
            else
            {
                GCHandleRef<Timer> timerHandleRef = _timerHandleRef;
                if (timerHandleRef != null && Interlocked.CompareExchange(ref _timerHandleRef, null, timerHandleRef) == timerHandleRef)
                {
                    timerHandleRef.Dispose();

                    Dbg.Trace("Cache", "Cache expiration timer disposed.");
                    while (_inFlush != 0)
                    {
                        Thread.Sleep(100);
                    }
                }
            }
        }

        internal MemoryCacheStore MemoryCacheStore
        {
            get
            {
                return _cacheStore;
            }
        }

        internal void Add(MemoryCacheEntry cacheEntry)
        {
            DateTime utcNow = DateTime.UtcNow;
            if (utcNow > cacheEntry.UtcAbsExp)
            {
                cacheEntry.UtcAbsExp = utcNow;
            }

            int bucket = UtcCalcExpiresBucket(cacheEntry.UtcAbsExp);
            _buckets[bucket].AddCacheEntry(cacheEntry);
        }

        internal void Remove(MemoryCacheEntry cacheEntry)
        {
            byte bucket = cacheEntry.ExpiresBucket;
            if (bucket != 0xff)
            {
                _buckets[bucket].RemoveCacheEntry(cacheEntry);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Grandfathered suppression from original caching code checkin")]
        internal void UtcUpdate(MemoryCacheEntry cacheEntry, DateTime utcNewExpires)
        {
            int oldBucket = cacheEntry.ExpiresBucket;
            int newBucket = UtcCalcExpiresBucket(utcNewExpires);

            if (oldBucket != newBucket)
            {
                Dbg.Trace("CacheExpiresUpdate",
                            "Updating item " + cacheEntry.Key + " from bucket " + oldBucket + " to new bucket " + newBucket);

                if (oldBucket != 0xff)
                {
                    _buckets[oldBucket].RemoveCacheEntry(cacheEntry);
                    cacheEntry.UtcAbsExp = utcNewExpires;
                    _buckets[newBucket].AddCacheEntry(cacheEntry);
                }
            }
            else
            {
                if (oldBucket != 0xff)
                {
                    _buckets[oldBucket].UtcUpdateCacheEntry(cacheEntry, utcNewExpires);
                }
            }
        }
    }
}
