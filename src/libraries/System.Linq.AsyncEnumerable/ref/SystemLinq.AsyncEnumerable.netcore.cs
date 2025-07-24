// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        public static System.Collections.Generic.IAsyncEnumerable<T> InfiniteSequence<T>(T start, T step) where T : System.Numerics.IAdditionOperators<T, T, T> { throw null; }
        public static System.Collections.Generic.IAsyncEnumerable<T> Sequence<T>(T start, T endInclusive, T step) where T : System.Numerics.INumber<T> { throw null; }
    }
}
