// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// Contains configuration settings advised to be used by metrics consumers when recording measurements for a given <see cref="Instrument{T}"/>.
    /// </summary>
    /// <typeparam name="T">Instrument value type.</typeparam>
    public sealed class InstrumentAdvice<T> where T : struct
    {
        /// <summary>
        /// Constructs a new instance of <see cref="InstrumentAdvice{T}"/>.
        /// </summary>
        /// <remarks>
        /// Notes:
        /// <list type="bullet">
        /// <item>An empty set of bucket boundaries hints that histogram instruments by default should NOT contain buckets and should only track count and sum values.</item>
        /// <item>A set of distinct increasing values for histogram bucket boundaries hints that histogram instruments should use those for its default bucket configuration.</item>
        /// </list>
        /// </remarks>
        /// <param name="histogramExplicitBucketBoundaries">Explicit bucket boundaries advised to be used with histogram instruments.</param>
        public InstrumentAdvice(IEnumerable<T> histogramExplicitBucketBoundaries)
        {
            if (histogramExplicitBucketBoundaries is null)
            {
                throw new ArgumentNullException(nameof(histogramExplicitBucketBoundaries));
            }

            List<T> explicitBucketBoundariesCopy = new List<T>(histogramExplicitBucketBoundaries);

            if (!IsSortedAndDistinct(explicitBucketBoundariesCopy))
            {
                throw new ArgumentException(SR.InvalidHistogramExplicitBucketBoundaries, nameof(histogramExplicitBucketBoundaries));
            }

            HistogramExplicitBucketBoundaries = new ReadOnlyCollection<T>(explicitBucketBoundariesCopy);
        }

        /// <summary>
        /// Gets the explicit bucket boundaries advised to be used with histogram instruments.
        /// </summary>
        /// <remarks>
        /// Notes:
        /// <list type="bullet">
        /// <item>A <see langword="null"/> value means no bucket boundaries have been configured and default values should be used for bucket configuration.</item>
        /// <item>An empty set of bucket boundaries hints that the histogram by default should NOT contain buckets and should only track count and sum values.</item>
        /// <item>A set of distinct increasing values for bucket boundaries hints that the histogram should use those for its default bucket configuration.</item>
        /// </list>
        /// </remarks>
        public IReadOnlyList<T>? HistogramExplicitBucketBoundaries { get; }

        private static bool IsSortedAndDistinct(List<T> values)
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
