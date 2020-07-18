// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Generic
{
    internal partial class ArraySortHelper<T, TComparer>
    {
        public static ArraySortHelper<T, TComparer> Default { get; } = new ArraySortHelper<T, TComparer>();
    }

    internal partial class ArraySortHelper<TKey, TValue, TComparer>
    {
        public static ArraySortHelper<TKey, TValue, TComparer> Default { get; } = new ArraySortHelper<TKey, TValue, TComparer>();
    }
}
