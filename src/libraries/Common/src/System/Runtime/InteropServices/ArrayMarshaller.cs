// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

//
// Types in this file are used for generated p/invokes (docs/design/features/source-generator-pinvokes.md).
//

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.GeneratedMarshalling
{
    // Stack-alloc threshold set to 256 bytes to enable small arrays to be passed on the stack.
    // Number kept small to ensure that P/Invokes with a lot of array parameters doesn't
    // blow the stack since this is a new optimization in the code-generated interop.
    [CustomTypeMarshaller(typeof(CustomTypeMarshallerAttribute.GenericPlaceholder[]), CustomTypeMarshallerKind.LinearCollection, Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.CallerAllocatedBuffer | CustomTypeMarshallerFeatures.TwoStageMarshalling, BufferSize = 0x200)]
#if LIBRARYIMPORT_GENERATOR_TEST
    public
#else
    internal
#endif
    unsafe ref struct ArrayMarshaller<T>
    {
        private T[]? _managedArray;
        private readonly int _sizeOfNativeElement;
        private IntPtr _allocatedMemory;

        public ArrayMarshaller(int sizeOfNativeElement)
            : this()
        {
            _sizeOfNativeElement = sizeOfNativeElement;
        }

        public ArrayMarshaller(T[]? managed, int sizeOfNativeElement)
            :this(managed, Span<byte>.Empty, sizeOfNativeElement)
        {
        }

        public ArrayMarshaller(T[]? managed, Span<byte> stackSpace, int sizeOfNativeElement)
        {
            _allocatedMemory = default;
            _sizeOfNativeElement = sizeOfNativeElement;
            if (managed is null)
            {
                _managedArray = null;
                NativeValueStorage = default;
                return;
            }
            _managedArray = managed;
            // Always allocate at least one byte when the array is zero-length.
            int spaceToAllocate = Math.Max(managed.Length * _sizeOfNativeElement, 1);
            if (spaceToAllocate <= stackSpace.Length)
            {
                NativeValueStorage = stackSpace[0..spaceToAllocate];
            }
            else
            {
                _allocatedMemory = Marshal.AllocCoTaskMem(spaceToAllocate);
                NativeValueStorage = new Span<byte>((void*)_allocatedMemory, spaceToAllocate);
            }
        }

        public ReadOnlySpan<T> GetManagedValuesSource() => _managedArray;
        public Span<T> GetManagedValuesDestination(int length) => _allocatedMemory == IntPtr.Zero ? null : _managedArray = new T[length];
        public Span<byte> GetNativeValuesDestination() => NativeValueStorage;

        public ReadOnlySpan<byte> GetNativeValuesSource(int length)
        {
            return _allocatedMemory == IntPtr.Zero ? default : NativeValueStorage = new Span<byte>((void*)_allocatedMemory, length * _sizeOfNativeElement);
        }
        private Span<byte> NativeValueStorage { get; set; }

        public ref byte GetPinnableReference() => ref MemoryMarshal.GetReference(NativeValueStorage);


        public byte* ToNativeValue() => (byte*)Unsafe.AsPointer(ref GetPinnableReference());

        public void FromNativeValue(byte* value)
        {
            _allocatedMemory = (IntPtr)value;
        }

        public T[]? ToManaged() => _managedArray;

        public void FreeNative()
        {
            Marshal.FreeCoTaskMem(_allocatedMemory);
        }
    }

    // Stack-alloc threshold set to 256 bytes to enable small arrays to be passed on the stack.
    // Number kept small to ensure that P/Invokes with a lot of array parameters doesn't
    // blow the stack since this is a new optimization in the code-generated interop.
    [CustomTypeMarshaller(typeof(CustomTypeMarshallerAttribute.GenericPlaceholder*[]), CustomTypeMarshallerKind.LinearCollection, Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.CallerAllocatedBuffer | CustomTypeMarshallerFeatures.TwoStageMarshalling, BufferSize = 0x200)]
#if LIBRARYIMPORT_GENERATOR_TEST
    public
#else
    internal
#endif
    unsafe ref struct PtrArrayMarshaller<T> where T : unmanaged
    {
        private T*[]? _managedArray;
        private readonly int _sizeOfNativeElement;
        private IntPtr _allocatedMemory;

        public PtrArrayMarshaller(int sizeOfNativeElement)
            : this()
        {
            _sizeOfNativeElement = sizeOfNativeElement;
        }

        public PtrArrayMarshaller(T*[]? managed, int sizeOfNativeElement)
            :this(managed, Span<byte>.Empty, sizeOfNativeElement)
        {
        }

        public PtrArrayMarshaller(T*[]? managed, Span<byte> stackSpace, int sizeOfNativeElement)
        {
            _allocatedMemory = default;
            _sizeOfNativeElement = sizeOfNativeElement;
            if (managed is null)
            {
                _managedArray = null;
                NativeValueStorage = default;
                return;
            }
            _managedArray = managed;
            // Always allocate at least one byte when the array is zero-length.
            int spaceToAllocate = Math.Max(managed.Length * _sizeOfNativeElement, 1);
            if (spaceToAllocate <= stackSpace.Length)
            {
                NativeValueStorage = stackSpace[0..spaceToAllocate];
            }
            else
            {
                _allocatedMemory = Marshal.AllocCoTaskMem(spaceToAllocate);
                NativeValueStorage = new Span<byte>((void*)_allocatedMemory, spaceToAllocate);
            }
        }

        public ReadOnlySpan<IntPtr> GetManagedValuesSource() => Unsafe.As<IntPtr[]>(_managedArray);
        public Span<IntPtr> GetManagedValuesDestination(int length) => _allocatedMemory == IntPtr.Zero ? null : Unsafe.As<IntPtr[]>(_managedArray = new T*[length]);
        public Span<byte> GetNativeValuesDestination() => NativeValueStorage;

        public ReadOnlySpan<byte> GetNativeValuesSource(int length)
        {
            return _allocatedMemory == IntPtr.Zero ? default : NativeValueStorage = new Span<byte>((void*)_allocatedMemory, length * _sizeOfNativeElement);
        }
        private Span<byte> NativeValueStorage { get; set; }

        public ref byte GetPinnableReference() => ref MemoryMarshal.GetReference(NativeValueStorage);
        public byte* ToNativeValue() => (byte*)Unsafe.AsPointer(ref GetPinnableReference());

        public void FromNativeValue(byte* value)
        {
            _allocatedMemory = (IntPtr)value;
        }

        public T*[]? ToManaged() => _managedArray;

        public void FreeNative()
        {
            Marshal.FreeCoTaskMem(_allocatedMemory);
        }
    }
}
