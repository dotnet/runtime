// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Internal.Runtime.CompilerServices;

namespace System.Collections.Concurrent
{
    internal abstract class DictionaryImpl<TKey, TValue>
        : DictionaryImpl
    {
        internal IEqualityComparer<TKey> _keyComparer;

        internal static Func<ConcurrentDictionary<TKey, TValue>, int, DictionaryImpl<TKey, TValue>> CreateRefUnsafe =
            (ConcurrentDictionary<TKey, TValue> topDict, int capacity) =>
            {
                var mObj = new Func<ConcurrentDictionary<object, object>, int, DictionaryImpl<object, object>> (DictionaryImpl.CreateRef);
                var method = mObj.GetMethodInfo().GetGenericMethodDefinition().MakeGenericMethod(new Type[] { typeof(TKey), typeof(TValue) });
                var del = (Func<ConcurrentDictionary<TKey, TValue>, int, DictionaryImpl<TKey, TValue>>)method
                    .CreateDelegate(typeof(Func<ConcurrentDictionary<TKey, TValue>, int, DictionaryImpl<TKey, TValue>>));

                var result = del(topDict, capacity);
                CreateRefUnsafe = del;

                return result;
            };

        internal DictionaryImpl() { }

        internal abstract void Clear();
        internal abstract int Count { get; }

        internal abstract object TryGetValue(TKey key);
        internal abstract bool PutIfMatch(TKey key, TValue newVal, ref TValue oldValue, ValueMatch match);
        internal abstract bool RemoveIfMatch(TKey key, ref TValue oldValue, ValueMatch match);
        internal abstract TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory);

        internal abstract Snapshot GetSnapshot();

        internal abstract class Snapshot
        {
            protected int _idx;
            protected TKey _curKey, _nextK;
            protected object _curValue, _nextV;

            public abstract int Count { get; }
            public abstract bool MoveNext();
            public abstract void Reset();

            internal DictionaryEntry Entry
            {
                get
                {
                    var curValue = this._curValue;
                    if (curValue == NULLVALUE)
                    {
                        // undefined behavior
                        return default;
                    }

                    var curValueUnboxed = default(TValue) != null ?
                        Unsafe.As<Boxed<TValue>>(curValue).Value :
                        (TValue)curValue;

                    return new DictionaryEntry(this._curKey, curValueUnboxed);
                }
            }

            internal KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    var curValue = this._curValue;
                    if (curValue == NULLVALUE)
                    {
                        // undefined behavior
                        return default;
                    }

                    var curValueUnboxed = default(TValue) != null ?
                                            Unsafe.As<Boxed<TValue>>(curValue).Value :
                                            (TValue)curValue;

                    return new KeyValuePair<TKey, TValue>(this._curKey, curValueUnboxed);
                }
            }
        }
    }
}
