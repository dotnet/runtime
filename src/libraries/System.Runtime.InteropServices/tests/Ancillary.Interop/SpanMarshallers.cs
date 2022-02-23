// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.GeneratedMarshalling
{
    [GenericContiguousCollectionMarshaller]
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

        /// <summary>
        /// Stack-alloc threshold set to 256 bytes to enable small arrays to be passed on the stack.
        /// Number kept small to ensure that P/Invokes with a lot of array parameters doesn't
        /// blow the stack since this is a new optimization in the code-generated interop.
        /// </summary>
        public const int BufferSize = 0x200;
        public const bool RequiresStackBuffer = true;

        public Span<T> ManagedValues => MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(_managedSpan), _managedSpan.Length);

        public Span<byte> NativeValueStorage { get; private set; }

        public ref byte GetPinnableReference() => ref MemoryMarshal.GetReference(NativeValueStorage);

        public void SetUnmarshalledCollectionLength(int length)
        {
            _managedSpan = new T[length];
        }

        public byte* Value
        {
            get
            {
                return (byte*)Unsafe.AsPointer(ref GetPinnableReference());
            }
            set
            {
                if (value == null)
                {
                    _managedSpan = null;
                    NativeValueStorage = default;
                }
                else
                {
                    _allocatedMemory = (IntPtr)value;
                    NativeValueStorage = new Span<byte>(value, _managedSpan.Length * _sizeOfNativeElement);
                }
            }
        }

        public ReadOnlySpan<T> ToManaged() => _managedSpan;

        public void FreeNative()
        {
            Marshal.FreeCoTaskMem(_allocatedMemory);
        }
    }

    [GenericContiguousCollectionMarshaller]
    public unsafe ref struct SpanMarshaller<T>
    {
        private ReadOnlySpanMarshaller<T> _inner;

        public SpanMarshaller(int sizeOfNativeElement)
            : this()
        {
            _inner = new ReadOnlySpanMarshaller<T>(sizeOfNativeElement);
        }

        public SpanMarshaller(Span<T> managed, int sizeOfNativeElement)
        {
            _inner = new ReadOnlySpanMarshaller<T>(managed, sizeOfNativeElement);
        }

        public SpanMarshaller(Span<T> managed, Span<byte> stackSpace, int sizeOfNativeElement)
        {
            _inner = new ReadOnlySpanMarshaller<T>(managed, stackSpace, sizeOfNativeElement);
        }

        /// <summary>
        /// Stack-alloc threshold set to 256 bytes to enable small arrays to be passed on the stack.
        /// Number kept small to ensure that P/Invokes with a lot of array parameters doesn't
        /// blow the stack since this is a new optimization in the code-generated interop.
        /// </summary>
        public const int BufferSize = ReadOnlySpanMarshaller<T>.BufferSize;
        public const bool RequiresStackBuffer = ReadOnlySpanMarshaller<T>.RequiresStackBuffer;

        public Span<T> ManagedValues => _inner.ManagedValues;

        public Span<byte> NativeValueStorage
        {
            get => _inner.NativeValueStorage;
        }

        public ref byte GetPinnableReference() => ref _inner.GetPinnableReference();

        public void SetUnmarshalledCollectionLength(int length)
        {
            _inner.SetUnmarshalledCollectionLength(length);
        }

        public byte* Value
        {
            get => _inner.Value;
            set => _inner.Value = value;
        }

        public Span<T> ToManaged()
        {
            ReadOnlySpan<T> managedInner = _inner.ToManaged();
            return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(managedInner), managedInner.Length);
        }

        public void FreeNative()
        {
            _inner.FreeNative();
        }
    }

    [GenericContiguousCollectionMarshaller]
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

        /// <summary>
        /// Stack-alloc threshold set to 256 bytes to enable small spans to be passed on the stack.
        /// Number kept small to ensure that P/Invokes with a lot of span parameters doesn't
        /// blow the stack.
        /// </summary>
        public const int BufferSize = SpanMarshaller<T>.BufferSize;

        public Span<T> ManagedValues => _inner.ManagedValues;

        public Span<byte> NativeValueStorage
        {
            get => _inner.NativeValueStorage;
        }

        public ref byte GetPinnableReference()
        {
            if (_inner.ManagedValues.Length == 0)
            {
                return ref *(byte*)0xa5a5a5a5;
            }
            return ref _inner.GetPinnableReference();
        }

        public void SetUnmarshalledCollectionLength(int length)
        {
            _inner.SetUnmarshalledCollectionLength(length);
        }

        public byte* Value
        {
            get
            {
                if (_inner.ManagedValues.Length == 0)
                {
                    return (byte*)0xa5a5a5a5;
                }
                return _inner.Value;
            }

            set => _inner.Value = value;
        }

        public Span<T> ToManaged() => _inner.ToManaged();

        public void FreeNative()
        {
            _inner.FreeNative();
        }
    }

    [GenericContiguousCollectionMarshaller]
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

        /// <summary>
        /// Stack-alloc threshold set to 256 bytes to enable small spans to be passed on the stack.
        /// Number kept small to ensure that P/Invokes with a lot of span parameters doesn't
        /// blow the stack.
        /// </summary>
        public const int BufferSize = SpanMarshaller<T>.BufferSize;
        public const bool RequiresStackBuffer = SpanMarshaller<T>.RequiresStackBuffer;

        public Span<T> ManagedValues => _inner.ManagedValues;

        public Span<byte> NativeValueStorage
        {
            get => _inner.NativeValueStorage;
        }

        public ref byte GetPinnableReference()
        {
            if (_inner.ManagedValues.Length == 0)
            {
                return ref *(byte*)0xa5a5a5a5;
            }
            return ref _inner.GetPinnableReference();
        }

        public void SetUnmarshalledCollectionLength(int length)
        {
            _inner.SetUnmarshalledCollectionLength(length);
        }

        public byte* Value
        {
            get
            {
                if (_inner.ManagedValues.Length == 0)
                {
                    return (byte*)0xa5a5a5a5;
                }
                return _inner.Value;
            }

            set => _inner.Value = value;
        }

        public ReadOnlySpan<T> ToManaged() => _inner.ToManaged();

        public void FreeNative()
        {
            _inner.FreeNative();
        }
    }

    [GenericContiguousCollectionMarshaller]
    public unsafe ref struct DirectSpanMarshaller<T>
        where T : unmanaged
    {
        private int _unmarshalledLength;
        private T* _allocatedMemory;
        private Span<T> _data;

        public DirectSpanMarshaller(int sizeOfNativeElement)
            :this()
        {
            // This check is not exhaustive, but it will catch the majority of cases.
            if (typeof(T) == typeof(bool) || typeof(T) == typeof(char) || Unsafe.SizeOf<T>() != sizeOfNativeElement)
            {
                throw new ArgumentException("This marshaller only supports blittable element types. The provided type parameter must be blittable", nameof(T));
            }
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

        /// <summary>
        /// Stack-alloc threshold set to 0 so that the generator can use the constructor that takes a stackSpace to let the marshaller know that the original data span can be used and safely pinned.
        /// </summary>
        public const int BufferSize = 0;

        public Span<T> ManagedValues => _data;

        public Span<byte> NativeValueStorage => _allocatedMemory != null
            ? new Span<byte>(_allocatedMemory, _data.Length * Unsafe.SizeOf<T>())
            : MemoryMarshal.Cast<T, byte>(_data);

        public ref T GetPinnableReference() => ref _data.GetPinnableReference();

        public void SetUnmarshalledCollectionLength(int length)
        {
            _unmarshalledLength = length;
        }

        public T* Value
        {
            get
            {
                if (_allocatedMemory  != null)
                {
                    return _allocatedMemory;
                }
                return (T*)Unsafe.AsPointer(ref GetPinnableReference());
            }
            set
            {
                // We don't save the pointer assigned here to be freed
                // since this marshaller passes back the actual memory span from native code
                // back to managed code.
                _allocatedMemory = null;
                _data = new Span<T>(value, _unmarshalledLength);
            }
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
