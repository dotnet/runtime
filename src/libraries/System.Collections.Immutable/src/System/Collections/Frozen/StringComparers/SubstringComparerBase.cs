// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Frozen
{
    internal abstract class SubstringComparerBase : StringComparerBase
    {
        public int Index;
        public int Count;

        public abstract bool EqualsPartial(string? x, string? y);
    }
}
