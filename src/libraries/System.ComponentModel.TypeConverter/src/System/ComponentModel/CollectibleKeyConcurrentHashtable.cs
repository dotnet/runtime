// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.ComponentModel
{
    /// <summary>
    /// Concurrent dictionary that maps MemberInfo object key to an object.
    /// Uses ConditionalWeakTable for the collectible keys (if MemberInfo.IsCollectible is true) and
    /// ConcurrentDictionary for non-collectible keys.
    /// </summary>
    internal sealed class CollectibleKeyConcurrentHashtable<TKey, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : MemberInfo
        where TValue : class?
    {
        private readonly ConcurrentDictionary<TKey, TValue> _defaultTable = new ConcurrentDictionary<TKey, TValue>();
        private readonly ConditionalWeakTable<TKey, object?> _collectibleTable = new ConditionalWeakTable<TKey, object?>();

        public TValue? this[TKey key]
        {
            get
            {
                return TryGetValue(key, out TValue? value) ? value : default;
            }

            set
            {
                if (!key.IsCollectible)
                {
                    _defaultTable[key] = value!;
                }
                else
                {
                    _collectibleTable.AddOrUpdate(key, value);
                }
            }
        }

        public bool ContainsKey(TKey key)
        {
            return !key.IsCollectible ? _defaultTable.ContainsKey(key) : _collectibleTable.TryGetValue(key, out _);
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (!key.IsCollectible)
                return _defaultTable.TryGetValue(key, out value);

            if (_collectibleTable.TryGetValue(key, out object? valueObj) && valueObj != null)
            {
                value = (TValue)valueObj;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryAdd(TKey key, TValue value)
        {
            return !key.IsCollectible
                ? _defaultTable.TryAdd(key, value)
                : _collectibleTable.TryAdd(key, value);
        }

        public void Clear()
        {
            _defaultTable.Clear();
            _collectibleTable.Clear();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() =>
            new Enumerator(_defaultTable.GetEnumerator(), ((IEnumerable<KeyValuePair<TKey, object?>>)_collectibleTable).GetEnumerator());

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly IEnumerator<KeyValuePair<TKey, TValue>> _defaultEnumerator;
            private readonly IEnumerator<KeyValuePair<TKey, object?>> _collectibleEnumerator;
            private bool _enumeratingCollectibleEnumerator;

            public Enumerator(IEnumerator<KeyValuePair<TKey, TValue>> defaultEnumerator, IEnumerator<KeyValuePair<TKey, object?>> collectibleEnumerator)
            {
                _defaultEnumerator = defaultEnumerator;
                _collectibleEnumerator = collectibleEnumerator;
                _enumeratingCollectibleEnumerator = false;
            }

            public KeyValuePair<TKey, TValue> Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                _defaultEnumerator.Dispose();
                _collectibleEnumerator.Dispose();
            }

            public bool MoveNext()
            {
                if (!_enumeratingCollectibleEnumerator && _defaultEnumerator.MoveNext())
                {
                    Current = _defaultEnumerator.Current;
                    return true;
                }

                _enumeratingCollectibleEnumerator = true;

                while (_collectibleEnumerator.MoveNext())
                {
                    if (_collectibleEnumerator.Current.Value is TValue value)
                    {
                        Current = new KeyValuePair<TKey, TValue>(_collectibleEnumerator.Current.Key, value);
                        return true;
                    }
                }

                Current = default;
                return false;
            }

            public void Reset()
            {
                _defaultEnumerator.Reset();
                _collectibleEnumerator.Reset();
                _enumeratingCollectibleEnumerator = false;
                Current = default;
            }
        }
    }
}
