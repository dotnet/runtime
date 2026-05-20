// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private interface IMinMaxCalc<T> where T : struct, IBinaryInteger<T>
        {
            static abstract bool Compare(T left, T right);
            static abstract Vector128<T> Compare(Vector128<T> left, Vector128<T> right);
            static abstract Vector256<T> Compare(Vector256<T> left, Vector256<T> right);
            static abstract Vector512<T> Compare(Vector512<T> left, Vector512<T> right);
            static abstract T MinMax(ReadOnlySpan<T> source);
        }

        private static T MinMaxInteger<T, TMinMax>(this IEnumerable<T> source)
            where T : struct, IBinaryInteger<T>
            where TMinMax : IMinMaxCalc<T>
        {
            T value;

            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source.TryGetSpan(out ReadOnlySpan<T> span))
            {
                value = TMinMax.MinMax(span);
            }
            else
            {
                using IEnumerator<T> e = source.GetEnumerator();
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                value = e.Current;
                while (e.MoveNext())
                {
                    T x = e.Current;
                    if (TMinMax.Compare(x, value))
                    {
                        value = x;
                    }
                }
            }

            return value;
        }
    }
}
