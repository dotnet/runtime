// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic
{
    internal interface IArraySortHelper<TKey, TComparer>
        where TComparer : IComparer<TKey>?
    {
        void Sort(Span<TKey> keys, TComparer comparer);
        int BinarySearch(TKey[] keys, int index, int length, TKey value, TComparer comparer);
    }

    [TypeDependency("System.Collections.Generic.GenericArraySortHelper`2")]
    internal partial class ArraySortHelper<T, TComparer>
        : IArraySortHelper<T, TComparer>
        where TComparer : IComparer<T>?
    {
        private static readonly IArraySortHelper<T, TComparer> s_defaultArraySortHelper = CreateArraySortHelper();

        public static IArraySortHelper<T, TComparer> Default => s_defaultArraySortHelper;

        [DynamicDependency("#ctor", typeof(GenericArraySortHelper<,>))]
        private static IArraySortHelper<T, TComparer> CreateArraySortHelper()
        {
            if (typeof(IComparable<T>).IsAssignableFrom(typeof(T)))
            {
                return (IArraySortHelper<T, TComparer>)RuntimeTypeHandle.Allocate(
                    typeof(GenericArraySortHelper<string, Comparer<string>>).TypeHandle.Instantiate(new Type[] { typeof(T), typeof(TComparer) }));
            }
            else
            {
                return new ArraySortHelper<T, TComparer>();
            }
        }
    }

    internal partial class GenericArraySortHelper<T, TComparer>
        : IArraySortHelper<T, TComparer>
    {
    }

    internal interface IArraySortHelper<TKey, TValue, TComparer>
        where TComparer : IComparer<TKey>?
    {
        void Sort(Span<TKey> keys, Span<TValue> values, TComparer comparer);
    }

    [TypeDependency("System.Collections.Generic.GenericArraySortHelper`3")]
    internal partial class ArraySortHelper<TKey, TValue, TComparer>
        : IArraySortHelper<TKey, TValue, TComparer>
        where TComparer : IComparer<TKey>?
    {
        private static readonly IArraySortHelper<TKey, TValue, TComparer> s_defaultArraySortHelper = CreateArraySortHelper();

        public static IArraySortHelper<TKey, TValue, TComparer> Default => s_defaultArraySortHelper;

        [DynamicDependency("#ctor", typeof(GenericArraySortHelper<,,>))]
        private static IArraySortHelper<TKey, TValue, TComparer> CreateArraySortHelper()
        {
            IArraySortHelper<TKey, TValue, TComparer> defaultArraySortHelper;

            if (typeof(IComparable<TKey>).IsAssignableFrom(typeof(TKey)))
            {
                defaultArraySortHelper = (IArraySortHelper<TKey, TValue, TComparer>)RuntimeTypeHandle.Allocate(
                    typeof(GenericArraySortHelper<string, string, Comparer<string>>).TypeHandle.Instantiate(new Type[] { typeof(TKey), typeof(TValue) }));
            }
            else
            {
                defaultArraySortHelper = new ArraySortHelper<TKey, TValue, TComparer>();
            }
            return defaultArraySortHelper;
        }
    }

    internal partial class GenericArraySortHelper<TKey, TValue, TComparer>
        : IArraySortHelper<TKey, TValue, TComparer>
    {
    }
}
