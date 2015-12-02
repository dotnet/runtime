// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace System.Reflection.Metadata.Decoding
{
    /// <summary>
    /// Represents the shape of an array type.
    /// </summary>
    public struct ArrayShape
    {
        private readonly int _rank;
        private readonly ImmutableArray<int> _sizes;
        private readonly ImmutableArray<int> _lowerBounds;

        public ArrayShape(int rank, ImmutableArray<int> sizes, ImmutableArray<int> lowerBounds)
        {
            _rank = rank;
            _sizes = sizes;
            _lowerBounds = lowerBounds;
        }

        /// <summary>
        /// Gets the number of dimensions in the array.
        /// </summary>
        public int Rank
        {
            get { return _rank; }
        }

        /// <summary>
        /// Gets the sizes of each dimension. Length may be smaller than rank, in which case the trailing dimensions have unspecified sizes.
        /// </summary>
        public ImmutableArray<int> Sizes
        {
            get { return _sizes; }
        }

        /// <summary>
        /// Gets the lower-bounds of each dimension. Length may be smaller than rank, in which case the trailing dimensions have unspecified lower bounds.
        /// </summary>
        public ImmutableArray<int> LowerBounds
        {
            get { return _lowerBounds; }
        }
    }
}
