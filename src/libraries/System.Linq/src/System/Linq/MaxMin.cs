// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

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
                }
                else if (!Vector256.IsHardwareAccelerated || !Vector256<T>.IsSupported || span.Length < Vector256<T>.Count)
                {
                    ref T current = ref MemoryMarshal.GetReference(span);
                    ref T lastVectorStart = ref Unsafe.Add(ref current, span.Length - Vector128<T>.Count);

                    Vector128<T> best = Vector128.LoadUnsafe(ref current);
                    current = ref Unsafe.Add(ref current, Vector128<T>.Count);

                    while (Unsafe.IsAddressLessThan(ref current, ref lastVectorStart))
                    {
                        best = TMinMax.Compare(best, Vector128.LoadUnsafe(ref current));
                        current = ref Unsafe.Add(ref current, Vector128<T>.Count);
                    }
                    best = TMinMax.Compare(best, Vector128.LoadUnsafe(ref lastVectorStart));

                    value = best[0];
                    for (int i = 1; i < Vector128<T>.Count; i++)
                    {
                        if (TMinMax.Compare(best[i], value))
                        {
                            value = best[i];
                        }
                    }
                }
                else if (!Vector512.IsHardwareAccelerated || !Vector512<T>.IsSupported || span.Length < Vector512<T>.Count)
                {
                    ref T current = ref MemoryMarshal.GetReference(span);
                    ref T lastVectorStart = ref Unsafe.Add(ref current, span.Length - Vector256<T>.Count);

                    Vector256<T> best = Vector256.LoadUnsafe(ref current);
                    current = ref Unsafe.Add(ref current, Vector256<T>.Count);

                    while (Unsafe.IsAddressLessThan(ref current, ref lastVectorStart))
                    {
                        best = TMinMax.Compare(best, Vector256.LoadUnsafe(ref current));
                        current = ref Unsafe.Add(ref current, Vector256<T>.Count);
                    }
                    best = TMinMax.Compare(best, Vector256.LoadUnsafe(ref lastVectorStart));

                    value = best[0];
                    for (int i = 1; i < Vector256<T>.Count; i++)
                    {
                        if (TMinMax.Compare(best[i], value))
                        {
                            value = best[i];
                        }
                    }
                }
                else
                {
                    ref T current = ref MemoryMarshal.GetReference(span);
                    ref T lastVectorStart = ref Unsafe.Add(ref current, span.Length - Vector512<T>.Count);

                    Vector512<T> best = Vector512.LoadUnsafe(ref current);
                    current = ref Unsafe.Add(ref current, Vector512<T>.Count);

                    while (Unsafe.IsAddressLessThan(ref current, ref lastVectorStart))
                    {
                        best = TMinMax.Compare(best, Vector512.LoadUnsafe(ref current));
                        current = ref Unsafe.Add(ref current, Vector512<T>.Count);
                    }
                    best = TMinMax.Compare(best, Vector512.LoadUnsafe(ref lastVectorStart));

                    value = best[0];
                    for (int i = 1; i < Vector512<T>.Count; i++)
                    {
                        if (TMinMax.Compare(best[i], value))
                        {
                            value = best[i];
                        }
                    }
                }
            }
            else
            {
                using (IEnumerator<T> e = source.GetEnumerator())
                {
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
            }

            return value;
        }
    }
}
