// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Collections.Immutable
{
    internal class MultiSet<T>
    {
        private readonly Dictionary<NullableKeyWrapper, int> _dictionary;

        public MultiSet(IEqualityComparer<T>? equalityComparer)
        {
            _dictionary = new Dictionary<NullableKeyWrapper, int>(new NullableKeyWrapperEqualityComparer(equalityComparer ?? EqualityComparer<T>.Default));
        }

        public void Add(T item)
        {
#if NET6_0_OR_GREATER
            ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(_dictionary, item, out _);
            count++;
#else
            _dictionary[item] = _dictionary.TryGetValue(item, out int count) ? count + 1 : 1;
#endif
        }

        public bool TryRemove(T item)
        {
#if NET6_0_OR_GREATER
            ref int count = ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary, item);
            if (Unsafe.IsNullRef(ref count) || count == 0)
#else
            if (!_dictionary.TryGetValue(item, out int count) || count == 0)
#endif
            {
                return false;
            }

#if NET6_0_OR_GREATER
            count--;
#else
            _dictionary[item] = count - 1;
#endif
            return true;
        }

        private readonly struct NullableKeyWrapper
        {
            public readonly T Key;

            public static implicit operator NullableKeyWrapper(T key)
            {
                return new NullableKeyWrapper(key);
            }

            private NullableKeyWrapper(T key)
            {
                Key = key;
            }
        }

        private class NullableKeyWrapperEqualityComparer : IEqualityComparer<NullableKeyWrapper>
        {
            private readonly IEqualityComparer<T> _keyComparer;

            public NullableKeyWrapperEqualityComparer(IEqualityComparer<T> keyComparer)
            {
                _keyComparer = keyComparer;
            }

            public int GetHashCode(NullableKeyWrapper obj)
            {
                return obj.Key == null ? 0 : _keyComparer.GetHashCode(obj.Key);
            }

            public bool Equals(NullableKeyWrapper x, NullableKeyWrapper y)
            {
                return _keyComparer.Equals(x.Key, y.Key);
            }
        }
    }
}
