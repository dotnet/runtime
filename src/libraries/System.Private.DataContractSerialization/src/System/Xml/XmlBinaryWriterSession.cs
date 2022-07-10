// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml;
using System.Collections;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml
{
    public class XmlBinaryWriterSession
    {
        private readonly PriorityDictionary<string, int> _strings;
        private readonly PriorityDictionary<IXmlDictionary, IntArray> _maps;
        private int _nextKey;

        public XmlBinaryWriterSession()
        {
            _nextKey = 0;
            _maps = new PriorityDictionary<IXmlDictionary, IntArray>();
            _strings = new PriorityDictionary<string, int>();
        }

        public virtual bool TryAdd(XmlDictionaryString value, out int key)
        {
            ArgumentNullException.ThrowIfNull(value);

            IntArray? keys;

            if (_maps.TryGetValue(value.Dictionary, out keys))
            {
                key = (keys[value.Key] - 1);

                if (key != -1)
                {
                    // If the key is already set, then something is wrong
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlKeyAlreadyExists));
                }

                key = Add(value.Value);
                keys[value.Key] = (key + 1);
                return true;
            }

            key = Add(value.Value);
            keys = AddKeys(value.Dictionary, value.Key + 1);
            keys[value.Key] = (key + 1);
            return true;
        }

        private int Add(string s)
        {
            int key = _nextKey++;
            _strings.Add(s, key);
            return key;
        }

        private IntArray AddKeys(IXmlDictionary dictionary, int minCount)
        {
            IntArray keys = new IntArray(Math.Max(minCount, 16));
            _maps.Add(dictionary, keys);
            return keys;
        }

        public void Reset()
        {
            _nextKey = 0;
            _maps.Clear();
            _strings.Clear();
        }

        internal bool TryLookup(XmlDictionaryString s, out int key)
        {
            IntArray? keys;
            if (_maps.TryGetValue(s.Dictionary, out keys))
            {
                key = (keys[s.Key] - 1);

                if (key != -1)
                {
                    return true;
                }
            }

            if (_strings.TryGetValue(s.Value, out key))
            {
                keys ??= AddKeys(s.Dictionary, s.Key + 1);

                keys[s.Key] = (key + 1);
                return true;
            }

            key = -1;
            return false;
        }

        private sealed class PriorityDictionary<K, V> where K : class
        {
            private Dictionary<K, V>? _dictionary;
            private readonly Entry[] _list;
            private int _listCount;
            private int _now;

            public PriorityDictionary()
            {
                _list = new Entry[16];
            }

            public void Clear()
            {
                _now = 0;
                _listCount = 0;
                Array.Clear(_list);
                _dictionary?.Clear();
            }

            public bool TryGetValue(K key, [MaybeNullWhen(false)] out V value)
            {
                for (int i = 0; i < _listCount; i++)
                {
                    if (_list[i].Key == key)
                    {
                        value = _list[i].Value;
                        _list[i].Time = Now;
                        return true;
                    }
                }

                for (int i = 0; i < _listCount; i++)
                {
                    if (_list[i].Key.Equals(key))
                    {
                        value = _list[i].Value;
                        _list[i].Time = Now;
                        return true;
                    }
                }

                if (_dictionary == null)
                {
                    value = default(V);
                    return false;
                }

                if (!_dictionary.TryGetValue(key, out value))
                {
                    return false;
                }

                int minIndex = 0;
                int minTime = _list[0].Time;
                for (int i = 1; i < _listCount; i++)
                {
                    if (_list[i].Time < minTime)
                    {
                        minIndex = i;
                        minTime = _list[i].Time;
                    }
                }

                _list[minIndex].Key = key;
                _list[minIndex].Value = value;
                _list[minIndex].Time = Now;
                return true;
            }

            public void Add(K key, V value)
            {
                if (_listCount < _list.Length)
                {
                    _list[_listCount].Key = key;
                    _list[_listCount].Value = value;
                    _listCount++;
                }
                else
                {
                    if (_dictionary == null)
                    {
                        _dictionary = new Dictionary<K, V>();
                        for (int i = 0; i < _listCount; i++)
                        {
                            _dictionary.Add(_list[i].Key, _list[i].Value);
                        }
                    }

                    _dictionary.Add(key, value);
                }
            }

            private int Now
            {
                get
                {
                    if (++_now == int.MaxValue)
                    {
                        DecreaseAll();
                    }

                    return _now;
                }
            }

            private void DecreaseAll()
            {
                for (int i = 0; i < _listCount; i++)
                {
                    _list[i].Time /= 2;
                }

                _now /= 2;
            }

            private struct Entry
            {
                public K Key;
                public V Value;
                public int Time;
            }
        }

        private sealed class IntArray
        {
            private int[] _array;

            public IntArray(int size)
            {
                _array = new int[size];
            }

            public int this[int index]
            {
                get
                {
                    if (index >= _array.Length)
                        return 0;

                    return _array[index];
                }
                set
                {
                    if (index >= _array.Length)
                    {
                        int[] newArray = new int[Math.Max(index + 1, _array.Length * 2)];
                        Array.Copy(_array, newArray, _array.Length);
                        _array = newArray;
                    }

                    _array[index] = value;
                }
            }
        }
    }
}
