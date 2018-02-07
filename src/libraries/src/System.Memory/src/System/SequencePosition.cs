// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Numerics.Hashing;
using System.ComponentModel;

namespace System
{
    /// <summary>
    /// Represents position in non-contiguous set of memory.
    /// Properties of this type should not be interpreted by anything but the type that created it.
    /// </summary>
    public readonly struct SequencePosition : IEquatable<SequencePosition>
    {
        /// <summary>
        /// Creates new <see cref="SequencePosition"/>
        /// </summary>
        public SequencePosition(object segment, int index)
        {
            Segment = segment;
            Index = index;
        }

        /// <summary>
        /// Segment of memory this <see cref="SequencePosition"/> points to.
        /// </summary>
        public object Segment { get; }

        /// <summary>
        /// Index inside segment of memory this <see cref="SequencePosition"/> points to.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Returns true if left and right point at the same segment and have the same index.
        /// </summary>
        public static bool operator ==(SequencePosition left, SequencePosition right) => left.Index == right.Index && left.Segment == right.Segment;

        /// <summary>
        /// Returns true if left and right do not point at the same segment and have the same index.
        /// </summary>
        public static bool operator !=(SequencePosition left, SequencePosition right) => !(left == right);

        /// <inheritdoc />
        public bool Equals(SequencePosition position) => this == position;

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => obj is SequencePosition other && this == other;

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => HashHelpers.Combine(Segment?.GetHashCode() ?? 0, Index);

        /// <inheritdoc />
        public override string ToString() => this == default ? "(default)" : Segment == null ? Index.ToString(): $"{Segment}[{Index}]";
    }
}
