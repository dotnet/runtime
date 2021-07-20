// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
    [DebuggerTypeProxy(typeof(ICollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class LinkedList<T> : ICollection<T>, ICollection, IReadOnlyCollection<T>, ISerializable, IDeserializationCallback
    {
        // This LinkedList is a doubly-Linked circular list.
        internal LinkedListNode<T>? head;
        internal int count;
        internal int version;
        private SerializationInfo? _siInfo; //A temporary variable which we need during deserialization.

        // names for serialization
        private const string VersionName = "Version"; // Do not rename (binary serialization)
        private const string CountName = "Count"; // Do not rename (binary serialization)
        private const string ValuesName = "Data"; // Do not rename (binary serialization)

        public LinkedList()
        {
        }

        public LinkedList(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            foreach (T item in collection)
            {
                AddLast(item);
            }
        }

        protected LinkedList(SerializationInfo info, StreamingContext context)
        {
            _siInfo = info;
        }

        public int Count
        {
            get { return count; }
        }

        public LinkedListNode<T>? First
        {
            get { return head; }
        }

        public LinkedListNode<T>? Last
        {
            get { return head == null ? null : head.prev; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        void ICollection<T>.Add(T value)
        {
            AddLast(value);
        }

        public LinkedListNode<T> AddAfter(LinkedListNode<T> node, T value)
        {
            ValidateNode(node);
            LinkedListNode<T> result = new LinkedListNode<T>(node.list!, value);
            InternalInsertNodeBefore(node.next!, result);
            return result;
        }

        public void AddAfter(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            ValidateNode(node);
            ValidateNewNode(newNode);
            InternalInsertNodeBefore(node.next!, newNode);
            newNode.list = this;
        }

        public LinkedListNode<T> AddBefore(LinkedListNode<T> node, T value)
        {
            ValidateNode(node);
            LinkedListNode<T> result = new LinkedListNode<T>(node.list!, value);
            InternalInsertNodeBefore(node, result);
            if (node == head)
            {
                head = result;
            }
            return result;
        }

        public void AddBefore(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            ValidateNode(node);
            ValidateNewNode(newNode);
            InternalInsertNodeBefore(node, newNode);
            newNode.list = this;
            if (node == head)
            {
                head = newNode;
            }
        }

        public LinkedListNode<T> AddFirst(T value)
        {
            LinkedListNode<T> result = new LinkedListNode<T>(this, value);
            if (head == null)
            {
                InternalInsertNodeToEmptyList(result);
            }
            else
            {
                InternalInsertNodeBefore(head, result);
                head = result;
            }
            return result;
        }

        public void AddFirst(LinkedListNode<T> node)
        {
            ValidateNewNode(node);

            if (head == null)
            {
                InternalInsertNodeToEmptyList(node);
            }
            else
            {
                InternalInsertNodeBefore(head, node);
                head = node;
            }
            node.list = this;
        }

        public LinkedListNode<T> AddLast(T value)
        {
            LinkedListNode<T> result = new LinkedListNode<T>(this, value);
            if (head == null)
            {
                InternalInsertNodeToEmptyList(result);
            }
            else
            {
                InternalInsertNodeBefore(head, result);
            }
            return result;
        }

        public void AddLast(LinkedListNode<T> node)
        {
            ValidateNewNode(node);

            if (head == null)
            {
                InternalInsertNodeToEmptyList(node);
            }
            else
            {
                InternalInsertNodeBefore(head, node);
            }
            node.list = this;
        }

        public void Clear()
        {
            LinkedListNode<T>? current = head;
            while (current != null)
            {
                LinkedListNode<T> temp = current;
                current = current.Next;
                temp.Invalidate();
            }

            head = null;
            count = 0;
            version++;
        }

        public bool Contains(T value)
        {
            return Find(value) != null;
        }

        public void CopyTo(T[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (index > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_BiggerThanCollection);
            }

            if (array.Length - index < Count)
            {
                throw new ArgumentException(SR.Arg_InsufficientSpace);
            }

            LinkedListNode<T>? node = head;
            if (node != null)
            {
                do
                {
                    array[index++] = node!.item;
                    node = node.next;
                } while (node != head);
            }
        }

        public LinkedListNode<T>? Find(T value)
        {
            LinkedListNode<T>? node = head;
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            if (node != null)
            {
                if (value != null)
                {
                    do
                    {
                        if (c.Equals(node!.item, value))
                        {
                            return node;
                        }
                        node = node.next;
                    } while (node != head);
                }
                else
                {
                    do
                    {
                        if (node!.item == null)
                        {
                            return node;
                        }
                        node = node.next;
                    } while (node != head);
                }
            }
            return null;
        }

        public LinkedListNode<T>? FindLast(T value)
        {
            if (head == null) return null;

            LinkedListNode<T>? last = head.prev;
            LinkedListNode<T>? node = last;
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            if (node != null)
            {
                if (value != null)
                {
                    do
                    {
                        if (c.Equals(node!.item, value))
                        {
                            return node;
                        }

                        node = node.prev;
                    } while (node != last);
                }
                else
                {
                    do
                    {
                        if (node!.item == null)
                        {
                            return node;
                        }
                        node = node.prev;
                    } while (node != last);
                }
            }
            return null;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Remove(T value)
        {
            LinkedListNode<T>? node = Find(value);
            if (node != null)
            {
                InternalRemoveNode(node);
                return true;
            }
            return false;
        }

        public void Remove(LinkedListNode<T> node)
        {
            ValidateNode(node);
            InternalRemoveNode(node);
        }

        public void RemoveFirst()
        {
            if (head == null) { throw new InvalidOperationException(SR.LinkedListEmpty); }
            InternalRemoveNode(head);
        }

        public void RemoveLast()
        {
            if (head == null) { throw new InvalidOperationException(SR.LinkedListEmpty); }
            InternalRemoveNode(head.prev!);
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Customized serialization for LinkedList.
            // We need to do this because it will be too expensive to Serialize each node.
            // This will give us the flexiblility to change internal implementation freely in future.
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(VersionName, version);
            info.AddValue(CountName, count); // this is the length of the bucket array.

            if (count != 0)
            {
                T[] array = new T[count];
                CopyTo(array, 0);
                info.AddValue(ValuesName, array, typeof(T[]));
            }
        }

        public virtual void OnDeserialization(object? sender)
        {
            if (_siInfo == null)
            {
                return; //Somebody had a dependency on this LinkedList and fixed us up before the ObjectManager got to it.
            }

            int realVersion = _siInfo.GetInt32(VersionName);
            int count = _siInfo.GetInt32(CountName);

            if (count != 0)
            {
                T[]? array = (T[]?)_siInfo.GetValue(ValuesName, typeof(T[]));

                if (array == null)
                {
                    throw new SerializationException(SR.Serialization_MissingValues);
                }
                for (int i = 0; i < array.Length; i++)
                {
                    AddLast(array[i]);
                }
            }
            else
            {
                head = null;
            }

            version = realVersion;
            _siInfo = null;
        }

        private void InternalInsertNodeBefore(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            newNode.next = node;
            newNode.prev = node.prev;
            node.prev!.next = newNode;
            node.prev = newNode;
            version++;
            count++;
        }

        private void InternalInsertNodeToEmptyList(LinkedListNode<T> newNode)
        {
            Debug.Assert(head == null && count == 0, "LinkedList must be empty when this method is called!");
            newNode.next = newNode;
            newNode.prev = newNode;
            head = newNode;
            version++;
            count++;
        }

        internal void InternalRemoveNode(LinkedListNode<T> node)
        {
            Debug.Assert(node.list == this, "Deleting the node from another list!");
            Debug.Assert(head != null, "This method shouldn't be called on empty list!");
            if (node.next == node)
            {
                Debug.Assert(count == 1 && head == node, "this should only be true for a list with only one node");
                head = null;
            }
            else
            {
                node.next!.prev = node.prev;
                node.prev!.next = node.next;
                if (head == node)
                {
                    head = node.next;
                }
            }
            node.Invalidate();
            count--;
            version++;
        }

        internal void ValidateNewNode(LinkedListNode<T> node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (node.list != null)
            {
                throw new InvalidOperationException(SR.LinkedListNodeIsAttached);
            }
        }

        internal void ValidateNode(LinkedListNode<T> node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (node.list != this)
            {
                throw new InvalidOperationException(SR.ExternalLinkedListNode);
            }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot => this;

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(SR.Arg_NonZeroLowerBound, nameof(array));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (array.Length - index < Count)
            {
                throw new ArgumentException(SR.Arg_InsufficientSpace);
            }

            T[]? tArray = array as T[];
            if (tArray != null)
            {
                CopyTo(tArray, index);
            }
            else
            {
                // No need to use reflection to verify that the types are compatible because it isn't 100% correct and we can rely
                // on the runtime validation during the cast that happens below (i.e. we will get an ArrayTypeMismatchException).
                object?[]? objects = array as object[];
                if (objects == null)
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }
                LinkedListNode<T>? node = head;
                try
                {
                    if (node != null)
                    {
                        do
                        {
                            objects[index++] = node!.item;
                            node = node.next;
                        } while (node != head);
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<T>, IEnumerator, ISerializable, IDeserializationCallback
        {
            private readonly LinkedList<T> _list;
            private LinkedListNode<T>? _node;
            private readonly int _version;
            private T? _current;
            private int _index;

            internal Enumerator(LinkedList<T> list)
            {
                _list = list;
                _version = list.version;
                _node = list.head;
                _current = default;
                _index = 0;
            }

            public T Current => _current!;

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || (_index == _list.Count + 1))
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }

                    return Current;
                }
            }

            public bool MoveNext()
            {
                if (_version != _list.version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }

                if (_node == null)
                {
                    _index = _list.Count + 1;
                    return false;
                }

                ++_index;
                _current = _node.item;
                _node = _node.next;
                if (_node == _list.head)
                {
                    _node = null;
                }
                return true;
            }

            void IEnumerator.Reset()
            {
                if (_version != _list.version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }

                _current = default;
                _node = _list.head;
                _index = 0;
            }

            public void Dispose()
            {
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                throw new PlatformNotSupportedException();
            }

            void IDeserializationCallback.OnDeserialization(object? sender)
            {
                throw new PlatformNotSupportedException();
            }
        }
    }

    // Note following class is not serializable since we customized the serialization of LinkedList.
    public sealed class LinkedListNode<T>
    {
        internal LinkedList<T>? list;
        internal LinkedListNode<T>? next;
        internal LinkedListNode<T>? prev;
        internal T item;

        public LinkedListNode(T value)
        {
            item = value;
        }

        internal LinkedListNode(LinkedList<T> list, T value)
        {
            this.list = list;
            item = value;
        }

        public LinkedList<T>? List
        {
            get { return list; }
        }

        public LinkedListNode<T>? Next
        {
            get { return next == null || next == list!.head ? null : next; }
        }

        public LinkedListNode<T>? Previous
        {
            get { return prev == null || this == list!.head ? null : prev; }
        }

        public T Value
        {
            get { return item; }
            set { item = value; }
        }

        /// <summary>Gets a reference to the value held by the node.</summary>
        public ref T ValueRef => ref item;

        internal void Invalidate()
        {
            list = null;
            next = null;
            prev = null;
        }
    }
}
