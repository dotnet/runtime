// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private interface IMinMaxCalc<T> where T : struct, IBinaryInteger<T>
        {
            public static abstract bool Compare(T left, T right);
            public static abstract Vector128<T> Compare(Vector128<T> left, Vector128<T> right);
            public static abstract Vector256<T> Compare(Vector256<T> left, Vector256<T> right);
            public static abstract Vector512<T> Compare(Vector512<T> left, Vector512<T> right);
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
                if (span.IsEmpty)
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                if (!Vector128.IsHardwareAccelerated || !Vector128<T>.IsSupported || span.Length < Vector128<T>.Count)
                {
                    value = span[0];
                    for (int i = 1; i < span.Length; i++)
                    {
                        if (TMinMax.Compare(span[i], value))
                        {
                            value = span[i];
                        }
                    }
                    return value;
                }

                // All vectorized paths reduce to 128-bit, so we can use that as our accumulator
                // regardless of the maximum supported vector size.
                Vector128<T> best128;

                if (!Vector256.IsHardwareAccelerated || span.Length < Vector256<T>.Count)
                {
                    ReadOnlySpan<T> data = span;
                    Vector128<T> best = Vector128.Create(data);
                    data = data.Slice(Vector128<T>.Count);

                    while (data.Length > Vector128<T>.Count)
                    {
                        best = TMinMax.Compare(best, Vector128.Create(data));
                        data = data.Slice(Vector128<T>.Count);
                    }
                    best128 = TMinMax.Compare(best, Vector128.Create(span.Slice(span.Length - Vector128<T>.Count)));
                }
                else if (!Vector512.IsHardwareAccelerated || span.Length < Vector512<T>.Count)
                {
                    ReadOnlySpan<T> data = span;
                    Vector256<T> best = Vector256.Create(data);
                    data = data.Slice(Vector256<T>.Count);

                    while (data.Length > Vector256<T>.Count)
                    {
                        best = TMinMax.Compare(best, Vector256.Create(data));
                        data = data.Slice(Vector256<T>.Count);
                    }
                    best = TMinMax.Compare(best, Vector256.Create(span.Slice(span.Length - Vector256<T>.Count)));

                    // Reduce to 128-bit
                    best128 = TMinMax.Compare(best.GetLower(), best.GetUpper());
                }
                else
                {
                    ReadOnlySpan<T> data = span;
                    Vector512<T> best = Vector512.Create(data);
                    data = data.Slice(Vector512<T>.Count);

                    while (data.Length > Vector512<T>.Count)
                    {
                        best = TMinMax.Compare(best, Vector512.Create(data));
                        data = data.Slice(Vector512<T>.Count);
                    }
                    best = TMinMax.Compare(best, Vector512.Create(span.Slice(span.Length - Vector512<T>.Count)));

                    // Reduce to 128-bit
                    Vector256<T> best256 = TMinMax.Compare(best.GetLower(), best.GetUpper());
                    best128 = TMinMax.Compare(best256.GetLower(), best256.GetUpper());
                }

                // Reduce to single value
                // NOTE: this can be optimized further with shuffles.
                value = best128[0];
                for (int i = 1; i < Vector128<T>.Count; i++)
                {
                    if (TMinMax.Compare(best128[i], value))
                    {
                        value = best128[i];
                    }
                }
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
