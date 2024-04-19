// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Represents a marshaller for arrays.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    /// <typeparam name="TUnmanagedElement">The unmanaged type for the element type.</typeparam>
    [CLSCompliant(false)]
    [CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder[]),
        MarshalMode.Default,
        typeof(ArrayMarshaller<,>))]
    [CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder[]),
        MarshalMode.ManagedToUnmanagedIn,
        typeof(ArrayMarshaller<,>.ManagedToUnmanagedIn))]
    [ContiguousCollectionMarshaller]
    public static unsafe class ArrayMarshaller<T, TUnmanagedElement>
        where TUnmanagedElement : unmanaged
    {
        /// <summary>
        /// Allocates memory for the unmanaged representation of the array.
        /// </summary>
        /// <param name="managed">The managed array.</param>
        /// <param name="numElements">The unmanaged element count.</param>
        /// <returns>The unmanaged pointer to the allocated memory.</returns>
        public static TUnmanagedElement* AllocateContainerForUnmanagedElements(T[]? managed, out int numElements)
        {
            if (managed is null)
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
        /// Gets a source for the managed elements in the array.
        /// </summary>
        /// <param name="managed">The managed array.</param>
        /// <returns>The <see cref="ReadOnlySpan{T}"/> containing the managed elements to marshal.</returns>
        public static ReadOnlySpan<T> GetManagedValuesSource(T[]? managed)
            => managed;

        /// <summary>
        /// Gets a destination for the unmanaged elements in the array.
        /// </summary>
        /// <param name="unmanaged">The unmanaged allocation.</param>
        /// <param name="numElements">The unmanaged element count.</param>
        /// <returns>The <see cref="Span{TUnmanagedElement}"/> of unmanaged elements.</returns>
        public static Span<TUnmanagedElement> GetUnmanagedValuesDestination(TUnmanagedElement* unmanaged, int numElements)
            => new Span<TUnmanagedElement>(unmanaged, numElements);

        /// <summary>
        /// Allocates memory for the managed representation of the array.
        /// </summary>
        /// <param name="unmanaged">The unmanaged array.</param>
        /// <param name="numElements">The unmanaged element count.</param>
        /// <returns>The managed array.</returns>
        public static T[]? AllocateContainerForManagedElements(TUnmanagedElement* unmanaged, int numElements)
        {
            if (unmanaged is null)
                return null;

            return new T[numElements];
        }

        /// <summary>
        /// Gets a destination for the managed elements in the array.
        /// </summary>
        /// <param name="managed">The managed array.</param>
        /// <returns>The <see cref="Span{T}"/> of managed elements.</returns>
        public static Span<T> GetManagedValuesDestination(T[]? managed)
            => managed;

        /// <summary>
        /// Gets a source for the unmanaged elements in the array.
        /// </summary>
        /// <param name="unmanagedValue">The unmanaged array.</param>
        /// <param name="numElements">The unmanaged element count.</param>
        /// <returns>The <see cref="ReadOnlySpan{TUnmanagedElement}"/> containing the unmanaged elements to marshal.</returns>
        public static ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(TUnmanagedElement* unmanagedValue, int numElements)
            => new ReadOnlySpan<TUnmanagedElement>(unmanagedValue, numElements);

        /// <summary>
        /// Frees memory for the unmanaged array.
        /// </summary>
        /// <param name="unmanaged">The unmanaged array.</param>
        public static void Free(TUnmanagedElement* unmanaged)
            => Marshal.FreeCoTaskMem((IntPtr)unmanaged);

        /// <summary>
        /// Marshaller for marshalling a array from managed to unmanaged.
        /// </summary>
        public ref struct ManagedToUnmanagedIn
        {
            /// <summary>
            /// Gets the requested caller-allocated buffer size.
            /// </summary>
            /// <remarks>
            /// This property represents a potential optimization for the marshaller.
            /// </remarks>
            // We'll keep the buffer size at a maximum of 512 bytes to avoid overflowing the stack.
            public static int BufferSize { get; } = 0x200 / sizeof(TUnmanagedElement);

            private T[]? _managedArray;
            private TUnmanagedElement* _allocatedMemory;
            private Span<TUnmanagedElement> _span;

            /// <summary>
            /// Initializes the <see cref="ArrayMarshaller{T, TUnmanagedElement}.ManagedToUnmanagedIn"/> marshaller.
            /// </summary>
            /// <param name="array">The array to be marshalled.</param>
            /// <param name="buffer">The buffer that may be used for marshalling.</param>
            /// <remarks>
            /// The <paramref name="buffer"/> must not be movable - that is, it should not be
            /// on the managed heap or it should be pinned.
            /// </remarks>
            public void FromManaged(T[]? array, Span<TUnmanagedElement> buffer)
            {
                _allocatedMemory = null;
                if (array is null)
                {
                    _managedArray = null;
                    _span = default;
                    return;
                }

                _managedArray = array;

                // Always allocate at least one byte when the array is zero-length.
                if (array.Length <= buffer.Length)
                {
                    _span = buffer[0..array.Length];
                }
                else
                {
                    int bufferSize = checked(array.Length * sizeof(TUnmanagedElement));
                    int spaceToAllocate = Math.Max(bufferSize, 1);
                    _allocatedMemory = (TUnmanagedElement*)NativeMemory.Alloc((nuint)spaceToAllocate);
                    _span = new Span<TUnmanagedElement>(_allocatedMemory, array.Length);
                }
            }

            /// <summary>
            /// Returns a span that points to the memory where the managed values of the array are stored.
            /// </summary>
            /// <returns>A span over managed values of the array.</returns>
            public ReadOnlySpan<T> GetManagedValuesSource() => _managedArray;

            /// <summary>
            /// Returns a span that points to the memory where the unmanaged values of the array should be stored.
            /// </summary>
            /// <returns>A span where unmanaged values of the array should be stored.</returns>
            public Span<TUnmanagedElement> GetUnmanagedValuesDestination() => _span;

            /// <summary>
            /// Returns a reference to the marshalled array.
            /// </summary>
            /// <returns>A pinnable reference to the unmanaged marshalled array.</returns>
            public ref TUnmanagedElement GetPinnableReference() => ref MemoryMarshal.GetReference(_span);

            /// <summary>
            /// Returns the unmanaged value representing the array.
            /// </summary>
            /// <returns>A pointer to the beginning of the unmanaged value.</returns>
            public TUnmanagedElement* ToUnmanaged()
            {
                // Unsafe.AsPointer is safe since buffer must be pinned
                return (TUnmanagedElement*)Unsafe.AsPointer(ref GetPinnableReference());
            }

            /// <summary>
            /// Frees resources.
            /// </summary>
            public void Free()
            {
                NativeMemory.Free(_allocatedMemory);
            }

            /// <summary>
            /// Gets a pinnable reference to the managed array.
            /// </summary>
            /// <param name="array">The managed array.</param>
            /// <returns>The reference that can be pinned and directly passed to unmanaged code.</returns>
            public static ref T GetPinnableReference(T[]? array)
            {
                if (array is null)
                {
                    return ref Unsafe.NullRef<T>();
                }
                return ref MemoryMarshal.GetArrayDataReference(array);
            }
        }
    }
}
