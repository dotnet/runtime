// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    ///  Represents a doubly linked list data structure. Used to support O(1) append of two lists,
    ///  that is currently not possible by using <see cref="Collections.Generic.LinkedList{T}"/>.
    ///  <see cref="Append(DoublyLinkedList{T})"/> operation is made use
    ///  of in the <see cref="RegexNodeConverter.ConvertToSymbolicRegexNode(RegexNode)"/> method
    ///  where it maintains linear construction time in terms of
    ///  the overall number of AST nodes in a given <see cref="RegexNode"/> input.
    /// </summary>
    /// <typeparam name="T">Element type of the list that must be not null</typeparam>
    internal sealed class DoublyLinkedList<T> where T : notnull
    {
        /// <summary>First node of the list</summary>
        private Node? _first;
        /// <summary>Last node of the list</summary>
        private Node? _last;
        /// <summary>Number of elements in the list (positive integer)</summary>
        internal int _size;

        internal T FirstElement { get { Debug.Assert(_first is not null && CheckValidity()); return _first._elem; } }

        /// <summary>Creates a new empty list</summary>
        public DoublyLinkedList() { }

        /// <summary>Creates a new singleton list containing the given element</summary>
        public DoublyLinkedList(T elem)
        {
            Node node = new Node(elem);
            _first = node;
            _last = node;
            _size = 1;
        }

        /// <summary>Append all the elements from the other list at the end of this list (O(1) operation, the other list must be discarded)</summary>
        public void Append(DoublyLinkedList<T> other)
        {
            //self append not allowed to avoid circularity
            Debug.Assert(other != this);
            Debug.Assert(CheckValidity());
            Debug.Assert(other.CheckValidity());

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

            _last._next = other._first;
            other._first._prev = _last;
            _last = other._last;
            _size += other._size;

            other._size = -1;
        }

        /// <summary>Append all the elements from all the other lists at the end of this list (O(others.Length) operation, the other lists must be discarded)</summary>
        public void Append(DoublyLinkedList<T>[] others)
        {
            for (int i = 0; i < others.Length; ++i)
            {
                Append(others[i]);
            }
        }

        /// <summary>Insert the given element at the end of this list (O(1) operation)</summary>
        public void InsertAtEnd(T elem)
        {
            Debug.Assert(CheckValidity());

            if (_last is null)
            {
                //this list is empty
                _first = new(elem);
                _last = _first;
                _size = 1;
                return;
            }

            _last._next = new(elem, _last, null);
            _last = _last._next;
            _size += 1;
        }

        /// <summary>Insert the given element at the start of this list (O(1) operation)</summary>
        public void InsertAtStart(T elem)
        {
            Debug.Assert(CheckValidity());

            if (_first is null)
            {
                //this list is empty
                _first = new(elem);
                _last = _first;
                _size = 1;
                return;
            }

            _first._prev = new(elem, null, _first);
            _first = _first._prev;
            _size += 1;
        }

        /// <summary>Enumerate all elements in the list</summary>
        /// <param name="inreverse">if true then the enumeration happens in reverse order</param>
        public IEnumerable<T> Enumerate(bool inreverse = false) => inreverse ? EnumerateBackwards() : EnumerateForwards();

        private IEnumerable<T> EnumerateForwards()
        {
            Debug.Assert(CheckValidity());

            Node? current = _first;
            while (current is not null)
            {
                yield return current._elem;
                current = current._next;
            }
        }

        private IEnumerable<T> EnumerateBackwards()
        {
            Debug.Assert(CheckValidity());

            Node? current = _last;
            while (current is not null)
            {
                yield return current._elem;
                current = current._prev;
            }
        }

        private sealed class Node
        {
            public Node? _next;
            public Node? _prev;
            public readonly T _elem;

            public Node(T elem) { _elem = elem; }

            public Node(T elem, Node? prev, Node? next) { _elem = elem; _prev = prev; _next = next; }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append('#');
            sb.Append(_size);
            sb.Append('(');
            Node? current = _first;
            while (current is not null)
            {
                sb.Append(current._elem);
                current = current._next;
                if (current is not null)
                {
                    sb.Append(',');
                }
            }
            sb.Append(')');
            return sb.ToString();
        }

        private bool CheckValidity() => _size >= 0 && // _size < 0 means that the list has been invalidated after Append
            (_size != 0 || (_first is null && _last is null)) && //empty list
            (_size == 0 || (_first is not null && _last is not null && _first._prev is null && _last._next is null)); //nonempty list
    }
}
