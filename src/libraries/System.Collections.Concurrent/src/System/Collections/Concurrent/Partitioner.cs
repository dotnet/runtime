// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Partitioner.cs
//

//
// Represents a particular way of splitting a collection into multiple partitions.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// Represents a particular manner of splitting a data source into multiple partitions.
    /// </summary>
    /// <typeparam name="TSource">Type of the elements in the collection.</typeparam>
    /// <remarks>
    /// <para>
    /// Inheritors of <see cref="Partitioner{TSource}"/> must adhere to the following rules:
    /// <ol>
    /// <li><see cref="GetPartitions"/> should throw a
    /// <see cref="T:System.ArgumentOutOfRangeException"/> if the requested partition count is less than or
    /// equal to zero.</li>
    /// <li><see cref="GetPartitions"/> should always return a number of enumerables equal to the requested
    /// partition count. If the partitioner runs out of data and cannot create as many partitions as 
    /// requested, an empty enumerator should be returned for each of the remaining partitions. If this rule
    /// is not followed, consumers of the implementation may throw a <see
    /// cref="T:System.InvalidOperationException"/>.</li>
    /// <li><see cref="GetPartitions"/> and <see cref="GetDynamicPartitions"/>
    /// should never return null. If null is returned, a consumer of the implementation may throw a
    /// <see cref="T:System.InvalidOperationException"/>.</li>
    /// <li><see cref="GetPartitions"/> and <see cref="GetDynamicPartitions"/> should always return
    /// partitions that can fully and uniquely enumerate the input data source. All of the data and only the
    /// data contained in the input source should be enumerated, with no duplication that was not already in
    /// the input, unless specifically required by the particular partitioner's design. If this is not
    /// followed, the output ordering may be scrambled.</li>
    /// </ol>
    /// </para>
    /// </remarks>
    public abstract class Partitioner<TSource>
    {
        /// <summary>
        /// Partitions the underlying collection into the given number of partitions.
        /// </summary>
        /// <param name="partitionCount">The number of partitions to create.</param>
        /// <returns>A list containing <paramref name="partitionCount"/> enumerators.</returns>
        public abstract IList<IEnumerator<TSource>> GetPartitions(int partitionCount);

        /// <summary>
        /// Gets whether additional partitions can be created dynamically.
        /// </summary>
        /// <returns>
        /// true if the <see cref="Partitioner{TSource}"/> can create partitions dynamically as they are
        /// requested; false if the <see cref="Partitioner{TSource}"/> can only allocate
        /// partitions statically.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If a derived class does not override and implement <see cref="GetDynamicPartitions"/>,
        /// <see cref="SupportsDynamicPartitions"/> should return false. The value of <see
        /// cref="SupportsDynamicPartitions"/> should not vary over the lifetime of this instance.
        /// </para>
        /// </remarks>
        public virtual bool SupportsDynamicPartitions
        {
            get { return false; }
        }

        /// <summary>
        /// Creates an object that can partition the underlying collection into a variable number of
        /// partitions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The returned object implements the <see
        /// cref="T:System.Collections.Generic.IEnumerable{TSource}"/> interface. Calling <see
        /// cref="System.Collections.Generic.IEnumerable{TSource}.GetEnumerator">GetEnumerator</see> on the
        /// object creates another partition over the sequence.
        /// </para>
        /// <para>
        /// The <see cref="GetDynamicPartitions"/> method is only supported if the <see
        /// cref="SupportsDynamicPartitions"/>
        /// property returns true.
        /// </para>
        /// </remarks>
        /// <returns>An object that can create partitions over the underlying data source.</returns>
        /// <exception cref="NotSupportedException">Dynamic partitioning is not supported by this
        /// partitioner.</exception>
        public virtual IEnumerable<TSource> GetDynamicPartitions()
        {
            throw new NotSupportedException(SR.Partitioner_DynamicPartitionsNotSupported);
        }
    }
}
