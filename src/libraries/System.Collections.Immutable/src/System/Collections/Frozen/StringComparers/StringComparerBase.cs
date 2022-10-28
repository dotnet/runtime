// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Immutable
{
    // We define this rather than using IEqualityComparer<string>, since virtual dispatch is faster than interface dispatch
    internal abstract class StringComparerBase : IEqualityComparer<string>
    {
        public int MinLength;
        public int MaxLength;

        public abstract bool Equals(string? x, string? y);
        public abstract int GetHashCode(string s);
        public virtual bool CaseInsensitive => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrivialReject(string s) => s.Length < MinLength || s.Length > MaxLength;
    }
}
