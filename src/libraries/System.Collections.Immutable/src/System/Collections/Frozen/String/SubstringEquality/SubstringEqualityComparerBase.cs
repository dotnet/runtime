// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen.String.SubstringEquality
{
    internal interface ISubstringEqualityComparer : IEqualityComparer<string>
    {
        /// <summary>
        /// The index at which to begin this slice
        /// </summary>
        /// <remarks>Offset from the left side (if zero or positive) or right side (if negative)</remarks>
        public int Index { get; set; }

        /// <summary>
        /// The desired length for the slice (exclusive).
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Creates a new readonly span over the portion of the target string.
        /// </summary>
        /// <param name="s">The target string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified Index or Count is not in range.
        /// </exception>
        public abstract ReadOnlySpan<char> Slice(string s);
    }

    internal abstract class SubstringEqualityComparerBase<TThisWrapper> : ISubstringEqualityComparer
    where TThisWrapper : struct, SubstringEqualityComparerBase<TThisWrapper>.IGenericSpecializedWrapper
    {
        /// <summary>A wrapper around this that enables access to important members without making virtual calls.</summary>
        private readonly TThisWrapper _this;

        protected SubstringEqualityComparerBase()
        {
            _this = default;
            _this.Store(this);
        }

        /// <inheritdoc />
        public int Index { get; set; }
        /// <inheritdoc />
        public int Count { get; set; }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> Slice(string s) => _this.Slice(s);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(string? x, string? y) => _this.Equals(x, y);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(string s) => _this.GetHashCode(s);

        /// <summary>Used to enable generic specialization with reference types.</summary>
        /// <remarks>
        /// To avoid each of those incurring virtual dispatch to the derived type, the derived
        /// type hands down a struct wrapper through which all calls are performed.  This base
        /// class uses that generic struct wrapper to specialize and de-virtualize.
        /// </remarks>
        internal interface IGenericSpecializedWrapper
        {
            void Store(ISubstringEqualityComparer @this);
            public ReadOnlySpan<char> Slice(string s);
            public bool Equals(string? x, string? y);
            public int GetHashCode(string s);
        }
    }
}
