// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the cosine similarity between the two specified non-empty, equal-length tensors of single-precision floating-point numbers.</summary>
        /// <remarks>Assumes arguments have already been validated to be non-empty and equal length.</remarks>
        private static float CosineSimilarityCore(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            // Compute the same as:
            // TensorPrimitives.Dot(x, y) / (Math.Sqrt(TensorPrimitives.SumOfSquares(x)) * Math.Sqrt(TensorPrimitives.SumOfSquares(y)))
            // but only looping over each span once.

            float dotProduct = 0f;
            float xSumOfSquares = 0f;
            float ySumOfSquares = 0f;

            if (Vector.IsHardwareAccelerated &&
                Vector<float>.Count <= 16 && // currently never greater than 8, but 16 would occur if/when AVX512 is supported, and logic in remainder handling assumes that maximum
                x.Length >= Vector<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);
                ref float yRef = ref MemoryMarshal.GetReference(y);

                Vector<float> dotProductVector = Vector<float>.Zero;
                Vector<float> xSumOfSquaresVector = Vector<float>.Zero;
                Vector<float> ySumOfSquaresVector = Vector<float>.Zero;

                // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                int oneVectorFromEnd = x.Length - Vector<float>.Count;
                int i = 0;
                do
                {
                    Vector<float> xVec = AsVector(ref xRef, i);
                    Vector<float> yVec = AsVector(ref yRef, i);

                    dotProductVector += xVec * yVec;
                    xSumOfSquaresVector += xVec * xVec;
                    ySumOfSquaresVector += yVec * yVec;

                    i += Vector<float>.Count;
                }
                while (i <= oneVectorFromEnd);

                // Process the last vector in the span, masking off elements already processed.
                if (i != x.Length)
                {
                    Vector<float> xVec = AsVector(ref xRef, x.Length - Vector<float>.Count);
                    Vector<float> yVec = AsVector(ref yRef, x.Length - Vector<float>.Count);

                    Vector<float> remainderMask = CreateRemainderMaskSingleVector(x.Length - i);
                    xVec &= remainderMask;
                    yVec &= remainderMask;

                    dotProductVector += xVec * yVec;
                    xSumOfSquaresVector += xVec * xVec;
                    ySumOfSquaresVector += yVec * yVec;
                }

                // Sum the vector lanes into the scalar result.
                for (int e = 0; e < Vector<float>.Count; e++)
                {
                    dotProduct += dotProductVector[e];
                    xSumOfSquares += xSumOfSquaresVector[e];
                    ySumOfSquares += ySumOfSquaresVector[e];
                }
            }
            else
            {
                // Vectorization isn't supported or there are too few elements to vectorize.
                // Use a scalar implementation.
                for (int i = 0; i < x.Length; i++)
                {
                    dotProduct += x[i] * y[i];
                    xSumOfSquares += x[i] * x[i];
                    ySumOfSquares += y[i] * y[i];
                }
            }

            // Sum(X * Y) / (|X| * |Y|)
            return dotProduct / (MathF.Sqrt(xSumOfSquares) * MathF.Sqrt(ySumOfSquares));
        }

        /// <summary>Performs an aggregation over all elements in <paramref name="x"/> to produce a single-precision floating-point value.</summary>
        /// <typeparam name="TTransformOperator">Specifies the transform operation that should be applied to each element loaded from <paramref name="x"/>.</typeparam>
        /// <typeparam name="TAggregationOperator">
        /// Specifies the aggregation binary operation that should be applied to multiple values to aggregate them into a single value.
        /// The aggregation is applied after the transform is applied to each element.
        /// </typeparam>
        private static unsafe float Aggregate<TTransformOperator, TAggregationOperator>(
            ReadOnlySpan<float> x, TTransformOperator transformOp = default, TAggregationOperator aggregationOp = default)
            where TTransformOperator : struct, IUnaryOperator
            where TAggregationOperator : struct, IAggregationOperator
        {
            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref float xRef = ref MemoryMarshal.GetReference(x);

            nuint remainder = (uint)(x.Length);

            if (Vector.IsHardwareAccelerated && transformOp.CanVectorize)
            {
                float result;

                if (remainder >= (uint)(Vector<float>.Count))
                {
                    result = Vectorized(ref xRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    result = VectorizedSmall(ref xRef, remainder);
                }

                return result;
            }

            // This is the software fallback when no acceleration is available
            // It requires no branches to hit

            return SoftwareFallback(ref xRef, remainder);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static float SoftwareFallback(ref float xRef, nuint length, TTransformOperator transformOp = default, TAggregationOperator aggregationOp = default)
            {
                float result = aggregationOp.IdentityValue;

                for (nuint i = 0; i < length; i++)
                {
                    result = aggregationOp.Invoke(result, transformOp.Invoke(Unsafe.Add(ref xRef, (nint)(i))));
                }

                return result;
            }

            static float Vectorized(ref float xRef, nuint remainder, TTransformOperator transformOp = default, TAggregationOperator aggregationOp = default)
            {
                Vector<float> vresult = new Vector<float>(aggregationOp.IdentityValue);

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector<float> beg = transformOp.Invoke(AsVector(ref xRef));
                Vector<float> end = transformOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count)));

                nuint misalignment = 0;

                if (remainder > (uint)(Vector<float>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (float* px = &xRef)
                    {
                        float* xPtr = px;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)(xPtr) % sizeof(float)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            misalignment = ((uint)(sizeof(Vector<float>)) - ((nuint)(xPtr) % (uint)(sizeof(Vector<float>)))) / sizeof(float);

                            xPtr += misalignment;

                            Debug.Assert(((nuint)(xPtr) % (uint)(sizeof(Vector<float>))) == 0);

                            remainder -= misalignment;
                        }

                        Vector<float> vector1;
                        Vector<float> vector2;
                        Vector<float> vector3;
                        Vector<float> vector4;

                        // We only need to load, so there isn't a lot of benefit to doing non-temporal operations

                        while (remainder >= (uint)(Vector<float>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = transformOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 0)));
                            vector2 = transformOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 1)));
                            vector3 = transformOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 2)));
                            vector4 = transformOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 3)));

                            vresult = aggregationOp.Invoke(vresult, vector1);
                            vresult = aggregationOp.Invoke(vresult, vector2);
                            vresult = aggregationOp.Invoke(vresult, vector3);
                            vresult = aggregationOp.Invoke(vresult, vector4);

                            // We load, process, and store the next four vectors

                            vector1 = transformOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 4)));
                            vector2 = transformOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 5)));
                            vector3 = transformOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 6)));
                            vector4 = transformOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 7)));

                            vresult = aggregationOp.Invoke(vresult, vector1);
                            vresult = aggregationOp.Invoke(vresult, vector2);
                            vresult = aggregationOp.Invoke(vresult, vector3);
                            vresult = aggregationOp.Invoke(vresult, vector4);

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector<float>.Count * 8);

                            remainder -= (uint)(Vector<float>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                    }
                }

                // Store the first block. Handling this separately simplifies the latter code as we know
                // they come after and so we can relegate it to full blocks or the trailing elements

                beg = Vector.ConditionalSelect(CreateAlignmentMaskSingleVector((int)(misalignment)), beg, new Vector<float>(aggregationOp.IdentityValue));
                vresult = aggregationOp.Invoke(vresult, beg);

                // Process the remaining [0, Count * 7] elements via a jump table
                //
                // We end up handling any trailing elements in case 0 and in the
                // worst case end up just doing the identity operation here if there
                // were no trailing elements.

                nuint blocks = remainder / (nuint)(Vector<float>.Count);
                nuint trailing = remainder - (blocks * (nuint)(Vector<float>.Count));
                blocks -= (misalignment == 0) ? 1u : 0u;
                remainder -= trailing;

                switch (blocks)
                {
                    case 7:
                    {
                        Vector<float> vector = transformOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 7)));
                        vresult = aggregationOp.Invoke(vresult, vector);
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector<float> vector = transformOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 6)));
                        vresult = aggregationOp.Invoke(vresult, vector);
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector<float> vector = transformOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 5)));
                        vresult = aggregationOp.Invoke(vresult, vector);
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector<float> vector = transformOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 4)));
                        vresult = aggregationOp.Invoke(vresult, vector);
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector<float> vector = transformOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 3)));
                        vresult = aggregationOp.Invoke(vresult, vector);
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector<float> vector = transformOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 2)));
                        vresult = aggregationOp.Invoke(vresult, vector);
                        goto case 1;
                    }

                    case 1:
                    {
                        Vector<float> vector = transformOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 1)));
                        vresult = aggregationOp.Invoke(vresult, vector);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end = Vector.ConditionalSelect(CreateRemainderMaskSingleVector((int)(trailing)), end, new Vector<float>(aggregationOp.IdentityValue));
                        vresult = aggregationOp.Invoke(vresult, end);
                        break;
                    }
                }

                float result = aggregationOp.IdentityValue;

                for (int i = 0; i < Vector<float>.Count; i++)
                {
                    result = aggregationOp.Invoke(result, vresult[i]);
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static float VectorizedSmall(ref float xRef, nuint remainder, TTransformOperator transformOp = default, TAggregationOperator aggregationOp = default)
            {
                float result = aggregationOp.IdentityValue;

                switch (remainder)
                {
                    case 7:
                    {
                        result = aggregationOp.Invoke(result, transformOp.Invoke(Unsafe.Add(ref xRef, 6)));
                        goto case 6;
                    }

                    case 6:
                    {
                        result = aggregationOp.Invoke(result, transformOp.Invoke(Unsafe.Add(ref xRef, 5)));
                        goto case 5;
                    }

                    case 5:
                    {
                        result = aggregationOp.Invoke(result, transformOp.Invoke(Unsafe.Add(ref xRef, 4)));
                        goto case 4;
                    }

                    case 4:
                    {
                        result = aggregationOp.Invoke(result, transformOp.Invoke(Unsafe.Add(ref xRef, 3)));
                        goto case 3;
                    }

                    case 3:
                    {
                        result = aggregationOp.Invoke(result, transformOp.Invoke(Unsafe.Add(ref xRef, 2)));
                        goto case 2;
                    }

                    case 2:
                    {
                        result = aggregationOp.Invoke(result, transformOp.Invoke(Unsafe.Add(ref xRef, 1)));
                        goto case 1;
                    }

                    case 1:
                    {
                        result = aggregationOp.Invoke(result, transformOp.Invoke(xRef));
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }

                return result;
            }
        }

        /// <summary>Performs an aggregation over all pair-wise elements in <paramref name="x"/> and <paramref name="y"/> to produce a single-precision floating-point value.</summary>
        /// <typeparam name="TBinaryOperator">Specifies the binary operation that should be applied to the pair-wise elements loaded from <paramref name="x"/> and <paramref name="y"/>.</typeparam>
        /// <typeparam name="TAggregationOperator">
        /// Specifies the aggregation binary operation that should be applied to multiple values to aggregate them into a single value.
        /// The aggregation is applied to the results of the binary operations on the pair-wise values.
        /// </typeparam>
        private static unsafe float Aggregate<TBinaryOperator, TAggregationOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, TBinaryOperator binaryOp = default, TAggregationOperator aggregationOp = default)
            where TBinaryOperator : struct, IBinaryOperator
            where TAggregationOperator : struct, IAggregationOperator
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float yRef = ref MemoryMarshal.GetReference(y);

            nuint remainder = (uint)(x.Length);

            if (Vector.IsHardwareAccelerated)
            {
                float result;

                if (remainder >= (uint)(Vector<float>.Count))
                {
                    result = Vectorized(ref xRef, ref yRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    result = VectorizedSmall(ref xRef, ref yRef, remainder);
                }

                return result;
            }

            // This is the software fallback when no acceleration is available
            // It requires no branches to hit

            return SoftwareFallback(ref xRef, ref yRef, remainder);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static float SoftwareFallback(ref float xRef, ref float yRef, nuint length, TBinaryOperator binaryOp = default, TAggregationOperator aggregationOp = default)
            {
                float result = aggregationOp.IdentityValue;

                for (nuint i = 0; i < length; i++)
                {
                    result = aggregationOp.Invoke(result, binaryOp.Invoke(Unsafe.Add(ref xRef, (nint)(i)),
                                                                          Unsafe.Add(ref yRef, (nint)(i))));
                }

                return result;
            }

            static float Vectorized(ref float xRef, ref float yRef, nuint remainder, TBinaryOperator binaryOp = default, TAggregationOperator aggregationOp = default)
            {
                Vector<float> vresult = new Vector<float>(aggregationOp.IdentityValue);

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector<float> beg = binaryOp.Invoke(AsVector(ref xRef),
                                                    AsVector(ref yRef));
                Vector<float> end = binaryOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count)),
                                                    AsVector(ref yRef, remainder - (uint)(Vector<float>.Count)));

                nuint misalignment = 0;

                if (remainder > (uint)(Vector<float>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (float* px = &xRef)
                    fixed (float* py = &yRef)
                    {
                        float* xPtr = px;
                        float* yPtr = py;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)(xPtr) % sizeof(float)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            misalignment = ((uint)(sizeof(Vector<float>)) - ((nuint)(xPtr) % (uint)(sizeof(Vector<float>)))) / sizeof(float);

                            xPtr += misalignment;
                            yPtr += misalignment;

                            Debug.Assert(((nuint)(xPtr) % (uint)(sizeof(Vector<float>))) == 0);

                            remainder -= misalignment;
                        }

                        Vector<float> vector1;
                        Vector<float> vector2;
                        Vector<float> vector3;
                        Vector<float> vector4;

                        // We only need to load, so there isn't a lot of benefit to doing non-temporal operations

                        while (remainder >= (uint)(Vector<float>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = binaryOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 0)),
                                                      *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 0)));
                            vector2 = binaryOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 1)),
                                                      *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 1)));
                            vector3 = binaryOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 2)),
                                                      *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 2)));
                            vector4 = binaryOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 3)),
                                                      *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 3)));

                            vresult = aggregationOp.Invoke(vresult, vector1);
                            vresult = aggregationOp.Invoke(vresult, vector2);
                            vresult = aggregationOp.Invoke(vresult, vector3);
                            vresult = aggregationOp.Invoke(vresult, vector4);

                            // We load, process, and store the next four vectors

                            vector1 = binaryOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 4)),
                                                      *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 4)));
                            vector2 = binaryOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 5)),
                                                      *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 5)));
                            vector3 = binaryOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 6)),
                                                      *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 6)));
                            vector4 = binaryOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 7)),
                                                      *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 7)));

                            vresult = aggregationOp.Invoke(vresult, vector1);
                            vresult = aggregationOp.Invoke(vresult, vector2);
                            vresult = aggregationOp.Invoke(vresult, vector3);
                            vresult = aggregationOp.Invoke(vresult, vector4);

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector<float>.Count * 8);
                            yPtr += (uint)(Vector<float>.Count * 8);

                            remainder -= (uint)(Vector<float>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                    }
                }

                // Store the first block. Handling this separately simplifies the latter code as we know
                // they come after and so we can relegate it to full blocks or the trailing elements

                beg = Vector.ConditionalSelect(CreateAlignmentMaskSingleVector((int)(misalignment)), beg, new Vector<float>(aggregationOp.IdentityValue));
                vresult = aggregationOp.Invoke(vresult, beg);

                // Process the remaining [0, Count * 7] elements via a jump table
                //
                // We end up handling any trailing elements in case 0 and in the
                // worst case end up just doing the identity operation here if there
                // were no trailing elements.

                nuint blocks = remainder / (nuint)(Vector<float>.Count);
                nuint trailing = remainder - (blocks * (nuint)(Vector<float>.Count));
                blocks -= (misalignment == 0) ? 1u : 0u;
                remainder -= trailing;

                switch (blocks)
                {
                    case 7:
                    {
                        Vector<float> vector = binaryOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 7)),
                                                               AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 7)));
                        vresult = aggregationOp.Invoke(vresult, vector);
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector<float> vector = binaryOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 6)),
                                                               AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 6)));
                        vresult = aggregationOp.Invoke(vresult, vector);
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector<float> vector = binaryOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 5)),
                                                               AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 5)));
                        vresult = aggregationOp.Invoke(vresult, vector);
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector<float> vector = binaryOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 4)),
                                                               AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 4)));
                        vresult = aggregationOp.Invoke(vresult, vector);
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector<float> vector = binaryOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 3)),
                                                               AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 3)));
                        vresult = aggregationOp.Invoke(vresult, vector);
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector<float> vector = binaryOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 2)),
                                                               AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 2)));
                        vresult = aggregationOp.Invoke(vresult, vector);
                        goto case 1;
                    }

                    case 1:
                    {
                        Vector<float> vector = binaryOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 1)),
                                                               AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 1)));
                        vresult = aggregationOp.Invoke(vresult, vector);
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        end = Vector.ConditionalSelect(CreateRemainderMaskSingleVector((int)(trailing)), end, new Vector<float>(aggregationOp.IdentityValue));
                        vresult = aggregationOp.Invoke(vresult, end);
                        break;
                    }
                }

                float result = aggregationOp.IdentityValue;

                for (int i = 0; i < Vector<float>.Count; i++)
                {
                    result = aggregationOp.Invoke(result, vresult[i]);
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static float VectorizedSmall(ref float xRef, ref float yRef, nuint remainder, TBinaryOperator binaryOp = default, TAggregationOperator aggregationOp = default)
            {
                float result = aggregationOp.IdentityValue;

                switch (remainder)
                {
                    case 7:
                    {
                        result = aggregationOp.Invoke(result, binaryOp.Invoke(Unsafe.Add(ref xRef, 6),
                                                                              Unsafe.Add(ref yRef, 6)));
                        goto case 6;
                    }

                    case 6:
                    {
                        result = aggregationOp.Invoke(result, binaryOp.Invoke(Unsafe.Add(ref xRef, 5),
                                                                              Unsafe.Add(ref yRef, 5)));
                        goto case 5;
                    }

                    case 5:
                    {
                        result = aggregationOp.Invoke(result, binaryOp.Invoke(Unsafe.Add(ref xRef, 4),
                                                                              Unsafe.Add(ref yRef, 4)));
                        goto case 4;
                    }

                    case 4:
                    {
                        result = aggregationOp.Invoke(result, binaryOp.Invoke(Unsafe.Add(ref xRef, 3),
                                                                              Unsafe.Add(ref yRef, 3)));
                        goto case 3;
                    }

                    case 3:
                    {
                        result = aggregationOp.Invoke(result, binaryOp.Invoke(Unsafe.Add(ref xRef, 2),
                                                                              Unsafe.Add(ref yRef, 2)));
                        goto case 2;
                    }

                    case 2:
                    {
                        result = aggregationOp.Invoke(result, binaryOp.Invoke(Unsafe.Add(ref xRef, 1),
                                                                              Unsafe.Add(ref yRef, 1)));
                        goto case 1;
                    }

                    case 1:
                    {
                        result = aggregationOp.Invoke(result, binaryOp.Invoke(xRef, yRef));
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }

                return result;
            }
        }

        /// <remarks>
        /// This is the same as <see cref="Aggregate{TTransformOperator, TAggregationOperator}(ReadOnlySpan{float}, TTransformOperator, TAggregationOperator)"/>
        /// with an identity transform, except it early exits on NaN.
        /// </remarks>
        private static float MinMaxCore<TMinMaxOperator>(ReadOnlySpan<float> x, TMinMaxOperator op = default)
            where TMinMaxOperator : struct, IBinaryOperator
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            // This matches the IEEE 754:2019 `maximum`/`minimum` functions.
            // It propagates NaN inputs back to the caller and
            // otherwise returns the greater of the inputs.
            // It treats +0 as greater than -0 as per the specification.

            float result = x[0];
            int i = 0;

            if (Vector.IsHardwareAccelerated && x.Length >= Vector<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector<float> resultVector = AsVector(ref xRef, 0), current;
                if (Vector.EqualsAll(resultVector, resultVector))
                {
                    int oneVectorFromEnd = x.Length - Vector<float>.Count;
                    i = Vector<float>.Count;

                    // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                    while (i <= oneVectorFromEnd)
                    {
                        // Load the next vector, and early exit on NaN.
                        current = AsVector(ref xRef, i);
                        if (!Vector.EqualsAll(current, current))
                        {
                            goto Scalar;
                        }

                        resultVector = op.Invoke(resultVector, current);
                        i += Vector<float>.Count;
                    }

                    // If any elements remain, handle them in one final vector.
                    if (i != x.Length)
                    {
                        current = AsVector(ref xRef, x.Length - Vector<float>.Count);
                        if (!Vector.EqualsAll(current, current))
                        {
                            goto Scalar;
                        }

                        resultVector = op.Invoke(resultVector, current);
                    }

                    // Aggregate the lanes in the vector to create the final scalar result.
                    for (int f = 0; f < Vector<float>.Count; f++)
                    {
                        result = op.Invoke(result, resultVector[f]);
                    }

                    return result;
                }
            }

            // Scalar path used when either vectorization is not supported, the input is too small to vectorize,
            // or a NaN is encountered.
            Scalar:
            for (; (uint)i < (uint)x.Length; i++)
            {
                float current = x[i];

                if (float.IsNaN(current))
                {
                    return current;
                }

                result = op.Invoke(result, current);
            }

            return result;
        }

        private static readonly int[] s_0through7 = [0, 1, 2, 3, 4, 5, 6, 7];

        private static int IndexOfMinMaxCore<T, TIndexOfMinMaxOperator>(ReadOnlySpan<float> x, TIndexOfMinMaxOperator op = default)
            where TIndexOfMinMaxOperator : struct, IIndexOfOperator
        {
            Debug.Assert(typeof(T) == typeof(float), "The generic parameter exists only to provide the same signature as the generic implementation.");

            if (x.IsEmpty)
            {
                return -1;
            }

            // This matches the IEEE 754:2019 `maximum`/`minimum` functions.
            // It propagates NaN inputs back to the caller and
            // otherwise returns the index of the greater of the inputs.
            // It treats +0 as greater than -0 as per the specification.

            int result;
            int i = 0;

            if (Vector.IsHardwareAccelerated && Vector<int>.Count <= 8 && x.Length >= Vector<float>.Count)
            {
                ref float xRef = ref MemoryMarshal.GetReference(x);

                Vector<int> resultIndex = new Vector<int>(s_0through7);
                Vector<int> currentIndex = resultIndex;
                Vector<int> increment = new Vector<int>(Vector<int>.Count);

                // Load the first vector as the initial set of results, and bail immediately
                // to scalar handling if it contains any NaNs (which don't compare equally to themselves).
                Vector<float> resultVector = AsVector(ref xRef, 0), current;
                if (Vector.EqualsAll(resultVector, resultVector))
                {
                    int oneVectorFromEnd = x.Length - Vector<float>.Count;
                    i = Vector<float>.Count;

                    // Aggregate additional vectors into the result as long as there's at least one full vector left to process.
                    while (i <= oneVectorFromEnd)
                    {
                        // Load the next vector, and early exit on NaN.
                        current = AsVector(ref xRef, i);
                        currentIndex = Vector.Add(currentIndex, increment);

                        if (!Vector.EqualsAll(current, current))
                        {
                            goto Scalar;
                        }

                        op.Invoke(ref resultVector, current, ref resultIndex, currentIndex);
                        i += Vector<float>.Count;
                    }

                    // If any elements remain, handle them in one final vector.
                    if (i != x.Length)
                    {
                        currentIndex = Vector.Add(currentIndex, new Vector<int>(x.Length - i));

                        current = AsVector(ref xRef, x.Length - Vector<float>.Count);
                        if (!Vector.EqualsAll(current, current))
                        {
                            goto Scalar;
                        }

                        op.Invoke(ref resultVector, current, ref resultIndex, currentIndex);
                    }

                    result = op.Invoke(resultVector, resultIndex);

                    return result;
                }
            }

        // Scalar path used when either vectorization is not supported, the input is too small to vectorize,
        // or a NaN is encountered.
        Scalar:
            float curResult = x[i];
            int curIn = i;
            if (float.IsNaN(curResult))
            {
                return curIn;
            }

            for (; i < x.Length; i++)
            {
                float current = x[i];
                if (float.IsNaN(current))
                {
                    return i;
                }

                curIn = op.Invoke(ref curResult, current, curIn, i);
            }

            return curIn;
        }

        /// <summary>Performs an element-wise operation on <paramref name="x"/> and writes the results to <paramref name="destination"/>.</summary>
        /// <typeparam name="TUnaryOperator">Specifies the operation to perform on each element loaded from <paramref name="x"/>.</typeparam>
        private static unsafe void InvokeSpanIntoSpan<TUnaryOperator>(
            ReadOnlySpan<float> x, Span<float> destination, TUnaryOperator op = default)
            where TUnaryOperator : struct, IUnaryOperator
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);

            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)(x.Length);

            if (Vector.IsHardwareAccelerated && op.CanVectorize)
            {
                if (remainder >= (uint)(Vector<float>.Count))
                {
                    Vectorized(ref xRef, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref dRef, remainder);
                }

                return;
            }

            // This is the software fallback when no acceleration is available
            // It requires no branches to hit

            SoftwareFallback(ref xRef, ref dRef, remainder);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void SoftwareFallback(ref float xRef, ref float dRef, nuint length, TUnaryOperator op = default)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, (nint)(i)) = op.Invoke(Unsafe.Add(ref xRef, (nint)(i)));
                }
            }

            static void Vectorized(ref float xRef, ref float dRef, nuint remainder, TUnaryOperator op = default)
            {
                ref float dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector<float> beg = op.Invoke(AsVector(ref xRef));
                Vector<float> end = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count)));

                if (remainder > (uint)(Vector<float>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (float* px = &xRef)
                    fixed (float* pd = &dRef)
                    {
                        float* xPtr = px;
                        float* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)(dPtr) % sizeof(float)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. THis is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)(sizeof(Vector<float>)) - ((nuint)(dPtr) % (uint)(sizeof(Vector<float>)))) / sizeof(float);

                            xPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)(dPtr) % (uint)(sizeof(Vector<float>))) == 0);

                            remainder -= misalignment;
                        }

                        Vector<float> vector1;
                        Vector<float> vector2;
                        Vector<float> vector3;
                        Vector<float> vector4;

                        while (remainder >= (uint)(Vector<float>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 0)));
                            vector2 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 1)));
                            vector3 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 2)));
                            vector4 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 3)));

                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 0)) = vector1;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 1)) = vector2;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 2)) = vector3;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 3)) = vector4;

                            // We load, process, and store the next four vectors

                            vector1 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 4)));
                            vector2 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 5)));
                            vector3 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 6)));
                            vector4 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 7)));

                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 4)) = vector1;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 5)) = vector2;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 6)) = vector3;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 7)) = vector4;

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector<float>.Count * 8);
                            dPtr += (uint)(Vector<float>.Count * 8);

                            remainder -= (uint)(Vector<float>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector<float>.Count - 1)) & (nuint)(-Vector<float>.Count);

                switch (remainder / (uint)(Vector<float>.Count))
                {
                    case 8:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 8)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 8)) = vector;
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 7)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 7)) = vector;
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 6)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 6)) = vector;
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 5)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 5)) = vector;
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 4)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 4)) = vector;
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 3)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 3)) = vector;
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 2)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 2)) = vector;
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        AsVector(ref dRef, endIndex - (uint)Vector<float>.Count) = end;
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        AsVector(ref dRefBeg) = beg;
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall(ref float xRef, ref float dRef, nuint remainder, TUnaryOperator op = default)
            {
                switch (remainder)
                {
                    case 7:
                    {
                        Unsafe.Add(ref dRef, 6) = op.Invoke(Unsafe.Add(ref xRef, 6));
                        goto case 6;
                    }

                    case 6:
                    {
                        Unsafe.Add(ref dRef, 5) = op.Invoke(Unsafe.Add(ref xRef, 5));
                        goto case 5;
                    }

                    case 5:
                    {
                        Unsafe.Add(ref dRef, 4) = op.Invoke(Unsafe.Add(ref xRef, 4));
                        goto case 4;
                    }

                    case 4:
                    {
                        Unsafe.Add(ref dRef, 3) = op.Invoke(Unsafe.Add(ref xRef, 3));
                        goto case 3;
                    }

                    case 3:
                    {
                        Unsafe.Add(ref dRef, 2) = op.Invoke(Unsafe.Add(ref xRef, 2));
                        goto case 2;
                    }

                    case 2:
                    {
                        Unsafe.Add(ref dRef, 1) = op.Invoke(Unsafe.Add(ref xRef, 1));
                        goto case 1;
                    }

                    case 1:
                    {
                        dRef = op.Invoke(xRef);
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/> and <paramref name="y"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TBinaryOperator">
        /// Specifies the operation to perform on the pair-wise elements loaded from <paramref name="x"/> and <paramref name="y"/>.
        /// </typeparam>
        private static unsafe void InvokeSpanSpanIntoSpan<TBinaryOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination, TBinaryOperator op = default)
            where TBinaryOperator : struct, IBinaryOperator
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);
            ValidateInputOutputSpanNonOverlapping(y, destination);

            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float yRef = ref MemoryMarshal.GetReference(y);
            ref float dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)(x.Length);

            if (Vector.IsHardwareAccelerated)
            {
                if (remainder >= (uint)(Vector<float>.Count))
                {
                    Vectorized(ref xRef, ref yRef, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref yRef, ref dRef, remainder);
                }

                return;
            }

            // This is the software fallback when no acceleration is available
            // It requires no branches to hit

            SoftwareFallback(ref xRef, ref yRef, ref dRef, remainder);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void SoftwareFallback(ref float xRef, ref float yRef, ref float dRef, nuint length, TBinaryOperator op = default)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, (nint)(i)) = op.Invoke(Unsafe.Add(ref xRef, (nint)(i)),
                                                                Unsafe.Add(ref yRef, (nint)(i)));
                }
            }

            static void Vectorized(ref float xRef, ref float yRef, ref float dRef, nuint remainder, TBinaryOperator op = default)
            {
                ref float dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector<float> beg = op.Invoke(AsVector(ref xRef),
                                              AsVector(ref yRef));
                Vector<float> end = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count)),
                                              AsVector(ref yRef, remainder - (uint)(Vector<float>.Count)));

                if (remainder > (uint)(Vector<float>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (float* px = &xRef)
                    fixed (float* py = &yRef)
                    fixed (float* pd = &dRef)
                    {
                        float* xPtr = px;
                        float* yPtr = py;
                        float* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)(dPtr) % sizeof(float)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. THis is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)(sizeof(Vector<float>)) - ((nuint)(dPtr) % (uint)(sizeof(Vector<float>)))) / sizeof(float);

                            xPtr += misalignment;
                            yPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)(dPtr) % (uint)(sizeof(Vector<float>))) == 0);

                            remainder -= misalignment;
                        }

                        Vector<float> vector1;
                        Vector<float> vector2;
                        Vector<float> vector3;
                        Vector<float> vector4;

                        while (remainder >= (uint)(Vector<float>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 0)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 0)));
                            vector2 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 1)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 1)));
                            vector3 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 2)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 2)));
                            vector4 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 3)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 3)));

                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 0)) = vector1;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 1)) = vector2;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 2)) = vector3;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 3)) = vector4;

                            // We load, process, and store the next four vectors

                            vector1 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 4)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 4)));
                            vector2 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 5)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 5)));
                            vector3 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 6)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 6)));
                            vector4 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 7)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 7)));

                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 4)) = vector1;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 5)) = vector2;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 6)) = vector3;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 7)) = vector4;

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector<float>.Count * 8);
                            yPtr += (uint)(Vector<float>.Count * 8);
                            dPtr += (uint)(Vector<float>.Count * 8);

                            remainder -= (uint)(Vector<float>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector<float>.Count - 1)) & (nuint)(-Vector<float>.Count);

                switch (remainder / (uint)(Vector<float>.Count))
                {
                    case 8:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 8)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 8)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 8)) = vector;
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 7)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 7)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 7)) = vector;
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 6)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 6)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 6)) = vector;
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 5)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 5)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 5)) = vector;
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 4)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 4)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 4)) = vector;
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 3)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 3)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 3)) = vector;
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 2)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 2)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 2)) = vector;
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        AsVector(ref dRef, endIndex - (uint)Vector<float>.Count) = end;
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        AsVector(ref dRefBeg) = beg;
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall(ref float xRef, ref float yRef, ref float dRef, nuint remainder, TBinaryOperator op = default)
            {
                switch (remainder)
                {
                    case 7:
                    {
                        Unsafe.Add(ref dRef, 6) = op.Invoke(Unsafe.Add(ref xRef, 6),
                                                            Unsafe.Add(ref yRef, 6));
                        goto case 6;
                    }

                    case 6:
                    {
                        Unsafe.Add(ref dRef, 5) = op.Invoke(Unsafe.Add(ref xRef, 5),
                                                            Unsafe.Add(ref yRef, 5));
                        goto case 5;
                    }

                    case 5:
                    {
                        Unsafe.Add(ref dRef, 4) = op.Invoke(Unsafe.Add(ref xRef, 4),
                                                            Unsafe.Add(ref yRef, 4));
                        goto case 4;
                    }

                    case 4:
                    {
                        Unsafe.Add(ref dRef, 3) = op.Invoke(Unsafe.Add(ref xRef, 3),
                                                            Unsafe.Add(ref yRef, 3));
                        goto case 3;
                    }

                    case 3:
                    {
                        Unsafe.Add(ref dRef, 2) = op.Invoke(Unsafe.Add(ref xRef, 2),
                                                            Unsafe.Add(ref yRef, 2));
                        goto case 2;
                    }

                    case 2:
                    {
                        Unsafe.Add(ref dRef, 1) = op.Invoke(Unsafe.Add(ref xRef, 1),
                                                            Unsafe.Add(ref yRef, 1));
                        goto case 1;
                    }

                    case 1:
                    {
                        dRef = op.Invoke(xRef, yRef);
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/> and <paramref name="y"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TBinaryOperator">
        /// Specifies the operation to perform on each element loaded from <paramref name="x"/> with <paramref name="y"/>.
        /// </typeparam>
        private static void InvokeSpanScalarIntoSpan<TBinaryOperator>(
            ReadOnlySpan<float> x, float y, Span<float> destination, TBinaryOperator op = default)
            where TBinaryOperator : struct, IBinaryOperator =>
            InvokeSpanScalarIntoSpan<IdentityOperator_Single, TBinaryOperator>(x, y, destination, default, op);

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/> and <paramref name="y"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TTransformOperator">
        /// Specifies the operation to perform on each element loaded from <paramref name="x"/>.
        /// It is not used with <paramref name="y"/>.
        /// </typeparam>
        /// <typeparam name="TBinaryOperator">
        /// Specifies the operation to perform on the transformed value from <paramref name="x"/> with <paramref name="y"/>.
        /// </typeparam>
        private static unsafe void InvokeSpanScalarIntoSpan<TTransformOperator, TBinaryOperator>(
            ReadOnlySpan<float> x, float y, Span<float> destination, TTransformOperator xTransformOp = default, TBinaryOperator binaryOp = default)
            where TTransformOperator : struct, IUnaryOperator
            where TBinaryOperator : struct, IBinaryOperator
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);

            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)(x.Length);

            if (Vector.IsHardwareAccelerated && xTransformOp.CanVectorize)
            {
                if (remainder >= (uint)(Vector<float>.Count))
                {
                    Vectorized(ref xRef, y, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, y, ref dRef, remainder);
                }

                return;
            }

            // This is the software fallback when no acceleration is available
            // It requires no branches to hit

            SoftwareFallback(ref xRef, y, ref dRef, remainder);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void SoftwareFallback(ref float xRef, float y, ref float dRef, nuint length, TTransformOperator xTransformOp = default, TBinaryOperator binaryOp = default)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, (nint)(i)) = binaryOp.Invoke(xTransformOp.Invoke(Unsafe.Add(ref xRef, (nint)(i))),
                                                                      y);
                }
            }

            static void Vectorized(ref float xRef, float y, ref float dRef, nuint remainder, TTransformOperator xTransformOp = default, TBinaryOperator binaryOp = default)
            {
                ref float dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector<float> yVec = new Vector<float>(y);

                Vector<float> beg = binaryOp.Invoke(xTransformOp.Invoke(AsVector(ref xRef)),
                                                    yVec);
                Vector<float> end = binaryOp.Invoke(xTransformOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count))),
                                                    yVec);

                if (remainder > (uint)(Vector<float>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (float* px = &xRef)
                    fixed (float* pd = &dRef)
                    {
                        float* xPtr = px;
                        float* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)(dPtr) % sizeof(float)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. THis is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)(sizeof(Vector<float>)) - ((nuint)(dPtr) % (uint)(sizeof(Vector<float>)))) / sizeof(float);

                            xPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)(dPtr) % (uint)(sizeof(Vector<float>))) == 0);

                            remainder -= misalignment;
                        }

                        Vector<float> vector1;
                        Vector<float> vector2;
                        Vector<float> vector3;
                        Vector<float> vector4;

                        while (remainder >= (uint)(Vector<float>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = binaryOp.Invoke(xTransformOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 0))),
                                                                          yVec);
                            vector2 = binaryOp.Invoke(xTransformOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 1))),
                                                                          yVec);
                            vector3 = binaryOp.Invoke(xTransformOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 2))),
                                                                          yVec);
                            vector4 = binaryOp.Invoke(xTransformOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 3))),
                                                                          yVec);

                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 0)) = vector1;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 1)) = vector2;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 2)) = vector3;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 3)) = vector4;

                            // We load, process, and store the next four vectors

                            vector1 = binaryOp.Invoke(xTransformOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 4))),
                                                                          yVec);
                            vector2 = binaryOp.Invoke(xTransformOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 5))),
                                                                          yVec);
                            vector3 = binaryOp.Invoke(xTransformOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 6))),
                                                                          yVec);
                            vector4 = binaryOp.Invoke(xTransformOp.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 7))),
                                                                          yVec);

                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 4)) = vector1;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 5)) = vector2;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 6)) = vector3;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 7)) = vector4;

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector<float>.Count * 8);
                            dPtr += (uint)(Vector<float>.Count * 8);

                            remainder -= (uint)(Vector<float>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector<float>.Count - 1)) & (nuint)(-Vector<float>.Count);

                switch (remainder / (uint)(Vector<float>.Count))
                {
                    case 8:
                    {
                        Vector<float> vector = binaryOp.Invoke(xTransformOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 8))),
                                                               yVec);
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 8)) = vector;
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector<float> vector = binaryOp.Invoke(xTransformOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 7))),
                                                               yVec);
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 7)) = vector;
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector<float> vector = binaryOp.Invoke(xTransformOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 6))),
                                                               yVec);
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 6)) = vector;
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector<float> vector = binaryOp.Invoke(xTransformOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 5))),
                                                               yVec);
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 5)) = vector;
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector<float> vector = binaryOp.Invoke(xTransformOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 4))),
                                                               yVec);
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 4)) = vector;
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector<float> vector = binaryOp.Invoke(xTransformOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 3))),
                                                               yVec);
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 3)) = vector;
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector<float> vector = binaryOp.Invoke(xTransformOp.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 2))),
                                                               yVec);
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 2)) = vector;
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        AsVector(ref dRef, endIndex - (uint)Vector<float>.Count) = end;
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        AsVector(ref dRefBeg) = beg;
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall(ref float xRef, float y, ref float dRef, nuint remainder, TTransformOperator xTransformOp = default, TBinaryOperator binaryOp = default)
            {
                switch (remainder)
                {
                    case 7:
                    {
                        Unsafe.Add(ref dRef, 6) = binaryOp.Invoke(xTransformOp.Invoke(Unsafe.Add(ref xRef, 6)),
                                                                  y);
                        goto case 6;
                    }

                    case 6:
                    {
                        Unsafe.Add(ref dRef, 5) = binaryOp.Invoke(xTransformOp.Invoke(Unsafe.Add(ref xRef, 5)),
                                                                  y);
                        goto case 5;
                    }

                    case 5:
                    {
                        Unsafe.Add(ref dRef, 4) = binaryOp.Invoke(xTransformOp.Invoke(Unsafe.Add(ref xRef, 4)),
                                                                  y);
                        goto case 4;
                    }

                    case 4:
                    {
                        Unsafe.Add(ref dRef, 3) = binaryOp.Invoke(xTransformOp.Invoke(Unsafe.Add(ref xRef, 3)),
                                                                  y);
                        goto case 3;
                    }

                    case 3:
                    {
                        Unsafe.Add(ref dRef, 2) = binaryOp.Invoke(xTransformOp.Invoke(Unsafe.Add(ref xRef, 2)),
                                                                  y);
                        goto case 2;
                    }

                    case 2:
                    {
                        Unsafe.Add(ref dRef, 1) = binaryOp.Invoke(xTransformOp.Invoke(Unsafe.Add(ref xRef, 1)),
                                                                  y);
                        goto case 1;
                    }

                    case 1:
                    {
                        dRef = binaryOp.Invoke(xTransformOp.Invoke(xRef), y);
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/>, <paramref name="y"/>, and <paramref name="z"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TTernaryOperator">
        /// Specifies the operation to perform on the pair-wise elements loaded from <paramref name="x"/>, <paramref name="y"/>,
        /// and <paramref name="z"/>.
        /// </typeparam>
        private static unsafe void InvokeSpanSpanSpanIntoSpan<TTernaryOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, ReadOnlySpan<float> z, Span<float> destination, TTernaryOperator op = default)
            where TTernaryOperator : struct, ITernaryOperator
        {
            if (x.Length != y.Length || x.Length != z.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);
            ValidateInputOutputSpanNonOverlapping(y, destination);
            ValidateInputOutputSpanNonOverlapping(z, destination);

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float yRef = ref MemoryMarshal.GetReference(y);
            ref float zRef = ref MemoryMarshal.GetReference(z);
            ref float dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)(x.Length);

            if (Vector.IsHardwareAccelerated)
            {
                if (remainder >= (uint)(Vector<float>.Count))
                {
                    Vectorized(ref xRef, ref yRef, ref zRef, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref yRef, ref zRef, ref dRef, remainder);
                }

                return;
            }

            // This is the software fallback when no acceleration is available
            // It requires no branches to hit

            SoftwareFallback(ref xRef, ref yRef, ref zRef, ref dRef, remainder);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void SoftwareFallback(ref float xRef, ref float yRef, ref float zRef, ref float dRef, nuint length, TTernaryOperator op = default)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, (nint)(i)) = op.Invoke(Unsafe.Add(ref xRef, (nint)(i)),
                                                                Unsafe.Add(ref yRef, (nint)(i)),
                                                                Unsafe.Add(ref zRef, (nint)(i)));
                }
            }

            static void Vectorized(ref float xRef, ref float yRef, ref float zRef, ref float dRef, nuint remainder, TTernaryOperator op = default)
            {
                ref float dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector<float> beg = op.Invoke(AsVector(ref xRef),
                                              AsVector(ref yRef),
                                              AsVector(ref zRef));
                Vector<float> end = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count)),
                                              AsVector(ref yRef, remainder - (uint)(Vector<float>.Count)),
                                              AsVector(ref zRef, remainder - (uint)(Vector<float>.Count)));

                if (remainder > (uint)(Vector<float>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (float* px = &xRef)
                    fixed (float* py = &yRef)
                    fixed (float* pz = &zRef)
                    fixed (float* pd = &dRef)
                    {
                        float* xPtr = px;
                        float* yPtr = py;
                        float* zPtr = pz;
                        float* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)(dPtr) % sizeof(float)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. THis is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)(sizeof(Vector<float>)) - ((nuint)(dPtr) % (uint)(sizeof(Vector<float>)))) / sizeof(float);

                            xPtr += misalignment;
                            yPtr += misalignment;
                            zPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)(dPtr) % (uint)(sizeof(Vector<float>))) == 0);

                            remainder -= misalignment;
                        }

                        Vector<float> vector1;
                        Vector<float> vector2;
                        Vector<float> vector3;
                        Vector<float> vector4;

                        while (remainder >= (uint)(Vector<float>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 0)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 0)),
                                                *(Vector<float>*)(zPtr + (uint)(Vector<float>.Count * 0)));
                            vector2 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 1)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 1)),
                                                *(Vector<float>*)(zPtr + (uint)(Vector<float>.Count * 1)));
                            vector3 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 2)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 2)),
                                                *(Vector<float>*)(zPtr + (uint)(Vector<float>.Count * 2)));
                            vector4 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 3)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 3)),
                                                *(Vector<float>*)(zPtr + (uint)(Vector<float>.Count * 3)));

                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 0)) = vector1;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 1)) = vector2;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 2)) = vector3;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 3)) = vector4;

                            // We load, process, and store the next four vectors

                            vector1 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 4)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 4)),
                                                *(Vector<float>*)(zPtr + (uint)(Vector<float>.Count * 4)));
                            vector2 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 5)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 5)),
                                                *(Vector<float>*)(zPtr + (uint)(Vector<float>.Count * 5)));
                            vector3 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 6)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 6)),
                                                *(Vector<float>*)(zPtr + (uint)(Vector<float>.Count * 6)));
                            vector4 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 7)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 7)),
                                                *(Vector<float>*)(zPtr + (uint)(Vector<float>.Count * 7)));

                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 4)) = vector1;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 5)) = vector2;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 6)) = vector3;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 7)) = vector4;

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector<float>.Count * 8);
                            yPtr += (uint)(Vector<float>.Count * 8);
                            zPtr += (uint)(Vector<float>.Count * 8);
                            dPtr += (uint)(Vector<float>.Count * 8);

                            remainder -= (uint)(Vector<float>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                        zRef = ref *zPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector<float>.Count - 1)) & (nuint)(-Vector<float>.Count);

                switch (remainder / (uint)(Vector<float>.Count))
                {
                    case 8:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 8)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 8)),
                                                         AsVector(ref zRef, remainder - (uint)(Vector<float>.Count * 8)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 8)) = vector;
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 7)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 7)),
                                                         AsVector(ref zRef, remainder - (uint)(Vector<float>.Count * 7)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 7)) = vector;
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 6)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 6)),
                                                         AsVector(ref zRef, remainder - (uint)(Vector<float>.Count * 6)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 6)) = vector;
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 5)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 5)),
                                                         AsVector(ref zRef, remainder - (uint)(Vector<float>.Count * 5)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 5)) = vector;
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 4)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 4)),
                                                         AsVector(ref zRef, remainder - (uint)(Vector<float>.Count * 4)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 4)) = vector;
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 3)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 3)),
                                                         AsVector(ref zRef, remainder - (uint)(Vector<float>.Count * 3)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 3)) = vector;
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 2)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 2)),
                                                         AsVector(ref zRef, remainder - (uint)(Vector<float>.Count * 2)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 2)) = vector;
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        AsVector(ref dRef, endIndex - (uint)Vector<float>.Count) = end;
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        AsVector(ref dRefBeg) = beg;
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall(ref float xRef, ref float yRef, ref float zRef, ref float dRef, nuint remainder, TTernaryOperator op = default)
            {
                switch (remainder)
                {
                    case 7:
                    {
                        Unsafe.Add(ref dRef, 6) = op.Invoke(Unsafe.Add(ref xRef, 6),
                                                            Unsafe.Add(ref yRef, 6),
                                                            Unsafe.Add(ref zRef, 6));
                        goto case 6;
                    }

                    case 6:
                    {
                        Unsafe.Add(ref dRef, 5) = op.Invoke(Unsafe.Add(ref xRef, 5),
                                                            Unsafe.Add(ref yRef, 5),
                                                            Unsafe.Add(ref zRef, 5));
                        goto case 5;
                    }

                    case 5:
                    {
                        Unsafe.Add(ref dRef, 4) = op.Invoke(Unsafe.Add(ref xRef, 4),
                                                            Unsafe.Add(ref yRef, 4),
                                                            Unsafe.Add(ref zRef, 4));
                        goto case 4;
                    }

                    case 4:
                    {
                        Unsafe.Add(ref dRef, 3) = op.Invoke(Unsafe.Add(ref xRef, 3),
                                                            Unsafe.Add(ref yRef, 3),
                                                            Unsafe.Add(ref zRef, 3));
                        goto case 3;
                    }

                    case 3:
                    {
                        Unsafe.Add(ref dRef, 2) = op.Invoke(Unsafe.Add(ref xRef, 2),
                                                            Unsafe.Add(ref yRef, 2),
                                                            Unsafe.Add(ref zRef, 2));
                        goto case 2;
                    }

                    case 2:
                    {
                        Unsafe.Add(ref dRef, 1) = op.Invoke(Unsafe.Add(ref xRef, 1),
                                                            Unsafe.Add(ref yRef, 1),
                                                            Unsafe.Add(ref zRef, 1));
                        goto case 1;
                    }

                    case 1:
                    {
                        dRef = op.Invoke(xRef, yRef, zRef);
                        break;
                    }

                    case 0:
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/>, <paramref name="y"/>, and <paramref name="z"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TTernaryOperator">
        /// Specifies the operation to perform on the pair-wise elements loaded from <paramref name="x"/> and <paramref name="y"/>
        /// with <paramref name="z"/>.
        /// </typeparam>
        private static unsafe void InvokeSpanSpanScalarIntoSpan<TTernaryOperator>(
            ReadOnlySpan<float> x, ReadOnlySpan<float> y, float z, Span<float> destination, TTernaryOperator op = default)
            where TTernaryOperator : struct, ITernaryOperator
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);
            ValidateInputOutputSpanNonOverlapping(y, destination);

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float yRef = ref MemoryMarshal.GetReference(y);
            ref float dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)(x.Length);

            if (Vector.IsHardwareAccelerated)
            {
                if (remainder >= (uint)(Vector<float>.Count))
                {
                    Vectorized(ref xRef, ref yRef, z, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref yRef, z, ref dRef, remainder);
                }

                return;
            }

            // This is the software fallback when no acceleration is available
            // It requires no branches to hit

            SoftwareFallback(ref xRef, ref yRef, z, ref dRef, remainder);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void SoftwareFallback(ref float xRef, ref float yRef, float z, ref float dRef, nuint length, TTernaryOperator op = default)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, (nint)(i)) = op.Invoke(Unsafe.Add(ref xRef, (nint)(i)),
                                                                Unsafe.Add(ref yRef, (nint)(i)),
                                                                z);
                }
            }

            static void Vectorized(ref float xRef, ref float yRef, float z, ref float dRef, nuint remainder, TTernaryOperator op = default)
            {
                ref float dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector<float> zVec = new Vector<float>(z);

                Vector<float> beg = op.Invoke(AsVector(ref xRef),
                                              AsVector(ref yRef),
                                              zVec);
                Vector<float> end = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count)),
                                              AsVector(ref yRef, remainder - (uint)(Vector<float>.Count)),
                                              zVec);

                if (remainder > (uint)(Vector<float>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (float* px = &xRef)
                    fixed (float* py = &yRef)
                    fixed (float* pd = &dRef)
                    {
                        float* xPtr = px;
                        float* yPtr = py;
                        float* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)(dPtr) % sizeof(float)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. THis is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)(sizeof(Vector<float>)) - ((nuint)(dPtr) % (uint)(sizeof(Vector<float>)))) / sizeof(float);

                            xPtr += misalignment;
                            yPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)(dPtr) % (uint)(sizeof(Vector<float>))) == 0);

                            remainder -= misalignment;
                        }

                        Vector<float> vector1;
                        Vector<float> vector2;
                        Vector<float> vector3;
                        Vector<float> vector4;

                        while (remainder >= (uint)(Vector<float>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 0)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 0)),
                                                zVec);
                            vector2 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 1)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 1)),
                                                zVec);
                            vector3 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 2)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 2)),
                                                zVec);
                            vector4 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 3)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 3)),
                                                zVec);

                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 0)) = vector1;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 1)) = vector2;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 2)) = vector3;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 3)) = vector4;

                            // We load, process, and store the next four vectors

                            vector1 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 4)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 4)),
                                                zVec);
                            vector2 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 5)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 5)),
                                                zVec);
                            vector3 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 6)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 6)),
                                                zVec);
                            vector4 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 7)),
                                                *(Vector<float>*)(yPtr + (uint)(Vector<float>.Count * 7)),
                                                zVec);

                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 4)) = vector1;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 5)) = vector2;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 6)) = vector3;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 7)) = vector4;

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector<float>.Count * 8);
                            yPtr += (uint)(Vector<float>.Count * 8);
                            dPtr += (uint)(Vector<float>.Count * 8);

                            remainder -= (uint)(Vector<float>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector<float>.Count - 1)) & (nuint)(-Vector<float>.Count);

                switch (remainder / (uint)(Vector<float>.Count))
                {
                    case 8:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 8)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 8)),
                                                         zVec);
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 8)) = vector;
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 7)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 7)),
                                                         zVec);
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 7)) = vector;
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 6)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 6)),
                                                         zVec);
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 6)) = vector;
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 5)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 5)),
                                                         zVec);
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 5)) = vector;
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 4)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 4)),
                                                         zVec);
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 4)) = vector;
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 3)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 3)),
                                                         zVec);
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 3)) = vector;
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 2)),
                                                         AsVector(ref yRef, remainder - (uint)(Vector<float>.Count * 2)),
                                                         zVec);
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 2)) = vector;
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        AsVector(ref dRef, endIndex - (uint)Vector<float>.Count) = end;
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        AsVector(ref dRefBeg) = beg;
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall(ref float xRef, ref float yRef, float z, ref float dRef, nuint remainder, TTernaryOperator op = default)
            {
                switch (remainder)
                {
                    case 7:
                    {
                        Unsafe.Add(ref dRef, 6) = op.Invoke(Unsafe.Add(ref xRef, 6),
                                                            Unsafe.Add(ref yRef, 6),
                                                            z);
                        goto case 6;
                    }

                    case 6:
                    {
                        Unsafe.Add(ref dRef, 5) = op.Invoke(Unsafe.Add(ref xRef, 5),
                                                            Unsafe.Add(ref yRef, 5),
                                                            z);
                        goto case 5;
                    }

                    case 5:
                    {
                        Unsafe.Add(ref dRef, 4) = op.Invoke(Unsafe.Add(ref xRef, 4),
                                                            Unsafe.Add(ref yRef, 4),
                                                            z);
                        goto case 4;
                    }

                    case 4:
                    {
                        Unsafe.Add(ref dRef, 3) = op.Invoke(Unsafe.Add(ref xRef, 3),
                                                            Unsafe.Add(ref yRef, 3),
                                                            z);
                        goto case 3;
                    }

                    case 3:
                    {
                        Unsafe.Add(ref dRef, 2) = op.Invoke(Unsafe.Add(ref xRef, 2),
                                                            Unsafe.Add(ref yRef, 2),
                                                            z);
                        goto case 2;
                    }

                    case 2:
                    {
                        Unsafe.Add(ref dRef, 1) = op.Invoke(Unsafe.Add(ref xRef, 1),
                                                            Unsafe.Add(ref yRef, 1),
                                                            z);
                        goto case 1;
                    }

                    case 1:
                    {
                        dRef = op.Invoke(xRef, yRef, z);
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/>, <paramref name="y"/>, and <paramref name="z"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="TTernaryOperator">
        /// Specifies the operation to perform on the pair-wise element loaded from <paramref name="x"/>, with <paramref name="y"/>,
        /// and the element loaded from <paramref name="z"/>.
        /// </typeparam>
        private static unsafe void InvokeSpanScalarSpanIntoSpan<TTernaryOperator>(
            ReadOnlySpan<float> x, float y, ReadOnlySpan<float> z, Span<float> destination, TTernaryOperator op = default)
            where TTernaryOperator : struct, ITernaryOperator
        {
            if (x.Length != z.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);
            ValidateInputOutputSpanNonOverlapping(z, destination);

            ref float xRef = ref MemoryMarshal.GetReference(x);
            ref float zRef = ref MemoryMarshal.GetReference(z);
            ref float dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)(x.Length);

            if (Vector.IsHardwareAccelerated)
            {
                if (remainder >= (uint)(Vector<float>.Count))
                {
                    Vectorized(ref xRef, y, ref zRef, ref dRef, remainder);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, y, ref zRef, ref dRef, remainder);
                }

                return;
            }

            // This is the software fallback when no acceleration is available
            // It requires no branches to hit

            SoftwareFallback(ref xRef, y, ref zRef, ref dRef, remainder);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void SoftwareFallback(ref float xRef, float y, ref float zRef, ref float dRef, nuint length, TTernaryOperator op = default)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, (nint)(i)) = op.Invoke(Unsafe.Add(ref xRef, (nint)(i)),
                                                                y,
                                                                Unsafe.Add(ref zRef, (nint)(i)));
                }
            }

            static void Vectorized(ref float xRef, float y, ref float zRef, ref float dRef, nuint remainder, TTernaryOperator op = default)
            {
                ref float dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector<float> yVec = new Vector<float>(y);

                Vector<float> beg = op.Invoke(AsVector(ref xRef),
                                              yVec,
                                              AsVector(ref zRef));
                Vector<float> end = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count)),
                                              yVec,
                                              AsVector(ref zRef, remainder - (uint)(Vector<float>.Count)));

                if (remainder > (uint)(Vector<float>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (float* px = &xRef)
                    fixed (float* pz = &zRef)
                    fixed (float* pd = &dRef)
                    {
                        float* xPtr = px;
                        float* zPtr = pz;
                        float* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)(dPtr) % sizeof(float)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. THis is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)(sizeof(Vector<float>)) - ((nuint)(dPtr) % (uint)(sizeof(Vector<float>)))) / sizeof(float);

                            xPtr += misalignment;
                            zPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)(dPtr) % (uint)(sizeof(Vector<float>))) == 0);

                            remainder -= misalignment;
                        }

                        Vector<float> vector1;
                        Vector<float> vector2;
                        Vector<float> vector3;
                        Vector<float> vector4;

                        while (remainder >= (uint)(Vector<float>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 0)),
                                                yVec,
                                                *(Vector<float>*)(zPtr + (uint)(Vector<float>.Count * 0)));
                            vector2 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 1)),
                                                yVec,
                                                *(Vector<float>*)(zPtr + (uint)(Vector<float>.Count * 1)));
                            vector3 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 2)),
                                                yVec,
                                                *(Vector<float>*)(zPtr + (uint)(Vector<float>.Count * 2)));
                            vector4 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 3)),
                                                yVec,
                                                *(Vector<float>*)(zPtr + (uint)(Vector<float>.Count * 3)));

                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 0)) = vector1;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 1)) = vector2;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 2)) = vector3;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 3)) = vector4;

                            // We load, process, and store the next four vectors

                            vector1 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 4)),
                                                yVec,
                                                *(Vector<float>*)(zPtr + (uint)(Vector<float>.Count * 4)));
                            vector2 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 5)),
                                                yVec,
                                                *(Vector<float>*)(zPtr + (uint)(Vector<float>.Count * 5)));
                            vector3 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 6)),
                                                yVec,
                                                *(Vector<float>*)(zPtr + (uint)(Vector<float>.Count * 6)));
                            vector4 = op.Invoke(*(Vector<float>*)(xPtr + (uint)(Vector<float>.Count * 7)),
                                                yVec,
                                                *(Vector<float>*)(zPtr + (uint)(Vector<float>.Count * 7)));

                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 4)) = vector1;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 5)) = vector2;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 6)) = vector3;
                            *(Vector<float>*)(dPtr + (uint)(Vector<float>.Count * 7)) = vector4;

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector<float>.Count * 8);
                            zPtr += (uint)(Vector<float>.Count * 8);
                            dPtr += (uint)(Vector<float>.Count * 8);

                            remainder -= (uint)(Vector<float>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        zRef = ref *zPtr;
                        dRef = ref *dPtr;
                    }
                }

                // Process the remaining [Count, Count * 8] elements via a jump table
                //
                // Unless the original length was an exact multiple of Count, then we'll
                // end up reprocessing a couple elements in case 1 for end. We'll also
                // potentially reprocess a few elements in case 0 for beg, to handle any
                // data before the first aligned address.

                nuint endIndex = remainder;
                remainder = (remainder + (uint)(Vector<float>.Count - 1)) & (nuint)(-Vector<float>.Count);

                switch (remainder / (uint)(Vector<float>.Count))
                {
                    case 8:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 8)),
                                                         yVec,
                                                         AsVector(ref zRef, remainder - (uint)(Vector<float>.Count * 8)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 8)) = vector;
                        goto case 7;
                    }

                    case 7:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 7)),
                                                         yVec,
                                                         AsVector(ref zRef, remainder - (uint)(Vector<float>.Count * 7)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 7)) = vector;
                        goto case 6;
                    }

                    case 6:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 6)),
                                                         yVec,
                                                         AsVector(ref zRef, remainder - (uint)(Vector<float>.Count * 6)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 6)) = vector;
                        goto case 5;
                    }

                    case 5:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 5)),
                                                         yVec,
                                                         AsVector(ref zRef, remainder - (uint)(Vector<float>.Count * 5)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 5)) = vector;
                        goto case 4;
                    }

                    case 4:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 4)),
                                                         yVec,
                                                         AsVector(ref zRef, remainder - (uint)(Vector<float>.Count * 4)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 4)) = vector;
                        goto case 3;
                    }

                    case 3:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 3)),
                                                         yVec,
                                                         AsVector(ref zRef, remainder - (uint)(Vector<float>.Count * 3)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 3)) = vector;
                        goto case 2;
                    }

                    case 2:
                    {
                        Vector<float> vector = op.Invoke(AsVector(ref xRef, remainder - (uint)(Vector<float>.Count * 2)),
                                                         yVec,
                                                         AsVector(ref zRef, remainder - (uint)(Vector<float>.Count * 2)));
                        AsVector(ref dRef, remainder - (uint)(Vector<float>.Count * 2)) = vector;
                        goto case 1;
                    }

                    case 1:
                    {
                        // Store the last block, which includes any elements that wouldn't fill a full vector
                        AsVector(ref dRef, endIndex - (uint)Vector<float>.Count) = end;
                        goto case 0;
                    }

                    case 0:
                    {
                        // Store the first block, which includes any elements preceding the first aligned block
                        AsVector(ref dRefBeg) = beg;
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall(ref float xRef, float y, ref float zRef, ref float dRef, nuint remainder, TTernaryOperator op = default)
            {
                switch (remainder)
                {
                    case 7:
                    {
                        Unsafe.Add(ref dRef, 6) = op.Invoke(Unsafe.Add(ref xRef, 6),
                                                            y,
                                                            Unsafe.Add(ref zRef, 6));
                        goto case 6;
                    }

                    case 6:
                    {
                        Unsafe.Add(ref dRef, 5) = op.Invoke(Unsafe.Add(ref xRef, 5),
                                                            y,
                                                            Unsafe.Add(ref zRef, 5));
                        goto case 5;
                    }

                    case 5:
                    {
                        Unsafe.Add(ref dRef, 4) = op.Invoke(Unsafe.Add(ref xRef, 4),
                                                            y,
                                                            Unsafe.Add(ref zRef, 4));
                        goto case 4;
                    }

                    case 4:
                    {
                        Unsafe.Add(ref dRef, 3) = op.Invoke(Unsafe.Add(ref xRef, 3),
                                                            y,
                                                            Unsafe.Add(ref zRef, 3));
                        goto case 3;
                    }

                    case 3:
                    {
                        Unsafe.Add(ref dRef, 2) = op.Invoke(Unsafe.Add(ref xRef, 2),
                                                            y,
                                                            Unsafe.Add(ref zRef, 2));
                        goto case 2;
                    }

                    case 2:
                    {
                        Unsafe.Add(ref dRef, 1) = op.Invoke(Unsafe.Add(ref xRef, 1),
                                                            y,
                                                            Unsafe.Add(ref zRef, 1));
                        goto case 1;
                    }

                    case 1:
                    {
                        dRef = op.Invoke(xRef, y, zRef);
                        goto case 0;
                    }

                    case 0:
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>Loads a <see cref="Vector{Single}"/> from <paramref name="start"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Vector<float> AsVector(ref float start) =>
            ref Unsafe.As<float, Vector<float>>(ref start);

        /// <summary>Loads a <see cref="Vector{Single}"/> that begins at the specified <paramref name="offset"/> from <paramref name="start"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Vector<float> AsVector(ref float start, int offset) =>
            ref Unsafe.As<float, Vector<float>>(
                ref Unsafe.Add(ref start, offset));

        /// <summary>Loads a <see cref="Vector{Single}"/> that begins at the specified <paramref name="offset"/> from <paramref name="start"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Vector<float> AsVector(ref float start, nuint offset) =>
            ref Unsafe.As<float, Vector<float>>(
                ref Unsafe.Add(ref start, (nint)(offset)));

        /// <summary>Loads a <see cref="Vector{Single}"/> that begins at the specified <paramref name="offset"/> from <paramref name="start"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Vector<int> AsVector(ref int start, int offset) =>
            ref Unsafe.As<int, Vector<int>>(
                ref Unsafe.Add(ref start, offset));

        /// <summary>Gets whether the specified <see cref="float"/> is positive.</summary>
        private static bool IsPositive(float f) => !IsNegative(f);

        /// <summary>Gets whether each specified <see cref="float"/> is positive.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<float> IsPositive(Vector<float> vector) =>
            ((Vector<float>)Vector.GreaterThan(((Vector<int>)vector), Vector<int>.Zero));

        /// <summary>Gets whether the specified <see cref="float"/> is negative.</summary>
        private static unsafe bool IsNegative(float f) => *(int*)&f < 0;

        /// <summary>Gets whether each specified <see cref="float"/> is negative.</summary>
        private static Vector<float> IsNegative(Vector<float> f) =>
            (Vector<float>)Vector.LessThan((Vector<int>)f, Vector<int>.Zero);

        /// <summary>Gets the base 2 logarithm of <paramref name="x"/>.</summary>
        private static float Log2(float x) => MathF.Log(x, 2);

        /// <summary>
        /// Gets a vector mask that will be all-ones-set for the first <paramref name="count"/> elements
        /// and zero for all other elements.
        /// </summary>
        private static Vector<float> CreateAlignmentMaskSingleVector(int count)
        {
            Debug.Assert(Vector<float>.Count is 4 or 8 or 16);

            return AsVector(
                ref Unsafe.As<uint, float>(ref MemoryMarshal.GetReference(AlignmentUInt32Mask_16x17)),
                (count * 16));
        }

        /// <summary>
        /// Gets a vector mask that will be all-ones-set for the last <paramref name="count"/> elements
        /// and zero for all other elements.
        /// </summary>
        private static Vector<float> CreateRemainderMaskSingleVector(int count)
        {
            Debug.Assert(Vector<float>.Count is 4 or 8 or 16);

            return AsVector(
                ref Unsafe.As<uint, float>(ref MemoryMarshal.GetReference(RemainderUInt32Mask_16x17)),
                (count * 16) + (16 - Vector<float>.Count));
        }

        /// <summary>x + y</summary>
        private readonly struct AddOperator_Single : IAggregationOperator
        {
            public float Invoke(float x, float y) => x + y;
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) => x + y;
            public float IdentityValue => 0;
        }

        /// <summary>x - y</summary>
        private readonly struct SubtractOperator_Single : IBinaryOperator
        {
            public float Invoke(float x, float y) => x - y;
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) => x - y;
        }

        /// <summary>(x - y) * (x - y)</summary>
        private readonly struct SubtractSquaredOperator_Single : IBinaryOperator
        {
            public float Invoke(float x, float y)
            {
                float tmp = x - y;
                return tmp * tmp;
            }

            public Vector<float> Invoke(Vector<float> x, Vector<float> y)
            {
                Vector<float> tmp = x - y;
                return tmp * tmp;
            }
        }

        /// <summary>x * y</summary>
        private readonly struct MultiplyOperator_Single : IAggregationOperator
        {
            public float Invoke(float x, float y) => x * y;
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) => x * y;
            public float IdentityValue => 1;
        }

        /// <summary>x / y</summary>
        private readonly struct DivideOperator_Single : IBinaryOperator
        {
            public float Invoke(float x, float y) => x / y;
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) => x / y;
        }

        private interface IIndexOfOperator
        {
            int Invoke(ref float result, float current, int resultIndex, int currentIndex);
            int Invoke(Vector<float> result, Vector<int> resultIndex);
            void Invoke(ref Vector<float> result, Vector<float> current, ref Vector<int> resultIndex, Vector<int> currentIndex);
        }

        /// <summary>Returns the index of MathF.Max(x, y)</summary>
        private readonly struct IndexOfMaxOperator_Single : IIndexOfOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Invoke(ref float result, float current, int resultIndex, int currentIndex)
            {
                if (result == current)
                {
                    bool resultNegative = IsNegative(result);
                    if ((resultNegative == IsNegative(current)) ? (currentIndex < resultIndex) : resultNegative)
                    {
                        result = current;
                        return currentIndex;
                    }
                }
                else if (current > result)
                {
                    result = current;
                    return currentIndex;
                }

                return resultIndex;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Invoke(Vector<float> current, Vector<int> currentIndex)
            {
                float result = current[0];
                int resultIndex = currentIndex[0];

                for (int i = 1; i < Vector<float>.Count; i++)
                {
                    if (current[i] == result)
                    {
                        bool resultNegative = IsNegative(result);
                        if ((resultNegative == IsNegative(current[i])) ? (currentIndex[i] < resultIndex) : resultNegative)
                        {
                            result = current[i];
                            resultIndex = currentIndex[i];
                        }
                    }
                    else if (current[i] > result)
                    {
                        result = current[i];
                        resultIndex = currentIndex[i];
                    }
                }

                return resultIndex;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Invoke(ref Vector<float> result, Vector<float> current, ref Vector<int> resultIndex, Vector<int> currentIndex)
            {
                Vector<int> useResult = Vector.GreaterThan(result, current);
                Vector<int> equalMask = Vector.Equals(result, current);

                if (equalMask != Vector<int>.Zero)
                {
                    // bool useResult = equal && ((IsNegative(result) == IsNegative(current)) ? (resultIndex < currentIndex) : IsNegative(current));
                    Vector<float> currentNegative = IsNegative(current);
                    useResult |=
                        equalMask &
                        Vector.ConditionalSelect(Vector.Equals(IsNegative(result), currentNegative),
                            Vector.LessThan(resultIndex, currentIndex),
                            (Vector<int>)currentNegative);
                }

                result = Vector.ConditionalSelect(useResult, result, current);
                resultIndex = Vector.ConditionalSelect(useResult, resultIndex, currentIndex);
            }
        }

        private readonly struct IndexOfMaxMagnitudeOperator_Single : IIndexOfOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Invoke(ref float result, float current, int resultIndex, int currentIndex)
            {
                float resultAbs = MathF.Abs(result);
                float currentAbs = MathF.Abs(current);

                if (resultAbs == currentAbs)
                {
                    bool resultNegative = IsNegative(result);
                    if ((resultNegative == IsNegative(current)) ? (currentIndex < resultIndex) : resultNegative)
                    {
                        result = current;
                        return currentIndex;
                    }
                }
                else if (currentAbs > resultAbs)
                {
                    result = current;
                    return currentIndex;
                }

                return resultIndex;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Invoke(Vector<float> current, Vector<int> currentIndex)
            {
                float result = current[0];
                float resultAbs = MathF.Abs(result);
                int resultIndex = currentIndex[0];

                for (int i = 1; i < Vector<float>.Count; i++)
                {
                    float currentAbs = MathF.Abs(current[i]);

                    if (resultAbs == currentAbs)
                    {
                        bool resultNegative = IsNegative(result);
                        if ((resultNegative == IsNegative(current[i])) ? (currentIndex[i] < resultIndex) : resultNegative)
                        {
                            result = current[i];
                            resultAbs = currentAbs;
                            resultIndex = currentIndex[i];
                        }
                    }
                    else if (currentAbs > resultAbs)
                    {
                        result = current[i];
                        resultAbs = currentAbs;
                        resultIndex = currentIndex[i];
                    }
                }

                return resultIndex;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Invoke(ref Vector<float> result, Vector<float> current, ref Vector<int> resultIndex, Vector<int> currentIndex)
            {
                Vector<float> resultMag = Vector.Abs(result), currentMag = Vector.Abs(current);
                Vector<int> useResult = Vector.GreaterThan(resultMag, currentMag);
                Vector<int> equalMask = Vector.Equals(resultMag, currentMag);

                if (equalMask != Vector<int>.Zero)
                {
                    // bool useResult = equal && ((IsNegative(result) == IsNegative(current)) ? (resultIndex < currentIndex) : IsNegative(current));
                    Vector<float> currentNegative = IsNegative(current);
                    useResult |=
                        equalMask &
                        Vector.ConditionalSelect(Vector.Equals(IsNegative(result), currentNegative),
                            Vector.LessThan(resultIndex, currentIndex),
                            (Vector<int>)currentNegative);
                }

                result = Vector.ConditionalSelect(useResult, result, current);
                resultIndex = Vector.ConditionalSelect(useResult, resultIndex, currentIndex);
            }
        }

        private readonly struct IndexOfMinOperator_Single : IIndexOfOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Invoke(ref float result, float current, int resultIndex, int currentIndex)
            {
                if (result == current)
                {
                    bool currentNegative = IsNegative(current);
                    if ((IsNegative(result) == currentNegative) ? (currentIndex < resultIndex) : currentNegative)
                    {
                        result = current;
                        return currentIndex;
                    }
                }
                else if (current < result)
                {
                    result = current;
                    return currentIndex;
                }

                return resultIndex;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Invoke(Vector<float> current, Vector<int> currentIndex)
            {
                float result = current[0];
                int resultIndex = currentIndex[0];

                for (int i = 1; i < Vector<float>.Count; i++)
                {
                    if (current[i] == result)
                    {
                        bool currentNegative = IsNegative(current[i]);
                        if ((IsNegative(result) == currentNegative) ? (currentIndex[i] < resultIndex) : currentNegative)
                        {
                            result = current[i];
                            resultIndex = currentIndex[i];
                        }
                    }
                    else if (current[i] < result)
                    {
                        result = current[i];
                        resultIndex = currentIndex[i];
                    }
                }

                return resultIndex;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Invoke(ref Vector<float> result, Vector<float> current, ref Vector<int> resultIndex, Vector<int> currentIndex)
            {
                Vector<int> useResult = Vector.LessThan(result, current);
                Vector<int> equalMask = Vector.Equals(result, current);

                if (equalMask != Vector<int>.Zero)
                {
                    // bool useResult = equal && ((IsNegative(result) == IsNegative(current)) ? (resultIndex < currentIndex) : IsNegative(result));
                    Vector<float> resultNegative = IsNegative(result);
                    useResult |=
                        equalMask &
                        Vector.ConditionalSelect(Vector.Equals(resultNegative, IsNegative(current)),
                            Vector.LessThan(resultIndex, currentIndex),
                            (Vector<int>)resultNegative);
                }

                result = Vector.ConditionalSelect(useResult, result, current);
                resultIndex = Vector.ConditionalSelect(useResult, resultIndex, currentIndex);
            }
        }

        private readonly struct IndexOfMinMagnitudeOperator_Single : IIndexOfOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Invoke(ref float result, float current, int resultIndex, int currentIndex)
            {
                float resultAbs = MathF.Abs(result);
                float currentAbs = MathF.Abs(current);

                if (resultAbs == currentAbs)
                {
                    bool currentNegative = IsNegative(current);
                    if ((IsNegative(result) == currentNegative) ? (currentIndex < resultIndex) : currentNegative)
                    {
                        result = current;
                        return currentIndex;
                    }
                }
                else if (currentAbs < resultAbs)
                {
                    result = current;
                    return currentIndex;
                }

                return resultIndex;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Invoke(Vector<float> current, Vector<int> currentIndex)
            {
                float result = current[0];
                float resultAbs = MathF.Abs(result);
                int resultIndex = currentIndex[0];

                for (int i = 1; i < Vector<float>.Count; i++)
                {
                    float currentAbs = MathF.Abs(current[i]);

                    if (resultAbs == currentAbs)
                    {
                        bool currentNegative = IsNegative(current[i]);
                        if ((IsNegative(result) == currentNegative) ? (currentIndex[i] < resultIndex) : currentNegative)
                        {
                            result = current[i];
                            resultAbs = currentAbs;
                            resultIndex = currentIndex[i];
                        }
                    }
                    else if (currentAbs < resultAbs)
                    {
                        result = current[i];
                        resultAbs = currentAbs;
                        resultIndex = currentIndex[i];
                    }
                }

                return resultIndex;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Invoke(ref Vector<float> result, Vector<float> current, ref Vector<int> resultIndex, Vector<int> currentIndex)
            {
                Vector<float> resultMag = Vector.Abs(result), currentMag = Vector.Abs(current);
                Vector<int> useResult = Vector.LessThan(resultMag, currentMag);
                Vector<int> equalMask = Vector.Equals(resultMag, currentMag);

                if (equalMask != Vector<int>.Zero)
                {
                    // bool useResult = equal && ((IsNegative(result) == IsNegative(current)) ? (resultIndex < currentIndex) : IsNegative(result));
                    Vector<float> resultNegative = IsNegative(result);
                    useResult |=
                        equalMask &
                        Vector.ConditionalSelect(Vector.Equals(resultNegative, IsNegative(current)),
                            Vector.LessThan(resultIndex, currentIndex),
                            (Vector<int>)resultNegative);
                }

                result = Vector.ConditionalSelect(useResult, result, current);
                resultIndex = Vector.ConditionalSelect(useResult, resultIndex, currentIndex);
            }
        }

        /// <summary>MathF.Max(x, y) (but without guaranteed NaN propagation)</summary>
        private readonly struct MaxOperator_Single : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Invoke(float x, float y) =>
                x == y ?
                    (IsNegative(x) ? y : x) :
                    (y > x ? y : x);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) =>
                Vector.ConditionalSelect(Vector.Equals(x, y),
                    Vector.ConditionalSelect(IsNegative(x), y, x),
                    Vector.Max(x, y));
        }

        /// <summary>MathF.Max(x, y)</summary>
        private readonly struct MaxPropagateNaNOperator_Single : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Invoke(float x, float y) => MathF.Max(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) =>
                Vector.ConditionalSelect(Vector.Equals(x, x),
                    Vector.ConditionalSelect(Vector.Equals(y, y),
                        Vector.ConditionalSelect(Vector.Equals(x, y),
                            Vector.ConditionalSelect(IsNegative(x), y, x),
                            Vector.Max(x, y)),
                        y),
                    x);
        }

        /// <summary>Operator to get x or y based on which has the larger MathF.Abs (but NaNs may not be propagated)</summary>
        private readonly struct MaxMagnitudeOperator_Single : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Invoke(float x, float y)
            {
                float xMag = MathF.Abs(x), yMag = MathF.Abs(y);
                return
                    yMag == xMag ?
                        (IsNegative(x) ? y : x) :
                        (xMag > yMag ? x : y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector<float> Invoke(Vector<float> x, Vector<float> y)
            {
                Vector<float> xMag = Vector.Abs(x), yMag = Vector.Abs(y);
                return
                    Vector.ConditionalSelect(Vector.Equals(xMag, yMag),
                        Vector.ConditionalSelect(IsNegative(x), y, x),
                        Vector.ConditionalSelect(Vector.GreaterThan(xMag, yMag), x, y));
            }
        }

        /// <summary>Operator to get x or y based on which has the larger MathF.Abs</summary>
        private readonly struct MaxMagnitudePropagateNaNOperator_Single : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Invoke(float x, float y)
            {
                float xMag = MathF.Abs(x), yMag = MathF.Abs(y);
                return xMag > yMag || float.IsNaN(xMag) || (xMag == yMag && !IsNegative(x)) ? x : y;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector<float> Invoke(Vector<float> x, Vector<float> y)
            {
                Vector<float> xMag = Vector.Abs(x), yMag = Vector.Abs(y);
                return
                    Vector.ConditionalSelect(Vector.Equals(x, x),
                        Vector.ConditionalSelect(Vector.Equals(y, y),
                            Vector.ConditionalSelect(Vector.Equals(xMag, yMag),
                                Vector.ConditionalSelect(IsNegative(x), y, x),
                                Vector.ConditionalSelect(Vector.GreaterThan(xMag, yMag), x, y)),
                            y),
                        x);
            }
        }

        /// <summary>MathF.Min(x, y) (but NaNs may not be propagated)</summary>
        private readonly struct MinOperator_Single : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Invoke(float x, float y) =>
                x == y ?
                    (IsNegative(y) ? y : x) :
                    (y < x ? y : x);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) =>
                Vector.ConditionalSelect(Vector.Equals(x, y),
                    Vector.ConditionalSelect(IsNegative(y), y, x),
                    Vector.Min(x, y));
        }

        /// <summary>MathF.Min(x, y)</summary>
        private readonly struct MinPropagateNaNOperator_Single : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Invoke(float x, float y) => MathF.Min(x, y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector<float> Invoke(Vector<float> x, Vector<float> y) =>
                Vector.ConditionalSelect(Vector.Equals(x, x),
                    Vector.ConditionalSelect(Vector.Equals(y, y),
                        Vector.ConditionalSelect(Vector.Equals(x, y),
                            Vector.ConditionalSelect(IsNegative(x), x, y),
                            Vector.Min(x, y)),
                        y),
                    x);
        }

        /// <summary>Operator to get x or y based on which has the smaller MathF.Abs (but NaNs may not be propagated)</summary>
        private readonly struct MinMagnitudeOperator_Single : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Invoke(float x, float y)
            {
                float xMag = MathF.Abs(x), yMag = MathF.Abs(y);
                return
                    yMag == xMag ?
                        (IsNegative(y) ? y : x) :
                        (yMag < xMag ? y : x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector<float> Invoke(Vector<float> x, Vector<float> y)
            {
                Vector<float> xMag = Vector.Abs(x), yMag = Vector.Abs(y);
                return
                    Vector.ConditionalSelect(Vector.Equals(yMag, xMag),
                        Vector.ConditionalSelect(IsNegative(y), y, x),
                        Vector.ConditionalSelect(Vector.LessThan(yMag, xMag), y, x));
            }
        }

        /// <summary>Operator to get x or y based on which has the smaller MathF.Abs</summary>
        private readonly struct MinMagnitudePropagateNaNOperator_Single : IBinaryOperator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Invoke(float x, float y)
            {
                float xMag = MathF.Abs(x), yMag = MathF.Abs(y);
                return xMag < yMag || float.IsNaN(xMag) || (xMag == yMag && IsNegative(x)) ? x : y;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vector<float> Invoke(Vector<float> x, Vector<float> y)
            {
                Vector<float> xMag = Vector.Abs(x), yMag = Vector.Abs(y);

                return
                    Vector.ConditionalSelect(Vector.Equals(x, x),
                        Vector.ConditionalSelect(Vector.Equals(y, y),
                            Vector.ConditionalSelect(Vector.Equals(yMag, xMag),
                                Vector.ConditionalSelect(IsNegative(x), x, y),
                                Vector.ConditionalSelect(Vector.LessThan(xMag, yMag), x, y)),
                            y),
                        x);
            }
        }

        /// <summary>-x</summary>
        private readonly struct NegateOperator_Single : IUnaryOperator
        {
            public bool CanVectorize => true;
            public float Invoke(float x) => -x;
            public Vector<float> Invoke(Vector<float> x) => -x;
        }

        /// <summary>(x + y) * z</summary>
        private readonly struct AddMultiplyOperator_Single : ITernaryOperator
        {
            public float Invoke(float x, float y, float z) => (x + y) * z;
            public Vector<float> Invoke(Vector<float> x, Vector<float> y, Vector<float> z) => (x + y) * z;
        }

        /// <summary>(x * y) + z</summary>
        private readonly struct MultiplyAddOperator_Single : ITernaryOperator
        {
            public float Invoke(float x, float y, float z) => (x * y) + z;
            public Vector<float> Invoke(Vector<float> x, Vector<float> y, Vector<float> z) => (x * y) + z;
        }

        /// <summary>x</summary>
        private readonly struct IdentityOperator_Single : IUnaryOperator
        {
            public bool CanVectorize => true;
            public float Invoke(float x) => x;
            public Vector<float> Invoke(Vector<float> x) => x;
        }

        /// <summary>x * x</summary>
        private readonly struct SquaredOperator_Single : IUnaryOperator
        {
            public bool CanVectorize => true;
            public float Invoke(float x) => x * x;
            public Vector<float> Invoke(Vector<float> x) => x * x;
        }

        /// <summary>MathF.Abs(x)</summary>
        private readonly struct AbsoluteOperator_Single : IUnaryOperator
        {
            public bool CanVectorize => true;
            public float Invoke(float x) => MathF.Abs(x);
            public Vector<float> Invoke(Vector<float> x) => Vector.Abs(x);
        }

        /// <summary>MathF.Exp(x)</summary>
        private readonly struct ExpOperator_Single : IUnaryOperator
        {
            public bool CanVectorize => false;
            public float Invoke(float x) => MathF.Exp(x);
            public Vector<float> Invoke(Vector<float> x) =>
                // requires ShiftLeft (.NET 7+)
                throw new NotImplementedException();
        }

        /// <summary>MathF.Sinh(x)</summary>
        private readonly struct SinhOperator_Single : IUnaryOperator
        {
            public bool CanVectorize => false;
            public float Invoke(float x) => MathF.Sinh(x);
            public Vector<float> Invoke(Vector<float> x) =>
                // requires ShiftLeft (.NET 7+)
                throw new NotImplementedException();
        }

        /// <summary>MathF.Cosh(x)</summary>
        private readonly struct CoshOperator_Single : IUnaryOperator
        {
            public bool CanVectorize => false;
            public float Invoke(float x) => MathF.Cosh(x);
            public Vector<float> Invoke(Vector<float> x) =>
                // requires ShiftLeft (.NET 7+)
                throw new NotImplementedException();
        }

        /// <summary>MathF.Tanh(x)</summary>
        private readonly struct TanhOperator_Single : IUnaryOperator
        {
            public bool CanVectorize => false;
            public float Invoke(float x) => MathF.Tanh(x);
            public Vector<float> Invoke(Vector<float> x) =>
                // requires ShiftLeft (.NET 7+)
                throw new NotImplementedException();
        }

        /// <summary>MathF.Log(x)</summary>
        private readonly struct LogOperator_Single : IUnaryOperator
        {
            public bool CanVectorize => false;
            public float Invoke(float x) => MathF.Log(x);
            public Vector<float> Invoke(Vector<float> x) =>
                // requires ShiftRightArithmetic (.NET 7+)
                throw new NotImplementedException();
        }

        /// <summary>MathF.Log2(x)</summary>
        private readonly struct Log2Operator_Single : IUnaryOperator
        {
            public bool CanVectorize => false;
            public float Invoke(float x) => Log2(x);
            public Vector<float> Invoke(Vector<float> x) =>
                // requires ShiftRightArithmetic (.NET 7+)
                throw new NotImplementedException();
        }

        /// <summary>1f / (1f + MathF.Exp(-x))</summary>
        private readonly struct SigmoidOperator_Single : IUnaryOperator
        {
            public bool CanVectorize => false;
            public float Invoke(float x) => 1.0f / (1.0f + MathF.Exp(-x));
            public Vector<float> Invoke(Vector<float> x) =>
                // requires ShiftRightArithmetic (.NET 7+)
                throw new NotImplementedException();
        }

        /// <summary>Operator that takes one input value and returns a single value.</summary>
        private interface IUnaryOperator
        {
            bool CanVectorize { get; }
            float Invoke(float x);
            Vector<float> Invoke(Vector<float> x);
        }

        /// <summary>Operator that takes two input values and returns a single value.</summary>
        private interface IBinaryOperator
        {
            float Invoke(float x, float y);
            Vector<float> Invoke(Vector<float> x, Vector<float> y);
        }

        /// <summary><see cref="IBinaryOperator"/> that specializes horizontal aggregation of all elements in a vector.</summary>
        private interface IAggregationOperator : IBinaryOperator
        {
            float IdentityValue { get; }
        }

        /// <summary>Operator that takes three input values and returns a single value.</summary>
        private interface ITernaryOperator
        {
            float Invoke(float x, float y, float z);
            Vector<float> Invoke(Vector<float> x, Vector<float> y, Vector<float> z);
        }
    }
}
