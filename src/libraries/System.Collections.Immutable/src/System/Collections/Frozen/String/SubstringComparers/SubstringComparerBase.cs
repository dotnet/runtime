// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen.String.SubstringComparers
{
    internal interface ISubstringComparer : IEqualityComparer<string>
    {
        public int Index { get; set; }   // offset from left side (if positive) or right side (if negative) of the string
        public int Count { get; set; }   // number of characters in the span

        public abstract ReadOnlySpan<char> Slice(string s);
    }

    internal abstract class SubstringComparerBase<TThisWrapper> : ISubstringComparer
    where TThisWrapper : struct, SubstringComparerBase<TThisWrapper>.IGenericSpecializedWrapper
    {
        /// <summary>A wrapper around this that enables access to important members without making virtual calls.</summary>
        private readonly TThisWrapper _this;

        protected SubstringComparerBase()
        {
            _this = default;
            _this.Store(this);
        }

        public int Index { get; set; }
        public int Count { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> Slice(string s) => _this.Slice(s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(string? x, string? y) => _this.Equals(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(string s) => _this.GetHashCode(s);

        /// <summary>Used to enable generic specialization with reference types.</summary>
        /// <remarks>
        /// To avoid each of those incurring virtual dispatch to the derived type, the derived
        /// type hands down a struct wrapper through which all calls are performed.  This base
        /// class uses that generic struct wrapper to specialize and devirtualize.
        /// </remarks>
        internal interface IGenericSpecializedWrapper
        {
            void Store(ISubstringComparer @this);
            public ReadOnlySpan<char> Slice(string s);
            public bool Equals(string? x, string? y);
            public int GetHashCode(string s);
        }
    }
}
