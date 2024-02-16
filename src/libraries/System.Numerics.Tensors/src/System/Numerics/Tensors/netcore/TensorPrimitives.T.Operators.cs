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
        // Defines the core vectorizable operator abstractions and
        // the workhorse methods that orchestrate them over spans of data.

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

        /// <summary>Operator that takes one input value and returns a single value.</summary>
        /// <remarks>The input type must be half the size of the output type.</remarks>
        private interface IUnaryOneToTwoOperator<TInput, TOutput>
        {
            static abstract bool Vectorizable { get; }
            static abstract TOutput Invoke(TInput x);
            static abstract (Vector128<TOutput> Lower, Vector128<TOutput> Upper) Invoke(Vector128<TInput> x);
            static abstract (Vector256<TOutput> Lower, Vector256<TOutput> Upper) Invoke(Vector256<TInput> x);
            static abstract (Vector512<TOutput> Lower, Vector512<TOutput> Upper) Invoke(Vector512<TInput> x);
        }

        /// <summary>Operator that takes one input value and returns a single value.</summary>
        /// <remarks>The input type must be twice the size of the output type.</remarks>
        private interface IUnaryTwoToOneOperator<TInput, TOutput>
        {
            static abstract bool Vectorizable { get; }
            static abstract TOutput Invoke(TInput x);
            static abstract Vector128<TOutput> Invoke(Vector128<TInput> lower, Vector128<TInput> upper);
            static abstract Vector256<TOutput> Invoke(Vector256<TInput> lower, Vector256<TInput> upper);
            static abstract Vector512<TOutput> Invoke(Vector512<TInput> lower, Vector512<TInput> upper);
        }

        /// <summary>Operator that takes one input value and returns two output values.</summary>
        private interface IUnaryInputBinaryOutput<T>
        {
            static abstract bool Vectorizable { get; }
            static abstract (T, T) Invoke(T x);
            static abstract (Vector128<T> First, Vector128<T> Second) Invoke(Vector128<T> x);
            static abstract (Vector256<T> First, Vector256<T> Second) Invoke(Vector256<T> x);
            static abstract (Vector512<T> First, Vector512<T> Second) Invoke(Vector512<T> x);
        }

        /// <summary>Operator that takes one input value and returns a single value.</summary>
        private interface IStatefulUnaryOperator<T>
        {
            static abstract bool Vectorizable { get; }
            T Invoke(T x);
            Vector128<T> Invoke(Vector128<T> x);
            Vector256<T> Invoke(Vector256<T> x);
            Vector512<T> Invoke(Vector512<T> x);
        }

        /// <summary>Operator that takes two input values and returns a single value.</summary>
        private interface IBinaryOperator<T>
        {
            static abstract bool Vectorizable { get; }
            static abstract T Invoke(T x, T y);
            static abstract Vector128<T> Invoke(Vector128<T> x, Vector128<T> y);
            static abstract Vector256<T> Invoke(Vector256<T> x, Vector256<T> y);
            static abstract Vector512<T> Invoke(Vector512<T> x, Vector512<T> y);
        }

        /// <summary><see cref="IBinaryOperator{T}"/> that specializes horizontal aggregation of all elements in a vector.</summary>
        private interface IAggregationOperator<T> : IBinaryOperator<T>
        {
            static abstract T Invoke(Vector128<T> x);
            static abstract T Invoke(Vector256<T> x);
            static abstract T Invoke(Vector512<T> x);

            static virtual T IdentityValue => throw new NotSupportedException();
        }

        /// <summary>Operator that takes three input values and returns a single value.</summary>
        private interface ITernaryOperator<T>
        {
            static abstract T Invoke(T x, T y, T z);
            static abstract Vector128<T> Invoke(Vector128<T> x, Vector128<T> y, Vector128<T> z);
            static abstract Vector256<T> Invoke(Vector256<T> x, Vector256<T> y, Vector256<T> z);
            static abstract Vector512<T> Invoke(Vector512<T> x, Vector512<T> y, Vector512<T> z);
        }

        private interface IIndexOfOperator<T>
        {
            static abstract int Invoke(ref T result, T current, int resultIndex, int currentIndex);
            static abstract void Invoke(ref Vector128<T> result, Vector128<T> current, ref Vector128<T> resultIndex, Vector128<T> currentIndex);
            static abstract void Invoke(ref Vector256<T> result, Vector256<T> current, ref Vector256<T> resultIndex, Vector256<T> currentIndex);
            static abstract void Invoke(ref Vector512<T> result, Vector512<T> current, ref Vector512<T> resultIndex, Vector512<T> currentIndex);
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

        /// <summary>Performs an element-wise operation on <paramref name="x"/> and writes the results to <paramref name="destination"/>.</summary>
        /// <typeparam name="TInput">The element input type.</typeparam>
        /// <typeparam name="TOutput">The element output type. Must be the same size as TInput if TInput and TOutput both support vectorization.</typeparam>
        /// <typeparam name="TUnaryOperator">Specifies the operation to perform on each element loaded from <paramref name="x"/>.</typeparam>
        /// <remarks>This should only be used when it's known that TInput/TOutput are vectorizable and the size of TInput is twice that of TOutput.</remarks>
        private static void InvokeSpanIntoSpan_2to1<TInput, TOutput, TUnaryOperator>(
            ReadOnlySpan<TInput> x, Span<TOutput> destination)
            where TUnaryOperator : struct, IUnaryTwoToOneOperator<TInput, TOutput>
        {
            Debug.Assert(sizeof(TInput) == sizeof(TOutput) * 2);

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ref TInput xRef = ref MemoryMarshal.GetReference(x);
            ref TOutput destinationRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, twoVectorsFromEnd;

            if (Vector512.IsHardwareAccelerated && TUnaryOperator.Vectorizable)
            {
                Debug.Assert(Vector512<TInput>.IsSupported);
                Debug.Assert(Vector512<TOutput>.IsSupported);

                twoVectorsFromEnd = x.Length - (Vector512<TInput>.Count * 2);
                if (i <= twoVectorsFromEnd)
                {
                    // Loop handling two input vectors / one output vector at a time.
                    do
                    {
                        TUnaryOperator.Invoke(
                            Vector512.LoadUnsafe(ref xRef, (uint)i),
                            Vector512.LoadUnsafe(ref xRef, (uint)(i + Vector512<TInput>.Count))).StoreUnsafe(ref destinationRef, (uint)i);

                        i += Vector512<TInput>.Count * 2;
                    }
                    while (i <= twoVectorsFromEnd);

                    // Handle any remaining elements with final vectors.
                    if (i != x.Length)
                    {
                        i = x.Length - (Vector512<TInput>.Count * 2);

                        TUnaryOperator.Invoke(
                            Vector512.LoadUnsafe(ref xRef, (uint)i),
                            Vector512.LoadUnsafe(ref xRef, (uint)(i + Vector512<TInput>.Count))).StoreUnsafe(ref destinationRef, (uint)i);
                    }

                    return;
                }
            }

            if (Vector256.IsHardwareAccelerated && TUnaryOperator.Vectorizable)
            {
                Debug.Assert(Vector256<TInput>.IsSupported);
                Debug.Assert(Vector256<TOutput>.IsSupported);

                twoVectorsFromEnd = x.Length - (Vector256<TInput>.Count * 2);
                if (i <= twoVectorsFromEnd)
                {
                    // Loop handling two input vectors / one output vector at a time.
                    do
                    {
                        TUnaryOperator.Invoke(
                            Vector256.LoadUnsafe(ref xRef, (uint)i),
                            Vector256.LoadUnsafe(ref xRef, (uint)(i + Vector256<TInput>.Count))).StoreUnsafe(ref destinationRef, (uint)i);

                        i += Vector256<TInput>.Count * 2;
                    }
                    while (i <= twoVectorsFromEnd);

                    // Handle any remaining elements with final vectors.
                    if (i != x.Length)
                    {
                        i = x.Length - (Vector256<TInput>.Count * 2);

                        TUnaryOperator.Invoke(
                            Vector256.LoadUnsafe(ref xRef, (uint)i),
                            Vector256.LoadUnsafe(ref xRef, (uint)(i + Vector256<TInput>.Count))).StoreUnsafe(ref destinationRef, (uint)i);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated && TUnaryOperator.Vectorizable)
            {
                Debug.Assert(Vector128<TInput>.IsSupported);
                Debug.Assert(Vector128<TOutput>.IsSupported);

                twoVectorsFromEnd = x.Length - (Vector128<TInput>.Count * 2);
                if (i <= twoVectorsFromEnd)
                {
                    // Loop handling two input vectors / one output vector at a time.
                    do
                    {
                        TUnaryOperator.Invoke(
                            Vector128.LoadUnsafe(ref xRef, (uint)i),
                            Vector128.LoadUnsafe(ref xRef, (uint)(i + Vector128<TInput>.Count))).StoreUnsafe(ref destinationRef, (uint)i);

                        i += Vector128<TInput>.Count * 2;
                    }
                    while (i <= twoVectorsFromEnd);

                    // Handle any remaining elements with final vectors.
                    if (i != x.Length)
                    {
                        i = x.Length - (Vector128<TInput>.Count * 2);

                        TUnaryOperator.Invoke(
                            Vector128.LoadUnsafe(ref xRef, (uint)i),
                            Vector128.LoadUnsafe(ref xRef, (uint)(i + Vector128<TInput>.Count))).StoreUnsafe(ref destinationRef, (uint)i);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref destinationRef, i) = TUnaryOperator.Invoke(Unsafe.Add(ref xRef, i));
                i++;
            }
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

        /// <summary>Performs an element-wise operation on <paramref name="x"/> and writes the results to <paramref name="destination"/>.</summary>
        /// <typeparam name="TInput">The element input type.</typeparam>
        /// <typeparam name="TOutput">The element output type. Must be the same size as TInput if TInput and TOutput both support vectorization.</typeparam>
        /// <typeparam name="TUnaryOperator">Specifies the operation to perform on each element loaded from <paramref name="x"/>.</typeparam>
        /// <remarks>This should only be used when it's known that TInput/TOutput are vectorizable and the size of TInput is half that of TOutput.</remarks>
        private static void InvokeSpanIntoSpan_1to2<TInput, TOutput, TUnaryOperator>(
            ReadOnlySpan<TInput> x, Span<TOutput> destination)
            where TUnaryOperator : struct, IUnaryOneToTwoOperator<TInput, TOutput>
        {
            Debug.Assert(sizeof(TInput) * 2 == sizeof(TOutput));

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            ref TInput sourceRef = ref MemoryMarshal.GetReference(x);
            ref TOutput destinationRef = ref MemoryMarshal.GetReference(destination);
            int i = 0, oneVectorFromEnd;

            if (Vector512.IsHardwareAccelerated && TUnaryOperator.Vectorizable)
            {
                Debug.Assert(Vector512<TInput>.IsSupported);
                Debug.Assert(Vector512<TOutput>.IsSupported);

                oneVectorFromEnd = x.Length - Vector512<TInput>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one input vector / two output vectors at a time.
                    do
                    {
                        (Vector512<TOutput> lower, Vector512<TOutput> upper) = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref sourceRef, (uint)i));
                        lower.StoreUnsafe(ref destinationRef, (uint)i);
                        upper.StoreUnsafe(ref destinationRef, (uint)(i + Vector512<TOutput>.Count));

                        i += Vector512<TInput>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final input vector.
                    if (i != x.Length)
                    {
                        i = x.Length - Vector512<TInput>.Count;

                        (Vector512<TOutput> lower, Vector512<TOutput> upper) = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref sourceRef, (uint)i));
                        lower.StoreUnsafe(ref destinationRef, (uint)i);
                        upper.StoreUnsafe(ref destinationRef, (uint)(i + Vector512<TOutput>.Count));
                    }

                    return;
                }
            }

            if (Vector256.IsHardwareAccelerated && TUnaryOperator.Vectorizable)
            {
                Debug.Assert(Vector256<TInput>.IsSupported);
                Debug.Assert(Vector256<TOutput>.IsSupported);

                oneVectorFromEnd = x.Length - Vector256<TInput>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one input vector / two output vectors at a time.
                    do
                    {
                        (Vector256<TOutput> lower, Vector256<TOutput> upper) = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref sourceRef, (uint)i));
                        lower.StoreUnsafe(ref destinationRef, (uint)i);
                        upper.StoreUnsafe(ref destinationRef, (uint)(i + Vector256<TOutput>.Count));

                        i += Vector256<TInput>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final input vector.
                    if (i != x.Length)
                    {
                        i = x.Length - Vector256<TInput>.Count;

                        (Vector256<TOutput> lower, Vector256<TOutput> upper) = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref sourceRef, (uint)i));
                        lower.StoreUnsafe(ref destinationRef, (uint)i);
                        upper.StoreUnsafe(ref destinationRef, (uint)(i + Vector256<TOutput>.Count));
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated && TUnaryOperator.Vectorizable)
            {
                Debug.Assert(Vector128<TInput>.IsSupported);
                Debug.Assert(Vector128<TOutput>.IsSupported);

                oneVectorFromEnd = x.Length - Vector128<TInput>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one input vector / two output vectors at a time.
                    do
                    {
                        (Vector128<TOutput> lower, Vector128<TOutput> upper) = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref sourceRef, (uint)i));
                        lower.StoreUnsafe(ref destinationRef, (uint)i);
                        upper.StoreUnsafe(ref destinationRef, (uint)(i + Vector128<TOutput>.Count));

                        i += Vector128<TInput>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final input vector.
                    if (i != x.Length)
                    {
                        i = x.Length - Vector128<TInput>.Count;

                        (Vector128<TOutput> lower, Vector128<TOutput> upper) = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref sourceRef, (uint)i));
                        lower.StoreUnsafe(ref destinationRef, (uint)i);
                        upper.StoreUnsafe(ref destinationRef, (uint)(i + Vector128<TOutput>.Count));
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                Unsafe.Add(ref destinationRef, i) = TUnaryOperator.Invoke(Unsafe.Add(ref sourceRef, i));
                i++;
            }
        }

        /// <summary>Performs an element-wise operation on <paramref name="x"/> and writes the results to <paramref name="destination1"/> and <paramref name="destination2"/>.</summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TUnaryOperator">Specifies the operation to perform on each element loaded from <paramref name="x"/>.</typeparam>
        private static void InvokeSpanIntoSpan_TwoOutputs<T, TUnaryOperator>(
            ReadOnlySpan<T> x, Span<T> destination1, Span<T> destination2)
            where TUnaryOperator : struct, IUnaryInputBinaryOutput<T>
        {
            if (x.Length > destination1.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort(nameof(destination1));
            }

            if (x.Length > destination2.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort(nameof(destination2));
            }

            ValidateInputOutputSpanNonOverlapping(x, destination1);
            ValidateInputOutputSpanNonOverlapping(x, destination2);

            ref T sourceRef = ref MemoryMarshal.GetReference(x);
            ref T destination1Ref = ref MemoryMarshal.GetReference(destination1);
            ref T destination2Ref = ref MemoryMarshal.GetReference(destination2);
            int i = 0, oneVectorFromEnd;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && TUnaryOperator.Vectorizable)
            {
                oneVectorFromEnd = x.Length - Vector512<T>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one input vector / two destination vectors at a time.
                    do
                    {
                        (Vector512<T> first, Vector512<T> second) = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref sourceRef, (uint)i));
                        first.StoreUnsafe(ref destination1Ref, (uint)i);
                        second.StoreUnsafe(ref destination2Ref, (uint)i);

                        i += Vector512<T>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final input vector.
                    if (i != x.Length)
                    {
                        i = x.Length - Vector512<T>.Count;

                        (Vector512<T> first, Vector512<T> second) = TUnaryOperator.Invoke(Vector512.LoadUnsafe(ref sourceRef, (uint)i));
                        first.StoreUnsafe(ref destination1Ref, (uint)i);
                        second.StoreUnsafe(ref destination2Ref, (uint)i);
                    }

                    return;
                }
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && TUnaryOperator.Vectorizable)
            {
                oneVectorFromEnd = x.Length - Vector256<T>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one input vector / two destination vectors at a time.
                    do
                    {
                        (Vector256<T> first, Vector256<T> second) = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref sourceRef, (uint)i));
                        first.StoreUnsafe(ref destination1Ref, (uint)i);
                        second.StoreUnsafe(ref destination2Ref, (uint)i);

                        i += Vector256<T>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final input vector.
                    if (i != x.Length)
                    {
                        i = x.Length - Vector256<T>.Count;

                        (Vector256<T> first, Vector256<T> second) = TUnaryOperator.Invoke(Vector256.LoadUnsafe(ref sourceRef, (uint)i));
                        first.StoreUnsafe(ref destination1Ref, (uint)i);
                        second.StoreUnsafe(ref destination2Ref, (uint)i);
                    }

                    return;
                }
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && TUnaryOperator.Vectorizable)
            {
                oneVectorFromEnd = x.Length - Vector128<T>.Count;
                if (i <= oneVectorFromEnd)
                {
                    // Loop handling one input vector / two destination vectors at a time.
                    do
                    {
                        (Vector128<T> first, Vector128<T> second) = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref sourceRef, (uint)i));
                        first.StoreUnsafe(ref destination1Ref, (uint)i);
                        second.StoreUnsafe(ref destination2Ref, (uint)i);

                        i += Vector128<T>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Handle any remaining elements with a final input vector.
                    if (i != x.Length)
                    {
                        i = x.Length - Vector128<T>.Count;

                        (Vector128<T> first, Vector128<T> second) = TUnaryOperator.Invoke(Vector128.LoadUnsafe(ref sourceRef, (uint)i));
                        first.StoreUnsafe(ref destination1Ref, (uint)i);
                        second.StoreUnsafe(ref destination2Ref, (uint)i);
                    }

                    return;
                }
            }

            while (i < x.Length)
            {
                (T first, T second) = TUnaryOperator.Invoke(Unsafe.Add(ref sourceRef, i));
                Unsafe.Add(ref destination1Ref, i) = first;
                Unsafe.Add(ref destination2Ref, i) = second;
                i++;
            }
        }

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/> and <paramref name="y"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TBinaryOperator{T}">
        /// Specifies the operation to perform on the pair-wise elements loaded from <paramref name="x"/> and <paramref name="y"/>.
        /// </typeparam>
        private static void InvokeSpanSpanIntoSpan<T, TBinaryOperator>(
            ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
            where TBinaryOperator : struct, IBinaryOperator<T>
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

            ref T xRef = ref MemoryMarshal.GetReference(x);
            ref T yRef = ref MemoryMarshal.GetReference(y);
            ref T dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)x.Length;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && TBinaryOperator.Vectorizable)
            {
                if (remainder >= (uint)Vector512<T>.Count)
                {
                    Vectorized512(ref xRef, ref yRef, ref dRef, remainder);
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

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && TBinaryOperator.Vectorizable)
            {
                if (remainder >= (uint)Vector256<T>.Count)
                {
                    Vectorized256(ref xRef, ref yRef, ref dRef, remainder);
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

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && TBinaryOperator.Vectorizable)
            {
                if (remainder >= (uint)Vector128<T>.Count)
                {
                    Vectorized128(ref xRef, ref yRef, ref dRef, remainder);
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
            static void SoftwareFallback(ref T xRef, ref T yRef, ref T dRef, nuint length)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, i) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                     Unsafe.Add(ref yRef, i));
                }
            }

            static void Vectorized128(ref T xRef, ref T yRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                          Vector128.LoadUnsafe(ref yRef));
                Vector128<T> end = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count));

                if (remainder > (uint)(Vector128<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;
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
                            yPtr += misalignment;
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

                                vector1 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 0)));
                                vector2 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 1)));
                                vector3 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 2)));
                                vector4 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 4)));
                                vector2 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 5)));
                                vector3 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 6)));
                                vector4 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                yPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
                        }
                        else
                        {
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

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 4)));
                                vector2 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 5)));
                                vector3 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 6)));
                                vector4 = TBinaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)),
                                                                 Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                yPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
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
                remainder = (remainder + (uint)(Vector128<T>.Count - 1)) & (nuint)(-Vector128<T>.Count);

                switch (remainder / (uint)Vector128<T>.Count)
                {
                    case 8:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 8)),
                                                                         Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 8)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 7)),
                                                                         Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 7)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 6)),
                                                                         Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 6)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 5)),
                                                                         Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 5)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 4)),
                                                                         Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 4)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 3)),
                                                                         Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 3)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 2)),
                                                                         Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 2)));
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

            static void Vectorized256(ref T xRef, ref T yRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                          Vector256.LoadUnsafe(ref yRef));
                Vector256<T> end = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count));

                if (remainder > (uint)(Vector256<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;
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
                            yPtr += misalignment;
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

                                vector1 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 0)));
                                vector2 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 1)));
                                vector3 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 2)));
                                vector4 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 4)));
                                vector2 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 5)));
                                vector3 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 6)));
                                vector4 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                yPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
                        }
                        else
                        {
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

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 4)));
                                vector2 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 5)));
                                vector3 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 6)));
                                vector4 = TBinaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
                                                                 Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                yPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
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
                remainder = (remainder + (uint)(Vector256<T>.Count - 1)) & (nuint)(-Vector256<T>.Count);

                switch (remainder / (uint)Vector256<T>.Count)
                {
                    case 8:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 8)),
                                                                         Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 8)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 7)),
                                                                         Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 7)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 6)),
                                                                         Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 6)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 5)),
                                                                         Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 5)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 4)),
                                                                         Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 4)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 3)),
                                                                         Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 3)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 2)),
                                                                         Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 2)));
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

            static void Vectorized512(ref T xRef, ref T yRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector512<T> beg = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef),
                                                          Vector512.LoadUnsafe(ref yRef));
                Vector512<T> end = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)Vector512<T>.Count),
                                                          Vector512.LoadUnsafe(ref yRef, remainder - (uint)Vector512<T>.Count));

                if (remainder > (uint)(Vector512<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;
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
                            yPtr += misalignment;
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

                                vector1 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 0)));
                                vector2 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 1)));
                                vector3 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 2)));
                                vector4 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 4)));
                                vector2 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 5)));
                                vector3 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 6)));
                                vector4 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                yPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
                        }
                        else
                        {
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

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 4)));
                                vector2 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 5)));
                                vector3 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 6)));
                                vector4 = TBinaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)),
                                                                 Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                yPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
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
                remainder = (remainder + (uint)(Vector512<T>.Count - 1)) & (nuint)(-Vector512<T>.Count);

                switch (remainder / (uint)Vector512<T>.Count)
                {
                    case 8:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 8)),
                                                                         Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 8)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 7)),
                                                                         Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 7)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 6)),
                                                                         Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 6)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 5)),
                                                                         Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 5)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 4)),
                                                                         Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 4)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 3)),
                                                                         Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 3)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 2)),
                                                                         Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 2)));
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
            static void VectorizedSmall(ref T xRef, ref T yRef, ref T dRef, nuint remainder)
            {
                if (sizeof(T) == 1)
                {
                    VectorizedSmall1(ref xRef, ref yRef, ref dRef, remainder);
                }
                else if (sizeof(T) == 2)
                {
                    VectorizedSmall2(ref xRef, ref yRef, ref dRef, remainder);
                }
                else if (sizeof(T) == 4)
                {
                    VectorizedSmall4(ref xRef, ref yRef, ref dRef, remainder);
                }
                else
                {
                    Debug.Assert(sizeof(T) == 8);
                    VectorizedSmall8(ref xRef, ref yRef, ref dRef, remainder);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall1(ref T xRef, ref T yRef, ref T dRef, nuint remainder)
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

                            Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                      Vector256.LoadUnsafe(ref yRef));
                            Vector256<T> end = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                      Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    // One Vector256's worth of data.
                    case 32:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                      Vector256.LoadUnsafe(ref yRef));
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

                            Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                      Vector128.LoadUnsafe(ref yRef));
                            Vector128<T> end = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                      Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    // One Vector128's worth of data.
                    case 16:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                      Vector128.LoadUnsafe(ref yRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    // Cases that are smaller than a single vector. No SIMD; just jump to the length and fall through each
                    // case to unroll the whole processing.
                    case 15:
                        Unsafe.Add(ref dRef, 14) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 14),
                                                                         Unsafe.Add(ref yRef, 14));
                        goto case 14;

                    case 14:
                        Unsafe.Add(ref dRef, 13) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 13),
                                                                         Unsafe.Add(ref yRef, 13));
                        goto case 13;

                    case 13:
                        Unsafe.Add(ref dRef, 12) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 12),
                                                                         Unsafe.Add(ref yRef, 12));
                        goto case 12;

                    case 12:
                        Unsafe.Add(ref dRef, 11) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 11),
                                                                         Unsafe.Add(ref yRef, 11));
                        goto case 11;

                    case 11:
                        Unsafe.Add(ref dRef, 10) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 10),
                                                                         Unsafe.Add(ref yRef, 10));
                        goto case 10;

                    case 10:
                        Unsafe.Add(ref dRef, 9) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 9),
                                                                         Unsafe.Add(ref yRef, 9));
                        goto case 9;

                    case 9:
                        Unsafe.Add(ref dRef, 8) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 8),
                                                                         Unsafe.Add(ref yRef, 8));
                        goto case 8;

                    case 8:
                        Unsafe.Add(ref dRef, 7) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 7),
                                                                         Unsafe.Add(ref yRef, 7));
                        goto case 7;

                    case 7:
                        Unsafe.Add(ref dRef, 6) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 6),
                                                                         Unsafe.Add(ref yRef, 6));
                        goto case 6;

                    case 6:
                        Unsafe.Add(ref dRef, 5) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 5),
                                                                         Unsafe.Add(ref yRef, 5));
                        goto case 5;

                    case 5:
                        Unsafe.Add(ref dRef, 4) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 4),
                                                                         Unsafe.Add(ref yRef, 4));
                        goto case 4;

                    case 4:
                        Unsafe.Add(ref dRef, 3) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 3),
                                                                         Unsafe.Add(ref yRef, 3));
                        goto case 3;

                    case 3:
                        Unsafe.Add(ref dRef, 2) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 2),
                                                                         Unsafe.Add(ref yRef, 2));
                        goto case 2;

                    case 2:
                        Unsafe.Add(ref dRef, 1) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 1),
                                                                         Unsafe.Add(ref yRef, 1));
                        goto case 1;

                    case 1:
                        dRef = TBinaryOperator.Invoke(xRef, yRef);
                        goto case 0;

                    case 0:
                        break;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall2(ref T xRef, ref T yRef, ref T dRef, nuint remainder)
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

                            Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                      Vector256.LoadUnsafe(ref yRef));
                            Vector256<T> end = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                      Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    // One Vector256's worth of data.
                    case 16:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                      Vector256.LoadUnsafe(ref yRef));
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

                            Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                      Vector128.LoadUnsafe(ref yRef));
                            Vector128<T> end = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                      Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    // One Vector128's worth of data.
                    case 8:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                      Vector128.LoadUnsafe(ref yRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    // Cases that are smaller than a single vector. No SIMD; just jump to the length and fall through each
                    // case to unroll the whole processing.
                    case 7:
                        Unsafe.Add(ref dRef, 6) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 6),
                                                                         Unsafe.Add(ref yRef, 6));
                        goto case 6;

                    case 6:
                        Unsafe.Add(ref dRef, 5) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 5),
                                                                         Unsafe.Add(ref yRef, 5));
                        goto case 5;

                    case 5:
                        Unsafe.Add(ref dRef, 4) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 4),
                                                                         Unsafe.Add(ref yRef, 4));
                        goto case 4;

                    case 4:
                        Unsafe.Add(ref dRef, 3) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 3),
                                                                         Unsafe.Add(ref yRef, 3));
                        goto case 3;

                    case 3:
                        Unsafe.Add(ref dRef, 2) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 2),
                                                                         Unsafe.Add(ref yRef, 2));
                        goto case 2;

                    case 2:
                        Unsafe.Add(ref dRef, 1) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 1),
                                                                         Unsafe.Add(ref yRef, 1));
                        goto case 1;

                    case 1:
                        dRef = TBinaryOperator.Invoke(xRef, yRef);
                        goto case 0;

                    case 0:
                        break;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall4(ref T xRef, ref T yRef, ref T dRef, nuint remainder)
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

                            Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                      Vector256.LoadUnsafe(ref yRef));
                            Vector256<T> end = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                      Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    case 8:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                      Vector256.LoadUnsafe(ref yRef));
                            beg.StoreUnsafe(ref dRef);

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

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    case 4:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                      Vector128.LoadUnsafe(ref yRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 3:
                        {
                            Unsafe.Add(ref dRef, 2) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 2),
                                                                             Unsafe.Add(ref yRef, 2));
                            goto case 2;
                        }

                    case 2:
                        {
                            Unsafe.Add(ref dRef, 1) = TBinaryOperator.Invoke(Unsafe.Add(ref xRef, 1),
                                                                             Unsafe.Add(ref yRef, 1));
                            goto case 1;
                        }

                    case 1:
                        {
                            dRef = TBinaryOperator.Invoke(xRef, yRef);
                            goto case 0;
                        }

                    case 0:
                        {
                            break;
                        }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall8(ref T xRef, ref T yRef, ref T dRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 8);

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

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    case 4:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TBinaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                      Vector256.LoadUnsafe(ref yRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 3:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                      Vector128.LoadUnsafe(ref yRef));
                            Vector128<T> end = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                      Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    case 2:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TBinaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                      Vector128.LoadUnsafe(ref yRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 1:
                        {
                            dRef = TBinaryOperator.Invoke(xRef, yRef);
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
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TBinaryOperator">
        /// Specifies the operation to perform on each element loaded from <paramref name="x"/> with <paramref name="y"/>.
        /// </typeparam>
        private static void InvokeScalarSpanIntoSpan<T, TBinaryOperator>(
            T x, ReadOnlySpan<T> y, Span<T> destination)
            where TBinaryOperator : struct, IBinaryOperator<T> =>
            InvokeSpanScalarIntoSpan<T, IdentityOperator<T>, InvertedBinaryOperator<TBinaryOperator, T>>(y, x, destination);

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/> and <paramref name="y"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TBinaryOperator">
        /// Specifies the operation to perform on each element loaded from <paramref name="x"/> with <paramref name="y"/>.
        /// </typeparam>
        private static void InvokeSpanScalarIntoSpan<T, TBinaryOperator>(
            ReadOnlySpan<T> x, T y, Span<T> destination)
            where TBinaryOperator : struct, IBinaryOperator<T> =>
            InvokeSpanScalarIntoSpan<T, IdentityOperator<T>, TBinaryOperator>(x, y, destination);

        /// <summary>
        /// Performs an element-wise operation on <paramref name="x"/> and <paramref name="y"/>,
        /// and writes the results to <paramref name="destination"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TTransformOperator">
        /// Specifies the operation to perform on each element loaded from <paramref name="x"/>.
        /// It is not used with <paramref name="y"/>.
        /// </typeparam>
        /// <typeparam name="TBinaryOperator">
        /// Specifies the operation to perform on the transformed value from <paramref name="x"/> with <paramref name="y"/>.
        /// </typeparam>
        private static void InvokeSpanScalarIntoSpan<T, TTransformOperator, TBinaryOperator>(
            ReadOnlySpan<T> x, T y, Span<T> destination)
            where TTransformOperator : struct, IUnaryOperator<T, T>
            where TBinaryOperator : struct, IBinaryOperator<T>
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

            ref T xRef = ref MemoryMarshal.GetReference(x);
            ref T dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)x.Length;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && TTransformOperator.Vectorizable && TBinaryOperator.Vectorizable)
            {
                if (remainder >= (uint)Vector512<T>.Count)
                {
                    Vectorized512(ref xRef, y, ref dRef, remainder);
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

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && TTransformOperator.Vectorizable && TBinaryOperator.Vectorizable)
            {
                if (remainder >= (uint)Vector256<T>.Count)
                {
                    Vectorized256(ref xRef, y, ref dRef, remainder);
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

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && TTransformOperator.Vectorizable && TBinaryOperator.Vectorizable)
            {
                if (remainder >= (uint)Vector128<T>.Count)
                {
                    Vectorized128(ref xRef, y, ref dRef, remainder);
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
            static void SoftwareFallback(ref T xRef, T y, ref T dRef, nuint length)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, i) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, i)),
                                                                     y);
                }
            }

            static void Vectorized128(ref T xRef, T y, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector128<T> yVec = Vector128.Create(y);

                Vector128<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef)),
                                                          yVec);
                Vector128<T> end = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count)),
                                                          yVec);

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

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3))),
                                                                 yVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7))),
                                                                 yVec);

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

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3))),
                                                                 yVec);

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7))),
                                                                 yVec);

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
                            Vector128<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 8))),
                                                                         yVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 7))),
                                                                         yVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 6))),
                                                                         yVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 5))),
                                                                         yVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 4))),
                                                                         yVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 3))),
                                                                         yVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector128<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 2))),
                                                                         yVec);
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

            static void Vectorized256(ref T xRef, T y, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector256<T> yVec = Vector256.Create(y);

                Vector256<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef)),
                                                          yVec);
                Vector256<T> end = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count)),
                                                          yVec);

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

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3))),
                                                                 yVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7))),
                                                                 yVec);

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

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3))),
                                                                 yVec);

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7))),
                                                                 yVec);

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
                            Vector256<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 8))),
                                                                         yVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 7))),
                                                                         yVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 6))),
                                                                         yVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 5))),
                                                                         yVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 4))),
                                                                         yVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 3))),
                                                                         yVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector256<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 2))),
                                                                         yVec);
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

            static void Vectorized512(ref T xRef, T y, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector512<T> yVec = Vector512.Create(y);

                Vector512<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef)),
                                                          yVec);
                Vector512<T> end = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)Vector512<T>.Count)),
                                                          yVec);

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

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3))),
                                                                 yVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7))),
                                                                 yVec);

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

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3))),
                                                                 yVec);

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4))),
                                                                 yVec);
                                vector2 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5))),
                                                                 yVec);
                                vector3 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6))),
                                                                 yVec);
                                vector4 = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7))),
                                                                 yVec);

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
                            Vector512<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 8))),
                                                                         yVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 7))),
                                                                         yVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 6))),
                                                                         yVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 5))),
                                                                         yVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 4))),
                                                                         yVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 3))),
                                                                         yVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector512<T> vector = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 2))),
                                                                         yVec);
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
            static void VectorizedSmall(ref T xRef, T y, ref T dRef, nuint remainder)
            {
                if (sizeof(T) == 1)
                {
                    VectorizedSmall1(ref xRef, y, ref dRef, remainder);
                }
                else if (sizeof(T) == 2)
                {
                    VectorizedSmall2(ref xRef, y, ref dRef, remainder);
                }
                else if (sizeof(T) == 4)
                {
                    VectorizedSmall4(ref xRef, y, ref dRef, remainder);
                }
                else
                {
                    Debug.Assert(sizeof(T) == 8);
                    VectorizedSmall8(ref xRef, y, ref dRef, remainder);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall1(ref T xRef, T y, ref T dRef, nuint remainder)
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

                            Vector256<T> yVec = Vector256.Create(y);

                            Vector256<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef)),
                                                                      yVec);
                            Vector256<T> end = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count)),
                                                                      yVec);

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    // One Vector256's worth of data.
                    case 32:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef)),
                                                                      Vector256.Create(y));
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

                            Vector128<T> yVec = Vector128.Create(y);

                            Vector128<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef)),
                                                                                                yVec);
                            Vector128<T> end = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count)),
                                                                                                yVec);

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    // One Vector128's worth of data.
                    case 16:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef)),
                                                                                                Vector128.Create(y));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    // Cases that are smaller than a single vector. No SIMD; just jump to the length and fall through each
                    // case to unroll the whole processing.
                    case 15:
                        Unsafe.Add(ref dRef, 14) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 14)),
                                                                          y);
                        goto case 14;

                    case 14:
                        Unsafe.Add(ref dRef, 13) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 13)),
                                                                          y);
                        goto case 13;

                    case 13:
                        Unsafe.Add(ref dRef, 12) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 12)),
                                                                          y);
                        goto case 12;

                    case 12:
                        Unsafe.Add(ref dRef, 11) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 11)),
                                                                          y);
                        goto case 11;

                    case 11:
                        Unsafe.Add(ref dRef, 10) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 10)),
                                                                          y);
                        goto case 10;

                    case 10:
                        Unsafe.Add(ref dRef, 9) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 9)),
                                                                         y);
                        goto case 9;

                    case 9:
                        Unsafe.Add(ref dRef, 8) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 8)),
                                                                         y);
                        goto case 8;

                    case 8:
                        Unsafe.Add(ref dRef, 7) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 7)),
                                                                         y);
                        goto case 7;

                    case 7:
                        Unsafe.Add(ref dRef, 6) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 6)),
                                                                         y);
                        goto case 6;

                    case 6:
                        Unsafe.Add(ref dRef, 5) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 5)),
                                                                         y);
                        goto case 5;

                    case 5:
                        Unsafe.Add(ref dRef, 4) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 4)),
                                                                         y);
                        goto case 4;

                    case 4:
                        Unsafe.Add(ref dRef, 3) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 3)),
                                                                         y);
                        goto case 3;

                    case 3:
                        Unsafe.Add(ref dRef, 2) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 2)),
                                                                         y);
                        goto case 2;

                    case 2:
                        Unsafe.Add(ref dRef, 1) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 1)),
                                                                         y);
                        goto case 1;

                    case 1:
                        dRef = TBinaryOperator.Invoke(TTransformOperator.Invoke(xRef), y);
                        goto case 0;

                    case 0:
                        break;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall2(ref T xRef, T y, ref T dRef, nuint remainder)
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

                            Vector256<T> yVec = Vector256.Create(y);

                            Vector256<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef)),
                                                                      yVec);
                            Vector256<T> end = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count)),
                                                                      yVec);

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    // One Vector256's worth of data.
                    case 16:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef)),
                                                                      Vector256.Create(y));
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

                            Vector128<T> yVec = Vector128.Create(y);

                            Vector128<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef)),
                                                                                                yVec);
                            Vector128<T> end = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count)),
                                                                                                yVec);

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    // One Vector128's worth of data.
                    case 8:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef)),
                                                                                                Vector128.Create(y));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    // Cases that are smaller than a single vector. No SIMD; just jump to the length and fall through each
                    // case to unroll the whole processing.
                    case 7:
                        Unsafe.Add(ref dRef, 6) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 6)),
                                                                         y);
                        goto case 6;

                    case 6:
                        Unsafe.Add(ref dRef, 5) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 5)),
                                                                         y);
                        goto case 5;

                    case 5:
                        Unsafe.Add(ref dRef, 4) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 4)),
                                                                         y);
                        goto case 4;

                    case 4:
                        Unsafe.Add(ref dRef, 3) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 3)),
                                                                         y);
                        goto case 3;

                    case 3:
                        Unsafe.Add(ref dRef, 2) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 2)),
                                                                         y);
                        goto case 2;

                    case 2:
                        Unsafe.Add(ref dRef, 1) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 1)),
                                                                         y);
                        goto case 1;

                    case 1:
                        dRef = TBinaryOperator.Invoke(TTransformOperator.Invoke(xRef), y);
                        goto case 0;

                    case 0:
                        break;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall4(ref T xRef, T y, ref T dRef, nuint remainder)
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

                            Vector256<T> yVec = Vector256.Create(y);

                            Vector256<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef)),
                                                                      yVec);
                            Vector256<T> end = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count)),
                                                                      yVec);

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    case 8:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef)),
                                                                      Vector256.Create(y));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 7:
                    case 6:
                    case 5:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> yVec = Vector128.Create(y);

                            Vector128<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef)),
                                                                                                yVec);
                            Vector128<T> end = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count)),
                                                                                                yVec);

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    case 4:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef)),
                                                                                                Vector128.Create(y));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 3:
                        {
                            Unsafe.Add(ref dRef, 2) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 2)),
                                                                             y);
                            goto case 2;
                        }

                    case 2:
                        {
                            Unsafe.Add(ref dRef, 1) = TBinaryOperator.Invoke(TTransformOperator.Invoke(Unsafe.Add(ref xRef, 1)),
                                                                              y);
                            goto case 1;
                        }

                    case 1:
                        {
                            dRef = TBinaryOperator.Invoke(TTransformOperator.Invoke(xRef), y);
                            goto case 0;
                        }

                    case 0:
                        {
                            break;
                        }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall8(ref T xRef, T y, ref T dRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 8);

                switch (remainder)
                {
                    case 7:
                    case 6:
                    case 5:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> yVec = Vector256.Create(y);

                            Vector256<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef)),
                                                                      yVec);
                            Vector256<T> end = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count)),
                                                                      yVec);

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    case 4:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector256.LoadUnsafe(ref xRef)),
                                                                      Vector256.Create(y));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 3:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> yVec = Vector128.Create(y);

                            Vector128<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef)),
                                                                                                yVec);
                            Vector128<T> end = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count)),
                                                                                                yVec);

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    case 2:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TBinaryOperator.Invoke(TTransformOperator.Invoke(Vector128.LoadUnsafe(ref xRef)),
                                                                                                Vector128.Create(y));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 1:
                        {
                            dRef = TBinaryOperator.Invoke(TTransformOperator.Invoke(xRef), y);
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
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TTernaryOperator">
        /// Specifies the operation to perform on the pair-wise elements loaded from <paramref name="x"/>, <paramref name="y"/>,
        /// and <paramref name="z"/>.
        /// </typeparam>
        private static void InvokeSpanSpanSpanIntoSpan<T, TTernaryOperator>(
            ReadOnlySpan<T> x, ReadOnlySpan<T> y, ReadOnlySpan<T> z, Span<T> destination)
            where TTernaryOperator : struct, ITernaryOperator<T>
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

            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref T xRef = ref MemoryMarshal.GetReference(x);
            ref T yRef = ref MemoryMarshal.GetReference(y);
            ref T zRef = ref MemoryMarshal.GetReference(z);
            ref T dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)x.Length;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported)
            {
                if (remainder >= (uint)Vector512<T>.Count)
                {
                    Vectorized512(ref xRef, ref yRef, ref zRef, ref dRef, remainder);
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

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported)
            {
                if (remainder >= (uint)Vector256<T>.Count)
                {
                    Vectorized256(ref xRef, ref yRef, ref zRef, ref dRef, remainder);
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

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported)
            {
                if (remainder >= (uint)Vector128<T>.Count)
                {
                    Vectorized128(ref xRef, ref yRef, ref zRef, ref dRef, remainder);
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
            static void SoftwareFallback(ref T xRef, ref T yRef, ref T zRef, ref T dRef, nuint length)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, i) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                      Unsafe.Add(ref yRef, i),
                                                                      Unsafe.Add(ref zRef, i));
                }
            }

            static void Vectorized128(ref T xRef, ref T yRef, ref T zRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                           Vector128.LoadUnsafe(ref yRef),
                                                           Vector128.LoadUnsafe(ref zRef));
                Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                           Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count),
                                                           Vector128.LoadUnsafe(ref zRef, remainder - (uint)Vector128<T>.Count));

                if (remainder > (uint)(Vector128<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    fixed (T* pz = &zRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;
                        T* zPtr = pz;
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
                            yPtr += misalignment;
                            zPtr += misalignment;
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

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                yPtr += (uint)(Vector128<T>.Count * 8);
                                zPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector128<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                yPtr += (uint)(Vector128<T>.Count * 8);
                                zPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
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
                remainder = (remainder + (uint)(Vector128<T>.Count - 1)) & (nuint)(-Vector128<T>.Count);

                switch (remainder / (uint)Vector128<T>.Count)
                {
                    case 8:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 8)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 8)),
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 8)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 7)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 7)),
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 7)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 6)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 6)),
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 6)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 5)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 5)),
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 5)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 4)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 4)),
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 4)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 3)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 3)),
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 3)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 2)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 2)),
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 2)));
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

            static void Vectorized256(ref T xRef, ref T yRef, ref T zRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                           Vector256.LoadUnsafe(ref yRef),
                                                           Vector256.LoadUnsafe(ref zRef));
                Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                           Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count),
                                                           Vector256.LoadUnsafe(ref zRef, remainder - (uint)Vector256<T>.Count));

                if (remainder > (uint)(Vector256<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    fixed (T* pz = &zRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;
                        T* zPtr = pz;
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

                            nuint misalignment = ((uint)sizeof(Vector256<T>) - ((nuint)dPtr % (uint)sizeof(Vector256<T>))) / (nuint)sizeof(T);

                            xPtr += misalignment;
                            yPtr += misalignment;
                            zPtr += misalignment;
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

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                yPtr += (uint)(Vector256<T>.Count * 8);
                                zPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector256<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                yPtr += (uint)(Vector256<T>.Count * 8);
                                zPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
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
                remainder = (remainder + (uint)(Vector256<T>.Count - 1)) & (nuint)(-Vector256<T>.Count);

                switch (remainder / (uint)Vector256<T>.Count)
                {
                    case 8:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 8)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 8)),
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 8)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 7)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 7)),
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 7)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 6)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 6)),
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 6)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 5)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 5)),
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 5)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 4)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 4)),
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 4)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 3)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 3)),
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 3)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 2)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 2)),
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 2)));
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

            static void Vectorized512(ref T xRef, ref T yRef, ref T zRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector512<T> beg = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef),
                                                           Vector512.LoadUnsafe(ref yRef),
                                                           Vector512.LoadUnsafe(ref zRef));
                Vector512<T> end = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)Vector512<T>.Count),
                                                           Vector512.LoadUnsafe(ref yRef, remainder - (uint)Vector512<T>.Count),
                                                           Vector512.LoadUnsafe(ref zRef, remainder - (uint)Vector512<T>.Count));

                if (remainder > (uint)(Vector512<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    fixed (T* pz = &zRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;
                        T* zPtr = pz;
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
                            yPtr += misalignment;
                            zPtr += misalignment;
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

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                yPtr += (uint)(Vector512<T>.Count * 8);
                                zPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector512<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                yPtr += (uint)(Vector512<T>.Count * 8);
                                zPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
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
                remainder = (remainder + (uint)(Vector512<T>.Count - 1)) & (nuint)(-Vector512<T>.Count);

                switch (remainder / (uint)Vector512<T>.Count)
                {
                    case 8:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 8)),
                                                                          Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 8)),
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 8)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 7)),
                                                                          Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 7)),
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 7)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 6)),
                                                                          Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 6)),
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 6)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 5)),
                                                                          Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 5)),
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 5)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 4)),
                                                                          Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 4)),
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 4)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 3)),
                                                                          Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 3)),
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 3)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 2)),
                                                                          Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 2)),
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 2)));
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
            static void VectorizedSmall(ref T xRef, ref T yRef, ref T zRef, ref T dRef, nuint remainder)
            {
                if (sizeof(T) == 1)
                {
                    VectorizedSmall1(ref xRef, ref yRef, ref zRef, ref dRef, remainder);
                }
                else if (sizeof(T) == 2)
                {
                    VectorizedSmall2(ref xRef, ref yRef, ref zRef, ref dRef, remainder);
                }
                else if (sizeof(T) == 4)
                {
                    VectorizedSmall4(ref xRef, ref yRef, ref zRef, ref dRef, remainder);
                }
                else
                {
                    Debug.Assert(sizeof(T) == 8);
                    VectorizedSmall8(ref xRef, ref yRef, ref zRef, ref dRef, remainder);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall1(ref T xRef, ref T yRef, ref T zRef, ref T dRef, nuint remainder)
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

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       Vector256.LoadUnsafe(ref zRef));
                            Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                       Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count),
                                                                       Vector256.LoadUnsafe(ref zRef, remainder - (uint)Vector256<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    // One Vector256's worth of data.
                    case 32:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                           Vector256.LoadUnsafe(ref yRef),
                                                                           Vector256.LoadUnsafe(ref zRef));
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

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                           Vector128.LoadUnsafe(ref yRef),
                                                                           Vector128.LoadUnsafe(ref zRef));
                            Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                           Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count),
                                                                           Vector128.LoadUnsafe(ref zRef, remainder - (uint)Vector128<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    // One Vector128's worth of data.
                    case 16:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                           Vector128.LoadUnsafe(ref yRef),
                                                                           Vector128.LoadUnsafe(ref zRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    // Cases that are smaller than a single vector. No SIMD; just jump to the length and fall through each
                    // case to unroll the whole processing.
                    case 15:
                        Unsafe.Add(ref dRef, 14) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 14),
                                                                          Unsafe.Add(ref yRef, 14),
                                                                          Unsafe.Add(ref zRef, 14));
                        goto case 14;

                    case 14:
                        Unsafe.Add(ref dRef, 13) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 13),
                                                                          Unsafe.Add(ref yRef, 13),
                                                                          Unsafe.Add(ref zRef, 13));
                        goto case 13;

                    case 13:
                        Unsafe.Add(ref dRef, 12) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 12),
                                                                          Unsafe.Add(ref yRef, 12),
                                                                          Unsafe.Add(ref zRef, 12));
                        goto case 12;

                    case 12:
                        Unsafe.Add(ref dRef, 11) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 11),
                                                                          Unsafe.Add(ref yRef, 11),
                                                                          Unsafe.Add(ref zRef, 11));
                        goto case 11;

                    case 11:
                        Unsafe.Add(ref dRef, 10) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 10),
                                                                          Unsafe.Add(ref yRef, 10),
                                                                          Unsafe.Add(ref zRef, 10));
                        goto case 10;

                    case 10:
                        Unsafe.Add(ref dRef, 9) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 9),
                                                                          Unsafe.Add(ref yRef, 9),
                                                                          Unsafe.Add(ref zRef, 9));
                        goto case 9;

                    case 9:
                        Unsafe.Add(ref dRef, 8) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 8),
                                                                          Unsafe.Add(ref yRef, 8),
                                                                          Unsafe.Add(ref zRef, 8));
                        goto case 8;

                    case 8:
                        Unsafe.Add(ref dRef, 7) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 7),
                                                                          Unsafe.Add(ref yRef, 7),
                                                                          Unsafe.Add(ref zRef, 7));
                        goto case 7;

                    case 7:
                        Unsafe.Add(ref dRef, 6) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 6),
                                                                          Unsafe.Add(ref yRef, 6),
                                                                          Unsafe.Add(ref zRef, 6));
                        goto case 6;

                    case 6:
                        Unsafe.Add(ref dRef, 5) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 5),
                                                                          Unsafe.Add(ref yRef, 5),
                                                                          Unsafe.Add(ref zRef, 5));
                        goto case 5;

                    case 5:
                        Unsafe.Add(ref dRef, 4) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 4),
                                                                          Unsafe.Add(ref yRef, 4),
                                                                          Unsafe.Add(ref zRef, 4));
                        goto case 4;

                    case 4:
                        Unsafe.Add(ref dRef, 3) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 3),
                                                                          Unsafe.Add(ref yRef, 3),
                                                                          Unsafe.Add(ref zRef, 3));
                        goto case 3;

                    case 3:
                        Unsafe.Add(ref dRef, 2) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 2),
                                                                          Unsafe.Add(ref yRef, 2),
                                                                          Unsafe.Add(ref zRef, 2));
                        goto case 2;

                    case 2:
                        Unsafe.Add(ref dRef, 1) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 1),
                                                                          Unsafe.Add(ref yRef, 1),
                                                                          Unsafe.Add(ref zRef, 1));
                        goto case 1;

                    case 1:
                        dRef = TTernaryOperator.Invoke(xRef, yRef, zRef);
                        goto case 0;

                    case 0:
                        break;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall2(ref T xRef, ref T yRef, ref T zRef, ref T dRef, nuint remainder)
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

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       Vector256.LoadUnsafe(ref zRef));
                            Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                       Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count),
                                                                       Vector256.LoadUnsafe(ref zRef, remainder - (uint)Vector256<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    // One Vector256's worth of data.
                    case 16:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                           Vector256.LoadUnsafe(ref yRef),
                                                                           Vector256.LoadUnsafe(ref zRef));
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

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                           Vector128.LoadUnsafe(ref yRef),
                                                                           Vector128.LoadUnsafe(ref zRef));
                            Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                           Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count),
                                                                           Vector128.LoadUnsafe(ref zRef, remainder - (uint)Vector128<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    // One Vector128's worth of data.
                    case 8:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                           Vector128.LoadUnsafe(ref yRef),
                                                                           Vector128.LoadUnsafe(ref zRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    // Cases that are smaller than a single vector. No SIMD; just jump to the length and fall through each
                    // case to unroll the whole processing.
                    case 7:
                        Unsafe.Add(ref dRef, 6) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 6),
                                                                          Unsafe.Add(ref yRef, 6),
                                                                          Unsafe.Add(ref zRef, 6));
                        goto case 6;

                    case 6:
                        Unsafe.Add(ref dRef, 5) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 5),
                                                                          Unsafe.Add(ref yRef, 5),
                                                                          Unsafe.Add(ref zRef, 5));
                        goto case 5;

                    case 5:
                        Unsafe.Add(ref dRef, 4) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 4),
                                                                          Unsafe.Add(ref yRef, 4),
                                                                          Unsafe.Add(ref zRef, 4));
                        goto case 4;

                    case 4:
                        Unsafe.Add(ref dRef, 3) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 3),
                                                                          Unsafe.Add(ref yRef, 3),
                                                                          Unsafe.Add(ref zRef, 3));
                        goto case 3;

                    case 3:
                        Unsafe.Add(ref dRef, 2) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 2),
                                                                          Unsafe.Add(ref yRef, 2),
                                                                          Unsafe.Add(ref zRef, 2));
                        goto case 2;

                    case 2:
                        Unsafe.Add(ref dRef, 1) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 1),
                                                                          Unsafe.Add(ref yRef, 1),
                                                                          Unsafe.Add(ref zRef, 1));
                        goto case 1;

                    case 1:
                        dRef = TTernaryOperator.Invoke(xRef, yRef, zRef);
                        goto case 0;

                    case 0:
                        break;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall4(ref T xRef, ref T yRef, ref T zRef, ref T dRef, nuint remainder)
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

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       Vector256.LoadUnsafe(ref zRef));
                            Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                       Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count),
                                                                       Vector256.LoadUnsafe(ref zRef, remainder - (uint)Vector256<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    case 8:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                           Vector256.LoadUnsafe(ref yRef),
                                                                           Vector256.LoadUnsafe(ref zRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 7:
                    case 6:
                    case 5:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                           Vector128.LoadUnsafe(ref yRef),
                                                                           Vector128.LoadUnsafe(ref zRef));
                            Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                           Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count),
                                                                           Vector128.LoadUnsafe(ref zRef, remainder - (uint)Vector128<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    case 4:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                           Vector128.LoadUnsafe(ref yRef),
                                                                           Vector128.LoadUnsafe(ref zRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 3:
                        {
                            Unsafe.Add(ref dRef, 2) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 2),
                                                                              Unsafe.Add(ref yRef, 2),
                                                                              Unsafe.Add(ref zRef, 2));
                            goto case 2;
                        }

                    case 2:
                        {
                            Unsafe.Add(ref dRef, 1) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 1),
                                                                              Unsafe.Add(ref yRef, 1),
                                                                              Unsafe.Add(ref zRef, 1));
                            goto case 1;
                        }

                    case 1:
                        {
                            dRef = TTernaryOperator.Invoke(xRef, yRef, zRef);
                            goto case 0;
                        }

                    case 0:
                        {
                            break;
                        }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall8(ref T xRef, ref T yRef, ref T zRef, ref T dRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 8);

                switch (remainder)
                {
                    case 7:
                    case 6:
                    case 5:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       Vector256.LoadUnsafe(ref zRef));
                            Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                       Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count),
                                                                       Vector256.LoadUnsafe(ref zRef, remainder - (uint)Vector256<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    case 4:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       Vector256.LoadUnsafe(ref zRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 3:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.LoadUnsafe(ref yRef),
                                                                       Vector128.LoadUnsafe(ref zRef));
                            Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                       Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count),
                                                                       Vector128.LoadUnsafe(ref zRef, remainder - (uint)Vector128<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    case 2:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.LoadUnsafe(ref yRef),
                                                                       Vector128.LoadUnsafe(ref zRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 1:
                        {
                            dRef = TTernaryOperator.Invoke(xRef, yRef, zRef);
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
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TTernaryOperator">
        /// Specifies the operation to perform on the pair-wise elements loaded from <paramref name="x"/> and <paramref name="y"/>
        /// with <paramref name="z"/>.
        /// </typeparam>
        private static void InvokeSpanSpanScalarIntoSpan<T, TTernaryOperator>(
            ReadOnlySpan<T> x, ReadOnlySpan<T> y, T z, Span<T> destination)
            where TTernaryOperator : struct, ITernaryOperator<T>
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

            ref T xRef = ref MemoryMarshal.GetReference(x);
            ref T yRef = ref MemoryMarshal.GetReference(y);
            ref T dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)x.Length;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported)
            {
                if (remainder >= (uint)Vector512<T>.Count)
                {
                    Vectorized512(ref xRef, ref yRef, z, ref dRef, remainder);
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

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported)
            {
                if (remainder >= (uint)Vector256<T>.Count)
                {
                    Vectorized256(ref xRef, ref yRef, z, ref dRef, remainder);
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

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported)
            {
                if (remainder >= (uint)Vector128<T>.Count)
                {
                    Vectorized128(ref xRef, ref yRef, z, ref dRef, remainder);
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
            static void SoftwareFallback(ref T xRef, ref T yRef, T z, ref T dRef, nuint length)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, i) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                      Unsafe.Add(ref yRef, i),
                                                                      z);
                }
            }

            static void Vectorized128(ref T xRef, ref T yRef, T z, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector128<T> zVec = Vector128.Create(z);

                Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                               Vector128.LoadUnsafe(ref yRef),
                                                               zVec);
                Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                               Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count),
                                                               zVec);

                if (remainder > (uint)(Vector128<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;
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
                            yPtr += misalignment;
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

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  zVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  zVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                yPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector128<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  zVec);

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  Vector128.Load(yPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  zVec);

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                yPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
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
                remainder = (remainder + (uint)(Vector128<T>.Count - 1)) & (nuint)(-Vector128<T>.Count);

                switch (remainder / (uint)Vector128<T>.Count)
                {
                    case 8:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 8)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 8)),
                                                                          zVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 7)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 7)),
                                                                          zVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 6)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 6)),
                                                                          zVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 5)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 5)),
                                                                          zVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 4)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 4)),
                                                                          zVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 3)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 3)),
                                                                          zVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 2)),
                                                                          Vector128.LoadUnsafe(ref yRef, remainder - (uint)(Vector128<T>.Count * 2)),
                                                                          zVec);
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

            static void Vectorized256(ref T xRef, ref T yRef, T z, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector256<T> zVec = Vector256.Create(z);

                Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                               Vector256.LoadUnsafe(ref yRef),
                                                               zVec);
                Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                               Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count),
                                                               zVec);

                if (remainder > (uint)(Vector256<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;
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
                            yPtr += misalignment;
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

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  zVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  zVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                yPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector256<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  zVec);

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  Vector256.Load(yPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  zVec);

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                yPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
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
                remainder = (remainder + (uint)(Vector256<T>.Count - 1)) & (nuint)(-Vector256<T>.Count);

                switch (remainder / (uint)Vector256<T>.Count)
                {
                    case 8:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 8)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 8)),
                                                                          zVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 7)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 7)),
                                                                          zVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 6)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 6)),
                                                                          zVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 5)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 5)),
                                                                          zVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 4)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 4)),
                                                                          zVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 3)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 3)),
                                                                          zVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 2)),
                                                                          Vector256.LoadUnsafe(ref yRef, remainder - (uint)(Vector256<T>.Count * 2)),
                                                                          zVec);
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

            static void Vectorized512(ref T xRef, ref T yRef, T z, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector512<T> zVec = Vector512.Create(z);

                Vector512<T> beg = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef),
                                                           Vector512.LoadUnsafe(ref yRef),
                                                           zVec);
                Vector512<T> end = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)Vector512<T>.Count),
                                                           Vector512.LoadUnsafe(ref yRef, remainder - (uint)Vector512<T>.Count),
                                                           zVec);

                if (remainder > (uint)(Vector512<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* py = &yRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* yPtr = py;
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
                            yPtr += misalignment;
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

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  zVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  zVec);

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                yPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector512<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  zVec);

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  zVec);
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  zVec);
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  zVec);
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  Vector512.Load(yPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  zVec);

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                yPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
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
                remainder = (remainder + (uint)(Vector512<T>.Count - 1)) & (nuint)(-Vector512<T>.Count);

                switch (remainder / (uint)Vector512<T>.Count)
                {
                    case 8:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 8)),
                                                                          Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 8)),
                                                                          zVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 7)),
                                                                          Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 7)),
                                                                          zVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 6)),
                                                                          Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 6)),
                                                                          zVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 5)),
                                                                          Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 5)),
                                                                          zVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 4)),
                                                                          Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 4)),
                                                                          zVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 3)),
                                                                          Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 3)),
                                                                          zVec);
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 2)),
                                                                          Vector512.LoadUnsafe(ref yRef, remainder - (uint)(Vector512<T>.Count * 2)),
                                                                          zVec);
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
            static void VectorizedSmall(ref T xRef, ref T yRef, T z, ref T dRef, nuint remainder)
            {
                if (sizeof(T) == 1)
                {
                    VectorizedSmall1(ref xRef, ref yRef, z, ref dRef, remainder);
                }
                else if (sizeof(T) == 2)
                {
                    VectorizedSmall2(ref xRef, ref yRef, z, ref dRef, remainder);
                }
                else if (sizeof(T) == 4)
                {
                    VectorizedSmall4(ref xRef, ref yRef, z, ref dRef, remainder);
                }
                else
                {
                    Debug.Assert(sizeof(T) == 8);
                    VectorizedSmall8(ref xRef, ref yRef, z, ref dRef, remainder);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall1(ref T xRef, ref T yRef, T z, ref T dRef, nuint remainder)
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

                            Vector256<T> zVec = Vector256.Create(z);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       zVec);
                            Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                       Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count),
                                                                       zVec);

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    // One Vector256's worth of data.
                    case 32:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       Vector256.Create(z));
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

                            Vector128<T> zVec = Vector128.Create(z);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.LoadUnsafe(ref yRef),
                                                                       zVec);
                            Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                       Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count),
                                                                       zVec);

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    // One Vector128's worth of data.
                    case 16:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.LoadUnsafe(ref yRef),
                                                                       Vector128.Create(z));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    // Cases that are smaller than a single vector. No SIMD; just jump to the length and fall through each
                    // case to unroll the whole processing.
                    case 15:
                        Unsafe.Add(ref dRef, 14) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 14),
                                                                          Unsafe.Add(ref yRef, 14),
                                                                          z);
                        goto case 14;

                    case 14:
                        Unsafe.Add(ref dRef, 13) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 13),
                                                                          Unsafe.Add(ref yRef, 13),
                                                                          z);
                        goto case 13;

                    case 13:
                        Unsafe.Add(ref dRef, 12) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 12),
                                                                          Unsafe.Add(ref yRef, 12),
                                                                          z);
                        goto case 12;

                    case 12:
                        Unsafe.Add(ref dRef, 11) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 11),
                                                                          Unsafe.Add(ref yRef, 11),
                                                                          z);
                        goto case 11;

                    case 11:
                        Unsafe.Add(ref dRef, 10) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 10),
                                                                          Unsafe.Add(ref yRef, 10),
                                                                          z);
                        goto case 10;

                    case 10:
                        Unsafe.Add(ref dRef, 9) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 9),
                                                                          Unsafe.Add(ref yRef, 9),
                                                                          z);
                        goto case 9;

                    case 9:
                        Unsafe.Add(ref dRef, 8) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 8),
                                                                          Unsafe.Add(ref yRef, 8),
                                                                          z);
                        goto case 8;

                    case 8:
                        Unsafe.Add(ref dRef, 7) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 7),
                                                                          Unsafe.Add(ref yRef, 7),
                                                                          z);
                        goto case 7;

                    case 7:
                        Unsafe.Add(ref dRef, 6) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 6),
                                                                          Unsafe.Add(ref yRef, 6),
                                                                          z);
                        goto case 6;

                    case 6:
                        Unsafe.Add(ref dRef, 5) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 5),
                                                                          Unsafe.Add(ref yRef, 5),
                                                                          z);
                        goto case 5;

                    case 5:
                        Unsafe.Add(ref dRef, 4) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 4),
                                                                          Unsafe.Add(ref yRef, 4),
                                                                          z);
                        goto case 4;

                    case 4:
                        Unsafe.Add(ref dRef, 3) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 3),
                                                                          Unsafe.Add(ref yRef, 3),
                                                                          z);
                        goto case 3;

                    case 3:
                        Unsafe.Add(ref dRef, 2) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 2),
                                                                          Unsafe.Add(ref yRef, 2),
                                                                          z);
                        goto case 2;

                    case 2:
                        Unsafe.Add(ref dRef, 1) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 1),
                                                                          Unsafe.Add(ref yRef, 1),
                                                                          z);
                        goto case 1;

                    case 1:
                        dRef = TTernaryOperator.Invoke(xRef, yRef, z);
                        goto case 0;

                    case 0:
                        break;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall2(ref T xRef, ref T yRef, T z, ref T dRef, nuint remainder)
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

                            Vector256<T> zVec = Vector256.Create(z);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       zVec);
                            Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                       Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count),
                                                                       zVec);

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    // One Vector256's worth of data.
                    case 16:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       Vector256.Create(z));
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

                            Vector128<T> zVec = Vector128.Create(z);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.LoadUnsafe(ref yRef),
                                                                       zVec);
                            Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                       Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count),
                                                                       zVec);

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    // One Vector128's worth of data.
                    case 8:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.LoadUnsafe(ref yRef),
                                                                       Vector128.Create(z));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    // Cases that are smaller than a single vector. No SIMD; just jump to the length and fall through each
                    // case to unroll the whole processing.
                    case 7:
                        Unsafe.Add(ref dRef, 6) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 6),
                                                                          Unsafe.Add(ref yRef, 6),
                                                                          z);
                        goto case 6;

                    case 6:
                        Unsafe.Add(ref dRef, 5) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 5),
                                                                          Unsafe.Add(ref yRef, 5),
                                                                          z);
                        goto case 5;

                    case 5:
                        Unsafe.Add(ref dRef, 4) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 4),
                                                                          Unsafe.Add(ref yRef, 4),
                                                                          z);
                        goto case 4;

                    case 4:
                        Unsafe.Add(ref dRef, 3) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 3),
                                                                          Unsafe.Add(ref yRef, 3),
                                                                          z);
                        goto case 3;

                    case 3:
                        Unsafe.Add(ref dRef, 2) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 2),
                                                                          Unsafe.Add(ref yRef, 2),
                                                                          z);
                        goto case 2;

                    case 2:
                        Unsafe.Add(ref dRef, 1) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 1),
                                                                          Unsafe.Add(ref yRef, 1),
                                                                          z);
                        goto case 1;

                    case 1:
                        dRef = TTernaryOperator.Invoke(xRef, yRef, z);
                        goto case 0;

                    case 0:
                        break;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall4(ref T xRef, ref T yRef, T z, ref T dRef, nuint remainder)
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

                            Vector256<T> zVec = Vector256.Create(z);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       zVec);
                            Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                       Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count),
                                                                       zVec);

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    case 8:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       Vector256.Create(z));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 7:
                    case 6:
                    case 5:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> zVec = Vector128.Create(z);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.LoadUnsafe(ref yRef),
                                                                       zVec);
                            Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                       Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count),
                                                                       zVec);

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    case 4:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.LoadUnsafe(ref yRef),
                                                                       Vector128.Create(z));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 3:
                        {
                            Unsafe.Add(ref dRef, 2) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 2),
                                                                              Unsafe.Add(ref yRef, 2),
                                                                              z);
                            goto case 2;
                        }

                    case 2:
                        {
                            Unsafe.Add(ref dRef, 1) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 1),
                                                                              Unsafe.Add(ref yRef, 1),
                                                                              z);
                            goto case 1;
                        }

                    case 1:
                        {
                            dRef = TTernaryOperator.Invoke(xRef, yRef, z);
                            goto case 0;
                        }

                    case 0:
                        {
                            break;
                        }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall8(ref T xRef, ref T yRef, T z, ref T dRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 8);

                switch (remainder)
                {
                    case 7:
                    case 6:
                    case 5:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> zVec = Vector256.Create(z);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       zVec);
                            Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                       Vector256.LoadUnsafe(ref yRef, remainder - (uint)Vector256<T>.Count),
                                                                       zVec);

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    case 4:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.LoadUnsafe(ref yRef),
                                                                       Vector256.Create(z));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 3:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> zVec = Vector128.Create(z);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.LoadUnsafe(ref yRef),
                                                                       zVec);
                            Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                       Vector128.LoadUnsafe(ref yRef, remainder - (uint)Vector128<T>.Count),
                                                                       zVec);

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    case 2:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.LoadUnsafe(ref yRef),
                                                                       Vector128.Create(z));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 1:
                        {
                            dRef = TTernaryOperator.Invoke(xRef, yRef, z);
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
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TTernaryOperator">
        /// Specifies the operation to perform on the pair-wise element loaded from <paramref name="x"/>, with <paramref name="y"/>,
        /// and the element loaded from <paramref name="z"/>.
        /// </typeparam>
        private static void InvokeSpanScalarSpanIntoSpan<T, TTernaryOperator>(
            ReadOnlySpan<T> x, T y, ReadOnlySpan<T> z, Span<T> destination)
            where TTernaryOperator : struct, ITernaryOperator<T>
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

            // Since every branch has a cost and since that cost is
            // essentially lost for larger inputs, we do branches
            // in a way that allows us to have the minimum possible
            // for small sizes

            ref T xRef = ref MemoryMarshal.GetReference(x);
            ref T zRef = ref MemoryMarshal.GetReference(z);
            ref T dRef = ref MemoryMarshal.GetReference(destination);

            nuint remainder = (uint)x.Length;

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported)
            {
                if (remainder >= (uint)Vector512<T>.Count)
                {
                    Vectorized512(ref xRef, y, ref zRef, ref dRef, remainder);
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

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported)
            {
                if (remainder >= (uint)Vector256<T>.Count)
                {
                    Vectorized256(ref xRef, y, ref zRef, ref dRef, remainder);
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

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported)
            {
                if (remainder >= (uint)Vector128<T>.Count)
                {
                    Vectorized128(ref xRef, y, ref zRef, ref dRef, remainder);
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
            static void SoftwareFallback(ref T xRef, T y, ref T zRef, ref T dRef, nuint length)
            {
                for (nuint i = 0; i < length; i++)
                {
                    Unsafe.Add(ref dRef, i) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, i),
                                                                      y,
                                                                      Unsafe.Add(ref zRef, i));
                }
            }

            static void Vectorized128(ref T xRef, T y, ref T zRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector128<T> yVec = Vector128.Create(y);

                Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                               yVec,
                                                               Vector128.LoadUnsafe(ref zRef));
                Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                               yVec,
                                                               Vector128.LoadUnsafe(ref zRef, remainder - (uint)Vector128<T>.Count));

                if (remainder > (uint)(Vector128<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* pz = &zRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* zPtr = pz;
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
                            zPtr += misalignment;
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

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                zPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector128<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 0)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 1)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 2)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 3)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 4)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 5)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 6)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector128.Load(xPtr + (uint)(Vector128<T>.Count * 7)),
                                                                  yVec,
                                                                  Vector128.Load(zPtr + (uint)(Vector128<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector128<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector128<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector128<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector128<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector128<T>.Count * 8);
                                zPtr += (uint)(Vector128<T>.Count * 8);
                                dPtr += (uint)(Vector128<T>.Count * 8);

                                remainder -= (uint)(Vector128<T>.Count * 8);
                            }
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
                remainder = (remainder + (uint)(Vector128<T>.Count - 1)) & (nuint)(-Vector128<T>.Count);

                switch (remainder / (uint)Vector128<T>.Count)
                {
                    case 8:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 8)),
                                                                          yVec,
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 8)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 7)),
                                                                          yVec,
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 7)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 6)),
                                                                          yVec,
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 6)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 5)),
                                                                          yVec,
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 5)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 4)),
                                                                          yVec,
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 4)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 3)),
                                                                          yVec,
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 3)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector128<T>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector128<T> vector = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)(Vector128<T>.Count * 2)),
                                                                          yVec,
                                                                          Vector128.LoadUnsafe(ref zRef, remainder - (uint)(Vector128<T>.Count * 2)));
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

            static void Vectorized256(ref T xRef, T y, ref T zRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector256<T> yVec = Vector256.Create(y);

                Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                               yVec,
                                                               Vector256.LoadUnsafe(ref zRef));
                Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                               yVec,
                                                               Vector256.LoadUnsafe(ref zRef, remainder - (uint)Vector256<T>.Count));

                if (remainder > (uint)(Vector256<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* pz = &zRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* zPtr = pz;
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
                            zPtr += misalignment;
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

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                zPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector256<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 0)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 1)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 2)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 3)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 4)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 5)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 6)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector256.Load(xPtr + (uint)(Vector256<T>.Count * 7)),
                                                                  yVec,
                                                                  Vector256.Load(zPtr + (uint)(Vector256<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector256<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector256<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector256<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector256<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector256<T>.Count * 8);
                                zPtr += (uint)(Vector256<T>.Count * 8);
                                dPtr += (uint)(Vector256<T>.Count * 8);

                                remainder -= (uint)(Vector256<T>.Count * 8);
                            }
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
                remainder = (remainder + (uint)(Vector256<T>.Count - 1)) & (nuint)(-Vector256<T>.Count);

                switch (remainder / (uint)Vector256<T>.Count)
                {
                    case 8:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 8)),
                                                                          yVec,
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 8)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 7)),
                                                                          yVec,
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 7)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 6)),
                                                                          yVec,
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 6)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 5)),
                                                                          yVec,
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 5)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 4)),
                                                                          yVec,
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 4)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 3)),
                                                                          yVec,
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 3)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector256<T>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector256<T> vector = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)(Vector256<T>.Count * 2)),
                                                                          yVec,
                                                                          Vector256.LoadUnsafe(ref zRef, remainder - (uint)(Vector256<T>.Count * 2)));
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

            static void Vectorized512(ref T xRef, T y, ref T zRef, ref T dRef, nuint remainder)
            {
                ref T dRefBeg = ref dRef;

                // Preload the beginning and end so that overlapping accesses don't negatively impact the data

                Vector512<T> yVec = Vector512.Create(y);

                Vector512<T> beg = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef),
                                                               yVec,
                                                               Vector512.LoadUnsafe(ref zRef));
                Vector512<T> end = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)Vector512<T>.Count),
                                                               yVec,
                                                               Vector512.LoadUnsafe(ref zRef, remainder - (uint)Vector512<T>.Count));

                if (remainder > (uint)(Vector512<T>.Count * 8))
                {
                    // Pinning is cheap and will be short lived for small inputs and unlikely to be impactful
                    // for large inputs (> 85KB) which are on the LOH and unlikely to be compacted.

                    fixed (T* px = &xRef)
                    fixed (T* pz = &zRef)
                    fixed (T* pd = &dRef)
                    {
                        T* xPtr = px;
                        T* zPtr = pz;
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
                            zPtr += misalignment;
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

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 3)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 7)));

                                vector1.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.StoreAlignedNonTemporal(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                zPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
                        }
                        else
                        {
                            while (remainder >= (uint)(Vector512<T>.Count * 8))
                            {
                                // We load, process, and store the first four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 0)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 0)));
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 1)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 1)));
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 2)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 2)));
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 3)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 3)));

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 0));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 1));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 2));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 3));

                                // We load, process, and store the next four vectors

                                vector1 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 4)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 4)));
                                vector2 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 5)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 5)));
                                vector3 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 6)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 6)));
                                vector4 = TTernaryOperator.Invoke(Vector512.Load(xPtr + (uint)(Vector512<T>.Count * 7)),
                                                                  yVec,
                                                                  Vector512.Load(zPtr + (uint)(Vector512<T>.Count * 7)));

                                vector1.Store(dPtr + (uint)(Vector512<T>.Count * 4));
                                vector2.Store(dPtr + (uint)(Vector512<T>.Count * 5));
                                vector3.Store(dPtr + (uint)(Vector512<T>.Count * 6));
                                vector4.Store(dPtr + (uint)(Vector512<T>.Count * 7));

                                // We adjust the source and destination references, then update
                                // the count of remaining elements to process.

                                xPtr += (uint)(Vector512<T>.Count * 8);
                                zPtr += (uint)(Vector512<T>.Count * 8);
                                dPtr += (uint)(Vector512<T>.Count * 8);

                                remainder -= (uint)(Vector512<T>.Count * 8);
                            }
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
                remainder = (remainder + (uint)(Vector512<T>.Count - 1)) & (nuint)(-Vector512<T>.Count);

                switch (remainder / (uint)Vector512<T>.Count)
                {
                    case 8:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 8)),
                                                                          yVec,
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 8)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 8));
                            goto case 7;
                        }

                    case 7:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 7)),
                                                                          yVec,
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 7)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 7));
                            goto case 6;
                        }

                    case 6:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 6)),
                                                                          yVec,
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 6)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 6));
                            goto case 5;
                        }

                    case 5:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 5)),
                                                                          yVec,
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 5)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 5));
                            goto case 4;
                        }

                    case 4:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 4)),
                                                                          yVec,
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 4)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 4));
                            goto case 3;
                        }

                    case 3:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 3)),
                                                                          yVec,
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 3)));
                            vector.StoreUnsafe(ref dRef, remainder - (uint)(Vector512<T>.Count * 3));
                            goto case 2;
                        }

                    case 2:
                        {
                            Vector512<T> vector = TTernaryOperator.Invoke(Vector512.LoadUnsafe(ref xRef, remainder - (uint)(Vector512<T>.Count * 2)),
                                                                          yVec,
                                                                          Vector512.LoadUnsafe(ref zRef, remainder - (uint)(Vector512<T>.Count * 2)));
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
            static void VectorizedSmall(ref T xRef, T y, ref T zRef, ref T dRef, nuint remainder)
            {
                if (sizeof(T) == 1)
                {
                    VectorizedSmall1(ref xRef, y, ref zRef, ref dRef, remainder);
                }
                else if (sizeof(T) == 2)
                {
                    VectorizedSmall2(ref xRef, y, ref zRef, ref dRef, remainder);
                }
                else if (sizeof(T) == 4)
                {
                    VectorizedSmall4(ref xRef, y, ref zRef, ref dRef, remainder);
                }
                else
                {
                    Debug.Assert(sizeof(T) == 8);
                    VectorizedSmall8(ref xRef, y, ref zRef, ref dRef, remainder);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall1(ref T xRef, T y, ref T zRef, ref T dRef, nuint remainder)
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

                            Vector256<T> yVec = Vector256.Create(y);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       yVec,
                                                                       Vector256.LoadUnsafe(ref zRef));
                            Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                       yVec,
                                                                       Vector256.LoadUnsafe(ref zRef, remainder - (uint)Vector256<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    // One Vector256's worth of data.
                    case 32:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.Create(y),
                                                                       Vector256.LoadUnsafe(ref zRef));
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

                            Vector128<T> yVec = Vector128.Create(y);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       yVec,
                                                                       Vector128.LoadUnsafe(ref zRef));
                            Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                       yVec,
                                                                       Vector128.LoadUnsafe(ref zRef, remainder - (uint)Vector128<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    // One Vector128's worth of data.
                    case 16:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.Create(y),
                                                                       Vector128.LoadUnsafe(ref zRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    // Cases that are smaller than a single vector. No SIMD; just jump to the length and fall through each
                    // case to unroll the whole processing.
                    case 15:
                        Unsafe.Add(ref dRef, 14) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 14),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 14));
                        goto case 14;

                    case 14:
                        Unsafe.Add(ref dRef, 13) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 13),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 13));
                        goto case 13;

                    case 13:
                        Unsafe.Add(ref dRef, 12) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 12),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 12));
                        goto case 12;

                    case 12:
                        Unsafe.Add(ref dRef, 11) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 11),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 11));
                        goto case 11;

                    case 11:
                        Unsafe.Add(ref dRef, 10) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 10),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 10));
                        goto case 10;

                    case 10:
                        Unsafe.Add(ref dRef, 9) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 9),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 9));
                        goto case 9;

                    case 9:
                        Unsafe.Add(ref dRef, 8) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 8),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 8));
                        goto case 8;

                    case 8:
                        Unsafe.Add(ref dRef, 7) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 7),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 7));
                        goto case 7;

                    case 7:
                        Unsafe.Add(ref dRef, 6) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 6),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 6));
                        goto case 6;

                    case 6:
                        Unsafe.Add(ref dRef, 5) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 5),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 5));
                        goto case 5;

                    case 5:
                        Unsafe.Add(ref dRef, 4) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 4),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 4));
                        goto case 4;

                    case 4:
                        Unsafe.Add(ref dRef, 3) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 3),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 3));
                        goto case 3;

                    case 3:
                        Unsafe.Add(ref dRef, 2) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 2),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 2));
                        goto case 2;

                    case 2:
                        Unsafe.Add(ref dRef, 1) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 1),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 1));
                        goto case 1;

                    case 1:
                        dRef = TTernaryOperator.Invoke(xRef, y, zRef);
                        goto case 0;

                    case 0:
                        break;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall2(ref T xRef, T y, ref T zRef, ref T dRef, nuint remainder)
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

                            Vector256<T> yVec = Vector256.Create(y);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       yVec,
                                                                       Vector256.LoadUnsafe(ref zRef));
                            Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                       yVec,
                                                                       Vector256.LoadUnsafe(ref zRef, remainder - (uint)Vector256<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    // One Vector256's worth of data.
                    case 16:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.Create(y),
                                                                       Vector256.LoadUnsafe(ref zRef));
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

                            Vector128<T> yVec = Vector128.Create(y);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       yVec,
                                                                       Vector128.LoadUnsafe(ref zRef));
                            Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                       yVec,
                                                                       Vector128.LoadUnsafe(ref zRef, remainder - (uint)Vector128<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    // One Vector128's worth of data.
                    case 8:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.Create(y),
                                                                       Vector128.LoadUnsafe(ref zRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    // Cases that are smaller than a single vector. No SIMD; just jump to the length and fall through each
                    // case to unroll the whole processing.
                    case 7:
                        Unsafe.Add(ref dRef, 6) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 6),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 6));
                        goto case 6;

                    case 6:
                        Unsafe.Add(ref dRef, 5) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 5),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 5));
                        goto case 5;

                    case 5:
                        Unsafe.Add(ref dRef, 4) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 4),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 4));
                        goto case 4;

                    case 4:
                        Unsafe.Add(ref dRef, 3) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 3),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 3));
                        goto case 3;

                    case 3:
                        Unsafe.Add(ref dRef, 2) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 2),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 2));
                        goto case 2;

                    case 2:
                        Unsafe.Add(ref dRef, 1) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 1),
                                                                          y,
                                                                          Unsafe.Add(ref zRef, 1));
                        goto case 1;

                    case 1:
                        dRef = TTernaryOperator.Invoke(xRef, y, zRef);
                        goto case 0;

                    case 0:
                        break;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall4(ref T xRef, T y, ref T zRef, ref T dRef, nuint remainder)
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

                            Vector256<T> yVec = Vector256.Create(y);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       yVec,
                                                                       Vector256.LoadUnsafe(ref zRef));
                            Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                       yVec,
                                                                       Vector256.LoadUnsafe(ref zRef, remainder - (uint)Vector256<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    case 8:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.Create(y),
                                                                       Vector256.LoadUnsafe(ref zRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 7:
                    case 6:
                    case 5:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> yVec = Vector128.Create(y);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       yVec,
                                                                       Vector128.LoadUnsafe(ref zRef));
                            Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                       yVec,
                                                                       Vector128.LoadUnsafe(ref zRef, remainder - (uint)Vector128<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    case 4:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.Create(y),
                                                                       Vector128.LoadUnsafe(ref zRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 3:
                        {
                            Unsafe.Add(ref dRef, 2) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 2),
                                                                              y,
                                                                              Unsafe.Add(ref zRef, 2));
                            goto case 2;
                        }

                    case 2:
                        {
                            Unsafe.Add(ref dRef, 1) = TTernaryOperator.Invoke(Unsafe.Add(ref xRef, 1),
                                                                              y,
                                                                              Unsafe.Add(ref zRef, 1));
                            goto case 1;
                        }

                    case 1:
                        {
                            dRef = TTernaryOperator.Invoke(xRef, y, zRef);
                            goto case 0;
                        }

                    case 0:
                        {
                            break;
                        }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void VectorizedSmall8(ref T xRef, T y, ref T zRef, ref T dRef, nuint remainder)
            {
                Debug.Assert(sizeof(T) == 8);

                switch (remainder)
                {
                    case 7:
                    case 6:
                    case 5:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> yVec = Vector256.Create(y);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       yVec,
                                                                       Vector256.LoadUnsafe(ref zRef));
                            Vector256<T> end = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef, remainder - (uint)Vector256<T>.Count),
                                                                       yVec,
                                                                       Vector256.LoadUnsafe(ref zRef, remainder - (uint)Vector256<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector256<T>.Count);

                            break;
                        }

                    case 4:
                        {
                            Debug.Assert(Vector256.IsHardwareAccelerated);

                            Vector256<T> beg = TTernaryOperator.Invoke(Vector256.LoadUnsafe(ref xRef),
                                                                       Vector256.Create(y),
                                                                       Vector256.LoadUnsafe(ref zRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 3:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> yVec = Vector128.Create(y);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       yVec,
                                                                       Vector128.LoadUnsafe(ref zRef));
                            Vector128<T> end = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef, remainder - (uint)Vector128<T>.Count),
                                                                       yVec,
                                                                       Vector128.LoadUnsafe(ref zRef, remainder - (uint)Vector128<T>.Count));

                            beg.StoreUnsafe(ref dRef);
                            end.StoreUnsafe(ref dRef, remainder - (uint)Vector128<T>.Count);

                            break;
                        }

                    case 2:
                        {
                            Debug.Assert(Vector128.IsHardwareAccelerated);

                            Vector128<T> beg = TTernaryOperator.Invoke(Vector128.LoadUnsafe(ref xRef),
                                                                       Vector128.Create(y),
                                                                       Vector128.LoadUnsafe(ref zRef));
                            beg.StoreUnsafe(ref dRef);

                            break;
                        }

                    case 1:
                        {
                            dRef = TTernaryOperator.Invoke(xRef, y, zRef);
                            goto case 0;
                        }

                    case 0:
                        {
                            break;
                        }
                }
            }
        }

        /// <summary>Aggregates all of the elements in the <paramref name="x"/> into a single value.</summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TAggregate">Specifies the operation to be performed on each pair of values.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T HorizontalAggregate<T, TAggregate>(Vector128<T> x) where TAggregate : struct, IBinaryOperator<T>
        {
            // We need to do log2(count) operations to compute the total sum

            if (Unsafe.SizeOf<T>() == 1)
            {
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsByte(), Vector128.Create((byte)8, 9, 10, 11, 12, 13, 14, 15, 0, 1, 2, 3, 4, 5, 6, 7)).As<byte, T>());
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsByte(), Vector128.Create((byte)4, 5, 6, 7, 0, 1, 2, 3, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>());
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsByte(), Vector128.Create((byte)2, 3, 0, 1, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>());
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsByte(), Vector128.Create((byte)1, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>());
            }
            else if (Unsafe.SizeOf<T>() == 2)
            {
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsInt16(), Vector128.Create(4, 5, 6, 7, 0, 1, 2, 3)).As<short, T>());
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsInt16(), Vector128.Create(2, 3, 0, 1, 4, 5, 6, 7)).As<short, T>());
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsInt16(), Vector128.Create(1, 0, 2, 3, 4, 5, 6, 7)).As<short, T>());
            }
            else if (Unsafe.SizeOf<T>() == 4)
            {
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsInt32(), Vector128.Create(2, 3, 0, 1)).As<int, T>());
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsInt32(), Vector128.Create(1, 0, 3, 2)).As<int, T>());
            }
            else
            {
                Debug.Assert(Unsafe.SizeOf<T>() == 8);
                x = TAggregate.Invoke(x, Vector128.Shuffle(x.AsInt64(), Vector128.Create(1, 0)).As<long, T>());
            }

            return x.ToScalar();
        }

        /// <summary>Aggregates all of the elements in the <paramref name="x"/> into a single value.</summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TAggregate">Specifies the operation to be performed on each pair of values.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T HorizontalAggregate<T, TAggregate>(Vector256<T> x) where TAggregate : struct, IBinaryOperator<T> =>
            HorizontalAggregate<T, TAggregate>(TAggregate.Invoke(x.GetLower(), x.GetUpper()));

        /// <summary>Aggregates all of the elements in the <paramref name="x"/> into a single value.</summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <typeparam name="TAggregate">Specifies the operation to be performed on each pair of values.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T HorizontalAggregate<T, TAggregate>(Vector512<T> x) where TAggregate : struct, IBinaryOperator<T> =>
            HorizontalAggregate<T, TAggregate>(TAggregate.Invoke(x.GetLower(), x.GetUpper()));


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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int IndexOfFinalAggregate<T, TIndexOfOperator>(Vector128<T> result, Vector128<T> resultIndex)
            where TIndexOfOperator : struct, IIndexOfOperator<T>
        {
            Vector128<T> tmpResult;
            Vector128<T> tmpIndex;

            if (sizeof(T) == 8)
            {
                // Compare 0 with 1
                tmpResult = Vector128.Shuffle(result.AsInt64(), Vector128.Create(1, 0)).As<long, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsInt64(), Vector128.Create(1, 0)).As<long, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Return 0
                return (int)resultIndex.As<T, long>().ToScalar();
            }

            if (sizeof(T) == 4)
            {
                // Compare 0,1 with 2,3
                tmpResult = Vector128.Shuffle(result.AsInt32(), Vector128.Create(2, 3, 0, 1)).As<int, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsInt32(), Vector128.Create(2, 3, 0, 1)).As<int, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Compare 0 with 1
                tmpResult = Vector128.Shuffle(result.AsInt32(), Vector128.Create(1, 0, 3, 2)).As<int, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsInt32(), Vector128.Create(1, 0, 3, 2)).As<int, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Return 0
                return resultIndex.As<T, int>().ToScalar();
            }

            if (sizeof(T) == 2)
            {
                // Compare 0,1,2,3 with 4,5,6,7
                tmpResult = Vector128.Shuffle(result.AsInt16(), Vector128.Create(4, 5, 6, 7, 0, 1, 2, 3)).As<short, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsInt16(), Vector128.Create(4, 5, 6, 7, 0, 1, 2, 3)).As<short, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Compare 0,1 with 2,3
                tmpResult = Vector128.Shuffle(result.AsInt16(), Vector128.Create(2, 3, 0, 1, 4, 5, 6, 7)).As<short, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsInt16(), Vector128.Create(2, 3, 0, 1, 4, 5, 6, 7)).As<short, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Compare 0 with 1
                tmpResult = Vector128.Shuffle(result.AsInt16(), Vector128.Create(1, 0, 2, 3, 4, 5, 6, 7)).As<short, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsInt16(), Vector128.Create(1, 0, 2, 3, 4, 5, 6, 7)).As<short, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Return 0
                return resultIndex.As<T, short>().ToScalar();
            }

            Debug.Assert(sizeof(T) == 1);
            {
                // Compare 0,1,2,3,4,5,6,7 with 8,9,10,11,12,13,14,15
                tmpResult = Vector128.Shuffle(result.AsByte(), Vector128.Create((byte)8, 9, 10, 11, 12, 13, 14, 15, 0, 1, 2, 3, 4, 5, 6, 7)).As<byte, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsByte(), Vector128.Create((byte)8, 9, 10, 11, 12, 13, 14, 15, 0, 1, 2, 3, 4, 5, 6, 7)).As<byte, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Compare 0,1,2,3 with 4,5,6,7
                tmpResult = Vector128.Shuffle(result.AsByte(), Vector128.Create((byte)4, 5, 6, 7, 0, 1, 2, 3, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsByte(), Vector128.Create((byte)4, 5, 6, 7, 0, 1, 2, 3, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Compare 0,1 with 2,3
                tmpResult = Vector128.Shuffle(result.AsByte(), Vector128.Create((byte)2, 3, 0, 1, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsByte(), Vector128.Create((byte)2, 3, 0, 1, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Compare 0 with 1
                tmpResult = Vector128.Shuffle(result.AsByte(), Vector128.Create((byte)1, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>();
                tmpIndex = Vector128.Shuffle(resultIndex.AsByte(), Vector128.Create((byte)1, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)).As<byte, T>();
                TIndexOfOperator.Invoke(ref result, tmpResult, ref resultIndex, tmpIndex);

                // Return 0
                return resultIndex.As<T, byte>().ToScalar();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int IndexOfFinalAggregate<T, TIndexOfOperator>(Vector256<T> result, Vector256<T> resultIndex)
            where TIndexOfOperator : struct, IIndexOfOperator<T>
        {
            // Min the upper/lower halves of the Vector256
            Vector128<T> resultLower = result.GetLower();
            Vector128<T> indexLower = resultIndex.GetLower();

            TIndexOfOperator.Invoke(ref resultLower, result.GetUpper(), ref indexLower, resultIndex.GetUpper());
            return IndexOfFinalAggregate<T, TIndexOfOperator>(resultLower, indexLower);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int IndexOfFinalAggregate<T, TIndexOfOperator>(Vector512<T> result, Vector512<T> resultIndex)
            where TIndexOfOperator : struct, IIndexOfOperator<T>
        {
            Vector256<T> resultLower = result.GetLower();
            Vector256<T> indexLower = resultIndex.GetLower();

            TIndexOfOperator.Invoke(ref resultLower, result.GetUpper(), ref indexLower, resultIndex.GetUpper());
            return IndexOfFinalAggregate<T, TIndexOfOperator>(resultLower, indexLower);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<T> IndexLessThan<T>(Vector128<T> indices1, Vector128<T> indices2) =>
            sizeof(T) == sizeof(long) ? Vector128.LessThan(indices1.AsInt64(), indices2.AsInt64()).As<long, T>() :
            sizeof(T) == sizeof(int) ? Vector128.LessThan(indices1.AsInt32(), indices2.AsInt32()).As<int, T>() :
            sizeof(T) == sizeof(short) ? Vector128.LessThan(indices1.AsInt16(), indices2.AsInt16()).As<short, T>() :
            Vector128.LessThan(indices1.AsByte(), indices2.AsByte()).As<byte, T>();

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

        /// <summary>Creates a span of <typeparamref name="TTo"/> from a <typeparamref name="TTo"/> when they're the same type.</summary>
        private static unsafe Span<TTo> Rename<TFrom, TTo>(Span<TFrom> span)
        {
            Debug.Assert(sizeof(TFrom) == sizeof(TTo));
            return MemoryMarshal.CreateSpan(ref Unsafe.As<TFrom, TTo>(ref MemoryMarshal.GetReference(span)), span.Length);
        }

        private readonly struct InvertedBinaryOperator<TOperator, T> : IBinaryOperator<T>
            where TOperator : IBinaryOperator<T>
        {
            public static bool Vectorizable => TOperator.Vectorizable;
            public static T Invoke(T x, T y) => TOperator.Invoke(y, x);
            public static Vector128<T> Invoke(Vector128<T> x, Vector128<T> y) => TOperator.Invoke(y, x);
            public static Vector256<T> Invoke(Vector256<T> x, Vector256<T> y) => TOperator.Invoke(y, x);
            public static Vector512<T> Invoke(Vector512<T> x, Vector512<T> y) => TOperator.Invoke(y, x);
        }
    }
}
