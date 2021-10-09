// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic
{
    internal interface IArraySortHelper<TKey>
    {
        void Sort(Span<TKey> keys, IComparer<TKey>? comparer);
        int BinarySearch(TKey[] keys, int index, int length, TKey value, IComparer<TKey>? comparer);
    }

    [TypeDependency("System.Collections.Generic.GenericArraySortHelper`1")]
    internal sealed partial class ArraySortHelper<T>
        : IArraySortHelper<T>
    {
        private static readonly IArraySortHelper<T> s_defaultArraySortHelper = CreateArraySortHelper();

        public static IArraySortHelper<T> Default => s_defaultArraySortHelper;

        [DynamicDependency("#ctor", typeof(GenericArraySortHelper<>))]
        private static IArraySortHelper<T> CreateArraySortHelper()
        {
            IArraySortHelper<T> defaultArraySortHelper;

            if (typeof(IComparable<T>).IsAssignableFrom(typeof(T)))
            {
                defaultArraySortHelper = (IArraySortHelper<T>)RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(GenericArraySortHelper<string>), (RuntimeType)typeof(T));
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

    internal interface IArraySortHelper<TKey, TValue>
    {
        void Sort(Span<TKey> keys, Span<TValue> values, IComparer<TKey>? comparer);
    }

    [TypeDependency("System.Collections.Generic.GenericArraySortHelper`2")]
    internal sealed partial class ArraySortHelper<TKey, TValue>
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
                defaultArraySortHelper = (IArraySortHelper<TKey, TValue>)RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(GenericArraySortHelper<string, string>), (RuntimeType)typeof(TKey), (RuntimeType)typeof(TValue));
            }
            else
            {
                defaultArraySortHelper = new ArraySortHelper<TKey, TValue>();
            }
            return defaultArraySortHelper;
        }
    }

    internal sealed partial class GenericArraySortHelper<TKey, TValue>
        : IArraySortHelper<TKey, TValue>
    {
    }
}
