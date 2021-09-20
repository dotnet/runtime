
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.GeneratedMarshalling
{
    public unsafe ref struct ArrayMarshaller<T>
    {
        private T[]? managedArray;
        private readonly int sizeOfNativeElement;
        private IntPtr allocatedMemory;

        public ArrayMarshaller(int sizeOfNativeElement)
            :this()
        {
            this.sizeOfNativeElement = sizeOfNativeElement;
        }

        public ArrayMarshaller(T[]? managed, int sizeOfNativeElement)
        {
            allocatedMemory = default;
            this.sizeOfNativeElement = sizeOfNativeElement;
            if (managed is null)
            {
                managedArray = null;
                NativeValueStorage = default;
                return;
            }
            managedArray = managed;
            this.sizeOfNativeElement = sizeOfNativeElement;
            // Always allocate at least one byte when the array is zero-length.
            int spaceToAllocate = Math.Max(managed.Length * sizeOfNativeElement, 1);
            allocatedMemory = Marshal.AllocCoTaskMem(spaceToAllocate);
            NativeValueStorage = new Span<byte>((void*)allocatedMemory, spaceToAllocate);
        }

        public ArrayMarshaller(T[]? managed, Span<byte> stackSpace, int sizeOfNativeElement)
        {
            allocatedMemory = default;
            this.sizeOfNativeElement = sizeOfNativeElement;
            if (managed is null)
            {
                managedArray = null;
                NativeValueStorage = default;
                return;
            }
            managedArray = managed;
            // Always allocate at least one byte when the array is zero-length.
            int spaceToAllocate = Math.Max(managed.Length * sizeOfNativeElement, 1);
            if (spaceToAllocate <= stackSpace.Length)
            {
                NativeValueStorage = stackSpace[0..spaceToAllocate];
            }
            else
            {
                allocatedMemory = Marshal.AllocCoTaskMem(spaceToAllocate);
                NativeValueStorage = new Span<byte>((void*)allocatedMemory, spaceToAllocate);
            }
        }

        /// <summary>
        /// Stack-alloc threshold set to 256 bytes to enable small arrays to be passed on the stack.
        /// Number kept small to ensure that P/Invokes with a lot of array parameters doesn't
        /// blow the stack since this is a new optimization in the code-generated interop.
        /// </summary>
        public const int StackBufferSize = 0x200;

        public Span<T> ManagedValues => managedArray;

        public Span<byte> NativeValueStorage { get; private set; }

        public ref byte GetPinnableReference() => ref MemoryMarshal.GetReference(NativeValueStorage);

        public void SetUnmarshalledCollectionLength(int length)
        {
            managedArray = new T[length];
        }

        public byte* Value
        {
            get
            {
                Debug.Assert(managedArray is null || allocatedMemory != IntPtr.Zero);
                return (byte*)allocatedMemory;
            }
            set
            {
                if (value == null)
                {
                    managedArray = null;
                    NativeValueStorage = default;
                }
                else
                {
                    allocatedMemory = (IntPtr)value;
                    NativeValueStorage = new Span<byte>(value, (managedArray?.Length ?? 0) * sizeOfNativeElement);
                }
            }
        }

        public T[]? ToManaged() => managedArray;

        public void FreeNative()
        {
            if (allocatedMemory != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(allocatedMemory);
            }
        }
    }

    public unsafe ref struct PtrArrayMarshaller<T> where T : unmanaged
    {
        private T*[]? managedArray;
        private readonly int sizeOfNativeElement;
        private IntPtr allocatedMemory;

        public PtrArrayMarshaller(int sizeOfNativeElement)
            : this()
        {
            this.sizeOfNativeElement = sizeOfNativeElement;
        }

        public PtrArrayMarshaller(T*[]? managed, int sizeOfNativeElement)
        {
            allocatedMemory = default;
            this.sizeOfNativeElement = sizeOfNativeElement;
            if (managed is null)
            {
                managedArray = null;
                NativeValueStorage = default;
                return;
            }
            managedArray = managed;
            this.sizeOfNativeElement = sizeOfNativeElement;
            // Always allocate at least one byte when the array is zero-length.
            int spaceToAllocate = Math.Max(managed.Length * sizeOfNativeElement, 1);
            allocatedMemory = Marshal.AllocCoTaskMem(spaceToAllocate);
            NativeValueStorage = new Span<byte>((void*)allocatedMemory, spaceToAllocate);
        }

        public PtrArrayMarshaller(T*[]? managed, Span<byte> stackSpace, int sizeOfNativeElement)
        {
            allocatedMemory = default;
            this.sizeOfNativeElement = sizeOfNativeElement;
            if (managed is null)
            {
                managedArray = null;
                NativeValueStorage = default;
                return;
            }
            managedArray = managed;
            // Always allocate at least one byte when the array is zero-length.
            int spaceToAllocate = Math.Max(managed.Length * sizeOfNativeElement, 1);
            if (spaceToAllocate <= stackSpace.Length)
            {
                NativeValueStorage = stackSpace[0..spaceToAllocate];
            }
            else
            {
                allocatedMemory = Marshal.AllocCoTaskMem(spaceToAllocate);
                NativeValueStorage = new Span<byte>((void*)allocatedMemory, spaceToAllocate);
            }
        }

        /// <summary>
        /// Stack-alloc threshold set to 256 bytes to enable small arrays to be passed on the stack.
        /// Number kept small to ensure that P/Invokes with a lot of array parameters doesn't
        /// blow the stack since this is a new optimization in the code-generated interop.
        /// </summary>
        public const int StackBufferSize = 0x200;

        public Span<IntPtr> ManagedValues => Unsafe.As<IntPtr[]>(managedArray);

        public Span<byte> NativeValueStorage { get; private set; }

        public ref byte GetPinnableReference() => ref MemoryMarshal.GetReference(NativeValueStorage);

        public void SetUnmarshalledCollectionLength(int length)
        {
            managedArray = new T*[length];
        }

        public byte* Value
        {
            get
            {
                Debug.Assert(managedArray is null || allocatedMemory != IntPtr.Zero);
                return (byte*)allocatedMemory;
            }
            set
            {
                if (value == null)
                {
                    managedArray = null;
                    NativeValueStorage = default;
                }
                else
                {
                    allocatedMemory = (IntPtr)value;
                    NativeValueStorage = new Span<byte>(value, (managedArray?.Length ?? 0) * sizeOfNativeElement);
                }

            }
        }

        public T*[]? ToManaged() => managedArray;

        public void FreeNative()
        {
            if (allocatedMemory != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(allocatedMemory);
            }
        }
    }
}