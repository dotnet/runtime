// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Represents the shape of an array type.
    /// </summary>
    public readonly struct ArrayShape
    {
        /// <summary>
        /// Gets the number of dimensions in the array.
        /// </summary>
        public int Rank { get; }

        /// <summary>
        /// Gets the sizes of each dimension. Length may be smaller than rank, in which case the trailing dimensions have unspecified sizes.
        /// </summary>
        public ImmutableArray<int> Sizes { get; }

        /// <summary>
        /// Gets the lower-bounds of each dimension. Length may be smaller than rank, in which case the trailing dimensions have unspecified lower bounds.
        /// </summary>
        public ImmutableArray<int> LowerBounds { get; }

        public ArrayShape(int rank, ImmutableArray<int> sizes, ImmutableArray<int> lowerBounds)
        {
            Rank = rank;
            Sizes = sizes;
            LowerBounds = lowerBounds;
        }
    }
}
