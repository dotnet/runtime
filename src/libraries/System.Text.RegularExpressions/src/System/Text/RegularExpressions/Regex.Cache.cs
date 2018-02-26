// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions
{
    public partial class Regex
    {
        internal static LinkedList<CachedCodeEntry> s_livecode = new LinkedList<CachedCodeEntry>();// the cache of code and factories that are currently loaded
        internal static int s_cacheSize = 15;

        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread-safety")]
        public static int CacheSize
        {
            get
            {
                return s_cacheSize;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                s_cacheSize = value;
                if (s_livecode.Count > s_cacheSize)
                {
                    lock (s_livecode)
                    {
                        while (s_livecode.Count > s_cacheSize)
                            s_livecode.RemoveLast();
                    }
                }
            }
        }

        /// <summary>
        /// Find code cache based on options+pattern
        /// </summary>
        private static CachedCodeEntry LookupCachedAndUpdate(CachedCodeEntryKey key)
        {
            lock (s_livecode)
            {
                for (LinkedListNode<CachedCodeEntry> current = s_livecode.First; current != null; current = current.Next)
                {
                    if (current.Value._key == key)
                    {
                        // If we find an entry in the cache, move it to the head at the same time.
                        s_livecode.Remove(current);
                        s_livecode.AddFirst(current);
                        return current.Value;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Add current code to the cache
        /// </summary>
        private CachedCodeEntry CacheCode(CachedCodeEntryKey key)
        {
            CachedCodeEntry newcached = null;

            lock (s_livecode)
            {
                // first look for it in the cache and move it to the head
                for (LinkedListNode<CachedCodeEntry> current = s_livecode.First; current != null; current = current.Next)
                {
                    if (current.Value._key == key)
                    {
                        s_livecode.Remove(current);
                        s_livecode.AddFirst(current);
                        return current.Value;
                    }
                }

                // it wasn't in the cache, so we'll add a new one.  Shortcut out for the case where cacheSize is zero.
                if (s_cacheSize != 0)
                {
                    newcached = new CachedCodeEntry(key, capnames, capslist, _code, caps, capsize, _runnerref, _replref);
                    s_livecode.AddFirst(newcached);
                    if (s_livecode.Count > s_cacheSize)
                        s_livecode.RemoveLast();
                }
            }

            return newcached;
        }

        /// <summary>
        /// Used as a key for CacheCodeEntry
        /// </summary>
        internal readonly struct CachedCodeEntryKey : IEquatable<CachedCodeEntryKey>
        {
            private readonly RegexOptions _options;
            private readonly string _cultureKey;
            private readonly string _pattern;

            internal CachedCodeEntryKey(RegexOptions options, string cultureKey, string pattern)
            {
                _options = options;
                _cultureKey = cultureKey;
                _pattern = pattern;
            }

            public override bool Equals(object obj)
            {
                return obj is CachedCodeEntryKey && Equals((CachedCodeEntryKey)obj);
            }

            public bool Equals(CachedCodeEntryKey other)
            {
                return this == other;
            }

            public static bool operator ==(CachedCodeEntryKey left, CachedCodeEntryKey right)
            {
                return left._options == right._options && left._cultureKey == right._cultureKey && left._pattern == right._pattern;
            }

            public static bool operator !=(CachedCodeEntryKey left, CachedCodeEntryKey right)
            {
                return !(left == right);
            }

            public override int GetHashCode()
            {
                return ((int)_options) ^ _cultureKey.GetHashCode() ^ _pattern.GetHashCode();
            }
        }

        /// <summary>
        /// Used to cache byte codes
        /// </summary>
        internal sealed class CachedCodeEntry
        {
            internal CachedCodeEntryKey _key;
            internal RegexCode _code;
            internal Hashtable _caps;
            internal Hashtable _capnames;
            internal string[] _capslist;
#if FEATURE_COMPILED
            internal RegexRunnerFactory _factory;
#endif
            internal int _capsize;
            internal ExclusiveReference _runnerref;
            internal SharedReference _replref;

            internal CachedCodeEntry(CachedCodeEntryKey key, Hashtable capnames, string[] capslist, RegexCode code, Hashtable caps, int capsize, ExclusiveReference runner, SharedReference repl)
            {
                _key = key;
                _capnames = capnames;
                _capslist = capslist;

                _code = code;
                _caps = caps;
                _capsize = capsize;

                _runnerref = runner;
                _replref = repl;
            }

#if FEATURE_COMPILED
            internal void AddCompiled(RegexRunnerFactory factory)
            {
                _factory = factory;
                _code = null;
            }
#endif
        }
    }
}
