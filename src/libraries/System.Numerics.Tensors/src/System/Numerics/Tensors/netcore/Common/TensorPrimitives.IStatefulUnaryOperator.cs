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
        /// <summary>Operator that takes one input value and returns a single value.</summary>
        private interface IStatefulUnaryOperator<T>
        {
            static abstract bool Vectorizable { get; }
            T Invoke(T x);
            Vector128<T> Invoke(Vector128<T> x);
            Vector256<T> Invoke(Vector256<T> x);
            Vector512<T> Invoke(Vector512<T> x);
        }

        /// <summary>Performs an element-wise operation on <paramref name="x"/> and writes the results to <paramref name="destination"/>.</summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TStatefulUnaryOperator">Specifies the operation to perform on each element loaded from <paramref name="x"/>.</typeparam>
        private static void InvokeSpanIntoSpan<T, TStatefulUnaryOperator>(
            ReadOnlySpan<T> x, TStatefulUnaryOperator op, Span<T> destination)
            where TStatefulUnaryOperator : struct, IStatefulUnaryOperator<T>
        {
            // NOTE: This implementation is an exact copy of InvokeSpanIntoSpan<T, TUnaryOperator>,
            // except it accepts an operator that carries state with it, using instance rather than
            // static invocation methods.

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ValidateInputOutputSpanNonOverlapping(x, destination);

            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref T xRef = ref MemoryMarshal.GetReference(x);
            ref T dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)x.Length;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && TStatefulUnaryOperator.Vectorizable)
            {
                if (remainder >= (uint)Vector512<T>.Count)
                {
                    Vectorized512(ref xRef, ref dRef, remainder, op);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref dRef, remainder, op);
                }

                return;
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && TStatefulUnaryOperator.Vectorizable)
            {
                if (remainder >= (uint)Vector256<T>.Count)
                {
                    Vectorized256(ref xRef, ref dRef, remainder, op);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref dRef, remainder, op);
                }

                return;
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && TStatefulUnaryOperator.Vectorizable)
            {
                if (remainder >= (uint)Vector128<T>.Count)
                {
                    Vectorized128(ref xRef, ref dRef, remainder, op);
                }
                else
                {
                    // We have less than a vector and so we can only handle this as scalar. To do this
                    // efficiently, we simply have a small jump table and fallthrough. So we get a simple
                    // length check, single jump, and then linear execution.

                    VectorizedSmall(ref xRef, ref dRef, remainder, op);
                }

                return;
            }

            // This is the software fallback when no acceleration is available
            // It requires no branches to hit

            SoftwareFallback(ref xRef, ref dRef, remainder, op);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void SoftwareFallback(ref T xRef, ref T dRef, nuint length, TStatefulUnaryOperator op)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, i) = op.Invoke(Unsafe.Add(ref xRef, i));
                }
            }

            static void Vectorized128(ref T xRef, ref T dRef, nuint remainder, TStatefulUnaryOperator op)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector128<T> beg = op.Invoke(Vector128.LoadUnsafe(ref xRef));
                Vector128<T> end = op.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count));

                if (remainder > (uint)(Vector128<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector128<T>) - ((nuint)dPtr % (uint)sizeof(Vector128<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector128<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector128<T> vector1;
                        Vector128<T> vector2;
                        Vector128<T> vector3;
                        Vector128<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector128<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = op.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)));
                                vector2 = op.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)));
                                vector3 = op.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)));
                                vector4 = op.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = op.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)));
                                vector2 = op.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)));
                                vector3 = op.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)));
                                vector4 = op.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector128<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = op.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)));
                                vector2 = op.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)));
                                vector3 = op.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)));
                                vector4 = op.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = op.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)));
                                vector2 = op.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)));
                                vector3 = op.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)));
                                vector4 = op.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
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
                remainder = (remainder + (uint)(Vector128<T>.Count - 1)) & (nuint)(-Vector128<T>.Count);

                switch (remainder / (uint)Vector128<T>.Count)
                {
                    case 8:
                        {
                            Vector128<T> vector = op.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 8)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector128<T> vector = op.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 7)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector128<T> vector = op.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 6)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector128<T> vector = op.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 5)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector128<T> vector = op.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 4)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector128<T> vector = op.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 3)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector128<T> vector = op.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 2)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 2));
                            goto case 1;
                        }

                    case 1:
                        {
                            // Store the last block, which includes any elements that wouldn't fill a full vector
                            end.StoreUnsafe(ref dRef, endIndex - (uint)Vector128<T>.Count);
                            goto case 0;
                        }

                    case 0:
                        {
                            // Store the first block, which includes any elements preceding the first aligned block
                            beg.StoreUnsafe(ref dRefBeg);
                            break;
                        }
                }
            }

            static void Vectorized256(ref T xRef, ref T dRef, nuint remainder, TStatefulUnaryOperator op)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector256<T> beg = op.Invoke(Vector256.LoadUnsafe(ref xRef));
                Vector256<T> end = op.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count));

                if (remainder > (uint)(Vector256<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector256<T>) - ((nuint)dPtr % (uint)sizeof(Vector256<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector256<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector256<T> vector1;
                        Vector256<T> vector2;
                        Vector256<T> vector3;
                        Vector256<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector256<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = op.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)));
                                vector2 = op.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)));
                                vector3 = op.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)));
                                vector4 = op.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = op.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)));
                                vector2 = op.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)));
                                vector3 = op.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)));
                                vector4 = op.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector256<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = op.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)));
                                vector2 = op.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)));
                                vector3 = op.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)));
                                vector4 = op.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = op.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)));
                                vector2 = op.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)));
                                vector3 = op.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)));
                                vector4 = op.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
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
                remainder = (remainder + (uint)(Vector256<T>.Count - 1)) & (nuint)(-Vector256<T>.Count);

                switch (remainder / (uint)Vector256<T>.Count)
                {
                    case 8:
                        {
                            Vector256<T> vector = op.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 8)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector256<T> vector = op.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 7)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector256<T> vector = op.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 6)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector256<T> vector = op.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 5)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector256<T> vector = op.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 4)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector256<T> vector = op.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 3)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector256<T> vector = op.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 2)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 2));
                            goto case 1;
                        }

                    case 1:
                        {
                            // Store the last block, which includes any elements that wouldn't fill a full vector
                            end.StoreUnsafe(ref dRef, endIndex - (uint)Vector256<T>.Count);
                            goto case 0;
                        }

                    case 0:
                        {
                            // Store the first block, which includes any elements preceding the first aligned block
                            beg.StoreUnsafe(ref dRefBeg);
                            break;
                        }
                }
            }

            static void Vectorized512(ref T xRef, ref T dRef, nuint remainder, TStatefulUnaryOperator op)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector512<T> beg = op.Invoke(Vector512.LoadUnsafe(ref xRef));
                Vector512<T> end = op.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)Vector512<T>.Count));

                if (remainder > (uint)(Vector512<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(T)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector512<T>) - ((nuint)dPtr % (uint)sizeof(Vector512<T>))) / (uint)sizeof(T);

                            xPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector512<T>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector512<T> vector1;
                        Vector512<T> vector2;
                        Vector512<T> vector3;
                        Vector512<T> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(T))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector512<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = op.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)));
                                vector2 = op.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)));
                                vector3 = op.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)));
                                vector4 = op.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = op.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)));
                                vector2 = op.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)));
                                vector3 = op.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)));
                                vector4 = op.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector512<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = op.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)));
                                vector2 = op.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)));
                                vector3 = op.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)));
                                vector4 = op.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = op.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)));
                                vector2 = op.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)));
                                vector3 = op.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)));
                                vector4 = op.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
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
                remainder = (remainder + (uint)(Vector512<T>.Count - 1)) & (nuint)(-Vector512<T>.Count);

                switch (remainder / (uint)Vector512<T>.Count)
                {
                    case 8:
                        {
                            Vector512<T> vector = op.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 8)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector512<T> vector = op.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 7)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector512<T> vector = op.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 6)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector512<T> vector = op.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 5)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector512<T> vector = op.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 4)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector512<T> vector = op.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 3)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector512<T> vector = op.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 2)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 2));
                            goto case 1;
                        }

                    case 1:
                        {
                            // Store the last block, which includes any elements that wouldn't fill a full vector
                            end.StoreUnsafe(ref dRef, endIndex - (uint)Vector512<T>.Count);
                            goto case 0;
                        }

                    case 0:
                        {
                            // Store the first block, which includes any elements preceding the first aligned block
                            beg.StoreUnsafe(ref dRefBeg);
                            break;
                        }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall(ref T xRef, ref T dRef, nuint remainder, TStatefulUnaryOperator op)
            {
                if (sizeof(T) == 1)
                {
                    VectorizedSmall1(ref xRef, ref dRef, remainder, op);
                }
                else if (sizeof(T) == 2)
                {
                    VectorizedSmall2(ref xRef, ref dRef, remainder, op);
                }
                else if (sizeof(T) == 4)
                {
                    VectorizedSmall4(ref xRef, ref dRef, remainder, op);
                }
                else
                {
                    Debug.Assert(sizeof(T) == 8);
                    VectorizedSmall8(ref xRef, ref dRef, remainder, op);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall1(ref T xRef, ref T dRef, nuint remainder, TStatefulUnaryOperator op)
            {
                Debug.Assert(sizeof(T) == 1);

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

                            Vector256<T> beg = op.Invoke(Vector256.LoadUnsafe(ref xRef));
                            Vector256<T> end = op.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    // One Vector256's worth of data.
                    case 32:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = op.Invoke(Vector256.LoadUnsafe(ref xRef));
                            beg.StoreUnsafe(ref dRef);

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

                            Vector128<T> beg = op.Invoke(Vector128.LoadUnsafe(ref xRef));
                            Vector128<T> end = op.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    // One Vector128's worth of data.
                    case 16:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = op.Invoke(Vector128.LoadUnsafe(ref xRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    // Cases that are smaller than a single vector. No SIMD; just jump to the length and fall through each
                    // case to unroll the whole processing.
                    case 15:
                        Unsafe.Add(ref dRef, 14) = op.Invoke(Unsafe.Add(ref xRef, 14));
                        goto case 14;

                    case 14:
                        Unsafe.Add(ref dRef, 13) = op.Invoke(Unsafe.Add(ref xRef, 13));
                        goto case 13;

                    case 13:
                        Unsafe.Add(ref dRef, 12) = op.Invoke(Unsafe.Add(ref xRef, 12));
                        goto case 12;

                    case 12:
                        Unsafe.Add(ref dRef, 11) = op.Invoke(Unsafe.Add(ref xRef, 11));
                        goto case 11;

                    case 11:
                        Unsafe.Add(ref dRef, 10) = op.Invoke(Unsafe.Add(ref xRef, 10));
                        goto case 10;

                    case 10:
                        Unsafe.Add(ref dRef, 9) = op.Invoke(Unsafe.Add(ref xRef, 9));
                        goto case 9;

                    case 9:
                        Unsafe.Add(ref dRef, 8) = op.Invoke(Unsafe.Add(ref xRef, 8));
                        goto case 8;

                    case 8:
                        Unsafe.Add(ref dRef, 7) = op.Invoke(Unsafe.Add(ref xRef, 7));
                        goto case 7;

                    case 7:
                        Unsafe.Add(ref dRef, 6) = op.Invoke(Unsafe.Add(ref xRef, 6));
                        goto case 6;

                    case 6:
                        Unsafe.Add(ref dRef, 5) = op.Invoke(Unsafe.Add(ref xRef, 5));
                        goto case 5;

                    case 5:
                        Unsafe.Add(ref dRef, 4) = op.Invoke(Unsafe.Add(ref xRef, 4));
                        goto case 4;

                    case 4:
                        Unsafe.Add(ref dRef, 3) = op.Invoke(Unsafe.Add(ref xRef, 3));
                        goto case 3;

                    case 3:
                        Unsafe.Add(ref dRef, 2) = op.Invoke(Unsafe.Add(ref xRef, 2));
                        goto case 2;

                    case 2:
                        Unsafe.Add(ref dRef, 1) = op.Invoke(Unsafe.Add(ref xRef, 1));
                        goto case 1;

                    case 1:
                        dRef = op.Invoke(xRef);
                        goto case 0;

                    case 0:
                        break;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall2(ref T xRef, ref T dRef, nuint remainder, TStatefulUnaryOperator op)
            {
                Debug.Assert(sizeof(T) == 2);

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

                            Vector256<T> beg = op.Invoke(Vector256.LoadUnsafe(ref xRef));
                            Vector256<T> end = op.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    // One Vector256's worth of data.
                    case 16:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = op.Invoke(Vector256.LoadUnsafe(ref xRef));
                            beg.StoreUnsafe(ref dRef);

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

                            Vector128<T> beg = op.Invoke(Vector128.LoadUnsafe(ref xRef));
                            Vector128<T> end = op.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    // One Vector128's worth of data.
                    case 8:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = op.Invoke(Vector128.LoadUnsafe(ref xRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    // Cases that are smaller than a single vector. No SIMD; just jump to the length and fall through each
                    // case to unroll the whole processing.
                    case 7:
                        Unsafe.Add(ref dRef, 6) = op.Invoke(Unsafe.Add(ref xRef, 6));
                        goto case 6;

                    case 6:
                        Unsafe.Add(ref dRef, 5) = op.Invoke(Unsafe.Add(ref xRef, 5));
                        goto case 5;

                    case 5:
                        Unsafe.Add(ref dRef, 4) = op.Invoke(Unsafe.Add(ref xRef, 4));
                        goto case 4;

                    case 4:
                        Unsafe.Add(ref dRef, 3) = op.Invoke(Unsafe.Add(ref xRef, 3));
                        goto case 3;

                    case 3:
                        Unsafe.Add(ref dRef, 2) = op.Invoke(Unsafe.Add(ref xRef, 2));
                        goto case 2;

                    case 2:
                        Unsafe.Add(ref dRef, 1) = op.Invoke(Unsafe.Add(ref xRef, 1));
                        goto case 1;

                    case 1:
                        dRef = op.Invoke(xRef);
                        goto case 0;

                    case 0:
                        break;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall4(ref T xRef, ref T dRef, nuint remainder, TStatefulUnaryOperator op)
            {
                Debug.Assert(sizeof(T) == 4);

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

                            Vector256<T> beg = op.Invoke(Vector256.LoadUnsafe(ref xRef));
                            Vector256<T> end = op.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    case 8:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = op.Invoke(Vector256.LoadUnsafe(ref xRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 7:
                    case 6:
                    case 5:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = op.Invoke(Vector128.LoadUnsafe(ref xRef));
                            Vector128<T> end = op.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    case 4:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = op.Invoke(Vector128.LoadUnsafe(ref xRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall8(ref T xRef, ref T dRef, nuint remainder, TStatefulUnaryOperator op)
            {
                Debug.Assert(sizeof(T) == 8);

                switch (remainder)
                {
                    case 7:
                    case 6:
                    case 5:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = op.Invoke(Vector256.LoadUnsafe(ref xRef));
                            Vector256<T> end = op.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    case 4:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = op.Invoke(Vector256.LoadUnsafe(ref xRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 3:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = op.Invoke(Vector128.LoadUnsafe(ref xRef));
                            Vector128<T> end = op.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    case 2:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = op.Invoke(Vector128.LoadUnsafe(ref xRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
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
    }
}
