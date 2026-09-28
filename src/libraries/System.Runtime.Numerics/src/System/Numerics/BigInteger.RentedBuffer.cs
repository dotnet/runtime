// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Numerics
{
    public readonly partial struct BigInteger
    {
        /// <summary>
        /// Provides temporary buffer management for <see cref="BigInteger"/> operations, using either stack-allocated inline storage or pooled arrays depending on size requirements.
        /// </summary>
        internal ref struct RentedBuffer : IDisposable
        {
            private const int InlineBufferSize = 64;
            private InlineBuffer _inline;
            private nuint[]? _array;

            /// <summary>
            /// Creates a buffer of the specified size and returns a span to it.
            /// </summary>
            /// <param name="size">The number of <see cref="nuint"/> elements required in the buffer.</param>
            /// <param name="rentedBuffer">When this method returns, contains the <see cref="RentedBuffer"/> instance that manages the buffer lifetime. This parameter is treated as uninitialized.</param>
            /// <returns>A span of <see cref="nuint"/> elements with the requested size, initialized to zero.</returns>
            public static Span<nuint> Create(int size, [UnscopedRef] out RentedBuffer rentedBuffer)
            {
                if (size <= InlineBufferSize)
                {
                    rentedBuffer = default;
                    return ((Span<nuint>)rentedBuffer._inline)[..size];
                }
                else
                {
                    nuint[] array = ArrayPool<nuint>.Shared.Rent(size);

                    rentedBuffer._array = array;
                    Unsafe.SkipInit(out rentedBuffer._inline);

                    Span<nuint> resultBuffer = array.AsSpan(0, size);
                    resultBuffer.Clear();

                    return resultBuffer;
                }
            }

            /// <summary>
            /// Returns the rented array to the pool, if one was allocated.
            /// </summary>
            /// <remarks>
            /// This method returns the underlying array to <see cref="ArrayPool{T}.Shared"/> if it was rented.
            /// If inline storage was used, this method does nothing.
            /// </remarks>
            public void Dispose()
            {
                nuint[]? array = _array;
                if (array is not null)
                {
                    _array = null;
                    ArrayPool<nuint>.Shared.Return(array);
                }
            }

            [InlineArray(InlineBufferSize)]
            private struct InlineBuffer
            {
                private nuint _value0;
            }
        }
    }
}
