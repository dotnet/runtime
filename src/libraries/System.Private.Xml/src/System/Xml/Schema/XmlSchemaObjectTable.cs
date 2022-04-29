// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;

    public class XmlSchemaObjectTable
    {
        private readonly Dictionary<XmlQualifiedName, XmlSchemaObject> _table = new Dictionary<XmlQualifiedName, XmlSchemaObject>();
        private readonly List<XmlSchemaObjectEntry> _entries = new List<XmlSchemaObjectEntry>();

        internal XmlSchemaObjectTable()
        {
        }

        internal void Add(XmlQualifiedName name, XmlSchemaObject value)
        {
            Debug.Assert(!_table.ContainsKey(name), "XmlSchemaObjectTable.Add: entry already exists");
            _table.Add(name, value);
            _entries.Add(new XmlSchemaObjectEntry(name, value));
        }

        internal void Insert(XmlQualifiedName name, XmlSchemaObject value)
        {
            XmlSchemaObject? oldValue;
            if (_table.TryGetValue(name, out oldValue))
            {
                _table[name] = value; //set new value
                Debug.Assert(oldValue != null);
                int matchedIndex = FindIndexByValue(oldValue);
                Debug.Assert(matchedIndex >= 0);
                //set new entry
                Debug.Assert(_entries[matchedIndex].qname == name);
                _entries[matchedIndex] = new XmlSchemaObjectEntry(name, value);
            }
            else
            {
                Add(name, value);
            }
        }

        internal void Replace(XmlQualifiedName name, XmlSchemaObject value)
        {
            XmlSchemaObject? oldValue;
            if (_table.TryGetValue(name, out oldValue))
            {
                _table[name] = value; //set new value
                Debug.Assert(oldValue != null);
                int matchedIndex = FindIndexByValue(oldValue);
                Debug.Assert(_entries[matchedIndex].qname == name);
                _entries[matchedIndex] = new XmlSchemaObjectEntry(name, value);
            }
        }

        internal void Clear()
        {
            _table.Clear();
            _entries.Clear();
        }

        internal void Remove(XmlQualifiedName name)
        {
            XmlSchemaObject? value;
            if (_table.TryGetValue(name, out value))
            {
                _table.Remove(name);
                int matchedIndex = FindIndexByValue(value);
                Debug.Assert(matchedIndex >= 0);
                Debug.Assert(_entries[matchedIndex].qname == name);
                _entries.RemoveAt(matchedIndex);
            }
        }

        private int FindIndexByValue(XmlSchemaObject xso)
        {
            int index;
            for (index = 0; index < _entries.Count; index++)
            {
                if ((object)_entries[index].xso == (object)xso)
                {
                    return index;
                }
            }
            return -1;
        }

        public int Count
        {
            get
            {
                Debug.Assert(_table.Count == _entries.Count);
                return _table.Count;
            }
        }

        public bool Contains(XmlQualifiedName name)
        {
            return _table.ContainsKey(name);
        }

        public XmlSchemaObject? this[XmlQualifiedName name]
        {
            get
            {
                XmlSchemaObject? value;
                if (_table.TryGetValue(name, out value))
                {
                    return value;
                }
                return null;
            }
        }

        public ICollection Names
        {
            get
            {
                return new NamesCollection(_entries, _table.Count);
            }
        }

        public ICollection Values
        {
            get
            {
                return new ValuesCollection(_entries, _table.Count);
            }
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return new XSODictionaryEnumerator(_entries, _table.Count, EnumeratorType.DictionaryEntry);
        }

        internal enum EnumeratorType
        {
            Keys,
            Values,
            DictionaryEntry,
        }

        internal struct XmlSchemaObjectEntry
        {
            internal XmlQualifiedName qname;
            internal XmlSchemaObject xso;

            public XmlSchemaObjectEntry(XmlQualifiedName name, XmlSchemaObject value)
            {
                qname = name;
                xso = value;
            }
        }

        internal sealed class NamesCollection : ICollection
        {
            private readonly List<XmlSchemaObjectEntry> _entries;
            private readonly int _size;

            internal NamesCollection(List<XmlSchemaObjectEntry> entries, int size)
            {
                _entries = entries;
                _size = size;
            }

            public int Count
            {
                get { return _size; }
            }

            public object SyncRoot
            {
                get
                {
                    return ((ICollection)_entries).SyncRoot;
                }
            }

            public bool IsSynchronized
            {
                get
                {
                    return ((ICollection)_entries).IsSynchronized;
                }
            }

            public void CopyTo(Array array, int arrayIndex)
            {
                ArgumentNullException.ThrowIfNull(array);

                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));

                Debug.Assert(array.Length >= _size, "array is not big enough to hold all the items in the ICollection");

                for (int i = 0; i < _size; i++)
                {
                    array.SetValue(_entries[i].qname, arrayIndex++);
                }
            }

            public IEnumerator GetEnumerator()
            {
                return new XSOEnumerator(_entries, _size, EnumeratorType.Keys);
            }
        }

        //ICollection for Values
        internal sealed class ValuesCollection : ICollection
        {
            private readonly List<XmlSchemaObjectEntry> _entries;
            private readonly int _size;

            internal ValuesCollection(List<XmlSchemaObjectEntry> entries, int size)
            {
                _entries = entries;
                _size = size;
            }

            public int Count
            {
                get { return _size; }
            }

            public object SyncRoot
            {
                get
                {
                    return ((ICollection)_entries).SyncRoot;
                }
            }

            public bool IsSynchronized
            {
                get
                {
                    return ((ICollection)_entries).IsSynchronized;
                }
            }

            public void CopyTo(Array array, int arrayIndex)
            {
                ArgumentNullException.ThrowIfNull(array);

                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));

                Debug.Assert(array.Length >= _size, "array is not big enough to hold all the items in the ICollection");

                for (int i = 0; i < _size; i++)
                {
                    array.SetValue(_entries[i].xso, arrayIndex++);
                }
            }

            public IEnumerator GetEnumerator()
            {
                return new XSOEnumerator(_entries, _size, EnumeratorType.Values);
            }
        }

        internal class XSOEnumerator : IEnumerator
        {
            private readonly List<XmlSchemaObjectEntry> _entries;
            private readonly EnumeratorType _enumType;

            protected int currentIndex;
            protected int size;
            protected XmlQualifiedName? currentKey;
            protected XmlSchemaObject? currentValue;


            internal XSOEnumerator(List<XmlSchemaObjectEntry> entries, int size, EnumeratorType enumType)
            {
                _entries = entries;
                this.size = size;
                _enumType = enumType;
                currentIndex = -1;
            }

            public object? Current
            {
                get
                {
                    if (currentIndex == -1)
                    {
                        throw new InvalidOperationException(SR.Format(SR.Sch_EnumNotStarted, string.Empty));
                    }
                    if (currentIndex >= size)
                    {
                        throw new InvalidOperationException(SR.Format(SR.Sch_EnumFinished, string.Empty));
                    }
                    switch (_enumType)
                    {
                        case EnumeratorType.Keys:
                            return currentKey;

                        case EnumeratorType.Values:
                            return currentValue;

                        case EnumeratorType.DictionaryEntry:
                            return new DictionaryEntry(currentKey!, currentValue);

                        default:
                            break;
                    }
                    return null;
                }
            }

            public bool MoveNext()
            {
                if (currentIndex >= size - 1)
                {
                    currentValue = null;
                    currentKey = null;
                    return false;
                }
                currentIndex++;
                currentValue = _entries[currentIndex].xso;
                currentKey = _entries[currentIndex].qname;
                return true;
            }

            public void Reset()
            {
                currentIndex = -1;
                currentValue = null;
                currentKey = null;
            }
        }

        internal sealed class XSODictionaryEnumerator : XSOEnumerator, IDictionaryEnumerator
        {
            internal XSODictionaryEnumerator(List<XmlSchemaObjectEntry> entries, int size, EnumeratorType enumType) : base(entries, size, enumType)
            {
            }

            //IDictionaryEnumerator members
            public DictionaryEntry Entry
            {
                get
                {
                    if (currentIndex == -1)
                    {
                        throw new InvalidOperationException(SR.Format(SR.Sch_EnumNotStarted, string.Empty));
                    }
                    if (currentIndex >= size)
                    {
                        throw new InvalidOperationException(SR.Format(SR.Sch_EnumFinished, string.Empty));
                    }
                    return new DictionaryEntry(currentKey!, currentValue);
                }
            }

            public object Key
            {
                get
                {
                    if (currentIndex == -1)
                    {
                        throw new InvalidOperationException(SR.Format(SR.Sch_EnumNotStarted, string.Empty));
                    }
                    if (currentIndex >= size)
                    {
                        throw new InvalidOperationException(SR.Format(SR.Sch_EnumFinished, string.Empty));
                    }
                    return currentKey!;
                }
            }

            public object? Value
            {
                get
                {
                    if (currentIndex == -1)
                    {
                        throw new InvalidOperationException(SR.Format(SR.Sch_EnumNotStarted, string.Empty));
                    }
                    if (currentIndex >= size)
                    {
                        throw new InvalidOperationException(SR.Format(SR.Sch_EnumFinished, string.Empty));
                    }
                    return currentValue;
                }
            }
        }
    }
}
