// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace System.Collections.Generic
{
    internal partial class ArraySortHelper<T>
    {
        private static readonly ArraySortHelper<T> s_defaultArraySortHelper = new ArraySortHelper<T>();

        public static ArraySortHelper<T> Default
        {
            get
            {
                return s_defaultArraySortHelper;
            }
        }
    }

    internal partial class ArraySortHelper<TKey, TValue>
    {
        private static readonly ArraySortHelper<TKey, TValue> s_defaultArraySortHelper = new ArraySortHelper<TKey, TValue>();

        public static ArraySortHelper<TKey, TValue> Default
        {
            get
            {
                return s_defaultArraySortHelper;
            }
        }
    }
}
