// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*============================================================
**
** 
** 
**
**
** Purpose: List for exceptions.
**
** 
===========================================================*/

#nullable enable
namespace System.Collections
{
    ///    This is a simple implementation of IDictionary using a singly linked list. This
    ///    will be smaller and faster than a Hashtable if the number of elements is 10 or less.
    ///    This should not be used if performance is important for large numbers of elements.
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
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
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key), SR.ArgumentNull_Key);
                }
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
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key), SR.ArgumentNull_Key);
                }


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
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key), SR.ArgumentNull_Key);
            }


            version++;
            DictionaryNode? last = null;
            DictionaryNode? node;
            for (node = head; node != null; node = node.next)
            {
                if (node.key.Equals(key))
                {
                    throw new ArgumentException(SR.Format(SR.Argument_AddingDuplicate__, node.key, key));
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

        public void Clear()
        {
            count = 0;
            head = null;
            version++;
        }

        public bool Contains(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key), SR.ArgumentNull_Key);
            }
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
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (array.Length - index < this.Count)
                throw new ArgumentException(SR.ArgumentOutOfRange_Index, nameof(index));

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
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key), SR.ArgumentNull_Key);
            }
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

        private class NodeEnumerator : IDictionaryEnumerator
        {
            private ListDictionaryInternal list;
            private DictionaryNode? current;
            private int version;
            private bool start;


            public NodeEnumerator(ListDictionaryInternal list)
            {
                this.list = list;
                version = list.version;
                start = true;
                current = null;
            }

#pragma warning disable CS8612 // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/30958
            public object Current
#pragma warning restore CS8612
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
                return (current != null);
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


        private class NodeKeyValueCollection : ICollection
        {
            private ListDictionaryInternal list;
            private bool isKeys;

            public NodeKeyValueCollection(ListDictionaryInternal list, bool isKeys)
            {
                this.list = list;
                this.isKeys = isKeys;
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (array.Rank != 1)
                    throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
                if (array.Length - index < list.Count)
                    throw new ArgumentException(SR.ArgumentOutOfRange_Index, nameof(index));
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
                    return list.SyncRoot;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new NodeKeyValueEnumerator(list, isKeys);
            }


            private class NodeKeyValueEnumerator : IEnumerator
            {
                private ListDictionaryInternal list;
                private DictionaryNode? current;
                private int version;
                private bool isKeys;
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
                    return (current != null);
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
        private class DictionaryNode
        {
            public object key = null!;
            public object? value;
            public DictionaryNode? next;
        }
    }
}
