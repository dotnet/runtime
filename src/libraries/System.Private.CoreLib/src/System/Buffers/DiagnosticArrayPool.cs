// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers
{
    /// <summary>
    /// Provides an <see cref="ArrayPool{T}"/> implementation used to find possible misuse in
    /// how <see cref="ArrayPool{T}.Shared"/> is consumed.
    /// </summary>
    /// <remarks>
    /// This implementation is very inefficient and should be used sparingly. It doesn't actually
    /// do any pooling, and on array return it both grabs a stack trace and allocates a finalizable object,
    /// which when finalized will do a full search of the array.
    /// </remarks>
    internal sealed class DiagnosticArrayPool<T> : ArrayPool<T>
    {
        /// <summary>Weak references to all returned arrays.</summary>
        /// <remarks>
        /// Returned arrays are never given out again. Instead, upon return they're added to this
        /// table, along with a finalizable object that holds the array and the stack trace from
        /// when the array was returned. If the array is ever returned again, we can detect that
        /// by the array already being in the table. And when no one is referencing the array and
        /// it becomes collectible, the finalizable object's finalizer can try to validate that the
        /// array wasn't written to post-return.
        /// </remarks>
        private static readonly ConditionalWeakTable<T[], ReturnedArrayState> s_returnedArrays = [];

        /// <inheritdoc />
        public override T[] Rent(int minimumLength)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(minimumLength, 0);

            // Allocate an array that has a size that's a power-of-2 and at least 16, to match the behavior of ArrayPool.Shared.
            minimumLength = Math.Max(minimumLength, 16);
            minimumLength = (int)Math.Min(int.MaxValue, BitOperations.RoundUpToPowerOf2((uint)minimumLength));
            var array = new T[minimumLength];

            // For unmanaged Ts, fill the array with garbage to help catch bugs in consumers that might
            // be assuming zero initialization.
            if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                IterateByteSpans(array, static (bytes, _) => Random.Shared.NextBytes(bytes), 0);
            }

            return array;
        }

        /// <inheritdoc />
        public override void Return(T[] array, bool clearArray = false)
        {
            ArgumentNullException.ThrowIfNull(array);

            // Clear it regardless of clearArray so that we can easily check whether any non-zero writes
            // were performed post-return. This could also help to catch bugs in consumers that might be
            // returning arrays and then still using them for their contents.
            Array.Clear(array);

            // Locking isn't necessary for safety of data structures, but rather to serialize access
            // to help catch concurrent misuse.
            lock (s_returnedArrays)
            {
                if (s_returnedArrays.TryGetValue(array, out ReturnedArrayState? state))
                {
                    // Array was previously returned.
                    Environment.FailFast(
                        $"The array being returned to the ArrayPool was double-returned.{Environment.NewLine}" +
                        $"Current stack: {Environment.StackTrace}{Environment.NewLine}" +
                        $"Previous stack: {state.Stack}");
                }

                // Put the array into the CWT so that we can:
                // 1) Detect if it's returned again.
                // 2) Detect if it's modified after being returned.
                s_returnedArrays.Add(array, new(array));
            }
        }

        /// <summary>Iterates through an array, reinterpreted as bytes, invoking the action for each chunk of up to int.MaxValue bytes.</summary>
        private static unsafe void IterateByteSpans<TState>(T[] array, Action<Span<byte>, TState> action, TState state)
        {
            fixed (byte* arrayPtr = &MemoryMarshal.GetArrayDataReference((Array)array))
            {
                byte* ptr = arrayPtr;
                long length = array.Length * (long)Unsafe.SizeOf<T>();

                while (length > 0)
                {
                    var span = new Span<byte>(ptr, (int)Math.Min(int.MaxValue, length));
                    action(span, state);

                    length -= span.Length;
                    ptr += span.Length;
                }
            }
        }

        /// <summary>Holds state used as part of validating no use-after-free.</summary>
        private sealed class ReturnedArrayState(T[] array)
        {
            private readonly T[] _array = array;

            public string Stack { get; } = Environment.StackTrace;

            ~ReturnedArrayState()
            {
                // Try to detect if the array was modified after being returned, and fail if it was.
                // The array was cleared upon return, so we search for any non-zero bytes.
                IterateByteSpans(_array, static (bytes, state) =>
                {
                    if (bytes.ContainsAnyExcept((byte)0))
                    {
                        Environment.FailFast($"An array returned to the ArrayPool was modified after it was returned. Returning stack: {state.Stack}");
                    }
                }, this);
            }
        }
    }
}
