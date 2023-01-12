// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Frozen
{
    /// <summary>
    /// A few numbers to drive implementation selection heuristics.
    /// </summary>
    /// <remarks>
    /// These numbers were arrived through simple benchmarks conducted against .NET 7.
    /// It's worth potentially tweaking these values if the implementation of the
    /// collections changes in a substantial way, or if the JIT got smarter over time.
    /// </remarks>
    internal static class Constants
    {
        /// <summary>
        /// Threshold when we switch from scanning to hashing for non-integer collections.
        /// </summary>
        /// <remarks>
        /// This determines the threshold where we switch from
        /// the scanning-based SmallFrozenDictionary/Set to the hashing-based
        /// DefaultFrozenDictionary/Set.
        /// </remarks>
        public const int MaxItemsInSmallFrozenCollection = 4;

        /// <summary>
        /// Threshold when we switch from scanning to hashing integer collections.
        /// </summary>
        /// <remarks>
        /// This determines the threshold when we switch from the scanning
        /// SmallIntegerFrozenDictionary/Set to the
        /// hashing IntegerFrozenDictionary/Set.
        /// </remarks>
        public const int MaxItemsInSmallIntegerFrozenCollection = 10;

        /// <summary>
        /// How much free space is allowed in a sparse integer set
        /// </summary>
        /// <remarks>
        /// This determines how much free space is allowed in a sparse integer set.
        /// This is a space/perf trade off. The sparse sets just use a bit vector to
        /// hold the state, so lookup is always fast. But there's a point where you're
        /// too much heap space.
        /// </remarks>
        public const int MaxSparsenessFactorInSparseRangeIntegerSet = 8;
    }
}
