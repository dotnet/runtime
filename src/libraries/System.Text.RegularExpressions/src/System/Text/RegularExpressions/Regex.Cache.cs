// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using SysDebug = System.Diagnostics.Debug;  // as Regex.Debug
using System.Collections.Generic;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Text.RegularExpressions
{
    public partial class Regex
    {
        private const int CacheDictionarySwitchLimit = 10;

        private static int s_cacheSize = 15;
        // the cache of code and factories that are currently loaded:
        // Dictionary for large cache
        private static readonly Dictionary<CachedCodeEntryKey, CachedCodeEntry> s_cache = new Dictionary<CachedCodeEntryKey, CachedCodeEntry>(s_cacheSize);
        // linked list for LRU and for small cache
        private static int s_cacheCount = 0;
        private static CachedCodeEntry? s_cacheFirst;
        private static CachedCodeEntry? s_cacheLast;

        public static int CacheSize
        {
            get => s_cacheSize;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                lock (s_cache)
                {
                    s_cacheSize = value;  // not to allow other thread to change it while we use cache
                    while (s_cacheCount > s_cacheSize)
                    {
                        CachedCodeEntry last = s_cacheLast!;
                        if (s_cacheCount >= CacheDictionarySwitchLimit)
                        {
                            SysDebug.Assert(s_cache.ContainsKey(last.Key));
                            s_cache.Remove(last.Key);
                        }

                        // update linked list:
                        s_cacheLast = last.Next;
                        if (last.Next != null)
                        {
                            SysDebug.Assert(s_cacheFirst != null);
                            SysDebug.Assert(s_cacheFirst != last);
                            SysDebug.Assert(last.Next.Previous == last);
                            last.Next.Previous = null;
                        }
                        else // last one removed
                        {
                            SysDebug.Assert(s_cacheFirst == last);
                            s_cacheFirst = null;
                        }

                        s_cacheCount--;
                    }
                }
            }
        }

        /// <summary>
        ///  Find cache based on options+pattern+culture and optionally add new cache if not found
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private CachedCodeEntry? GetCachedCode(CachedCodeEntryKey key, bool isToAdd)
        {
            // to avoid lock:
            CachedCodeEntry? first = s_cacheFirst;
            if (first != null && first.Key.Equals(key))
                return first;

            if (s_cacheSize == 0)
                return null;

            return GetCachedCodeEntryInternal(key, isToAdd);
        }

        private CachedCodeEntry? GetCachedCodeEntryInternal(CachedCodeEntryKey key, bool isToAdd)
        {
            lock (s_cache)
            {
                // first look for it in the cache and move it to the head
                CachedCodeEntry? entry = LookupCachedAndPromote(key);

                // it wasn't in the cache, so we'll add a new one
                if (entry == null && isToAdd && s_cacheSize != 0) // check cache size again in case it changed
                {
                    entry = new CachedCodeEntry(key, capnames!, capslist!, _code!, caps!, capsize, _runnerref!, _replref!);

                    // put first in linked list:
                    if (s_cacheFirst != null)
                    {
                        SysDebug.Assert(s_cacheFirst.Next == null);
                        s_cacheFirst.Next = entry;
                        entry.Previous = s_cacheFirst;
                    }
                    s_cacheFirst = entry;

                    s_cacheCount++;
                    if (s_cacheCount >= CacheDictionarySwitchLimit)
                    {
                        if (s_cacheCount == CacheDictionarySwitchLimit)
                        {
                            FillCacheDictionary();
                        }
                        else
                        {
                            s_cache.Add(key, entry);
                        }

                        SysDebug.Assert(s_cacheCount == s_cache.Count);
                    }

                    // update last in linked list:
                    if (s_cacheLast == null)
                    {
                        s_cacheLast = entry;
                    }
                    else if (s_cacheCount > s_cacheSize) // remove last
                    {
                        CachedCodeEntry last = s_cacheLast;
                        if (s_cacheCount >= CacheDictionarySwitchLimit)
                        {
                            SysDebug.Assert(s_cache[last.Key] == s_cacheLast);
                            s_cache.Remove(last.Key);
                        }

                        SysDebug.Assert(last.Previous == null);
                        SysDebug.Assert(last.Next != null);
                        SysDebug.Assert(last.Next.Previous == last);
                        last.Next.Previous = null;
                        s_cacheLast = last.Next;
                        s_cacheCount--;
                    }
                }

                return entry;
            }
        }

        private void FillCacheDictionary()
        {
            s_cache.Clear();
            CachedCodeEntry? next = s_cacheFirst;
            while (next != null)
            {
                s_cache.Add(next.Key, next);
                next = next.Previous;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Unprofitable inline - JIT overly pessimistic
        private static bool TryGetCacheValue(CachedCodeEntryKey key, [NotNullWhen(true)] out CachedCodeEntry? entry)
        {
            if (s_cacheCount >= CacheDictionarySwitchLimit)
            {
                SysDebug.Assert((s_cacheFirst != null && s_cacheLast != null && s_cache.Count > 0) ||
                                (s_cacheFirst == null && s_cacheLast == null && s_cache.Count == 0),
                                "Linked list and Dict should be synchronized");
                return s_cache.TryGetValue(key, out entry);
            }

            return TryGetCacheValueSmall(key, out entry);
        }

        private static bool TryGetCacheValueSmall(CachedCodeEntryKey key, [NotNullWhen(true)] out CachedCodeEntry? entry)
        {
            CachedCodeEntry? current = s_cacheFirst; // first already checked
            if (current != null)
            {
                for (current = current.Previous; current != null; current = current.Previous)
                {
                    if (current.Key.Equals(key))
                    {
                        entry = current;
                        return true;
                    }
                }
            }

            entry = null;
            return false;
        }

        private static CachedCodeEntry? LookupCachedAndPromote(CachedCodeEntryKey key)
        {
            SysDebug.Assert(Monitor.IsEntered(s_cache));

            CachedCodeEntry? entry = s_cacheFirst;
            if (entry != null &&
                !entry.Key.Equals(key) && // again check this as could have been promoted by other thread
                TryGetCacheValue(key, out entry))
            {
                // promote:
                SysDebug.Assert(s_cacheFirst != entry, "key should not get s_livecode_first");
                SysDebug.Assert(s_cacheFirst != null, "as Dict has at least one");
                SysDebug.Assert(s_cacheFirst.Next == null);
                SysDebug.Assert(s_cacheFirst.Previous != null);
                SysDebug.Assert(entry.Next != null, "not first so Next should exist");
                SysDebug.Assert(entry.Next.Previous == entry);
                if (s_cacheLast == entry)
                {
                    SysDebug.Assert(entry.Previous == null, "last");
                    s_cacheLast = entry.Next;
                }
                else
                {
                    SysDebug.Assert(entry.Previous != null, "in middle");
                    SysDebug.Assert(entry.Previous.Next == entry);
                    entry.Previous.Next = entry.Next;
                }
                entry.Next.Previous = entry.Previous;

                s_cacheFirst.Next = entry;
                entry.Previous = s_cacheFirst;
                entry.Next = null;
                s_cacheFirst = entry;
            }

            return entry;
        }

        /// <summary>
        /// Used as a key for CacheCodeEntry
        /// </summary>
        internal readonly struct CachedCodeEntryKey : IEquatable<CachedCodeEntryKey>
        {
            private readonly string _pattern;
            private readonly string _cultureKey;
            private readonly RegexOptions _options;
            private readonly bool _hasTimeout;

            public CachedCodeEntryKey(string pattern, string cultureKey, RegexOptions options, bool hasTimeout)
            {
                SysDebug.Assert(pattern != null, "Pattern must be provided");
                SysDebug.Assert(cultureKey != null, "Culture must be provided");

                _pattern = pattern;
                _cultureKey = cultureKey;
                _options = options;
                _hasTimeout = hasTimeout;
            }

            public override bool Equals(object? obj) =>
                obj is CachedCodeEntryKey other && Equals(other);

            public bool Equals(CachedCodeEntryKey other) =>
                _pattern.Equals(other._pattern) &&
                _cultureKey.Equals(other._cultureKey) &&
                _options == other._options &&
                _hasTimeout == other._hasTimeout;

            public static bool operator ==(CachedCodeEntryKey left, CachedCodeEntryKey right) =>
                left.Equals(right);

            public static bool operator !=(CachedCodeEntryKey left, CachedCodeEntryKey right) =>
                !left.Equals(right);

            public override int GetHashCode() =>
                _pattern.GetHashCode() ^
                _cultureKey.GetHashCode() ^
                ((int)_options);
                // no need to include timeout in the hashcode; it'll almost always be the same
        }

        /// <summary>
        /// Used to cache byte codes
        /// </summary>
        internal sealed class CachedCodeEntry
        {
            public CachedCodeEntry? Next;
            public CachedCodeEntry? Previous;
            public readonly CachedCodeEntryKey Key;
            public RegexCode? Code;
            public readonly Hashtable Caps;
            public readonly Hashtable Capnames;
            public readonly string[] Capslist;
#if FEATURE_COMPILED
            public RegexRunnerFactory? Factory;
#endif
            public readonly int Capsize;
            public readonly ExclusiveReference Runnerref;
            public readonly WeakReference<RegexReplacement?> ReplRef;

            public CachedCodeEntry(CachedCodeEntryKey key, Hashtable capnames, string[] capslist, RegexCode code,
                Hashtable caps, int capsize, ExclusiveReference runner, WeakReference<RegexReplacement?> replref)
            {
                Key = key;
                Capnames = capnames;
                Capslist = capslist;
                Code = code;
                Caps = caps;
                Capsize = capsize;
                Runnerref = runner;
                ReplRef = replref;
            }

#if FEATURE_COMPILED
            public void AddCompiled(RegexRunnerFactory factory)
            {
                Factory = factory;
                Code = null;
            }
#endif
        }
    }
}
