// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic
{
    internal sealed class BidirectionalDictionary<T1, T2> : IEnumerable<KeyValuePair<T1, T2>>
            where T1 : notnull
            where T2 : notnull
    {
        private readonly Dictionary<T1, T2> _forward;
        private readonly Dictionary<T2, T1> _backward;

        public BidirectionalDictionary(int capacity)
        {
            _forward = new Dictionary<T1, T2>(capacity);
            _backward = new Dictionary<T2, T1>(capacity);
        }

        public int Count
        {
            get
            {
                Debug.Assert(_forward.Count == _backward.Count, "both the dictionaries must have the same number of elements");
                return _forward.Count;
            }
        }

        public void Add(T1 item1, T2 item2)
        {
            Debug.Assert(!_backward.ContainsKey(item2), "No added item1 should ever have existing item2");
            _forward.Add(item1, item2);
            _backward.Add(item2, item1);
        }

        public bool TryGetForward(T1 item1, [MaybeNullWhen(false)] out T2 item2)
        {
            return _forward.TryGetValue(item1, out item2);
        }

        public bool TryGetBackward(T2 item2, [MaybeNullWhen(false)] out T1 item1)
        {
            return _backward.TryGetValue(item2, out item1);
        }

        public Dictionary<T1, T2>.Enumerator GetEnumerator()
        {
            return _forward.GetEnumerator();
        }

        IEnumerator<KeyValuePair<T1, T2>> IEnumerable<KeyValuePair<T1, T2>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
