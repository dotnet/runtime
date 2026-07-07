// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static unsafe partial class TensorPrimitives
    {
        private interface IIndexOfMinMaxOperator<T>
        {
            static abstract T Aggregate(Vector128<T> value);
            static abstract T Aggregate(Vector256<T> value);
            static abstract T Aggregate(Vector512<T> value);
            static abstract bool Compare(T x, T y);
            static abstract Vector128<T> Compare(Vector128<T> x, Vector128<T> y);
            static abstract Vector256<T> Compare(Vector256<T> x, Vector256<T> y);
            static abstract Vector512<T> Compare(Vector512<T> x, Vector512<T> y);
        }

        private static int IndexOfMinMaxCore<T, TOperator>(ReadOnlySpan<T> x)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            if (x.IsEmpty)
            {
                return -1;
            }

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && x.Length >= Vector512<T>.Count)
            {
                return sizeof(T) == 8 ? IndexOfMinMaxVectorized512Size4Plus<T, TOperator, ulong>(x) :
                    sizeof(T) == 4 ? IndexOfMinMaxVectorized512Size4Plus<T, TOperator, uint>(x) :
                    sizeof(T) == 2 ? IndexOfMinMaxVectorized512Size2<T, TOperator>(x) :
                    IndexOfMinMaxVectorized512Size1<T, TOperator>(x);
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && x.Length >= Vector256<T>.Count)
            {
                return sizeof(T) == 8 ? IndexOfMinMaxVectorized256Size4Plus<T, TOperator, ulong>(x) :
                    sizeof(T) == 4 ? IndexOfMinMaxVectorized256Size4Plus<T, TOperator, uint>(x) :
                    sizeof(T) == 2 ? IndexOfMinMaxVectorized256Size2<T, TOperator>(x) :
                    IndexOfMinMaxVectorized256Size1<T, TOperator>(x);
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && x.Length >= Vector128<T>.Count)
            {
                return sizeof(T) == 8 ? IndexOfMinMaxVectorized128Size4Plus<T, TOperator, ulong>(x) :
                    sizeof(T) == 4 ? IndexOfMinMaxVectorized128Size4Plus<T, TOperator, uint>(x) :
                    sizeof(T) == 2 ? IndexOfMinMaxVectorized128Size2<T, TOperator>(x) :
                    IndexOfMinMaxVectorized128Size1<T, TOperator>(x);
            }

            return IndexOfMinMaxFallback<T, TOperator>(x);
        }

        private static int IndexOfMinMaxFallback<T, TOperator>(ReadOnlySpan<T> x)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            T result = x[0];
            int resultIndex = 0;
            if (T.IsNaN(result))
            {
                return resultIndex;
            }

            for (int i = 1; i < x.Length; i++)
            {
                T current = x[i];
                if (T.IsNaN(current))
                {
                    return i;
                }
                if (TOperator.Compare(current, result))
                {
                    result = current;
                    resultIndex = i;
                }
            }

            return resultIndex;
        }

        private static int IndexOfMinMaxVectorized128Size4Plus<T, TOperator, TInt>(ReadOnlySpan<T> x)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T> where TInt : IBinaryInteger<TInt>
        {
            Debug.Assert(sizeof(T) == 4 || sizeof(T) == 8);
            Debug.Assert(typeof(TInt) == typeof(uint) || typeof(TInt) == typeof(ulong));
            Debug.Assert(sizeof(TInt) == sizeof(T));

            // Initialize result by reading first vector and quick return if possible.
            Vector128<T> result = Vector128.Create(x);
            if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
            {
                Vector128<T> nanMask = IsNaN(result);
                if (nanMask != Vector128<T>.Zero)
                {
                    return IndexOfFirstMatch(nanMask);
                }
            }

            // Initialize indices.
            Vector128<TInt> indexIncrement = Vector128.Create(TInt.CreateTruncating(Vector128<TInt>.Count));
            Vector128<TInt> resultIndex = Vector128<TInt>.Indices;
            Vector128<TInt> currentIndex = resultIndex + indexIncrement;
            ReadOnlySpan<T> span = x.Slice(Vector128<T>.Count);

            while (!span.IsEmpty)
            {
                Vector128<T> current;
                if (span.Length >= Vector128<T>.Count)
                {
                    current = Vector128.Create(span);
                    span = span.Slice(Vector128<T>.Count);
                }
                else
                {
                    // Process a final back-shifted to cover remaining elements in x in one vector.
                    int start = x.Length - Vector128<T>.Count;
                    current = Vector128.Create(x.Slice(start));
                    currentIndex = Vector128.Create(TInt.CreateTruncating(start)) + Vector128<TInt>.Indices;
                    span = ReadOnlySpan<T>.Empty;
                }

                // Quick return if possible.
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector128<T> nanMask = IsNaN(current);
                    if (nanMask != Vector128<T>.Zero)
                    {
                        return int.CreateTruncating(currentIndex.ToScalar()) + IndexOfFirstMatch(nanMask);
                    }
                }

                // Get mask for which lanes that should have result updated.
                Vector128<T> mask = TOperator.Compare(current, result);

                // Update result and indices.
                result = ElementWiseSelect(mask, current, result);
                resultIndex = ElementWiseSelect(mask.As<T, TInt>(), currentIndex, resultIndex);
                currentIndex += indexIncrement;
            }

            {
                // Where result does not bitwise-equal the aggregate min/max value; replace indices with uint.MaxValue. Then find the min index.
                T aggResult = TOperator.Aggregate(result);
                Vector128<TInt> aggMask = ~Vector128.Equals(result.As<T, TInt>(), Vector128.Create(aggResult).As<T, TInt>());
                Vector128<TInt> aggIndex = resultIndex | aggMask;
                return int.CreateTruncating(HorizontalAggregate<TInt, MinOperator<TInt>>(aggIndex));
            }
        }

        private static int IndexOfMinMaxVectorized128Size2<T, TOperator>(ReadOnlySpan<T> x)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            Debug.Assert(sizeof(T) == 2);

            // Initialize result by reading first vector and quick return if possible.
            Vector128<T> result = Vector128.Create(x);
            if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
            {
                Vector128<T> nanMask = IsNaN(result);
                if (nanMask != Vector128<T>.Zero)
                {
                    return IndexOfFirstMatch(nanMask);
                }
            }

            // Initialize indices.
            Vector128<uint> indexIncrement = Vector128.Create((uint)Vector128<uint>.Count);
            Vector128<uint> resultIndex1 = Vector128<uint>.Indices;
            Vector128<uint> resultIndex2 = resultIndex1 + indexIncrement;
            Vector128<uint> currentIndex = resultIndex2 + indexIncrement;
            ReadOnlySpan<T> span = x.Slice(Vector128<T>.Count);

            while (!span.IsEmpty)
            {
                Vector128<T> current;
                if (span.Length >= Vector128<T>.Count)
                {
                    current = Vector128.Create(span);
                    span = span.Slice(Vector128<T>.Count);
                }
                else
                {
                    // Process a final back-shifted to cover remaining elements in x in one vector.
                    int start = x.Length - Vector128<T>.Count;
                    current = Vector128.Create(x.Slice(start));
                    currentIndex = Vector128.Create((uint)start) + Vector128<uint>.Indices;
                    span = ReadOnlySpan<T>.Empty;
                }

                // Quick return if possible.
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector128<T> nanMask = IsNaN(current);
                    if (nanMask != Vector128<T>.Zero)
                    {
                        return (int)currentIndex.ToScalar() + IndexOfFirstMatch(nanMask);
                    }
                }

                // Get mask for which lanes that should have result updated, also widen it for updating the indices.
                Vector128<T> mask = TOperator.Compare(current, result);
                (Vector128<int> mask1, Vector128<int> mask2) = Vector128.Widen(mask.AsInt16());

                // Update result and indices.
                result = ElementWiseSelect(mask, current, result);
                resultIndex1 = ElementWiseSelect(mask1.AsUInt32(), currentIndex, resultIndex1);
                currentIndex += indexIncrement;
                resultIndex2 = ElementWiseSelect(mask2.AsUInt32(), currentIndex, resultIndex2);
                currentIndex += indexIncrement;
            }

            {
                // Where result does not bitwise-equal the aggregate min/max value; replace indices with uint.MaxValue. Then find the min index.
                T aggResult = TOperator.Aggregate(result);
                Vector128<short> aggMask = ~Vector128.Equals(result.AsInt16(), Vector128.Create(aggResult).AsInt16());

                (Vector128<int> mask1, Vector128<int> mask2) = Vector128.Widen(aggMask);
                Vector128<uint> aggIndex = resultIndex1 | mask1.AsUInt32();
                aggIndex = MinOperator<uint>.Invoke(aggIndex, resultIndex2 | mask2.AsUInt32());

                return (int)HorizontalAggregate<uint, MinOperator<uint>>(aggIndex);
            }
        }

        private static int IndexOfMinMaxVectorized128Size1<T, TOperator>(ReadOnlySpan<T> x)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            Debug.Assert(sizeof(T) == 1);

            // Initialize result by reading first vector and quick return if possible.
            Vector128<T> result = Vector128.Create(x);
            if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
            {
                Vector128<T> nanMask = IsNaN(result);
                if (nanMask != Vector128<T>.Zero)
                {
                    return IndexOfFirstMatch(nanMask);
                }
            }

            // Initialize indices.
            Vector128<uint> indexIncrement = Vector128.Create((uint)Vector128<uint>.Count);
            Vector128<uint> resultIndex1 = Vector128<uint>.Indices;
            Vector128<uint> resultIndex2 = resultIndex1 + indexIncrement;
            Vector128<uint> resultIndex3 = resultIndex2 + indexIncrement;
            Vector128<uint> resultIndex4 = resultIndex3 + indexIncrement;
            Vector128<uint> currentIndex = resultIndex4 + indexIncrement;
            ReadOnlySpan<T> span = x.Slice(Vector128<T>.Count);

            while (!span.IsEmpty)
            {
                Vector128<T> current;
                if (span.Length >= Vector128<T>.Count)
                {
                    current = Vector128.Create(span);
                    span = span.Slice(Vector128<T>.Count);
                }
                else
                {
                    // Process a final back-shifted to cover remaining elements in x in one vector.
                    int start = x.Length - Vector128<T>.Count;
                    current = Vector128.Create(x.Slice(start));
                    currentIndex = Vector128.Create((uint)start) + Vector128<uint>.Indices;
                    span = ReadOnlySpan<T>.Empty;
                }

                // Quick return if possible.
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector128<T> nanMask = IsNaN(current);
                    if (nanMask != Vector128<T>.Zero)
                    {
                        return (int)currentIndex.ToScalar() + IndexOfFirstMatch(nanMask);
                    }
                }

                // Get mask for which lanes that should have result updated, also widen it for updating the indices.
                Vector128<T> mask = TOperator.Compare(current, result);
                (Vector128<short> lowerMask, Vector128<short> upperMask) = Vector128.Widen(mask.AsSByte());
                (Vector128<int> mask1, Vector128<int> mask2) = Vector128.Widen(lowerMask);
                (Vector128<int> mask3, Vector128<int> mask4) = Vector128.Widen(upperMask);

                // Update result and indices.
                result = ElementWiseSelect(mask, current, result);
                resultIndex1 = ElementWiseSelect(mask1.AsUInt32(), currentIndex, resultIndex1);
                currentIndex += indexIncrement;
                resultIndex2 = ElementWiseSelect(mask2.AsUInt32(), currentIndex, resultIndex2);
                currentIndex += indexIncrement;
                resultIndex3 = ElementWiseSelect(mask3.AsUInt32(), currentIndex, resultIndex3);
                currentIndex += indexIncrement;
                resultIndex4 = ElementWiseSelect(mask4.AsUInt32(), currentIndex, resultIndex4);
                currentIndex += indexIncrement;
            }

            {
                // Where result does not bitwise-equal the aggregate min/max value; replace indices with uint.MaxValue. Then find the min index.
                T aggResult = TOperator.Aggregate(result);
                Vector128<sbyte> aggMask = ~Vector128.Equals(result.AsSByte(), Vector128.Create(aggResult).AsSByte());

                (Vector128<short> lowerMask, Vector128<short> upperMask) = Vector128.Widen(aggMask);
                (Vector128<int> mask1, Vector128<int> mask2) = Vector128.Widen(lowerMask);
                (Vector128<int> mask3, Vector128<int> mask4) = Vector128.Widen(upperMask);
                Vector128<uint> aggIndex = resultIndex1 | mask1.AsUInt32();
                aggIndex = MinOperator<uint>.Invoke(aggIndex, resultIndex2 | mask2.AsUInt32());
                aggIndex = MinOperator<uint>.Invoke(aggIndex, resultIndex3 | mask3.AsUInt32());
                aggIndex = MinOperator<uint>.Invoke(aggIndex, resultIndex4 | mask4.AsUInt32());

                return (int)HorizontalAggregate<uint, MinOperator<uint>>(aggIndex);
            }
        }

        private static int IndexOfMinMaxVectorized256Size4Plus<T, TOperator, TInt>(ReadOnlySpan<T> x)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T> where TInt : IBinaryInteger<TInt>
        {
            Debug.Assert(sizeof(T) == 4 || sizeof(T) == 8);
            Debug.Assert(typeof(TInt) == typeof(uint) || typeof(TInt) == typeof(ulong));
            Debug.Assert(sizeof(TInt) == sizeof(T));

            // Initialize result by reading first vector and quick return if possible.
            Vector256<T> result = Vector256.Create(x);
            if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
            {
                Vector256<T> nanMask = IsNaN(result);
                if (nanMask != Vector256<T>.Zero)
                {
                    return IndexOfFirstMatch(nanMask);
                }
            }

            // Initialize indices.
            Vector256<TInt> indexIncrement = Vector256.Create(TInt.CreateTruncating(Vector256<TInt>.Count));
            Vector256<TInt> resultIndex = Vector256<TInt>.Indices;
            Vector256<TInt> currentIndex = resultIndex + indexIncrement;
            ReadOnlySpan<T> span = x.Slice(Vector256<T>.Count);

            while (!span.IsEmpty)
            {
                Vector256<T> current;
                if (span.Length >= Vector256<T>.Count)
                {
                    current = Vector256.Create(span);
                    span = span.Slice(Vector256<T>.Count);
                }
                else
                {
                    // Process a final back-shifted to cover remaining elements in x in one vector.
                    int start = x.Length - Vector256<T>.Count;
                    current = Vector256.Create(x.Slice(start));
                    currentIndex = Vector256.Create(TInt.CreateTruncating(start)) + Vector256<TInt>.Indices;
                    span = ReadOnlySpan<T>.Empty;
                }

                // Quick return if possible.
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector256<T> nanMask = IsNaN(current);
                    if (nanMask != Vector256<T>.Zero)
                    {
                        return int.CreateTruncating(currentIndex.ToScalar()) + IndexOfFirstMatch(nanMask);
                    }
                }

                // Get mask for which lanes that should have result updated.
                Vector256<T> mask = TOperator.Compare(current, result);

                // Update result and indices.
                result = ElementWiseSelect(mask, current, result);
                resultIndex = ElementWiseSelect(mask.As<T, TInt>(), currentIndex, resultIndex);
                currentIndex += indexIncrement;
            }

            {
                // Where result does not bitwise-equal the aggregate min/max value; replace indices with uint.MaxValue. Then find the min index.
                T aggResult = TOperator.Aggregate(result);
                Vector256<TInt> aggMask = ~Vector256.Equals(result.As<T, TInt>(), Vector256.Create(aggResult).As<T, TInt>());
                Vector256<TInt> aggIndex = resultIndex | aggMask;
                return int.CreateTruncating(HorizontalAggregate<TInt, MinOperator<TInt>>(aggIndex));
            }
        }

        private static int IndexOfMinMaxVectorized256Size2<T, TOperator>(ReadOnlySpan<T> x)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            Debug.Assert(sizeof(T) == 2);

            // Initialize result by reading first vector and quick return if possible.
            Vector256<T> result = Vector256.Create(x);
            if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
            {
                Vector256<T> nanMask = IsNaN(result);
                if (nanMask != Vector256<T>.Zero)
                {
                    return IndexOfFirstMatch(nanMask);
                }
            }

            // Initialize indices.
            Vector256<uint> indexIncrement = Vector256.Create((uint)Vector256<uint>.Count);
            Vector256<uint> resultIndex1 = Vector256<uint>.Indices;
            Vector256<uint> resultIndex2 = resultIndex1 + indexIncrement;
            Vector256<uint> currentIndex = resultIndex2 + indexIncrement;
            ReadOnlySpan<T> span = x.Slice(Vector256<T>.Count);

            while (!span.IsEmpty)
            {
                Vector256<T> current;
                if (span.Length >= Vector256<T>.Count)
                {
                    current = Vector256.Create(span);
                    span = span.Slice(Vector256<T>.Count);
                }
                else
                {
                    // Process a final back-shifted to cover remaining elements in x in one vector.
                    int start = x.Length - Vector256<T>.Count;
                    current = Vector256.Create(x.Slice(start));
                    currentIndex = Vector256.Create((uint)start) + Vector256<uint>.Indices;
                    span = ReadOnlySpan<T>.Empty;
                }

                // Quick return if possible.
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector256<T> nanMask = IsNaN(current);
                    if (nanMask != Vector256<T>.Zero)
                    {
                        return (int)currentIndex.ToScalar() + IndexOfFirstMatch(nanMask);
                    }
                }

                // Get mask for which lanes that should have result updated, also widen it for updating the indices.
                Vector256<T> mask = TOperator.Compare(current, result);
                (Vector256<int> mask1, Vector256<int> mask2) = Vector256.Widen(mask.AsInt16());

                // Update result and indices.
                result = ElementWiseSelect(mask, current, result);
                resultIndex1 = ElementWiseSelect(mask1.AsUInt32(), currentIndex, resultIndex1);
                currentIndex += indexIncrement;
                resultIndex2 = ElementWiseSelect(mask2.AsUInt32(), currentIndex, resultIndex2);
                currentIndex += indexIncrement;
            }

            {
                // Where result does not bitwise-equal the aggregate min/max value; replace indices with uint.MaxValue. Then find the min index.
                T aggResult = TOperator.Aggregate(result);
                Vector256<short> aggMask = ~Vector256.Equals(result.AsInt16(), Vector256.Create(aggResult).AsInt16());

                (Vector256<int> mask1, Vector256<int> mask2) = Vector256.Widen(aggMask);
                Vector256<uint> aggIndex = resultIndex1 | mask1.AsUInt32();
                aggIndex = MinOperator<uint>.Invoke(aggIndex, resultIndex2 | mask2.AsUInt32());

                return (int)HorizontalAggregate<uint, MinOperator<uint>>(aggIndex);
            }
        }

        private static int IndexOfMinMaxVectorized256Size1<T, TOperator>(ReadOnlySpan<T> x)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            Debug.Assert(sizeof(T) == 1);

            // Initialize result by reading first vector and quick return if possible.
            Vector256<T> result = Vector256.Create(x);
            if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
            {
                Vector256<T> nanMask = IsNaN(result);
                if (nanMask != Vector256<T>.Zero)
                {
                    return IndexOfFirstMatch(nanMask);
                }
            }

            // Initialize indices.
            Vector256<uint> indexIncrement = Vector256.Create((uint)Vector256<uint>.Count);
            Vector256<uint> resultIndex1 = Vector256<uint>.Indices;
            Vector256<uint> resultIndex2 = resultIndex1 + indexIncrement;
            Vector256<uint> resultIndex3 = resultIndex2 + indexIncrement;
            Vector256<uint> resultIndex4 = resultIndex3 + indexIncrement;
            Vector256<uint> currentIndex = resultIndex4 + indexIncrement;
            ReadOnlySpan<T> span = x.Slice(Vector256<T>.Count);

            while (!span.IsEmpty)
            {
                Vector256<T> current;
                if (span.Length >= Vector256<T>.Count)
                {
                    current = Vector256.Create(span);
                    span = span.Slice(Vector256<T>.Count);
                }
                else
                {
                    // Process a final back-shifted to cover remaining elements in x in one vector.
                    int start = x.Length - Vector256<T>.Count;
                    current = Vector256.Create(x.Slice(start));
                    currentIndex = Vector256.Create((uint)start) + Vector256<uint>.Indices;
                    span = ReadOnlySpan<T>.Empty;
                }

                // Quick return if possible.
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector256<T> nanMask = IsNaN(current);
                    if (nanMask != Vector256<T>.Zero)
                    {
                        return (int)currentIndex.ToScalar() + IndexOfFirstMatch(nanMask);
                    }
                }

                // Get mask for which lanes that should have result updated, also widen it for updating the indices.
                Vector256<T> mask = TOperator.Compare(current, result);
                (Vector256<short> lowerMask, Vector256<short> upperMask) = Vector256.Widen(mask.AsSByte());
                (Vector256<int> mask1, Vector256<int> mask2) = Vector256.Widen(lowerMask);
                (Vector256<int> mask3, Vector256<int> mask4) = Vector256.Widen(upperMask);

                // Update result and indices.
                result = ElementWiseSelect(mask, current, result);
                resultIndex1 = ElementWiseSelect(mask1.AsUInt32(), currentIndex, resultIndex1);
                currentIndex += indexIncrement;
                resultIndex2 = ElementWiseSelect(mask2.AsUInt32(), currentIndex, resultIndex2);
                currentIndex += indexIncrement;
                resultIndex3 = ElementWiseSelect(mask3.AsUInt32(), currentIndex, resultIndex3);
                currentIndex += indexIncrement;
                resultIndex4 = ElementWiseSelect(mask4.AsUInt32(), currentIndex, resultIndex4);
                currentIndex += indexIncrement;
            }

            {
                // Where result does not bitwise-equal the aggregate min/max value; replace indices with uint.MaxValue. Then find the min index.
                T aggResult = TOperator.Aggregate(result);
                Vector256<sbyte> aggMask = ~Vector256.Equals(result.AsSByte(), Vector256.Create(aggResult).AsSByte());

                (Vector256<short> lowerMask, Vector256<short> upperMask) = Vector256.Widen(aggMask);
                (Vector256<int> mask1, Vector256<int> mask2) = Vector256.Widen(lowerMask);
                (Vector256<int> mask3, Vector256<int> mask4) = Vector256.Widen(upperMask);
                Vector256<uint> aggIndex = resultIndex1 | mask1.AsUInt32();
                aggIndex = MinOperator<uint>.Invoke(aggIndex, resultIndex2 | mask2.AsUInt32());
                aggIndex = MinOperator<uint>.Invoke(aggIndex, resultIndex3 | mask3.AsUInt32());
                aggIndex = MinOperator<uint>.Invoke(aggIndex, resultIndex4 | mask4.AsUInt32());

                return (int)HorizontalAggregate<uint, MinOperator<uint>>(aggIndex);
            }
        }

        private static int IndexOfMinMaxVectorized512Size4Plus<T, TOperator, TInt>(ReadOnlySpan<T> x)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T> where TInt : IBinaryInteger<TInt>
        {
            Debug.Assert(sizeof(T) == 4 || sizeof(T) == 8);
            Debug.Assert(typeof(TInt) == typeof(uint) || typeof(TInt) == typeof(ulong));
            Debug.Assert(sizeof(TInt) == sizeof(T));

            // Initialize result by reading first vector and quick return if possible.
            Vector512<T> result = Vector512.Create(x);
            if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
            {
                Vector512<T> nanMask = IsNaN(result);
                if (nanMask != Vector512<T>.Zero)
                {
                    return IndexOfFirstMatch(nanMask);
                }
            }

            // Initialize indices.
            Vector512<TInt> indexIncrement = Vector512.Create(TInt.CreateTruncating(Vector512<TInt>.Count));
            Vector512<TInt> resultIndex = Vector512<TInt>.Indices;
            Vector512<TInt> currentIndex = resultIndex + indexIncrement;
            ReadOnlySpan<T> span = x.Slice(Vector512<T>.Count);

            while (!span.IsEmpty)
            {
                Vector512<T> current;
                if (span.Length >= Vector512<T>.Count)
                {
                    current = Vector512.Create(span);
                    span = span.Slice(Vector512<T>.Count);
                }
                else
                {
                    // Process a final back-shifted to cover remaining elements in x in one vector.
                    int start = x.Length - Vector512<T>.Count;
                    current = Vector512.Create(x.Slice(start));
                    currentIndex = Vector512.Create(TInt.CreateTruncating(start)) + Vector512<TInt>.Indices;
                    span = ReadOnlySpan<T>.Empty;
                }

                // Quick return if possible.
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector512<T> nanMask = IsNaN(current);
                    if (nanMask != Vector512<T>.Zero)
                    {
                        return int.CreateTruncating(currentIndex.ToScalar()) + IndexOfFirstMatch(nanMask);
                    }
                }

                // Get mask for which lanes that should have result updated.
                Vector512<T> mask = TOperator.Compare(current, result);

                // Update result and indices.
                result = ElementWiseSelect(mask, current, result);
                resultIndex = ElementWiseSelect(mask.As<T, TInt>(), currentIndex, resultIndex);
                currentIndex += indexIncrement;
            }

            {
                // Where result does not bitwise-equal the aggregate min/max value; replace indices with uint.MaxValue. Then find the min index.
                T aggResult = TOperator.Aggregate(result);
                Vector512<TInt> aggMask = ~Vector512.Equals(result.As<T, TInt>(), Vector512.Create(aggResult).As<T, TInt>());
                Vector512<TInt> aggIndex = resultIndex | aggMask;
                return int.CreateTruncating(HorizontalAggregate<TInt, MinOperator<TInt>>(aggIndex));
            }
        }

        private static int IndexOfMinMaxVectorized512Size2<T, TOperator>(ReadOnlySpan<T> x)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            Debug.Assert(sizeof(T) == 2);

            // Initialize result by reading first vector and quick return if possible.
            Vector512<T> result = Vector512.Create(x);
            if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
            {
                Vector512<T> nanMask = IsNaN(result);
                if (nanMask != Vector512<T>.Zero)
                {
                    return IndexOfFirstMatch(nanMask);
                }
            }

            // Initialize indices.
            Vector512<uint> indexIncrement = Vector512.Create((uint)Vector512<uint>.Count);
            Vector512<uint> resultIndex1 = Vector512<uint>.Indices;
            Vector512<uint> resultIndex2 = resultIndex1 + indexIncrement;
            Vector512<uint> currentIndex = resultIndex2 + indexIncrement;
            ReadOnlySpan<T> span = x.Slice(Vector512<T>.Count);

            while (!span.IsEmpty)
            {
                Vector512<T> current;
                if (span.Length >= Vector512<T>.Count)
                {
                    current = Vector512.Create(span);
                    span = span.Slice(Vector512<T>.Count);
                }
                else
                {
                    // Process a final back-shifted to cover remaining elements in x in one vector.
                    int start = x.Length - Vector512<T>.Count;
                    current = Vector512.Create(x.Slice(start));
                    currentIndex = Vector512.Create((uint)start) + Vector512<uint>.Indices;
                    span = ReadOnlySpan<T>.Empty;
                }

                // Quick return if possible.
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector512<T> nanMask = IsNaN(current);
                    if (nanMask != Vector512<T>.Zero)
                    {
                        return (int)currentIndex.ToScalar() + IndexOfFirstMatch(nanMask);
                    }
                }

                // Get mask for which lanes that should have result updated, also widen it for updating the indices.
                Vector512<T> mask = TOperator.Compare(current, result);
                (Vector512<int> mask1, Vector512<int> mask2) = Vector512.Widen(mask.AsInt16());

                // Update result and indices.
                result = ElementWiseSelect(mask, current, result);
                resultIndex1 = ElementWiseSelect(mask1.AsUInt32(), currentIndex, resultIndex1);
                currentIndex += indexIncrement;
                resultIndex2 = ElementWiseSelect(mask2.AsUInt32(), currentIndex, resultIndex2);
                currentIndex += indexIncrement;
            }

            {
                // Where result does not bitwise-equal the aggregate min/max value; replace indices with uint.MaxValue. Then find the min index.
                T aggResult = TOperator.Aggregate(result);
                Vector512<short> aggMask = ~Vector512.Equals(result.AsInt16(), Vector512.Create(aggResult).AsInt16());

                (Vector512<int> mask1, Vector512<int> mask2) = Vector512.Widen(aggMask);
                Vector512<uint> aggIndex = resultIndex1 | mask1.AsUInt32();
                aggIndex = MinOperator<uint>.Invoke(aggIndex, resultIndex2 | mask2.AsUInt32());

                return (int)HorizontalAggregate<uint, MinOperator<uint>>(aggIndex);
            }
        }

        private static int IndexOfMinMaxVectorized512Size1<T, TOperator>(ReadOnlySpan<T> x)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            Debug.Assert(sizeof(T) == 1);

            // Initialize result by reading first vector and quick return if possible.
            Vector512<T> result = Vector512.Create(x);
            if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
            {
                Vector512<T> nanMask = IsNaN(result);
                if (nanMask != Vector512<T>.Zero)
                {
                    return IndexOfFirstMatch(nanMask);
                }
            }

            // Initialize indices.
            Vector512<uint> indexIncrement = Vector512.Create((uint)Vector512<uint>.Count);
            Vector512<uint> resultIndex1 = Vector512<uint>.Indices;
            Vector512<uint> resultIndex2 = resultIndex1 + indexIncrement;
            Vector512<uint> resultIndex3 = resultIndex2 + indexIncrement;
            Vector512<uint> resultIndex4 = resultIndex3 + indexIncrement;
            Vector512<uint> currentIndex = resultIndex4 + indexIncrement;
            ReadOnlySpan<T> span = x.Slice(Vector512<T>.Count);

            while (!span.IsEmpty)
            {
                Vector512<T> current;
                if (span.Length >= Vector512<T>.Count)
                {
                    current = Vector512.Create(span);
                    span = span.Slice(Vector512<T>.Count);
                }
                else
                {
                    // Process a final back-shifted to cover remaining elements in x in one vector.
                    int start = x.Length - Vector512<T>.Count;
                    current = Vector512.Create(x.Slice(start));
                    currentIndex = Vector512.Create((uint)start) + Vector512<uint>.Indices;
                    span = ReadOnlySpan<T>.Empty;
                }

                // Quick return if possible.
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    Vector512<T> nanMask = IsNaN(current);
                    if (nanMask != Vector512<T>.Zero)
                    {
                        return (int)currentIndex.ToScalar() + IndexOfFirstMatch(nanMask);
                    }
                }

                // Get mask for which lanes that should have result updated, also widen it for updating the indices.
                Vector512<T> mask = TOperator.Compare(current, result);
                (Vector512<short> lowerMask, Vector512<short> upperMask) = Vector512.Widen(mask.AsSByte());
                (Vector512<int> mask1, Vector512<int> mask2) = Vector512.Widen(lowerMask);
                (Vector512<int> mask3, Vector512<int> mask4) = Vector512.Widen(upperMask);

                // Update result and indices.
                result = ElementWiseSelect(mask, current, result);
                resultIndex1 = ElementWiseSelect(mask1.AsUInt32(), currentIndex, resultIndex1);
                currentIndex += indexIncrement;
                resultIndex2 = ElementWiseSelect(mask2.AsUInt32(), currentIndex, resultIndex2);
                currentIndex += indexIncrement;
                resultIndex3 = ElementWiseSelect(mask3.AsUInt32(), currentIndex, resultIndex3);
                currentIndex += indexIncrement;
                resultIndex4 = ElementWiseSelect(mask4.AsUInt32(), currentIndex, resultIndex4);
                currentIndex += indexIncrement;
            }

            {
                // Where result does not bitwise-equal the aggregate min/max value; replace indices with uint.MaxValue. Then find the min index.
                T aggResult = TOperator.Aggregate(result);
                Vector512<sbyte> aggMask = ~Vector512.Equals(result.AsSByte(), Vector512.Create(aggResult).AsSByte());

                (Vector512<short> lowerMask, Vector512<short> upperMask) = Vector512.Widen(aggMask);
                (Vector512<int> mask1, Vector512<int> mask2) = Vector512.Widen(lowerMask);
                (Vector512<int> mask3, Vector512<int> mask4) = Vector512.Widen(upperMask);
                Vector512<uint> aggIndex = resultIndex1 | mask1.AsUInt32();
                aggIndex = MinOperator<uint>.Invoke(aggIndex, resultIndex2 | mask2.AsUInt32());
                aggIndex = MinOperator<uint>.Invoke(aggIndex, resultIndex3 | mask3.AsUInt32());
                aggIndex = MinOperator<uint>.Invoke(aggIndex, resultIndex4 | mask4.AsUInt32());

                return (int)HorizontalAggregate<uint, MinOperator<uint>>(aggIndex);
            }
        }
    }
}
