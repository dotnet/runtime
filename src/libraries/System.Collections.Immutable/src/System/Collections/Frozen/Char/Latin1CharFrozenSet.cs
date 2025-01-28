// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen set to use when the values are <see cref="char"/>s, the default comparer is used, and all values are &lt;= 255.</summary>
    /// <remarks>
    /// This should practically always be used with ASCII-only values, but since we're using the <see cref="BitVector256"/> helper,
    /// we might as well use it for values between 128-255 too (hence Latin1 instead of Ascii in the name).
    /// </remarks>
    internal sealed class Latin1CharFrozenSet : FrozenSetInternalBase<char, Latin1CharFrozenSet.GSW>
    {
        private readonly BitVector256 _values;
        private readonly int _count;

        internal Latin1CharFrozenSet(ReadOnlySpan<char> values) : base(EqualityComparer<char>.Default)
        {
            Debug.Assert(!values.IsEmpty);

            foreach (char c in values)
            {
                if (c > 255)
                {
                    // Source was modified concurrent with the call to FrozenSet.Create.
                    ThrowHelper.ThrowInvalidOperationException();
                }

                _values.Set(c);
            }

            _count = _values.Count();
        }

        /// <inheritdoc />
        [field: MaybeNull]
        private protected override char[] ItemsCore => field ??= _values.GetCharValues();

        /// <inheritdoc />
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(ItemsCore);

        /// <inheritdoc />
        private protected override int CountCore => _count;

        /// <inheritdoc />
        private protected override int FindItemIndex(char item) => _values.IndexOf(item);

        /// <inheritdoc />
        private protected override bool ContainsCore(char item) => _values.Contains256(item);

        internal struct GSW : IGenericSpecializedWrapper
        {
            private Latin1CharFrozenSet _set;
            public void Store(FrozenSet<char> set) => _set = (Latin1CharFrozenSet)set;

            public int Count => _set.Count;
            public IEqualityComparer<char> Comparer => _set.Comparer;
            public int FindItemIndex(char item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
