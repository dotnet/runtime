// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text.RegularExpressions
{
    public partial class Regex
    {
        public static int CacheSize
        {
            get => RegexCache.CacheSize;
            set => RegexCache.CacheSize = value;
        }
    }

    internal sealed class RegexCache
    {
        private const int DefaultMaxCacheSize = 15;
        private const int CacheDictionarySwitchLimit = 10;

        private static readonly Dictionary<Key, Node> s_cache = new Dictionary<Key, Node>(DefaultMaxCacheSize);
        private static Node? s_cacheFirst, s_cacheLast; // linked list for LRU and for small cache
        private static int s_maxCacheSize = DefaultMaxCacheSize;
        private static int s_cacheCount = 0;

        public static int CacheSize
        {
            get => s_maxCacheSize;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                lock (s_cache)
                {
                    s_maxCacheSize = value;  // not to allow other thread to change it while we use cache
                    while (s_cacheCount > s_maxCacheSize)
                    {
                        Node last = s_cacheLast!;
                        if (s_cacheCount >= CacheDictionarySwitchLimit)
                        {
                            Debug.Assert(s_cache.ContainsKey(last.Key));
                            s_cache.Remove(last.Key);
                        }

                        // update linked list:
                        s_cacheLast = last.Next;
                        if (last.Next != null)
                        {
                            Debug.Assert(s_cacheFirst != null);
                            Debug.Assert(s_cacheFirst != last);
                            Debug.Assert(last.Next.Previous == last);
                            last.Next.Previous = null;
                        }
                        else // last one removed
                        {
                            Debug.Assert(s_cacheFirst == last);
                            s_cacheFirst = null;
                        }

                        s_cacheCount--;
                    }
                }
            }
        }

        public static Regex GetOrAdd(string pattern)
        {
            Regex.ValidatePattern(pattern);

            CultureInfo culture = CultureInfo.CurrentCulture;
            Key key = new Key(pattern, culture.ToString(), RegexOptions.None, hasTimeout: false);

            if (!TryGet(key, out Regex? regex))
            {
                regex = new Regex(pattern, culture);
                Add(key, regex);
            }

            return regex;
        }

        public static Regex GetOrAdd(string pattern, RegexOptions options, TimeSpan matchTimeout)
        {
            Regex.ValidatePattern(pattern);
            Regex.ValidateOptions(options);
            Regex.ValidateMatchTimeout(matchTimeout);

            CultureInfo culture = (options & RegexOptions.CultureInvariant) != 0 ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
            Key key = new Key(pattern, culture.ToString(), options, matchTimeout != Regex.InfiniteMatchTimeout);

            if (!TryGet(key, out Regex? regex))
            {
                regex = new Regex(pattern, options, matchTimeout, culture);
                Add(key, regex);
            }

            return regex;
        }

        private static bool TryGet(Key key, [NotNullWhen(true)] out Regex? regex)
        {
            Node? cachedRegex = s_cacheFirst;
            if (cachedRegex != null)
            {
                if (cachedRegex.Key.Equals(key))
                {
                    regex = cachedRegex.Regex;
                    return true;
                }

                if (s_maxCacheSize != 0)
                {
                    lock (s_cache)
                    {
                        cachedRegex = LookupCachedAndPromote(key);
                        if (cachedRegex != null)
                        {
                            regex = cachedRegex.Regex;
                            return true;
                        }
                    }
                }
            }

            regex = null;
            return false;
        }

        private static void Add(Key key, Regex regex)
        {
            lock (s_cache)
            {
                // If we're not supposed to cache, or if the entry is already cached, we're done.
                if (s_maxCacheSize == 0 || LookupCachedAndPromote(key) != null)
                {
                    return;
                }

                // Create the entry for caching.
                var entry = new Node(key, regex);

                // Put it at the beginning of the linked list, as it is the most-recently used.
                if (s_cacheFirst != null)
                {
                    Debug.Assert(s_cacheFirst.Next == null);
                    s_cacheFirst.Next = entry;
                    entry.Previous = s_cacheFirst;
                }
                s_cacheFirst = entry;
                s_cacheCount++;

                // If we've graduated to using the dictionary for lookups, add it to the dictionary.
                if (s_cacheCount >= CacheDictionarySwitchLimit)
                {
                    if (s_cacheCount == CacheDictionarySwitchLimit)
                    {
                        // If we just hit the threshold, we need to populate the dictionary from the list.
                        s_cache.Clear();
                        for (Node? next = s_cacheFirst; next != null; next = next.Previous)
                        {
                            s_cache.Add(next.Key, next);
                        }
                    }
                    else
                    {
                        // If we've already populated the dictionary, just add this one entry.
                        s_cache.Add(key, entry);
                    }

                    Debug.Assert(s_cacheCount == s_cache.Count);
                }

                // Update the tail of the linked list.  If nothing was cached, just set the tail.
                // If we're over our cache limit, remove the tail.
                if (s_cacheLast == null)
                {
                    s_cacheLast = entry;
                }
                else if (s_cacheCount > s_maxCacheSize)
                {
                    Node last = s_cacheLast;
                    if (s_cacheCount >= CacheDictionarySwitchLimit)
                    {
                        Debug.Assert(s_cache[last.Key] == s_cacheLast);
                        s_cache.Remove(last.Key);
                    }

                    Debug.Assert(last.Previous == null);
                    Debug.Assert(last.Next != null);
                    Debug.Assert(last.Next.Previous == last);

                    last.Next.Previous = null;
                    s_cacheLast = last.Next;

                    s_cacheCount--;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // single call site, separated out for convenience
        private static bool TryGetCacheValueAfterFirst(Key key, [NotNullWhen(true)] out Node? entry)
        {
            Debug.Assert(Monitor.IsEntered(s_cache));
            Debug.Assert(s_cacheFirst != null);
            Debug.Assert(s_cacheLast != null);

            if (s_cacheCount >= CacheDictionarySwitchLimit)
            {
                Debug.Assert(s_cache.Count == s_cacheCount);
                return s_cache.TryGetValue(key, out entry);
            }

            for (Node? current = s_cacheFirst.Previous; // s_cacheFirst already checked by caller, so skip it here
                 current != null;
                 current = current.Previous)
            {
                if (current.Key.Equals(key))
                {
                    entry = current;
                    return true;
                }
            }

            entry = null;
            return false;
        }

        private static Node? LookupCachedAndPromote(Key key)
        {
            Debug.Assert(Monitor.IsEntered(s_cache));

            Node? entry = s_cacheFirst;
            if (entry != null &&
                !entry.Key.Equals(key) && // again check this as could have been promoted by other thread
                TryGetCacheValueAfterFirst(key, out entry))
            {
                // We found the item and it wasn't the first; it needs to be promoted.

                Debug.Assert(s_cacheFirst != entry, "key should not get s_livecode_first");
                Debug.Assert(s_cacheFirst != null, "as Dict has at least one");
                Debug.Assert(s_cacheFirst.Next == null);
                Debug.Assert(s_cacheFirst.Previous != null);
                Debug.Assert(entry.Next != null, "not first so Next should exist");
                Debug.Assert(entry.Next.Previous == entry);

                if (s_cacheLast == entry)
                {
                    Debug.Assert(entry.Previous == null, "last");
                    s_cacheLast = entry.Next;
                }
                else
                {
                    Debug.Assert(entry.Previous != null, "in middle");
                    Debug.Assert(entry.Previous.Next == entry);
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

        /// <summary>Used as a key for <see cref="Node"/>.</summary>
        internal readonly struct Key : IEquatable<Key>
        {
            private readonly string _pattern;
            private readonly string _culture;
            private readonly RegexOptions _options;
            private readonly bool _hasTimeout;

            public Key(string pattern, string culture, RegexOptions options, bool hasTimeout)
            {
                Debug.Assert(pattern != null, "Pattern must be provided");
                Debug.Assert(culture != null, "Culture must be provided");

                _pattern = pattern;
                _culture = culture;
                _options = options;
                _hasTimeout = hasTimeout;
            }

            public override bool Equals(object? obj) =>
                obj is Key other && Equals(other);

            public bool Equals(Key other) =>
                _pattern.Equals(other._pattern) &&
                _culture.Equals(other._culture) &&
                _options == other._options &&
                _hasTimeout == other._hasTimeout;

            public static bool operator ==(Key left, Key right) =>
                left.Equals(right);

            public static bool operator !=(Key left, Key right) =>
                !left.Equals(right);

            public override int GetHashCode() =>
                _pattern.GetHashCode() ^
                _culture.GetHashCode() ^
                ((int)_options);
                // no need to include timeout in the hashcode; it'll almost always be the same
        }

        /// <summary>Used to cache Regex instances.</summary>
        private sealed class Node
        {
            public readonly Key Key;
            public readonly Regex Regex;
            public Node? Next;
            public Node? Previous;

            public Node(Key key, Regex regex)
            {
                Key = key;
                Regex = regex;
            }
        }
    }
}
