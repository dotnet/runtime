// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System
{
    /// <summary>
    /// Represents position in non-contiguous set of memory.
    /// Parts of this type should not be interpreted by anything but the type that created it.
    /// </summary>
    public readonly struct SequencePosition : IEquatable<SequencePosition>
    {
        private readonly object? _object;
        private readonly int _integer;

        /// <summary>
        /// Creates new <see cref="SequencePosition"/>
        /// </summary>
        public SequencePosition(object? @object, int integer)
        {
            _object = @object;
            _integer = integer;
        }

        /// <summary>
        /// Returns object part of this <see cref="SequencePosition"/>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object? GetObject() => _object;

        /// <summary>
        /// Returns integer part of this <see cref="SequencePosition"/>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int GetInteger() => _integer;

        /// <summary>
        /// Indicates whether the current <see cref="SequencePosition"/> is equal to another <see cref="SequencePosition"/>.
        /// <see cref="SequencePosition"/> equality does not guarantee that they point to the same location in <see cref="System.Buffers.ReadOnlySequence{T}" />
        /// </summary>
        public bool Equals(SequencePosition other) => _integer == other._integer && object.Equals(this._object, other._object);

        /// <summary>
        /// Indicates whether the current <see cref="SequencePosition"/> is equal to another <see cref="object"/>.
        /// <see cref="SequencePosition"/> equality does not guarantee that they point to the same location in <see cref="System.Buffers.ReadOnlySequence{T}" />
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is SequencePosition other && this.Equals(other);

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => HashCode.Combine(_object?.GetHashCode() ?? 0, _integer);
    }
}
