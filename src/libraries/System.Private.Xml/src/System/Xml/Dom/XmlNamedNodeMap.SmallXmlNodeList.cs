// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Xml
{
    public partial class XmlNamedNodeMap
    {
        // Optimized to minimize space in the zero or one element cases.
        internal struct SmallXmlNodeList
        {
            // If field is null, that represents an empty list.
            // If field is non-null, but not an List<object>, then the 'list' contains a single
            // object.
            // Otherwise, field is an List<object>. Once the field upgrades to an List<object>, it
            // never degrades back, even if all elements are removed.
            private object? _field;

            public int Count
            {
                get
                {
                    if (_field == null)
                        return 0;

                    List<object>? list = _field as List<object>;
                    if (list != null)
                        return list.Count;

                    return 1;
                }
            }

            public object this[int index]
            {
                get
                {
                    if (_field == null)
                        throw new ArgumentOutOfRangeException(nameof(index));

                    List<object>? list = _field as List<object>;
                    if (list != null)
                        return list[index];

                    ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);

                    return _field;
                }
            }

            public void Add(object value)
            {
                if (_field == null)
                {
                    if (value == null)
                    {
                        Debug.Fail("Null was added to the collection which didn't expect it");
                        // If a single null value needs to be stored, then
                        // upgrade to an ArrayList
                        List<object> temp = new List<object>();
                        temp.Add(null!);
                        _field = temp;
                    }
                    else
                        _field = value;

                    return;
                }

                List<object>? list = _field as List<object>;
                if (list != null)
                {
                    list.Add(value);
                }
                else
                {
                    list = new List<object>();
                    list.Add(_field);
                    list.Add(value);
                    _field = list;
                }
            }

            public void RemoveAt(int index)
            {
                if (_field == null)
                    throw new ArgumentOutOfRangeException(nameof(index));

                List<object?>? list = _field as List<object?>;
                if (list != null)
                {
                    list.RemoveAt(index);
                    return;
                }

                ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);

                _field = null;
            }

            public void Insert(int index, object value)
            {
                if (_field == null)
                {
                    ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);
                    Add(value);
                    return;
                }

                List<object>? list = _field as List<object>;
                if (list != null)
                {
                    list.Insert(index, value);
                    return;
                }

                if (index == 0)
                {
                    list = new List<object>();
                    list.Add(value);
                    list.Add(_field);
                    _field = list;
                }
                else if (index == 1)
                {
                    list = new List<object>();
                    list.Add(_field);
                    list.Add(value);
                    _field = list;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
            }

            private sealed class SingleObjectEnumerator : IEnumerator
            {
                private readonly object _loneValue;
                private int _position = -1;

                public SingleObjectEnumerator(object value)
                {
                    _loneValue = value;
                }

                public object Current
                {
                    get
                    {
                        if (_position != 0)
                        {
                            throw new InvalidOperationException();
                        }

                        return _loneValue;
                    }
                }

                public bool MoveNext()
                {
                    if (_position < 0)
                    {
                        _position = 0;
                        return true;
                    }

                    _position = 1;
                    return false;
                }

                public void Reset()
                {
                    _position = -1;
                }
            }

            public IEnumerator GetEnumerator()
            {
                if (_field == null)
                {
                    return XmlDocument.EmptyEnumerator;
                }

                List<object>? list = _field as List<object>;
                if (list != null)
                {
                    return list.GetEnumerator();
                }

                return new SingleObjectEnumerator(_field);
            }
        }
    }
}
