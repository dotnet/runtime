// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
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

    internal partial class ArraySortHelperPaired<TKey, TValue>
    {
        private static readonly ArraySortHelperPaired<TKey, TValue> s_defaultArraySortHelper = new ArraySortHelperPaired<TKey, TValue>();

        public static ArraySortHelperPaired<TKey, TValue> Default
        {
            get
            {
                return s_defaultArraySortHelper;
            }
        }
    }
}
