// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Concurrent
{
    /// <summary>Debug view for the IProducerConsumerCollection.</summary>
    /// <typeparam name="T">Specifies the type of the data being aggregated.</typeparam>
    internal sealed class IProducerConsumerCollection_DebugView<T>
    {
        private IProducerConsumerCollection<T> _collection;

        public IProducerConsumerCollection_DebugView(IProducerConsumerCollection<T> collection)
        {
            _collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Values { get { return _collection.ToArray(); } }
    }

    /// <summary>
    /// Provides a base implementation for producer-consumer collections that wrap other
    /// producer-consumer collections.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the collection.</typeparam>
    public abstract class ProducerConsumerCollectionBase<T> : IProducerConsumerCollection<T>
    {
        private readonly IProducerConsumerCollection<T> _contained;

        /// <summary>Initializes the ProducerConsumerCollectionBase instance.</summary>
        /// <param name="contained">The collection to be wrapped by this instance.</param>
        protected ProducerConsumerCollectionBase(IProducerConsumerCollection<T> contained)
        {
            if (contained == null) throw new ArgumentNullException("contained");
            _contained = contained;
        }

        /// <summary>Gets the contained collection.</summary>
        protected IProducerConsumerCollection<T> ContainedCollection { get { return _contained; } }

        /// <summary>Attempts to add the specified value to the end of the deque.</summary>
        /// <param name="item">The item to add.</param>
        /// <returns>true if the item could be added; otherwise, false.</returns>
        protected virtual bool TryAdd(T item) { return _contained.TryAdd(item); }

        /// <summary>Attempts to remove and return an item from the collection.</summary>
        /// <param name="item">
        /// When this method returns, if the operation was successful, item contains the item removed. If
        /// no item was available to be removed, the value is unspecified.
        /// </param>
        /// <returns>
        /// true if an element was removed and returned from the collection; otherwise, false.
        /// </returns>
        protected virtual bool TryTake(out T item) { return _contained.TryTake(out item); }

        /// <summary>Attempts to add the specified value to the end of the deque.</summary>
        /// <param name="item">The item to add.</param>
        /// <returns>true if the item could be added; otherwise, false.</returns>
        bool IProducerConsumerCollection<T>.TryAdd(T item) { return TryAdd(item); }

        /// <summary>Attempts to remove and return an item from the collection.</summary>
        /// <param name="item">
        /// When this method returns, if the operation was successful, item contains the item removed. If
        /// no item was available to be removed, the value is unspecified.
        /// </param>
        /// <returns>
        /// true if an element was removed and returned from the collection; otherwise, false.
        /// </returns>
        bool IProducerConsumerCollection<T>.TryTake(out T item) { return TryTake(out item); }

        /// <summary>Gets the number of elements contained in the collection.</summary>
        public int Count { get { return _contained.Count; } }

        /// <summary>Creates an array containing the contents of the collection.</summary>
        /// <returns>The array.</returns>
        public T[] ToArray() { return _contained.ToArray(); }

        /// <summary>Copies the contents of the collection to an array.</summary>
        /// <param name="array">The array to which the data should be copied.</param>
        /// <param name="index">The starting index at which data should be copied.</param>
        public void CopyTo(T[] array, int index) { _contained.CopyTo(array, index); }

        /// <summary>Copies the contents of the collection to an array.</summary>
        /// <param name="array">The array to which the data should be copied.</param>
        /// <param name="index">The starting index at which data should be copied.</param>
        void ICollection.CopyTo(Array array, int index) { _contained.CopyTo(array, index); }

        /// <summary>Gets an enumerator for the collection.</summary>
        /// <returns>An enumerator.</returns>
        public IEnumerator<T> GetEnumerator() { return _contained.GetEnumerator(); }

        /// <summary>Gets an enumerator for the collection.</summary>
        /// <returns>An enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        /// <summary>Gets whether the collection is synchronized.</summary>
        bool ICollection.IsSynchronized { get { return _contained.IsSynchronized; } }

        /// <summary>Gets the synchronization root object for the collection.</summary>
        object ICollection.SyncRoot { get { return _contained.SyncRoot; } }
    }
}
