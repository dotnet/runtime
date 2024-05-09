// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace System.Numerics.Tensors
{
    public static unsafe partial class TensorPrimitives
    {
        /// <summary><see cref="IBinaryOperator{T}"/> that specializes horizontal aggregation of all elements in a vector.</summary>
        private interface IAggregationOperator<T> : IBinaryOperator<T>
        {
            static abstract T Invoke(Vector128<T> x);
            static abstract T Invoke(Vector256<T> x);
            static abstract T Invoke(Vector512<T> x);

            static virtual T IdentityValue => throw new NotSupportedException();
        }

        /// <summary>Performs an aggregation over all elements in <paramref name="x"/> to produce a single-precision floating-point value.</summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TTransformOperator">Specifies the transform operation that should be applied to each element loaded from <paramref name="x"/>.</typeparam>
        /// <typeparam name="TAggregationOperator">
        /// Specifies the aggregation binary operation that should be applied to multiple values to aggregate them into a single value.
        /// The aggregation is applied after the transform is applied to each element.
        /// </typeparam>
        private static T Aggregate<T, TTransformOperator, TAggregationOperator>(
            ReadOnlySpan<T> x)
            where TTransformOperator : struct, IUnaryOperator<T, T>
            where TAggregationOperator : struct, IAggregationOperator<T>
        {
            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref T xRef = ref MemoryMarshal.GetReference(x);

            nuint remainder = (uint)x.Length;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && TTransformOperator.Vectorizable)
            {
                T result;

                if (remainder >= (uint)Vector512<T>.Count)
                {
                    result = Vectorized512(ref xRef, remainder);
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

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && TTransformOperator.Vectorizable)
            {
                T result;

                if (remainder >= (uint)Vector256<T>.Count)
                {
                    result = Vectorized256(ref xRef, remainder);
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

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && TTransformOperator.Vectorizable)
            {
                T result;

                if (remainder >= (uint)Vector128<T>.Count)
                {
                    result = Vectorized128(ref xRef, remainder);
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

            // This is the software fallback when no acceleration is available.
            // It requires no branches to hit.

            return SoftwareFallback(ref xRef, remainder);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T SoftwareFallback(ref T xRef, nuint length)
            {
                T result = TAggregationOperator.IdentityValue;

                for (nuint i = 0; i < length; i++)
                {
                    result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, i)));
                }

                return result;
            }

            static T Vectorized128(ref T xRef, nuint remainder)
            {
                Vector128<T> vresult = Vector128.Create(TAggregationOperator.IdentityValue);

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector128<T> beg = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                Vector128<T> end = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count));

                nuint misalignment = 0;

                if (remainder > (uint)(Vector128<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    {
                        T* xPtr = px;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)xPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            misalignment = ((uint)sizeof(Vector128<T>) - ((nuint)xPtr % (uint)sizeof(Vector128<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;

                            Debug.Assert(((nuint)xPtr % (uint)sizeof(Vector128<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector128<T> vector1;
                        Vector128<T> vector2;
                        Vector128<T> vector3;
                        Vector128<T> vector4;

                        // We only need to load, so there isn't a lot of benefit to doing non-temporal operations

                        while (remainder >= (uint)(Vector128<T>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)));
                            vector2 = TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)));
                            vector3 = TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)));
                            vector4 = TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We load, process, and store the next four vectors

                            vector1 = TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)));
                            vector2 = TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)));
                            vector3 = TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)));
                            vector4 = TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector128<T>.Count * 8);

                            remainder -= (uint)(Vector128<T>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                    }
                }

                // Store the first block. Handling this separately simplifies the latter code as we know
                // they come after and so we can relegate it to full blocks or the trailing elements

                beg = Vector128.ConditionalSelect(CreateAlignmentMaskVector128<T>((int)misalignment), beg, Vector128.Create(TAggregationOperator.IdentityValue));
                vresult = TAggregationOperator.Invoke(vresult, beg);

                // Process the remaining [0, Count * 7] elements via a jump table
                //
                // We end up handling any trailing elements in case 0 and in the
                // worst case end up just doing the identity operation here if there
                // were no trailing elements.

                (nuint blocks, nuint trailing) = Math.DivRem(remainder, (nuint)Vector128<T>.Count);
                blocks -= (misalignment == 0) ? 1u : 0u;
                remainder -= trailing;

                switch (blocks)
                {
                    case 7:
                        {
                            Vector128<T> vector = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 7)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector128<T> vector = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 6)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector128<T> vector = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 5)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector128<T> vector = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 4)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector128<T> vector = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 3)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector128<T> vector = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 2)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 1;
                        }

                    case 1:
                        {
                            Vector128<T> vector = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 1)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 0;
                        }

                    case 0:
                        {
                            // Store the last block, which includes any elements that wouldn't fill a full vector
                            end = Vector128.ConditionalSelect(CreateRemainderMaskVector128<T>((int)trailing), end, Vector128.Create(TAggregationOperator.IdentityValue));
                            vresult = TAggregationOperator.Invoke(vresult, end);
                            break;
                        }
                }

                return TAggregationOperator.Invoke(vresult);
            }

            static T Vectorized256(ref T xRef, nuint remainder)
            {
                Vector256<T> vresult = Vector256.Create(TAggregationOperator.IdentityValue);

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector256<T> beg = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                Vector256<T> end = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count));

                nuint misalignment = 0;

                if (remainder > (uint)(Vector256<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    {
                        T* xPtr = px;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)xPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            misalignment = ((uint)sizeof(Vector256<T>) - ((nuint)xPtr % (uint)sizeof(Vector256<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;

                            Debug.Assert(((nuint)xPtr % (uint)sizeof(Vector256<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector256<T> vector1;
                        Vector256<T> vector2;
                        Vector256<T> vector3;
                        Vector256<T> vector4;

                        // We only need to load, so there isn't a lot of benefit to doing non-temporal operations

                        while (remainder >= (uint)(Vector256<T>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)));
                            vector2 = TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)));
                            vector3 = TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)));
                            vector4 = TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We load, process, and store the next four vectors

                            vector1 = TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)));
                            vector2 = TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)));
                            vector3 = TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)));
                            vector4 = TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector256<T>.Count * 8);

                            remainder -= (uint)(Vector256<T>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                    }
                }

                // Store the first block. Handling this separately simplifies the latter code as we know
                // they come after and so we can relegate it to full blocks or the trailing elements

                beg = Vector256.ConditionalSelect(CreateAlignmentMaskVector256<T>((int)misalignment), beg, Vector256.Create(TAggregationOperator.IdentityValue));
                vresult = TAggregationOperator.Invoke(vresult, beg);

                // Process the remaining [0, Count * 7] elements via a jump table
                //
                // We end up handling any trailing elements in case 0 and in the
                // worst case end up just doing the identity operation here if there
                // were no trailing elements.

                (nuint blocks, nuint trailing) = Math.DivRem(remainder, (nuint)Vector256<T>.Count);
                blocks -= (misalignment == 0) ? 1u : 0u;
                remainder -= trailing;

                switch (blocks)
                {
                    case 7:
                        {
                            Vector256<T> vector = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 7)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector256<T> vector = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 6)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector256<T> vector = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 5)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector256<T> vector = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 4)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector256<T> vector = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 3)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector256<T> vector = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 2)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 1;
                        }

                    case 1:
                        {
                            Vector256<T> vector = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 1)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 0;
                        }

                    case 0:
                        {
                            // Store the last block, which includes any elements that wouldn't fill a full vector
                            end = Vector256.ConditionalSelect(CreateRemainderMaskVector256<T>((int)trailing), end, Vector256.Create(TAggregationOperator.IdentityValue));
                            vresult = TAggregationOperator.Invoke(vresult, end);
                            break;
                        }
                }

                return TAggregationOperator.Invoke(vresult);
            }

            static T Vectorized512(ref T xRef, nuint remainder)
            {
                Vector512<T> vresult = Vector512.Create(TAggregationOperator.IdentityValue);

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector512<T> beg = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef));
                Vector512<T> end = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)Vector512<T>.Count));

                nuint misalignment = 0;

                if (remainder > (uint)(Vector512<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    {
                        T* xPtr = px;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)xPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            misalignment = ((uint)sizeof(Vector512<T>) - ((nuint)xPtr % (uint)sizeof(Vector512<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;

                            Debug.Assert(((nuint)xPtr % (uint)sizeof(Vector512<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector512<T> vector1;
                        Vector512<T> vector2;
                        Vector512<T> vector3;
                        Vector512<T> vector4;

                        // We only need to load, so there isn't a lot of benefit to doing non-temporal operations

                        while (remainder >= (uint)(Vector512<T>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)));
                            vector2 = TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)));
                            vector3 = TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)));
                            vector4 = TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We load, process, and store the next four vectors

                            vector1 = TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)));
                            vector2 = TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)));
                            vector3 = TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)));
                            vector4 = TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector512<T>.Count * 8);

                            remainder -= (uint)(Vector512<T>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                    }
                }

                // Store the first block. Handling this separately simplifies the latter code as we know
                // they come after and so we can relegate it to full blocks or the trailing elements

                beg = Vector512.ConditionalSelect(CreateAlignmentMaskVector512<T>((int)misalignment), beg, Vector512.Create(TAggregationOperator.IdentityValue));
                vresult = TAggregationOperator.Invoke(vresult, beg);

                // Process the remaining [0, Count * 7] elements via a jump table
                //
                // We end up handling any trailing elements in case 0 and in the
                // worst case end up just doing the identity operation here if there
                // were no trailing elements.

                (nuint blocks, nuint trailing) = Math.DivRem(remainder, (nuint)Vector512<T>.Count);
                blocks -= (misalignment == 0) ? 1u : 0u;
                remainder -= trailing;

                switch (blocks)
                {
                    case 7:
                        {
                            Vector512<T> vector = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 7)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector512<T> vector = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 6)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector512<T> vector = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 5)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector512<T> vector = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 4)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector512<T> vector = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 3)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector512<T> vector = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 2)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 1;
                        }

                    case 1:
                        {
                            Vector512<T> vector = TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 1)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 0;
                        }

                    case 0:
                        {
                            // Store the last block, which includes any elements that wouldn't fill a full vector
                            end = Vector512.ConditionalSelect(CreateRemainderMaskVector512<T>((int)trailing), end, Vector512.Create(TAggregationOperator.IdentityValue));
                            vresult = TAggregationOperator.Invoke(vresult, end);
                            break;
                        }
                }

                return TAggregationOperator.Invoke(vresult);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T VectorizedSmall(ref T xRef, nuint remainder)
            {
                if (sizeof(T) == 1)
                {
                    return VectorizedSmall1(ref xRef, remainder);
                }
                else if (sizeof(T) == 2)
                {
                    return VectorizedSmall2(ref xRef, remainder);
                }
                else if (sizeof(T) == 4)
                {
                    return VectorizedSmall4(ref xRef, remainder);
                }
                else
                {
                    Debug.Assert(sizeof(T) == 8);
                    return VectorizedSmall8(ref xRef, remainder);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T VectorizedSmall1(ref T xRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 1);
                T result = TAggregationOperator.IdentityValue;

                switch (remainder)
                {
                    // Two Vector256's worth of data, with at least one element overlapping.
                    case 63:
                    case 62:
                    case 61:
                    case 60:
                    case 59:
                    case 58:
                    case 57:
                    case 56:
                    case 55:
                    case 54:
                    case 53:
                    case 52:
                    case 51:
                    case 50:
                    case 49:
                    case 48:
                    case 47:
                    case 46:
                    case 45:
                    case 44:
                    case 43:
                    case 42:
                    case 41:
                    case 40:
                    case 39:
                    case 38:
                    case 37:
                    case 36:
                    case 35:
                    case 34:
                    case 33:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                            Vector256<T> end = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count));

                            end = Vector256.ConditionalSelect(CreateRemainderMaskVector256<T>((int)(remainder % (uint)Vector256<T>.Count)), end, Vector256.Create(TAggregationOperator.IdentityValue));

                            result = TAggregationOperator.Invoke(TAggregationOperator.Invoke(beg, end));
                            break;
                        }

                    // One Vector256's worth of data.
                    case 32:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef));

                            result = TAggregationOperator.Invoke(beg);
                            break;
                        }

                    // Two Vector128's worth of data, with at least one element overlapping.
                    case 31:
                    case 30:
                    case 29:
                    case 28:
                    case 27:
                    case 26:
                    case 25:
                    case 24:
                    case 23:
                    case 22:
                    case 21:
                    case 20:
                    case 19:
                    case 18:
                    case 17:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                            Vector128<T> end = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count));

                            end = Vector128.ConditionalSelect(CreateRemainderMaskVector128<T>((int)(remainder % (uint)Vector128<T>.Count)), end, Vector128.Create(TAggregationOperator.IdentityValue));

                            result = TAggregationOperator.Invoke(TAggregationOperator.Invoke(beg, end));
                            break;
                        }

                    // One Vector128's worth of data.
                    case 16:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef));

                            result = TAggregationOperator.Invoke(beg);
                            break;
                        }

                    // Cases that are smaller than a single vector. No SIMD; just jump to the length and fall through each
                    // case to unroll the whole processing.
                    case 15:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 14)));
                        goto case 14;

                    case 14:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 13)));
                        goto case 13;

                    case 13:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 12)));
                        goto case 12;

                    case 12:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 11)));
                        goto case 11;

                    case 11:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 10)));
                        goto case 10;

                    case 10:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 9)));
                        goto case 9;

                    case 9:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 8)));
                        goto case 8;

                    case 8:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 7)));
                        goto case 7;

                    case 7:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 6)));
                        goto case 6;

                    case 6:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 5)));
                        goto case 5;

                    case 5:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 4)));
                        goto case 4;

                    case 4:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 3)));
                        goto case 3;

                    case 3:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 2)));
                        goto case 2;

                    case 2:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 1)));
                        goto case 1;

                    case 1:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(xRef));
                        goto case 0;

                    case 0:
                        break;
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T VectorizedSmall2(ref T xRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 2);
                T result = TAggregationOperator.IdentityValue;

                switch (remainder)
                {
                    // Two Vector256's worth of data, with at least one element overlapping.
                    case 31:
                    case 30:
                    case 29:
                    case 28:
                    case 27:
                    case 26:
                    case 25:
                    case 24:
                    case 23:
                    case 22:
                    case 21:
                    case 20:
                    case 19:
                    case 18:
                    case 17:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                            Vector256<T> end = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count));

                            end = Vector256.ConditionalSelect(CreateRemainderMaskVector256<T>((int)(remainder % (uint)Vector256<T>.Count)), end, Vector256.Create(TAggregationOperator.IdentityValue));

                            result = TAggregationOperator.Invoke(TAggregationOperator.Invoke(beg, end));
                            break;
                        }

                    // One Vector256's worth of data.
                    case 16:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef));

                            result = TAggregationOperator.Invoke(beg);
                            break;
                        }

                    // Two Vector128's worth of data, with at least one element overlapping.
                    case 15:
                    case 14:
                    case 13:
                    case 12:
                    case 11:
                    case 10:
                    case 9:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                            Vector128<T> end = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count));

                            end = Vector128.ConditionalSelect(CreateRemainderMaskVector128<T>((int)(remainder % (uint)Vector128<T>.Count)), end, Vector128.Create(TAggregationOperator.IdentityValue));

                            result = TAggregationOperator.Invoke(TAggregationOperator.Invoke(beg, end));
                            break;
                        }

                    // One Vector128's worth of data.
                    case 8:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef));

                            result = TAggregationOperator.Invoke(beg);
                            break;
                        }

                    // Cases that are smaller than a single vector. No SIMD; just jump to the length and fall through each
                    // case to unroll the whole processing.
                    case 7:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 6)));
                        goto case 6;

                    case 6:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 5)));
                        goto case 5;

                    case 5:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 4)));
                        goto case 4;

                    case 4:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 3)));
                        goto case 3;

                    case 3:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 2)));
                        goto case 2;

                    case 2:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 1)));
                        goto case 1;

                    case 1:
                        result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(xRef));
                        goto case 0;

                    case 0:
                        break;
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T VectorizedSmall4(ref T xRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 4);
                T result = TAggregationOperator.IdentityValue;

                switch (remainder)
                {
                    case 15:
                    case 14:
                    case 13:
                    case 12:
                    case 11:
                    case 10:
                    case 9:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                            Vector256<T> end = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count));

                            end = Vector256.ConditionalSelect(CreateRemainderMaskVector256<T>((int)(remainder % (uint)Vector256<T>.Count)), end, Vector256.Create(TAggregationOperator.IdentityValue));

                            result = TAggregationOperator.Invoke(TAggregationOperator.Invoke(beg, end));
                            break;
                        }

                    case 8:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef));

                            result = TAggregationOperator.Invoke(beg);
                            break;
                        }

                    case 7:
                    case 6:
                    case 5:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                            Vector128<T> end = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count));

                            end = Vector128.ConditionalSelect(CreateRemainderMaskVector128<T>((int)(remainder % (uint)Vector128<T>.Count)), end, Vector128.Create(TAggregationOperator.IdentityValue));

                            result = TAggregationOperator.Invoke(TAggregationOperator.Invoke(beg, end));
                            break;
                        }

                    case 4:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef));

                            result = TAggregationOperator.Invoke(beg);
                            break;
                        }

                    case 3:
                        {
                            result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 2)));
                            goto case 2;
                        }

                    case 2:
                        {
                            result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(Unsafe.Add(ref xRef, 1)));
                            goto case 1;
                        }

                    case 1:
                        {
                            result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(xRef));
                            goto case 0;
                        }

                    case 0:
                        {
                            break;
                        }
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T VectorizedSmall8(ref T xRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 8);
                T result = TAggregationOperator.IdentityValue;

                switch (remainder)
                {
                    case 7:
                    case 6:
                    case 5:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                            Vector256<T> end = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count));

                            end = Vector256.ConditionalSelect(CreateRemainderMaskVector256<T>((int)(remainder % (uint)Vector256<T>.Count)), end, Vector256.Create(TAggregationOperator.IdentityValue));

                            result = TAggregationOperator.Invoke(TAggregationOperator.Invoke(beg, end));
                            break;
                        }

                    case 4:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef));

                            result = TAggregationOperator.Invoke(beg);
                            break;
                        }

                    case 3:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                            Vector128<T> end = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count));

                            end = Vector128.ConditionalSelect(CreateRemainderMaskVector128<T>((int)(remainder % (uint)Vector128<T>.Count)), end, Vector128.Create(TAggregationOperator.IdentityValue));

                            result = TAggregationOperator.Invoke(TAggregationOperator.Invoke(beg, end));
                            break;
                        }

                    case 2:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef));

                            result = TAggregationOperator.Invoke(beg);
                            break;
                        }

                    case 1:
                        {
                            result = TAggregationOperator.Invoke(result, TTransformOperator.Invoke(xRef));
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
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TBinaryOperator">Specifies the binary operation that should be applied to the pair-wise elements loaded from <paramref name="x"/> and <paramref name="y"/>.</typeparam>
        /// <typeparam name="TAggregationOperator">
        /// Specifies the aggregation binary operation that should be applied to multiple values to aggregate them into a single value.
        /// The aggregation is applied to the results of the binary operations on the pair-wise values.
        /// </typeparam>
        private static T Aggregate<T, TBinaryOperator, TAggregationOperator>(
            ReadOnlySpan<T> x, ReadOnlySpan<T> y)
            where TBinaryOperator : struct, IBinaryOperator<T>
            where TAggregationOperator : struct, IAggregationOperator<T>
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref T xRef = ref MemoryMarshal.GetReference(x);
            ref T yRef = ref MemoryMarshal.GetReference(y);

            nuint remainder = (uint)x.Length;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && TBinaryOperator.Vectorizable)
            {
                T result;

                if (remainder >= (uint)Vector512<T>.Count)
                {
                    result = Vectorized512(ref xRef, ref yRef, remainder);
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

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && TBinaryOperator.Vectorizable)
            {
                T result;

                if (remainder >= (uint)Vector256<T>.Count)
                {
                    result = Vectorized256(ref xRef, ref yRef, remainder);
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

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && TBinaryOperator.Vectorizable)
            {
                T result;

                if (remainder >= (uint)Vector128<T>.Count)
                {
                    result = Vectorized128(ref xRef, ref yRef, remainder);
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
            static T SoftwareFallback(ref T xRef, ref T yRef, nuint length)
            {
                T result = TAggregationOperator.IdentityValue;

                for (nuint i = 0; i < length; i++)
                {
                    result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                                        Unsafe.Add(ref yRef, i)));
                }

                return result;
            }

            static T Vectorized128(ref T xRef, ref T yRef, nuint remainder)
            {
                Vector128<T> vresult = Vector128.Create(TAggregationOperator.IdentityValue);

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                          Vector128.LoadUnsafe(ref yRef));
                Vector128<T> end = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count));

                nuint misalignment = 0;

                if (remainder > (uint)(Vector128<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)xPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            misalignment = ((uint)sizeof(Vector128<T>) - ((nuint)xPtr % (uint)sizeof(Vector128<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            yPtr += misalignment;

                            Debug.Assert(((nuint)xPtr % (uint)sizeof(Vector128<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector128<T> vector1;
                        Vector128<T> vector2;
                        Vector128<T> vector3;
                        Vector128<T> vector4;

                        // We only need to load, so there isn't a lot of benefit to doing non-temporal operations

                        while (remainder >= (uint)(Vector128<T>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)),
                                                             Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 0)));
                            vector2 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)),
                                                             Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 1)));
                            vector3 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)),
                                                             Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 2)));
                            vector4 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)),
                                                             Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 3)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We load, process, and store the next four vectors

                            vector1 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)),
                                                             Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 4)));
                            vector2 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)),
                                                             Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 5)));
                            vector3 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)),
                                                             Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 6)));
                            vector4 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)),
                                                             Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 7)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector128<T>.Count * 8);
                            yPtr += (uint)(Vector128<T>.Count * 8);

                            remainder -= (uint)(Vector128<T>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                    }
                }

                // Store the first block. Handling this separately simplifies the latter code as we know
                // they come after and so we can relegate it to full blocks or the trailing elements

                beg = Vector128.ConditionalSelect(CreateAlignmentMaskVector128<T>((int)misalignment), beg, Vector128.Create(TAggregationOperator.IdentityValue));
                vresult = TAggregationOperator.Invoke(vresult, beg);

                // Process the remaining [0, Count * 7] elements via a jump table
                //
                // We end up handling any trailing elements in case 0 and in the
                // worst case end up just doing the identity operation here if there
                // were no trailing elements.

                (nuint blocks, nuint trailing) = Math.DivRem(remainder, (nuint)Vector128<T>.Count);
                blocks -= (misalignment == 0) ? 1u : 0u;
                remainder -= trailing;

                switch (blocks)
                {
                    case 7:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 7)),
                                                                         Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 7)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 6)),
                                                                         Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 6)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 5)),
                                                                         Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 5)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 4)),
                                                                         Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 4)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 3)),
                                                                         Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 3)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 2)),
                                                                         Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 2)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 1;
                        }

                    case 1:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 1)),
                                                                         Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 1)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 0;
                        }

                    case 0:
                        {
                            // Store the last block, which includes any elements that wouldn't fill a full vector
                            end = Vector128.ConditionalSelect(CreateRemainderMaskVector128<T>((int)trailing), end, Vector128.Create(TAggregationOperator.IdentityValue));
                            vresult = TAggregationOperator.Invoke(vresult, end);
                            break;
                        }
                }

                return TAggregationOperator.Invoke(vresult);
            }

            static T Vectorized256(ref T xRef, ref T yRef, nuint remainder)
            {
                Vector256<T> vresult = Vector256.Create(TAggregationOperator.IdentityValue);

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                          Vector256.LoadUnsafe(ref yRef));
                Vector256<T> end = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count));

                nuint misalignment = 0;

                if (remainder > (uint)(Vector256<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)xPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            misalignment = ((uint)sizeof(Vector256<T>) - ((nuint)xPtr % (uint)sizeof(Vector256<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            yPtr += misalignment;

                            Debug.Assert(((nuint)xPtr % (uint)sizeof(Vector256<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector256<T> vector1;
                        Vector256<T> vector2;
                        Vector256<T> vector3;
                        Vector256<T> vector4;

                        // We only need to load, so there isn't a lot of benefit to doing non-temporal operations

                        while (remainder >= (uint)(Vector256<T>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)),
                                                             Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 0)));
                            vector2 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)),
                                                             Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 1)));
                            vector3 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)),
                                                             Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 2)));
                            vector4 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)),
                                                             Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 3)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We load, process, and store the next four vectors

                            vector1 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
                                                             Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 4)));
                            vector2 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
                                                             Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 5)));
                            vector3 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
                                                             Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 6)));
                            vector4 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
                                                             Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 7)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector256<T>.Count * 8);
                            yPtr += (uint)(Vector256<T>.Count * 8);

                            remainder -= (uint)(Vector256<T>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                    }
                }

                // Store the first block. Handling this separately simplifies the latter code as we know
                // they come after and so we can relegate it to full blocks or the trailing elements

                beg = Vector256.ConditionalSelect(CreateAlignmentMaskVector256<T>((int)misalignment), beg, Vector256.Create(TAggregationOperator.IdentityValue));
                vresult = TAggregationOperator.Invoke(vresult, beg);

                // Process the remaining [0, Count * 7] elements via a jump table
                //
                // We end up handling any trailing elements in case 0 and in the
                // worst case end up just doing the identity operation here if there
                // were no trailing elements.

                (nuint blocks, nuint trailing) = Math.DivRem(remainder, (nuint)Vector256<T>.Count);
                blocks -= (misalignment == 0) ? 1u : 0u;
                remainder -= trailing;

                switch (blocks)
                {
                    case 7:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 7)),
                                                                         Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 7)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 6)),
                                                                         Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 6)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 5)),
                                                                         Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 5)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 4)),
                                                                         Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 4)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 3)),
                                                                         Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 3)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 2)),
                                                                         Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 2)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 1;
                        }

                    case 1:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 1)),
                                                                         Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 1)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 0;
                        }

                    case 0:
                        {
                            // Store the last block, which includes any elements that wouldn't fill a full vector
                            end = Vector256.ConditionalSelect(CreateRemainderMaskVector256<T>((int)trailing), end, Vector256.Create(TAggregationOperator.IdentityValue));
                            vresult = TAggregationOperator.Invoke(vresult, end);
                            break;
                        }
                }

                return TAggregationOperator.Invoke(vresult);
            }

            static T Vectorized512(ref T xRef, ref T yRef, nuint remainder)
            {
                Vector512<T> vresult = Vector512.Create(TAggregationOperator.IdentityValue);

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector512<T> beg = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef),
                                                          Vector512.LoadUnsafe(ref yRef));
                Vector512<T> end = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)Vector512<T>.Count),
                                                          Vector512.LoadUnsafe(ref yRef, remainder - (uint)Vector512<T>.Count));

                nuint misalignment = 0;

                if (remainder > (uint)(Vector512<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)xPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            misalignment = ((uint)sizeof(Vector512<T>) - ((nuint)xPtr % (uint)sizeof(Vector512<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            yPtr += misalignment;

                            Debug.Assert(((nuint)xPtr % (uint)sizeof(Vector512<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector512<T> vector1;
                        Vector512<T> vector2;
                        Vector512<T> vector3;
                        Vector512<T> vector4;

                        // We only need to load, so there isn't a lot of benefit to doing non-temporal operations

                        while (remainder >= (uint)(Vector512<T>.Count * 8))
                        {
                            // We load, process, and store the first four vectors

                            vector1 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)),
                                                             Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 0)));
                            vector2 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)),
                                                             Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 1)));
                            vector3 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)),
                                                             Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 2)));
                            vector4 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)),
                                                             Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 3)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We load, process, and store the next four vectors

                            vector1 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)),
                                                             Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 4)));
                            vector2 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)),
                                                             Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 5)));
                            vector3 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)),
                                                             Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 6)));
                            vector4 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)),
                                                             Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 7)));

                            vresult = TAggregationOperator.Invoke(vresult, vector1);
                            vresult = TAggregationOperator.Invoke(vresult, vector2);
                            vresult = TAggregationOperator.Invoke(vresult, vector3);
                            vresult = TAggregationOperator.Invoke(vresult, vector4);

                            // We adjust the source and destination references, then update
                            // the count of remaining elements to process.

                            xPtr += (uint)(Vector512<T>.Count * 8);
                            yPtr += (uint)(Vector512<T>.Count * 8);

                            remainder -= (uint)(Vector512<T>.Count * 8);
                        }

                        // Adjusting the refs here allows us to avoid pinning for very small inputs

                        xRef = ref *xPtr;
                        yRef = ref *yPtr;
                    }
                }

                // Store the first block. Handling this separately simplifies the latter code as we know
                // they come after and so we can relegate it to full blocks or the trailing elements

                beg = Vector512.ConditionalSelect(CreateAlignmentMaskVector512<T>((int)misalignment), beg, Vector512.Create(TAggregationOperator.IdentityValue));
                vresult = TAggregationOperator.Invoke(vresult, beg);

                // Process the remaining [0, Count * 7] elements via a jump table
                //
                // We end up handling any trailing elements in case 0 and in the
                // worst case end up just doing the identity operation here if there
                // were no trailing elements.

                (nuint blocks, nuint trailing) = Math.DivRem(remainder, (nuint)Vector512<T>.Count);
                blocks -= (misalignment == 0) ? 1u : 0u;
                remainder -= trailing;

                switch (blocks)
                {
                    case 7:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 7)),
                                                                         Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 7)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 6)),
                                                                         Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 6)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 5)),
                                                                         Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 5)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 4)),
                                                                         Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 4)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 3)),
                                                                         Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 3)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 2)),
                                                                         Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 2)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 1;
                        }

                    case 1:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 1)),
                                                                         Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 1)));
                            vresult = TAggregationOperator.Invoke(vresult, vector);
                            goto case 0;
                        }

                    case 0:
                        {
                            // Store the last block, which includes any elements that wouldn't fill a full vector
                            end = Vector512.ConditionalSelect(CreateRemainderMaskVector512<T>((int)trailing), end, Vector512.Create(TAggregationOperator.IdentityValue));
                            vresult = TAggregationOperator.Invoke(vresult, end);
                            break;
                        }
                }

                return TAggregationOperator.Invoke(vresult);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T VectorizedSmall(ref T xRef, ref T yRef, nuint remainder)
            {
                if (sizeof(T) == 1)
                {
                    return VectorizedSmall1(ref xRef, ref yRef, remainder);
                }
                else if (sizeof(T) == 2)
                {
                    return VectorizedSmall2(ref xRef, ref yRef, remainder);
                }
                else if (sizeof(T) == 4)
                {
                    return VectorizedSmall4(ref xRef, ref yRef, remainder);
                }
                else
                {
                    Debug.Assert(sizeof(T) == 8);
                    return VectorizedSmall8(ref xRef, ref yRef, remainder);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T VectorizedSmall1(ref T xRef, ref T yRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 1);
                T result = TAggregationOperator.IdentityValue;

                switch (remainder)
                {
                    // Two Vector256's worth of data, with at least one element overlapping.
                    case 63:
                    case 62:
                    case 61:
                    case 60:
                    case 59:
                    case 58:
                    case 57:
                    case 56:
                    case 55:
                    case 54:
                    case 53:
                    case 52:
                    case 51:
                    case 50:
                    case 49:
                    case 48:
                    case 47:
                    case 46:
                    case 45:
                    case 44:
                    case 43:
                    case 42:
                    case 41:
                    case 40:
                    case 39:
                    case 38:
                    case 37:
                    case 36:
                    case 35:
                    case 34:
                    case 33:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                      Vector256.LoadUnsafe(ref yRef));
                            Vector256<T> end = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                      Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count));

                            end = Vector256.ConditionalSelect(CreateRemainderMaskVector256<T>((int)(remainder % (uint)Vector256<T>.Count)), end, Vector256.Create(TAggregationOperator.IdentityValue));

                            result = TAggregationOperator.Invoke(TAggregationOperator.Invoke(beg, end));
                            break;
                        }

                    // One Vector256's worth of data.
                    case 32:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                      Vector256.LoadUnsafe(ref yRef));

                            result = TAggregationOperator.Invoke(beg);
                            break;
                        }

                    // Two Vector128's worth of data, with at least one element overlapping.
                    case 31:
                    case 30:
                    case 29:
                    case 28:
                    case 27:
                    case 26:
                    case 25:
                    case 24:
                    case 23:
                    case 22:
                    case 21:
                    case 20:
                    case 19:
                    case 18:
                    case 17:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                      Vector128.LoadUnsafe(ref yRef));
                            Vector128<T> end = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                      Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count));

                            end = Vector128.ConditionalSelect(CreateRemainderMaskVector128<T>((int)(remainder % (uint)Vector128<T>.Count)), end, Vector128.Create(TAggregationOperator.IdentityValue));

                            result = TAggregationOperator.Invoke(TAggregationOperator.Invoke(beg, end));
                            break;
                        }

                    // One Vector128's worth of data.
                    case 16:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                      Vector128.LoadUnsafe(ref yRef));

                            result = TAggregationOperator.Invoke(beg);
                            break;
                        }

                    // Cases that are smaller than a single vector. No SIMD; just jump to the length and fall through each
                    // case to unroll the whole processing.
                    case 15:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 14), Unsafe.Add(ref yRef, 14)));
                        goto case 14;

                    case 14:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 13), Unsafe.Add(ref yRef, 13)));
                        goto case 13;

                    case 13:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 12), Unsafe.Add(ref yRef, 12)));
                        goto case 12;

                    case 12:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 11), Unsafe.Add(ref yRef, 11)));
                        goto case 11;

                    case 11:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 10), Unsafe.Add(ref yRef, 10)));
                        goto case 10;

                    case 10:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 9), Unsafe.Add(ref yRef, 9)));
                        goto case 9;

                    case 9:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 8), Unsafe.Add(ref yRef, 8)));
                        goto case 8;

                    case 8:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 7), Unsafe.Add(ref yRef, 7)));
                        goto case 7;

                    case 7:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 6), Unsafe.Add(ref yRef, 6)));
                        goto case 6;

                    case 6:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 5), Unsafe.Add(ref yRef, 5)));
                        goto case 5;

                    case 5:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 4), Unsafe.Add(ref yRef, 4)));
                        goto case 4;

                    case 4:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 3), Unsafe.Add(ref yRef, 3)));
                        goto case 3;

                    case 3:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 2), Unsafe.Add(ref yRef, 2)));
                        goto case 2;

                    case 2:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 1), Unsafe.Add(ref yRef, 1)));
                        goto case 1;

                    case 1:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(xRef, yRef));
                        goto case 0;

                    case 0:
                        break;
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T VectorizedSmall2(ref T xRef, ref T yRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 2);
                T result = TAggregationOperator.IdentityValue;

                switch (remainder)
                {
                    // Two Vector256's worth of data, with at least one element overlapping.
                    case 31:
                    case 30:
                    case 29:
                    case 28:
                    case 27:
                    case 26:
                    case 25:
                    case 24:
                    case 23:
                    case 22:
                    case 21:
                    case 20:
                    case 19:
                    case 18:
                    case 17:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                      Vector256.LoadUnsafe(ref yRef));
                            Vector256<T> end = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                      Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count));

                            end = Vector256.ConditionalSelect(CreateRemainderMaskVector256<T>((int)(remainder % (uint)Vector256<T>.Count)), end, Vector256.Create(TAggregationOperator.IdentityValue));

                            result = TAggregationOperator.Invoke(TAggregationOperator.Invoke(beg, end));
                            break;
                        }

                    // One Vector256's worth of data.
                    case 16:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                      Vector256.LoadUnsafe(ref yRef));

                            result = TAggregationOperator.Invoke(beg);
                            break;
                        }

                    // Two Vector128's worth of data, with at least one element overlapping.
                    case 15:
                    case 14:
                    case 13:
                    case 12:
                    case 11:
                    case 10:
                    case 9:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                      Vector128.LoadUnsafe(ref yRef));
                            Vector128<T> end = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                      Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count));

                            end = Vector128.ConditionalSelect(CreateRemainderMaskVector128<T>((int)(remainder % (uint)Vector128<T>.Count)), end, Vector128.Create(TAggregationOperator.IdentityValue));

                            result = TAggregationOperator.Invoke(TAggregationOperator.Invoke(beg, end));
                            break;
                        }

                    // One Vector128's worth of data.
                    case 8:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                      Vector128.LoadUnsafe(ref yRef));

                            result = TAggregationOperator.Invoke(beg);
                            break;
                        }

                    // Cases that are smaller than a single vector. No SIMD; just jump to the length and fall through each
                    // case to unroll the whole processing.
                    case 7:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 6), Unsafe.Add(ref yRef, 6)));
                        goto case 6;

                    case 6:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 5), Unsafe.Add(ref yRef, 5)));
                        goto case 5;

                    case 5:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 4), Unsafe.Add(ref yRef, 4)));
                        goto case 4;

                    case 4:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 3), Unsafe.Add(ref yRef, 3)));
                        goto case 3;

                    case 3:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 2), Unsafe.Add(ref yRef, 2)));
                        goto case 2;

                    case 2:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 1), Unsafe.Add(ref yRef, 1)));
                        goto case 1;

                    case 1:
                        result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(xRef, yRef));
                        goto case 0;

                    case 0:
                        break;
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T VectorizedSmall4(ref T xRef, ref T yRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 4);
                T result = TAggregationOperator.IdentityValue;

                switch (remainder)
                {
                    case 15:
                    case 14:
                    case 13:
                    case 12:
                    case 11:
                    case 10:
                    case 9:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                      Vector256.LoadUnsafe(ref yRef));
                            Vector256<T> end = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                      Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count));

                            end = Vector256.ConditionalSelect(CreateRemainderMaskVector256<T>((int)(remainder % (uint)Vector256<T>.Count)), end, Vector256.Create(TAggregationOperator.IdentityValue));

                            result = TAggregationOperator.Invoke(TAggregationOperator.Invoke(beg, end));
                            break;
                        }

                    case 8:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                      Vector256.LoadUnsafe(ref yRef));

                            result = TAggregationOperator.Invoke(beg);
                            break;
                        }

                    case 7:
                    case 6:
                    case 5:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                      Vector128.LoadUnsafe(ref yRef));
                            Vector128<T> end = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                      Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count));

                            end = Vector128.ConditionalSelect(CreateRemainderMaskVector128<T>((int)(remainder % (uint)Vector128<T>.Count)), end, Vector128.Create(TAggregationOperator.IdentityValue));

                            result = TAggregationOperator.Invoke(TAggregationOperator.Invoke(beg, end));
                            break;
                        }

                    case 4:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                      Vector128.LoadUnsafe(ref yRef));

                            result = TAggregationOperator.Invoke(beg);
                            break;
                        }

                    case 3:
                        {
                            result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 2),
                                                                                                Unsafe.Add(ref yRef, 2)));
                            goto case 2;
                        }

                    case 2:
                        {
                            result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 1),
                                                                                                Unsafe.Add(ref yRef, 1)));
                            goto case 1;
                        }

                    case 1:
                        {
                            result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(xRef, yRef));
                            goto case 0;
                        }

                    case 0:
                        {
                            break;
                        }
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T VectorizedSmall8(ref T xRef, ref T yRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 8);
                T result = TAggregationOperator.IdentityValue;

                switch (remainder)
                {
                    case 7:
                    case 6:
                    case 5:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                      Vector256.LoadUnsafe(ref yRef));
                            Vector256<T> end = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                      Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count));

                            end = Vector256.ConditionalSelect(CreateRemainderMaskVector256<T>((int)(remainder % (uint)Vector256<T>.Count)), end, Vector256.Create(TAggregationOperator.IdentityValue));

                            result = TAggregationOperator.Invoke(TAggregationOperator.Invoke(beg, end));
                            break;
                        }

                    case 4:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                      Vector256.LoadUnsafe(ref yRef));

                            result = TAggregationOperator.Invoke(beg);
                            break;
                        }

                    case 3:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                      Vector128.LoadUnsafe(ref yRef));
                            Vector128<T> end = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                      Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count));

                            end = Vector128.ConditionalSelect(CreateRemainderMaskVector128<T>((int)(remainder % (uint)Vector128<T>.Count)), end, Vector128.Create(TAggregationOperator.IdentityValue));

                            result = TAggregationOperator.Invoke(TAggregationOperator.Invoke(beg, end));
                            break;
                        }

                    case 2:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                      Vector128.LoadUnsafe(ref yRef));

                            result = TAggregationOperator.Invoke(beg);
                            break;
                        }

                    case 1:
                        {
                            result = TAggregationOperator.Invoke(result, TBinaryOperator.Invoke(xRef, yRef));
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

        /// <summary>
        /// Gets a vector mask that will be all-ones-set for the last <paramref name="count"/> elements
        /// and zero for all other elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<T> CreateAlignmentMaskVector128<T>(int count)
        {
            if (Unsafe.SizeOf<T>() == 1)
            {
                return Vector128.LoadUnsafe(
                    ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(AlignmentByteMask_64x65)),
                    (uint)(count * 64));
            }

            if (Unsafe.SizeOf<T>() == 2)
            {
                return Vector128.LoadUnsafe(
                    ref Unsafe.As<ushort, T>(ref MemoryMarshal.GetReference(AlignmentUInt16Mask_32x33)),
                    (uint)(count * 32));
            }

            if (Unsafe.SizeOf<T>() == 4)
            {
                return Vector128.LoadUnsafe(
                    ref Unsafe.As<uint, T>(ref MemoryMarshal.GetReference(AlignmentUInt32Mask_16x17)),
                    (uint)(count * 16));
            }

            Debug.Assert(Unsafe.SizeOf<T>() == 8);
            {
                return Vector128.LoadUnsafe(
                    ref Unsafe.As<ulong, T>(ref MemoryMarshal.GetReference(AlignmentUInt64Mask_8x9)),
                    (uint)(count * 8));
            }
        }

        /// <summary>
        /// Gets a vector mask that will be all-ones-set for the last <paramref name="count"/> elements
        /// and zero for all other elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<T> CreateAlignmentMaskVector256<T>(int count)
        {
            if (Unsafe.SizeOf<T>() == 1)
            {
                return Vector256.LoadUnsafe(
                    ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(AlignmentByteMask_64x65)),
                    (uint)(count * 64));
            }

            if (Unsafe.SizeOf<T>() == 2)
            {
                return Vector256.LoadUnsafe(
                    ref Unsafe.As<ushort, T>(ref MemoryMarshal.GetReference(AlignmentUInt16Mask_32x33)),
                    (uint)(count * 32));
            }

            if (Unsafe.SizeOf<T>() == 4)
            {
                return Vector256.LoadUnsafe(
                    ref Unsafe.As<uint, T>(ref MemoryMarshal.GetReference(AlignmentUInt32Mask_16x17)),
                    (uint)(count * 16));
            }

            Debug.Assert(Unsafe.SizeOf<T>() == 8);
            {
                return Vector256.LoadUnsafe(
                    ref Unsafe.As<ulong, T>(ref MemoryMarshal.GetReference(AlignmentUInt64Mask_8x9)),
                    (uint)(count * 8));
            }
        }

        /// <summary>
        /// Gets a vector mask that will be all-ones-set for the last <paramref name="count"/> elements
        /// and zero for all other elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<T> CreateAlignmentMaskVector512<T>(int count)
        {
            if (Unsafe.SizeOf<T>() == 1)
            {
                return Vector512.LoadUnsafe(
                    ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(AlignmentByteMask_64x65)),
                    (uint)(count * 64));
            }

            if (Unsafe.SizeOf<T>() == 2)
            {
                return Vector512.LoadUnsafe(
                    ref Unsafe.As<ushort, T>(ref MemoryMarshal.GetReference(AlignmentUInt16Mask_32x33)),
                    (uint)(count * 32));
            }

            if (Unsafe.SizeOf<T>() == 4)
            {
                return Vector512.LoadUnsafe(
                    ref Unsafe.As<uint, T>(ref MemoryMarshal.GetReference(AlignmentUInt32Mask_16x17)),
                    (uint)(count * 16));
            }

            Debug.Assert(Unsafe.SizeOf<T>() == 8);
            {
                return Vector512.LoadUnsafe(
                    ref Unsafe.As<ulong, T>(ref MemoryMarshal.GetReference(AlignmentUInt64Mask_8x9)),
                    (uint)(count * 8));
            }
        }

        /// <summary>
        /// Gets a vector mask that will be all-ones-set for the last <paramref name="count"/> elements
        /// and zero for all other elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<T> CreateRemainderMaskVector128<T>(int count)
        {
            if (Unsafe.SizeOf<T>() == 1)
            {
                return Vector128.LoadUnsafe(
                    ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(RemainderByteMask_64x65)),
                    (uint)(count * 64) + 48); // last 16 bytes in the row
            }

            if (Unsafe.SizeOf<T>() == 2)
            {
                return Vector128.LoadUnsafe(
                    ref Unsafe.As<ushort, T>(ref MemoryMarshal.GetReference(RemainderUInt16Mask_32x33)),
                    (uint)(count * 32) + 24); // last 8 shorts in the row
            }

            if (Unsafe.SizeOf<T>() == 4)
            {
                return Vector128.LoadUnsafe(
                    ref Unsafe.As<uint, T>(ref MemoryMarshal.GetReference(RemainderUInt32Mask_16x17)),
                    (uint)(count * 16) + 12); // last 4 ints in the row
            }

            Debug.Assert(Unsafe.SizeOf<T>() == 8);
            {
                return Vector128.LoadUnsafe(
                    ref Unsafe.As<ulong, T>(ref MemoryMarshal.GetReference(RemainderUInt64Mask_8x9)),
                    (uint)(count * 8) + 6); // last 2 longs in the row
            }
        }

        /// <summary>
        /// Gets a vector mask that will be all-ones-set for the last <paramref name="count"/> elements
        /// and zero for all other elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<T> CreateRemainderMaskVector256<T>(int count)
        {
            if (Unsafe.SizeOf<T>() == 1)
            {
                return Vector256.LoadUnsafe(
                    ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(RemainderByteMask_64x65)),
                    (uint)(count * 64) + 32); // last 32 bytes in the row
            }

            if (Unsafe.SizeOf<T>() == 2)
            {
                return Vector256.LoadUnsafe(
                    ref Unsafe.As<ushort, T>(ref MemoryMarshal.GetReference(RemainderUInt16Mask_32x33)),
                    (uint)(count * 32) + 16); // last 16 shorts in the row
            }

            if (Unsafe.SizeOf<T>() == 4)
            {
                return Vector256.LoadUnsafe(
                    ref Unsafe.As<uint, T>(ref MemoryMarshal.GetReference(RemainderUInt32Mask_16x17)),
                    (uint)(count * 16) + 8); // last 8 ints in the row
            }

            Debug.Assert(Unsafe.SizeOf<T>() == 8);
            {
                return Vector256.LoadUnsafe(
                    ref Unsafe.As<ulong, T>(ref MemoryMarshal.GetReference(RemainderUInt64Mask_8x9)),
                    (uint)(count * 8) + 4); // last 4 longs in the row
            }
        }

        /// <summary>
        /// Gets a vector mask that will be all-ones-set for the last <paramref name="count"/> elements
        /// and zero for all other elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<T> CreateRemainderMaskVector512<T>(int count)
        {
            if (Unsafe.SizeOf<T>() == 1)
            {
                return Vector512.LoadUnsafe(
                    ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(RemainderByteMask_64x65)),
                    (uint)(count * 64));
            }

            if (Unsafe.SizeOf<T>() == 2)
            {
                return Vector512.LoadUnsafe(
                    ref Unsafe.As<ushort, T>(ref MemoryMarshal.GetReference(RemainderUInt16Mask_32x33)),
                    (uint)(count * 32));
            }

            if (Unsafe.SizeOf<T>() == 4)
            {
                return Vector512.LoadUnsafe(
                    ref Unsafe.As<uint, T>(ref MemoryMarshal.GetReference(RemainderUInt32Mask_16x17)),
                    (uint)(count * 16));
            }

            Debug.Assert(Unsafe.SizeOf<T>() == 8);
            {
                return Vector512.LoadUnsafe(
                    ref Unsafe.As<ulong, T>(ref MemoryMarshal.GetReference(RemainderUInt64Mask_8x9)),
                    (uint)(count * 8));
            }
        }
    }
}
