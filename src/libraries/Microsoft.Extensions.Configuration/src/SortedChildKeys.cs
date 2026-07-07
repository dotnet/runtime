// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// The accumulated immediate child keys threaded through the providers by
    /// <see cref="IConfigurationProvider.GetChildKeys(IEnumerable{string}, string?)"/>. A case-insensitive
    /// <see cref="HashSet{T}"/> de-duplicates the keys while a parallel <see cref="List{T}"/> holds them; the list is
    /// sorted with <see cref="ConfigurationKeyComparer"/> lazily, at most once, the first time the accumulator is
    /// enumerated after a change. Not thread-safe; each aggregation builds its own instance.
    /// </summary>
    internal sealed class SortedChildKeys : IEnumerable<string>
    {
        private readonly HashSet<string> _set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _list = new List<string>();
#if NET
        private readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> _lookup;
#endif
        private bool _sorted = true;

        internal SortedChildKeys()
        {
#if NET
            _lookup = _set.GetAlternateLookup<ReadOnlySpan<char>>();
#endif
        }

        internal SortedChildKeys(IEnumerable<string> keys) : this()
        {
            UnionWith(keys);
        }

        /// <summary>Gets the number of distinct child keys accumulated so far.</summary>
        public int Count => _list.Count;

        /// <summary>
        /// Adds the immediate child segment <c>key.AsSpan(start, length)</c>. Where span alternate lookups are
        /// available the segment is de-duplicated by span, so a duplicate never allocates a substring; only a genuinely
        /// new segment is materialized (and a segment that spans a whole key reuses that key).
        /// </summary>
        /// <param name="key">The full key containing the segment.</param>
        /// <param name="start">The index in <paramref name="key"/> at which the segment starts.</param>
        /// <param name="length">The length of the segment.</param>
        internal void AddSegment(string key, int start, int length)
        {
#if NET
            if (!_lookup.Contains(key.AsSpan(start, length)))
            {
                string item = start == 0 && length == key.Length ? key : key.Substring(start, length);
                _set.Add(item);
                _list.Add(item);
                _sorted = false;
            }
#else
            string item = start == 0 && length == key.Length ? key : key.Substring(start, length);
            if (_set.Add(item))
            {
                _list.Add(item);
                _sorted = false;
            }
#endif
        }

        /// <summary>Adds every key in <paramref name="keys"/>, de-duplicating against the current contents.</summary>
        /// <param name="keys">The keys to add.</param>
        internal void UnionWith(IEnumerable<string> keys)
        {
            foreach (string key in keys)
            {
                if (_set.Add(key))
                {
                    _list.Add(key);
                    _sorted = false;
                }
            }
        }

        /// <summary>
        /// Replaces the contents with <paramref name="keys"/>, de-duplicated. The keys come from a provider that
        /// returned a sequence other than this accumulator; such a provider produces them in its own (sorted) order, so
        /// they are taken as already sorted and are not re-sorted. This mirrors a provider chain that never re-orders a
        /// preceding provider's result.
        /// </summary>
        /// <param name="keys">The keys to replace the current contents with.</param>
        internal void Overwrite(IEnumerable<string> keys)
        {
            _set.Clear();
            _list.Clear();

            // Size the storage up front for a foreign result of known length, but only while the list is still at its
            // default (zero) capacity. The classic provider chain returns each provider's own keys concatenated with the
            // earlier keys, so a later overwrite of the same accumulator receives a longer, duplicate-inflated sequence;
            // guarding on the capacity keeps us from expanding to that inflated upper bound and instead reuses the
            // capacity the first overwrite already grew.
            if (_list.Capacity == 0 && keys is ICollection<string> collection)
            {
                int capacity = collection.Count;
                _list.Capacity = capacity;
#if NET
                _set.EnsureCapacity(capacity);
#endif
            }

            foreach (string key in keys)
            {
                if (_set.Add(key))
                {
                    _list.Add(key);
                }
            }

            _sorted = true;
        }

        /// <summary>Returns an enumerator that yields the keys in sorted order.</summary>
        /// <returns>An enumerator over the sorted keys.</returns>
        public IEnumerator<string> GetEnumerator()
        {
            EnsureSorted();
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Sorts the keys on demand, once, so repeated enumeration does not re-sort. Uses the same comparer as the
        // pre-existing per-provider sort so the child order is identical to what callers observed before.
        private void EnsureSorted()
        {
            if (!_sorted)
            {
                _list.Sort(ConfigurationKeyComparer.Comparison);
                _sorted = true;
            }
        }
    }
}
