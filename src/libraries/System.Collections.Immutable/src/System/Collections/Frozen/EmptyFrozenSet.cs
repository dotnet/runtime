// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace System.Collections.Frozen
{
    /// <summary>Provides an empty <see cref="FrozenSet{T}"/> to use when there are zero values to be stored.</summary>
    internal sealed class EmptyFrozenSet<T> : FrozenSet<T>
    {
        internal EmptyFrozenSet() : base(EqualityComparer<T>.Default) { }

        /// <inheritdoc />
        private protected override ImmutableArray<T> ItemsCore => ImmutableArray<T>.Empty;

        /// <inheritdoc />
        private protected override int CountCore => 0;

        /// <inheritdoc />
        private protected override int FindItemIndex(T item) => -1;

        /// <inheritdoc />
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(Array.Empty<T>());

        /// <inheritdoc />
        private protected override bool IsProperSubsetOfCore(IEnumerable<T> other) => !OtherIsEmpty(other);

        /// <inheritdoc />
        private protected override bool IsProperSupersetOfCore(IEnumerable<T> other) => false;

        /// <inheritdoc />
        private protected override bool IsSubsetOfCore(IEnumerable<T> other) => true;

        /// <inheritdoc />
        private protected override bool IsSupersetOfCore(IEnumerable<T> other) => OtherIsEmpty(other);

        /// <inheritdoc />
        private protected override bool OverlapsCore(IEnumerable<T> other) => false;

        /// <inheritdoc />
        private protected override bool SetEqualsCore(IEnumerable<T> other) => OtherIsEmpty(other);

        private static bool OtherIsEmpty(IEnumerable<T> other) =>
            other is IReadOnlyCollection<T> s ? s.Count == 0 : // TODO https://github.com/dotnet/runtime/issues/42254: Remove if/when Any includes this check
            !other.Any();
    }
}
