// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections
{
    /// <summary>
    /// Implements <see cref="IDictionary"/> using a singly linked list.
    /// Recommended for collections that typically include fewer than 10 items.
    /// </summary>
    [DebuggerDisplay("Count = {count}")]
    [DebuggerTypeProxy(typeof(ListDictionaryInternalDebugView))]
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public class ListDictionaryInternal : IDictionary
    {
        private DictionaryNode? head; // Do not rename (binary serialization)
        private int version; // Do not rename (binary serialization)
        private int count; // Do not rename (binary serialization)

        public ListDictionaryInternal()
        {
        }

        public object? this[object key]
        {
            get
            {
                ArgumentNullException.ThrowIfNull(key);

                DictionaryNode? node = head;

                while (node != null)
                {
                    if (node.key.Equals(key))
                    {
                        return node.value;
                    }
                    node = node.next;
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
                    if (node.key.Equals(key))
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

        public int Count => count;

        public ICollection Keys => new NodeKeyValueCollection(this, true);

        public bool IsReadOnly => false;

        public bool IsFixedSize => false;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        public ICollection Values => new NodeKeyValueCollection(this, false);

        public void Add(object key, object? value)
        {
            ArgumentNullException.ThrowIfNull(key);

            version++;
            DictionaryNode? last = null;
            for (DictionaryNode? node = head; node != null; node = node.next)
            {
                if (node.key.Equals(key))
                {
                    throw new ArgumentException(SR.Format(SR.Argument_AddingDuplicate__, node.key, key));
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
                if (node.key.Equals(key))
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);

            ArgumentOutOfRangeException.ThrowIfNegative(index);

            if (array.Length - index < this.Count)
                throw new ArgumentException(SR.ArgumentOutOfRange_IndexMustBeLessOrEqual, nameof(index));

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
                if (node.key.Equals(key))
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
                last!.next = node.next;
            }
            count--;
        }

        private sealed class NodeEnumerator : IDictionaryEnumerator
        {
            private readonly ListDictionaryInternal list;
            private DictionaryNode? current;
            private readonly int version;
            private bool start;

            public NodeEnumerator(ListDictionaryInternal list)
            {
                this.list = list;
                version = list.version;
                start = true;
                current = null;
            }

            public object Current => Entry;

            public DictionaryEntry Entry
            {
                get
                {
                    if (current == null)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }
                    return new DictionaryEntry(current.key, current.value);
                }
            }

            public object Key
            {
                get
                {
                    if (current == null)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }
                    return current.key;
                }
            }

            public object? Value
            {
                get
                {
                    if (current == null)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }
                    return current.value;
                }
            }

            public bool MoveNext()
            {
                if (version != list.version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }
                if (start)
                {
                    current = list.head;
                    start = false;
                }
                else
                {
                    if (current != null)
                    {
                        current = current.next;
                    }
                }
                return current != null;
            }

            public void Reset()
            {
                if (version != list.version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }
                start = true;
                current = null;
            }
        }

        private sealed class NodeKeyValueCollection : ICollection
        {
            private readonly ListDictionaryInternal list;
            private readonly bool isKeys;

            public NodeKeyValueCollection(ListDictionaryInternal list, bool isKeys)
            {
                this.list = list;
                this.isKeys = isKeys;
            }

            void ICollection.CopyTo(Array array, int index)
            {
                ArgumentNullException.ThrowIfNull(array);

                if (array.Rank != 1)
                    throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
                ArgumentOutOfRangeException.ThrowIfNegative(index);
                if (array.Length - index < list.Count)
                    throw new ArgumentException(SR.ArgumentOutOfRange_IndexMustBeLessOrEqual, nameof(index));
                for (DictionaryNode? node = list.head; node != null; node = node.next)
                {
                    array.SetValue(isKeys ? node.key : node.value, index);
                    index++;
                }
            }

            int ICollection.Count
            {
                get
                {
                    int count = 0;
                    for (DictionaryNode? node = list.head; node != null; node = node.next)
                    {
                        count++;
                    }
                    return count;
                }
            }

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => list.SyncRoot;

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new NodeKeyValueEnumerator(list, isKeys);
            }

            private sealed class NodeKeyValueEnumerator : IEnumerator
            {
                private readonly ListDictionaryInternal list;
                private DictionaryNode? current;
                private readonly int version;
                private readonly bool isKeys;
                private bool start;

                public NodeKeyValueEnumerator(ListDictionaryInternal list, bool isKeys)
                {
                    this.list = list;
                    this.isKeys = isKeys;
                    version = list.version;
                    start = true;
                    current = null;
                }

                public object? Current
                {
                    get
                    {
                        if (current == null)
                        {
                            throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                        }
                        return isKeys ? current.key : current.value;
                    }
                }

                public bool MoveNext()
                {
                    if (version != list.version)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                    }
                    if (start)
                    {
                        current = list.head;
                        start = false;
                    }
                    else
                    {
                        if (current != null)
                        {
                            current = current.next;
                        }
                    }
                    return current != null;
                }

                public void Reset()
                {
                    if (version != list.version)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                    }
                    start = true;
                    current = null;
                }
            }
        }

        [Serializable]
        private sealed class DictionaryNode
        {
            public object key = null!;
            public object? value;
            public DictionaryNode? next;
        }

        private sealed class ListDictionaryInternalDebugView
        {
            private readonly ListDictionaryInternal _list;

            public ListDictionaryInternalDebugView(ListDictionaryInternal list)
            {
                ArgumentNullException.ThrowIfNull(list);
                _list = list;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public DebugViewDictionaryItem<object, object?>[] Items
            {
                get
                {
                    var array = new DebugViewDictionaryItem<object, object?>[_list.count];
                    int index = 0;
                    for (DictionaryNode? node = _list.head; node != null; node = node.next)
                    {
                        array[index++] = new DebugViewDictionaryItem<object, object?>(node.key, node.value);
                    }
                    return array;
                }
            }
        }
    }
}
