// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ConcurrentStack.cs
//
// A lock-free, concurrent stack primitive, and its associated debugger view type.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    [DebuggerTypeProxy(typeof(IProducerConsumerCollectionDebugView<>))]
    public class ConcurrentStack<T> : IProducerConsumerCollection<T>, IReadOnlyCollection<T>
    {
        /// <summary>
        /// A simple (internal) node type used to store elements of concurrent stacks and queues.
        /// </summary>
        private sealed class Node
        {
            internal readonly T _value; // Value of the node.
            internal Node? _next; // Next pointer.

            /// <summary>
            /// Constructs a new node with the specified value and no next node.
            /// </summary>
            /// <param name="value">The value of the node.</param>
            internal Node(T value)
            {
                _value = value;
                _next = null;
            }
        }

        private volatile Node? _head; // The stack is a singly linked list, and only remembers the head.
        private const int BACKOFF_MAX_YIELDS = 8; // Arbitrary number to cap backoff.

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentStack{T}"/>
        /// class.
        /// </summary>
        public ConcurrentStack()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentStack{T}"/>
        /// class that contains elements copied from the specified collection
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new <see
        /// cref="ConcurrentStack{T}"/>.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="collection"/> argument is
        /// null.</exception>
        public ConcurrentStack(IEnumerable<T> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            InitializeFromCollection(collection);
        }

        /// <summary>
        /// Initializes the contents of the stack from an existing collection.
        /// </summary>
        /// <param name="collection">A collection from which to copy elements.</param>
        private void InitializeFromCollection(IEnumerable<T> collection)
        {
            // We just copy the contents of the collection to our stack.
            Node? lastNode = null;
            foreach (T element in collection)
            {
                Node newNode = new Node(element);
                newNode._next = lastNode;
                lastNode = newNode;
            }

            _head = lastNode;
        }


        /// <summary>
        /// Gets a value that indicates whether the <see cref="ConcurrentStack{T}"/> is empty.
        /// </summary>
        /// <value>true if the <see cref="ConcurrentStack{T}"/> is empty; otherwise, false.</value>
        /// <remarks>
        /// For determining whether the collection contains any items, use of this property is recommended
        /// rather than retrieving the number of items from the <see cref="Count"/> property and comparing it
        /// to 0.  However, as this collection is intended to be accessed concurrently, it may be the case
        /// that another thread will modify the collection after <see cref="IsEmpty"/> returns, thus invalidating
        /// the result.
        /// </remarks>
        public bool IsEmpty
        {
            // Checks whether the stack is empty. Clearly the answer may be out of date even prior to
            // the function returning (i.e. if another thread concurrently adds to the stack). It does
            // guarantee, however, that, if another thread does not mutate the stack, a subsequent call
            // to TryPop will return true -- i.e. it will also read the stack as non-empty.
            get { return _head == null; }
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

                for (Node? curr = _head; curr != null; curr = curr._next)
                {
                    count++; //we don't handle overflow, to be consistent with existing generic collection types in CLR
                }

                return count;
            }
        }


        /// <summary>
        /// Gets a value indicating whether access to the <see cref="System.Collections.ICollection"/> is
        /// synchronized with the SyncRoot.
        /// </summary>
        /// <value>true if access to the <see cref="System.Collections.ICollection"/> is synchronized
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
        /// cref="System.Collections.ICollection"/>. This property is not supported.
        /// </summary>
        /// <exception cref="System.NotSupportedException">The SyncRoot property is not supported</exception>
        object ICollection.SyncRoot
        {
            get
            {
                throw new NotSupportedException(SR.ConcurrentCollection_SyncRoot_NotSupported);
            }
        }

        /// <summary>
        /// Removes all objects from the <see cref="ConcurrentStack{T}"/>.
        /// </summary>
        public void Clear()
        {
            // Clear the list by setting the head to null. We don't need to use an atomic
            // operation for this: anybody who is mutating the head by pushing or popping
            // will need to use an atomic operation to guarantee they serialize and don't
            // overwrite our setting of the head to null.
            _head = null;
        }

        /// <summary>
        /// Copies the elements of the <see cref="System.Collections.ICollection"/> to an <see
        /// cref="System.Array"/>, starting at a particular
        /// <see cref="System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="System.Array"/> that is the destination of
        /// the elements copied from the
        /// <see cref="ConcurrentStack{T}"/>. The <see cref="System.Array"/> must
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
        /// -or- The number of elements in the source <see cref="System.Collections.ICollection"/> is
        /// greater than the available space from <paramref name="index"/> to the end of the destination
        /// <paramref name="array"/>. -or- The type of the source <see
        /// cref="System.Collections.ICollection"/> cannot be cast automatically to the type of the
        /// destination <paramref name="array"/>.
        /// </exception>
        void ICollection.CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);

            // We must be careful not to corrupt the array, so we will first accumulate an
            // internal list of elements that we will then copy to the array. This requires
            // some extra allocation, but is necessary since we don't know up front whether
            // the array is sufficiently large to hold the stack's contents.
            ((ICollection)ToList()).CopyTo(array, index);
        }

        /// <summary>
        /// Copies the <see cref="ConcurrentStack{T}"/> elements to an existing one-dimensional <see
        /// cref="System.Array"/>, starting at the specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="System.Array"/> that is the destination of
        /// the elements copied from the
        /// <see cref="ConcurrentStack{T}"/>. The <see cref="System.Array"/> must have zero-based
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
            ArgumentNullException.ThrowIfNull(array);

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
            newNode._next = _head;
            if (Interlocked.CompareExchange(ref _head, newNode, newNode._next) == newNode._next)
            {
                return;
            }

            // If we failed, go to the slow path and loop around until we succeed.
            PushCore(newNode, newNode);
        }

        /// <summary>
        /// Inserts multiple objects at the top of the <see cref="ConcurrentStack{T}"/> atomically.
        /// </summary>
        /// <param name="items">The objects to push onto the <see cref="ConcurrentStack{T}"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <remarks>
        /// When adding multiple items to the stack, using PushRange is a more efficient
        /// mechanism than using <see cref="Push"/> one item at a time.  Additionally, PushRange
        /// guarantees that all of the elements will be added atomically, meaning that no other threads will
        /// be able to inject elements between the elements being pushed.  Items at lower indices in
        /// the <paramref name="items"/> array will be pushed before items at higher indices.
        /// </remarks>
        public void PushRange(T[] items)
        {
            ArgumentNullException.ThrowIfNull(items);

            PushRange(items, 0, items.Length);
        }

        /// <summary>
        /// Inserts multiple objects at the top of the <see cref="ConcurrentStack{T}"/> atomically.
        /// </summary>
        /// <param name="items">The objects to push onto the <see cref="ConcurrentStack{T}"/>.</param>
        /// <param name="startIndex">The zero-based offset in <paramref name="items"/> at which to begin
        /// inserting elements onto the top of the <see cref="ConcurrentStack{T}"/>.</param>
        /// <param name="count">The number of elements to be inserted onto the top of the <see
        /// cref="ConcurrentStack{T}"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> or <paramref
        /// name="count"/> is negative. Or <paramref name="startIndex"/> is greater than or equal to the length
        /// of <paramref name="items"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="startIndex"/> + <paramref name="count"/> is
        /// greater than the length of <paramref name="items"/>.</exception>
        /// <remarks>
        /// When adding multiple items to the stack, using PushRange is a more efficient
        /// mechanism than using <see cref="Push"/> one item at a time. Additionally, PushRange
        /// guarantees that all of the elements will be added atomically, meaning that no other threads will
        /// be able to inject elements between the elements being pushed. Items at lower indices in the
        /// <paramref name="items"/> array will be pushed before items at higher indices.
        /// </remarks>
        public void PushRange(T[] items, int startIndex, int count)
        {
            ValidatePushPopRangeInput(items, startIndex, count);

            // No op if the count is zero
            if (count == 0)
                return;

            Node head, tail;
            head = tail = new Node(items[startIndex]);
            for (int i = startIndex + 1; i < startIndex + count; i++)
            {
                Node node = new Node(items[i]);
                node._next = head;
                head = node;
            }

            tail._next = _head;
            if (Interlocked.CompareExchange(ref _head, head, tail._next) == tail._next)
            {
                return;
            }

            // If we failed, go to the slow path and loop around until we succeed.
            PushCore(head, tail);
        }


        /// <summary>
        /// Push one or many nodes into the stack, if head and tails are equal then push one node to the stack other wise push the list between head
        /// and tail to the stack
        /// </summary>
        /// <param name="head">The head pointer to the new list</param>
        /// <param name="tail">The tail pointer to the new list</param>
        private void PushCore(Node head, Node tail)
        {
            SpinWait spin = default;

            // Keep trying to CAS the existing head with the new node until we succeed.
            do
            {
                spin.SpinOnce(sleep1Threshold: -1);
                // Reread the head and link our new node.
                tail._next = _head;
            }
            while (Interlocked.CompareExchange(
                ref _head, head, tail._next) != tail._next);

            if (CDSCollectionETWBCLProvider.Log.IsEnabled())
            {
                CDSCollectionETWBCLProvider.Log.ConcurrentStack_FastPushFailed(spin.Count);
            }
        }

        /// <summary>
        /// Local helper function to validate the Pop Push range methods input
        /// </summary>
        private static void ValidatePushPopRangeInput(T[] items, int startIndex, int count)
        {
            ArgumentNullException.ThrowIfNull(items);

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ConcurrentStack_PushPopRange_CountOutOfRange);
            }
            int length = items.Length;
            if (startIndex > length || startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ConcurrentStack_PushPopRange_StartOutOfRange);
            }
            if (length - count < startIndex) //instead of (startIndex + count > items.Length) to prevent overflow
            {
                throw new ArgumentException(SR.ConcurrentStack_PushPopRange_InvalidCount);
            }
        }

        /// <summary>
        /// Attempts to add an object to the <see
        /// cref="System.Collections.Concurrent.IProducerConsumerCollection{T}"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see
        /// cref="System.Collections.Concurrent.IProducerConsumerCollection{T}"/>. The value can be a null
        /// reference (Nothing in Visual Basic) for reference types.
        /// </param>
        /// <returns>true if the object was added successfully; otherwise, false.</returns>
        /// <remarks>For <see cref="ConcurrentStack{T}"/>, this operation
        /// will always insert the object onto the top of the <see cref="ConcurrentStack{T}"/>
        /// and return true.</remarks>
        bool IProducerConsumerCollection<T>.TryAdd(T item)
        {
            Push(item);
            return true;
        }

        /// <summary>
        /// Attempts to return an object from the top of the <see cref="ConcurrentStack{T}"/>
        /// without removing it.
        /// </summary>
        /// <param name="result">When this method returns, <paramref name="result"/> contains an object from
        /// the top of the <see cref="System.Collections.Concurrent.ConcurrentStack{T}"/> or an
        /// unspecified value if the operation failed.</param>
        /// <returns>true if and object was returned successfully; otherwise, false.</returns>
        public bool TryPeek([MaybeNullWhen(false)] out T result)
        {
            Node? head = _head;

            // If the stack is empty, return false; else return the element and true.
            if (head == null)
            {
                result = default(T)!;
                return false;
            }
            else
            {
                result = head._value;
                return true;
            }
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
        /// successfully; otherwise, false.</returns>
        public bool TryPop([MaybeNullWhen(false)] out T result)
        {
            Node? head = _head;
            //stack is empty
            if (head == null)
            {
                result = default(T)!;
                return false;
            }
            if (Interlocked.CompareExchange(ref _head, head._next, head) == head)
            {
                result = head._value;
                return true;
            }

            // Fall through to the slow path.
            return TryPopCore(out result);
        }

        /// <summary>
        /// Attempts to pop and return multiple objects from the top of the <see cref="ConcurrentStack{T}"/>
        /// atomically.
        /// </summary>
        /// <param name="items">
        /// The <see cref="System.Array"/> to which objects popped from the top of the <see
        /// cref="ConcurrentStack{T}"/> will be added.
        /// </param>
        /// <returns>The number of objects successfully popped from the top of the <see
        /// cref="ConcurrentStack{T}"/> and inserted in
        /// <paramref name="items"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is a null argument (Nothing
        /// in Visual Basic).</exception>
        /// <remarks>
        /// When popping multiple items, if there is little contention on the stack, using
        /// TryPopRange can be more efficient than using <see cref="TryPop"/>
        /// once per item to be removed.  Nodes fill the <paramref name="items"/>
        /// with the first node to be popped at the startIndex, the second node to be popped
        /// at startIndex + 1, and so on.
        /// </remarks>
        public int TryPopRange(T[] items)
        {
            ArgumentNullException.ThrowIfNull(items);

            return TryPopRange(items, 0, items.Length);
        }

        /// <summary>
        /// Attempts to pop and return multiple objects from the top of the <see cref="ConcurrentStack{T}"/>
        /// atomically.
        /// </summary>
        /// <param name="items">
        /// The <see cref="System.Array"/> to which objects popped from the top of the <see
        /// cref="ConcurrentStack{T}"/> will be added.
        /// </param>
        /// <param name="startIndex">The zero-based offset in <paramref name="items"/> at which to begin
        /// inserting elements from the top of the <see cref="ConcurrentStack{T}"/>.</param>
        /// <param name="count">The number of elements to be popped from top of the <see
        /// cref="ConcurrentStack{T}"/> and inserted into <paramref name="items"/>.</param>
        /// <returns>The number of objects successfully popped from the top of
        /// the <see cref="ConcurrentStack{T}"/> and inserted in <paramref name="items"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> or <paramref
        /// name="count"/> is negative. Or <paramref name="startIndex"/> is greater than or equal to the length
        /// of <paramref name="items"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="startIndex"/> + <paramref name="count"/> is
        /// greater than the length of <paramref name="items"/>.</exception>
        /// <remarks>
        /// When popping multiple items, if there is little contention on the stack, using
        /// TryPopRange can be more efficient than using <see cref="TryPop"/>
        /// once per item to be removed.  Nodes fill the <paramref name="items"/>
        /// with the first node to be popped at the startIndex, the second node to be popped
        /// at startIndex + 1, and so on.
        /// </remarks>
        public int TryPopRange(T[] items, int startIndex, int count)
        {
            ValidatePushPopRangeInput(items, startIndex, count);

            // No op if the count is zero
            if (count == 0)
                return 0;

            Node? poppedHead;
            int nodesCount = TryPopCore(count, out poppedHead);
            if (nodesCount > 0)
            {
                CopyRemovedItems(poppedHead!, items, startIndex, nodesCount);
            }
            return nodesCount;
        }

        /// <summary>
        /// Local helper function to Pop an item from the stack, slow path
        /// </summary>
        /// <param name="result">The popped item</param>
        /// <returns>True if succeeded, false otherwise</returns>
        private bool TryPopCore([MaybeNullWhen(false)] out T result)
        {
            Node? poppedNode;

            if (TryPopCore(1, out poppedNode) == 1)
            {
                result = poppedNode!._value;
                return true;
            }

            result = default(T)!;
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
        /// <returns>The number of objects successfully popped from the top of
        /// the <see cref="ConcurrentStack{T}"/>.</returns>
        private int TryPopCore(int count, out Node? poppedHead)
        {
            SpinWait spin = default;

            // Try to CAS the head with its current next.  We stop when we succeed or
            // when we notice that the stack is empty, whichever comes first.
            Node? head;
            Node next;
            int backoff = 1;
            while (true)
            {
                head = _head;
                // Is the stack empty?
                if (head == null)
                {
                    if (count == 1 && CDSCollectionETWBCLProvider.Log.IsEnabled())
                    {
                        CDSCollectionETWBCLProvider.Log.ConcurrentStack_FastPopFailed(spin.Count);
                    }

                    poppedHead = null;
                    return 0;
                }
                next = head;
                int nodesCount = 1;
                for (; nodesCount < count && next._next != null; nodesCount++)
                {
                    next = next._next;
                }

                // Try to swap the new head.  If we succeed, break out of the loop.
                if (Interlocked.CompareExchange(ref _head, next._next, head) == head)
                {
                    if (count == 1 && CDSCollectionETWBCLProvider.Log.IsEnabled())
                    {
                        CDSCollectionETWBCLProvider.Log.ConcurrentStack_FastPopFailed(spin.Count);
                    }

                    // Return the popped Node.
                    poppedHead = head;
                    return nodesCount;
                }

                // We failed to CAS the new head.  Spin briefly and retry.
                for (int i = 0; i < backoff; i++)
                {
                    spin.SpinOnce(sleep1Threshold: -1);
                }

                if (spin.NextSpinWillYield)
                {
                    backoff = Random.Shared.Next(1, BACKOFF_MAX_YIELDS);
                }
                else
                {
                    backoff *= 2;
                }
            }
        }

        /// <summary>
        /// Local helper function to copy the popped elements into a given collection
        /// </summary>
        /// <param name="head">The head of the list to be copied</param>
        /// <param name="collection">The collection to place the popped items in</param>
        /// <param name="startIndex">the beginning of index of where to place the popped items</param>
        /// <param name="nodesCount">The number of nodes.</param>
        private static void CopyRemovedItems(Node head, T[] collection, int startIndex, int nodesCount)
        {
            Node? current = head;
            for (int i = startIndex; i < startIndex + nodesCount; i++)
            {
                collection[i] = current!._value;
                current = current._next;
            }
        }

        /// <summary>
        /// Attempts to remove and return an object from the <see
        /// cref="System.Collections.Concurrent.IProducerConsumerCollection{T}"/>.
        /// </summary>
        /// <param name="item">
        /// When this method returns, if the operation was successful, <paramref name="item"/> contains the
        /// object removed. If no object was available to be removed, the value is unspecified.
        /// </param>
        /// <returns>true if an element was removed and returned successfully; otherwise, false.</returns>
        /// <remarks>For <see cref="ConcurrentStack{T}"/>, this operation will attempt to pope the object at
        /// the top of the <see cref="ConcurrentStack{T}"/>.
        /// </remarks>
        bool IProducerConsumerCollection<T>.TryTake([MaybeNullWhen(false)] out T item)
        {
            return TryPop(out item);
        }

        /// <summary>
        /// Copies the items stored in the <see cref="ConcurrentStack{T}"/> to a new array.
        /// </summary>
        /// <returns>A new array containing a snapshot of elements copied from the <see
        /// cref="ConcurrentStack{T}"/>.</returns>
        public T[] ToArray()
        {
            Node? curr = _head;
            return curr == null ?
                Array.Empty<T>() :
                ToList(curr).ToArray();
        }

        /// <summary>
        /// Returns an array containing a snapshot of the list's contents, using
        /// the target list node as the head of a region in the list.
        /// </summary>
        /// <returns>A list of the stack's contents.</returns>
        private List<T> ToList()
        {
            return ToList(_head);
        }

        /// <summary>
        /// Returns an array containing a snapshot of the list's contents starting at the specified node.
        /// </summary>
        /// <returns>A list of the stack's contents starting at the specified node.</returns>
        private static List<T> ToList(Node? curr)
        {
            List<T> list = new List<T>();

            while (curr != null)
            {
                list.Add(curr._value);
                curr = curr._next;
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
        /// <see cref="GetEnumerator()"/> was called.  The enumerator is safe to use
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
            //value of _head in a buffer and call out to a helper method
            return GetEnumerator(_head);
        }

        private static IEnumerator<T> GetEnumerator(Node? head)
        {
            Node? current = head;
            while (current != null)
            {
                yield return current._value;
                current = current._next;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="System.Collections.IEnumerator"/> that can be used to iterate through
        /// the collection.</returns>
        /// <remarks>
        /// The enumeration represents a moment-in-time snapshot of the contents of the stack. It does not
        /// reflect any updates to the collection after
        /// <see cref="GetEnumerator()"/> was called. The enumerator is safe to use concurrently with reads
        /// from and writes to the stack.
        /// </remarks>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }
    }
}
