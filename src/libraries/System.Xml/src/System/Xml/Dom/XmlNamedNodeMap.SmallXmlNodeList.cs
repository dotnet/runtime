// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    using System.Collections;

    public partial class XmlNamedNodeMap
    {
        // Optimized to minimize space in the zero or one element cases.
        internal struct SmallXmlNodeList
        {
            // If field is null, that represents an empty list.
            // If field is non-null, but not an ArrayList, then the 'list' contains a single
            // object.
            // Otherwise, field is an ArrayList. Once the field upgrades to an ArrayList, it
            // never degrades back, even if all elements are removed.
            private object _field;

            public int Count
            {
                get
                {
                    if (_field == null)
                        return 0;

                    ArrayList list = _field as ArrayList;
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
                        throw new ArgumentOutOfRangeException("index");

                    ArrayList list = _field as ArrayList;
                    if (list != null)
                        return list[index];

                    if (index != 0)
                        throw new ArgumentOutOfRangeException("index");

                    return _field;
                }
            }

            public void Add(object value)
            {
                if (_field == null)
                {
                    if (value == null)
                    {
                        // If a single null value needs to be stored, then
                        // upgrade to an ArrayList
                        ArrayList temp = new ArrayList();
                        temp.Add(null);
                        _field = temp;
                    }
                    else
                        _field = value;

                    return;
                }

                ArrayList list = _field as ArrayList;
                if (list != null)
                {
                    list.Add(value);
                }
                else
                {
                    list = new ArrayList();
                    list.Add(_field);
                    list.Add(value);
                    _field = list;
                }
            }

            public void RemoveAt(int index)
            {
                if (_field == null)
                    throw new ArgumentOutOfRangeException("index");

                ArrayList list = _field as ArrayList;
                if (list != null)
                {
                    list.RemoveAt(index);
                    return;
                }

                if (index != 0)
                    throw new ArgumentOutOfRangeException("index");

                _field = null;
            }

            public void Insert(int index, object value)
            {
                if (_field == null)
                {
                    if (index != 0)
                        throw new ArgumentOutOfRangeException("index");
                    Add(value);
                    return;
                }

                ArrayList list = _field as ArrayList;
                if (list != null)
                {
                    list.Insert(index, value);
                    return;
                }

                if (index == 0)
                {
                    list = new ArrayList();
                    list.Add(value);
                    list.Add(_field);
                    _field = list;
                }
                else if (index == 1)
                {
                    list = new ArrayList();
                    list.Add(_field);
                    list.Add(value);
                    _field = list;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("index");
                }
            }

            private class SingleObjectEnumerator : IEnumerator
            {
                private object _loneValue;
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

                ArrayList list = _field as ArrayList;
                if (list != null)
                {
                    return list.GetEnumerator();
                }

                return new SingleObjectEnumerator(_field);
            }
        }
    }
}