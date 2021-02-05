// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;

namespace System.Text.RegularExpressions
{
    public partial class Regex
    {
        public static int CacheSize
        {
            get => RegexCache.MaxCacheSize;
            set
            {
                if (value < 0)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
                }

                RegexCache.MaxCacheSize = value;
            }
        }
    }

    /// <summary>Cache used to store Regex instances used by the static methods on Regex.</summary>
    internal sealed class RegexCache
    {
        // The implementation is optimized to make cache hits fast and lock-free, only taking a global lock
        // when adding a new Regex to the cache.  Previous implementations of the cache took a global lock
        // on all accesses, negatively impacting scalability, in order to minimize costs when the cache
        // limit was hit and items needed to be dropped.  In such situations, however, we're having to
        // pay the relatively hefty cost of creating a new Regex, anyway, and if the consuming app cares
        // about such costs, it should either increase Regex.CacheSize or do its own Regex instance caching.

        /// <summary>The default maximum number of items to store in the cache.</summary>
        private const int DefaultMaxCacheSize = 15;
        /// <summary>The maximum number of cached items to examine when we need to replace an existing one in the cache with a new one.</summary>
        /// <remarks>This is a somewhat arbitrary value, chosen to be small but at least as large as DefaultMaxCacheSize.</remarks>
        private const int MaxExamineOnDrop = 30;

        /// <summary>A read-through cache of one element, representing the most recently used regular expression.</summary>
        private static volatile Node? s_lastAccessed;
        /// <summary>The thread-safe dictionary storing all the items in the cache.</summary>
        /// <remarks>
        /// The concurrency level is initialized to 1 as we're using our own global lock for all mutations, so we don't need ConcurrentDictionary's
        /// striped locking.  Capacity is initialized to 31, which is the same as (the private) ConcurrentDictionary.DefaultCapacity.
        /// </remarks>
        private static readonly ConcurrentDictionary<Key, Node> s_cacheDictionary = new ConcurrentDictionary<Key, Node>(concurrencyLevel: 1, capacity: 31);
        /// <summary>A list of all the items in the cache.  Protected by <see cref="SyncObj"/>.</summary>
        private static readonly List<Node> s_cacheList = new List<Node>(DefaultMaxCacheSize);
        /// <summary>Random number generator used to examine a subset of items when we need to drop one from a large list.  Protected by <see cref="SyncObj"/>.</summary>
        private static readonly Random s_random = new Random();
        /// <summary>The current maximum number of items allowed in the cache.  This rarely changes.  Mostly protected by <see cref="SyncObj"/>.</summary>
        private static int s_maxCacheSize = DefaultMaxCacheSize;

        /// <summary>Lock used to protect shared state on mutations.</summary>
        private static object SyncObj => s_cacheDictionary;

        /// <summary>Gets or sets the maximum size of the cache.</summary>
        public static int MaxCacheSize
        {
            get => s_maxCacheSize;
            set
            {
                Debug.Assert(value >= 0);

                lock (SyncObj)
                {
                    // Store the new max cache size
                    s_maxCacheSize = value;

                    if (value == 0)
                    {
                        // If the value is being changed to zero, just clear out the cache.
                        s_cacheDictionary.Clear();
                        s_cacheList.Clear();
                        s_lastAccessed = null;
                    }
                    else if (value < s_cacheList.Count)
                    {
                        // If the value is being changed to less than the number of items we're currently storing,
                        // sort the entries descending by last access stamp, and remove the excess.  This is expensive, but
                        // this should be exceedingly rare, as CacheSize is generally set once (if at all) and then left unchanged.
                        s_cacheList.Sort((n1, n2) => Volatile.Read(ref n2.LastAccessStamp).CompareTo(Volatile.Read(ref n1.LastAccessStamp)));
                        s_lastAccessed = s_cacheList[0];
                        for (int i = value; i < s_cacheList.Count; i++)
                        {
                            s_cacheDictionary.TryRemove(s_cacheList[i].Key, out _);
                        }
                        s_cacheList.RemoveRange(value, s_cacheList.Count - value);

                        Debug.Assert(s_cacheList.Count == value);
                        Debug.Assert(s_cacheDictionary.Count == value);
                    }
                }
            }
        }

        public static Regex GetOrAdd(string pattern)
        {
            // Does not delegate to GetOrAdd(..., RegexOptions, ...) in order to avoid having
            // a statically-reachable path to the 'new Regex(..., RegexOptions, ...)', which
            // will force the Regex compiler to be reachable and thus rooted for the linker.

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
            long lastAccessedStamp = 0;

            // We optimize for repeated usage of the same regular expression over and over,
            // by having a fast-path that stores the most recently used instance.  Check
            // to see if that instance is the one we want; if it is, we're done.
            Node? lastAccessed = s_lastAccessed;
            if (lastAccessed != null)
            {
                if (lastAccessed.Key.Equals(key))
                {
                    regex = lastAccessed.Regex;
                    return true;
                }

                // We had a last accessed item, but it didn't match the one being requested.
                // In case we need to replace the last accessed node, remember this one's stamp;
                // we'll use it to compute the new access value for the new node replacing it.
                lastAccessedStamp = Volatile.Read(ref lastAccessed.LastAccessStamp);
            }

            // Now consult the full cache.
            if (s_maxCacheSize != 0 && // hot-read of s_maxCacheSize to try to avoid the cost of the dictionary lookup if the cache is disabled
                s_cacheDictionary.TryGetValue(key, out Node? node))
            {
                // We found our item in the cache. Make this node's last access stamp one higher than
                // the previous one.  It's ok if multiple threads racing to update the last access cause
                // multiple nodes to have the same value; it's an approximate value meant only to help
                // remove the least valuable items when an item needs to be dropped from the cache.  We
                // do, however, need to read the old value and write the new value using Volatile.Read/Write,
                // in order to prevent tearing of the 64-bit value on 32-bit platforms, and to help ensure
                // that another thread subsequently sees this updated value.
                Volatile.Write(ref node.LastAccessStamp, lastAccessedStamp + 1);

                //  Update our fast-path single-field cache.
                s_lastAccessed = node;

                // Return the cached regex.
                regex = node.Regex;
                return true;
            }

            // Not in the cache.
            regex = null;
            return false;
        }

        private static void Add(Key key, Regex regex)
        {
            lock (SyncObj)
            {
                Debug.Assert(s_cacheList.Count == s_cacheDictionary.Count);

                // If the cache has been disabled, there's nothing to add. And if between just checking
                // the cache in the caller and taking the lock, another thread could have added the regex.
                // If that occurred, there's also nothing to add, and we don't bother to update any of the
                // time stamp / fast-path field information, because hitting this race condition means it
                // was just updated, and we gain little by updating it again.
                if (s_maxCacheSize == 0 || s_cacheDictionary.TryGetValue(key, out _))
                {
                    return;
                }

                // If the cache is full, remove an item to make room for the new one.
                if (s_cacheList.Count == s_maxCacheSize)
                {
                    int itemsToExamine;
                    bool useRandom;

                    if (s_maxCacheSize <= MaxExamineOnDrop)
                    {
                        // Our maximum cache size is <= the number of items we're willing to examine (which is kept small simply
                        // to avoid spending a lot of time).  As such, we can just examine the whole list.
                        itemsToExamine = s_cacheList.Count;
                        useRandom = false;
                    }
                    else
                    {
                        // Our maximum cache size is > the number of items we're willing to examine, so we'll instead
                        // examine a random subset.  This isn't perfect: if the size of the list is only a tiny bit
                        // larger than the max we're willing to examine, there's a good chance we'll look at some of
                        // the same items twice.  That's fine; this doesn't need to be perfect.  We do not need a perfect LRU
                        // cache, just one that generally gets rid of older things when new things come in.
                        itemsToExamine = MaxExamineOnDrop;
                        useRandom = true;
                    }

                    // Pick the first item to use as the min.
                    int minListIndex = useRandom ? s_random.Next(s_cacheList.Count) : 0;
                    long min = Volatile.Read(ref s_cacheList[minListIndex].LastAccessStamp);

                    // Now examine the rest, keeping track of the smallest access stamp we find.
                    for (int i = 1; i < itemsToExamine; i++)
                    {
                        int nextIndex = useRandom ? s_random.Next(s_cacheList.Count) : i;
                        long next = Volatile.Read(ref s_cacheList[nextIndex].LastAccessStamp);
                        if (next < min)
                        {
                            minListIndex = nextIndex;
                            min = next;
                        }
                    }

                    // Remove the key found to have the smallest access stamp.
                    s_cacheDictionary.TryRemove(s_cacheList[minListIndex].Key, out _);
                    s_cacheList.RemoveAt(minListIndex);
                }

                // Finally add the regex.
                var node = new Node(key, regex);
                s_lastAccessed = node;
                s_cacheList.Add(node);
                s_cacheDictionary.TryAdd(key, node);

                Debug.Assert(s_cacheList.Count <= s_maxCacheSize);
                Debug.Assert(s_cacheList.Count == s_cacheDictionary.Count);
            }
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

            public override bool Equals([NotNullWhen(true)] object? obj) =>
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

        /// <summary>Node for a cached Regex instance.</summary>
        private sealed class Node
        {
            /// <summary>The key associated with this cached instance.</summary>
            public readonly Key Key;
            /// <summary>The cached Regex instance.</summary>
            public readonly Regex Regex;
            /// <summary>A "time" stamp representing the approximate last access time for this Regex.</summary>
            public long LastAccessStamp;

            public Node(Key key, Regex regex)
            {
                Key = key;
                Regex = regex;
            }
        }
    }
}
