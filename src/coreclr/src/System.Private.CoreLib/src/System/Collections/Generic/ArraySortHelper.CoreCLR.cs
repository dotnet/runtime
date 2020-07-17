// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic
{
    internal interface IComparerArraySortHelper<TKey, TComparer>
        where TComparer : IComparer<TKey>
    {
        void Sort(Span<TKey> keys, TComparer comparer);
        int BinarySearch(TKey[] keys, int index, int length, TKey value, TComparer comparer);
    }

    internal interface IArraySortHelper<TKey>
    {
        void Sort<TComparer>(Span<TKey> keys, TComparer comparer) where TComparer : IComparer<TKey>;
        int BinarySearch<TComparer>(TKey[] keys, int index, int length, TKey value, TComparer comparer) where TComparer : IComparer<TKey>;
    }

    [TypeDependency("System.Collections.Generic.GenericArraySortHelper`1")]
    internal partial class ComparerArraySortHelper<T, TComparer>
        : IComparerArraySortHelper<T, TComparer>
    {
        private static readonly IComparerArraySortHelper<T, TComparer> s_defaultArraySortHelper = CreateArraySortHelper();

        public static IComparerArraySortHelper<T, TComparer> Default => s_defaultArraySortHelper;

        [DynamicDependency("#ctor", typeof(GenericArraySortHelper<>))]
        private static IComparerArraySortHelper<T, TComparer> CreateArraySortHelper()
        {
            if (typeof(IComparable<T>).IsAssignableFrom(typeof(T)))
            {
                return (IArraySortHelper<T>)RuntimeTypeHandle.Allocate(typeof(GenericArraySortHelper<string>).TypeHandle.Instantiate(new Type[] { typeof(T) }));
            }
            else
            {
                return new ComparerArraySortHelper<T, TComparer>();
            }
        }
    }

    [TypeDependency("System.Collections.Generic.GenericArraySortHelper`1")]
    internal partial class ArraySortHelper<T>
    {
        private static readonly IArraySortHelper<T> s_defaultArraySortHelper = CreateArraySortHelper();

        public static IArraySortHelper<T> Default => s_defaultArraySortHelper;

        [DynamicDependency("#ctor", typeof(GenericArraySortHelper<>))]
        private static IArraySortHelper<T> CreateArraySortHelper()
        {
            if (typeof(IComparable<T>).IsAssignableFrom(typeof(T)))
            {
                return (IArraySortHelper<T>)RuntimeTypeHandle.Allocate(typeof(GenericArraySortHelper<string>).TypeHandle.Instantiate(new Type[] { typeof(T) }));
            }
            else
            {
                return new ComparerArraySortHelper<T, TComparer>();
            }
        }
    }

    internal partial class GenericArraySortHelper<T>
        : IArraySortHelper<T>
    {
    }

    //internal interface IComparerArraySortHelper<TKey, TValue, TComparer>
    //    where TComparer : IComparer<TKey>
    //{
    //    void Sort(Span<TKey> keys, Span<TValue> values, TComparer comparer);
    //}

    //internal interface IArraySortHelper<TKey, TValue>
    //{
    //    void Sort(Span<TKey> keys, Span<TValue> values);
    //}
    internal interface IArraySortHelper<TKey, TValue>
    {
        void Sort(Span<TKey> keys, Span<TValue> values, IComparer<TKey>? comparer);
    }


    [TypeDependency("System.Collections.Generic.GenericArraySortHelper`2")]
    internal partial class ArraySortHelper<TKey, TValue>
        : IArraySortHelper<TKey, TValue>
    {
        private static readonly IArraySortHelper<TKey, TValue> s_defaultArraySortHelper = CreateArraySortHelper();

        public static IArraySortHelper<TKey, TValue> Default => s_defaultArraySortHelper;

        [DynamicDependency("#ctor", typeof(GenericArraySortHelper<,>))]
        private static IArraySortHelper<TKey, TValue> CreateArraySortHelper()
        {
            IArraySortHelper<TKey, TValue> defaultArraySortHelper;

            if (typeof(IComparable<TKey>).IsAssignableFrom(typeof(TKey)))
            {
                defaultArraySortHelper = (IArraySortHelper<TKey, TValue>)RuntimeTypeHandle.Allocate(typeof(GenericArraySortHelper<string, string>).TypeHandle.Instantiate(new Type[] { typeof(TKey), typeof(TValue) }));
            }
            else
            {
                defaultArraySortHelper = new ArraySortHelper<TKey, TValue>();
            }
            return defaultArraySortHelper;
        }
    }

    internal partial class GenericArraySortHelper<TKey, TValue>
        : IArraySortHelper<TKey, TValue>
    {
    }
}
