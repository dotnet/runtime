// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Represents a doubly linked list data structure.</summary>
    /// <typeparam name="T">Element type of the list that must be not null</typeparam>
    /// <remarks>
    /// Used to support O(1) append of two lists that is currently not possible by using <see cref="LinkedList{T}"/>.
    /// <see cref="AddLast(DoublyLinkedList{T})"/> operation is made use of in the <see cref="RegexNodeConverter.ConvertToSymbolicRegexNode(RegexNode)"/> method
    /// where it maintains linear construction time in terms of the overall number of AST nodes in a given <see cref="RegexNode"/> input.
    /// </remarks>
    internal sealed class DoublyLinkedList<T> where T : notnull
    {
        /// <summary>First node of the list</summary>
        private Node? _first;
        /// <summary>Last node of the list</summary>
        private Node? _last;
        /// <summary>The number of elements in the list.</summary>
        private int _size;

        /// <summary>Creates a new empty list</summary>
        public DoublyLinkedList() { }

        /// <summary>Creates a new singleton list containing the given element</summary>
        public DoublyLinkedList(T elem)
        {
            _first = _last = new Node(elem);
            _size = 1;
        }

        /// <summary>Number of elements in the list (positive integer)</summary>
        public int Count => _size;

        internal T FirstElement
        {
            get
            {
                Debug.Assert(_first is not null);
                AssertInvariants();

                return _first.Value;
            }
        }

        /// <summary>Append all the elements from the other list at the end of this list (O(1) operation, the other list must be discarded)</summary>
        public void AddLast(DoublyLinkedList<T> other)
        {
            Debug.Assert(other != this, "self append not allowed to avoid circularity");
            AssertInvariants();
            other.AssertInvariants();

            if (other._first is null)
            {
                other._size = -1;
                return;
            }

            if (_first is null)
            {
                //this list is empty
                _first = other._first;
                _last = other._last;
                _size = other._size;
                other._size = -1;
                return;
            }

            Debug.Assert(_last is not null);

            _last.Next = other._first;
            other._first.Prev = _last;
            _last = other._last;
            _size += other._size;

            other._size = -1;
        }

        /// <summary>Insert the given element at the end of this list (O(1) operation)</summary>
        public void AddLast(T elem)
        {
            AssertInvariants();

            if (_last is null)
            {
                //this list is empty
                _first = new(elem);
                _last = _first;
                _size = 1;
                return;
            }

            _last = _last.Next = new Node(elem, _last, null);
            _size++;
        }

        /// <summary>Insert the given element at the start of this list (O(1) operation)</summary>
        public void AddFirst(T elem)
        {
            AssertInvariants();

            if (_first is null)
            {
                //this list is empty
                _first = new(elem);
                _last = _first;
                _size = 1;
                return;
            }

            _first.Prev = new(elem, null, _first);
            _first = _first.Prev;
            _size++;
        }

        /// <summary>Enumerates the elements in the list from last to first.</summary>
        public IEnumerable<T> EnumerateLastToFirst()
        {
            AssertInvariants();

            for (Node? current = _last; current is not null; current = current.Prev)
            {
                yield return current.Value;
            }
        }

#if DEBUG
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder().Append('#').Append(_size).Append('(');

            string separator = "";
            for (Node? current = _first; current is not null; current = current.Next)
            {
                sb.Append(separator).Append(current.Value);
                separator = ",";
            }

            return sb.Append(')').ToString();
        }
#endif

        [Conditional("DEBUG")]
        private void AssertInvariants()
        {
            if (_size == 0)
            {

                Debug.Assert(_first is null && _last is null, "empty list");
            }
            else
            {
                Debug.Assert(_size > 0, "_size < 0 means that the list has been invalidated after Append");
                Debug.Assert(_first is not null && _last is not null && _first.Prev is null && _last.Next is null, "non-empty list");
            }
        }

        private sealed class Node
        {
            public Node? Next;
            public Node? Prev;
            public readonly T Value;

            public Node(T elem) { Value = elem; }

            public Node(T elem, Node? prev, Node? next)
            {
                Value = elem;
                Prev = prev;
                Next = next;
            }
        }
    }
}
