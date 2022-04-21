// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    ///  Represents a doubly linked list data structure.
    ///  The list cannot be created empty to avoid null values of fields.
    /// </summary>
    /// <typeparam name="T">Element type of the list that must be not null</typeparam>
    internal class DoublyLinkedList<T> where T : notnull
    {
        /// <summary>First node of the list</summary>
        internal Node _first;
        /// <summary>Last node of the list</summary>
        internal Node _last;
        /// <summary>Number of elements in the list (positive integer)</summary>
        internal int _size;

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
            //check internal integrity of both lists
            Debug.Assert(_first._prev is null);
            Debug.Assert(_last._next is null);
            Debug.Assert(other._first._prev is null);
            Debug.Assert(other._last._next is null);

            _last._next = other._first;
            other._first._prev = _last;
            _last = other._last;
            _size += other._size;
        }

        /// <summary>Insert the given element at the end of this list (O(1) operation)</summary>
        public void InsertAtEnd(T elem)
        {
            //check internal integrity of this list
            Debug.Assert(_first._prev is null);
            Debug.Assert(_last._next is null);

            _last._next = new(elem, _last, null);
            _last = _last._next;
            _size += 1;
        }

        /// <summary>Insert the given element at the start of this list (O(1) operation)</summary>
        public void InsertAtStart(T elem)
        {
            //check internal integrity of this list
            Debug.Assert(_first._prev is null);
            Debug.Assert(_last._next is null);

            _first._prev = new(elem, null, _first);
            _first = _first._prev;
            _size += 1;
        }

        internal class Node
        {
            public Node? _next;
            public Node? _prev;
            public readonly T _elem;

            public Node(T elem) { _elem = elem; }

            public Node(T elem, Node? prev, Node? next) { _elem = elem; _prev = prev; _next = next; }
        }

        /// <summary>Displays the list as a comma separated sequence of elements that is enclosed in parenthesis</summary>
        public override string ToString()
        {
            StringBuilder sb = new();
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
    }
}
