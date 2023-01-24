// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading
{
    /// <summary>Represents ambient data that is local to a given asynchronous control flow, such as an asynchronous method.</summary>
    /// <typeparam name="T">The type of the ambient data.</typeparam>
    public sealed class AsyncLocal<T> : IAsyncLocal
    {
        private readonly Action<AsyncLocalValueChangedArgs<T>>? _valueChangedHandler;

        /// <summary>Instantiates an <see cref="AsyncLocal{T}"/> instance that does not receive change notifications.</summary>
        public AsyncLocal()
        {
        }

        /// <summary>Instantiates an <see cref="AsyncLocal{T}"/> instance that receives change notifications.</summary>
        /// <param name="valueChangedHandler">The delegate that is called whenever the current value changes on any thread.</param>
        public AsyncLocal(Action<AsyncLocalValueChangedArgs<T>>? valueChangedHandler)
        {
            _valueChangedHandler = valueChangedHandler;
        }

        /// <summary>Gets or sets the value of the ambient data.</summary>
        /// <value>The value of the ambient data. If no value has been set, the returned value is default(T).</value>
        [MaybeNull]
        public T Value
        {
            get
            {
                object? value = ExecutionContext.GetLocalValue(this);
                if (typeof(T).IsValueType && value is null)
                {
                    return default;
                }

                return (T)value!;
            }
            set
            {
                ExecutionContext.SetLocalValue(this, value, _valueChangedHandler is not null);
            }
        }

        void IAsyncLocal.OnValueChanged(object? previousValueObj, object? currentValueObj, bool contextChanged)
        {
            Debug.Assert(_valueChangedHandler is not null);
            T previousValue = previousValueObj is null ? default! : (T)previousValueObj;
            T currentValue = currentValueObj is null ? default! : (T)currentValueObj;
            _valueChangedHandler(new AsyncLocalValueChangedArgs<T>(previousValue, currentValue, contextChanged));
        }
    }

    /// <summary>Interface to allow non-generic code in ExecutionContext to call into the generic <see cref="AsyncLocal{T}"/> type.</summary>
    internal interface IAsyncLocal
    {
        void OnValueChanged(object? previousValue, object? currentValue, bool contextChanged);
    }

    /// <summary>The class that provides data change information to <see cref="AsyncLocal{T}"/> instances that register for change notifications.</summary>
    /// <typeparam name="T">The type of the data.</typeparam>
    public readonly struct AsyncLocalValueChangedArgs<T>
    {
        /// <summary>Gets the data's previous value.</summary>
        public T? PreviousValue { get; }

        /// <summary>Gets the data's current value.</summary>
        public T? CurrentValue { get; }

        /// <summary>Returns a value that indicates whether the value changes because of a change of execution context.</summary>
        /// <value>true if the value changed because of a change of execution context; otherwise, false.</value>
        public bool ThreadContextChanged { get; }

        internal AsyncLocalValueChangedArgs(T? previousValue, T? currentValue, bool contextChanged)
        {
            PreviousValue = previousValue!;
            CurrentValue = currentValue!;
            ThreadContextChanged = contextChanged;
        }
    }

    /// <summary>
    /// Interface used to store an IAsyncLocal => object mapping in ExecutionContext.
    /// Implementations are specialized based on the number of elements in the immutable
    /// map in order to minimize memory consumption and look-up times.
    /// </summary>
    internal interface IAsyncLocalValueMap
    {
        bool TryGetValue(IAsyncLocal key, out object? value);
        IAsyncLocalValueMap Set(IAsyncLocal key, object? value, bool treatNullValueAsNonexistent);
    }

    /// <summary>Utility functions for getting/creating instances of IAsyncLocalValueMap</summary>
    internal static class AsyncLocalValueMap
    {
        public static IAsyncLocalValueMap Empty { get; } = new EmptyAsyncLocalValueMap();

        public static bool IsEmpty(IAsyncLocalValueMap asyncLocalValueMap)
        {
            Debug.Assert(asyncLocalValueMap is not null);
            Debug.Assert(asyncLocalValueMap == Empty || asyncLocalValueMap.GetType() != typeof(EmptyAsyncLocalValueMap));

            return asyncLocalValueMap == Empty;
        }

        public static IAsyncLocalValueMap Create(IAsyncLocal key, object? value, bool treatNullValueAsNonexistent)
        {
            // If the value isn't null or a null value may not be treated as nonexistent, then create a new one-element map
            // to store the key/value pair.  Otherwise, use the empty map.
            return value is not null || !treatNullValueAsNonexistent ?
                new OneElementAsyncLocalValueMap(KeyValuePair.Create(key, value)) :
                Empty;
        }

        // Instance without any key/value pairs.  Used as a singleton/
        private sealed class EmptyAsyncLocalValueMap : IAsyncLocalValueMap
        {
            public IAsyncLocalValueMap Set(IAsyncLocal key, object? value, bool treatNullValueAsNonexistent)
            {
                // If the value isn't null or a null value may not be treated as nonexistent, then create a new one-element map
                // to store the key/value pair.  Otherwise, use the empty map.
                return value is not null || !treatNullValueAsNonexistent ?
                    new OneElementAsyncLocalValueMap(KeyValuePair.Create(key, value)) :
                    this;
            }

            public bool TryGetValue(IAsyncLocal key, out object? value)
            {
                value = null;
                return false;
            }
        }

        /// <summary>Instance with one key/value pair.</summary>
        private sealed class OneElementAsyncLocalValueMap : IAsyncLocalValueMap
        {
            private readonly KeyValuePair<IAsyncLocal, object?> _item0;

            public OneElementAsyncLocalValueMap(KeyValuePair<IAsyncLocal, object?> item0)
            {
                _item0 = item0;
            }

            public IAsyncLocalValueMap Set(IAsyncLocal key, object? value, bool treatNullValueAsNonexistent)
            {
                if (value is not null || !treatNullValueAsNonexistent)
                {
                    // If the key matches one already contained in this map, then create a new one-element map with the updated
                    // value, otherwise create a two-element map with the additional key/value.
                    KeyValuePair<IAsyncLocal, object?> newItem = KeyValuePair.Create(key, value);
                    return ReferenceEquals(key, _item0.Key) ?
                        new OneElementAsyncLocalValueMap(newItem) :
                        new TwoElementAsyncLocalValueMap(_item0, newItem);
                }
                else
                {
                    // If the key exists in this map, remove it by downgrading to an empty map.  Otherwise, there's nothing to
                    // add or remove, so just return this map.
                    return ReferenceEquals(key, _item0.Key) ?
                        Empty :
                        this;
                }
            }

            public bool TryGetValue(IAsyncLocal key, out object? value)
            {
                if (ReferenceEquals(key, _item0.Key))
                {
                    value = _item0.Value;
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }
        }

        /// <summary>Instance with two key/value pairs.</summary>
        private sealed class TwoElementAsyncLocalValueMap : IAsyncLocalValueMap
        {
            private readonly KeyValuePair<IAsyncLocal, object?> _item0;
            private readonly KeyValuePair<IAsyncLocal, object?> _item1;

            public TwoElementAsyncLocalValueMap(
                KeyValuePair<IAsyncLocal, object?> item0,
                KeyValuePair<IAsyncLocal, object?> item1)
            {
                _item0 = item0;
                _item1 = item1;
            }

            public IAsyncLocalValueMap Set(IAsyncLocal key, object? value, bool treatNullValueAsNonexistent)
            {
                if (value is not null || !treatNullValueAsNonexistent)
                {
                    // If the key matches one already contained in this map, then create a new two-element map with the updated
                    // value, otherwise create a three-element map with the additional key/value.
                    KeyValuePair<IAsyncLocal, object?> newItem = KeyValuePair.Create(key, value);
                    return
                        ReferenceEquals(key, _item0.Key) ? new TwoElementAsyncLocalValueMap(newItem, _item1) :
                        ReferenceEquals(key, _item1.Key) ? new TwoElementAsyncLocalValueMap(_item0, newItem) :
                        new ThreeElementAsyncLocalValueMap(_item0, _item1, newItem);
                }
                else
                {
                    // If the key exists in this map, remove it by downgrading to a one-element map without the key.  Otherwise,
                    // there's nothing to add or remove, so just return this map.
                    return
                        ReferenceEquals(key, _item0.Key) ? new OneElementAsyncLocalValueMap(_item1) :
                        ReferenceEquals(key, _item1.Key) ? new OneElementAsyncLocalValueMap(_item0) :
                        this;
                }
            }

            public bool TryGetValue(IAsyncLocal key, out object? value)
            {
                if (ReferenceEquals(key, _item0.Key))
                {
                    value = _item0.Value;
                    return true;
                }
                else if (ReferenceEquals(key, _item1.Key))
                {
                    value = _item1.Value;
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }
        }

        /// <summary>Instance with three key/value pairs.</summary>
        private sealed class ThreeElementAsyncLocalValueMap : IAsyncLocalValueMap
        {
            private readonly KeyValuePair<IAsyncLocal, object?> _item0;
            private readonly KeyValuePair<IAsyncLocal, object?> _item1;
            private readonly KeyValuePair<IAsyncLocal, object?> _item2;

            public ThreeElementAsyncLocalValueMap(
                KeyValuePair<IAsyncLocal, object?> item0,
                KeyValuePair<IAsyncLocal, object?> item1,
                KeyValuePair<IAsyncLocal, object?> item2)
            {
                _item0 = item0;
                _item1 = item1;
                _item2 = item2;
            }

            public IAsyncLocalValueMap Set(IAsyncLocal key, object? value, bool treatNullValueAsNonexistent)
            {
                if (value is not null || !treatNullValueAsNonexistent)
                {
                    // If the key matches one already contained in this map, then create a new three-element map with the updated
                    // value, otherwise create a three-element map with the additional key/value.
                    KeyValuePair<IAsyncLocal, object?> newItem = KeyValuePair.Create(key, value);
                    return
                        ReferenceEquals(key, _item0.Key) ? new ThreeElementAsyncLocalValueMap(newItem, _item1, _item2) :
                        ReferenceEquals(key, _item1.Key) ? new ThreeElementAsyncLocalValueMap(_item0, newItem, _item2) :
                        ReferenceEquals(key, _item2.Key) ? new ThreeElementAsyncLocalValueMap(_item0, _item1, newItem) :
                        new FourElementAsyncLocalValueMap(_item0, _item1, _item2, newItem);
                }
                else
                {
                    // If the key exists in this map, remove it by downgrading to a one-element map without the key.  Otherwise,
                    // there's nothing to add or remove, so just return this map.
                    return
                        ReferenceEquals(key, _item0.Key) ? new TwoElementAsyncLocalValueMap(_item1, _item2) :
                        ReferenceEquals(key, _item1.Key) ? new TwoElementAsyncLocalValueMap(_item0, _item2) :
                        ReferenceEquals(key, _item2.Key) ? new TwoElementAsyncLocalValueMap(_item0, _item1) :
                        this;
                }
            }

            public bool TryGetValue(IAsyncLocal key, out object? value)
            {
                if (ReferenceEquals(key, _item0.Key))
                {
                    value = _item0.Value;
                    return true;
                }
                else if (ReferenceEquals(key, _item1.Key))
                {
                    value = _item1.Value;
                    return true;
                }
                else if (ReferenceEquals(key, _item2.Key))
                {
                    value = _item2.Value;
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }
        }

        /// <summary>Instance with four key/value pairs.</summary>
        private sealed class FourElementAsyncLocalValueMap : IAsyncLocalValueMap
        {
            private readonly KeyValuePair<IAsyncLocal, object?> _item0;
            private readonly KeyValuePair<IAsyncLocal, object?> _item1;
            private readonly KeyValuePair<IAsyncLocal, object?> _item2;
            private readonly KeyValuePair<IAsyncLocal, object?> _item3;

            public FourElementAsyncLocalValueMap(
                KeyValuePair<IAsyncLocal, object?> item0,
                KeyValuePair<IAsyncLocal, object?> item1,
                KeyValuePair<IAsyncLocal, object?> item2,
                KeyValuePair<IAsyncLocal, object?> item3)
            {
                _item0 = item0;
                _item1 = item1;
                _item2 = item2;
                _item3 = item3;
            }

            public IAsyncLocalValueMap Set(IAsyncLocal key, object? value, bool treatNullValueAsNonexistent)
            {
                if (value is not null || !treatNullValueAsNonexistent)
                {
                    // If the key matches one already contained in this map, then create a new four-element map with the updated value.
                    KeyValuePair<IAsyncLocal, object?> newItem = KeyValuePair.Create(key, value);
                    return
                        ReferenceEquals(key, _item0.Key) ? new FourElementAsyncLocalValueMap(newItem, _item1, _item2, _item3) :
                        ReferenceEquals(key, _item1.Key) ? new FourElementAsyncLocalValueMap(_item0, newItem, _item2, _item3) :
                        ReferenceEquals(key, _item2.Key) ? new FourElementAsyncLocalValueMap(_item0, _item1, newItem, _item3) :
                        ReferenceEquals(key, _item3.Key) ? new FourElementAsyncLocalValueMap(_item0, _item1, _item2, newItem) :
                        new MultiElementAsyncLocalValueMap(new[] { _item0, _item1, _item2, _item3, newItem }); // upgrade to a multi
                }
                else
                {
                    // If the key exists in this map, remove it by downgrading to a two-element map without the key.  Otherwise,
                    // there's nothing to add or remove, so just return this map.
                    return
                        ReferenceEquals(key, _item0.Key) ? new ThreeElementAsyncLocalValueMap(_item1, _item2, _item3) :
                        ReferenceEquals(key, _item1.Key) ? new ThreeElementAsyncLocalValueMap(_item0, _item2, _item3) :
                        ReferenceEquals(key, _item2.Key) ? new ThreeElementAsyncLocalValueMap(_item0, _item1, _item3) :
                        ReferenceEquals(key, _item3.Key) ? new ThreeElementAsyncLocalValueMap(_item0, _item1, _item2) :
                        this;
                }
            }

            public bool TryGetValue(IAsyncLocal key, out object? value)
            {
                if (ReferenceEquals(key, _item0.Key))
                {
                    value = _item0.Value;
                    return true;
                }
                else if (ReferenceEquals(key, _item1.Key))
                {
                    value = _item1.Value;
                    return true;
                }
                else if (ReferenceEquals(key, _item2.Key))
                {
                    value = _item2.Value;
                    return true;
                }
                else if (ReferenceEquals(key, _item3.Key))
                {
                    value = _item3.Value;
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }
        }

        /// <summary>Instance with up to 16 key/value pairs.</summary>
        private sealed class MultiElementAsyncLocalValueMap : IAsyncLocalValueMap
        {
            internal const int MaxMultiElements = 16;
            private readonly KeyValuePair<IAsyncLocal, object?>[] _keyValues;

            internal MultiElementAsyncLocalValueMap(KeyValuePair<IAsyncLocal, object?>[] keyValues)
            {
                Debug.Assert(keyValues.Length is >= 5 and <= MaxMultiElements);
                _keyValues = keyValues;
            }

            public IAsyncLocalValueMap Set(IAsyncLocal key, object? value, bool treatNullValueAsNonexistent)
            {
                // Find the key in this map.
                for (int i = 0; i < _keyValues.Length; i++)
                {
                    if (ReferenceEquals(key, _keyValues[i].Key))
                    {
                        // The key is in the map.
                        if (value is not null || !treatNullValueAsNonexistent)
                        {
                            // Create a new map of the same size that has all of the same pairs, with this new key/value pair overwriting the old.
                            KeyValuePair<IAsyncLocal, object?>[] newValues = _keyValues.AsSpan().ToArray();
                            newValues[i] = KeyValuePair.Create(key, value);
                            return new MultiElementAsyncLocalValueMap(newValues);
                        }
                        else if (_keyValues.Length == 5)
                        {
                            // We only have five elements, one of which we're removing, so downgrade to a four-element map,
                            // without the matching element.
                            return i switch
                            {
                                0 => new FourElementAsyncLocalValueMap(_keyValues[1], _keyValues[2], _keyValues[3], _keyValues[4]),
                                1 => new FourElementAsyncLocalValueMap(_keyValues[0], _keyValues[2], _keyValues[3], _keyValues[4]),
                                2 => new FourElementAsyncLocalValueMap(_keyValues[0], _keyValues[1], _keyValues[3], _keyValues[4]),
                                3 => new FourElementAsyncLocalValueMap(_keyValues[0], _keyValues[1], _keyValues[2], _keyValues[4]),
                                _ => new FourElementAsyncLocalValueMap(_keyValues[0], _keyValues[1], _keyValues[2], _keyValues[3]),
                            };
                        }
                        else
                        {
                            // We have enough elements remaining to warrant a multi map.  Create a new one and copy all of the
                            // elements from this one, except the one to be removed.
                            var newValues = new KeyValuePair<IAsyncLocal, object?>[_keyValues.Length - 1];
                            if (i != 0) Array.Copy(_keyValues, newValues, i);
                            if (i != _keyValues.Length - 1) Array.Copy(_keyValues, i + 1, newValues, i, _keyValues.Length - i - 1);
                            return new MultiElementAsyncLocalValueMap(newValues);
                        }
                    }
                }

                // The key does not already exist in this map.

                if (value is null && treatNullValueAsNonexistent)
                {
                    // We can simply return this same map, as there's nothing to add or remove.
                    return this;
                }

                // We need to create a new map that has the additional key/value pair.
                // If with the addition we can still fit in a multi map, create one.
                if (_keyValues.Length < MaxMultiElements)
                {
                    var newValues = new KeyValuePair<IAsyncLocal, object?>[_keyValues.Length + 1];
                    Array.Copy(_keyValues, newValues, _keyValues.Length);
                    newValues[_keyValues.Length] = KeyValuePair.Create(key, value);
                    return new MultiElementAsyncLocalValueMap(newValues);
                }

                // Otherwise, upgrade to a many map.
                var many = new ManyElementAsyncLocalValueMap(MaxMultiElements + 1);
                foreach (KeyValuePair<IAsyncLocal, object?> pair in _keyValues)
                {
                    many[pair.Key] = pair.Value;
                }
                many[key] = value;
                return many;
            }

            public bool TryGetValue(IAsyncLocal key, out object? value)
            {
                foreach (KeyValuePair<IAsyncLocal, object?> pair in _keyValues)
                {
                    if (ReferenceEquals(key, pair.Key))
                    {
                        value = pair.Value;
                        return true;
                    }
                }
                value = null;
                return false;
            }
        }

        /// <summary>Instance with any number of key/value pairs.</summary>
        private sealed class ManyElementAsyncLocalValueMap : Dictionary<IAsyncLocal, object?>, IAsyncLocalValueMap
        {
            public ManyElementAsyncLocalValueMap(int capacity) : base(capacity) { }

            public IAsyncLocalValueMap Set(IAsyncLocal key, object? value, bool treatNullValueAsNonexistent)
            {
                int count = Count;
                bool containsKey = ContainsKey(key);

                // If the value being set exists, create a new many map, copy all of the elements from this one,
                // and then store the new key/value pair into it.  This is the most common case.
                if (value is not null || !treatNullValueAsNonexistent)
                {
                    var map = new ManyElementAsyncLocalValueMap(count + (containsKey ? 0 : 1));
                    foreach (KeyValuePair<IAsyncLocal, object?> pair in this)
                    {
                        map[pair.Key] = pair.Value;
                    }
                    map[key] = value;
                    return map;
                }

                // Otherwise, the value is null and a null value may be treated as nonexistent. We can downgrade to a smaller
                // map rather than storing null.

                // If the key is contained in this map, we're going to create a new map that's one pair smaller.
                if (containsKey)
                {
                    // If the new count would be within range of a multi map instead of a many map,
                    // downgrade to the multi map, which uses less memory and is faster to access.
                    // Otherwise, just create a new many map that's missing this key.
                    if (count == MultiElementAsyncLocalValueMap.MaxMultiElements + 1)
                    {
                        var newValues = new KeyValuePair<IAsyncLocal, object?>[MultiElementAsyncLocalValueMap.MaxMultiElements];
                        int index = 0;
                        foreach (KeyValuePair<IAsyncLocal, object?> pair in this)
                        {
                            if (!ReferenceEquals(key, pair.Key))
                            {
                                newValues[index++] = pair;
                            }
                        }
                        Debug.Assert(index == MultiElementAsyncLocalValueMap.MaxMultiElements);
                        return new MultiElementAsyncLocalValueMap(newValues);
                    }
                    else
                    {
                        var map = new ManyElementAsyncLocalValueMap(count - 1);
                        foreach (KeyValuePair<IAsyncLocal, object?> pair in this)
                        {
                            if (!ReferenceEquals(key, pair.Key))
                            {
                                map[pair.Key] = pair.Value;
                            }
                        }
                        Debug.Assert(map.Count == count - 1);
                        return map;
                    }
                }

                // We were storing null and a null value may be treated as nonexistent, but the key wasn't in the map, so
                // there's nothing to change.  Just return this instance.
                return this;
            }
        }
    }
}
