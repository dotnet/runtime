// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#pragma warning disable 0420


// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
//
//
// A lock-free, concurrent stack primitive, and its associated debugger view type.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;

namespace System.Collections.Concurrent
{
    // A stack that uses CAS operations internally to maintain thread-safety in a lock-free
    // manner. Attempting to push or pop concurrently from the stack will not trigger waiting,
    // although some optimistic concurrency and retry is used, possibly leading to lack of
    // fairness and/or livelock. The stack uses spinning and backoff to add some randomization,
    // in hopes of statistically decreasing the possibility of livelock.
    // 
    // Note that we currently allocate a new node on every push. This avoids having to worry
    // about potential ABA issues, since the CLR GC ensures that a memory address cannot be
    // reused before all references to it have died.

    /// <summary>
    /// Represents a thread-safe last-in, first-out collection of objects.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the stack.</typeparam>
    /// <remarks>
    /// All public and protected members of <see cref="ConcurrentStack{T}"/> are thread-safe and may be used
    /// concurrently from multiple threads.
    /// </remarks>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(SystemCollectionsConcurrent_ProducerConsumerCollectionDebugView<>))]
    internal class ConcurrentStack<T> : IProducerConsumerCollection<T>, IReadOnlyCollection<T>
    {
        /// <summary>
        /// A simple (internal) node type used to store elements of concurrent stacks and queues.
        /// </summary>
        private class Node
        {
            internal readonly T m_value; // Value of the node.
            internal Node m_next; // Next pointer.

            /// <summary>
            /// Constructs a new node with the specified value and no next node.
            /// </summary>
            /// <param name="value">The value of the node.</param>
            internal Node(T value)
            {
                m_value = value;
                m_next = null;
            }
        }

        private volatile Node m_head; // The stack is a singly linked list, and only remembers the head.

