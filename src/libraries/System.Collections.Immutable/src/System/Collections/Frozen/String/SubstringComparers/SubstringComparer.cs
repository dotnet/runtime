// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Collections.Frozen.String.SubstringComparers
{
    internal sealed class LeftSubstringOrdinalComparer : SubstringComparerBase<LeftSubstringOrdinalComparer.GSW>
    {
        internal struct GSW : IGenericSpecializedWrapper
        {
            private LeftSubstringOrdinalComparer _this;
            public void Store(ISubstringComparer @this) => _this = (LeftSubstringOrdinalComparer)@this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<char> Slice(string s) => s.AsSpan(_this.Index, _this.Count);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(string? x, string? y) => Slice(x!).SequenceEqual(Slice(y!));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(string s) => Hashing.GetHashCodeOrdinal(Slice(s));
        }
    }

    internal sealed class RightSubstringOrdinalComparer : SubstringComparerBase<RightSubstringOrdinalComparer.GSW>
    {
        internal struct GSW : IGenericSpecializedWrapper
        {
            private RightSubstringOrdinalComparer _this;
            public void Store(ISubstringComparer @this) => _this = (RightSubstringOrdinalComparer)@this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<char> Slice(string s) => s.AsSpan(s.Length + _this.Index, _this.Count);


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(string? x, string? y) => Slice(x!).SequenceEqual(Slice(y!));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(string s) => Hashing.GetHashCodeOrdinal(Slice(s));
        }
    }

    internal sealed class LeftSubstringCaseInsensitiveComparer : SubstringComparerBase<LeftSubstringCaseInsensitiveComparer.GSW>
    {
        internal struct GSW : IGenericSpecializedWrapper
        {
            private LeftSubstringCaseInsensitiveComparer _this;
            public void Store(ISubstringComparer @this) => _this = (LeftSubstringCaseInsensitiveComparer)@this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<char> Slice(string s) => s.AsSpan(_this.Index, _this.Count);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(string? x, string? y) => Slice(x!).Equals(Slice(y!), StringComparison.OrdinalIgnoreCase);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(string s) => Hashing.GetHashCodeOrdinalIgnoreCase(Slice(s));
        }
    }

    internal sealed class RightSubstringCaseInsensitiveComparer : SubstringComparerBase<RightSubstringCaseInsensitiveComparer.GSW>
    {
        internal struct GSW : IGenericSpecializedWrapper
        {
            private RightSubstringCaseInsensitiveComparer _this;
            public void Store(ISubstringComparer @this) => _this = (RightSubstringCaseInsensitiveComparer)@this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<char> Slice(string s) => s.AsSpan(s.Length + _this.Index, _this.Count);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(string? x, string? y) => Slice(x!).Equals(Slice(y!), StringComparison.OrdinalIgnoreCase);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(string s) => Hashing.GetHashCodeOrdinalIgnoreCase(Slice(s));
        }
    }

    internal sealed class FullStringComparer : SubstringComparerBase<FullStringComparer.GSW>
    {
        internal struct GSW : IGenericSpecializedWrapper
        {
            private FullStringComparer _this;
            public void Store(ISubstringComparer @this) => _this = (FullStringComparer)@this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<char> Slice(string s) => s.AsSpan();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(string? x, string? y) => Slice(x!).Equals(Slice(y!), StringComparison.OrdinalIgnoreCase);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(string s) => Hashing.GetHashCodeOrdinalIgnoreCase(Slice(s));
        }
    }
}
