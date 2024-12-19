// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen set to use when the values are <see cref="byte"/>s and the default comparer is used.</summary>
    internal sealed partial class ByteFrozenSet : FrozenSetInternalBase<byte, ByteFrozenSet.GSW>
    {
        private readonly BitVector256 _values;
        private readonly int _count;
        private byte[]? _items;

        internal ByteFrozenSet(ReadOnlySpan<byte> values) : base(EqualityComparer<byte>.Default)
        {
            Debug.Assert(!values.IsEmpty);

            foreach (byte b in values)
            {
                _values.Set(b);
            }

            _count = _values.Count();
        }

        internal ByteFrozenSet(IEnumerable<byte> values) : base(EqualityComparer<byte>.Default)
        {
            foreach (byte b in values)
            {
                _values.Set(b);
            }

            _count = _values.Count();

            Debug.Assert(_count > 0);
        }

        /// <inheritdoc />
        private protected override byte[] ItemsCore => _items ??= _values.GetByteValues();

        /// <inheritdoc />
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(ItemsCore);

        /// <inheritdoc />
        private protected override int CountCore => _count;

        /// <inheritdoc />
        private protected override int FindItemIndex(byte item) => _values.IndexOf(item);

        /// <inheritdoc />
        private protected override bool ContainsCore(byte item) => _values.Contains(item);

        internal struct GSW : IGenericSpecializedWrapper
        {
            private ByteFrozenSet _set;
            public void Store(FrozenSet<byte> set) => _set = (ByteFrozenSet)set;

            public int Count => _set.Count;
            public IEqualityComparer<byte> Comparer => _set.Comparer;
            public int FindItemIndex(byte item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
