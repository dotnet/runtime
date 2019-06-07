// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// BlockingCollection.cs
//
// A class that implements the bounding and blocking functionality while abstracting away
// the underlying storage mechanism. This file also contains BlockingCollection's 
// associated debugger view type.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;

namespace System.Collections.Concurrent
{
    /// <summary> 
    /// Provides blocking and bounding capabilities for thread-safe collections that 
    /// implement <see cref="T:System.Collections.Concurrent.IProducerConsumerCollection{T}"/>. 
    /// </summary>
    /// <remarks>
    /// <see cref="T:System.Collections.Concurrent.IProducerConsumerCollection{T}"/> represents a collection
    /// that allows for thread-safe adding and removing of data. 
    /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> is used as a wrapper
    /// for an <see cref="T:System.Collections.Concurrent.IProducerConsumerCollection{T}"/> instance, allowing
    /// removal attempts from the collection to block until data is available to be removed.  Similarly,
    /// a <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> can be created to enforce
    /// an upper-bound on the number of data elements allowed in the 
    /// <see cref="T:System.Collections.Concurrent.IProducerConsumerCollection{T}"/>; addition attempts to the
    /// collection may then block until space is available to store the added items.  In this manner,
    /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> is similar to a traditional
    /// blocking queue data structure, except that the underlying data storage mechanism is abstracted
    /// away as an <see cref="T:System.Collections.Concurrent.IProducerConsumerCollection{T}"/>. 
    /// </remarks>
    /// <typeparam name="T">Specifies the type of elements in the collection.</typeparam>
    [DebuggerTypeProxy(typeof(BlockingCollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}, Type = {_collection}")]
    public class BlockingCollection<T> : IEnumerable<T>, ICollection, IDisposable, IReadOnlyCollection<T>
    {
        private IProducerConsumerCollection<T> _collection = null!;
        private int _boundedCapacity;
        private const int NON_BOUNDED = -1;
        private SemaphoreSlim? _freeNodes;
        private SemaphoreSlim _occupiedNodes = null!;
        private bool _isDisposed;
        private CancellationTokenSource _consumersCancellationTokenSource = null!;
        private CancellationTokenSource _producersCancellationTokenSource = null!;

        private volatile int _currentAdders;
        private const int COMPLETE_ADDING_ON_MASK = unchecked((int)0x80000000);

        #region Properties
        /// <summary>Gets the bounded capacity of this <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance.</summary>
        /// <value>The bounded capacity of this collection, or -1 if no bound was supplied.</value>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        public int BoundedCapacity
        {
            get
            {
                CheckDisposed();
                return _boundedCapacity;
            }
        }

        /// <summary>Gets whether this <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been marked as complete for adding.</summary>
        /// <value>Whether this collection has been marked as complete for adding.</value>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        public bool IsAddingCompleted
        {
            get
            {
                CheckDisposed();
                return (_currentAdders == COMPLETE_ADDING_ON_MASK);
            }
        }

        /// <summary>Gets whether this <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been marked as complete for adding and is empty.</summary>
        /// <value>Whether this collection has been marked as complete for adding and is empty.</value>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        public bool IsCompleted
        {
            get
            {
                CheckDisposed();
                return (IsAddingCompleted && (_occupiedNodes.CurrentCount == 0));
            }
        }

        /// <summary>Gets the number of items contained in the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/>.</summary>
        /// <value>The number of items contained in the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/>.</value>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        public int Count
        {
            get
            {
                CheckDisposed();
                return _occupiedNodes.CurrentCount;
            }
        }

        /// <summary>Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is synchronized.</summary>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        bool ICollection.IsSynchronized
        {
            get
            {
                CheckDisposed();
                return false;
            }
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
        #endregion


        /// <summary>Initializes a new instance of the 
        /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/>
        /// class without an upper-bound.
        /// </summary>
        /// <remarks>
        /// The default underlying collection is a <see cref="System.Collections.Concurrent.ConcurrentQueue{T}">ConcurrentQueue&lt;T&gt;</see>.
        /// </remarks>
        public BlockingCollection()
            : this(new ConcurrentQueue<T>())
        {
        }

        /// <summary>Initializes a new instance of the <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/>
        /// class with the specified upper-bound.
        /// </summary>
        /// <param name="boundedCapacity">The bounded size of the collection.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The <paramref name="boundedCapacity"/> is
        /// not a positive value.</exception>
        /// <remarks>
        /// The default underlying collection is a <see cref="System.Collections.Concurrent.ConcurrentQueue{T}">ConcurrentQueue&lt;T&gt;</see>.
        /// </remarks>
        public BlockingCollection(int boundedCapacity)
            : this(new ConcurrentQueue<T>(), boundedCapacity)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/>
        /// class with the specified upper-bound and using the provided 
        /// <see cref="T:System.Collections.Concurrent.IProducerConsumerCollection{T}"/> as its underlying data store.</summary>
        /// <param name="collection">The collection to use as the underlying data store.</param>
        /// <param name="boundedCapacity">The bounded size of the collection.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="collection"/> argument is
        /// null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The <paramref name="boundedCapacity"/> is not a positive value.</exception>
        /// <exception cref="System.ArgumentException">The supplied <paramref name="collection"/> contains more values 
        /// than is permitted by <paramref name="boundedCapacity"/>.</exception>
        public BlockingCollection(IProducerConsumerCollection<T> collection, int boundedCapacity)
        {
            if (boundedCapacity < 1)
            {
                throw new ArgumentOutOfRangeException(
nameof(boundedCapacity), boundedCapacity,
                    SR.BlockingCollection_ctor_BoundedCapacityRange);
            }
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }
            int count = collection.Count;
            if (count > boundedCapacity)
            {
                throw new ArgumentException(SR.BlockingCollection_ctor_CountMoreThanCapacity);
            }
            Initialize(collection, boundedCapacity, count);
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/>
        /// class without an upper-bound and using the provided 
        /// <see cref="T:System.Collections.Concurrent.IProducerConsumerCollection{T}"/> as its underlying data store.</summary>
        /// <param name="collection">The collection to use as the underlying data store.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="collection"/> argument is
        /// null.</exception>
        public BlockingCollection(IProducerConsumerCollection<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }
            Initialize(collection, NON_BOUNDED, collection.Count);
        }

        /// <summary>Initializes the BlockingCollection instance.</summary>
        /// <param name="collection">The collection to use as the underlying data store.</param>
        /// <param name="boundedCapacity">The bounded size of the collection.</param>
        /// <param name="collectionCount">The number of items currently in the underlying collection.</param>
        private void Initialize(IProducerConsumerCollection<T> collection, int boundedCapacity, int collectionCount)
        {
            Debug.Assert(boundedCapacity > 0 || boundedCapacity == NON_BOUNDED);

            _collection = collection;
            _boundedCapacity = boundedCapacity; ;
            _isDisposed = false;
            _consumersCancellationTokenSource = new CancellationTokenSource();
            _producersCancellationTokenSource = new CancellationTokenSource();

            if (boundedCapacity == NON_BOUNDED)
            {
                _freeNodes = null;
            }
            else
            {
                Debug.Assert(boundedCapacity > 0);
                _freeNodes = new SemaphoreSlim(boundedCapacity - collectionCount);
            }


            _occupiedNodes = new SemaphoreSlim(collectionCount);
        }


        /// <summary>
        /// Adds the item to the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/>.
        /// </summary>
        /// <param name="item">The item to be added to the collection. The value can be a null reference.</param>
        /// <exception cref="T:System.InvalidOperationException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been marked
        /// as complete with regards to additions.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The underlying collection didn't accept the item.</exception>
        /// <remarks>
        /// If a bounded capacity was specified when this instance of 
        /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> was initialized, 
        /// a call to Add may block until space is available to store the provided item.
        /// </remarks>
        public void Add(T item)
        {
#if DEBUG
            bool tryAddReturnValue =
#endif
            TryAddWithNoTimeValidation(item, Timeout.Infinite, new CancellationToken());
#if DEBUG
            Debug.Assert(tryAddReturnValue, "TryAdd() was expected to return true.");
#endif
        }

        /// <summary>
        /// Adds the item to the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/>.
        /// A <see cref="System.OperationCanceledException"/> is thrown if the <see cref="CancellationToken"/> is
        /// canceled.
        /// </summary>
        /// <param name="item">The item to be added to the collection. The value can be a null reference.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken"/> is canceled.</exception>
        /// <exception cref="T:System.InvalidOperationException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been marked
        /// as complete with regards to additions.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The underlying collection didn't accept the item.</exception>
        /// <remarks>
        /// If a bounded capacity was specified when this instance of 
        /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> was initialized, 
        /// a call to <see cref="Add(T,System.Threading.CancellationToken)"/> may block until space is available to store the provided item.
        /// </remarks>
        public void Add(T item, CancellationToken cancellationToken)
        {
#if DEBUG
            bool tryAddReturnValue =
#endif
            TryAddWithNoTimeValidation(item, Timeout.Infinite, cancellationToken);
#if DEBUG
            Debug.Assert(tryAddReturnValue, "TryAdd() was expected to return true.");
#endif
        }

        /// <summary>
        /// Attempts to add the specified item to the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/>.
        /// </summary>
        /// <param name="item">The item to be added to the collection.</param>
        /// <returns>true if the <paramref name="item"/> could be added; otherwise, false.</returns>
        /// <exception cref="T:System.InvalidOperationException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been marked
        /// as complete with regards to additions.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The underlying collection didn't accept the item.</exception>
        public bool TryAdd(T item)
        {
            return TryAddWithNoTimeValidation(item, 0, new CancellationToken());
        }

        /// <summary>
        /// Attempts to add the specified item to the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/>.
        /// </summary>
        /// <param name="item">The item to be added to the collection.</param>
        /// <param name="timeout">A <see cref="System.TimeSpan"/> that represents the number of milliseconds
        /// to wait, or a <see cref="System.TimeSpan"/> that represents -1 milliseconds to wait indefinitely.
        /// </param>
        /// <returns>true if the <paramref name="item"/> could be added to the collection within 
        /// the alloted time; otherwise, false.</returns>
        /// <exception cref="T:System.InvalidOperationException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been marked
        /// as complete with regards to additions.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout"/> is a negative number
        /// other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater than
        /// <see cref="System.Int32.MaxValue"/>.</exception>
        /// <exception cref="T:System.InvalidOperationException">The underlying collection didn't accept the item.</exception>
        public bool TryAdd(T item, TimeSpan timeout)
        {
            ValidateTimeout(timeout);
            return TryAddWithNoTimeValidation(item, (int)timeout.TotalMilliseconds, new CancellationToken());
        }

        /// <summary>
        /// Attempts to add the specified item to the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/>.
        /// </summary>
        /// <param name="item">The item to be added to the collection.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see
        /// cref="System.Threading.Timeout.Infinite"/> (-1) to wait indefinitely.</param>
        /// <returns>true if the <paramref name="item"/> could be added to the collection within 
        /// the alloted time; otherwise, false.</returns>
        /// <exception cref="T:System.InvalidOperationException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been marked
        /// as complete with regards to additions.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a
        /// negative number other than -1, which represents an infinite time-out.</exception>
        /// <exception cref="T:System.InvalidOperationException">The underlying collection didn't accept the item.</exception>
        public bool TryAdd(T item, int millisecondsTimeout)
        {
            ValidateMillisecondsTimeout(millisecondsTimeout);
            return TryAddWithNoTimeValidation(item, millisecondsTimeout, new CancellationToken());
        }

        /// <summary>
        /// Attempts to add the specified item to the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/>.
        /// A <see cref="System.OperationCanceledException"/> is thrown if the <see cref="CancellationToken"/> is
        /// canceled.
        /// </summary>
        /// <param name="item">The item to be added to the collection.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see
        /// cref="System.Threading.Timeout.Infinite"/> (-1) to wait indefinitely.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>true if the <paramref name="item"/> could be added to the collection within 
        /// the alloted time; otherwise, false.</returns>
        /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken"/> is canceled.</exception>
        /// <exception cref="T:System.InvalidOperationException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been marked
        /// as complete with regards to additions.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a
        /// negative number other than -1, which represents an infinite time-out.</exception>
        /// <exception cref="T:System.InvalidOperationException">The underlying collection didn't accept the item.</exception>
        public bool TryAdd(T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            ValidateMillisecondsTimeout(millisecondsTimeout);
            return TryAddWithNoTimeValidation(item, millisecondsTimeout, cancellationToken);
        }

        /// <summary>Adds an item into the underlying data store using its IProducerConsumerCollection&lt;T&gt;.Add 
        /// method. If a bounded capacity was specified and the collection was full, 
        /// this method will wait for, at most, the timeout period trying to add the item. 
        /// If the timeout period was exhausted before successfully adding the item this method will 
        /// return false.</summary>
        /// <param name="item">The item to be added to the collection.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait for the collection to accept the item,
        /// or Timeout.Infinite to wait indefinitely.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>False if the collection remained full till the timeout period was exhausted.True otherwise.</returns>
        /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken"/> is canceled.</exception>
        /// <exception cref="System.InvalidOperationException">the collection has already been marked
        /// as complete with regards to additions.</exception>
        /// <exception cref="System.ObjectDisposedException">If the collection has been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The underlying collection didn't accept the item.</exception>
        private bool TryAddWithNoTimeValidation(T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            CheckDisposed();

            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(SR.Common_OperationCanceled, cancellationToken);

            if (IsAddingCompleted)
            {
                throw new InvalidOperationException(SR.BlockingCollection_Completed);
            }

            bool waitForSemaphoreWasSuccessful = true;

            if (_freeNodes != null)
            {
                //If the _freeNodes semaphore threw OperationCanceledException then this means that CompleteAdding()
                //was called concurrently with Adding which is not supported by BlockingCollection.
                CancellationTokenSource? linkedTokenSource = null;
                try
                {
                    waitForSemaphoreWasSuccessful = _freeNodes.Wait(0);
                    if (waitForSemaphoreWasSuccessful == false && millisecondsTimeout != 0)
                    {
                        linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                            cancellationToken, _producersCancellationTokenSource.Token);
                        waitForSemaphoreWasSuccessful = _freeNodes.Wait(millisecondsTimeout, linkedTokenSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    //if cancellation was via external token, throw an OCE
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(SR.Common_OperationCanceled, cancellationToken);

                    //if cancellation was via internal token, this indicates invalid use, hence InvalidOpEx.
                    //Debug.Assert(_ProducersCancellationTokenSource.Token.IsCancellationRequested);

                    throw new InvalidOperationException
                        (SR.BlockingCollection_Add_ConcurrentCompleteAdd);
                }
                finally
                {
                    if (linkedTokenSource != null)
                    {
                        linkedTokenSource.Dispose();
                    }
                }
            }
            if (waitForSemaphoreWasSuccessful)
            {
                // Update the adders count if the complete adding was not requested, otherwise
                // spins until all adders finish then throw IOE
                // The idea behind to spin until all adders finish, is to avoid to return to the caller with IOE while there are still some adders have
                // not been finished yet
                SpinWait spinner = new SpinWait();
                while (true)
                {
                    int observedAdders = _currentAdders;
                    if ((observedAdders & COMPLETE_ADDING_ON_MASK) != 0)
                    {
                        spinner.Reset();
                        // CompleteAdding is requested, spin then throw
                        while (_currentAdders != COMPLETE_ADDING_ON_MASK) spinner.SpinOnce();
                        throw new InvalidOperationException(SR.BlockingCollection_Completed);
                    }

                    if (Interlocked.CompareExchange(ref _currentAdders, observedAdders + 1, observedAdders) == observedAdders)
                    {
                        Debug.Assert((observedAdders + 1) <= (~COMPLETE_ADDING_ON_MASK), "The number of concurrent adders thread exceeded the maximum limit.");
                        break;
                    }
                    spinner.SpinOnce(sleep1Threshold: -1);
                }

                // This outer try/finally to workaround of repeating the decrement adders code 3 times, because we should decrement the adders if:
                // 1- _collection.TryAdd threw an exception
                // 2- _collection.TryAdd succeeded
                // 3- _collection.TryAdd returned false
                // so we put the decrement code in the finally block
                try
                {
                    //TryAdd is guaranteed to find a place to add the element. Its return value depends
                    //on the semantics of the underlying store. Some underlying stores will not add an already
                    //existing item and thus TryAdd returns false indicating that the size of the underlying
                    //store did not increase.


                    bool addingSucceeded = false;
                    try
                    {
                        //The token may have been canceled before the collection had space available, so we need a check after the wait has completed.
                        //This fixes bug #702328, case 2 of 2.
                        cancellationToken.ThrowIfCancellationRequested();
                        addingSucceeded = _collection.TryAdd(item);
                    }
                    catch
                    {
                        //TryAdd did not result in increasing the size of the underlying store and hence we need
                        //to increment back the count of the _freeNodes semaphore.
                        if (_freeNodes != null)
                        {
                            _freeNodes.Release();
                        }
                        throw;
                    }
                    if (addingSucceeded)
                    {
                        //After adding an element to the underlying storage, signal to the consumers 
                        //waiting on _occupiedNodes that there is a new item added ready to be consumed.
                        _occupiedNodes.Release();
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.BlockingCollection_Add_Failed);
                    }
                }
                finally
                {
                    // decrement the adders count
                    Debug.Assert((_currentAdders & ~COMPLETE_ADDING_ON_MASK) > 0);
                    Interlocked.Decrement(ref _currentAdders);
                }
            }
            return waitForSemaphoreWasSuccessful;
        }

        /// <summary>Takes an item from the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/>.</summary>
        /// <returns>The item removed from the collection.</returns>
        /// <exception cref="T:System.OperationCanceledException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> is empty and has been marked
        /// as complete with regards to additions.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The underlying collection was modified
        /// outside of this <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance.</exception>
        /// <remarks>A call to <see cref="Take()"/> may block until an item is available to be removed.</remarks>
        public T Take()
        {
            T item;

            if (!TryTake(out item, Timeout.Infinite, CancellationToken.None))
            {
                throw new InvalidOperationException(SR.BlockingCollection_CantTakeWhenDone);
            }

            return item;
        }

        /// <summary>Takes an item from the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/>.</summary>
        /// <returns>The item removed from the collection.</returns>
        /// <exception cref="T:System.OperationCanceledException">If the <see cref="CancellationToken"/> is
        /// canceled or the <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> is empty and has been marked
        /// as complete with regards to additions.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The underlying collection was modified
        /// outside of this <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance.</exception>
        /// <remarks>A call to <see cref="Take(CancellationToken)"/> may block until an item is available to be removed.</remarks>
        public T Take(CancellationToken cancellationToken)
        {
            T item;

            if (!TryTake(out item, Timeout.Infinite, cancellationToken))
            {
                throw new InvalidOperationException(SR.BlockingCollection_CantTakeWhenDone);
            }

            return item;
        }

        /// <summary>
        /// Attempts to remove an item from the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/>.
        /// </summary>
        /// <param name="item">The item removed from the collection.</param>
        /// <returns>true if an item could be removed; otherwise, false.</returns>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The underlying collection was modified
        /// outside of this <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance.</exception>
        public bool TryTake([MaybeNullWhen(false)] out T item)
        {
            return TryTake(out item, 0, CancellationToken.None);
        }

        /// <summary>
        /// Attempts to remove an item from the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/>.
        /// </summary>
        /// <param name="item">The item removed from the collection.</param>
        /// <param name="timeout">A <see cref="System.TimeSpan"/> that represents the number of milliseconds
        /// to wait, or a <see cref="System.TimeSpan"/> that represents -1 milliseconds to wait indefinitely.
        /// </param>
        /// <returns>true if an item could be removed from the collection within 
        /// the alloted time; otherwise, false.</returns>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout"/> is a negative number
        /// other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater than
        /// <see cref="System.Int32.MaxValue"/>.</exception>
        /// <exception cref="T:System.InvalidOperationException">The underlying collection was modified
        /// outside of this <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance.</exception>
        public bool TryTake([MaybeNullWhen(false)] out T item, TimeSpan timeout)
        {
            ValidateTimeout(timeout);
            return TryTakeWithNoTimeValidation(out item, (int)timeout.TotalMilliseconds, CancellationToken.None, null);
        }

        /// <summary>
        /// Attempts to remove an item from the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/>.
        /// </summary>
        /// <param name="item">The item removed from the collection.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see
        /// cref="System.Threading.Timeout.Infinite"/> (-1) to wait indefinitely.</param>
        /// <returns>true if an item could be removed from the collection within 
        /// the alloted time; otherwise, false.</returns>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a
        /// negative number other than -1, which represents an infinite time-out.</exception>
        /// <exception cref="T:System.InvalidOperationException">The underlying collection was modified
        /// outside of this <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance.</exception>
        public bool TryTake([MaybeNullWhen(false)] out T item, int millisecondsTimeout)
        {
            ValidateMillisecondsTimeout(millisecondsTimeout);
            return TryTakeWithNoTimeValidation(out item, millisecondsTimeout, CancellationToken.None, null);
        }

        /// <summary>
        /// Attempts to remove an item from the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/>.
        /// A <see cref="System.OperationCanceledException"/> is thrown if the <see cref="CancellationToken"/> is
        /// canceled.
        /// </summary>
        /// <param name="item">The item removed from the collection.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see
        /// cref="System.Threading.Timeout.Infinite"/> (-1) to wait indefinitely.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>true if an item could be removed from the collection within 
        /// the alloted time; otherwise, false.</returns>
        /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken"/> is canceled.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a
        /// negative number other than -1, which represents an infinite time-out.</exception>
        /// <exception cref="T:System.InvalidOperationException">The underlying collection was modified
        /// outside of this <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance.</exception>
        public bool TryTake([MaybeNullWhen(false)] out T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            ValidateMillisecondsTimeout(millisecondsTimeout);
            return TryTakeWithNoTimeValidation(out item, millisecondsTimeout, cancellationToken, null);
        }

        /// <summary>Takes an item from the underlying data store using its IProducerConsumerCollection&lt;T&gt;.Take 
        /// method. If the collection was empty, this method will wait for, at most, the timeout period (if AddingIsCompleted is false)
        /// trying to remove an item. If the timeout period was exhausted before successfully removing an item 
        /// this method will return false.
        /// A <see cref="System.OperationCanceledException"/> is thrown if the <see cref="CancellationToken"/> is
        /// canceled.
        /// </summary>
        /// <param name="item">The item removed from the collection.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait for the collection to have an item available 
        /// for removal, or Timeout.Infinite to wait indefinitely.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <param name="combinedTokenSource">A combined cancellation token if created, it is only created by GetConsumingEnumerable to avoid creating the linked token 
        /// multiple times.</param>
        /// <returns>False if the collection remained empty till the timeout period was exhausted. True otherwise.</returns>
        /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken"/> is canceled.</exception>
        /// <exception cref="System.ObjectDisposedException">If the collection has been disposed.</exception>
        private bool TryTakeWithNoTimeValidation([MaybeNullWhen(false)] out T item, int millisecondsTimeout, CancellationToken cancellationToken, CancellationTokenSource? combinedTokenSource)
        {
            CheckDisposed();
            item = default(T)!;

            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(SR.Common_OperationCanceled, cancellationToken);

            //If the collection is completed then there is no need to wait.
            if (IsCompleted)
            {
                return false;
            }
            bool waitForSemaphoreWasSuccessful = false;

            // set the combined token source to the combinedToken parameter if it is not null (came from GetConsumingEnumerable)
            CancellationTokenSource? linkedTokenSource = combinedTokenSource;
            try
            {
                waitForSemaphoreWasSuccessful = _occupiedNodes.Wait(0);
                if (waitForSemaphoreWasSuccessful == false && millisecondsTimeout != 0)
                {
                    // create the linked token if it is not created yet
                    if (linkedTokenSource == null)
                        linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                                                                                          _consumersCancellationTokenSource.Token);
                    waitForSemaphoreWasSuccessful = _occupiedNodes.Wait(millisecondsTimeout, linkedTokenSource.Token);
                }
            }
            //The collection became completed while waiting on the semaphore.
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(SR.Common_OperationCanceled, cancellationToken);

                return false;
            }
            finally
            {
                // only dispose the combined token source if we created it here, otherwise the caller (GetConsumingEnumerable) is responsible for disposing it
                if (linkedTokenSource != null && combinedTokenSource == null)
                {
                    linkedTokenSource.Dispose();
                }
            }

            if (waitForSemaphoreWasSuccessful)
            {
                bool removeSucceeded = false;
                bool removeFaulted = true;
                try
                {
                    //The token may have been canceled before an item arrived, so we need a check after the wait has completed.
                    //This fixes bug #702328, case 1 of 2.
                    cancellationToken.ThrowIfCancellationRequested();

                    //If an item was successfully removed from the underlying collection.
                    removeSucceeded = _collection.TryTake(out item);
                    removeFaulted = false;
                    if (!removeSucceeded)
                    {
                        // Check if the collection is empty which means that the collection was modified outside BlockingCollection
                        throw new InvalidOperationException
                            (SR.BlockingCollection_Take_CollectionModified);
                    }
                }
                finally
                {
                    // removeFaulted implies !removeSucceeded, but the reverse is not true.
                    if (removeSucceeded)
                    {
                        if (_freeNodes != null)
                        {
                            Debug.Assert(_boundedCapacity != NON_BOUNDED);
                            _freeNodes.Release();
                        }
                    }
                    else if (removeFaulted)
                    {
                        _occupiedNodes.Release();
                    }
                    //Last remover will detect that it has actually removed the last item from the 
                    //collection and that CompleteAdding() was called previously. Thus, it will cancel the semaphores
                    //so that any thread waiting on them wakes up. Note several threads may call CancelWaitingConsumers
                    //but this is not a problem.
                    if (IsCompleted)
                    {
                        CancelWaitingConsumers();
                    }
                }
            }
            return waitForSemaphoreWasSuccessful;
        }



        /// <summary>
        /// Adds the specified item to any one of the specified
        /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances.
        /// </summary>
        /// <param name="collections">The array of collections.</param>
        /// <param name="item">The item to be added to one of the collections.</param>
        /// <returns>The index of the collection in the <paramref name="collections"/> array to which the item was added.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="collections"/> argument is
        /// null.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="collections"/> argument is
        /// a 0-length array or contains a null element, or at least one of collections has been
        /// marked as complete for adding.</exception>
        /// <exception cref="T:System.ObjectDisposedException">At least one of the <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances has been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">At least one underlying collection didn't accept the item.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The count of <paramref name="collections"/> is greater than the maximum size of
        /// 62 for STA and 63 for MTA.</exception>
        /// <remarks>
        /// If a bounded capacity was specified when all of the
        /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances were initialized, 
        /// a call to AddToAny may block until space is available in one of the collections
        /// to store the provided item.
        /// </remarks>
        public static int AddToAny(BlockingCollection<T>[] collections, T item)
        {
#if DEBUG
            int tryAddAnyReturnValue =
#else
            return
#endif
                TryAddToAny(collections, item, Timeout.Infinite, CancellationToken.None);
#if DEBUG
            Debug.Assert((tryAddAnyReturnValue >= 0 && tryAddAnyReturnValue < collections.Length)
                                , "TryAddToAny() was expected to return an index within the bounds of the collections array.");
            return tryAddAnyReturnValue;
#endif
        }

        /// <summary>
        /// Adds the specified item to any one of the specified
        /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances.
        /// A <see cref="System.OperationCanceledException"/> is thrown if the <see cref="CancellationToken"/> is
        /// canceled. 
        /// </summary>
        /// <param name="collections">The array of collections.</param>
        /// <param name="item">The item to be added to one of the collections.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>The index of the collection in the <paramref name="collections"/> array to which the item was added.</returns>
        /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken"/> is canceled.</exception>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="collections"/> argument is
        /// null.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="collections"/> argument is
        /// a 0-length array or contains a null element, or at least one of collections has been
        /// marked as complete for adding.</exception>
        /// <exception cref="T:System.ObjectDisposedException">At least one of the <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances has been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">At least one underlying collection didn't accept the item.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The count of <paramref name="collections"/> is greater than the maximum size of
        /// 62 for STA and 63 for MTA.</exception>
        /// <remarks>
        /// If a bounded capacity was specified when all of the
        /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances were initialized, 
        /// a call to AddToAny may block until space is available in one of the collections
        /// to store the provided item.
        /// </remarks>
        public static int AddToAny(BlockingCollection<T>[] collections, T item, CancellationToken cancellationToken)
        {
#if DEBUG
            int tryAddAnyReturnValue =
#else
            return
#endif
                TryAddToAny(collections, item, Timeout.Infinite, cancellationToken);
#if DEBUG
            Debug.Assert((tryAddAnyReturnValue >= 0 && tryAddAnyReturnValue < collections.Length)
                                , "TryAddToAny() was expected to return an index within the bounds of the collections array.");
            return tryAddAnyReturnValue;
#endif
        }

        /// <summary>
        /// Attempts to add the specified item to any one of the specified
        /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances.
        /// </summary>
        /// <param name="collections">The array of collections.</param>
        /// <param name="item">The item to be added to one of the collections.</param>
        /// <returns>The index of the collection in the <paramref name="collections"/> 
        /// array to which the item was added, or -1 if the item could not be added.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="collections"/> argument is
        /// null.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="collections"/> argument is
        /// a 0-length array or contains a null element, or at least one of collections has been
        /// marked as complete for adding.</exception>
        /// <exception cref="T:System.ObjectDisposedException">At least one of the <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances has been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">At least one underlying collection didn't accept the item.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The count of <paramref name="collections"/> is greater than the maximum size of
        /// 62 for STA and 63 for MTA.</exception>
        public static int TryAddToAny(BlockingCollection<T>[] collections, T item)
        {
            return TryAddToAny(collections, item, 0, CancellationToken.None);
        }

        /// <summary>
        /// Attempts to add the specified item to any one of the specified
        /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances.
        /// </summary>
        /// <param name="collections">The array of collections.</param>
        /// <param name="item">The item to be added to one of the collections.</param>
        /// <param name="timeout">A <see cref="System.TimeSpan"/> that represents the number of milliseconds
        /// to wait, or a <see cref="System.TimeSpan"/> that represents -1 milliseconds to wait indefinitely.
        /// </param>
        /// <returns>The index of the collection in the <paramref name="collections"/> 
        /// array to which the item was added, or -1 if the item could not be added.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="collections"/> argument is
        /// null.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="collections"/> argument is
        /// a 0-length array or contains a null element, or at least one of collections has been
        /// marked as complete for adding.</exception>
        /// <exception cref="T:System.ObjectDisposedException">At least one of the <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances has been disposed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout"/> is a negative number
        /// other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater than
        /// <see cref="System.Int32.MaxValue"/>.</exception>
        /// <exception cref="T:System.InvalidOperationException">At least one underlying collection didn't accept the item.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The count of <paramref name="collections"/> is greater than the maximum size of
        /// 62 for STA and 63 for MTA.</exception>
        public static int TryAddToAny(BlockingCollection<T>[] collections, T item, TimeSpan timeout)
        {
            ValidateTimeout(timeout);
            return TryAddToAnyCore(collections, item, (int)timeout.TotalMilliseconds, CancellationToken.None);
        }

        /// <summary>
        /// Attempts to add the specified item to any one of the specified
        /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances.
        /// </summary>
        /// <param name="collections">The array of collections.</param>
        /// <param name="item">The item to be added to one of the collections.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see
        /// cref="System.Threading.Timeout.Infinite"/> (-1) to wait indefinitely.</param>        /// <returns>The index of the collection in the <paramref name="collections"/> 
        /// array to which the item was added, or -1 if the item could not be added.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="collections"/> argument is
        /// null.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="collections"/> argument is
        /// a 0-length array or contains a null element, or at least one of collections has been
        /// marked as complete for adding.</exception>
        /// <exception cref="T:System.ObjectDisposedException">At least one of the <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances has been disposed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a
        /// negative number other than -1, which represents an infinite time-out.</exception>
        /// <exception cref="T:System.InvalidOperationException">At least one underlying collection didn't accept the item.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The count of <paramref name="collections"/> is greater than the maximum size of
        /// 62 for STA and 63 for MTA.</exception>
        public static int TryAddToAny(BlockingCollection<T>[] collections, T item, int millisecondsTimeout)
        {
            ValidateMillisecondsTimeout(millisecondsTimeout);
            return TryAddToAnyCore(collections, item, millisecondsTimeout, CancellationToken.None);
        }

        /// <summary>
        /// Attempts to add the specified item to any one of the specified
        /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances.
        /// A <see cref="System.OperationCanceledException"/> is thrown if the <see cref="CancellationToken"/> is
        /// canceled.
        /// </summary>
        /// <param name="collections">The array of collections.</param>
        /// <param name="item">The item to be added to one of the collections.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see
        /// cref="System.Threading.Timeout.Infinite"/> (-1) to wait indefinitely.</param>        
        /// <returns>The index of the collection in the <paramref name="collections"/> 
        /// array to which the item was added, or -1 if the item could not be added.</returns>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken"/> is canceled.</exception>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="collections"/> argument is
        /// null.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="collections"/> argument is
        /// a 0-length array or contains a null element, or at least one of collections has been
        /// marked as complete for adding.</exception>
        /// <exception cref="T:System.ObjectDisposedException">At least one of the <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances has been disposed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a
        /// negative number other than -1, which represents an infinite time-out.</exception>
        /// <exception cref="T:System.InvalidOperationException">At least one underlying collection didn't accept the item.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The count of <paramref name="collections"/> is greater than the maximum size of
        /// 62 for STA and 63 for MTA.</exception>
        public static int TryAddToAny(BlockingCollection<T>[] collections, T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            ValidateMillisecondsTimeout(millisecondsTimeout);
            return TryAddToAnyCore(collections, item, millisecondsTimeout, cancellationToken);
        }

        /// <summary>Adds an item to anyone of the specified collections.
        /// A <see cref="System.OperationCanceledException"/> is thrown if the <see cref="CancellationToken"/> is
        /// canceled. 
        /// </summary>
        /// <param name="collections">The collections into which the item can be added.</param>
        /// <param name="item">The item to be added .</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait for a collection to accept the 
        /// operation, or -1 to wait indefinitely.</param>
        /// <param name="externalCancellationToken">A cancellation token to observe.</param>
        /// <returns>The index into collections for the collection which accepted the 
        /// adding of the item; -1 if the item could not be added.</returns>
        /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken"/> is canceled.</exception>
        /// <exception cref="System.ArgumentNullException">If the collections argument is null.</exception>
        /// <exception cref="System.ArgumentException">If the collections argument is a 0-length array or contains a 
        /// null element. Also, if at least one of the collections has been marked complete for adds.</exception>
        /// <exception cref="System.ObjectDisposedException">If at least one of the collections has been disposed.</exception>
        private static int TryAddToAnyCore(BlockingCollection<T>[] collections, T item, int millisecondsTimeout, CancellationToken externalCancellationToken)
        {
            ValidateCollectionsArray(collections, true);
            const int OPERATION_FAILED = -1;

            // Copy the wait time to another local variable to update it
            int timeout = millisecondsTimeout;

            uint startTime = 0;
            if (millisecondsTimeout != Timeout.Infinite)
            {
                startTime = unchecked((uint)Environment.TickCount);
            }

            // Fast path for adding if there is at least one unbounded collection
            int index = TryAddToAnyFast(collections, item);
            if (index > -1)
                return index;


            // Get wait handles and the tokens for all collections,
            // and construct a single combined token from all the tokens,
            // add the combined token handle to the handles list
            // call WaitAny for all handles
            // After WaitAny returns check if the token is cancelled and that caused the WaitAny to return or not
            // If the combined token is cancelled, this mean either the external token is cancelled then throw OCE
            // or one if the collection is AddingCompleted then throw AE
            CancellationToken[] collatedCancellationTokens;
            List<WaitHandle> handles = GetHandles(collections, externalCancellationToken, true, out collatedCancellationTokens);

            //Loop until one of these conditions is met:
            // 1- The operation is succeeded
            // 2- The timeout expired for try* versions
            // 3- The external token is cancelled, throw
            // 4- There is at least one collection marked as adding completed then throw
            while (millisecondsTimeout == Timeout.Infinite || timeout >= 0)
            {
                index = -1;

                using (CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(collatedCancellationTokens))
                {
                    handles.Add(linkedTokenSource.Token.WaitHandle); // add the combined token to the handles list

                    //Wait for any collection to become available.
                    index = WaitHandle.WaitAny(handles.ToArray(), timeout);

                    handles.RemoveAt(handles.Count - 1); //remove the linked token

                    if (linkedTokenSource.IsCancellationRequested)
                    {
                        if (externalCancellationToken.IsCancellationRequested) //case#3
                            throw new OperationCanceledException(SR.Common_OperationCanceled, externalCancellationToken);
                        else //case#4
                            throw new ArgumentException(SR.BlockingCollection_CantAddAnyWhenCompleted, nameof(collections));
                    }
                }

                Debug.Assert((index == WaitHandle.WaitTimeout) || (index >= 0 && index < handles.Count));

                if (index == WaitHandle.WaitTimeout) //case#2
                    return OPERATION_FAILED;

                //If the timeout period was not exhausted and the appropriate operation succeeded.
                if (collections[index].TryAdd(item)) //case#1
                    return index;

                // Update the timeout
                if (millisecondsTimeout != Timeout.Infinite)
                    timeout = UpdateTimeOut(startTime, millisecondsTimeout);
            }

            // case #2
            return OPERATION_FAILED;
        }

        /// <summary>
        /// Fast path for TryAddToAny to find a non bounded collection and add the items in it
        /// </summary>
        /// <param name="collections">The collections list</param>
        /// <param name="item">The item to be added</param>
        /// <returns>The index which the item has been added, -1 if failed</returns>
        private static int TryAddToAnyFast(BlockingCollection<T>[] collections, T item)
        {
            for (int i = 0; i < collections.Length; i++)
            {
                if (collections[i]._freeNodes == null)
                {
#if DEBUG
                    bool result =
#endif
                    collections[i].TryAdd(item);
#if DEBUG
                    Debug.Assert(result);
#endif
                    return i;
                }
            }
            return -1;
        }
        /// <summary>
        /// Local static method, used by TryAddTakeAny to get the wait handles for the collection, with exclude option to exclude the Completed collections
        /// </summary>
        /// <param name="collections">The blocking collections</param>
        /// <param name="externalCancellationToken">The original CancellationToken</param>
        /// <param name="isAddOperation">True if Add or TryAdd, false if Take or TryTake</param>
        /// <param name="cancellationTokens">Complete list of cancellationTokens to observe</param>
        /// <returns>The collections wait handles</returns>
        private static List<WaitHandle> GetHandles(BlockingCollection<T>[] collections, CancellationToken externalCancellationToken, bool isAddOperation, out CancellationToken[] cancellationTokens)
        {
            Debug.Assert(collections != null);
            List<WaitHandle> handlesList = new List<WaitHandle>(collections.Length + 1); // + 1 for the external token handle to be added
            List<CancellationToken> tokensList = new List<CancellationToken>(collections.Length + 1); // + 1 for the external token
            tokensList.Add(externalCancellationToken);

            //Read the appropriate WaitHandle based on the operation mode.
            if (isAddOperation)
            {
                for (int i = 0; i < collections.Length; i++)
                {
                    if (collections[i]._freeNodes != null)
                    {
                        handlesList.Add(collections[i]._freeNodes!.AvailableWaitHandle); // TODO-NULLABLE: Indexer nullability tracked (https://github.com/dotnet/roslyn/issues/34644)
                        tokensList.Add(collections[i]._producersCancellationTokenSource.Token);
                    }
                }
            }
            else
            {
                for (int i = 0; i < collections.Length; i++)
                {
                    if (collections[i].IsCompleted) //exclude Completed collections if it is take operation
                        continue;

                    handlesList.Add(collections[i]._occupiedNodes.AvailableWaitHandle);
                    tokensList.Add(collections[i]._consumersCancellationTokenSource.Token);
                }
            }

            cancellationTokens = tokensList.ToArray();
            return handlesList;
        }

        /// <summary>
        /// Helper function to measure and update the wait time
        /// </summary>
        /// <param name="startTime"> The first time (in milliseconds) observed when the wait started</param>
        /// <param name="originalWaitMillisecondsTimeout">The original wait timeoutout in milliseconds</param>
        /// <returns>The new wait time in milliseconds, -1 if the time expired</returns>
        private static int UpdateTimeOut(uint startTime, int originalWaitMillisecondsTimeout)
        {
            if (originalWaitMillisecondsTimeout == 0)
            {
                return 0;
            }
            // The function must be called in case the time out is not infinite
            Debug.Assert(originalWaitMillisecondsTimeout != Timeout.Infinite);

            uint elapsedMilliseconds = unchecked((uint)Environment.TickCount - startTime);

            // Check the elapsed milliseconds is greater than max int because this property is uint
            if (elapsedMilliseconds > int.MaxValue)
            {
                return 0;
            }

            // Subtract the elapsed time from the current wait time
            int currentWaitTimeout = originalWaitMillisecondsTimeout - (int)elapsedMilliseconds; ;
            if (currentWaitTimeout <= 0)
            {
                return 0;
            }

            return currentWaitTimeout;
        }
        /// <summary>
        /// Takes an item from any one of the specified
        /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances.
        /// </summary>
        /// <param name="collections">The array of collections.</param>
        /// <param name="item">The item removed from one of the collections.</param>
        /// <returns>The index of the collection in the <paramref name="collections"/> array from which 
        /// the item was removed, or -1 if an item could not be removed.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="collections"/> argument is
        /// null.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="collections"/> argument is
        /// a 0-length array or contains a null element.</exception>
        /// <exception cref="T:System.ObjectDisposedException">At least one of the <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances has been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">At least one of the underlying collections was modified
        /// outside of its <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The count of <paramref name="collections"/> is greater than the maximum size of
        /// 62 for STA and 63 for MTA.</exception>
        /// <remarks>A call to TakeFromAny may block until an item is available to be removed.</remarks>
        public static int TakeFromAny(BlockingCollection<T>[] collections, [MaybeNullWhen(false)] out T item)
        {
            return TakeFromAny(collections, out item, CancellationToken.None);
        }

        /// <summary>
        /// Takes an item from any one of the specified
        /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances.
        /// A <see cref="System.OperationCanceledException"/> is thrown if the <see cref="CancellationToken"/> is
        /// canceled.
        /// </summary>
        /// <param name="collections">The array of collections.</param>
        /// <param name="item">The item removed from one of the collections.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>The index of the collection in the <paramref name="collections"/> array from which 
        /// the item was removed, or -1 if an item could not be removed.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="collections"/> argument is
        /// null.</exception>
        /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken"/> is canceled.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="collections"/> argument is
        /// a 0-length array or contains a null element.</exception>
        /// <exception cref="T:System.ObjectDisposedException">At least one of the <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances has been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">At least one of the underlying collections was modified
        /// outside of its <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The count of <paramref name="collections"/> is greater than the maximum size of 
        /// 62 for STA and 63 for MTA.</exception>
        /// <remarks>A call to TakeFromAny may block until an item is available to be removed.</remarks>
        public static int TakeFromAny(BlockingCollection<T>[] collections, [MaybeNullWhen(false)] out T item, CancellationToken cancellationToken)
        {
            int returnValue = TryTakeFromAnyCore(collections, out item, Timeout.Infinite, true, cancellationToken);
            Debug.Assert((returnValue >= 0 && returnValue < collections.Length)
                                          , "TryTakeFromAny() was expected to return an index within the bounds of the collections array.");
            return returnValue;
        }

        /// <summary>
        /// Attempts to remove an item from any one of the specified
        /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances.
        /// </summary>
        /// <param name="collections">The array of collections.</param>
        /// <param name="item">The item removed from one of the collections.</param>
        /// <returns>The index of the collection in the <paramref name="collections"/> array from which 
        /// the item was removed, or -1 if an item could not be removed.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="collections"/> argument is
        /// null.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="collections"/> argument is
        /// a 0-length array or contains a null element.</exception>
        /// <exception cref="T:System.ObjectDisposedException">At least one of the <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances has been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">At least one of the underlying collections was modified
        /// outside of its <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The count of <paramref name="collections"/> is greater than the maximum size of
        /// 62 for STA and 63 for MTA.</exception>
        /// <remarks>A call to TryTakeFromAny may block until an item is available to be removed.</remarks>
        public static int TryTakeFromAny(BlockingCollection<T>[] collections, [MaybeNullWhen(false)] out T item)
        {
            return TryTakeFromAny(collections, out item, 0);
        }

        /// <summary>
        /// Attempts to remove an item from any one of the specified
        /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances.
        /// </summary>
        /// <param name="collections">The array of collections.</param>
        /// <param name="item">The item removed from one of the collections.</param>
        /// <param name="timeout">A <see cref="System.TimeSpan"/> that represents the number of milliseconds
        /// to wait, or a <see cref="System.TimeSpan"/> that represents -1 milliseconds to wait indefinitely.
        /// </param>
        /// <returns>The index of the collection in the <paramref name="collections"/> array from which 
        /// the item was removed, or -1 if an item could not be removed.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="collections"/> argument is
        /// null.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="collections"/> argument is
        /// a 0-length array or contains a null element.</exception>
        /// <exception cref="T:System.ObjectDisposedException">At least one of the <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances has been disposed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout"/> is a negative number
        /// other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater than
        /// <see cref="System.Int32.MaxValue"/>.</exception>
        /// <exception cref="T:System.InvalidOperationException">At least one of the underlying collections was modified
        /// outside of its <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The count of <paramref name="collections"/> is greater than the maximum size of
        /// 62 for STA and 63 for MTA.</exception>
        /// <remarks>A call to TryTakeFromAny may block until an item is available to be removed.</remarks>
        public static int TryTakeFromAny(BlockingCollection<T>[] collections, [MaybeNullWhen(false)] out T item, TimeSpan timeout)
        {
            ValidateTimeout(timeout);
            return TryTakeFromAnyCore(collections, out item, (int)timeout.TotalMilliseconds, false, CancellationToken.None);
        }

        /// <summary>
        /// Attempts to remove an item from any one of the specified
        /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances.
        /// </summary>
        /// <param name="collections">The array of collections.</param>
        /// <param name="item">The item removed from one of the collections.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see
        /// cref="System.Threading.Timeout.Infinite"/> (-1) to wait indefinitely.</param>
        /// <returns>The index of the collection in the <paramref name="collections"/> array from which 
        /// the item was removed, or -1 if an item could not be removed.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="collections"/> argument is
        /// null.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="collections"/> argument is
        /// a 0-length array or contains a null element.</exception>
        /// <exception cref="T:System.ObjectDisposedException">At least one of the <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances has been disposed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a
        /// negative number other than -1, which represents an infinite time-out.</exception>
        /// <exception cref="T:System.InvalidOperationException">At least one of the underlying collections was modified
        /// outside of its <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The count of <paramref name="collections"/> is greater than the maximum size of
        /// 62 for STA and 63 for MTA.</exception>
        /// <remarks>A call to TryTakeFromAny may block until an item is available to be removed.</remarks>
        public static int TryTakeFromAny(BlockingCollection<T>[] collections, [MaybeNullWhen(false)] out T item, int millisecondsTimeout)
        {
            ValidateMillisecondsTimeout(millisecondsTimeout);
            return TryTakeFromAnyCore(collections, out item, millisecondsTimeout, false, CancellationToken.None);
        }

        /// <summary>
        /// Attempts to remove an item from any one of the specified
        /// <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances.
        /// A <see cref="System.OperationCanceledException"/> is thrown if the <see cref="CancellationToken"/> is
        /// canceled. 
        /// </summary>
        /// <param name="collections">The array of collections.</param>
        /// <param name="item">The item removed from one of the collections.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see
        /// cref="System.Threading.Timeout.Infinite"/> (-1) to wait indefinitely.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>The index of the collection in the <paramref name="collections"/> array from which 
        /// the item was removed, or -1 if an item could not be removed.</returns>
        /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken"/> is canceled.</exception>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="collections"/> argument is
        /// null.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="collections"/> argument is
        /// a 0-length array or contains a null element.</exception>
        /// <exception cref="T:System.ObjectDisposedException">At least one of the <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances has been disposed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a
        /// negative number other than -1, which represents an infinite time-out.</exception>
        /// <exception cref="T:System.InvalidOperationException">At least one of the underlying collections was modified
        /// outside of its <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The count of <paramref name="collections"/> is greater than the maximum size of
        /// 62 for STA and 63 for MTA.</exception>
        /// <remarks>A call to TryTakeFromAny may block until an item is available to be removed.</remarks>
        public static int TryTakeFromAny(BlockingCollection<T>[] collections, [MaybeNullWhen(false)] out T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            ValidateMillisecondsTimeout(millisecondsTimeout);
            return TryTakeFromAnyCore(collections, out item, millisecondsTimeout, false, cancellationToken);
        }

        /// <summary>Takes an item from anyone of the specified collections.
        /// A <see cref="System.OperationCanceledException"/> is thrown if the <see cref="CancellationToken"/> is
        /// canceled. 
        /// </summary>
        /// <param name="collections">The collections from which the item can be removed.</param>
        /// <param name="item">The item removed and returned to the caller.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait for a collection to accept the 
        /// operation, or -1 to wait indefinitely.</param>
        /// <param name="isTakeOperation">True if Take, false if TryTake.</param>
        /// <param name="externalCancellationToken">A cancellation token to observe.</param>
        /// <returns>The index into collections for the collection which accepted the 
        /// removal of the item; -1 if the item could not be removed.</returns>
        /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken"/> is canceled.</exception>
        /// <exception cref="System.ArgumentNullException">If the collections argument is null.</exception>
        /// <exception cref="System.ArgumentException">If the collections argument is a 0-length array or contains a 
        /// null element. Also, if at least one of the collections has been marked complete for adds.</exception>
        /// <exception cref="System.ObjectDisposedException">If at least one of the collections has been disposed.</exception>
        private static int TryTakeFromAnyCore(BlockingCollection<T>[] collections, [MaybeNullWhen(false)] out T item, int millisecondsTimeout, bool isTakeOperation, CancellationToken externalCancellationToken)
        {
            ValidateCollectionsArray(collections, false);

            //try the fast path first
            for (int i = 0; i < collections.Length; i++)
            {
                // Check if the collection is not completed, and potentially has at least one element by checking the semaphore count
                if (!collections[i].IsCompleted && collections[i]._occupiedNodes.CurrentCount > 0 && collections[i].TryTake(out item))
                    return i;
            }

            //Fast path failed, try the slow path
            return TryTakeFromAnyCoreSlow(collections, out item, millisecondsTimeout, isTakeOperation, externalCancellationToken);
        }


        /// <summary>Takes an item from anyone of the specified collections.
        /// A <see cref="System.OperationCanceledException"/> is thrown if the <see cref="CancellationToken"/> is
        /// canceled. 
        /// </summary>
        /// <param name="collections">The collections copy from which the item can be removed.</param>
        /// <param name="item">The item removed and returned to the caller.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait for a collection to accept the 
        /// operation, or -1 to wait indefinitely.</param>
        /// <param name="isTakeOperation">True if Take, false if TryTake.</param>
        /// <param name="externalCancellationToken">A cancellation token to observe.</param>
        /// <returns>The index into collections for the collection which accepted the 
        /// removal of the item; -1 if the item could not be removed.</returns>
        /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken"/> is canceled.</exception>
        /// <exception cref="System.ArgumentNullException">If the collections argument is null.</exception>
        /// <exception cref="System.ArgumentException">If the collections argument is a 0-length array or contains a 
        /// null element. Also, if at least one of the collections has been marked complete for adds.</exception>
        /// <exception cref="System.ObjectDisposedException">If at least one of the collections has been disposed.</exception>
        private static int TryTakeFromAnyCoreSlow(BlockingCollection<T>[] collections, [MaybeNullWhen(false)] out T item, int millisecondsTimeout, bool isTakeOperation, CancellationToken externalCancellationToken)
        {
            const int OPERATION_FAILED = -1;

            // Copy the wait time to another local variable to update it
            int timeout = millisecondsTimeout;

            uint startTime = 0;
            if (millisecondsTimeout != Timeout.Infinite)
            {
                startTime = unchecked((uint)Environment.TickCount);
            }


            //Loop until one of these conditions is met:
            // 1- The operation is succeeded
            // 2- The timeout expired for try* versions
            // 3- The external token is cancelled, throw
            // 4- The operation is TryTake and all collections are marked as completed, return false
            // 5- The operation is Take and all collection are marked as completed, throw
            while (millisecondsTimeout == Timeout.Infinite || timeout >= 0)
            {
                // Get wait handles and the tokens for all collections,
                // and construct a single combined token from all the tokens,
                // add the combined token handle to the handles list
                // call WaitAny for all handles
                // After WaitAny returns check if the token is cancelled and that caused the WaitAny to return or not
                // If the combined token is cancelled, this mean either the external token is cancelled then throw OCE
                // or one if the collection is Completed then exclude it and retry
                CancellationToken[] collatedCancellationTokens;
                List<WaitHandle> handles = GetHandles(collections, externalCancellationToken, false, out collatedCancellationTokens);

                if (handles.Count == 0 && isTakeOperation) //case#5
                    throw new ArgumentException(SR.BlockingCollection_CantTakeAnyWhenAllDone, nameof(collections));

                else if (handles.Count == 0) //case#4
                    break;


                //Wait for any collection to become available.
                using (CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(collatedCancellationTokens))
                {
                    handles.Add(linkedTokenSource.Token.WaitHandle); // add the combined token to the handles list
                    int index = WaitHandle.WaitAny(handles.ToArray(), timeout);

                    if (linkedTokenSource.IsCancellationRequested && externalCancellationToken.IsCancellationRequested)//case#3
                        throw new OperationCanceledException(SR.Common_OperationCanceled, externalCancellationToken);


                    else if (!linkedTokenSource.IsCancellationRequested) // if neither internal nor external cancellation requested
                    {
                        Debug.Assert((index == WaitHandle.WaitTimeout) || (index >= 0 && index < handles.Count));
                        if (index == WaitHandle.WaitTimeout) //case#2
                            break;

                        // adjust the index in case one or more handles removed because they are completed
                        if (collections.Length != handles.Count - 1) // -1 because of the combined token handle
                        {
                            for (int i = 0; i < collections.Length; i++)
                            {
                                if (collections[i]._occupiedNodes.AvailableWaitHandle == handles[index])
                                {
                                    index = i;
                                    break;
                                }
                            }
                        }

                        if (collections[index].TryTake(out item)) //case#1
                            return index;
                    }
                }

                // Update the timeout
                if (millisecondsTimeout != Timeout.Infinite)
                    timeout = UpdateTimeOut(startTime, millisecondsTimeout);
            }

            item = default(T)!; //case#2
            return OPERATION_FAILED;
        }

        /// <summary>
        /// Marks the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instances
        /// as not accepting any more additions.  
        /// </summary>
        /// <remarks>
        /// After a collection has been marked as complete for adding, adding to the collection is not permitted 
        /// and attempts to remove from the collection will not wait when the collection is empty.
        /// </remarks>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        public void CompleteAdding()
        {
            CheckDisposed();

            if (IsAddingCompleted)
                return;

            SpinWait spinner = new SpinWait();
            while (true)
            {
                int observedAdders = _currentAdders;
                if ((observedAdders & COMPLETE_ADDING_ON_MASK) != 0)
                {
                    spinner.Reset();
                    // If there is another CompleteAdding in progress waiting the current adders, then spin until it finishes
                    while (_currentAdders != COMPLETE_ADDING_ON_MASK) spinner.SpinOnce();
                    return;
                }

                if (Interlocked.CompareExchange(ref _currentAdders, observedAdders | COMPLETE_ADDING_ON_MASK, observedAdders) == observedAdders)
                {
                    spinner.Reset();
                    while (_currentAdders != COMPLETE_ADDING_ON_MASK) spinner.SpinOnce();

                    if (Count == 0)
                    {
                        CancelWaitingConsumers();
                    }

                    // We should always wake waiting producers, and have them throw exceptions as
                    // Add&CompleteAdding should not be used concurrently.
                    CancelWaitingProducers();
                    return;
                }
                spinner.SpinOnce(sleep1Threshold: -1);
            }
        }

        /// <summary>Cancels the semaphores.</summary>
        private void CancelWaitingConsumers()
        {
            _consumersCancellationTokenSource.Cancel();
        }

        private void CancelWaitingProducers()
        {
            _producersCancellationTokenSource.Cancel();
        }


        /// <summary>
        /// Releases resources used by the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources used by the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance.
        /// </summary>
        /// <param name="disposing">Whether being disposed explicitly (true) or due to a finalizer (false).</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (_freeNodes != null)
                {
                    _freeNodes.Dispose();
                }

                _occupiedNodes.Dispose();

                _isDisposed = true;
            }
        }

        /// <summary>Copies the items from the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance into a new array.</summary>
        /// <returns>An array containing copies of the elements of the collection.</returns>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        /// <remarks>
        /// The copied elements are not removed from the collection.
        /// </remarks>
        public T[] ToArray()
        {
            CheckDisposed();
            return _collection.ToArray();
        }

        /// <summary>Copies all of the items in the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance 
        /// to a compatible one-dimensional array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from 
        /// the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance. The array must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="array"/> argument is
        /// null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">The <paramref name="index"/> argument is less than zero.</exception>
        /// <exception cref="System.ArgumentException">The <paramref name="index"/> argument is equal to or greater 
        /// than the length of the <paramref name="array"/>.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        public void CopyTo(T[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        /// <summary>Copies all of the items in the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance 
        /// to a compatible one-dimensional array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from 
        /// the <see cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> instance. The array must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="array"/> argument is
        /// null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">The <paramref name="index"/> argument is less than zero.</exception>
        /// <exception cref="System.ArgumentException">The <paramref name="index"/> argument is equal to or greater 
        /// than the length of the <paramref name="array"/>, the array is multidimensional, or the type parameter for the collection 
        /// cannot be cast automatically to the type of the destination array.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        void ICollection.CopyTo(Array array, int index)
        {
            CheckDisposed();

            //We don't call _collection.CopyTo() directly because we rely on Array.Copy method to customize 
            //all array exceptions.  
            T[] collectionSnapShot = _collection.ToArray();

            try
            {
                Array.Copy(collectionSnapShot, 0, array, index, collectionSnapShot.Length);
            }
            catch (ArgumentNullException)
            {
                throw new ArgumentNullException(nameof(array));
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.BlockingCollection_CopyTo_NonNegative);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException(SR.Collection_CopyTo_TooManyElems, nameof(index));
            }
            catch (RankException)
            {
                throw new ArgumentException(SR.BlockingCollection_CopyTo_MultiDim, nameof(array));
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(SR.BlockingCollection_CopyTo_IncorrectType, nameof(array));
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException(SR.BlockingCollection_CopyTo_IncorrectType, nameof(array));
            }
        }

        /// <summary>Provides a consuming <see cref="T:System.Collections.Generics.IEnumerable{T}"/> for items in the collection.</summary>
        /// <returns>An <see cref="T:System.Collections.Generics.IEnumerable{T}"/> that removes and returns items from the collection.</returns>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        public IEnumerable<T> GetConsumingEnumerable()
        {
            return GetConsumingEnumerable(CancellationToken.None);
        }

        /// <summary>Provides a consuming <see cref="T:System.Collections.Generics.IEnumerable{T}"/> for items in the collection.
        /// Calling MoveNext on the returned enumerable will block if there is no data available, or will
        /// throw an <see cref="System.OperationCanceledException"/> if the <see cref="CancellationToken"/> is canceled.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>An <see cref="T:System.Collections.Generics.IEnumerable{T}"/> that removes and returns items from the collection.</returns>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken"/> is canceled.</exception>
        public IEnumerable<T> GetConsumingEnumerable(CancellationToken cancellationToken)
        {
            CancellationTokenSource? linkedTokenSource = null;
            try
            {
                linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _consumersCancellationTokenSource.Token);
                while (!IsCompleted)
                {
                    T item;
                    if (TryTakeWithNoTimeValidation(out item, Timeout.Infinite, cancellationToken, linkedTokenSource))
                    {
                        yield return item;
                    }
                }
            }
            finally
            {
                if (linkedTokenSource != null)
                {
                    linkedTokenSource.Dispose();
                }
            }
        }

        /// <summary>Provides an <see cref="T:System.Collections.Generics.IEnumerator{T}"/> for items in the collection.</summary>
        /// <returns>An <see cref="T:System.Collections.Generics.IEnumerator{T}"/> for the items in the collection.</returns>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            CheckDisposed();
            return _collection.GetEnumerator();
        }

        /// <summary>Provides an <see cref="T:System.Collections.IEnumerator"/> for items in the collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator"/> for the items in the collection.</returns>
        /// <exception cref="T:System.ObjectDisposedException">The <see
        /// cref="T:System.Collections.Concurrent.BlockingCollection{T}"/> has been disposed.</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        /// <summary>Centralizes the logic for validating the BlockingCollections array passed to TryAddToAny()
        /// and TryTakeFromAny().</summary>
        /// <param name="collections">The collections to/from which an item should be added/removed.</param>
        /// <param name="isAddOperation">Indicates whether this method is called to Add or Take.</param>
        /// <exception cref="System.ArgumentNullException">If the collections argument is null.</exception>
        /// <exception cref="System.ArgumentException">If the collections argument is a 0-length array or contains a 
        /// null element. Also, if at least one of the collections has been marked complete for adds.</exception>
        /// <exception cref="System.ObjectDisposedException">If at least one of the collections has been disposed.</exception>
        private static void ValidateCollectionsArray(BlockingCollection<T>[] collections, bool isAddOperation)
        {
            if (collections == null)
            {
                throw new ArgumentNullException(nameof(collections));
            }
            else if (collections.Length < 1)
            {
                throw new ArgumentException(
                    SR.BlockingCollection_ValidateCollectionsArray_ZeroSize, nameof(collections));
            }
            else if ((!IsSTAThread && collections.Length > 63) || (IsSTAThread && collections.Length > 62))
            //The number of WaitHandles must be <= 64 for MTA, and <=63 for STA, and we reserve one for CancellationToken                
            {
                throw new ArgumentOutOfRangeException(
nameof(collections), SR.BlockingCollection_ValidateCollectionsArray_LargeSize);
            }

            for (int i = 0; i < collections.Length; ++i)
            {
                if (collections[i] == null)
                {
                    throw new ArgumentException(
                        SR.BlockingCollection_ValidateCollectionsArray_NullElems, nameof(collections));
                }

                if (collections[i]._isDisposed)
                    throw new ObjectDisposedException(
nameof(collections), SR.BlockingCollection_ValidateCollectionsArray_DispElems);

                if (isAddOperation && collections[i].IsAddingCompleted)
                {
                    throw new ArgumentException(
                        SR.BlockingCollection_CantAddAnyWhenCompleted, nameof(collections));
                }
            }
        }

        private static bool IsSTAThread
        {
            get
            {
                return false;
            }
        }

        // ---------
        // Private Helpers.
        /// <summary>Centralizes the logic of validating the timeout input argument.</summary>
        /// <param name="timeout">The TimeSpan to wait for to successfully complete an operation on the collection.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">If the number of milliseconds represented by the timeout 
        /// TimeSpan is less than 0 or is larger than Int32.MaxValue and not Timeout.Infinite</exception>
        private static void ValidateTimeout(TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if ((totalMilliseconds < 0 || totalMilliseconds > int.MaxValue) && (totalMilliseconds != Timeout.Infinite))
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout,
                    SR.Format(CultureInfo.InvariantCulture, SR.BlockingCollection_TimeoutInvalid, int.MaxValue));
            }
        }

        /// <summary>Centralizes the logic of validating the millisecondsTimeout input argument.</summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait for to successfully complete an 
        /// operation on the collection.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">If the number of milliseconds is less than 0 and not 
        /// equal to Timeout.Infinite.</exception>
        private static void ValidateMillisecondsTimeout(int millisecondsTimeout)
        {
            if ((millisecondsTimeout < 0) && (millisecondsTimeout != Timeout.Infinite))
            {
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), millisecondsTimeout,
                    SR.Format(CultureInfo.InvariantCulture, SR.BlockingCollection_TimeoutInvalid, int.MaxValue));
            }
        }

        /// <summary>Throws a System.ObjectDisposedException if the collection was disposed</summary>
        /// <exception cref="System.ObjectDisposedException">If the collection has been disposed.</exception>
        private void CheckDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(BlockingCollection<T>), SR.BlockingCollection_Disposed);
            }
        }
    }

    /// <summary>A debugger view of the blocking collection that makes it simple to browse the
    /// collection's contents at a point in time.</summary>
    /// <typeparam name="T">The type of element that the BlockingCollection will hold.</typeparam>
    internal sealed class BlockingCollectionDebugView<T>
    {
        private readonly BlockingCollection<T> _blockingCollection; // The collection being viewed.

        /// <summary>Constructs a new debugger view object for the provided blocking collection object.</summary>
        /// <param name="collection">A blocking collection to browse in the debugger.</param>
        public BlockingCollectionDebugView(BlockingCollection<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            _blockingCollection = collection;
        }

        /// <summary>Returns a snapshot of the underlying collection's elements.</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                return _blockingCollection.ToArray();
            }
        }
    }
}
