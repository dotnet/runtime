// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics
{
    internal sealed partial class LinkedListNode<T>
    {
        public LinkedListNode(T value) => Value = value;
        public T Value;
        public LinkedListNode<T>? Next;
    }

    // We are not using the public LinkedList<T> because we need to ensure thread safety operation on the list.
    internal sealed class LinkedList<T> : IEnumerable<T>
    {
        private LinkedListNode<T>? _first;
        private LinkedListNode<T>? _last;

        public LinkedList() {}

        public LinkedList(T firstValue) => _last = _first = new LinkedListNode<T>(firstValue);

        public LinkedList(IEnumerator<T> e)
        {
            Debug.Assert(e is not null);
            _last = _first = new LinkedListNode<T>(e.Current);

            while (e.MoveNext())
            {
                _last.Next = new LinkedListNode<T>(e.Current);
                _last = _last.Next;
            }
        }

        public LinkedListNode<T>? First => _first;

        public void Clear()
        {
            lock (this)
            {
                _first = _last = null;
            }
        }

        private void UnsafeAdd(LinkedListNode<T> newNode)
        {
            if (_first is null)
            {
                _first = _last = newNode;
                return;
            }

            Debug.Assert(_first is not null);
            Debug.Assert(_last is not null);

            _last!.Next = newNode;
            _last = newNode;
        }

        public void Add(T value)
        {
            LinkedListNode<T> newNode = new LinkedListNode<T>(value);

            lock (this)
            {
                UnsafeAdd(newNode);
            }
        }

        public bool AddIfNotExist(T value, Func<T, T, bool> compare)
        {
            lock (this)
            {
                LinkedListNode<T>? current = _first;
                while (current is not null)
                {
                    if (compare(value, current.Value))
                    {
                        return false;
                    }

                    current = current.Next;
                }

                LinkedListNode<T> newNode = new LinkedListNode<T>(value);
                UnsafeAdd(newNode);

                return true;
            }
        }

        public T? Remove(T value, Func<T, T, bool> compare)
        {
            lock (this)
            {
                LinkedListNode<T>? previous = _first;
                if (previous is null)
                {
                    return default;
                }

                if (compare(previous.Value, value))
                {
                    _first = previous.Next;
                    if (_first is null)
                    {
                        _last = null;
                    }
                    return previous.Value;
                }

                LinkedListNode<T>? current = previous.Next;

                while (current is not null)
                {
                    if (compare(current.Value, value))
                    {
                        previous.Next = current.Next;
                        if (object.ReferenceEquals(_last, current))
                        {
                            _last = previous;
                        }

                        return current.Value;
                    }

                    previous = current;
                    current = current.Next;
                }

                return default;
            }
        }

        public void AddFront(T value)
        {
            LinkedListNode<T> newNode = new LinkedListNode<T>(value);

            lock (this)
            {
                newNode.Next = _first;
                _first = newNode;
            }
        }

        // Note: Some consumers use this GetEnumerator dynamically to avoid allocations.
        public Enumerator<T> GetEnumerator() => new Enumerator<T>(_first);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    // Note: Some consumers use this Enumerator dynamically to avoid allocations.
    internal struct Enumerator<T> : IEnumerator<T>
    {
        private LinkedListNode<T>? _nextNode;
        [AllowNull, MaybeNull] private T _currentItem;

        public Enumerator(LinkedListNode<T>? head)
        {
            _nextNode = head;
            _currentItem = default;
        }

        public T Current => _currentItem!;

        object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_nextNode == null)
            {
                _currentItem = default;
                return false;
            }

            _currentItem = _nextNode.Value;
            _nextNode = _nextNode.Next;
            return true;
        }

        public void Reset() => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }

}