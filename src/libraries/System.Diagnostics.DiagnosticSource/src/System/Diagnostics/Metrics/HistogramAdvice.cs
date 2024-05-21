// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// Contains settings used to advise metrics consumers how to construct storage for <see cref="Histogram{T}"/> instruments.
    /// </summary>
    /// <typeparam name="T">Histogram value type.</typeparam>
    public sealed class HistogramAdvice<T> where T : struct
    {
        /// <summary>
        /// Constructs a new instance of <see cref="HistogramAdvice{T}"/>.
        /// </summary>
        /// <param name="explicitBucketBoundaries">
        /// <para>Explicit bucket boundaries advised to be used with the histogram.</para>
        /// <para>Notes:
        /// <list type="bullet">
        /// <item>An empty set of bucket boundaries hints that the histogram by default should NOT contain buckets and should only track count and sum values.</item>
        /// <item>A set of distinct increasing values for bucket boundaries hints that the histogram should use those for its default bucket configuration.</item>
        /// </list>
        /// </para>
        /// </param>
        public HistogramAdvice(IEnumerable<T> explicitBucketBoundaries)
        {
            if (explicitBucketBoundaries is null)
            {
                throw new ArgumentNullException(nameof(explicitBucketBoundaries));
            }

            IReadOnlyList<T> explicitBucketBoundariesCopy = new ReadOnlyCollection<T>(new List<T>(explicitBucketBoundaries));

            if (!IsSortedAndDistinct(explicitBucketBoundariesCopy))
            {
                throw new ArgumentException(SR.InvalidHistogramExplicitBucketBoundaries, nameof(explicitBucketBoundaries));
            }

            ExplicitBucketBoundaries = explicitBucketBoundariesCopy;
        }

        /// <summary>
        /// Gets the explicit bucket boundaries advised to be used with the histogram.
        /// </summary>
        /// <remarks>
        /// Notes:
        /// <list type="bullet">
        /// <item>A <see langword="null"/> value means no bucket boundaries have been configured and default values should be used for bucket configuration.</item>
        /// <item>An empty set of bucket boundaries hints that the histogram by default should NOT contain buckets and should only track count and sum values.</item>
        /// <item>A set of distinct increasing values for bucket boundaries hints that the histogram should use those for its default bucket configuration.</item>
        /// </list>
        /// </remarks>
        public IReadOnlyList<T>? ExplicitBucketBoundaries { get; }

        private static bool IsSortedAndDistinct(IReadOnlyList<T> values)
        {
            Comparer<T> comparer = Comparer<T>.Default;

            for (int i = 1; i < values.Count; i++)
            {
                if (comparer.Compare(values[i - 1], values[i]) >= 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
