// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Immutable
{
    public readonly partial struct ImmutableArray<T> : IReadOnlyList<T>, IList<T>, IEquatable<ImmutableArray<T>>, IList, IImmutableArray, IStructuralComparable, IStructuralEquatable, IImmutableList<T>
    {
        /// <summary>
        /// Creates a <see cref="ReadOnlySpan{T}"/> over the portion of current <see cref="ImmutableArray{T}"/> based on specified <paramref name="range"/>
        /// </summary>
        /// <param name="range">Range in current <see cref="ImmutableArray{T}"/>.</param>
        /// <returns>The <see cref="ReadOnlySpan{T}"/> representation of the <see cref="ImmutableArray{T}"/></returns>
        public ReadOnlySpan<T> AsSpan(Range range)
        {
            var self = this;
            self.ThrowNullRefIfNotInitialized();

            (int start, int length) = range.GetOffsetAndLength(self.Length);
            return new ReadOnlySpan<T>(self.array, start, length);
        }
    }
}
