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
        /// <summary>Defines the threshold, in bytes, at which non-temporal stores will be used.</summary>
        /// <remarks>
        ///     A non-temporal store is one that allows the CPU to bypass the cache when writing to memory.
        ///
        ///     This can be beneficial when working with large amounts of memory where the writes would otherwise
        ///     cause large amounts of repeated updates and evictions. The hardware optimization manuals recommend
        ///     the threshold to be roughly half the size of the last level of on-die cache -- that is, if you have approximately
        ///     4MB of L3 cache per core, you'd want this to be approx. 1-2MB, depending on if hyperthreading was enabled.
        ///
        ///     However, actually computing the amount of L3 cache per core can be tricky or error prone. Native memcpy
        ///     algorithms use a constant threshold that is typically around 256KB and we match that here for simplicity. This
        ///     threshold accounts for most processors in the last 10-15 years that had approx. 1MB L3 per core and support
        ///     hyperthreading, giving a per core last level cache of approx. 512KB.
        /// </remarks>
        private const nuint NonTemporalByteThreshold = 256 * 1024;

        /// <summary>Operator that takes one input value and returns a single value.</summary>
        /// <remarks>The input and output type must be of the same size if vectorization is desired.</remarks>
        internal interface IUnaryOperator<TInput, TOutput>
        {
            static abstract bool Vectorizable { get; }
            static abstract TOutput Invoke(TInput x);
            static abstract Vector128<TOutput> Invoke(Vector128<TInput> x);
            static abstract Vector256<TOutput> Invoke(Vector256<TInput> x);
            static abstract Vector512<TOutput> Invoke(Vector512<TInput> x);
        }

        /// <summary>x</summary>
        internal readonly struct IdentityOperator<T> : IUnaryOperator<T, T>
        {
            public static bool Vectorizable => true;
            public static T Invoke(T x) => x;
            public static Vector128<T> Invoke(Vector128<T> x) => x;
            public static Vector256<T> Invoke(Vector256<T> x) => x;
            public static Vector512<T> Invoke(Vector512<T> x) => x;
        }

        /// <summary>Performs an element-wise operation on <paramref name="x"/> and writes the results to <paramref name="destination"/>.</summary>
        /// <typeparam name="T">The element input type.</typeparam>
        /// <typeparam name="TUnaryOperator">Specifies the operation to perform on each element loaded from <paramref name="x"/>.</typeparam>
        private static void InvokeSpanIntoSpan<T, TUnaryOperator>(
            ReadOnlySpan<T> x, Span<T> destination)
            where TUnaryOperator : struct, IUnaryOperator<T, T> =>
            InvokeSpanIntoSpan<T, T, TUnaryOperator>(x, destination);

        /// <summary>Performs an element-wise operation on <paramref name="x"/> and writes the results to <paramref name="destination"/>.</summary>
        /// <typeparam name="TInput">The element input type.</typeparam>
        /// <typeparam name="TOutput">The element output type. Must be the same size as TInput if TInput and TOutput both support vectorization.</typeparam>
        /// <typeparam name="TUnaryOperator">Specifies the operation to perform on each element loaded from <paramref name="x"/>.</typeparam>
        /// <remarks>
        /// This supports vectorizing the operation if <typeparamref name="TInput"/> and <typeparamref name="TOutput"/> are the same size.
        /// Otherwise, it'll fall back to scalar operations.
        /// </remarks>
        private static void InvokeSpanIntoSpan<TInput, TOutput, TUnaryOperator>(
            ReadOnlySpan<TInput> x, Span<TOutput> destination)
            where TUnaryOperator : struct, IUnaryOperator<TInput, TOutput>
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            if (typeof(TInput) == typeof(TOutput))
            {
                // This ignores the unsafe case where a developer passes in overlapping spans for distinct types.
                ValidateInputOutputSpanNonOverlapping(x, Rename<TOutput, TInput>(destination));
            }

            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref TInput xRef = ref MemoryMarshal.GetReference(x);
            ref TOutput dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)x.Length;

            if (Vector512.IsHardwareAccelerated && Vector512<TInput>.IsSupported && Vector512<TOutput>.IsSupported && TUnaryOperator.Vectorizable && Unsafe.SizeOf<TInput>() == Unsafe.SizeOf<TOutput>())
            {
                if (remainder >= (uint)Vector512<TInput>.Count)
                {
                    Vectorized512(ref xRef, ref dRef, remainder);
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

            if (Vector256.IsHardwareAccelerated && Vector256<TInput>.IsSupported && Vector256<TOutput>.IsSupported && TUnaryOperator.Vectorizable && Unsafe.SizeOf<TInput>() == Unsafe.SizeOf<TOutput>())
            {
                if (remainder >= (uint)Vector256<TInput>.Count)
                {
                    Vectorized256(ref xRef, ref dRef, remainder);
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

            if (Vector128.IsHardwareAccelerated && Vector128<TInput>.IsSupported && Vector128<TOutput>.IsSupported && TUnaryOperator.Vectorizable && Unsafe.SizeOf<TInput>() == Unsafe.SizeOf<TOutput>())
            {
                if (remainder >= (uint)Vector128<TInput>.Count)
                {
                    Vectorized128(ref xRef, ref dRef, remainder);
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
            static void SoftwareFallback(ref TInput xRef, ref TOutput dRef, nuint length)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, i) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, i));
                }
            }

            static void Vectorized128(ref TInput xRef, ref TOutput dRef, nuint remainder)
            {
                ref TOutput dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector128<TOutput> beg = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                Vector128<TOutput> end = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<TInput>.Count));

                if (remainder > (uint)(Vector128<TInput>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (TInput* px = &xRef)
                    fixed (TOutput* pd = &dRef)
                    {
                        TInput* xPtr = px;
                        TOutput* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(TInput)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector128<TInput>) - ((nuint)dPtr % (uint)sizeof(Vector128<TInput>))) / (uint)sizeof(TInput);

                            xPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector128<TInput>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector128<TOutput> vector1;
                        Vector128<TOutput> vector2;
                        Vector128<TOutput> vector3;
                        Vector128<TOutput> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(TInput))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector128<TInput>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<TInput>.Count * 0)));
                                vector2 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<TInput>.Count * 1)));
                                vector3 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<TInput>.Count * 2)));
                                vector4 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<TInput>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<TOutput>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<TOutput>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<TOutput>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<TOutput>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<TInput>.Count * 4)));
                                vector2 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<TInput>.Count * 5)));
                                vector3 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<TInput>.Count * 6)));
                                vector4 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<TInput>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<TOutput>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<TOutput>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<TOutput>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<TOutput>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<TInput>.Count * 8);
                                dPtr += (uint)(Vector128<TOutput>.Count * 8);

                                remainder -= (uint)(Vector128<TInput>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector128<TInput>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<TInput>.Count * 0)));
                                vector2 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<TInput>.Count * 1)));
                                vector3 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<TInput>.Count * 2)));
                                vector4 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<TInput>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector128<TOutput>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector128<TOutput>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector128<TOutput>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector128<TOutput>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<TInput>.Count * 4)));
                                vector2 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<TInput>.Count * 5)));
                                vector3 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<TInput>.Count * 6)));
                                vector4 = TUnaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<TInput>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector128<TOutput>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector128<TOutput>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector128<TOutput>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector128<TOutput>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<TInput>.Count * 8);
                                dPtr += (uint)(Vector128<TOutput>.Count * 8);

                                remainder -= (uint)(Vector128<TInput>.Count * 8);
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
                remainder = (remainder + (uint)(Vector128<TInput>.Count - 1)) & (nuint)(-Vector128<TInput>.Count);

                switch (remainder / (uint)Vector128<TInput>.Count)
                {
                    case 8:
                        {
                            Vector128<TOutput> vector = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<TInput>.Count * 8)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<TOutput>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector128<TOutput> vector = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<TInput>.Count * 7)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<TOutput>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector128<TOutput> vector = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<TInput>.Count * 6)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<TOutput>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector128<TOutput> vector = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<TInput>.Count * 5)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<TOutput>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector128<TOutput> vector = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<TInput>.Count * 4)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<TOutput>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector128<TOutput> vector = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<TInput>.Count * 3)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<TOutput>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector128<TOutput> vector = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<TInput>.Count * 2)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<TOutput>.Count * 2));
                            goto case 1;
                        }

                    case 1:
                        {
                            // Store the last block, which includes any elements that wouldn't fill a full vector
                            end.StoreUnsafe(ref dRef, endIndex - (uint)Vector128<TInput>.Count);
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

            static void Vectorized256(ref TInput xRef, ref TOutput dRef, nuint remainder)
            {
                ref TOutput dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector256<TOutput> beg = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                Vector256<TOutput> end = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<TInput>.Count));

                if (remainder > (uint)(Vector256<TInput>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (TInput* px = &xRef)
                    fixed (TOutput* pd = &dRef)
                    {
                        TInput* xPtr = px;
                        TOutput* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(TInput)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector256<TInput>) - ((nuint)dPtr % (uint)sizeof(Vector256<TInput>))) / (uint)sizeof(TInput);

                            xPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector256<TInput>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector256<TOutput> vector1;
                        Vector256<TOutput> vector2;
                        Vector256<TOutput> vector3;
                        Vector256<TOutput> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(TInput))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector256<TInput>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 0)));
                                vector2 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 1)));
                                vector3 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 2)));
                                vector4 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<TOutput>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<TOutput>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<TOutput>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<TOutput>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 4)));
                                vector2 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 5)));
                                vector3 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 6)));
                                vector4 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<TOutput>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<TOutput>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<TOutput>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<TOutput>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<TInput>.Count * 8);
                                dPtr += (uint)(Vector256<TOutput>.Count * 8);

                                remainder -= (uint)(Vector256<TInput>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector256<TInput>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 0)));
                                vector2 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 1)));
                                vector3 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 2)));
                                vector4 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector256<TOutput>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector256<TOutput>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector256<TOutput>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector256<TOutput>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 4)));
                                vector2 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 5)));
                                vector3 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 6)));
                                vector4 = TUnaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<TInput>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector256<TOutput>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector256<TOutput>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector256<TOutput>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector256<TOutput>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<TInput>.Count * 8);
                                dPtr += (uint)(Vector256<TOutput>.Count * 8);

                                remainder -= (uint)(Vector256<TInput>.Count * 8);
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
                remainder = (remainder + (uint)(Vector256<TInput>.Count - 1)) & (nuint)(-Vector256<TInput>.Count);

                switch (remainder / (uint)Vector256<TInput>.Count)
                {
                    case 8:
                        {
                            Vector256<TOutput> vector = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<TInput>.Count * 8)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<TOutput>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector256<TOutput> vector = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<TInput>.Count * 7)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<TOutput>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector256<TOutput> vector = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<TInput>.Count * 6)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<TOutput>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector256<TOutput> vector = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<TInput>.Count * 5)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<TOutput>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector256<TOutput> vector = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<TInput>.Count * 4)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<TOutput>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector256<TOutput> vector = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<TInput>.Count * 3)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<TOutput>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector256<TOutput> vector = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<TInput>.Count * 2)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<TOutput>.Count * 2));
                            goto case 1;
                        }

                    case 1:
                        {
                            // Store the last block, which includes any elements that wouldn't fill a full vector
                            end.StoreUnsafe(ref dRef, endIndex - (uint)Vector256<TOutput>.Count);
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

            static void Vectorized512(ref TInput xRef, ref TOutput dRef, nuint remainder)
            {
                ref TOutput dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector512<TOutput> beg = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef));
                Vector512<TOutput> end = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)Vector512<TInput>.Count));

                if (remainder > (uint)(Vector512<TInput>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (TInput* px = &xRef)
                    fixed (TOutput* pd = &dRef)
                    {
                        TInput* xPtr = px;
                        TOutput* dPtr = pd;

                        // We need to the ensure the underlying data can be aligned and only align
                        // it if it can. It is possible we have an unaligned ref, in which case we
                        // can never achieve the required SIMD alignment.

                        bool canAlign = ((nuint)dPtr % (nuint)sizeof(TInput)) == 0;

                        if (canAlign)
                        {
                            // Compute by how many elements we're misaligned and adjust the pointers accordingly
                            //
                            // Noting that we are only actually aligning dPtr. This is because unaligned stores
                            // are more expensive than unaligned loads and aligning both is significantly more
                            // complex.

                            nuint misalignment = ((uint)sizeof(Vector512<TInput>) - ((nuint)dPtr % (uint)sizeof(Vector512<TInput>))) / (uint)sizeof(TInput);

                            xPtr += misalignment;
                            dPtr += misalignment;

                            Debug.Assert(((nuint)dPtr % (uint)sizeof(Vector512<TInput>)) == 0);

                            remainder -= misalignment;
                        }

                        Vector512<TOutput> vector1;
                        Vector512<TOutput> vector2;
                        Vector512<TOutput> vector3;
                        Vector512<TOutput> vector4;

                        if ((remainder > (NonTemporalByteThreshold / (nuint)sizeof(TInput))) && canAlign)
                        {
                            // This loop stores the data non-temporally, which benefits us when there
                            // is a large amount of data involved as it avoids polluting the cache.

                            while (remainder >= (uint)(Vector512<TInput>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<TInput>.Count * 0)));
                                vector2 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<TInput>.Count * 1)));
                                vector3 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<TInput>.Count * 2)));
                                vector4 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<TInput>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<TOutput>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<TOutput>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<TOutput>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<TOutput>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<TInput>.Count * 4)));
                                vector2 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<TInput>.Count * 5)));
                                vector3 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<TInput>.Count * 6)));
                                vector4 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<TInput>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<TOutput>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<TOutput>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<TOutput>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<TOutput>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<TInput>.Count * 8);
                                dPtr += (uint)(Vector512<TInput>.Count * 8);

                                remainder -= (uint)(Vector512<TInput>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector512<TInput>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<TInput>.Count * 0)));
                                vector2 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<TInput>.Count * 1)));
                                vector3 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<TInput>.Count * 2)));
                                vector4 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<TInput>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector512<TOutput>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector512<TOutput>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector512<TOutput>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector512<TOutput>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<TInput>.Count * 4)));
                                vector2 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<TInput>.Count * 5)));
                                vector3 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<TInput>.Count * 6)));
                                vector4 = TUnaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<TInput>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector512<TOutput>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector512<TOutput>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector512<TOutput>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector512<TOutput>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<TInput>.Count * 8);
                                dPtr += (uint)(Vector512<TOutput>.Count * 8);

                                remainder -= (uint)(Vector512<TInput>.Count * 8);
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
                remainder = (remainder + (uint)(Vector512<TInput>.Count - 1)) & (nuint)(-Vector512<TInput>.Count);

                switch (remainder / (uint)Vector512<TInput>.Count)
                {
                    case 8:
                        {
                            Vector512<TOutput> vector = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<TInput>.Count * 8)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<TOutput>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector512<TOutput> vector = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<TInput>.Count * 7)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<TOutput>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector512<TOutput> vector = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<TInput>.Count * 6)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<TOutput>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector512<TOutput> vector = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<TInput>.Count * 5)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<TOutput>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector512<TOutput> vector = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<TInput>.Count * 4)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<TOutput>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector512<TOutput> vector = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<TInput>.Count * 3)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<TOutput>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector512<TOutput> vector = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<TInput>.Count * 2)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<TOutput>.Count * 2));
                            goto case 1;
                        }

                    case 1:
                        {
                            // Store the last block, which includes any elements that wouldn't fill a full vector
                            end.StoreUnsafe(ref dRef, endIndex - (uint)Vector512<TOutput>.Count);
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
            static void VectorizedSmall(ref TInput xRef, ref TOutput dRef, nuint remainder)
            {
                if (sizeof(TInput) == 1)
                {
                    VectorizedSmall1(ref xRef, ref dRef, remainder);
                }
                else if (sizeof(TInput) == 2)
                {
                    VectorizedSmall2(ref xRef, ref dRef, remainder);
                }
                else if (sizeof(TInput) == 4)
                {
                    VectorizedSmall4(ref xRef, ref dRef, remainder);
                }
                else
                {
                    Debug.Assert(sizeof(TInput) == 8);
                    VectorizedSmall8(ref xRef, ref dRef, remainder);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall1(ref TInput xRef, ref TOutput dRef, nuint remainder)
            {
                Debug.Assert(sizeof(TInput) == 1);

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

                            Vector256<TOutput> beg = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                            Vector256<TOutput> end = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<TInput>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<TOutput>.Count);

                            break;
                        }

                    // One Vector256's worth of data.
                    case 32:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<TOutput> beg = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
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

                            Vector128<TOutput> beg = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                            Vector128<TOutput> end = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<TInput>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<TOutput>.Count);

                            break;
                        }

                    // One Vector128's worth of data.
                    case 16:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<TOutput> beg = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    // Cases that are smaller than a single vector. No SIMD; just jump to the length and fall through each
                    // case to unroll the whole processing.
                    case 15:
                        Unsafe.Add(ref dRef, 14) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 14));
                        goto case 14;

                    case 14:
                        Unsafe.Add(ref dRef, 13) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 13));
                        goto case 13;

                    case 13:
                        Unsafe.Add(ref dRef, 12) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 12));
                        goto case 12;

                    case 12:
                        Unsafe.Add(ref dRef, 11) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 11));
                        goto case 11;

                    case 11:
                        Unsafe.Add(ref dRef, 10) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 10));
                        goto case 10;

                    case 10:
                        Unsafe.Add(ref dRef, 9) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 9));
                        goto case 9;

                    case 9:
                        Unsafe.Add(ref dRef, 8) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 8));
                        goto case 8;

                    case 8:
                        Unsafe.Add(ref dRef, 7) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 7));
                        goto case 7;

                    case 7:
                        Unsafe.Add(ref dRef, 6) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 6));
                        goto case 6;

                    case 6:
                        Unsafe.Add(ref dRef, 5) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 5));
                        goto case 5;

                    case 5:
                        Unsafe.Add(ref dRef, 4) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 4));
                        goto case 4;

                    case 4:
                        Unsafe.Add(ref dRef, 3) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 3));
                        goto case 3;

                    case 3:
                        Unsafe.Add(ref dRef, 2) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 2));
                        goto case 2;

                    case 2:
                        Unsafe.Add(ref dRef, 1) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 1));
                        goto case 1;

                    case 1:
                        dRef = TUnaryOperator.Invoke(xRef);
                        goto case 0;

                    case 0:
                        break;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall2(ref TInput xRef, ref TOutput dRef, nuint remainder)
            {
                Debug.Assert(sizeof(TInput) == 2);

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

                            Vector256<TOutput> beg = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                            Vector256<TOutput> end = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<TInput>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<TOutput>.Count);

                            break;
                        }

                    // One Vector256's worth of data.
                    case 16:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<TOutput> beg = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
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

                            Vector128<TOutput> beg = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                            Vector128<TOutput> end = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<TInput>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<TOutput>.Count);

                            break;
                        }

                    // One Vector128's worth of data.
                    case 8:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<TOutput> beg = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    // Cases that are smaller than a single vector. No SIMD; just jump to the length and fall through each
                    // case to unroll the whole processing.
                    case 7:
                        Unsafe.Add(ref dRef, 6) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 6));
                        goto case 6;

                    case 6:
                        Unsafe.Add(ref dRef, 5) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 5));
                        goto case 5;

                    case 5:
                        Unsafe.Add(ref dRef, 4) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 4));
                        goto case 4;

                    case 4:
                        Unsafe.Add(ref dRef, 3) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 3));
                        goto case 3;

                    case 3:
                        Unsafe.Add(ref dRef, 2) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 2));
                        goto case 2;

                    case 2:
                        Unsafe.Add(ref dRef, 1) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 1));
                        goto case 1;

                    case 1:
                        dRef = TUnaryOperator.Invoke(xRef);
                        goto case 0;

                    case 0:
                        break;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall4(ref TInput xRef, ref TOutput dRef, nuint remainder)
            {
                Debug.Assert(sizeof(TInput) == 4);

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

                            Vector256<TOutput> beg = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                            Vector256<TOutput> end = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<TInput>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<TOutput>.Count);

                            break;
                        }

                    case 8:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<TOutput> beg = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 7:
                    case 6:
                    case 5:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<TOutput> beg = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                            Vector128<TOutput> end = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<TInput>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<TOutput>.Count);

                            break;
                        }

                    case 4:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<TOutput> beg = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 3:
                        {
                            Unsafe.Add(ref dRef, 2) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 2));
                            goto case 2;
                        }

                    case 2:
                        {
                            Unsafe.Add(ref dRef, 1) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, 1));
                            goto case 1;
                        }

                    case 1:
                        {
                            dRef = TUnaryOperator.Invoke(xRef);
                            goto case 0;
                        }

                    case 0:
                        {
                            break;
                        }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall8(ref TInput xRef, ref TOutput dRef, nuint remainder)
            {
                Debug.Assert(sizeof(TInput) == 8);

                switch (remainder)
                {
                    case 7:
                    case 6:
                    case 5:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<TOutput> beg = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                            Vector256<TOutput> end = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<TInput>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<TOutput>.Count);

                            break;
                        }

                    case 4:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<TOutput> beg = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 3:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<TOutput> beg = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                            Vector128<TOutput> end = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<TInput>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<TOutput>.Count);

                            break;
                        }

                    case 2:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<TOutput> beg = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 1:
                        {
                            dRef = TUnaryOperator.Invoke(xRef);
                            goto case 0;
                        }

                    case 0:
                        {
                            break;
                        }
                }
            }
        }

        /// <summary>Creates a span of <typeparamref name="TTo"/> from a <typeparamref name="TTo"/> when they're the same type.</summary>
        private static unsafe Span<TTo> Rename<TFrom, TTo>(Span<TFrom> span)
        {
            Debug.Assert(sizeof(TFrom) == sizeof(TTo));
            return MemoryMarshal.CreateSpan(ref Unsafe.As<TFrom, TTo>(ref MemoryMarshal.GetReference(span)), span.Length);
        }
    }
}
