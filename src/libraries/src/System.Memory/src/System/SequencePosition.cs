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
        private readonly object _object;
        private readonly int _integer;

        /// <summary>
        /// Creates new <see cref="SequencePosition"/>
        /// </summary>
        public SequencePosition(object @object, int integer)
        {
            _object = @object;
            _integer = integer;
        }

        /// <summary>
        /// Returns object part of this <see cref="SequencePosition"/>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object GetObject() => _object;

        /// <summary>
        /// Returns integer part of this <see cref="SequencePosition"/>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int GetInteger() => _integer;

        /// <summary>
        /// Returns true if left and right point at the same segment and have the same index.
        /// </summary>
        public static bool operator ==(SequencePosition left, SequencePosition right) => left._integer == right._integer && left._object == right._object;

        /// <summary>
        /// Returns true if left and right do not point at the same segment and have the same index.
        /// </summary>
        public static bool operator !=(SequencePosition left, SequencePosition right) => !(left == right);

        /// <inheritdoc />
        public bool Equals(SequencePosition other) => this == other;

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => obj is SequencePosition other && this == other;

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => HashHelpers.Combine(_object?.GetHashCode() ?? 0, _integer);
    }
}