        private const int BACKOFF_MAX_YIELDS = 8; // Arbitrary number to cap backoff.

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentStack{T}"/>
        /// class.
        /// </summary>
        public ConcurrentStack()
        {
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="ConcurrentStack{T}"/>.
        /// </summary>
        /// <value>The number of elements contained in the <see cref="ConcurrentStack{T}"/>.</value>
        /// <remarks>
        /// For determining whether the collection contains any items, use of the <see cref="IsEmpty"/>
        /// property is recommended rather than retrieving the number of items from the <see cref="Count"/>
        /// property and comparing it to 0.
        /// </remarks>
        public int Count
        {
            // Counts the number of entries in the stack. This is an O(n) operation. The answer may be out
            // of date before returning, but guarantees to return a count that was once valid. Conceptually,
            // the implementation snaps a copy of the list and then counts the entries, though physically
            // this is not what actually happens.
            get
            {
                int count = 0;

                // Just whip through the list and tally up the number of nodes. We rely on the fact that
                // node next pointers are immutable after being enqueued for the first time, even as
                // they are being dequeued. If we ever changed this (e.g. to pool nodes somehow),
                // we'd need to revisit this implementation.

                for (Node curr = m_head; curr != null; curr = curr.m_next)
                {
                    count++; //we don't handle overflow, to be consistent with existing generic collection types in CLR
                }

                return count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is
        /// synchronized with the SyncRoot.
        /// </summary>
        /// <value>true if access to the <see cref="T:System.Collections.ICollection"/> is synchronized
        /// with the SyncRoot; otherwise, false. For <see cref="ConcurrentStack{T}"/>, this property always
        /// returns false.</value>
        bool ICollection.IsSynchronized
        {
            // Gets a value indicating whether access to this collection is synchronized. Always returns
            // false. The reason is subtle. While access is in face thread safe, it's not the case that
            // locking on the SyncRoot would have prevented concurrent pushes and pops, as this property
            // would typically indicate; that's because we internally use CAS operations vs. true locks.
            get { return false; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see
        /// cref="T:System.Collections.ICollection"/>. This property is not supported.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The SyncRoot property is not supported</exception>
        object ICollection.SyncRoot
        {
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("ConcurrentCollection_SyncRoot_NotSupported"));
            }
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection"/> to an <see
        /// cref="T:System.Array"/>, starting at a particular
        /// <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of
        /// the elements copied from the
        /// <see cref="ConcurrentStack{T}"/>. The <see cref="T:System.Array"/> must
        /// have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is a null reference (Nothing in
        /// Visual Basic).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// zero.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="array"/> is multidimensional. -or-
        /// <paramref name="array"/> does not have zero-based indexing. -or-
        /// <paramref name="index"/> is equal to or greater than the length of the <paramref name="array"/>
        /// -or- The number of elements in the source <see cref="T:System.Collections.ICollection"/> is
        /// greater than the available space from <paramref name="index"/> to the end of the destination
        /// <paramref name="array"/>. -or- The type of the source <see
        /// cref="T:System.Collections.ICollection"/> cannot be cast automatically to the type of the
        /// destination <paramref name="array"/>.
        /// </exception>
        void ICollection.CopyTo(Array array, int index)
        {
            // Validate arguments.
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            // We must be careful not to corrupt the array, so we will first accumulate an
            // internal list of elements that we will then copy to the array. This requires
            // some extra allocation, but is necessary since we don't know up front whether
            // the array is sufficiently large to hold the stack's contents.
            ((ICollection)ToList()).CopyTo(array, index);
        }

        /// <summary>
        /// Copies the <see cref="ConcurrentStack{T}"/> elements to an existing one-dimensional <see
        /// cref="T:System.Array"/>, starting at the specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of
        /// the elements copied from the
        /// <see cref="ConcurrentStack{T}"/>. The <see cref="T:System.Array"/> must have zero-based
        /// indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is a null reference (Nothing in
        /// Visual Basic).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> is equal to or greater than the
        /// length of the <paramref name="array"/>
        /// -or- The number of elements in the source <see cref="ConcurrentStack{T}"/> is greater than the
        /// available space from <paramref name="index"/> to the end of the destination <paramref
        /// name="array"/>.
        /// </exception>
        public void CopyTo(T[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            // We must be careful not to corrupt the array, so we will first accumulate an
            // internal list of elements that we will then copy to the array. This requires
            // some extra allocation, but is necessary since we don't know up front whether
            // the array is sufficiently large to hold the stack's contents.
            ToList().CopyTo(array, index);
        }


        /// <summary>
        /// Inserts an object at the top of the <see cref="ConcurrentStack{T}"/>.
        /// </summary>
        /// <param name="item">The object to push onto the <see cref="ConcurrentStack{T}"/>. The value can be
        /// a null reference (Nothing in Visual Basic) for reference types.
        /// </param>
        public void Push(T item)
        {
            // Pushes a node onto the front of the stack thread-safely. Internally, this simply
            // swaps the current head pointer using a (thread safe) CAS operation to accomplish
            // lock freedom. If the CAS fails, we add some back off to statistically decrease
            // contention at the head, and then go back around and retry.

            Node newNode = new Node(item);
            newNode.m_next = m_head;
            if (Interlocked.CompareExchange(ref m_head, newNode, newNode.m_next) == newNode.m_next)
            {
                return;
            }

            // If we failed, go to the slow path and loop around until we succeed.
            PushCore(newNode, newNode);
        }


        /// <summary>
        /// Push one or many nodes into the stack, if head and tails are equal then push one node to the stack other wise push the list between head
        /// and tail to the stack
        /// </summary>
        /// <param name="head">The head pointer to the new list</param>
        /// <param name="tail">The tail pointer to the new list</param>
        private void PushCore(Node head, Node tail)
        {
            SpinWait spin = new SpinWait();

            // Keep trying to CAS the exising head with the new node until we succeed.
            do
            {
                spin.SpinOnce();
                // Reread the head and link our new node.
                tail.m_next = m_head;
            }
            while (Interlocked.CompareExchange(
                ref m_head, head, tail.m_next) != tail.m_next);
        }

        /// <summary>
        /// Attempts to pop and return the object at the top of the <see cref="ConcurrentStack{T}"/>.
        /// </summary>
        /// <param name="result">
        /// When this method returns, if the operation was successful, <paramref name="result"/> contains the
        /// object removed. If no object was available to be removed, the value is unspecified.
        /// </param>
        /// <returns>true if an element was removed and returned from the top of the <see
        /// cref="ConcurrentStack{T}"/>
        /// succesfully; otherwise, false.</returns>
        public bool TryPop(out T result)
        {
            Node head = m_head;
            //stack is empty
            if (head == null)
            {
                result = default(T);
                return false;
            }
            if (Interlocked.CompareExchange(ref m_head, head.m_next, head) == head)
            {
                result = head.m_value;
                return true;
            }

            // Fall through to the slow path.
            return TryPopCore(out result);
        }

        /// <summary>
        /// Local helper function to Pop an item from the stack, slow path
        /// </summary>
        /// <param name="result">The popped item</param>
        /// <returns>True if succeeded, false otherwise</returns>
        private bool TryPopCore(out T result)
        {
            Node poppedNode;

            if (TryPopCore(1, out poppedNode) == 1)
            {
                result = poppedNode.m_value;
                return true;
            }

            result = default(T);
            return false;
        }

        /// <summary>
        /// Slow path helper for TryPop. This method assumes an initial attempt to pop an element
        /// has already occurred and failed, so it begins spinning right away.
        /// </summary>
        /// <param name="count">The number of items to pop.</param>
        /// <param name="poppedHead">
        /// When this method returns, if the pop succeeded, contains the removed object. If no object was
        /// available to be removed, the value is unspecified. This parameter is passed uninitialized.
        /// </param>
        /// <returns>True if an element was removed and returned; otherwise, false.</returns>
        private int TryPopCore(int count, out Node poppedHead)
        {
            SpinWait spin = new SpinWait();

            // Try to CAS the head with its current next.  We stop when we succeed or
            // when we notice that the stack is empty, whichever comes first.
            Node head;
            Node next;
            int backoff = 1;
            Random r = null;
            while (true)
            {
                head = m_head;
                // Is the stack empty?
                if (head == null)
                {
                    poppedHead = null;
                    return 0;
                }
                next = head;
                int nodesCount = 1;
                for (; nodesCount < count && next.m_next != null; nodesCount++)
                {
                    next = next.m_next;
                }

                // Try to swap the new head.  If we succeed, break out of the loop.
                if (Interlocked.CompareExchange(ref m_head, next.m_next, head) == head)
                {
                    // Return the popped Node.
                    poppedHead = head;
                    return nodesCount;
                }

                // We failed to CAS the new head.  Spin briefly and retry.
                for (int i = 0; i < backoff; i++)
                {
                    spin.SpinOnce();
                }

                if (spin.NextSpinWillYield)
                {
                    if (r == null)
                    {
                        r = new Random();
                    }
                    backoff = r.Next(1, BACKOFF_MAX_YIELDS);
                }
                else
                {
                    backoff *= 2;
                }
            }
        }

        /// <summary>
        /// Copies the items stored in the <see cref="ConcurrentStack{T}"/> to a new array.
        /// </summary>
        /// <returns>A new array containing a snapshot of elements copied from the <see
        /// cref="ConcurrentStack{T}"/>.</returns>
        public T[] ToArray()
        {
            return ToList().ToArray();
        }

        /// <summary>
        /// Returns an array containing a snapshot of the list's contents, using
        /// the target list node as the head of a region in the list.
        /// </summary>
        /// <returns>An array of the list's contents.</returns>
        private List<T> ToList()
        {
            List<T> list = new List<T>();

            Node curr = m_head;
            while (curr != null)
            {
                list.Add(curr.m_value);
                curr = curr.m_next;
            }

            return list;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="ConcurrentStack{T}"/>.
        /// </summary>
        /// <returns>An enumerator for the <see cref="ConcurrentStack{T}"/>.</returns>
        /// <remarks>
        /// The enumeration represents a moment-in-time snapshot of the contents
        /// of the stack.  It does not reflect any updates to the collection after 
        /// <see cref="GetEnumerator"/> was called.  The enumerator is safe to use
        /// concurrently with reads from and writes to the stack.
        /// </remarks>
        public IEnumerator<T> GetEnumerator()
        {
            // Returns an enumerator for the stack. This effectively takes a snapshot
            // of the stack's contents at the time of the call, i.e. subsequent modifications
            // (pushes or pops) will not be reflected in the enumerator's contents.

            //If we put yield-return here, the iterator will be lazily evaluated. As a result a snapshot of
            //the stack is not taken when GetEnumerator is initialized but when MoveNext() is first called.
            //This is inconsistent with existing generic collections. In order to prevent it, we capture the 
            //value of m_head in a buffer and call out to a helper method
            return GetEnumerator(m_head);
        }

        private IEnumerator<T> GetEnumerator(Node head)
        {
            Node current = head;
            while (current != null)
            {
                yield return current.m_value;
                current = current.m_next;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator"/> that can be used to iterate through
        /// the collection.</returns>
        /// <remarks>
        /// The enumeration represents a moment-in-time snapshot of the contents of the stack. It does not
        /// reflect any updates to the collection after
        /// <see cref="GetEnumerator"/> was called. The enumerator is safe to use concurrently with reads
        /// from and writes to the stack.
        /// </remarks>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }
    }
}
