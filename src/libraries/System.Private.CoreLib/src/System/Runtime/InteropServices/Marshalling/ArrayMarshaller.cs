// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Marshaller for arrays
    /// </summary>
    /// <typeparam name="T">Array element type</typeparam>
    [CLSCompliant(false)]
    [CustomTypeMarshaller(typeof(CustomTypeMarshallerAttribute.GenericPlaceholder[]),
        CustomTypeMarshallerKind.LinearCollection, BufferSize = 0x200,
        Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.CallerAllocatedBuffer | CustomTypeMarshallerFeatures.TwoStageMarshalling)]
    public unsafe ref struct ArrayMarshaller<T>
    {
        private readonly int _sizeOfNativeElement;

        private T[]? _managedArray;
        private IntPtr _allocatedMemory;
        private Span<byte> _span;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayMarshaller{T}"/>.
        /// </summary>
        /// <param name="sizeOfNativeElement">Size of the native element in bytes.</param>
        public ArrayMarshaller(int sizeOfNativeElement)
            : this()
        {
            _sizeOfNativeElement = sizeOfNativeElement;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayMarshaller{T}"/>.
        /// </summary>
        /// <param name="array">Array to be marshalled.</param>
        /// <param name="sizeOfNativeElement">Size of the native element in bytes.</param>
        public ArrayMarshaller(T[]? array, int sizeOfNativeElement)
            : this(array, Span<byte>.Empty, sizeOfNativeElement)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayMarshaller{T}"/>.
        /// </summary>
        /// <param name="array">Array to be marshalled.</param>
        /// <param name="buffer">Buffer that may be used for marshalling.</param>
        /// <param name="sizeOfNativeElement">Size of the native element in bytes.</param>
        /// <remarks>
        /// The <paramref name="buffer"/> must not be movable - that is, it should not be
        /// on the managed heap or it should be pinned.
        /// <seealso cref="CustomTypeMarshallerFeatures.CallerAllocatedBuffer"/>
        /// </remarks>
        public ArrayMarshaller(T[]? array, Span<byte> buffer, int sizeOfNativeElement)
        {
            _allocatedMemory = default;
            _sizeOfNativeElement = sizeOfNativeElement;
            if (array is null)
            {
                _managedArray = null;
                _span = default;
                return;
            }

            _managedArray = array;

            // Always allocate at least one byte when the array is zero-length.
            int bufferSize = checked(array.Length * _sizeOfNativeElement);
            int spaceToAllocate = Math.Max(bufferSize, 1);
            if (spaceToAllocate <= buffer.Length)
            {
                _span = buffer[0..spaceToAllocate];
            }
            else
            {
                _allocatedMemory = Marshal.AllocCoTaskMem(spaceToAllocate);
                _span = new Span<byte>((void*)_allocatedMemory, spaceToAllocate);
            }
        }

        /// <summary>
        /// Gets a span that points to the memory where the managed values of the array are stored.
        /// </summary>
        /// <returns>Span over managed values of the array.</returns>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerDirection.In"/>
        /// </remarks>
        public ReadOnlySpan<T> GetManagedValuesSource() => _managedArray;

        /// <summary>
        /// Gets a span that points to the memory where the unmarshalled managed values of the array should be stored.
        /// </summary>
        /// <param name="length">Length of the array.</param>
        /// <returns>Span where managed values of the array should be stored.</returns>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerDirection.Out"/>
        /// </remarks>
        public Span<T> GetManagedValuesDestination(int length) => _allocatedMemory == IntPtr.Zero ? null : _managedArray = new T[length];

        /// <summary>
        /// Returns a span that points to the memory where the native values of the array are stored after the native call.
        /// </summary>
        /// <param name="length">Length of the array.</param>
        /// <returns>Span over the native values of the array.</returns>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerDirection.Out"/>
        /// </remarks>
        public ReadOnlySpan<byte> GetNativeValuesSource(int length)
        {
            if (_allocatedMemory == IntPtr.Zero)
                return default;

            int allocatedSize = checked(length * _sizeOfNativeElement);
            _span = new Span<byte>((void*)_allocatedMemory, allocatedSize);
            return _span;
        }

        /// <summary>
        /// Returns a span that points to the memory where the native values of the array should be stored.
        /// </summary>
        /// <returns>Span where native values of the array should be stored.</returns>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerDirection.In"/>
        /// </remarks>
        public Span<byte> GetNativeValuesDestination() => _span;

        /// <summary>
        /// Returns a reference to the marshalled array.
        /// </summary>
        public ref byte GetPinnableReference() => ref MemoryMarshal.GetReference(_span);

        /// <summary>
        /// Returns the native value representing the array.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.TwoStageMarshalling"/>
        /// </remarks>
        public byte* ToNativeValue() => (byte*)Unsafe.AsPointer(ref GetPinnableReference());

        /// <summary>
        /// Sets the native value representing the array.
        /// </summary>
        /// <param name="value">The native value.</param>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.TwoStageMarshalling"/>
        /// </remarks>
        public void FromNativeValue(byte* value)
        {
            _allocatedMemory = (IntPtr)value;
        }

        /// <summary>
        /// Returns the managed array.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerDirection.Out"/>
        /// </remarks>
        public T[]? ToManaged() => _managedArray;

        /// <summary>
        /// Frees native resources.
        /// </summary>
        /// <remarks>
        /// <seealso cref="CustomTypeMarshallerFeatures.UnmanagedResources"/>
        /// </remarks>
        public void FreeNative()
        {
            Marshal.FreeCoTaskMem(_allocatedMemory);
        }
    }
}
