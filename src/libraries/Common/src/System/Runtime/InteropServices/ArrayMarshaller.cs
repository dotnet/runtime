// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

//
// Types in this file are used for generated p/invokes (docs/design/features/source-generator-pinvokes.md).
// See the DllImportGenerator experiment in https://github.com/dotnet/runtimelab.
//
#if DLLIMPORTGENERATOR_INTERNALUNSAFE
using Internal.Runtime.CompilerServices;
#else
using System.Runtime.CompilerServices;
#endif
using System.Diagnostics;

namespace System.Runtime.InteropServices.GeneratedMarshalling
{
#if DLLIMPORT_GENERATOR_TEST
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

        /// <summary>
        /// Stack-alloc threshold set to 256 bytes to enable small arrays to be passed on the stack.
        /// Number kept small to ensure that P/Invokes with a lot of array parameters doesn't
        /// blow the stack since this is a new optimization in the code-generated interop.
        /// </summary>
        public const int BufferSize = 0x200;
        public const bool RequiresStackBuffer = true;

        public Span<T> ManagedValues => _managedArray;

        public Span<byte> NativeValueStorage { get; private set; }

        public ref byte GetPinnableReference() => ref MemoryMarshal.GetReference(NativeValueStorage);

        public void SetUnmarshalledCollectionLength(int length)
        {
            _managedArray = new T[length];
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
                    _managedArray = null;
                    NativeValueStorage = default;
                }
                else
                {
                    _allocatedMemory = (IntPtr)value;
                    NativeValueStorage = new Span<byte>(value, (_managedArray?.Length ?? 0) * _sizeOfNativeElement);
                }
            }
        }

        public T[]? ToManaged() => _managedArray;

        public void FreeNative()
        {
            Marshal.FreeCoTaskMem(_allocatedMemory);
        }
    }

#if DLLIMPORT_GENERATOR_TEST
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

        /// <summary>
        /// Stack-alloc threshold set to 256 bytes to enable small arrays to be passed on the stack.
        /// Number kept small to ensure that P/Invokes with a lot of array parameters doesn't
        /// blow the stack since this is a new optimization in the code-generated interop.
        /// </summary>
        public const int BufferSize = 0x200;
        public const bool RequiresStackBuffer = true;

        public Span<IntPtr> ManagedValues => Unsafe.As<IntPtr[]>(_managedArray);

        public Span<byte> NativeValueStorage { get; private set; }

        public ref byte GetPinnableReference() => ref MemoryMarshal.GetReference(NativeValueStorage);

        public void SetUnmarshalledCollectionLength(int length)
        {
            _managedArray = new T*[length];
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
                    _managedArray = null;
                    NativeValueStorage = default;
                }
                else
                {
                    _allocatedMemory = (IntPtr)value;
                    NativeValueStorage = new Span<byte>(value, (_managedArray?.Length ?? 0) * _sizeOfNativeElement);
                }

            }
        }

        public T*[]? ToManaged() => _managedArray;

        public void FreeNative()
        {
            Marshal.FreeCoTaskMem(_allocatedMemory);
        }
    }
}
