// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// PartitionerStatic.cs
//
// A class of default partitioners for Partitioner<TSource>
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// Out-of-the-box partitioners are created with a set of default behaviors.  
    /// For example, by default, some form of buffering and chunking will be employed to achieve 
    /// optimal performance in the common scenario where an <see cref="IEnumerable{T}"/> implementation is fast and 
    /// non-blocking.  These behaviors can be overridden via this enumeration.
    /// </summary>
    [Flags]
    public enum EnumerablePartitionerOptions
    {
        /// <summary>
        /// Use the default behavior (i.e., use buffering to achieve optimal performance)
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Creates a partitioner that will take items from the source enumerable one at a time
        /// and will not use intermediate storage that can be accessed more efficiently by multiple threads.  
        /// This option provides support for low latency (items will be processed as soon as they are available from 
        /// the source) and partial support for dependencies between items (a thread cannot deadlock waiting for an item 
        /// that it, itself, is responsible for processing).
        /// </summary>
        NoBuffering = 0x1
    }

    // The static class Partitioners implements 3 default partitioning strategies:
    // 1. dynamic load balance partitioning for indexable data source (IList and arrays)
    // 2. static partitioning for indexable data source (IList and arrays)
    // 3. dynamic load balance partitioning for enumerables. Enumerables have indexes, which are the natural order
    //    of elements, but enumerators are not indexable 
    // - data source of type IList/arrays have both dynamic and static partitioning, as 1 and 3.
    //   We assume that the source data of IList/Array is not changing concurrently.
    // - data source of type IEnumerable can only be partitioned dynamically (load-balance)
    // - Dynamic partitioning methods 1 and 3 are same, both being dynamic and load-balance. But the 
    //   implementation is different for data source of IList/Array vs. IEnumerable:
    //   * When the source collection is IList/Arrays, we use Interlocked on the shared index; 
    //   * When the source collection is IEnumerable, we use Monitor to wrap around the access to the source 
    //     enumerator.

    /// <summary>
    /// Provides common partitioning strategies for arrays, lists, and enumerables.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The static methods on <see cref="Partitioner"/> are all thread-safe and may be used concurrently
    /// from multiple threads. However, while a created partitioner is in use, the underlying data source
    /// should not be modified, whether from the same thread that's using a partitioner or from a separate
    /// thread.
    /// </para>
    /// </remarks>
    public static class Partitioner
    {
        /// <summary>
        /// Creates an orderable partitioner from an <see cref="System.Collections.Generic.IList{T}"/>
        /// instance.
        /// </summary>
        /// <typeparam name="TSource">Type of the elements in source list.</typeparam>
        /// <param name="list">The list to be partitioned.</param>
        /// <param name="loadBalance">
        /// A Boolean value that indicates whether the created partitioner should dynamically
        /// load balance between partitions rather than statically partition.
        /// </param>
        /// <returns>
        /// An orderable partitioner based on the input list.
        /// </returns>
        public static OrderablePartitioner<TSource> Create<TSource>(IList<TSource> list, bool loadBalance)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }
            if (loadBalance)
            {
                return (new DynamicPartitionerForIList<TSource>(list));
            }
            else
            {
                return (new StaticIndexRangePartitionerForIList<TSource>(list));
            }
        }

        /// <summary>
        /// Creates an orderable partitioner from a <see cref="System.Array"/> instance.
        /// </summary>
        /// <typeparam name="TSource">Type of the elements in source array.</typeparam>
        /// <param name="array">The array to be partitioned.</param>
        /// <param name="loadBalance">
        /// A Boolean value that indicates whether the created partitioner should dynamically load balance
        /// between partitions rather than statically partition.
        /// </param>
        /// <returns>
        /// An orderable partitioner based on the input array.
        /// </returns>
        public static OrderablePartitioner<TSource> Create<TSource>(TSource[] array, bool loadBalance)
        {
            // This implementation uses 'ldelem' instructions for element retrieval, rather than using a
            // method call.

            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (loadBalance)
            {
                return (new DynamicPartitionerForArray<TSource>(array));
            }
            else
            {
                return (new StaticIndexRangePartitionerForArray<TSource>(array));
            }
        }

        /// <summary>
        /// Creates an orderable partitioner from a <see cref="System.Collections.Generic.IEnumerable{TSource}"/> instance.
        /// </summary>
        /// <typeparam name="TSource">Type of the elements in source enumerable.</typeparam>
        /// <param name="source">The enumerable to be partitioned.</param>
        /// <returns>
        /// An orderable partitioner based on the input array.
        /// </returns>
        /// <remarks>
        /// The ordering used in the created partitioner is determined by the natural order of the elements 
        /// as retrieved from the source enumerable.
        /// </remarks>
        public static OrderablePartitioner<TSource> Create<TSource>(IEnumerable<TSource> source)
        {
            return Create<TSource>(source, EnumerablePartitionerOptions.None);
        }

        /// <summary>
        /// Creates an orderable partitioner from a <see cref="System.Collections.Generic.IEnumerable{TSource}"/> instance.
        /// </summary>
        /// <typeparam name="TSource">Type of the elements in source enumerable.</typeparam>
        /// <param name="source">The enumerable to be partitioned.</param>
        /// <param name="partitionerOptions">Options to control the buffering behavior of the partitioner.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The <paramref name="partitionerOptions"/> argument specifies an invalid value for <see
        /// cref="T:System.Collections.Concurrent.EnumerablePartitionerOptions"/>.
        /// </exception>
        /// <returns>
        /// An orderable partitioner based on the input array.
        /// </returns>
        /// <remarks>
        /// The ordering used in the created partitioner is determined by the natural order of the elements 
        /// as retrieved from the source enumerable.
        /// </remarks>
        public static OrderablePartitioner<TSource> Create<TSource>(IEnumerable<TSource> source, EnumerablePartitionerOptions partitionerOptions)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if ((partitionerOptions & (~EnumerablePartitionerOptions.NoBuffering)) != 0)
                throw new ArgumentOutOfRangeException(nameof(partitionerOptions));

            return (new DynamicPartitionerForIEnumerable<TSource>(source, partitionerOptions));
        }

        /// <summary>Creates a partitioner that chunks the user-specified range.</summary>
        /// <param name="fromInclusive">The lower, inclusive bound of the range.</param>
        /// <param name="toExclusive">The upper, exclusive bound of the range.</param>
        /// <returns>A partitioner.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException"> The <paramref name="toExclusive"/> argument is 
        /// less than or equal to the <paramref name="fromInclusive"/> argument.</exception>
        public static OrderablePartitioner<Tuple<long, long>> Create(long fromInclusive, long toExclusive)
        {
            // How many chunks do we want to divide the range into?  If this is 1, then the
            // answer is "one chunk per core".  Generally, though, you'll achieve better
            // load balancing on a busy system if you make it higher than 1.
            int coreOversubscriptionRate = 3;

            if (toExclusive <= fromInclusive) throw new ArgumentOutOfRangeException(nameof(toExclusive));
            long rangeSize = (toExclusive - fromInclusive) /
                (PlatformHelper.ProcessorCount * coreOversubscriptionRate);
            if (rangeSize == 0) rangeSize = 1;
            return Partitioner.Create(CreateRanges(fromInclusive, toExclusive, rangeSize), EnumerablePartitionerOptions.NoBuffering); // chunk one range at a time
        }

        /// <summary>Creates a partitioner that chunks the user-specified range.</summary>
        /// <param name="fromInclusive">The lower, inclusive bound of the range.</param>
        /// <param name="toExclusive">The upper, exclusive bound of the range.</param>
        /// <param name="rangeSize">The size of each subrange.</param>
        /// <returns>A partitioner.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException"> The <paramref name="toExclusive"/> argument is 
        /// less than or equal to the <paramref name="fromInclusive"/> argument.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"> The <paramref name="rangeSize"/> argument is 
        /// less than or equal to 0.</exception>
        public static OrderablePartitioner<Tuple<long, long>> Create(long fromInclusive, long toExclusive, long rangeSize)
        {
            if (toExclusive <= fromInclusive) throw new ArgumentOutOfRangeException(nameof(toExclusive));
            if (rangeSize <= 0) throw new ArgumentOutOfRangeException(nameof(rangeSize));
            return Partitioner.Create(CreateRanges(fromInclusive, toExclusive, rangeSize), EnumerablePartitionerOptions.NoBuffering); // chunk one range at a time
        }

        // Private method to parcel out range tuples.
        private static IEnumerable<Tuple<long, long>> CreateRanges(long fromInclusive, long toExclusive, long rangeSize)
        {
            // Enumerate all of the ranges
            long from, to;
            bool shouldQuit = false;

            for (long i = fromInclusive; (i < toExclusive) && !shouldQuit; i = unchecked(i + rangeSize))
            {
                from = i;
                try { checked { to = i + rangeSize; } }
                catch (OverflowException)
                {
                    to = toExclusive;
                    shouldQuit = true;
                }
                if (to > toExclusive) to = toExclusive;
                yield return new Tuple<long, long>(from, to);
            }
        }

        /// <summary>Creates a partitioner that chunks the user-specified range.</summary>
        /// <param name="fromInclusive">The lower, inclusive bound of the range.</param>
        /// <param name="toExclusive">The upper, exclusive bound of the range.</param>
        /// <returns>A partitioner.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException"> The <paramref name="toExclusive"/> argument is 
        /// less than or equal to the <paramref name="fromInclusive"/> argument.</exception>
        public static OrderablePartitioner<Tuple<int, int>> Create(int fromInclusive, int toExclusive)
        {
            // How many chunks do we want to divide the range into?  If this is 1, then the
            // answer is "one chunk per core".  Generally, though, you'll achieve better
            // load balancing on a busy system if you make it higher than 1.
            int coreOversubscriptionRate = 3;

            if (toExclusive <= fromInclusive) throw new ArgumentOutOfRangeException(nameof(toExclusive));
            int rangeSize = (toExclusive - fromInclusive) /
                (PlatformHelper.ProcessorCount * coreOversubscriptionRate);
            if (rangeSize == 0) rangeSize = 1;
            return Partitioner.Create(CreateRanges(fromInclusive, toExclusive, rangeSize), EnumerablePartitionerOptions.NoBuffering); // chunk one range at a time
        }

        /// <summary>Creates a partitioner that chunks the user-specified range.</summary>
        /// <param name="fromInclusive">The lower, inclusive bound of the range.</param>
        /// <param name="toExclusive">The upper, exclusive bound of the range.</param>
        /// <param name="rangeSize">The size of each subrange.</param>
        /// <returns>A partitioner.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException"> The <paramref name="toExclusive"/> argument is 
        /// less than or equal to the <paramref name="fromInclusive"/> argument.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"> The <paramref name="rangeSize"/> argument is 
        /// less than or equal to 0.</exception>
        public static OrderablePartitioner<Tuple<int, int>> Create(int fromInclusive, int toExclusive, int rangeSize)
        {
            if (toExclusive <= fromInclusive) throw new ArgumentOutOfRangeException(nameof(toExclusive));
            if (rangeSize <= 0) throw new ArgumentOutOfRangeException(nameof(rangeSize));
            return Partitioner.Create(CreateRanges(fromInclusive, toExclusive, rangeSize), EnumerablePartitionerOptions.NoBuffering); // chunk one range at a time
        }

        // Private method to parcel out range tuples.
        private static IEnumerable<Tuple<int, int>> CreateRanges(int fromInclusive, int toExclusive, int rangeSize)
        {
            // Enumerate all of the ranges
            int from, to;
            bool shouldQuit = false;

            for (int i = fromInclusive; (i < toExclusive) && !shouldQuit; i = unchecked(i + rangeSize))
            {
                from = i;
                try { checked { to = i + rangeSize; } }
                catch (OverflowException)
                {
                    to = toExclusive;
                    shouldQuit = true;
                }
                if (to > toExclusive) to = toExclusive;
                yield return new Tuple<int, int>(from, to);
            }
        }

        #region DynamicPartitionEnumerator_Abstract class
        /// <summary>
        /// DynamicPartitionEnumerator_Abstract defines the enumerator for each partition for the dynamic load-balance
        /// partitioning algorithm. 
        /// - Partition is an enumerator of KeyValuePairs, each corresponding to an item in the data source: 
        ///   the key is the index in the source collection; the value is the item itself.
        /// - a set of such partitions share a reader over data source. The type of the reader is specified by
        ///   TSourceReader. 
        /// - each partition requests a contiguous chunk of elements at a time from the source data. The chunk 
        ///   size is initially 1, and doubles every time until it reaches the maximum chunk size. 
        ///   The implementation for GrabNextChunk() method has two versions: one for data source of IndexRange 
        ///   types (IList and the array), one for data source of IEnumerable.
        /// - The method "Reset" is not supported for any partitioning algorithm.
        /// - The implementation for MoveNext() method is same for all dynamic partitioners, so we provide it
        ///   in this abstract class.
        /// </summary>
        /// <typeparam name="TSource">Type of the elements in the data source</typeparam>
        /// <typeparam name="TSourceReader">Type of the reader on the data source</typeparam>
        //TSourceReader is 
        //  - IList<TSource>, when source data is IList<TSource>, the shared reader is source data itself
        //  - TSource[], when source data is TSource[], the shared reader is source data itself
        //  - IEnumerator<TSource>, when source data is IEnumerable<TSource>, and the shared reader is an 
        //    enumerator of the source data
        private abstract class DynamicPartitionEnumerator_Abstract<TSource, TSourceReader> : IEnumerator<KeyValuePair<long, TSource>>
        {
            //----------------- common fields and constructor for all dynamic partitioners -----------------
            //--- shared by all derived class with source data type: IList, Array, and IEnumerator
            protected readonly TSourceReader _sharedReader;

            protected static int s_defaultMaxChunkSize = GetDefaultChunkSize<TSource>();

            //deferred allocating in MoveNext() with initial value 0, to avoid false sharing
            //we also use the fact that: (_currentChunkSize==null) means MoveNext is never called on this enumerator 
            protected SharedInt? _currentChunkSize;

            //deferring allocation in MoveNext() with initial value -1, to avoid false sharing
            protected SharedInt? _localOffset;

            private const int CHUNK_DOUBLING_RATE = 3; // Double the chunk size every this many grabs
            private int _doublingCountdown; // Number of grabs remaining until chunk size doubles
            protected readonly int _maxChunkSize; // s_defaultMaxChunkSize unless single-chunking is requested by the caller

            // _sharedIndex shared by this set of partitions, and particularly when _sharedReader is IEnumerable
            // it serves as tracking of the natural order of elements in _sharedReader
            // the value of this field is passed in from outside (already initialized) by the constructor, 
            protected readonly SharedLong _sharedIndex;

            protected DynamicPartitionEnumerator_Abstract(TSourceReader sharedReader, SharedLong sharedIndex)
                : this(sharedReader, sharedIndex, false)
            {
            }

            protected DynamicPartitionEnumerator_Abstract(TSourceReader sharedReader, SharedLong sharedIndex, bool useSingleChunking)
            {
                _sharedReader = sharedReader;
                _sharedIndex = sharedIndex;
                _maxChunkSize = useSingleChunking ? 1 : s_defaultMaxChunkSize;
            }

            // ---------------- abstract method declarations --------------

            /// <summary>
            /// Abstract method to request a contiguous chunk of elements from the source collection
            /// </summary>
            /// <param name="requestedChunkSize">specified number of elements requested</param>
            /// <returns>
            /// true if we successfully reserved at least one element (up to #=requestedChunkSize) 
            /// false if all elements in the source collection have been reserved.
            /// </returns>
            //GrabNextChunk does the following: 
            //  - grab # of requestedChunkSize elements from source data through shared reader, 
            //  - at the time of function returns, _currentChunkSize is updated with the number of 
            //    elements actually got assigned (<=requestedChunkSize). 
            //  - GrabNextChunk returns true if at least one element is assigned to this partition; 
            //    false if the shared reader already hits the last element of the source data before 
            //    we call GrabNextChunk
            protected abstract bool GrabNextChunk(int requestedChunkSize);

            /// <summary>
            /// Abstract property, returns whether or not the shared reader has already read the last 
            /// element of the source data 
            /// </summary>
            protected abstract bool HasNoElementsLeft { get; }

            /// <summary>
            /// Get the current element in the current partition. Property required by IEnumerator interface
            /// This property is abstract because the implementation is different depending on the type
            /// of the source data: IList, Array or IEnumerable
            /// </summary>
            public abstract KeyValuePair<long, TSource> Current { get; }

            /// <summary>
            /// Dispose is abstract, and depends on the type of the source data:
            /// - For source data type IList and Array, the type of the shared reader is just the data itself.
            ///   We don't do anything in Dispose method for IList and Array. 
            /// - For source data type IEnumerable, the type of the shared reader is an enumerator we created.
            ///   Thus we need to dispose this shared reader enumerator, when there is no more active partitions
            ///   left.
            /// </summary>
            public abstract void Dispose();

            /// <summary>
            /// Reset on partitions is not supported
            /// </summary>
            public void Reset()
            {
                throw new NotSupportedException();
            }


            /// <summary>
            /// Get the current element in the current partition. Property required by IEnumerator interface
            /// </summary>
            object? IEnumerator.Current
            {
                get
                {
                    return ((DynamicPartitionEnumerator_Abstract<TSource, TSourceReader>)this).Current;
                }
            }

            /// <summary>
            /// Moves to the next element if any.
            /// Try current chunk first, if the current chunk do not have any elements left, then we 
            /// attempt to grab a chunk from the source collection.
            /// </summary>
            /// <returns>
            /// true if successfully moving to the next position;
            /// false otherwise, if and only if there is no more elements left in the current chunk 
            /// AND the source collection is exhausted. 
            /// </returns>
            public bool MoveNext()
            {
                //perform deferred allocating of the local variables. 
                if (_localOffset == null)
                {
                    Debug.Assert(_currentChunkSize == null);
                    _localOffset = new SharedInt(-1);
                    _currentChunkSize = new SharedInt(0);
                    _doublingCountdown = CHUNK_DOUBLING_RATE;
                }
                Debug.Assert(_currentChunkSize != null);

                if (_localOffset.Value < _currentChunkSize.Value - 1)
                //attempt to grab the next element from the local chunk
                {
                    _localOffset.Value++;
                    return true;
                }
                else
                //otherwise it means we exhausted the local chunk
                //grab a new chunk from the source enumerator
                {
                    // The second part of the || condition is necessary to handle the case when MoveNext() is called
                    // after a previous MoveNext call returned false.
                    Debug.Assert(_localOffset.Value == _currentChunkSize.Value - 1 || _currentChunkSize.Value == 0);

                    //set the requested chunk size to a proper value
                    int requestedChunkSize;
                    if (_currentChunkSize.Value == 0) //first time grabbing from source enumerator
                    {
                        requestedChunkSize = 1;
                    }
                    else if (_doublingCountdown > 0)
                    {
                        requestedChunkSize = _currentChunkSize.Value;
                    }
                    else
                    {
                        requestedChunkSize = Math.Min(_currentChunkSize.Value * 2, _maxChunkSize);
                        _doublingCountdown = CHUNK_DOUBLING_RATE; // reset
                    }

                    // Decrement your doubling countdown
                    _doublingCountdown--;

                    Debug.Assert(requestedChunkSize > 0 && requestedChunkSize <= _maxChunkSize);
                    //GrabNextChunk will update the value of _currentChunkSize
                    if (GrabNextChunk(requestedChunkSize))
                    {
                        Debug.Assert(_currentChunkSize.Value <= requestedChunkSize && _currentChunkSize.Value > 0);
                        _localOffset.Value = 0;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        #endregion

        #region Dynamic Partitioner for source data of IEnuemrable<> type
        /// <summary>
        /// Inherits from DynamicPartitioners
        /// Provides customized implementation of GetOrderableDynamicPartitions_Factory method, to return an instance
        /// of EnumerableOfPartitionsForIEnumerator defined internally
        /// </summary>
        /// <typeparam name="TSource">Type of elements in the source data</typeparam>
        private class DynamicPartitionerForIEnumerable<TSource> : OrderablePartitioner<TSource>
        {
            IEnumerable<TSource> _source;
            readonly bool _useSingleChunking;

            //constructor
            internal DynamicPartitionerForIEnumerable(IEnumerable<TSource> source, EnumerablePartitionerOptions partitionerOptions)
                : base(true, false, true)
            {
                _source = source;
                _useSingleChunking = ((partitionerOptions & EnumerablePartitionerOptions.NoBuffering) != 0);
            }

            /// <summary>
            /// Overrides OrderablePartitioner.GetOrderablePartitions.
            /// Partitions the underlying collection into the given number of orderable partitions.
            /// </summary>
            /// <param name="partitionCount">number of partitions requested</param>
            /// <returns>A list containing <paramref name="partitionCount"/> enumerators.</returns>
            override public IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount)
            {
                if (partitionCount <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(partitionCount));
                }
                IEnumerator<KeyValuePair<long, TSource>>[] partitions
                    = new IEnumerator<KeyValuePair<long, TSource>>[partitionCount];

                IEnumerable<KeyValuePair<long, TSource>> partitionEnumerable = new InternalPartitionEnumerable(_source.GetEnumerator(), _useSingleChunking, true);
                for (int i = 0; i < partitionCount; i++)
                {
                    partitions[i] = partitionEnumerable.GetEnumerator();
                }
                return partitions;
            }

            /// <summary>
            /// Overrides OrderablePartitioner.GetOrderableDynamicPartitions
            /// </summary>
            /// <returns>a enumerable collection of orderable partitions</returns>
            override public IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions()
            {
                return new InternalPartitionEnumerable(_source.GetEnumerator(), _useSingleChunking, false);
            }

            /// <summary>
            /// Whether additional partitions can be created dynamically.
            /// </summary>
            override public bool SupportsDynamicPartitions
            {
                get { return true; }
            }

            #region Internal classes:  InternalPartitionEnumerable, InternalPartitionEnumerator
            /// <summary>
            /// Provides customized implementation for source data of IEnumerable
            /// Different from the counterpart for IList/Array, this enumerable maintains several additional fields
            /// shared by the partitions it owns, including a boolean "_hasNoElementsLef", a shared lock, and a 
            /// shared count "_activePartitionCount" used to track active partitions when they were created statically
            /// </summary>
            private class InternalPartitionEnumerable : IEnumerable<KeyValuePair<long, TSource>>, IDisposable
            {
                //reader through which we access the source data
                private readonly IEnumerator<TSource> _sharedReader;
                private SharedLong _sharedIndex;//initial value -1

                private volatile KeyValuePair<long, TSource>[]? _fillBuffer;  // intermediate buffer to reduce locking
                private volatile int _fillBufferSize;               // actual number of elements in _FillBuffer. Will start
                                                                     // at _FillBuffer.Length, and might be reduced during the last refill
                private volatile int _fillBufferCurrentPosition;    //shared value to be accessed by Interlock.Increment only
                private volatile int _activeCopiers;               //number of active copiers

                //fields shared by all partitions that this Enumerable owns, their allocation is deferred
                private SharedBool _hasNoElementsLeft; // no elements left at all.
                private SharedBool _sourceDepleted;    // no elements left in the enumerator, but there may be elements in the Fill Buffer

                //shared synchronization lock, created by this Enumerable
                private object _sharedLock;//deferring allocation by enumerator

                private bool _disposed;

                // If dynamic partitioning, then _activePartitionCount == null
                // If static partitioning, then it keeps track of active partition count
                private SharedInt? _activePartitionCount;

                // records whether or not the user has requested single-chunking behavior
                private readonly bool _useSingleChunking;

                internal InternalPartitionEnumerable(IEnumerator<TSource> sharedReader, bool useSingleChunking, bool isStaticPartitioning)
                {
                    _sharedReader = sharedReader;
                    _sharedIndex = new SharedLong(-1);
                    _hasNoElementsLeft = new SharedBool(false);
                    _sourceDepleted = new SharedBool(false);
                    _sharedLock = new object();
                    _useSingleChunking = useSingleChunking;

                    // Only allocate the fill-buffer if single-chunking is not in effect
                    if (!_useSingleChunking)
                    {
                        // Time to allocate the fill buffer which is used to reduce the contention on the shared lock.
                        // First pick the buffer size multiplier. We use 4 for when there are more than 4 cores, and just 1 for below. This is based on empirical evidence.
                        int fillBufferMultiplier = (PlatformHelper.ProcessorCount > 4) ? 4 : 1;

                        // and allocate the fill buffer using these two numbers
                        _fillBuffer = new KeyValuePair<long, TSource>[fillBufferMultiplier * Partitioner.GetDefaultChunkSize<TSource>()];
                    }

                    if (isStaticPartitioning)
                    {
                        // If this object is created for static partitioning (ie. via GetPartitions(int partitionCount), 
                        // GetOrderablePartitions(int partitionCount)), we track the active partitions, in order to dispose 
                        // this object when all the partitions have been disposed.
                        _activePartitionCount = new SharedInt(0);
                    }
                    else
                    {
                        // Otherwise this object is created for dynamic partitioning (ie, via GetDynamicPartitions(),
                        // GetOrderableDynamicPartitions()), we do not need tracking. This object must be disposed
                        // explicitly
                        _activePartitionCount = null;
                    }
                }

                public IEnumerator<KeyValuePair<long, TSource>> GetEnumerator()
                {
                    if (_disposed)
                    {
                        throw new ObjectDisposedException(SR.PartitionerStatic_CanNotCallGetEnumeratorAfterSourceHasBeenDisposed);
                    }
                    else
                    {
                        return new InternalPartitionEnumerator(_sharedReader, _sharedIndex,
                            _hasNoElementsLeft, _activePartitionCount, this, _useSingleChunking);
                    }
                }


                IEnumerator IEnumerable.GetEnumerator()
                {
                    return ((InternalPartitionEnumerable)this).GetEnumerator();
                }

                ///////////////////
                //
                // Used by GrabChunk_Buffered()
                private void TryCopyFromFillBuffer(KeyValuePair<long, TSource>[] destArray,
                                                  int requestedChunkSize,
                                                  ref int actualNumElementsGrabbed)
                {
                    actualNumElementsGrabbed = 0;


                    // making a local defensive copy of the fill buffer reference, just in case it gets nulled out
                    KeyValuePair<long, TSource>[]? fillBufferLocalRef = _fillBuffer;
                    if (fillBufferLocalRef == null) return;

                    // first do a quick check, and give up if the current position is at the end
                    // so that we don't do an unnecessary pair of Interlocked.Increment / Decrement calls
                    if (_fillBufferCurrentPosition >= _fillBufferSize)
                    {
                        return; // no elements in the buffer to copy from
                    }

                    // We might have a chance to grab elements from the buffer. We will know for sure 
                    // when we do the Interlocked.Add below. 
                    // But first we must register as a potential copier in order to make sure 
                    // the elements we're copying from don't get overwritten by another thread 
                    // that starts refilling the buffer right after our Interlocked.Add.
                    Interlocked.Increment(ref _activeCopiers);

                    int endPos = Interlocked.Add(ref _fillBufferCurrentPosition, requestedChunkSize);
                    int beginPos = endPos - requestedChunkSize;

                    if (beginPos < _fillBufferSize)
                    {
                        // adjust index and do the actual copy
                        actualNumElementsGrabbed = (endPos < _fillBufferSize) ? endPos : _fillBufferSize - beginPos;
                        Array.Copy(fillBufferLocalRef, beginPos, destArray, 0, actualNumElementsGrabbed);
                    }

                    // let the record show we are no longer accessing the buffer
                    Interlocked.Decrement(ref _activeCopiers);
                }

                /// <summary>
                /// This is the common entry point for consuming items from the source enumerable
                /// </summary>
                /// <returns>
                /// true if we successfully reserved at least one element 
                /// false if all elements in the source collection have been reserved.
                /// </returns>
                internal bool GrabChunk(KeyValuePair<long, TSource>[] destArray, int requestedChunkSize, ref int actualNumElementsGrabbed)
                {
                    actualNumElementsGrabbed = 0;

                    if (_hasNoElementsLeft.Value)
                    {
                        return false;
                    }

                    if (_useSingleChunking)
                    {
                        return GrabChunk_Single(destArray, requestedChunkSize, ref actualNumElementsGrabbed);
                    }
                    else
                    {
                        return GrabChunk_Buffered(destArray, requestedChunkSize, ref actualNumElementsGrabbed);
                    }
                }

                /// <summary>
                /// Version of GrabChunk that grabs a single element at a time from the source enumerable
                /// </summary>
                /// <returns>
                /// true if we successfully reserved an element 
                /// false if all elements in the source collection have been reserved.
                /// </returns>
                internal bool GrabChunk_Single(KeyValuePair<long, TSource>[] destArray, int requestedChunkSize, ref int actualNumElementsGrabbed)
                {
                    Debug.Assert(_useSingleChunking, "Expected _useSingleChecking to be true");
                    Debug.Assert(requestedChunkSize == 1, "Got requested chunk size of " + requestedChunkSize + " when single-chunking was on");
                    Debug.Assert(actualNumElementsGrabbed == 0, "Expected actualNumElementsGrabbed == 0, instead it is " + actualNumElementsGrabbed);
                    Debug.Assert(destArray.Length == 1, "Expected destArray to be of length 1, instead its length is " + destArray.Length);

                    lock (_sharedLock)
                    {
                        if (_hasNoElementsLeft.Value) return false;

                        try
                        {
                            if (_sharedReader.MoveNext())
                            {
                                _sharedIndex.Value = checked(_sharedIndex.Value + 1);
                                destArray[0]
                                    = new KeyValuePair<long, TSource>(_sharedIndex.Value,
                                                                        _sharedReader.Current);
                                actualNumElementsGrabbed = 1;
                                return true;
                            }
                            else
                            {
                                //if MoveNext() return false, we set the flag to inform other partitions
                                _sourceDepleted.Value = true;
                                _hasNoElementsLeft.Value = true;
                                return false;
                            }
                        }
                        catch
                        {
                            // On an exception, make sure that no additional items are hereafter enumerated
                            _sourceDepleted.Value = true;
                            _hasNoElementsLeft.Value = true;
                            throw;
                        }
                    }
                }



                /// <summary>
                /// Version of GrabChunk that uses buffering scheme to grab items out of source enumerable
                /// </summary>
                /// <returns>
                /// true if we successfully reserved at least one element (up to #=requestedChunkSize) 
                /// false if all elements in the source collection have been reserved.
                /// </returns>
                internal bool GrabChunk_Buffered(KeyValuePair<long, TSource>[] destArray, int requestedChunkSize, ref int actualNumElementsGrabbed)
                {
                    Debug.Assert(requestedChunkSize > 0);
                    Debug.Assert(!_useSingleChunking, "Did not expect to be in single-chunking mode");

                    TryCopyFromFillBuffer(destArray, requestedChunkSize, ref actualNumElementsGrabbed);

                    if (actualNumElementsGrabbed == requestedChunkSize)
                    {
                        // that was easy.
                        return true;
                    }
                    else if (_sourceDepleted.Value)
                    {
                        // looks like we both reached the end of the fill buffer, and the source was depleted previously
                        // this means no more work to do for any other worker
                        _hasNoElementsLeft.Value = true;
                        _fillBuffer = null;
                        return (actualNumElementsGrabbed > 0);
                    }


                    //
                    //  now's the time to take the shared lock and enumerate
                    //
                    lock (_sharedLock)
                    {
                        if (_sourceDepleted.Value)
                        {
                            return (actualNumElementsGrabbed > 0);
                        }

                        try
                        {
                            // we need to make sure all array copiers are finished
                            if (_activeCopiers > 0)
                            {
                                SpinWait sw = new SpinWait();
                                while (_activeCopiers > 0) sw.SpinOnce();
                            }

                            Debug.Assert(_sharedIndex != null); //already been allocated in MoveNext() before calling GrabNextChunk

                            // Now's the time to actually enumerate the source

                            // We first fill up the requested # of elements in the caller's array
                            // continue from the where TryCopyFromFillBuffer() left off
                            for (; actualNumElementsGrabbed < requestedChunkSize; actualNumElementsGrabbed++)
                            {
                                if (_sharedReader.MoveNext())
                                {
                                    _sharedIndex.Value = checked(_sharedIndex.Value + 1);
                                    destArray[actualNumElementsGrabbed]
                                        = new KeyValuePair<long, TSource>(_sharedIndex.Value,
                                                                          _sharedReader.Current);
                                }
                                else
                                {
                                    //if MoveNext() return false, we set the flag to inform other partitions
                                    _sourceDepleted.Value = true;
                                    break;
                                }
                            }

                            // taking a local snapshot of _FillBuffer in case some other thread decides to null out _FillBuffer 
                            // in the entry of this method after observing _sourceCompleted = true
                            var localFillBufferRef = _fillBuffer;

                            // If the big buffer seems to be depleted, we will also fill that up while we are under the lock
                            // Note that this is the only place that _FillBufferCurrentPosition can be reset
                            if (_sourceDepleted.Value == false && localFillBufferRef != null &&
                                _fillBufferCurrentPosition >= localFillBufferRef.Length)
                            {
                                for (int i = 0; i < localFillBufferRef.Length; i++)
                                {
                                    if (_sharedReader.MoveNext())
                                    {
                                        _sharedIndex.Value = checked(_sharedIndex.Value + 1);
                                        localFillBufferRef[i]
                                            = new KeyValuePair<long, TSource>(_sharedIndex.Value,
                                                                              _sharedReader.Current);
                                    }
                                    else
                                    {
                                        // No more elements left in the enumerator.
                                        // Record this, so that the next request can skip the lock
                                        _sourceDepleted.Value = true;

                                        // also record the current count in _FillBufferSize
                                        _fillBufferSize = i;

                                        // and exit the for loop so that we don't keep incrementing _FillBufferSize
                                        break;
                                    }
                                }

                                _fillBufferCurrentPosition = 0;
                            }
                        }
                        catch
                        {
                            // If an exception occurs, don't let the other enumerators try to enumerate.
                            // NOTE: this could instead throw an InvalidOperationException, but that would be unexpected 
                            // and not helpful to the end user.  We know the root cause is being communicated already.)
                            _sourceDepleted.Value = true;
                            _hasNoElementsLeft.Value = true;
                            throw;
                        }
                    }

                    return (actualNumElementsGrabbed > 0);
                }

                public void Dispose()
                {
                    if (!_disposed)
                    {
                        _disposed = true;
                        _sharedReader.Dispose();
                    }
                }
            }

            /// <summary>
            /// Inherits from DynamicPartitionEnumerator_Abstract directly
            /// Provides customized implementation for: GrabNextChunk, HasNoElementsLeft, Current, Dispose
            /// </summary>
            private class InternalPartitionEnumerator : DynamicPartitionEnumerator_Abstract<TSource, IEnumerator<TSource>>
            {
                //---- fields ----
                //cached local copy of the current chunk
                private KeyValuePair<long, TSource>[]? _localList; //defer allocating to avoid false sharing

                // the values of the following two fields are passed in from
                // outside(already initialized) by the constructor, 
                private readonly SharedBool _hasNoElementsLeft;
                private readonly SharedInt? _activePartitionCount;
                private InternalPartitionEnumerable _enumerable;

                //constructor
                internal InternalPartitionEnumerator(
                    IEnumerator<TSource> sharedReader,
                    SharedLong sharedIndex,
                    SharedBool hasNoElementsLeft,
                    SharedInt? activePartitionCount,
                    InternalPartitionEnumerable enumerable,
                    bool useSingleChunking)
                    : base(sharedReader, sharedIndex, useSingleChunking)
                {
                    _hasNoElementsLeft = hasNoElementsLeft;
                    _enumerable = enumerable;
                    _activePartitionCount = activePartitionCount;

                    if (_activePartitionCount != null)
                    {
                        // If static partitioning, we need to increase the active partition count.
                        Interlocked.Increment(ref _activePartitionCount.Value);
                    }
                }

                //overriding methods

                /// <summary>
                /// Reserves a contiguous range of elements from source data
                /// </summary>
                /// <param name="requestedChunkSize">specified number of elements requested</param>
                /// <returns>
                /// true if we successfully reserved at least one element (up to #=requestedChunkSize) 
                /// false if all elements in the source collection have been reserved.
                /// </returns>
                override protected bool GrabNextChunk(int requestedChunkSize)
                {
                    Debug.Assert(requestedChunkSize > 0);

                    if (HasNoElementsLeft)
                    {
                        return false;
                    }

                    // defer allocation to avoid false sharing
                    if (_localList == null)
                    {
                        _localList = new KeyValuePair<long, TSource>[_maxChunkSize];
                    }

#pragma warning disable 0420 // TODO: https://github.com/dotnet/corefx/issues/35022
                    // make the actual call to the enumerable that grabs a chunk
                    return _enumerable.GrabChunk(_localList, requestedChunkSize, ref _currentChunkSize!.Value);
#pragma warning restore 0420
                }

                /// <summary>
                /// Returns whether or not the shared reader has already read the last 
                /// element of the source data 
                /// </summary>
                /// <remarks>
                /// We cannot call _sharedReader.MoveNext(), to see if it hits the last element
                /// or not, because we can't undo MoveNext(). Thus we need to maintain a shared 
                /// boolean value _hasNoElementsLeft across all partitions
                /// </remarks>
                override protected bool HasNoElementsLeft
                {
                    get { return _hasNoElementsLeft.Value; }
                }

                override public KeyValuePair<long, TSource> Current
                {
                    get
                    {
                        //verify that MoveNext is at least called once before Current is called 
                        if (_currentChunkSize == null)
                        {
                            throw new InvalidOperationException(SR.PartitionerStatic_CurrentCalledBeforeMoveNext);
                        }
                        Debug.Assert(_localList != null);
                        Debug.Assert(_localOffset!.Value >= 0 && _localOffset.Value < _currentChunkSize.Value);
                        return (_localList[_localOffset.Value]);
                    }
                }

                override public void Dispose()
                {
                    // If this is static partitioning, i.e. _activePartitionCount != null, since the current partition 
                    // is disposed, we decrement the number of active partitions for the shared reader. 
                    if (_activePartitionCount != null && Interlocked.Decrement(ref _activePartitionCount.Value) == 0)
                    {
                        // If the number of active partitions becomes 0, we need to dispose the shared 
                        // reader we created in the _enumerable object.
                        _enumerable.Dispose();
                    }
                    // If this is dynamic partitioning, i.e. _activePartitionCount != null, then _enumerable needs to
                    // be disposed explicitly by the user, and we do not need to anything here
                }
            }
            #endregion
        }
        #endregion

        #region Dynamic Partitioner for source data of IndexRange types (IList<> and Array<>)
        /// <summary>
        /// Dynamic load-balance partitioner. This class is abstract and to be derived from by 
        /// the customized partitioner classes for IList, Array, and IEnumerable
        /// </summary>
        /// <typeparam name="TSource">Type of the elements in the source data</typeparam>
        /// <typeparam name="TCollection"> Type of the source data collection</typeparam>
        private abstract class DynamicPartitionerForIndexRange_Abstract<TSource, TCollection> : OrderablePartitioner<TSource>
        {
            // TCollection can be: IList<TSource>, TSource[] and IEnumerable<TSource>
            // Derived classes specify TCollection, and implement the abstract method GetOrderableDynamicPartitions_Factory accordingly
            TCollection _data;

            /// <summary>
            /// Constructs a new orderable partitioner 
            /// </summary>
            /// <param name="data">source data collection</param>
            protected DynamicPartitionerForIndexRange_Abstract(TCollection data)
                : base(true, false, true)
            {
                _data = data;
            }

            /// <summary>
            /// Partition the source data and create an enumerable over the resulting partitions. 
            /// </summary>
            /// <param name="data">the source data collection</param>
            /// <returns>an enumerable of partitions of </returns>
            protected abstract IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions_Factory(TCollection data);

            /// <summary>
            /// Overrides OrderablePartitioner.GetOrderablePartitions.
            /// Partitions the underlying collection into the given number of orderable partitions.
            /// </summary>
            /// <param name="partitionCount">number of partitions requested</param>
            /// <returns>A list containing <paramref name="partitionCount"/> enumerators.</returns>
            override public IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount)
            {
                if (partitionCount <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(partitionCount));
                }
                IEnumerator<KeyValuePair<long, TSource>>[] partitions
                    = new IEnumerator<KeyValuePair<long, TSource>>[partitionCount];
                IEnumerable<KeyValuePair<long, TSource>> partitionEnumerable = GetOrderableDynamicPartitions_Factory(_data);
                for (int i = 0; i < partitionCount; i++)
                {
                    partitions[i] = partitionEnumerable.GetEnumerator();
                }
                return partitions;
            }

            /// <summary>
            /// Overrides OrderablePartitioner.GetOrderableDynamicPartitions
            /// </summary>
            /// <returns>a enumerable collection of orderable partitions</returns>
            override public IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions()
            {
                return GetOrderableDynamicPartitions_Factory(_data);
            }

            /// <summary>
            /// Whether additional partitions can be created dynamically.
            /// </summary>
            override public bool SupportsDynamicPartitions
            {
                get { return true; }
            }
        }

        /// <summary>
        /// Defines dynamic partition for source data of IList and Array. 
        /// This class inherits DynamicPartitionEnumerator_Abstract
        ///   - implements GrabNextChunk, HasNoElementsLeft, and Dispose methods for IList and Array
        ///   - Current property still remains abstract, implementation is different for IList and Array
        ///   - introduces another abstract method SourceCount, which returns the number of elements in
        ///     the source data. Implementation differs for IList and Array
        /// </summary>
        /// <typeparam name="TSource">Type of the elements in the data source</typeparam>
        /// <typeparam name="TSourceReader">Type of the reader on the source data</typeparam>
        private abstract class DynamicPartitionEnumeratorForIndexRange_Abstract<TSource, TSourceReader> : DynamicPartitionEnumerator_Abstract<TSource, TSourceReader>
        {
            //fields
            protected int _startIndex; //initially zero

            //constructor
            protected DynamicPartitionEnumeratorForIndexRange_Abstract(TSourceReader sharedReader, SharedLong sharedIndex)
                : base(sharedReader, sharedIndex)
            {
            }

            //abstract methods
            //the Current property is still abstract, and will be implemented by derived classes
            //we add another abstract method SourceCount to get the number of elements from the source reader

            /// <summary>
            /// Get the number of elements from the source reader.
            /// It calls IList.Count or Array.Length
            /// </summary>
            protected abstract int SourceCount { get; }

            //overriding methods

            /// <summary>
            /// Reserves a contiguous range of elements from source data
            /// </summary>
            /// <param name="requestedChunkSize">specified number of elements requested</param>
            /// <returns>
            /// true if we successfully reserved at least one element (up to #=requestedChunkSize) 
            /// false if all elements in the source collection have been reserved.
            /// </returns>
            override protected bool GrabNextChunk(int requestedChunkSize)
            {
                Debug.Assert(requestedChunkSize > 0);

                while (!HasNoElementsLeft)
                {
                    Debug.Assert(_sharedIndex != null);
                    // use the new Volatile.Read method because it is cheaper than Interlocked.Read on AMD64 architecture
                    long oldSharedIndex = Volatile.Read(ref _sharedIndex.Value);

                    if (HasNoElementsLeft)
                    {
                        //HasNoElementsLeft situation changed from false to true immediately
                        //and oldSharedIndex becomes stale
                        return false;
                    }

                    //there won't be overflow, because the index of IList/array is int, and we 
                    //have casted it to long. 
                    long newSharedIndex = Math.Min(SourceCount - 1, oldSharedIndex + requestedChunkSize);


                    //the following CAS, if successful, reserves a chunk of elements [oldSharedIndex+1, newSharedIndex] 
                    //inclusive in the source collection
                    if (Interlocked.CompareExchange(ref _sharedIndex.Value, newSharedIndex, oldSharedIndex)
                        == oldSharedIndex)
                    {
                        //set up local indexes.
                        //_currentChunkSize is always set to requestedChunkSize when source data had 
                        //enough elements of what we requested
                        _currentChunkSize!.Value = (int)(newSharedIndex - oldSharedIndex);
                        _localOffset!.Value = -1;
                        _startIndex = (int)(oldSharedIndex + 1);
                        return true;
                    }
                }
                //didn't get any element, return false;
                return false;
            }

            /// <summary>
            /// Returns whether or not the shared reader has already read the last 
            /// element of the source data 
            /// </summary>
            override protected bool HasNoElementsLeft
            {
                get
                {
                    Debug.Assert(_sharedIndex != null);
                    // use the new Volatile.Read method because it is cheaper than Interlocked.Read on AMD64 architecture
                    return Volatile.Read(ref _sharedIndex.Value) >= SourceCount - 1;
                }
            }

            /// <summary>
            /// For source data type IList and Array, the type of the shared reader is just the data itself.
            /// We don't do anything in Dispose method for IList and Array. 
            /// </summary>
            override public void Dispose()
            { }
        }


        /// <summary>
        /// Inherits from DynamicPartitioners
        /// Provides customized implementation of GetOrderableDynamicPartitions_Factory method, to return an instance
        /// of EnumerableOfPartitionsForIList defined internally
        /// </summary>
        /// <typeparam name="TSource">Type of elements in the source data</typeparam>
        private class DynamicPartitionerForIList<TSource> : DynamicPartitionerForIndexRange_Abstract<TSource, IList<TSource>>
        {
            //constructor
            internal DynamicPartitionerForIList(IList<TSource> source)
                : base(source)
            { }

            //override methods
            override protected IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions_Factory(IList<TSource> _data)
            {
                //_data itself serves as shared reader
                return new InternalPartitionEnumerable(_data);
            }

            /// <summary>
            /// Inherits from PartitionList_Abstract 
            /// Provides customized implementation for source data of IList
            /// </summary>
            private class InternalPartitionEnumerable : IEnumerable<KeyValuePair<long, TSource>>
            {
                //reader through which we access the source data
                private readonly IList<TSource> _sharedReader;
                private SharedLong _sharedIndex;

                internal InternalPartitionEnumerable(IList<TSource> sharedReader)
                {
                    _sharedReader = sharedReader;
                    _sharedIndex = new SharedLong(-1);
                }

                public IEnumerator<KeyValuePair<long, TSource>> GetEnumerator()
                {
                    return new InternalPartitionEnumerator(_sharedReader, _sharedIndex);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return ((InternalPartitionEnumerable)this).GetEnumerator();
                }
            }

            /// <summary>
            /// Inherits from DynamicPartitionEnumeratorForIndexRange_Abstract
            /// Provides customized implementation of SourceCount property and Current property for IList
            /// </summary>
            private class InternalPartitionEnumerator : DynamicPartitionEnumeratorForIndexRange_Abstract<TSource, IList<TSource>>
            {
                //constructor
                internal InternalPartitionEnumerator(IList<TSource> sharedReader, SharedLong sharedIndex)
                    : base(sharedReader, sharedIndex)
                { }

                //overriding methods
                override protected int SourceCount
                {
                    get { return _sharedReader.Count; }
                }
                /// <summary>
                /// return a KeyValuePair of the current element and its key 
                /// </summary>
                override public KeyValuePair<long, TSource> Current
                {
                    get
                    {
                        //verify that MoveNext is at least called once before Current is called 
                        if (_currentChunkSize == null)
                        {
                            throw new InvalidOperationException(SR.PartitionerStatic_CurrentCalledBeforeMoveNext);
                        }

                        Debug.Assert(_localOffset!.Value >= 0 && _localOffset.Value < _currentChunkSize.Value);
                        return new KeyValuePair<long, TSource>(_startIndex + _localOffset.Value,
                            _sharedReader[_startIndex + _localOffset.Value]);
                    }
                }
            }
        }



        /// <summary>
        /// Inherits from DynamicPartitioners
        /// Provides customized implementation of GetOrderableDynamicPartitions_Factory method, to return an instance
        /// of EnumerableOfPartitionsForArray defined internally
        /// </summary>
        /// <typeparam name="TSource">Type of elements in the source data</typeparam>
        private class DynamicPartitionerForArray<TSource> : DynamicPartitionerForIndexRange_Abstract<TSource, TSource[]>
        {
            //constructor
            internal DynamicPartitionerForArray(TSource[] source)
                : base(source)
            { }

            //override methods
            override protected IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions_Factory(TSource[] _data)
            {
                return new InternalPartitionEnumerable(_data);
            }

            /// <summary>
            /// Inherits from PartitionList_Abstract 
            /// Provides customized implementation for source data of Array
            /// </summary>
            private class InternalPartitionEnumerable : IEnumerable<KeyValuePair<long, TSource>>
            {
                //reader through which we access the source data
                private readonly TSource[] _sharedReader;
                private SharedLong _sharedIndex;

                internal InternalPartitionEnumerable(TSource[] sharedReader)
                {
                    _sharedReader = sharedReader;
                    _sharedIndex = new SharedLong(-1);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return ((InternalPartitionEnumerable)this).GetEnumerator();
                }


                public IEnumerator<KeyValuePair<long, TSource>> GetEnumerator()
                {
                    return new InternalPartitionEnumerator(_sharedReader, _sharedIndex);
                }
            }

            /// <summary>
            /// Inherits from DynamicPartitionEnumeratorForIndexRange_Abstract
            /// Provides customized implementation of SourceCount property and Current property for Array
            /// </summary>
            private class InternalPartitionEnumerator : DynamicPartitionEnumeratorForIndexRange_Abstract<TSource, TSource[]>
            {
                //constructor
                internal InternalPartitionEnumerator(TSource[] sharedReader, SharedLong sharedIndex)
                    : base(sharedReader, sharedIndex)
                { }

                //overriding methods
                override protected int SourceCount
                {
                    get { return _sharedReader.Length; }
                }

                override public KeyValuePair<long, TSource> Current
                {
                    get
                    {
                        //verify that MoveNext is at least called once before Current is called 
                        if (_currentChunkSize == null)
                        {
                            throw new InvalidOperationException(SR.PartitionerStatic_CurrentCalledBeforeMoveNext);
                        }

                        Debug.Assert(_localOffset!.Value >= 0 && _localOffset.Value < _currentChunkSize.Value);
                        return new KeyValuePair<long, TSource>(_startIndex + _localOffset.Value,
                            _sharedReader[_startIndex + _localOffset.Value]);
                    }
                }
            }
        }
        #endregion


        #region Static partitioning for IList and Array, abstract classes
        /// <summary>
        /// Static partitioning over IList. 
        /// - dynamic and load-balance
        /// - Keys are ordered within each partition
        /// - Keys are ordered across partitions
        /// - Keys are normalized
        /// - Number of partitions is fixed once specified, and the elements of the source data are 
        /// distributed to each partition as evenly as possible. 
        /// </summary>
        /// <typeparam name="TSource">type of the elements</typeparam>        
        /// <typeparam name="TCollection">Type of the source data collection</typeparam>
        private abstract class StaticIndexRangePartitioner<TSource, TCollection> : OrderablePartitioner<TSource>
        {
            protected StaticIndexRangePartitioner()
                : base(true, true, true)
            { }

            /// <summary>
            /// Abstract method to return the number of elements in the source data
            /// </summary>
            protected abstract int SourceCount { get; }

            /// <summary>
            /// Abstract method to create a partition that covers a range over source data, 
            /// starting from "startIndex", ending at "endIndex"
            /// </summary>
            /// <param name="startIndex">start index of the current partition on the source data</param>
            /// <param name="endIndex">end index of the current partition on the source data</param>
            /// <returns>a partition enumerator over the specified range</returns>
            // The partitioning algorithm is implemented in GetOrderablePartitions method
            // This method delegates according to source data type IList/Array
            protected abstract IEnumerator<KeyValuePair<long, TSource>> CreatePartition(int startIndex, int endIndex);

            /// <summary>
            /// Overrides OrderablePartitioner.GetOrderablePartitions
            /// Return a list of partitions, each of which enumerate a fixed part of the source data
            /// The elements of the source data are distributed to each partition as evenly as possible. 
            /// Specifically, if the total number of elements is N, and number of partitions is x, and N = a*x +b, 
            /// where a is the quotient, and b is the remainder. Then the first b partitions each has a + 1 elements,
            /// and the last x-b partitions each has a elements.
            /// For example, if N=10, x =3, then 
            ///    partition 0 ranges [0,3],
            ///    partition 1 ranges [4,6],
            ///    partition 2 ranges [7,9].
            /// This also takes care of the situation of (x&gt;N), the last x-N partitions are empty enumerators. 
            /// An empty enumerator is indicated by 
            ///      (_startIndex == list.Count &amp;&amp; _endIndex == list.Count -1)
            /// </summary>
            /// <param name="partitionCount">specified number of partitions</param>
            /// <returns>a list of partitions</returns>
            override public IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount)
            {
                if (partitionCount <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(partitionCount));
                }

                int quotient, remainder;
                quotient = SourceCount / partitionCount;
                remainder = SourceCount % partitionCount;

                IEnumerator<KeyValuePair<long, TSource>>[] partitions = new IEnumerator<KeyValuePair<long, TSource>>[partitionCount];
                int lastEndIndex = -1;
                for (int i = 0; i < partitionCount; i++)
                {
                    int startIndex = lastEndIndex + 1;

                    if (i < remainder)
                    {
                        lastEndIndex = startIndex + quotient;
                    }
                    else
                    {
                        lastEndIndex = startIndex + quotient - 1;
                    }
                    partitions[i] = CreatePartition(startIndex, lastEndIndex);
                }
                return partitions;
            }
        }

        /// <summary>
        /// Static Partition for IList/Array.
        /// This class implements all methods required by IEnumerator interface, except for the Current property.
        /// Current Property is different for IList and Array. Arrays calls 'ldelem' instructions for faster element 
        /// retrieval.
        /// </summary>
        //We assume the source collection is not being updated concurrently. Otherwise it will break the  
        //static partitioning, since each partition operates on the source collection directly, it does 
        //not have a local cache of the elements assigned to them.  
        private abstract class StaticIndexRangePartition<TSource> : IEnumerator<KeyValuePair<long, TSource>>
        {
            //the start and end position in the source collection for the current partition
            //the partition is empty if and only if 
            // (_startIndex == _data.Count && _endIndex == _data.Count-1)
            protected readonly int _startIndex;
            protected readonly int _endIndex;

            //the current index of the current partition while enumerating on the source collection
            protected volatile int _offset;

            /// <summary>
            /// Constructs an instance of StaticIndexRangePartition
            /// </summary>
            /// <param name="startIndex">the start index in the source collection for the current partition </param>
            /// <param name="endIndex">the end index in the source collection for the current partition</param>
            protected StaticIndexRangePartition(int startIndex, int endIndex)
            {
                _startIndex = startIndex;
                _endIndex = endIndex;
                _offset = startIndex - 1;
            }

            /// <summary>
            /// Current Property is different for IList and Array. Arrays calls 'ldelem' instructions for faster 
            /// element retrieval.
            /// </summary>
            public abstract KeyValuePair<long, TSource> Current { get; }

            /// <summary>
            /// We don't dispose the source for IList and array
            /// </summary>
            public void Dispose()
            { }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Moves to the next item
            /// Before the first MoveNext is called: _offset == _startIndex-1;
            /// </summary>
            /// <returns>true if successful, false if there is no item left</returns>
            public bool MoveNext()
            {
                if (_offset < _endIndex)
                {
                    _offset++;
                    return true;
                }
                else
                {
                    //After we have enumerated over all elements, we set _offset to _endIndex +1.
                    //The reason we do this is, for an empty enumerator, we need to tell the Current 
                    //property whether MoveNext has been called or not. 
                    //For an empty enumerator, it starts with (_offset == _startIndex-1 == _endIndex), 
                    //and we don't set a new value to _offset, then the above condition will always be 
                    //true, and the Current property will mistakenly assume MoveNext is never called.
                    _offset = _endIndex + 1;
                    return false;
                }
            }

            object? IEnumerator.Current
            {
                get
                {
                    return ((StaticIndexRangePartition<TSource>)this).Current;
                }
            }
        }
        #endregion

        #region Static partitioning for IList
        /// <summary>
        /// Inherits from StaticIndexRangePartitioner
        /// Provides customized implementation of SourceCount and CreatePartition
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        private class StaticIndexRangePartitionerForIList<TSource> : StaticIndexRangePartitioner<TSource, IList<TSource>>
        {
            IList<TSource> _list;
            internal StaticIndexRangePartitionerForIList(IList<TSource> list)
                : base()
            {
                Debug.Assert(list != null);
                _list = list;
            }
            override protected int SourceCount
            {
                get { return _list.Count; }
            }
            override protected IEnumerator<KeyValuePair<long, TSource>> CreatePartition(int startIndex, int endIndex)
            {
                return new StaticIndexRangePartitionForIList<TSource>(_list, startIndex, endIndex);
            }
        }

        /// <summary>
        /// Inherits from StaticIndexRangePartition
        /// Provides customized implementation of Current property
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        private class StaticIndexRangePartitionForIList<TSource> : StaticIndexRangePartition<TSource>
        {
            //the source collection shared by all partitions
            private volatile IList<TSource> _list;

            internal StaticIndexRangePartitionForIList(IList<TSource> list, int startIndex, int endIndex)
                : base(startIndex, endIndex)
            {
                Debug.Assert(startIndex >= 0 && endIndex <= list.Count - 1);
                _list = list;
            }

            override public KeyValuePair<long, TSource> Current
            {
                get
                {
                    //verify that MoveNext is at least called once before Current is called 
                    if (_offset < _startIndex)
                    {
                        throw new InvalidOperationException(SR.PartitionerStatic_CurrentCalledBeforeMoveNext);
                    }

                    Debug.Assert(_offset >= _startIndex && _offset <= _endIndex);
                    return (new KeyValuePair<long, TSource>(_offset, _list[_offset]));
                }
            }
        }
        #endregion

        #region static partitioning for Arrays
        /// <summary>
        /// Inherits from StaticIndexRangePartitioner
        /// Provides customized implementation of SourceCount and CreatePartition for Array
        /// </summary>
        private class StaticIndexRangePartitionerForArray<TSource> : StaticIndexRangePartitioner<TSource, TSource[]>
        {
            TSource[] _array;
            internal StaticIndexRangePartitionerForArray(TSource[] array)
                : base()
            {
                Debug.Assert(array != null);
                _array = array;
            }
            override protected int SourceCount
            {
                get { return _array.Length; }
            }
            override protected IEnumerator<KeyValuePair<long, TSource>> CreatePartition(int startIndex, int endIndex)
            {
                return new StaticIndexRangePartitionForArray<TSource>(_array, startIndex, endIndex);
            }
        }

        /// <summary>
        /// Inherits from StaticIndexRangePartitioner
        /// Provides customized implementation of SourceCount and CreatePartition
        /// </summary>
        private class StaticIndexRangePartitionForArray<TSource> : StaticIndexRangePartition<TSource>
        {
            //the source collection shared by all partitions
            private volatile TSource[] _array;

            internal StaticIndexRangePartitionForArray(TSource[] array, int startIndex, int endIndex)
                : base(startIndex, endIndex)
            {
                Debug.Assert(startIndex >= 0 && endIndex <= array.Length - 1);
                _array = array;
            }

            override public KeyValuePair<long, TSource> Current
            {
                get
                {
                    //verify that MoveNext is at least called once before Current is called 
                    if (_offset < _startIndex)
                    {
                        throw new InvalidOperationException(SR.PartitionerStatic_CurrentCalledBeforeMoveNext);
                    }

                    Debug.Assert(_offset >= _startIndex && _offset <= _endIndex);
                    return (new KeyValuePair<long, TSource>(_offset, _array[_offset]));
                }
            }
        }
        #endregion


        #region Utility functions
        /// <summary>
        /// A very simple primitive that allows us to share a value across multiple threads.
        /// </summary>
        private class SharedInt
        {
            internal volatile int Value;

            internal SharedInt(int value)
            {
                Value = value;
            }
        }

        /// <summary>
        /// A very simple primitive that allows us to share a value across multiple threads.
        /// </summary>
        private class SharedBool
        {
            internal volatile bool Value;

            internal SharedBool(bool value)
            {
                Value = value;
            }
        }

        /// <summary>
        /// A very simple primitive that allows us to share a value across multiple threads.
        /// </summary>
        private class SharedLong
        {
            internal long Value;
            internal SharedLong(long value)
            {
                Value = value;
            }
        }

        //--------------------
        // The following part calculates the default chunk size. It is copied from System.Linq.Parallel.Scheduling.
        //--------------------

        // The number of bytes we want "chunks" to be, when partitioning, etc. We choose 4 cache
        // lines worth, assuming 128b cache line.  Most (popular) architectures use 64b cache lines,
        // but choosing 128b works for 64b too whereas a multiple of 64b isn't necessarily sufficient
        // for 128b cache systems.  So 128b it is.
        private const int DEFAULT_BYTES_PER_UNIT = 128;
        private const int DEFAULT_BYTES_PER_CHUNK = DEFAULT_BYTES_PER_UNIT * 4;

        private static int GetDefaultChunkSize<TSource>()
        {
            int chunkSize;

            // Because of the lack of typeof(T).IsValueType we need two pieces of information
            // to determine this. default(T) will return a non null for Value Types, except those
            // using Nullable<>, that is why we need a second condition.
            if (default(TSource)! != null || Nullable.GetUnderlyingType(typeof(TSource)) != null) // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/34757
            {
                // Marshal.SizeOf fails for value types that don't have explicit layouts. We
                // just fall back to some arbitrary constant in that case. Is there a better way?
                {
                    // We choose '128' because this ensures, no matter the actual size of the value type,
                    // the total bytes used will be a multiple of 128. This ensures it's cache aligned.
                    chunkSize = DEFAULT_BYTES_PER_UNIT;
                }
            }
            else
            {
                Debug.Assert((DEFAULT_BYTES_PER_CHUNK % IntPtr.Size) == 0, "bytes per chunk should be a multiple of pointer size");
                chunkSize = (DEFAULT_BYTES_PER_CHUNK / IntPtr.Size);
            }
            return chunkSize;
        }
        #endregion

    }
}
