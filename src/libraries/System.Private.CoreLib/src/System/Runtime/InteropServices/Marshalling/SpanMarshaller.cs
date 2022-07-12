// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Runtime.InteropServices.Marshalling
{
    [CLSCompliant(false)]
    [CustomMarshaller(typeof(Span<>), MarshalMode.Default, typeof(SpanMarshaller<,>))]
    [CustomMarshaller(typeof(Span<>), MarshalMode.ManagedToUnmanagedIn, typeof(SpanMarshaller<,>.ManagedToUnmanagedIn))]
    [ContiguousCollectionMarshaller]
    public static unsafe class SpanMarshaller<T, TUnmanagedElement>
        where TUnmanagedElement : unmanaged
    {
        public static TUnmanagedElement* AllocateContainerForUnmanagedElements(Span<T> managed, out int numElements)
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

        public static ReadOnlySpan<T> GetManagedValuesSource(Span<T> managed)
            => managed;

        public static Span<TUnmanagedElement> GetUnmanagedValuesDestination(TUnmanagedElement* unmanaged, int numElements)
            => new Span<TUnmanagedElement>(unmanaged, numElements);

        public static Span<T> AllocateContainerForManagedElements(TUnmanagedElement* unmanaged, int length)
        {
            if (unmanaged is null)
                return null;

            return new T[length];
        }

        public static Span<T> GetManagedValuesDestination(Span<T> managed)
            => managed;

        public static ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(TUnmanagedElement* unmanagedValue, int numElements)
            => new ReadOnlySpan<TUnmanagedElement>(unmanagedValue, numElements);

        public static void Free(TUnmanagedElement* unmanaged)
            => Marshal.FreeCoTaskMem((IntPtr)unmanaged);

        public ref struct ManagedToUnmanagedIn
        {
            // We'll keep the buffer size at a maximum of 200 bytes to avoid overflowing the stack.
            public static int BufferSize { get; } = 0x200 / sizeof(TUnmanagedElement);

            private Span<T> _managedArray;
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
            public void FromManaged(Span<T> managed, Span<TUnmanagedElement> buffer)
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
                    int spaceToAllocate = Math.Max(bufferSize, 1);
                    _allocatedMemory = (TUnmanagedElement*)NativeMemory.Alloc((nuint)spaceToAllocate);
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

            public static ref T GetPinnableReference(Span<T> managed)
            {
                return ref MemoryMarshal.GetReference(managed);
            }
        }
    }
}
