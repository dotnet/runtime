// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    internal struct UsageEntryRef
    {
        internal static readonly UsageEntryRef INVALID = new UsageEntryRef(0, 0);

        private const uint ENTRY_MASK = 0x000000ffu;
        private const int PAGE_SHIFT = 8;

        private readonly uint _ref;

        internal UsageEntryRef(int pageIndex, int entryIndex)
        {
            Debug.Assert((pageIndex & 0x00ffffff) == pageIndex, "(pageIndex & 0x00ffffff) == pageIndex");
            Debug.Assert((Math.Abs(entryIndex) & ENTRY_MASK) == (Math.Abs(entryIndex)), "(Math.Abs(entryIndex) & ENTRY_MASK) == Math.Abs(entryIndex)");
            Debug.Assert(entryIndex != 0 || pageIndex == 0, "entryIndex != 0 || pageIndex == 0");

            _ref = ((((uint)pageIndex) << PAGE_SHIFT) | (((uint)(entryIndex)) & ENTRY_MASK));
        }

        public override bool Equals(object value)
        {
            if (value is UsageEntryRef)
            {
                return _ref == ((UsageEntryRef)value)._ref;
            }

            return false;
        }
        public static bool operator ==(UsageEntryRef r1, UsageEntryRef r2)
        {
            return r1._ref == r2._ref;
        }

        public static bool operator !=(UsageEntryRef r1, UsageEntryRef r2)
        {
            return r1._ref != r2._ref;
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

        internal int Ref1Index
        {
            get
            {
                int result = (int)(sbyte)(_ref & ENTRY_MASK);
                Debug.Assert(result > 0, "result > 0");
                return result;
            }
        }

        internal int Ref2Index
        {
            get
            {
                int result = (int)(sbyte)(_ref & ENTRY_MASK);
                Debug.Assert(result < 0, "result < 0");
                return -result;
            }
        }

        internal bool IsRef1
        {
            get
            {
                return ((int)(sbyte)(_ref & ENTRY_MASK)) > 0;
            }
        }

        internal bool IsRef2
        {
            get
            {
                return ((int)(sbyte)(_ref & ENTRY_MASK)) < 0;
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

    internal struct UsageEntryLink
    {
        internal UsageEntryRef _next;
        internal UsageEntryRef _prev;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct UsageEntry
    {
        [FieldOffset(0)]
        internal UsageEntryLink _ref1;

        [FieldOffset(4)]
        internal int _cFree;

        [FieldOffset(8)]
        internal UsageEntryLink _ref2;

        [FieldOffset(16)]
        internal DateTime _utcDate;

        [FieldOffset(24)]
        internal MemoryCacheEntry _cacheEntry;
    }

    internal struct UsagePage
    {
        internal UsageEntry[] _entries;
        internal int _pageNext;
        internal int _pagePrev;
    }

    internal struct UsagePageList
    {
        internal int _head;
        internal int _tail;
    }

    internal sealed class UsageBucket
    {
        private const int NUM_ENTRIES = 127;
        private const int LENGTH_ENTRIES = 128;

        private const int MIN_PAGES_INCREMENT = 10;
        private const int MAX_PAGES_INCREMENT = 340;
        private const double MIN_LOAD_FACTOR = 0.5;

        private readonly CacheUsage _cacheUsage;
        private readonly byte _bucket;

        private UsagePage[] _pages;
        private int _cEntriesInUse;
        private int _cPagesInUse;
        private int _cEntriesInFlush;
        private int _minEntriesInUse;

        private UsagePageList _freePageList;
        private UsagePageList _freeEntryList;

        private UsageEntryRef _lastRefHead;
        private UsageEntryRef _lastRefTail;
        private UsageEntryRef _addRef2Head;

        private bool _blockReduce;

        internal UsageBucket(CacheUsage cacheUsage, byte bucket)
        {
            _cacheUsage = cacheUsage;
            _bucket = bucket;
            InitZeroPages();
        }

        private void InitZeroPages()
        {
            Debug.Assert(_cPagesInUse == 0, "_cPagesInUse == 0");
            Debug.Assert(_cEntriesInUse == 0, "_cEntriesInUse == 0");
            Debug.Assert(_cEntriesInFlush == 0, "_cEntriesInFlush == 0");
            Debug.Assert(_lastRefHead.IsInvalid, "_lastRefHead.IsInvalid");
            Debug.Assert(_lastRefTail.IsInvalid, "_lastRefTail.IsInvalid");
            Debug.Assert(_addRef2Head.IsInvalid, "_addRef2Head.IsInvalid");

            _pages = null;
            _minEntriesInUse = -1;
            _freePageList._head = -1;
            _freePageList._tail = -1;
            _freeEntryList._head = -1;
            _freeEntryList._tail = -1;
        }

        private void AddToListHead(int pageIndex, ref UsagePageList list)
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

        private void AddToListTail(int pageIndex, ref UsagePageList list)
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

        private int RemoveFromListHead(ref UsagePageList list)
        {
            Debug.Assert(list._head != -1, "list._head != -1");

            int oldHead = list._head;
            RemoveFromList(oldHead, ref list);
            return oldHead;
        }

        private void RemoveFromList(int pageIndex, ref UsagePageList list)
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

        private void MoveToListHead(int pageIndex, ref UsagePageList list)
        {
            Debug.Assert(list._head != -1, "list._head != -1");
            Debug.Assert(list._tail != -1, "list._tail != -1");

            if (list._head == pageIndex)
                return;

            RemoveFromList(pageIndex, ref list);
            AddToListHead(pageIndex, ref list);
        }

        private void MoveToListTail(int pageIndex, ref UsagePageList list)
        {
            Debug.Assert(list._head != -1, "list._head != -1");
            Debug.Assert(list._tail != -1, "list._tail != -1");

            if (list._tail == pageIndex)
                return;

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

        private UsageEntryRef GetFreeUsageEntry()
        {
            Debug.Assert(_freeEntryList._head >= 0, "_freeEntryList._head >= 0");
            int pageIndex = _freeEntryList._head;

            UsageEntry[] entries = (_pages[(pageIndex)]._entries);
            int entryIndex = ((entries)[0]._ref1._next).Ref1Index;

            ((entries)[0]._ref1._next) = entries[entryIndex]._ref1._next;
            ((entries)[0]._cFree)--;
            if (((entries)[0]._cFree) == 0)
            {
                Debug.Assert(((entries)[0]._ref1._next).IsInvalid, "FreeEntryHead(entries).IsInvalid");
                RemoveFromList(pageIndex, ref _freeEntryList);
            }
            return new UsageEntryRef(pageIndex, entryIndex);
        }

        private void AddUsageEntryToFreeList(UsageEntryRef entryRef)
        {
            Debug.Assert(entryRef.IsRef1, "entryRef.IsRef1");

            UsageEntry[] entries = (_pages[(entryRef.PageIndex)]._entries);
            int entryIndex = entryRef.Ref1Index;

            Debug.Assert(entries[entryIndex]._cacheEntry == null, "entries[entryIndex]._cacheEntry == null");
            entries[entryIndex]._utcDate = DateTime.MinValue;
            entries[entryIndex]._ref1._prev = UsageEntryRef.INVALID;
            entries[entryIndex]._ref2._next = UsageEntryRef.INVALID;
            entries[entryIndex]._ref2._prev = UsageEntryRef.INVALID;

            entries[entryIndex]._ref1._next = ((entries)[0]._ref1._next);
            ((entries)[0]._ref1._next) = entryRef;

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

                UsagePage[] newPages = new UsagePage[newLength];

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

            UsageEntry[] entries = new UsageEntry[LENGTH_ENTRIES];
            ((entries)[0]._cFree) = NUM_ENTRIES;

            for (int i = 0; i < entries.Length - 1; i++)
            {
                entries[i]._ref1._next = new UsageEntryRef(pageIndex, i + 1);
            }
            entries[entries.Length - 1]._ref1._next = UsageEntryRef.INVALID;

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
            UsageEntry[] entries;

            while (true)
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
                    break;

                pageIndexCurrent = pageIndexNext;
            }

            while (true)
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
                    UsageEntryRef newRef1 = GetFreeUsageEntry();
                    UsageEntryRef newRef2 = (new UsageEntryRef((newRef1).PageIndex, -(newRef1).Ref1Index));
                    Debug.Assert(newRef1.PageIndex != _freeEntryList._tail, "newRef1.PageIndex != _freeEntryList._tail");

                    UsageEntryRef oldRef1 = new UsageEntryRef(_freeEntryList._tail, i);
                    UsageEntryRef oldRef2 = (new UsageEntryRef((oldRef1).PageIndex, -(oldRef1).Ref1Index));

                    MemoryCacheEntry cacheEntry = entries[i]._cacheEntry;
                    Debug.Assert(cacheEntry.UsageEntryRef == oldRef1, "cacheEntry.UsageEntryRef == oldRef1");
                    cacheEntry.UsageEntryRef = newRef1;

                    UsageEntry[] newEntries = (_pages[(newRef1.PageIndex)]._entries);
                    newEntries[newRef1.Ref1Index] = entries[i];

                    ((entries)[0]._cFree)++;

                    UsageEntryRef prev = newEntries[newRef1.Ref1Index]._ref1._prev;
                    Debug.Assert(prev != oldRef2, "prev != oldRef2");

                    UsageEntryRef next = newEntries[newRef1.Ref1Index]._ref1._next;
                    if (next == oldRef2)
                    {
                        next = newRef2;
                    }

                    { if ((prev).IsRef1) { (_pages[((prev).PageIndex)]._entries)[(prev).Ref1Index]._ref1._next = (newRef1); } else if ((prev).IsRef2) { (_pages[((prev).PageIndex)]._entries)[(prev).Ref2Index]._ref2._next = (newRef1); } else { _lastRefHead = (newRef1); } };
                    { if ((next).IsRef1) { (_pages[((next).PageIndex)]._entries)[(next).Ref1Index]._ref1._prev = (newRef1); } else if ((next).IsRef2) { (_pages[((next).PageIndex)]._entries)[(next).Ref2Index]._ref2._prev = (newRef1); } else { _lastRefTail = (newRef1); } };

                    prev = newEntries[newRef1.Ref1Index]._ref2._prev;
                    if (prev == oldRef1)
                    {
                        prev = newRef1;
                    }

                    next = newEntries[newRef1.Ref1Index]._ref2._next;
                    Debug.Assert(next != oldRef1, "next != oldRef1");

                    { if ((prev).IsRef1) { (_pages[((prev).PageIndex)]._entries)[(prev).Ref1Index]._ref1._next = (newRef2); } else if ((prev).IsRef2) { (_pages[((prev).PageIndex)]._entries)[(prev).Ref2Index]._ref2._next = (newRef2); } else { _lastRefHead = (newRef2); } };
                    { if ((next).IsRef1) { (_pages[((next).PageIndex)]._entries)[(next).Ref1Index]._ref1._prev = (newRef2); } else if ((next).IsRef2) { (_pages[((next).PageIndex)]._entries)[(next).Ref2Index]._ref2._prev = (newRef2); } else { _lastRefTail = (newRef2); } };

                    if (_addRef2Head == oldRef2)
                    {
                        _addRef2Head = newRef2;
                    }
                }

                RemovePage(_freeEntryList._tail);
            }
        }

        internal void AddCacheEntry(MemoryCacheEntry cacheEntry)
        {
            lock (this)
            {
                if (_freeEntryList._head == -1)
                {
                    Expand();
                }

                UsageEntryRef freeRef1 = GetFreeUsageEntry();
                UsageEntryRef freeRef2 = (new UsageEntryRef((freeRef1).PageIndex, -(freeRef1).Ref1Index));
                Debug.Assert(cacheEntry.UsageEntryRef.IsInvalid, "cacheEntry.UsageEntryRef.IsInvalid");
                cacheEntry.UsageEntryRef = freeRef1;

                UsageEntry[] entries = (_pages[(freeRef1.PageIndex)]._entries);
                int entryIndex = freeRef1.Ref1Index;
                entries[entryIndex]._cacheEntry = cacheEntry;
                entries[entryIndex]._utcDate = DateTime.UtcNow;

                entries[entryIndex]._ref1._prev = UsageEntryRef.INVALID;
                entries[entryIndex]._ref2._next = _addRef2Head;
                if (_lastRefHead.IsInvalid)
                {
                    entries[entryIndex]._ref1._next = freeRef2;
                    entries[entryIndex]._ref2._prev = freeRef1;
                    _lastRefTail = freeRef2;
                }
                else
                {
                    entries[entryIndex]._ref1._next = _lastRefHead;
                    { if ((_lastRefHead).IsRef1) { (_pages[((_lastRefHead).PageIndex)]._entries)[(_lastRefHead).Ref1Index]._ref1._prev = (freeRef1); } else if ((_lastRefHead).IsRef2) { (_pages[((_lastRefHead).PageIndex)]._entries)[(_lastRefHead).Ref2Index]._ref2._prev = (freeRef1); } else { _lastRefTail = (freeRef1); } };

                    UsageEntryRef next, prev;
                    if (_addRef2Head.IsInvalid)
                    {
                        prev = _lastRefTail;
                        next = UsageEntryRef.INVALID;
                    }
                    else
                    {
                        prev = (_pages[(_addRef2Head.PageIndex)]._entries)[_addRef2Head.Ref2Index]._ref2._prev;
                        next = _addRef2Head;
                    }

                    entries[entryIndex]._ref2._prev = prev;
                    { if ((prev).IsRef1) { (_pages[((prev).PageIndex)]._entries)[(prev).Ref1Index]._ref1._next = (freeRef2); } else if ((prev).IsRef2) { (_pages[((prev).PageIndex)]._entries)[(prev).Ref2Index]._ref2._next = (freeRef2); } else { _lastRefHead = (freeRef2); } };
                    { if ((next).IsRef1) { (_pages[((next).PageIndex)]._entries)[(next).Ref1Index]._ref1._prev = (freeRef2); } else if ((next).IsRef2) { (_pages[((next).PageIndex)]._entries)[(next).Ref2Index]._ref2._prev = (freeRef2); } else { _lastRefTail = (freeRef2); } };
                }

                _lastRefHead = freeRef1;
                _addRef2Head = freeRef2;

                _cEntriesInUse++;

                Dbg.Trace("CacheUsageAdd",
                            "Added item=" + cacheEntry.Key +
                            ",_bucket=" + _bucket +
                            ",ref=" + freeRef1);
            }
        }

        private void RemoveEntryFromLastRefList(UsageEntryRef entryRef)
        {
            Debug.Assert(entryRef.IsRef1, "entryRef.IsRef1");
            UsageEntry[] entries = (_pages[(entryRef.PageIndex)]._entries);
            int entryIndex = entryRef.Ref1Index;

            UsageEntryRef prev = entries[entryIndex]._ref1._prev;
            UsageEntryRef next = entries[entryIndex]._ref1._next;

            { if ((prev).IsRef1) { (_pages[((prev).PageIndex)]._entries)[(prev).Ref1Index]._ref1._next = (next); } else if ((prev).IsRef2) { (_pages[((prev).PageIndex)]._entries)[(prev).Ref2Index]._ref2._next = (next); } else { _lastRefHead = (next); } };
            { if ((next).IsRef1) { (_pages[((next).PageIndex)]._entries)[(next).Ref1Index]._ref1._prev = (prev); } else if ((next).IsRef2) { (_pages[((next).PageIndex)]._entries)[(next).Ref2Index]._ref2._prev = (prev); } else { _lastRefTail = (prev); } };

            prev = entries[entryIndex]._ref2._prev;
            next = entries[entryIndex]._ref2._next;

            UsageEntryRef entryRef2 = (new UsageEntryRef((entryRef).PageIndex, -(entryRef).Ref1Index));

            { if ((prev).IsRef1) { (_pages[((prev).PageIndex)]._entries)[(prev).Ref1Index]._ref1._next = (next); } else if ((prev).IsRef2) { (_pages[((prev).PageIndex)]._entries)[(prev).Ref2Index]._ref2._next = (next); } else { _lastRefHead = (next); } };
            { if ((next).IsRef1) { (_pages[((next).PageIndex)]._entries)[(next).Ref1Index]._ref1._prev = (prev); } else if ((next).IsRef2) { (_pages[((next).PageIndex)]._entries)[(next).Ref2Index]._ref2._prev = (prev); } else { _lastRefTail = (prev); } };

            if (_addRef2Head == entryRef2)
            {
                _addRef2Head = next;
            }
        }

        internal void RemoveCacheEntry(MemoryCacheEntry cacheEntry)
        {
            lock (this)
            {
                UsageEntryRef entryRef = cacheEntry.UsageEntryRef;
                if (entryRef.IsInvalid)
                    return;

                UsageEntry[] entries = (_pages[(entryRef.PageIndex)]._entries);
                int entryIndex = entryRef.Ref1Index;

                cacheEntry.UsageEntryRef = UsageEntryRef.INVALID;
                entries[entryIndex]._cacheEntry = null;

                RemoveEntryFromLastRefList(entryRef);

                AddUsageEntryToFreeList(entryRef);

                Reduce();

                Dbg.Trace("CacheUsageRemove",
                            "Removed item=" + cacheEntry.Key +
                            ",_bucket=" + _bucket +
                            ",ref=" + entryRef);
            }
        }

        internal void UpdateCacheEntry(MemoryCacheEntry cacheEntry)
        {
            lock (this)
            {
                UsageEntryRef entryRef = cacheEntry.UsageEntryRef;
                if (entryRef.IsInvalid)
                    return;

                UsageEntry[] entries = (_pages[(entryRef.PageIndex)]._entries);
                int entryIndex = entryRef.Ref1Index;
                UsageEntryRef entryRef2 = (new UsageEntryRef((entryRef).PageIndex, -(entryRef).Ref1Index));

                UsageEntryRef prev = entries[entryIndex]._ref2._prev;
                UsageEntryRef next = entries[entryIndex]._ref2._next;

                { if ((prev).IsRef1) { (_pages[((prev).PageIndex)]._entries)[(prev).Ref1Index]._ref1._next = (next); } else if ((prev).IsRef2) { (_pages[((prev).PageIndex)]._entries)[(prev).Ref2Index]._ref2._next = (next); } else { _lastRefHead = (next); } };
                { if ((next).IsRef1) { (_pages[((next).PageIndex)]._entries)[(next).Ref1Index]._ref1._prev = (prev); } else if ((next).IsRef2) { (_pages[((next).PageIndex)]._entries)[(next).Ref2Index]._ref2._prev = (prev); } else { _lastRefTail = (prev); } };

                if (_addRef2Head == entryRef2)
                {
                    _addRef2Head = next;
                }

                entries[entryIndex]._ref2 = entries[entryIndex]._ref1;
                prev = entries[entryIndex]._ref2._prev;
                next = entries[entryIndex]._ref2._next;

                { if ((prev).IsRef1) { (_pages[((prev).PageIndex)]._entries)[(prev).Ref1Index]._ref1._next = (entryRef2); } else if ((prev).IsRef2) { (_pages[((prev).PageIndex)]._entries)[(prev).Ref2Index]._ref2._next = (entryRef2); } else { _lastRefHead = (entryRef2); } };
                { if ((next).IsRef1) { (_pages[((next).PageIndex)]._entries)[(next).Ref1Index]._ref1._prev = (entryRef2); } else if ((next).IsRef2) { (_pages[((next).PageIndex)]._entries)[(next).Ref2Index]._ref2._prev = (entryRef2); } else { _lastRefTail = (entryRef2); } };

                entries[entryIndex]._ref1._prev = UsageEntryRef.INVALID;
                entries[entryIndex]._ref1._next = _lastRefHead;

                { if ((_lastRefHead).IsRef1) { (_pages[((_lastRefHead).PageIndex)]._entries)[(_lastRefHead).Ref1Index]._ref1._prev = (entryRef); } else if ((_lastRefHead).IsRef2) { (_pages[((_lastRefHead).PageIndex)]._entries)[(_lastRefHead).Ref2Index]._ref2._prev = (entryRef); } else { _lastRefTail = (entryRef); } };
                _lastRefHead = entryRef;

                Dbg.Trace("CacheUsageUpdate",
                            "Updated item=" + cacheEntry.Key +
                            ",_bucket=" + _bucket +
                            ",ref=" + entryRef);
            }
        }

        internal int FlushUnderUsedItems(int maxFlush, bool force)
        {
            if (_cEntriesInUse == 0)
                return 0;

            Debug.Assert(maxFlush > 0, "maxFlush is not greater than 0, instead is " + maxFlush);
            Debug.Assert(_cEntriesInFlush == 0, "_cEntriesInFlush == 0");

            UsageEntryRef inFlushHead = UsageEntryRef.INVALID;

            UsageEntryRef prev, prevNext;
            DateTime utcDate;
            UsageEntry[] entries;
            int entryIndex;
            MemoryCacheEntry cacheEntry;
            int flushed = 0;

            try
            {
                _cacheUsage.MemoryCacheStore.BlockInsert();

                lock (this)
                {
                    Debug.Assert(_blockReduce == false, "_blockReduce == false");

                    if (_cEntriesInUse == 0)
                        return 0;

                    DateTime utcNow = DateTime.UtcNow;

                    for (prev = _lastRefTail; _cEntriesInFlush < maxFlush && !prev.IsInvalid; prev = prevNext)
                    {
                        Debug.Assert(_cEntriesInUse > 0, "_cEntriesInUse > 0");

                        prevNext = (_pages[(prev.PageIndex)]._entries)[prev.Ref2Index]._ref2._prev;
                        while (prevNext.IsRef1)
                        {
                            prevNext = (_pages[(prevNext.PageIndex)]._entries)[prevNext.Ref1Index]._ref1._prev;
                        }

                        entries = (_pages[(prev.PageIndex)]._entries);
                        entryIndex = prev.Ref2Index;

                        if (!force)
                        {
                            utcDate = entries[entryIndex]._utcDate;
                            Debug.Assert(utcDate != DateTime.MinValue, "utcDate != DateTime.MinValue");

                            if (utcNow - utcDate <= CacheUsage.NEWADD_INTERVAL && utcNow >= utcDate)
                                continue;
                        }

                        UsageEntryRef prev1 = (new UsageEntryRef((prev).PageIndex, (prev).Ref2Index));
                        cacheEntry = entries[entryIndex]._cacheEntry;
                        Debug.Assert(cacheEntry.UsageEntryRef == prev1, "cacheEntry.UsageEntryRef == prev1");
                        Dbg.Trace("CacheUsageFlushUnderUsedItem", "Flushing underused items, item=" + cacheEntry.Key + ", bucket=" + _bucket);

                        cacheEntry.UsageEntryRef = UsageEntryRef.INVALID;

                        RemoveEntryFromLastRefList(prev1);

                        entries[entryIndex]._ref1._next = inFlushHead;
                        inFlushHead = prev1;

                        flushed++;
                        _cEntriesInFlush++;
                    }

                    if (flushed == 0)
                    {
                        Dbg.Trace("CacheUsageFlushTotal", "Flush(" + maxFlush + "," + force + ") removed " + flushed +
                                    " underused items; Time=" + DateTime.Now.ToString("o", CultureInfo.InvariantCulture));

                        return 0;
                    }

                    _blockReduce = true;
                }
            }
            finally
            {
                _cacheUsage.MemoryCacheStore.UnblockInsert();
            }

            Debug.Assert(!inFlushHead.IsInvalid, "!inFlushHead.IsInvalid");

            MemoryCacheStore cacheStore = _cacheUsage.MemoryCacheStore;
            UsageEntryRef current = inFlushHead;
            UsageEntryRef next;
            while (!current.IsInvalid)
            {
                entries = (_pages[(current.PageIndex)]._entries);
                entryIndex = current.Ref1Index;

                next = entries[entryIndex]._ref1._next;

                cacheEntry = entries[entryIndex]._cacheEntry;
                entries[entryIndex]._cacheEntry = null;
                Debug.Assert(cacheEntry.UsageEntryRef.IsInvalid, "cacheEntry.UsageEntryRef.IsInvalid");
                cacheStore.Remove(cacheEntry, cacheEntry, CacheEntryRemovedReason.Evicted);

                current = next;
            }

            try
            {
                _cacheUsage.MemoryCacheStore.BlockInsert();

                lock (this)
                {
                    current = inFlushHead;
                    while (!current.IsInvalid)
                    {
                        entries = (_pages[(current.PageIndex)]._entries);
                        entryIndex = current.Ref1Index;

                        next = entries[entryIndex]._ref1._next;

                        _cEntriesInFlush--;
                        AddUsageEntryToFreeList(current);

                        current = next;
                    }

                    Debug.Assert(_cEntriesInFlush == 0, "_cEntriesInFlush == 0");

                    _blockReduce = false;
                    Reduce();

                    Dbg.Trace("CacheUsageFlushTotal", "Flush(" + maxFlush + "," + force + ") removed " + flushed +
                                " underused items; Time=" + DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
                }
            }
            finally
            {
                _cacheUsage.MemoryCacheStore.UnblockInsert();
            }

            return flushed;
        }
    }

    internal sealed class CacheUsage
    {
        internal static readonly TimeSpan NEWADD_INTERVAL = new TimeSpan(0, 0, 10);
        internal static readonly TimeSpan CORRELATED_REQUEST_TIMEOUT = new TimeSpan(0, 0, 1);
        internal static readonly TimeSpan MIN_LIFETIME_FOR_USAGE = NEWADD_INTERVAL;
        private const byte NUMBUCKETS = 1;

        private readonly MemoryCacheStore _cacheStore;
        internal readonly UsageBucket[] _buckets;
        private int _inFlush;

        internal CacheUsage(MemoryCacheStore cacheStore)
        {
            _cacheStore = cacheStore;
            _buckets = new UsageBucket[NUMBUCKETS];
            for (byte b = 0; b < _buckets.Length; b++)
            {
                _buckets[b] = new UsageBucket(this, b);
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
            byte bucket = cacheEntry.UsageBucket;
            Debug.Assert(bucket != 0xff, "bucket != 0xff");
            _buckets[bucket].AddCacheEntry(cacheEntry);
        }

        internal void Remove(MemoryCacheEntry cacheEntry)
        {
            byte bucket = cacheEntry.UsageBucket;
            if (bucket != 0xff)
            {
                _buckets[bucket].RemoveCacheEntry(cacheEntry);
            }
        }

        internal void Update(MemoryCacheEntry cacheEntry)
        {
            byte bucket = cacheEntry.UsageBucket;
            if (bucket != 0xff)
            {
                _buckets[bucket].UpdateCacheEntry(cacheEntry);
            }
        }

        internal int FlushUnderUsedItems(int toFlush)
        {
            int flushed = 0;

            if (Interlocked.Exchange(ref _inFlush, 1) == 0)
            {
                try
                {
                    foreach (UsageBucket usageBucket in _buckets)
                    {
                        int flushedOne = usageBucket.FlushUnderUsedItems(toFlush - flushed, false);
                        flushed += flushedOne;
                        if (flushed >= toFlush)
                            break;
                    }

                    if (flushed < toFlush)
                    {
                        foreach (UsageBucket usageBucket in _buckets)
                        {
                            int flushedOne = usageBucket.FlushUnderUsedItems(toFlush - flushed, true);
                            flushed += flushedOne;
                            if (flushed >= toFlush)
                                break;
                        }
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _inFlush, 0);
                }
            }

            return flushed;
        }
    }
}
