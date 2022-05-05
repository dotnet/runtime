// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling
{
    // Stack-alloc threshold set to 256 bytes to enable small arrays to be passed on the stack.
    // Number kept small to ensure that P/Invokes with a lot of array parameters doesn't
    // blow the stack since this is a new optimization in the code-generated interop.
    [CustomTypeMarshaller(typeof(ReadOnlySpan<>), CustomTypeMarshallerKind.LinearCollection, Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.CallerAllocatedBuffer | CustomTypeMarshallerFeatures.TwoStageMarshalling, BufferSize = 0x200)]
    public unsafe ref struct ReadOnlySpanMarshaller<T>
    {
        private ReadOnlySpan<T> _managedSpan;
        private readonly int _sizeOfNativeElement;
        private IntPtr _allocatedMemory;

        public ReadOnlySpanMarshaller(int sizeOfNativeElement)
            : this()
        {
            _sizeOfNativeElement = sizeOfNativeElement;
        }

        public ReadOnlySpanMarshaller(ReadOnlySpan<T> managed, int sizeOfNativeElement)
            :this(managed, Span<byte>.Empty, sizeOfNativeElement)
        {
        }

        public ReadOnlySpanMarshaller(ReadOnlySpan<T> managed, Span<byte> stackSpace, int sizeOfNativeElement)
        {
            _allocatedMemory = default;
            _sizeOfNativeElement = sizeOfNativeElement;
            if (managed.Length == 0)
            {
                _managedSpan = default;
                NativeValueStorage = default;
                return;
            }
            _managedSpan = managed;
            int spaceToAllocate = managed.Length * sizeOfNativeElement;
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

        public ReadOnlySpan<T> GetManagedValuesSource() => _managedSpan;

        public Span<byte> GetNativeValuesDestination() => NativeValueStorage;

        private Span<byte> NativeValueStorage { get; }

        public ref byte GetPinnableReference() => ref MemoryMarshal.GetReference(NativeValueStorage);

        public byte* ToNativeValue() => (byte*)Unsafe.AsPointer(ref GetPinnableReference());

        public void FreeNative()
        {
            Marshal.FreeCoTaskMem(_allocatedMemory);
        }
    }

    [CustomTypeMarshaller(typeof(Span<>), CustomTypeMarshallerKind.LinearCollection, Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.CallerAllocatedBuffer | CustomTypeMarshallerFeatures.TwoStageMarshalling, BufferSize = 0x200)]
    public unsafe ref struct SpanMarshaller<T>
    {
        private Span<T> _managedSpan;
        private readonly int _sizeOfNativeElement;
        private IntPtr _allocatedMemory;

        public SpanMarshaller(int sizeOfNativeElement)
            : this()
        {
            _sizeOfNativeElement = sizeOfNativeElement;
        }

        public SpanMarshaller(Span<T> managed, int sizeOfNativeElement)
            :this(managed, Span<byte>.Empty, sizeOfNativeElement)
        {
        }

        public SpanMarshaller(Span<T> managed, Span<byte> stackSpace, int sizeOfNativeElement)
        {
            _allocatedMemory = default;
            _sizeOfNativeElement = sizeOfNativeElement;
            if (managed.Length == 0)
            {
                _managedSpan = default;
                NativeValueStorage = default;
                return;
            }
            _managedSpan = managed;
            int spaceToAllocate = managed.Length * sizeOfNativeElement;
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

        private Span<byte> NativeValueStorage { get; set; }

        public ReadOnlySpan<T> GetManagedValuesSource() => _managedSpan;
        public Span<T> GetManagedValuesDestination(int length) => _managedSpan = new T[length];
        public Span<byte> GetNativeValuesDestination() => NativeValueStorage;
        public ReadOnlySpan<byte> GetNativeValuesSource(int length) => NativeValueStorage = new Span<byte>((void*)_allocatedMemory, length * _sizeOfNativeElement);
        public ref byte GetPinnableReference() => ref NativeValueStorage.GetPinnableReference();
        public byte* ToNativeValue() => (byte*)Unsafe.AsPointer(ref GetPinnableReference());
        public void FromNativeValue(byte* value) => _allocatedMemory = (IntPtr)value;

        public Span<T> ToManaged()
        {
            return _managedSpan;
        }

        public void FreeNative()
        {
            Marshal.FreeCoTaskMem(_allocatedMemory);
        }
    }

    [CustomTypeMarshaller(typeof(Span<>), CustomTypeMarshallerKind.LinearCollection, Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.CallerAllocatedBuffer | CustomTypeMarshallerFeatures.TwoStageMarshalling, BufferSize = 0x200)]
    public unsafe ref struct NeverNullSpanMarshaller<T>
    {
        private SpanMarshaller<T> _inner;

        public NeverNullSpanMarshaller(int sizeOfNativeElement)
            : this()
        {
            _inner = new SpanMarshaller<T>(sizeOfNativeElement);
        }

        public NeverNullSpanMarshaller(Span<T> managed, int sizeOfNativeElement)
        {
            _inner = new SpanMarshaller<T>(managed, sizeOfNativeElement);
        }

        public NeverNullSpanMarshaller(Span<T> managed, Span<byte> stackSpace, int sizeOfNativeElement)
        {
            _inner = new SpanMarshaller<T>(managed, stackSpace, sizeOfNativeElement);
        }


        public ReadOnlySpan<T> GetManagedValuesSource() => _inner.GetManagedValuesSource();
        public Span<T> GetManagedValuesDestination(int length) => _inner.GetManagedValuesDestination(length);
        public Span<byte> GetNativeValuesDestination() => _inner.GetNativeValuesDestination();
        public ReadOnlySpan<byte> GetNativeValuesSource(int length) => _inner.GetNativeValuesSource(length);
        public ref byte GetPinnableReference() => ref _inner.GetPinnableReference();
        public byte* ToNativeValue() => _inner.GetManagedValuesSource().Length == 0 ? (byte*)0xa5a5a5a5 : (byte*)Unsafe.AsPointer(ref GetPinnableReference());
        public void FromNativeValue(byte* value) => _inner.FromNativeValue(value);

        public Span<T> ToManaged() => _inner.ToManaged();

        public void FreeNative()
        {
            _inner.FreeNative();
        }
    }

    [CustomTypeMarshaller(typeof(ReadOnlySpan<>), CustomTypeMarshallerKind.LinearCollection, Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.CallerAllocatedBuffer | CustomTypeMarshallerFeatures.TwoStageMarshalling, BufferSize = 0x200)]
    public unsafe ref struct NeverNullReadOnlySpanMarshaller<T>
    {
        private ReadOnlySpanMarshaller<T> _inner;

        public NeverNullReadOnlySpanMarshaller(int sizeOfNativeElement)
            : this()
        {
            _inner = new ReadOnlySpanMarshaller<T>(sizeOfNativeElement);
        }

        public NeverNullReadOnlySpanMarshaller(ReadOnlySpan<T> managed, int sizeOfNativeElement)
        {
            _inner = new ReadOnlySpanMarshaller<T>(managed, sizeOfNativeElement);
        }

        public NeverNullReadOnlySpanMarshaller(ReadOnlySpan<T> managed, Span<byte> stackSpace, int sizeOfNativeElement)
        {
            _inner = new ReadOnlySpanMarshaller<T>(managed, stackSpace, sizeOfNativeElement);
        }

        public ReadOnlySpan<T> GetManagedValuesSource() => _inner.GetManagedValuesSource();
        public Span<byte> GetNativeValuesDestination() => _inner.GetNativeValuesDestination();
        public ref byte GetPinnableReference() => ref _inner.GetPinnableReference();
        public byte* ToNativeValue() => _inner.GetManagedValuesSource().Length == 0 ? (byte*)0xa5a5a5a5 : (byte*)Unsafe.AsPointer(ref GetPinnableReference());

        public void FreeNative()
        {
            _inner.FreeNative();
        }
    }

    // Stack-alloc threshold set to 0 so that the generator can use the constructor that takes a stackSpace to let the marshaller know that the original data span can be used and safely pinned.
    [CustomTypeMarshaller(typeof(Span<>), CustomTypeMarshallerKind.LinearCollection, Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.CallerAllocatedBuffer | CustomTypeMarshallerFeatures.TwoStageMarshalling, BufferSize = 0)]
    public unsafe ref struct DirectSpanMarshaller<T>
        where T : unmanaged
    {
        private T* _allocatedMemory;
        private T* _nativeValue;
        private Span<T> _data;

        public DirectSpanMarshaller(int sizeOfNativeElement)
            :this()
        {
        }

        public DirectSpanMarshaller(Span<T> managed, int sizeOfNativeElement)
            :this(sizeOfNativeElement)
        {
            if (managed.Length == 0)
            {
                return;
            }

            int spaceToAllocate = managed.Length * Unsafe.SizeOf<T>();
            _allocatedMemory = (T*)Marshal.AllocCoTaskMem(spaceToAllocate);
            _data = managed;
        }

        public DirectSpanMarshaller(Span<T> managed, Span<byte> stackSpace, int sizeOfNativeElement)
            :this(sizeOfNativeElement)
        {
            Debug.Assert(stackSpace.IsEmpty);
            _data = managed;
        }

        public ReadOnlySpan<T> GetManagedValuesSource() => _data;
        public Span<T> GetManagedValuesDestination(int length) => _data = new Span<T>(_nativeValue, length);
        public Span<byte> GetNativeValuesDestination() => _allocatedMemory != null
            ? new Span<byte>(_allocatedMemory, _data.Length * Unsafe.SizeOf<T>())
            : MemoryMarshal.Cast<T, byte>(_data);

        public ReadOnlySpan<byte> GetNativeValuesSource(int length) => new ReadOnlySpan<byte>(_nativeValue, length * sizeof(T));

        public ref T GetPinnableReference() => ref _data.GetPinnableReference();

        public T* ToNativeValue()
        {
            if (_allocatedMemory  != null)
            {
                return _allocatedMemory;
            }
            return (T*)Unsafe.AsPointer(ref GetPinnableReference());
        }

        public void FromNativeValue(T* value)
        {
            _allocatedMemory = null;
            _nativeValue = value;
        }

        public Span<T> ToManaged()
        {
            return _data;
        }

        public void FreeNative()
        {
            Marshal.FreeCoTaskMem((IntPtr)_allocatedMemory);
        }
    }
}
