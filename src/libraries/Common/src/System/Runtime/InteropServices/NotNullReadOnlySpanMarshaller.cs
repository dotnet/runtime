// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Runtime.InteropServices.Marshalling
{
    [CustomMarshaller(typeof(ReadOnlySpan<>), MarshalMode.ManagedToUnmanagedIn, typeof(NotNullReadOnlySpanMarshaller<,>.ManagedToUnmanagedIn))]
    [CustomMarshaller(typeof(ReadOnlySpan<>), MarshalMode.ManagedToUnmanagedOut, typeof(NotNullReadOnlySpanMarshaller<,>.ManagedToUnmanagedOut))]
    [ContiguousCollectionMarshaller]
    internal static unsafe class NotNullReadOnlySpanMarshaller<T, TUnmanagedElement>
        where TUnmanagedElement : unmanaged
    {
        public ref struct ManagedToUnmanagedIn
        {
            /// <summary>
            /// Gets the size of the caller-allocated buffer to allocate.
            /// </summary>
            // We'll keep the buffer size at a maximum of 512 bytes to avoid overflowing the stack.
            public static int BufferSize => 0x200 / sizeof(TUnmanagedElement);

            private ReadOnlySpan<T> _managedArray;
            private TUnmanagedElement* _allocatedMemory;
            private Span<TUnmanagedElement> _span;

            public void FromManaged(ReadOnlySpan<T> managed, Span<TUnmanagedElement> buffer)
            {
                _managedArray = managed;

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

            public ReadOnlySpan<T> GetManagedValuesSource() => _managedArray;

            public Span<TUnmanagedElement> GetUnmanagedValuesDestination() => _span;

            public ref TUnmanagedElement GetPinnableReference() => ref MemoryMarshal.GetReference(_span);

            public TUnmanagedElement* ToUnmanaged()
            {
                // Unsafe.AsPointer is safe since buffer must be pinned
                return (TUnmanagedElement*)Unsafe.AsPointer(ref GetPinnableReference());
            }

            public void Free()
            {
                NativeMemory.Free(_allocatedMemory);
            }

            public static ref T GetPinnableReference(ReadOnlySpan<T> managed)
            {
                if (Unsafe.IsNullRef(ref MemoryMarshal.GetReference(managed)) && managed.IsEmpty)
                {
                    return ref MemoryMarshal.GetArrayDataReference(Array.Empty<T>());
                }
                else
                {
                    return ref MemoryMarshal.GetReference(managed);
                }
            }
        }

        public struct ManagedToUnmanagedOut
        {
            private TUnmanagedElement* _unmanagedArray;
            private T[]? _managedValues;

            public void FromUnmanaged(TUnmanagedElement* unmanaged)
            {
                _unmanagedArray = unmanaged;
            }

            public ReadOnlySpan<T> ToManaged()
            {
                return new ReadOnlySpan<T>(_managedValues!);
            }

            public ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(int numElements)
            {
                return new ReadOnlySpan<TUnmanagedElement>(_unmanagedArray, numElements);
            }

            public Span<T> GetManagedValuesDestination(int numElements)
            {
                _managedValues = new T[numElements];
                return _managedValues;
            }

            public void Free()
            {
                Marshal.FreeCoTaskMem((IntPtr)_unmanagedArray);
            }
        }
    }
}
