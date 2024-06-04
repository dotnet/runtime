// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Diagnostics
{
    internal sealed partial class DiagNode<T>
    {
        public DiagNode(T value) => Value = value;
        public T Value;
        public DiagNode<T>? Next;
    }

    // We are not using the public LinkedList<T> because we need to ensure thread safety operation on the list.
    internal sealed class DiagLinkedList<T> : IEnumerable<T>
    {
        private DiagNode<T>? _first;
        private DiagNode<T>? _last;

        public DiagLinkedList() { }

        public DiagLinkedList(T firstValue) => _last = _first = new DiagNode<T>(firstValue);

        public DiagLinkedList(IEnumerator<T> e)
        {
            Debug.Assert(e is not null);
            _last = _first = new DiagNode<T>(e.Current);

            while (e.MoveNext())
            {
                _last.Next = new DiagNode<T>(e.Current);
                _last = _last.Next;
            }
        }

        public DiagNode<T>? First => _first;

        public void Clear()
        {
            lock (this)
            {
                _first = _last = null;
            }
        }

        private void UnsafeAdd(DiagNode<T> newNode)
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
            DiagNode<T> newNode = new DiagNode<T>(value);

            lock (this)
            {
                UnsafeAdd(newNode);
            }
        }

        public bool AddIfNotExist(T value, Func<T, T, bool> compare)
        {
            lock (this)
            {
                DiagNode<T>? current = _first;
                while (current is not null)
                {
                    if (compare(value, current.Value))
                    {
                        return false;
                    }

                    current = current.Next;
                }

                DiagNode<T> newNode = new DiagNode<T>(value);
                UnsafeAdd(newNode);

                return true;
            }
        }

        public T? Remove(T value, Func<T, T, bool> compare)
        {
            lock (this)
            {
                DiagNode<T>? previous = _first;
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

                DiagNode<T>? current = previous.Next;

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
            DiagNode<T> newNode = new DiagNode<T>(value);

            lock (this)
            {
                newNode.Next = _first;
                _first = newNode;
            }
        }

        public DiagEnumerator<T> GetEnumerator() => new DiagEnumerator<T>(_first);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal struct DiagEnumerator<T> : IEnumerator<T>
    {
        private static readonly DiagNode<T> s_Empty = new DiagNode<T>(default!);

        private DiagNode<T>? _nextNode;
        private DiagNode<T> _currentNode;

        public DiagEnumerator(DiagNode<T>? head)
        {
            _nextNode = head;
            _currentNode = s_Empty;
        }

        public T Current => _currentNode.Value;

        object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_nextNode == null)
            {
                _currentNode = s_Empty;
                return false;
            }

            _currentNode = _nextNode;
            _nextNode = _nextNode.Next;
            return true;
        }

        public void Reset() => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}
