// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Collections.Frozen.String.SubstringEquality
{
    internal static class Slicers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> LeftSlice(string s, int index, int count) => s.AsSpan(index, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> RightSlice(string s, int index, int count) => s.AsSpan(s.Length + index, count);
    }

    internal static class OrdinalEquality
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y) => x.SequenceEqual(y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHashCode(ReadOnlySpan<char> s) => Hashing.GetHashCodeOrdinal(s);
    }

    internal static class OrdinalInsensitiveEquality
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y) => x.Equals(y, StringComparison.OrdinalIgnoreCase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHashCode(ReadOnlySpan<char> s) => Hashing.GetHashCodeOrdinalIgnoreCase(s);
    }

    /// <inheritdoc/>
    internal sealed class LeftSubstringOrdinalComparer : SubstringEqualityComparerBase<LeftSubstringOrdinalComparer.GSW>
    {
        internal struct GSW : IGenericSpecializedWrapper
        {
            private LeftSubstringOrdinalComparer _this;
            public void Store(ISubstringEqualityComparer @this) => _this = (LeftSubstringOrdinalComparer)@this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<char> Slice(string s) => Slicers.LeftSlice(s, _this.Index, _this.Count);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(string? x, string? y) => OrdinalEquality.Equals(Slice(x!), Slice(y!));
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(string s) => OrdinalEquality.GetHashCode(Slice(s));
        }
    }

    /// <inheritdoc/>
    internal sealed class RightSubstringOrdinalComparer : SubstringEqualityComparerBase<RightSubstringOrdinalComparer.GSW>
    {
        internal struct GSW : IGenericSpecializedWrapper
        {
            private RightSubstringOrdinalComparer _this;
            public void Store(ISubstringEqualityComparer @this) => _this = (RightSubstringOrdinalComparer)@this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<char> Slice(string s) => Slicers.RightSlice(s, _this.Index, _this.Count);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(string? x, string? y) => OrdinalEquality.Equals(Slice(x!), Slice(y!));
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(string s) => OrdinalEquality.GetHashCode(Slice(s));
        }
    }

    /// <inheritdoc/>
    internal sealed class LeftSubstringCaseInsensitiveComparer : SubstringEqualityComparerBase<LeftSubstringCaseInsensitiveComparer.GSW>
    {
        internal struct GSW : IGenericSpecializedWrapper
        {
            private LeftSubstringCaseInsensitiveComparer _this;
            public void Store(ISubstringEqualityComparer @this) => _this = (LeftSubstringCaseInsensitiveComparer)@this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<char> Slice(string s) => Slicers.LeftSlice(s, _this.Index, _this.Count);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(string? x, string? y) => OrdinalInsensitiveEquality.Equals(Slice(x!), Slice(y!));
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(string s) => OrdinalInsensitiveEquality.GetHashCode(Slice(s));
        }
    }

    internal sealed class RightSubstringCaseInsensitiveComparer : SubstringEqualityComparerBase<RightSubstringCaseInsensitiveComparer.GSW>
    {
        internal struct GSW : IGenericSpecializedWrapper
        {
            private RightSubstringCaseInsensitiveComparer _this;
            public void Store(ISubstringEqualityComparer @this) => _this = (RightSubstringCaseInsensitiveComparer)@this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<char> Slice(string s) => Slicers.RightSlice(s, _this.Index, _this.Count);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(string? x, string? y) => OrdinalInsensitiveEquality.Equals(Slice(x!), Slice(y!));
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(string s) => OrdinalInsensitiveEquality.GetHashCode(Slice(s));
        }
    }

    internal sealed class FullStringEqualityComparer : SubstringEqualityComparerBase<FullStringEqualityComparer.GSW>
    {
        public FullStringEqualityComparer()
        {
            _index = 0;
            _count = 0;
        }

        internal struct GSW : IGenericSpecializedWrapper
        {
            public void Store(ISubstringEqualityComparer @this)
            {
                // this one doesn't do slicing, so no wrapper or state
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<char> Slice(string s) => s.AsSpan();
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(string? x, string? y) => string.Equals(x, y);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(string s) => s.GetHashCode();
        }
    }
}
