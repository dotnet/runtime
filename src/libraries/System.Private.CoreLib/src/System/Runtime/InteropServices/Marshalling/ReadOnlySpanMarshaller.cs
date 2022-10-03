// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// The <see cref="ReadOnlySpanMarshaller{T, TUnmanagedElement}"/> type supports marshalling a <see cref="ReadOnlySpan{T}"/> from managed value
    /// to a contiguous native array of the unmanaged values of the elements.
    /// </summary>
    /// <typeparam name="T">The managed element type of the span.</typeparam>
    /// <typeparam name="TUnmanagedElement">The unmanaged type for the elements of the span.</typeparam>
    /// <remarks>
    /// A <see cref="ReadOnlySpan{T}"/> marshalled with this marshaller will match the semantics of <see cref="MemoryMarshal.GetReference{T}(ReadOnlySpan{T})"/>.
    /// In particular, this marshaller will pass a non-null value for a zero-length span if the span was constructed with a non-null value.
    /// </remarks>
    [CLSCompliant(false)]
    [CustomMarshaller(typeof(ReadOnlySpan<>), MarshalMode.ManagedToUnmanagedIn, typeof(ReadOnlySpanMarshaller<,>.ManagedToUnmanagedIn))]
    [CustomMarshaller(typeof(ReadOnlySpan<>), MarshalMode.UnmanagedToManagedOut, typeof(ReadOnlySpanMarshaller<,>.UnmanagedToManagedOut))]
    [ContiguousCollectionMarshaller]
    public static unsafe class ReadOnlySpanMarshaller<T, TUnmanagedElement>
        where TUnmanagedElement : unmanaged
    {
        /// <summary>
        /// The marshaller type that supports marshalling from managed into unmanaged in a call from unmanaged code to managed code.
        /// </summary>
        public static unsafe class UnmanagedToManagedOut
        {
            /// <summary>
            /// Allocate the space to store the unmanaged elements.
            /// </summary>
            /// <param name="managed">The managed span.</param>
            /// <param name="numElements">The number of elements in the span.</param>
            /// <returns>A pointer to the block of memory for the unmanaged elements.</returns>
            public static TUnmanagedElement* AllocateContainerForUnmanagedElements(ReadOnlySpan<T> managed, out int numElements)
            {
                // Emulate the pinning behavior:
                // If the span is over a null reference, then pass a null pointer.
                if (Unsafe.IsNullRef(ref MemoryMarshal.GetReference(managed)))
                {
                    numElements = 0;
                    return null;
                }

                numElements = managed.Length;

                // Always allocate at least one byte when the array is zero-length.
                int spaceToAllocate = Math.Max(checked(sizeof(TUnmanagedElement) * numElements), 1);
                return (TUnmanagedElement*)Marshal.AllocCoTaskMem(spaceToAllocate);
            }

            /// <summary>
            /// Get a span of the managed collection elements.
            /// </summary>
            /// <param name="managed">The managed collection.</param>
            /// <returns>A span of the managed collection elements.</returns>
            public static ReadOnlySpan<T> GetManagedValuesSource(ReadOnlySpan<T> managed)
                => managed;

            /// <summary>
            /// Get a span of the space where the unmanaged collection elements should be stored.
            /// </summary>
            /// <param name="unmanaged">The pointer to the block of memory for the unmanaged elements.</param>
            /// <param name="numElements">The number of elements that will be copied into the memory block.</param>
            /// <returns>A span over the unmanaged memory that can contain the specified number of elements.</returns>
            public static Span<TUnmanagedElement> GetUnmanagedValuesDestination(TUnmanagedElement* unmanaged, int numElements)
                => new Span<TUnmanagedElement>(unmanaged, numElements);
        }

        /// <summary>
        /// The marshaller type that supports marshalling from managed into unmanaged in a call from managed code to unmanaged code.
        /// </summary>
        public ref struct ManagedToUnmanagedIn
        {
            /// <summary>
            /// The size of the caller-allocated buffer to allocate.
            /// </summary>
            // We'll keep the buffer size at a maximum of 200 bytes to avoid overflowing the stack.
            public static int BufferSize { get; } = 0x200 / sizeof(TUnmanagedElement);

            private ReadOnlySpan<T> _managedArray;
            private TUnmanagedElement* _allocatedMemory;
            private Span<TUnmanagedElement> _span;

            /// <summary>
            /// Initializes the <see cref="SpanMarshaller{T, TUnmanagedElement}.ManagedToUnmanagedIn"/> marshaller.
            /// </summary>
            /// <param name="managed">Span to be marshalled.</param>
            /// <param name="buffer">Buffer that may be used for marshalling.</param>
            /// <remarks>
            /// The <paramref name="buffer"/> must not be movable - that is, it should not be
            /// on the managed heap or it should be pinned.
            /// </remarks>
            public void FromManaged(ReadOnlySpan<T> managed, Span<TUnmanagedElement> buffer)
            {
                _allocatedMemory = null;
                // Emulate the pinning behavior:
                // If the span is over a null reference, then pass a null pointer.
                if (Unsafe.IsNullRef(ref MemoryMarshal.GetReference(managed)))
                {
                    _managedArray = null;
                    _span = default;
                    return;
                }

                _managedArray = managed;

                // Always allocate at least one byte when the span is zero-length.
                if (managed.Length <= buffer.Length)
                {
                    _span = buffer[0..managed.Length];
                }
                else
                {
                    int bufferSize = checked(managed.Length * sizeof(TUnmanagedElement));
                    _allocatedMemory = (TUnmanagedElement*)NativeMemory.Alloc((nuint)bufferSize);
                    _span = new Span<TUnmanagedElement>(_allocatedMemory, managed.Length);
                }
            }

            /// <summary>
            /// Gets a span that points to the memory where the managed values of the array are stored.
            /// </summary>
            /// <returns>Span over managed values of the array.</returns>
            public ReadOnlySpan<T> GetManagedValuesSource() => _managedArray;

            /// <summary>
            /// Returns a span that points to the memory where the unmanaged values of the array should be stored.
            /// </summary>
            /// <returns>Span where unmanaged values of the array should be stored.</returns>
            public Span<TUnmanagedElement> GetUnmanagedValuesDestination() => _span;

            /// <summary>
            /// Returns a reference to the marshalled array.
            /// </summary>
            public ref TUnmanagedElement GetPinnableReference() => ref MemoryMarshal.GetReference(_span);

            /// <summary>
            /// Returns the unmanaged value representing the array.
            /// </summary>
            public TUnmanagedElement* ToUnmanaged() => (TUnmanagedElement*)Unsafe.AsPointer(ref GetPinnableReference());

            /// <summary>
            /// Frees resources.
            /// </summary>
            public void Free()
            {
                NativeMemory.Free(_allocatedMemory);
            }

            /// <summary>
            /// Pin the managed span to a pointer to pass directly to unmanaged code.
            /// </summary>
            /// <param name="managed">The managed span.</param>
            /// <returns>A reference that can be pinned and directly passed to unmanaged code.</returns>
            public static ref T GetPinnableReference(ReadOnlySpan<T> managed)
            {
                return ref MemoryMarshal.GetReference(managed);
            }
        }
    }
}
