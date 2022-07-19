// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Marshaller for arrays of pointers
    /// </summary>
    /// <typeparam name="T">Array element pointer type</typeparam>
    /// <typeparam name="TUnmanagedElement">The unmanaged type for the element pointer type</typeparam>
    [CLSCompliant(false)]
    [CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder*[]),
        MarshalMode.Default,
        typeof(PointerArrayMarshaller<,>))]
    [CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder*[]),
        MarshalMode.ManagedToUnmanagedIn,
        typeof(PointerArrayMarshaller<,>.ManagedToUnmanagedIn))]
    [ContiguousCollectionMarshaller]
    public static unsafe class PointerArrayMarshaller<T, TUnmanagedElement>
        where T : unmanaged
        where TUnmanagedElement : unmanaged
    {
        public static TUnmanagedElement* AllocateContainerForUnmanagedElements(T*[]? managed, out int numElements)
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

        public static ReadOnlySpan<IntPtr> GetManagedValuesSource(T*[]? managed)
            => Unsafe.As<IntPtr[]>(managed);

        public static Span<TUnmanagedElement> GetUnmanagedValuesDestination(TUnmanagedElement* unmanaged, int numElements)
            => new Span<TUnmanagedElement>(unmanaged, numElements);

        public static T*[]? AllocateContainerForManagedElements(TUnmanagedElement* unmanaged, int numElements)
        {
            if (unmanaged is null)
                return null;

            return new T*[numElements];
        }

        public static Span<IntPtr> GetManagedValuesDestination(T*[]? managed)
            => Unsafe.As<IntPtr[]>(managed);

        public static ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(TUnmanagedElement* unmanagedValue, int numElements)
            => new ReadOnlySpan<TUnmanagedElement>(unmanagedValue, numElements);

        public static void Free(TUnmanagedElement* unmanaged)
            => Marshal.FreeCoTaskMem((IntPtr)unmanaged);

        public ref struct ManagedToUnmanagedIn
        {
            public static int BufferSize => 0x200 / sizeof(TUnmanagedElement);

            private T*[]? _managedArray;
            private TUnmanagedElement* _allocatedMemory;
            private Span<TUnmanagedElement> _span;

            /// <summary>
            /// Initializes the <see cref="PointerArrayMarshaller{T, TUnmanagedElement}.ManagedToUnmanagedIn"/> marshaller.
            /// </summary>
            /// <param name="array">Array to be marshalled.</param>
            /// <param name="buffer">Buffer that may be used for marshalling.</param>
            /// <remarks>
            /// The <paramref name="buffer"/> must not be movable - that is, it should not be
            /// on the managed heap or it should be pinned.
            /// </remarks>
            public void FromManaged(T*[]? array, Span<TUnmanagedElement> buffer)
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
            /// Gets a span that points to the memory where the managed values of the array are stored.
            /// </summary>
            /// <returns>Span over managed values of the array.</returns>
            public ReadOnlySpan<IntPtr> GetManagedValuesSource() => Unsafe.As<IntPtr[]>(_managedArray);

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

            public static ref byte GetPinnableReference(T*[]? array)
            {
                if (array is null)
                {
                    return ref Unsafe.NullRef<byte>();
                }
                return ref MemoryMarshal.GetArrayDataReference(array);
            }
        }
    }
}
