// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ConcurrentBag.cs
//
// An unordered collection that allows duplicates and that provides add and get operations.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// Represents an thread-safe, unordered collection of objects. 
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the bag.</typeparam>
    /// <remarks>
    /// <para>
    /// Bags are useful for storing objects when ordering doesn't matter, and unlike sets, bags support
    /// duplicates. <see cref="ConcurrentBag{T}"/> is a thread-safe bag implementation, optimized for
    /// scenarios where the same thread will be both producing and consuming data stored in the bag.
    /// </para>
    /// <para>
    /// <see cref="ConcurrentBag{T}"/> accepts null reference (Nothing in Visual Basic) as a valid 
    /// value for reference types.
    /// </para>
    /// <para>
    /// All public and protected members of <see cref="ConcurrentBag{T}"/> are thread-safe and may be used
    /// concurrently from multiple threads.
    /// </para>
    /// </remarks>
    [DebuggerTypeProxy(typeof(IProducerConsumerCollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public class ConcurrentBag<T> : IProducerConsumerCollection<T>, IReadOnlyCollection<T>
    {
        // ThreadLocalList object that contains the data per thread
        private ThreadLocal<ThreadLocalList> _locals;

        // This head and tail pointers points to the first and last local lists, to allow enumeration on the thread locals objects
        private volatile ThreadLocalList _headList, _tailList;

        // A flag used to tell the operations thread that it must synchronize the operation, this flag is set/unset within
        // GlobalListsLock lock
        private bool _needSync;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentBag{T}"/>
        /// class.
        /// </summary>
        public ConcurrentBag()
        {
            Initialize(null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentBag{T}"/>
        /// class that contains elements copied from the specified collection.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new <see
        /// cref="ConcurrentBag{T}"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public ConcurrentBag(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection), SR.ConcurrentBag_Ctor_ArgumentNullException);
            }
            Initialize(collection);
        }


        /// <summary>
        /// Local helper function to initialize a new bag object
        /// </summary>
        /// <param name="collection">An enumeration containing items with which to initialize this bag.</param>
        private void Initialize(IEnumerable<T> collection)
        {
            _locals = new ThreadLocal<ThreadLocalList>();

            // Copy the collection to the bag
            if (collection != null)
            {
                ThreadLocalList list = GetThreadList(true);
                foreach (T item in collection)
                {
                    list.Add(item, false);
                }
            }
        }

        /// <summary>
        /// Adds an object to the <see cref="ConcurrentBag{T}"/>.
        /// </summary>
        /// <param name="item">The object to be added to the
        /// <see cref="ConcurrentBag{T}"/>. The value can be a null reference
        /// (Nothing in Visual Basic) for reference types.</param>
        public void Add(T item)
        {
            // Get the local list for that thread, create a new list if this thread doesn't exist 
            //(first time to call add)
            ThreadLocalList list = GetThreadList(true);
            AddInternal(list, item);
        }

        /// <summary>
        /// </summary>
        /// <param name="list"></param>
        /// <param name="item"></param>
        private void AddInternal(ThreadLocalList list, T item)
        {
            bool lockTaken = false;
            try
            {
#pragma warning disable 0420
                Interlocked.Exchange(ref list._currentOp, (int)ListOperation.Add);
#pragma warning restore 0420
                //Synchronization cases:
                // if the list count is less than two to avoid conflict with any stealing thread
                // if _needSync is set, this means there is a thread that needs to freeze the bag
                if (list.Count < 2 || _needSync)
                {
                    // reset it back to zero to avoid deadlock with stealing thread
                    list._currentOp = (int)ListOperation.None;
                    Monitor.Enter(list, ref lockTaken);
                }
                list.Add(item, lockTaken);
            }
            finally
            {
                list._currentOp = (int)ListOperation.None;
                if (lockTaken)
                {
                    Monitor.Exit(list);
                }
            }
        }

        /// <summary>
        /// Attempts to add an object to the <see cref="ConcurrentBag{T}"/>.
        /// </summary>
        /// <param name="item">The object to be added to the 
        /// <see cref="ConcurrentBag{T}"/>. The value can be a null reference
        /// (Nothing in Visual Basic) for reference types.</param>
        /// <returns>Always returns true</returns>
        bool IProducerConsumerCollection<T>.TryAdd(T item)
        {
            Add(item);
            return true;
        }

        /// <summary>
        /// Attempts to remove and return an object from the <see
        /// cref="ConcurrentBag{T}"/>.
        /// </summary>
        /// <param name="result">When this method returns, <paramref name="result"/> contains the object
        /// removed from the <see cref="ConcurrentBag{T}"/> or the default value
        /// of <typeparamref name="T"/> if the operation failed.</param>
        /// <returns>true if an object was removed successfully; otherwise, false.</returns>
        public bool TryTake(out T result)
        {
            return TryTakeOrPeek(out result, true);
        }

        /// <summary>
        /// Attempts to return an object from the <see cref="ConcurrentBag{T}"/>
        /// without removing it.
        /// </summary>
        /// <param name="result">When this method returns, <paramref name="result"/> contains an object from
        /// the <see cref="ConcurrentBag{T}"/> or the default value of
        /// <typeparamref name="T"/> if the operation failed.</param>
        /// <returns>true if and object was returned successfully; otherwise, false.</returns>
        public bool TryPeek(out T result)
        {
            return TryTakeOrPeek(out result, false);
        }

        /// <summary>
        /// Local helper function to Take or Peek an item from the bag
        /// </summary>
        /// <param name="result">To receive the item retrieved from the bag</param>
        /// <param name="take">True means Take operation, false means Peek operation</param>
        /// <returns>True if succeeded, false otherwise</returns>
        private bool TryTakeOrPeek(out T result, bool take)
        {
            // Get the local list for that thread, return null if the thread doesn't exit 
            //(this thread never add before) 
            ThreadLocalList list = GetThreadList(false);
            if (list == null || list.Count == 0)
            {
                return Steal(out result, take);
            }

            bool lockTaken = false;
            try
            {
                if (take) // Take operation
                {
#pragma warning disable 0420
                    Interlocked.Exchange(ref list._currentOp, (int)ListOperation.Take);
#pragma warning restore 0420
                    //Synchronization cases:
                    // if the list count is less than or equal two to avoid conflict with any stealing thread
                    // if _needSync is set, this means there is a thread that needs to freeze the bag
                    if (list.Count <= 2 || _needSync)
                    {
                        // reset it back to zero to avoid deadlock with stealing thread
                        list._currentOp = (int)ListOperation.None;
                        Monitor.Enter(list, ref lockTaken);

                        // Double check the count and steal if it became empty
                        if (list.Count == 0)
                        {
                            // Release the lock before stealing
                            if (lockTaken)
                            {
                                try { }
                                finally
                                {
                                    lockTaken = false; // reset lockTaken to avoid calling Monitor.Exit again in the finally block
                                    Monitor.Exit(list);
                                }
                            }
                            return Steal(out result, true);
                        }
                    }
                    list.Remove(out result);
                }
                else
                {
                    if (!list.Peek(out result))
                    {
                        return Steal(out result, false);
                    }
                }
            }
            finally
            {
                list._currentOp = (int)ListOperation.None;
                if (lockTaken)
                {
                    Monitor.Exit(list);
                }
            }
            return true;
        }


        /// <summary>
        /// Local helper function to retrieve a thread local list by a thread object
        /// </summary>
        /// <param name="forceCreate">Create a new list if the thread does ot exist</param>
        /// <returns>The local list object</returns>
        private ThreadLocalList GetThreadList(bool forceCreate)
        {
            ThreadLocalList list = _locals.Value;

            if (list != null)
            {
                return list;
            }
            else if (forceCreate)
            {
                // Acquire the lock to update the _tailList pointer
                lock (GlobalListsLock)
                {
                    if (_headList == null)
                    {
                        list = new ThreadLocalList(Environment.CurrentManagedThreadId);
                        _headList = list;
                        _tailList = list;
                    }
                    else
                    {
                        list = GetUnownedList();
                        if (list == null)
                        {
                            list = new ThreadLocalList(Environment.CurrentManagedThreadId);
                            _tailList._nextList = list;
                            _tailList = list;
                        }
                    }
                    _locals.Value = list;
                }
            }
            else
            {
                return null;
            }
            Debug.Assert(list != null);
            return list;
        }

        /// <summary>
        /// Try to reuse an unowned list if exist
        /// unowned lists are the lists that their owner threads are aborted or terminated
        /// this is workaround to avoid memory leaks.
        /// </summary>
        /// <returns>The list object, null if all lists are owned</returns>
        private ThreadLocalList GetUnownedList()
        {
            //the global lock must be held at this point
            Debug.Assert(Monitor.IsEntered(GlobalListsLock));

            int currentThreadId = Environment.CurrentManagedThreadId;
            ThreadLocalList currentList = _headList;
            while (currentList != null)
            {
                if (currentList._ownerThreadId == currentThreadId)
                {
                    return currentList;
                }
                currentList = currentList._nextList;
            }
            return null;
        }


        /// <summary>
        /// Local helper method to steal an item from any other non empty thread
        /// It enumerate all other threads in two passes first pass acquire the lock with TryEnter if succeeded
        /// it steals the item, otherwise it enumerate them again in 2nd pass and acquire the lock using Enter
        /// </summary>
        /// <param name="result">To receive the item retrieved from the bag</param>
        /// <param name="take">Whether to remove or peek.</param>
        /// <returns>True if succeeded, false otherwise.</returns>
        private bool Steal(out T result, bool take)
        {
#if FEATURE_TRACING
            if (take)
                CDSCollectionETWBCLProvider.Log.ConcurrentBag_TryTakeSteals();
            else
                CDSCollectionETWBCLProvider.Log.ConcurrentBag_TryPeekSteals();
#endif

            bool loop;
            List<int> versionsList = new List<int>(); // save the lists version
            do
            {
                versionsList.Clear(); //clear the list from the previous iteration
                loop = false;


                ThreadLocalList currentList = _headList;
                while (currentList != null)
                {
                    versionsList.Add(currentList._version);
                    if (currentList._head != null && TrySteal(currentList, out result, take))
                    {
                        return true;
                    }
                    currentList = currentList._nextList;
                }

                // verify versioning, if other items are added to this list since we last visit it, we should retry
                currentList = _headList;
                foreach (int version in versionsList)
                {
                    if (version != currentList._version) //oops state changed
                    {
                        loop = true;
                        if (currentList._head != null && TrySteal(currentList, out result, take))
                            return true;
                    }
                    currentList = currentList._nextList;
                }
            } while (loop);


            result = default(T);
            return false;
        }

        /// <summary>
        /// local helper function tries to steal an item from given local list
        /// </summary>
        private bool TrySteal(ThreadLocalList list, out T result, bool take)
        {
            lock (list)
            {
                if (CanSteal(list))
                {
                    list.Steal(out result, take);
                    return true;
                }
                result = default(T);
                return false;
            }
        }
        /// <summary>
        /// Local helper function to check the list if it became empty after acquiring the lock
        /// and wait if there is unsynchronized Add/Take operation in the list to be done
        /// </summary>
        /// <param name="list">The list to steal</param>
        /// <returns>True if can steal, false otherwise</returns>
        private static bool CanSteal(ThreadLocalList list)
        {
            if (list.Count <= 2 && list._currentOp != (int)ListOperation.None)
            {
                SpinWait spinner = new SpinWait();
                while (list._currentOp != (int)ListOperation.None)
                {
                    spinner.SpinOnce();
                }
            }
            return list.Count > 0;
        }

        /// <summary>
        /// Copies the <see cref="ConcurrentBag{T}"/> elements to an existing
        /// one-dimensional <see cref="T:System.Array">Array</see>, starting at the specified array
        /// index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array">Array</see> that is the
        /// destination of the elements copied from the
        /// <see cref="ConcurrentBag{T}"/>. The <see
        /// cref="T:System.Array">Array</see> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is a null reference (Nothing in
        /// Visual Basic).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> is equal to or greater than the
        /// length of the <paramref name="array"/>
        /// -or- the number of elements in the source <see
        /// cref="ConcurrentBag{T}"/> is greater than the available space from
        /// <paramref name="index"/> to the end of the destination <paramref name="array"/>.</exception>
        public void CopyTo(T[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array), SR.ConcurrentBag_CopyTo_ArgumentNullException);
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException
                    (nameof(index), SR.ConcurrentBag_CopyTo_ArgumentOutOfRangeException);
            }

            // Short path if the bag is empty
            if (_headList == null)
                return;

            bool lockTaken = false;
            try
            {
                FreezeBag(ref lockTaken);
                ToList().CopyTo(array, index);
            }
            finally
            {
                UnfreezeBag(lockTaken);
            }
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection"/> to an <see
        /// cref="T:System.Array"/>, starting at a particular
        /// <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array">Array</see> that is the
        /// destination of the elements copied from the
        /// <see cref="ConcurrentBag{T}"/>. The <see
        /// cref="T:System.Array">Array</see> must have zero-based indexing.</param>
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
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array), SR.ConcurrentBag_CopyTo_ArgumentNullException);
            }

            bool lockTaken = false;
            try
            {
                FreezeBag(ref lockTaken);
                ((ICollection)ToList()).CopyTo(array, index);
            }
            finally
            {
                UnfreezeBag(lockTaken);
            }
        }


        /// <summary>
        /// Copies the <see cref="ConcurrentBag{T}"/> elements to a new array.
        /// </summary>
        /// <returns>A new array containing a snapshot of elements copied from the <see
        /// cref="ConcurrentBag{T}"/>.</returns>
        public T[] ToArray()
        {
            // Short path if the bag is empty
            if (_headList == null)
                return Array.Empty<T>();

            bool lockTaken = false;
            try
            {
                FreezeBag(ref lockTaken);
                return ToList().ToArray();
            }
            finally
            {
                UnfreezeBag(lockTaken);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see
        /// cref="ConcurrentBag{T}"/>.
        /// </summary>
        /// <returns>An enumerator for the contents of the <see
        /// cref="ConcurrentBag{T}"/>.</returns>
        /// <remarks>
        /// The enumeration represents a moment-in-time snapshot of the contents
        /// of the bag.  It does not reflect any updates to the collection after 
        /// <see cref="GetEnumerator"/> was called.  The enumerator is safe to use
        /// concurrently with reads from and writes to the bag.
        /// </remarks>
        public IEnumerator<T> GetEnumerator()
        {
            // Short path if the bag is empty
            if (_headList == null)
                return ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator();

            bool lockTaken = false;
            try
            {
                FreezeBag(ref lockTaken);
                return ToList().GetEnumerator();
            }
            finally
            {
                UnfreezeBag(lockTaken);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see
        /// cref="ConcurrentBag{T}"/>.
        /// </summary>
        /// <returns>An enumerator for the contents of the <see
        /// cref="ConcurrentBag{T}"/>.</returns>
        /// <remarks>
        /// The items enumerated represent a moment-in-time snapshot of the contents
        /// of the bag.  It does not reflect any update to the collection after 
        /// <see cref="GetEnumerator"/> was called.
        /// </remarks>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ConcurrentBag<T>)this).GetEnumerator();
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="ConcurrentBag{T}"/>.
        /// </summary>
        /// <value>The number of elements contained in the <see cref="ConcurrentBag{T}"/>.</value>
        /// <remarks>
        /// The count returned represents a moment-in-time snapshot of the contents
        /// of the bag.  It does not reflect any updates to the collection after 
        /// <see cref="GetEnumerator"/> was called.
        /// </remarks>
        public int Count
        {
            get
            {
                // Short path if the bag is empty
                if (_headList == null)
                    return 0;

                bool lockTaken = false;
                try
                {
                    FreezeBag(ref lockTaken);
                    return GetCountInternal();
                }
                finally
                {
                    UnfreezeBag(lockTaken);
                }
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="ConcurrentBag{T}"/> is empty.
        /// </summary>
        /// <value>true if the <see cref="ConcurrentBag{T}"/> is empty; otherwise, false.</value>
        public bool IsEmpty
        {
            get
            {
                if (_headList == null)
                    return true;

                bool lockTaken = false;
                try
                {
                    FreezeBag(ref lockTaken);
                    ThreadLocalList currentList = _headList;
                    while (currentList != null)
                    {
                        if (currentList._head != null)
                        //at least this list is not empty, we return false
                        {
                            return false;
                        }
                        currentList = currentList._nextList;
                    }
                    return true;
                }
                finally
                {
                    UnfreezeBag(lockTaken);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is
        /// synchronized with the SyncRoot.
        /// </summary>
        /// <value>true if access to the <see cref="T:System.Collections.ICollection"/> is synchronized
        /// with the SyncRoot; otherwise, false. For <see cref="ConcurrentBag{T}"/>, this property always
        /// returns false.</value>
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see
        /// cref="T:System.Collections.ICollection"/>. This property is not supported.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The SyncRoot property is not supported.</exception>
        object ICollection.SyncRoot
        {
            get
            {
                throw new NotSupportedException(SR.ConcurrentCollection_SyncRoot_NotSupported);
            }
        }


        /// <summary>
        ///  A global lock object, used in two cases:
        ///  1- To  maintain the _tailList pointer for each new list addition process ( first time a thread called Add )
        ///  2- To freeze the bag in GetEnumerator, CopyTo, ToArray and Count members
        /// </summary>
        private object GlobalListsLock
        {
            get
            {
                Debug.Assert(_locals != null);
                return _locals;
            }
        }


        #region Freeze bag helper methods
        /// <summary>
        /// Local helper method to freeze all bag operations, it
        /// 1- Acquire the global lock to prevent any other thread to freeze the bag, and also new new thread can be added
        /// to the dictionary
        /// 2- Then Acquire all local lists locks to prevent steal and synchronized operations
        /// 3- Wait for all un-synchronized operations to be done
        /// </summary>
        /// <param name="lockTaken">Retrieve the lock taken result for the global lock, to be passed to Unfreeze method</param>
        private void FreezeBag(ref bool lockTaken)
        {
            Debug.Assert(!Monitor.IsEntered(GlobalListsLock));

            // global lock to be safe against multi threads calls count and corrupt _needSync
            Monitor.Enter(GlobalListsLock, ref lockTaken);

            // This will force any future add/take operation to be synchronized
            _needSync = true;

            //Acquire all local lists locks
            AcquireAllLocks();

            // Wait for all un-synchronized operation to be done
            WaitAllOperations();
        }

        /// <summary>
        /// Local helper method to unfreeze the bag from a frozen state
        /// </summary>
        /// <param name="lockTaken">The lock taken result from the Freeze method</param>
        private void UnfreezeBag(bool lockTaken)
        {
            ReleaseAllLocks();
            _needSync = false;
            if (lockTaken)
            {
                Monitor.Exit(GlobalListsLock);
            }
        }

        /// <summary>
        /// local helper method to acquire all local lists locks
        /// </summary>
        private void AcquireAllLocks()
        {
            Debug.Assert(Monitor.IsEntered(GlobalListsLock));

            bool lockTaken = false;
            ThreadLocalList currentList = _headList;
            while (currentList != null)
            {
                // Try/Finally block to avoid thread abort between acquiring the lock and setting the taken flag
                try
                {
                    Monitor.Enter(currentList, ref lockTaken);
                }
                finally
                {
                    if (lockTaken)
                    {
                        currentList._lockTaken = true;
                        lockTaken = false;
                    }
                }
                currentList = currentList._nextList;
            }
        }

        /// <summary>
        /// Local helper method to release all local lists locks
        /// </summary>
        private void ReleaseAllLocks()
        {
            ThreadLocalList currentList = _headList;
            while (currentList != null)
            {
                if (currentList._lockTaken)
                {
                    currentList._lockTaken = false;
                    Monitor.Exit(currentList);
                }
                currentList = currentList._nextList;
            }
        }

        /// <summary>
        /// Local helper function to wait all unsynchronized operations
        /// </summary>
        private void WaitAllOperations()
        {
            Debug.Assert(Monitor.IsEntered(GlobalListsLock));

            ThreadLocalList currentList = _headList;
            while (currentList != null)
            {
                if (currentList._currentOp != (int)ListOperation.None)
                {
                    SpinWait spinner = new SpinWait();
                    while (currentList._currentOp != (int)ListOperation.None)
                    {
                        spinner.SpinOnce();
                    }
                }
                currentList = currentList._nextList;
            }
        }

        /// <summary>
        /// Local helper function to get the bag count, the caller should call it from Freeze/Unfreeze block
        /// </summary>
        /// <returns>The current bag count</returns>
        private int GetCountInternal()
        {
            Debug.Assert(Monitor.IsEntered(GlobalListsLock));

            int count = 0;
            ThreadLocalList currentList = _headList;
            while (currentList != null)
            {
                checked
                {
                    count += currentList.Count;
                }
                currentList = currentList._nextList;
            }
            return count;
        }

        /// <summary>
        /// Local helper function to return the bag item in a list, this is mainly used by CopyTo and ToArray
        /// This is not thread safe, should be called in Freeze/UnFreeze bag block
        /// </summary>
        /// <returns>List the contains the bag items</returns>
        private List<T> ToList()
        {
            Debug.Assert(Monitor.IsEntered(GlobalListsLock));

            List<T> list = new List<T>();
            ThreadLocalList currentList = _headList;
            while (currentList != null)
            {
                Node currentNode = currentList._head;
                while (currentNode != null)
                {
                    list.Add(currentNode._value);
                    currentNode = currentNode._next;
                }
                currentList = currentList._nextList;
            }

            return list;
        }

        #endregion


        #region Inner Classes

        /// <summary>
        /// A class that represents a node in the lock thread list
        /// </summary>
        internal class Node
        {
            public Node(T value)
            {
                _value = value;
            }
            public readonly T _value;
            public Node _next;
            public Node _prev;
        }

        /// <summary>
        /// A class that represents the lock thread list
        /// </summary>
        internal class ThreadLocalList
        {
            // Tead node in the list, null means the list is empty
            internal volatile Node _head;

            // Tail node for the list
            private volatile Node _tail;

            // The current list operation
            internal volatile int _currentOp;

            // The list count from the Add/Take perspective
            private int _count;

            // The stealing count
            internal int _stealCount;

            // Next list in the dictionary values
            internal volatile ThreadLocalList _nextList;

            // Set if the locl lock is taken
            internal bool _lockTaken;

            // The owner thread for this list
            internal int _ownerThreadId;

            // the version of the list, incremented only when the list changed from empty to non empty state
            internal volatile int _version;

            /// <summary>
            /// ThreadLocalList constructor
            /// </summary>
            /// <param name="ownerThread">The owner thread for this list</param>
            internal ThreadLocalList(int ownerThreadId)
            {
                _ownerThreadId = ownerThreadId;
            }
            /// <summary>
            /// Add new item to head of the list
            /// </summary>
            /// <param name="item">The item to add.</param>
            /// <param name="updateCount">Whether to update the count.</param>
            internal void Add(T item, bool updateCount)
            {
                checked
                {
                    _count++;
                }
                Node node = new Node(item);
                if (_head == null)
                {
                    Debug.Assert(_tail == null);
                    _head = node;
                    _tail = node;
                    _version++; // changing from empty state to non empty state
                }
                else
                {
                    node._next = _head;
                    _head._prev = node;
                    _head = node;
                }
                if (updateCount) // update the count to avoid overflow if this add is synchronized
                {
                    _count = _count - _stealCount;
                    _stealCount = 0;
                }
            }

            /// <summary>
            /// Remove an item from the head of the list
            /// </summary>
            /// <param name="result">The removed item</param>
            internal void Remove(out T result)
            {
                Debug.Assert(_head != null);
                Node head = _head;
                _head = _head._next;
                if (_head != null)
                {
                    _head._prev = null;
                }
                else
                {
                    _tail = null;
                }
                _count--;
                result = head._value;
            }

            /// <summary>
            /// Peek an item from the head of the list
            /// </summary>
            /// <param name="result">the peeked item</param>
            /// <returns>True if succeeded, false otherwise</returns>
            internal bool Peek(out T result)
            {
                Node head = _head;
                if (head != null)
                {
                    result = head._value;
                    return true;
                }
                result = default(T);
                return false;
            }

            /// <summary>
            /// Steal an item from the tail of the list
            /// </summary>
            /// <param name="result">the removed item</param>
            /// <param name="remove">remove or peek flag</param>
            internal void Steal(out T result, bool remove)
            {
                Node tail = _tail;
                Debug.Assert(tail != null);
                if (remove) // Take operation
                {
                    _tail = _tail._prev;
                    if (_tail != null)
                    {
                        _tail._next = null;
                    }
                    else
                    {
                        _head = null;
                    }
                    // Increment the steal count
                    _stealCount++;
                }
                result = tail._value;
            }


            /// <summary>
            /// Gets the total list count, it's not thread safe, may provide incorrect count if it is called concurrently
            /// </summary>
            internal int Count
            {
                get
                {
                    return _count - _stealCount;
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// List operations for ConcurrentBag
    /// </summary>
    internal enum ListOperation
    {
        None,
        Add,
        Take
    };

}
