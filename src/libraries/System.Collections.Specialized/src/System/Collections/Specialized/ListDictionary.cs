// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Collections.Specialized
{
    /// <devdoc>
    ///  <para>
    ///    This is a simple implementation of IDictionary using a singly linked list. This
    ///    will be smaller and faster than a Hashtable if the number of elements is 10 or less.
    ///    This should not be used if performance is important for large numbers of elements.
    ///  </para>
    /// </devdoc>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class ListDictionary : IDictionary
    {
        private DictionaryNode? head; // Do not rename (binary serialization)
        private int version; // Do not rename (binary serialization)
        private int count; // Do not rename (binary serialization)
        private readonly IComparer? comparer; // Do not rename (binary serialization)

        public ListDictionary()
        {
        }

        public ListDictionary(IComparer? comparer)
        {
            this.comparer = comparer;
        }

        public object? this[object key]
        {
            get
            {
                ArgumentNullException.ThrowIfNull(key);

                DictionaryNode? node = head;
                if (comparer == null)
                {
                    while (node != null)
                    {
                        object oldKey = node.key;
                        if (oldKey.Equals(key))
                        {
                            return node.value;
                        }
                        node = node.next;
                    }
                }
                else
                {
                    while (node != null)
                    {
                        object oldKey = node.key;
                        if (comparer.Compare(oldKey, key) == 0)
                        {
                            return node.value;
                        }
                        node = node.next;
                    }
                }
                return null;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(key);

                version++;
                DictionaryNode? last = null;
                DictionaryNode? node;
                for (node = head; node != null; node = node.next)
                {
                    object oldKey = node.key;
                    if ((comparer == null) ? oldKey.Equals(key) : comparer.Compare(oldKey, key) == 0)
                    {
                        break;
                    }
                    last = node;
                }
                if (node != null)
                {
                    // Found it
                    node.value = value;
                    return;
                }
                // Not found, so add a new one
                DictionaryNode newNode = new DictionaryNode();
                newNode.key = key;
                newNode.value = value;
                if (last != null)
                {
                    last.next = newNode;
                }
                else
                {
                    head = newNode;
                }
                count++;
            }
        }

        public int Count
        {
            get
            {
                return count;
            }
        }

        public ICollection Keys
        {
            get
            {
                return new NodeKeyValueCollection(this, true);
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public object SyncRoot => this;

        public ICollection Values
        {
            get
            {
                return new NodeKeyValueCollection(this, false);
            }
        }

        public void Add(object key, object? value)
        {
            ArgumentNullException.ThrowIfNull(key);

            version++;
            DictionaryNode? last = null;

            for (DictionaryNode? node = head; node != null; node = node.next)
            {
                object oldKey = node.key;
                if ((comparer == null) ? oldKey.Equals(key) : comparer.Compare(oldKey, key) == 0)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_AddingDuplicate, key));
                }
                last = node;
            }
            // Not found, so add a new one
            DictionaryNode newNode = new DictionaryNode();
            newNode.key = key;
            newNode.value = value;
            if (last != null)
            {
                last.next = newNode;
            }
            else
            {
                head = newNode;
            }
            count++;
        }

        public void Clear()
        {
            count = 0;
            head = null;
            version++;
        }

        public bool Contains(object key)
        {
            ArgumentNullException.ThrowIfNull(key);

            for (DictionaryNode? node = head; node != null; node = node.next)
            {
                object oldKey = node.key;
                if ((comparer == null) ? oldKey.Equals(key) : comparer.Compare(oldKey, key) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum_Index);

            if (array.Length - index < count)
                throw new ArgumentException(SR.Arg_InsufficientSpace);

            for (DictionaryNode? node = head; node != null; node = node.next)
            {
                array.SetValue(new DictionaryEntry(node.key, node.value), index);
                index++;
            }
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return new NodeEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new NodeEnumerator(this);
        }

        public void Remove(object key)
        {
            ArgumentNullException.ThrowIfNull(key);

            version++;
            DictionaryNode? last = null;
            DictionaryNode? node;
            for (node = head; node != null; node = node.next)
            {
                object oldKey = node.key;
                if ((comparer == null) ? oldKey.Equals(key) : comparer.Compare(oldKey, key) == 0)
                {
                    break;
                }
                last = node;
            }
            if (node == null)
            {
                return;
            }
            if (node == head)
            {
                head = node.next;
            }
            else
            {
                Debug.Assert(last != null);
                last.next = node.next;
            }
            count--;
        }

        private sealed class NodeEnumerator : IDictionaryEnumerator
        {
            private readonly ListDictionary _list;
            private DictionaryNode? _current;
            private readonly int _version;
            private bool _start;


            public NodeEnumerator(ListDictionary list)
            {
                _list = list;
                _version = list.version;
                _start = true;
                _current = null;
            }

            public object Current
            {
                get
                {
                    return Entry;
                }
            }

            public DictionaryEntry Entry
            {
                get
                {
                    if (_current == null)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }
                    return new DictionaryEntry(_current.key, _current.value);
                }
            }

            public object Key
            {
                get
                {
                    if (_current == null)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }
                    return _current.key;
                }
            }

            public object? Value
            {
                get
                {
                    if (_current == null)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }
                    return _current.value;
                }
            }

            public bool MoveNext()
            {
                if (_version != _list.version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }
                if (_start)
                {
                    _current = _list.head;
                    _start = false;
                }
                else if (_current != null)
                {
                    _current = _current.next;
                }
                return (_current != null);
            }

            public void Reset()
            {
                if (_version != _list.version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }
                _start = true;
                _current = null;
            }
        }

        private sealed class NodeKeyValueCollection : ICollection
        {
            private readonly ListDictionary _list;
            private readonly bool _isKeys;

            public NodeKeyValueCollection(ListDictionary list, bool isKeys)
            {
                _list = list;
                _isKeys = isKeys;
            }

            void ICollection.CopyTo(Array array, int index)
            {
                ArgumentNullException.ThrowIfNull(array);

                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum_Index);

                for (DictionaryNode? node = _list.head; node != null; node = node.next)
                {
                    array.SetValue(_isKeys ? node.key : node.value, index);
                    index++;
                }
            }

            int ICollection.Count
            {
                get
                {
                    int count = 0;
                    for (DictionaryNode? node = _list.head; node != null; node = node.next)
                    {
                        count++;
                    }
                    return count;
                }
            }

            bool ICollection.IsSynchronized
            {
                get
                {
                    return false;
                }
            }

            object ICollection.SyncRoot
            {
                get
                {
                    return _list.SyncRoot;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new NodeKeyValueEnumerator(_list, _isKeys);
            }


            private sealed class NodeKeyValueEnumerator : IEnumerator
            {
                private readonly ListDictionary _list;
                private DictionaryNode? _current;
                private readonly int _version;
                private readonly bool _isKeys;
                private bool _start;

                public NodeKeyValueEnumerator(ListDictionary list, bool isKeys)
                {
                    _list = list;
                    _isKeys = isKeys;
                    _version = list.version;
                    _start = true;
                    _current = null;
                }

                public object? Current
                {
                    get
                    {
                        if (_current == null)
                        {
                            throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                        }
                        return _isKeys ? _current.key : _current.value;
                    }
                }

                public bool MoveNext()
                {
                    if (_version != _list.version)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                    }
                    if (_start)
                    {
                        _current = _list.head;
                        _start = false;
                    }
                    else if (_current != null)
                    {
                        _current = _current.next;
                    }
                    return (_current != null);
                }

                public void Reset()
                {
                    if (_version != _list.version)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                    }
                    _start = true;
                    _current = null;
                }
            }
        }

        [Serializable]
        [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
        public class DictionaryNode
        {
            public object key = null!; // Do not rename (binary serialization)
            public object? value; // Do not rename (binary serialization)
            public DictionaryNode? next; // Do not rename (binary serialization)
        }
    }
}
