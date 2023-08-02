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
        public int Index { get; }

        /// <summary>
        /// The desired length for the slice (exclusive).
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Moves the starting index one to the left (decrements)
        /// <remarks>When we're doing left-justified slicing, the would move the starting point closer to zero, when
        /// we're doing right-justified slicing, this moves index away from zero, so toward the beginning of the
        /// string (as indexed from the right side).</remarks>
        /// </summary>
        public abstract void GoLeft();

        /// <summary>
        /// Moves the starting index one to the right (increments)
        /// <remarks>When we're doing left-justified slicing, the would move the starting point away from zero, when
        /// we're doing right-justified slicing, this moves index toward zero, so toward the end of the
        /// string (as indexed from the right side).</remarks>
        /// </summary>
        public abstract void GoRight();

        /// <summary>
        /// Sets up for either left or right justified slicing of a string
        /// </summary>
        /// <param name="index">The starting index for slicing, if zero or greater, then this is a left-justified slice from
        /// the start of the input string. If less then zero, then this is a right-justified slice from the end of the
        /// input string.</param>
        /// <param name="count">The number of characters to include in the slice.</param>
        /// <remarks>Typical left-justified slices would pass <paramref name="index"/> of zero. Typical right-justified
        /// slices would pass <paramref name="index"/> value that is the negated value of the <paramref name="count"/> count
        /// so that the slice would be the last set of characters</remarks>
        public abstract void Start(int index, int count);

        /// <summary>
        /// Creates a new readonly span over the portion of the target string.
        /// </summary>
        /// <param name="s">The target string.</param>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified index or count is not in range.
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

        protected int _index;
        protected int _count;

        /// <inheritdoc />
        public int Index { get => _index; }
        /// <inheritdoc />
        public int Count { get => _count; }

        /// <inheritdoc />
        public void GoLeft() => _index--;

        /// <inheritdoc />
        public void GoRight() => _index++;

        /// <inheritdoc />
        public void Start(int index, int count)
        {
            _index = index;
            _count = count;
        }

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
