// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// OrderablePartitioner.cs
//

//
//
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// Represents a particular manner of splitting an orderable data source into multiple partitions.
    /// </summary>
    /// <typeparam name="TSource">Type of the elements in the collection.</typeparam>
    /// <remarks>
    /// <para>
    /// Each element in each partition has an integer index associated with it, which determines the relative
    /// order of that element against elements in other partitions.
    /// </para>
    /// <para>
    /// Inheritors of <see cref="OrderablePartitioner{TSource}"/> must adhere to the following rules:
    /// <ol>
    /// <li>All indices must be unique, such that there may not be duplicate indices. If all indices are not
    /// unique, the output ordering may be scrambled.</li>
    /// <li>All indices must be non-negative. If any indices are negative, consumers of the implementation
    /// may throw exceptions.</li>
    /// <li><see cref="GetPartitions"/> and <see cref="GetOrderablePartitions"/> should throw a
    /// <see cref="System.ArgumentOutOfRangeException"/> if the requested partition count is less than or
    /// equal to zero.</li>
    /// <li><see cref="GetPartitions"/> and <see cref="GetOrderablePartitions"/> should always return a number
    /// of enumerables equal to the requested partition count. If the partitioner runs out of data and cannot
    /// create as many partitions as requested, an empty enumerator should be returned for each of the
    /// remaining partitions. If this rule is not followed, consumers of the implementation may throw a <see
    /// cref="System.InvalidOperationException"/>.</li>
    /// <li><see cref="GetPartitions"/>, <see cref="GetOrderablePartitions"/>,
    /// <see cref="GetDynamicPartitions"/>, and <see cref="GetOrderableDynamicPartitions"/>
    /// should never return null. If null is returned, a consumer of the implementation may throw a
    /// <see cref="System.InvalidOperationException"/>.</li>
    /// <li><see cref="GetPartitions"/>, <see cref="GetOrderablePartitions"/>,
    /// <see cref="GetDynamicPartitions"/>, and <see cref="GetOrderableDynamicPartitions"/>
    /// should always return partitions that can fully and uniquely enumerate the input data source. All of
    /// the data and only the data contained in the input source should be enumerated, with no duplication
    /// that was not already in the input, unless specifically required by the particular partitioner's
    /// design. If this is not followed, the output ordering may be scrambled.</li>
    /// <li>If <see cref="KeysOrderedInEachPartition"/> returns true, each partition must return elements
    /// with increasing key indices.</li>
    /// <li>If <see cref="KeysOrderedAcrossPartitions"/> returns true, all the keys in partition numbered N
    /// must be larger than all the keys in partition numbered N-1.</li>
    /// <li>If <see cref="KeysNormalized"/> returns true, all indices must be monotonically increasing from
    /// 0, though not necessarily within a single partition.</li>
    /// </ol>
    /// </para>
    /// </remarks>
    public abstract class OrderablePartitioner<TSource> : Partitioner<TSource>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderablePartitioner{TSource}"/> class with the
        /// specified constraints on the index keys.
        /// </summary>
        /// <param name="keysOrderedInEachPartition">
        /// Indicates whether the elements in each partition are yielded in the order of
        /// increasing keys.
        /// </param>
        /// <param name="keysOrderedAcrossPartitions">
        /// Indicates whether elements in an earlier partition always come before
        /// elements in a later partition. If true, each element in partition 0 has a smaller order key than
        /// any element in partition 1, each element in partition 1 has a smaller order key than any element
        /// in partition 2, and so on.
        /// </param>
        /// <param name="keysNormalized">
        /// Indicates whether keys are normalized. If true, all order keys are distinct
        /// integers in the range [0 .. numberOfElements-1]. If false, order keys must still be distinct, but
        /// only their relative order is considered, not their absolute values.
        /// </param>
        protected OrderablePartitioner(bool keysOrderedInEachPartition, bool keysOrderedAcrossPartitions, bool keysNormalized)
        {
            KeysOrderedInEachPartition = keysOrderedInEachPartition;
            KeysOrderedAcrossPartitions = keysOrderedAcrossPartitions;
            KeysNormalized = keysNormalized;
        }

        /// <summary>
        /// Partitions the underlying collection into the specified number of orderable partitions.
        /// </summary>
        /// <remarks>
        /// Each partition is represented as an enumerator over key-value pairs.
        /// The value of the pair is the element itself, and the key is an integer which determines
        /// the relative ordering of this element against other elements in the data source.
        /// </remarks>
        /// <param name="partitionCount">The number of partitions to create.</param>
        /// <returns>A list containing <paramref name="partitionCount"/> enumerators.</returns>
        public abstract IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount);

        /// <summary>
        /// Creates an object that can partition the underlying collection into a variable number of
        /// partitions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The returned object implements the <see
        /// cref="System.Collections.Generic.IEnumerable{TSource}"/> interface. Calling <see
        /// cref="System.Collections.Generic.IEnumerable{TSource}.GetEnumerator">GetEnumerator</see> on the
        /// object creates another partition over the sequence.
        /// </para>
        /// <para>
        /// Each partition is represented as an enumerator over key-value pairs. The value in the pair is the element
        /// itself, and the key is an integer which determines the relative ordering of this element against
        /// other elements.
        /// </para>
        /// <para>
        /// The <see cref="GetOrderableDynamicPartitions"/> method is only supported if the <see
        /// cref="System.Collections.Concurrent.Partitioner{TSource}.SupportsDynamicPartitions">SupportsDynamicPartitions</see>
        /// property returns true.
        /// </para>
        /// </remarks>
        /// <returns>An object that can create partitions over the underlying data source.</returns>
        /// <exception cref="NotSupportedException">Dynamic partitioning is not supported by this
        /// partitioner.</exception>
        public virtual IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions()
        {
            throw new NotSupportedException(SR.Partitioner_DynamicPartitionsNotSupported);
        }

        /// <summary>
        /// Gets whether elements in each partition are yielded in the order of increasing keys.
        /// </summary>
        public bool KeysOrderedInEachPartition { get; private set; }

        /// <summary>
        /// Gets whether elements in an earlier partition always come before elements in a later partition.
        /// </summary>
        /// <remarks>
        /// If <see cref="KeysOrderedAcrossPartitions"/> returns true, each element in partition 0 has a
        /// smaller order key than any element in partition 1, each element in partition 1 has a smaller
        /// order key than any element in partition 2, and so on.
        /// </remarks>
        public bool KeysOrderedAcrossPartitions { get; private set; }

        /// <summary>
        /// Gets whether order keys are normalized.
        /// </summary>
        /// <remarks>
        /// If <see cref="KeysNormalized"/> returns true, all order keys are distinct integers in the range
        /// [0 .. numberOfElements-1]. If the property returns false, order keys must still be distinct, but
        /// only their relative order is considered, not their absolute values.
        /// </remarks>
        public bool KeysNormalized { get; private set; }

        /// <summary>
        /// Partitions the underlying collection into the given number of ordered partitions.
        /// </summary>
        /// <remarks>
        /// The default implementation provides the same behavior as <see cref="GetOrderablePartitions"/> except
        /// that the returned set of partitions does not provide the keys for the elements.
        /// </remarks>
        /// <param name="partitionCount">The number of partitions to create.</param>
        /// <returns>A list containing <paramref name="partitionCount"/> enumerators.</returns>
        public override IList<IEnumerator<TSource>> GetPartitions(int partitionCount)
        {
            IList<IEnumerator<KeyValuePair<long, TSource>>> orderablePartitions = GetOrderablePartitions(partitionCount);

            if (orderablePartitions.Count != partitionCount)
            {
                throw new InvalidOperationException("OrderablePartitioner_GetPartitions_WrongNumberOfPartitions");
            }

            IEnumerator<TSource>[] partitions = new IEnumerator<TSource>[partitionCount];
            for (int i = 0; i < partitionCount; i++)
            {
                partitions[i] = new EnumeratorDropIndices(orderablePartitions[i]);
            }
            return partitions;
        }

        /// <summary>
        /// Creates an object that can partition the underlying collection into a variable number of
        /// partitions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The returned object implements the <see
        /// cref="System.Collections.Generic.IEnumerable{TSource}"/> interface. Calling <see
        /// cref="System.Collections.Generic.IEnumerable{TSource}.GetEnumerator">GetEnumerator</see> on the
        /// object creates another partition over the sequence.
        /// </para>
        /// <para>
        /// The default implementation provides the same behavior as <see cref="GetOrderableDynamicPartitions"/> except
        /// that the returned set of partitions does not provide the keys for the elements.
        /// </para>
        /// <para>
        /// The <see cref="GetDynamicPartitions"/> method is only supported if the <see
        /// cref="System.Collections.Concurrent.Partitioner{TSource}.SupportsDynamicPartitions"/>
        /// property returns true.
        /// </para>
        /// </remarks>
        /// <returns>An object that can create partitions over the underlying data source.</returns>
        /// <exception cref="NotSupportedException">Dynamic partitioning is not supported by this
        /// partitioner.</exception>
        public override IEnumerable<TSource> GetDynamicPartitions()
        {
            IEnumerable<KeyValuePair<long, TSource>> orderablePartitions = GetOrderableDynamicPartitions();
            return new EnumerableDropIndices(orderablePartitions);
        }

        /// <summary>
        /// Converts an enumerable over key-value pairs to an enumerable over values.
        /// </summary>
        private sealed class EnumerableDropIndices : IEnumerable<TSource>, IDisposable
        {
            private readonly IEnumerable<KeyValuePair<long, TSource>> _source;
            public EnumerableDropIndices(IEnumerable<KeyValuePair<long, TSource>> source)
            {
                _source = source;
            }
            public IEnumerator<TSource> GetEnumerator()
            {
                return new EnumeratorDropIndices(_source.GetEnumerator());
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((EnumerableDropIndices)this).GetEnumerator();
            }
            public void Dispose()
            {
                IDisposable? d = _source as IDisposable;
                if (d != null)
                {
                    d.Dispose();
                }
            }
        }

        private sealed class EnumeratorDropIndices : IEnumerator<TSource>
        {
            private readonly IEnumerator<KeyValuePair<long, TSource>> _source;
            public EnumeratorDropIndices(IEnumerator<KeyValuePair<long, TSource>> source)
            {
                _source = source;
            }
            public bool MoveNext()
            {
                return _source.MoveNext();
            }
            public TSource Current
            {
                get
                {
                    return _source.Current.Value;
                }
            }
            object? IEnumerator.Current
            {
                get
                {
                    return ((EnumeratorDropIndices)this).Current;
                }
            }
            public void Dispose()
            {
                _source.Dispose();
            }
            public void Reset()
            {
                _source.Reset();
            }
        }
    }
}
