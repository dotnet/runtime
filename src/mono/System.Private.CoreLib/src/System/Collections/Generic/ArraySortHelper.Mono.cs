// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    internal interface IArraySortHelper<TKey>
    {
        void SortFallback(Span<TKey> keys);
        int BinarySearchFallback(TKey[] keys, int index, int length, TKey value);
    }

    internal sealed partial class ArraySortHelper<T>
        : IArraySortHelper<T>
    {
        private static readonly IArraySortHelper<T> s_defaultArraySortHelper = CreateArraySortHelper();

        public static IArraySortHelper<T> Default => s_defaultArraySortHelper;

        private static IArraySortHelper<T> CreateArraySortHelper()
        {
            IArraySortHelper<T> defaultArraySortHelper;

            if (typeof(IComparable<T>).IsAssignableFrom(typeof(T)))
            {
                defaultArraySortHelper = (IArraySortHelper<T>)RuntimeType.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(GenericArraySortHelper<>), (RuntimeType)typeof(T));
            }
            else
            {
                defaultArraySortHelper = new ArraySortHelper<T>();
            }
            return defaultArraySortHelper;
        }
    }

    internal sealed partial class GenericArraySortHelper<T>
        : IArraySortHelper<T>
    {
    }

    internal interface IArraySortHelperPaired<TKey, TValue>
    {
        void SortFallBack(Span<TKey> keys, Span<TValue> values);
    }

    internal sealed partial class ArraySortHelperPaired<TKey, TValue>
        : IArraySortHelperPaired<TKey, TValue>
    {
        private static readonly IArraySortHelperPaired<TKey, TValue> s_defaultArraySortHelper = CreateArraySortHelper();

        public static IArraySortHelperPaired<TKey, TValue> Default => s_defaultArraySortHelper;

        private static IArraySortHelperPaired<TKey, TValue> CreateArraySortHelper()
        {
            IArraySortHelperPaired<TKey, TValue> defaultArraySortHelper;

            if (typeof(IComparable<TKey>).IsAssignableFrom(typeof(TKey)))
            {
                defaultArraySortHelper = (IArraySortHelperPaired<TKey, TValue>)RuntimeType.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(GenericArraySortHelperPaired<,>), (RuntimeType)typeof(TKey), (RuntimeType)typeof(TValue));
            }
            else
            {
                defaultArraySortHelper = new ArraySortHelperPaired<TKey, TValue>();
            }
            return defaultArraySortHelper;
        }
    }

    internal sealed partial class GenericArraySortHelperPaired<TKey, TValue>
        : IArraySortHelperPaired<TKey, TValue>
    {
    }
}
