// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Lookup.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Lookup class implements the ILookup interface. Lookup is very similar to a dictionary
    /// except multiple values are allowed to map to the same key, and null keys are supported.
    ///
    /// Support for null keys adds an issue because the Dictionary class Lookup uses for
    /// storage does not support null keys. So, we need to treat null keys separately.
    /// Unfortunately, since TKey may be a value type, we cannot test whether the key is null
    /// using the user-specified equality comparer.
    ///
    /// C# does allow us to compare the key against null using the == operator, but there is a
    /// possibility that the user's equality comparer considers null to be equal to other values.
    /// Now, MSDN documentation specifies that if IEqualityComparer.Equals(x,y) returns true, it
    /// must be the case that x and y have the same hash code, and null has no hash code. Despite
    /// that, we might as well support the use case, even if it is bad practice.
    ///
    /// The solution the Lookup class uses is to treat the key default(TKey) as a special case,
    /// and hold its associated grouping - if any - in a special field instead of inserting it
    /// into a dictionary.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TElement"></typeparam>
    internal sealed class Lookup<TKey, TElement> : ILookup<TKey, TElement> where TKey : notnull
    {
        private readonly Dictionary<TKey, IGrouping<TKey, TElement>> _dict;
        private readonly IEqualityComparer<TKey> _comparer;
        private IGrouping<TKey, TElement>? _defaultKeyGrouping;

        internal Lookup(IEqualityComparer<TKey> comparer)
        {
            _comparer = comparer;
            _dict = new Dictionary<TKey, IGrouping<TKey, TElement>>(_comparer);
        }

        public int Count
        {
            get
            {
                int count = _dict.Count;
                if (_defaultKeyGrouping != null)
                {
                    count++;
                }

                return count;
            }
        }

        // Returns an empty sequence if the key is not in the lookup.
        public IEnumerable<TElement> this[TKey key]
        {
            get
            {
                if (_comparer.Equals(key, default))
                {
                    if (_defaultKeyGrouping != null)
                    {
                        return _defaultKeyGrouping;
                    }

                    return Enumerable.Empty<TElement>();
                }
                else
                {
                    IGrouping<TKey, TElement>? grouping;
                    if (_dict.TryGetValue(key, out grouping))
                    {
                        return grouping;
                    }

                    return Enumerable.Empty<TElement>();
                }
            }
        }

        public bool Contains(TKey key)
        {
            if (_comparer.Equals(key, default))
            {
                return _defaultKeyGrouping != null;
            }
            else
            {
                return _dict.ContainsKey(key);
            }
        }

        //
        // Adds a grouping to the lookup
        //
        // Note: The grouping should be cheap to enumerate (IGrouping extends IEnumerable), as
        // it may be enumerated multiple times depending how the user manipulates the lookup.
        // Our code must guarantee that we never attempt to insert two groupings with the same
        // key into a lookup.
        //

        internal void Add(IGrouping<TKey, TElement> grouping)
        {
            if (_comparer.Equals(grouping.Key, default))
            {
                Debug.Assert(_defaultKeyGrouping == null, "Cannot insert two groupings with the default key into a lookup.");

                _defaultKeyGrouping = grouping;
            }
            else
            {
                Debug.Assert(!_dict.ContainsKey(grouping.Key));

                _dict.Add(grouping.Key, grouping);
            }
        }

        public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
        {
            // First iterate over the groupings in the dictionary, and then over the default-key
            // grouping, if there is one.

            foreach (IGrouping<TKey, TElement> grouping in _dict.Values)
            {
                yield return grouping;
            }

            if (_defaultKeyGrouping != null)
            {
                yield return _defaultKeyGrouping;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<IGrouping<TKey, TElement>>)this).GetEnumerator();
        }
    }
}
