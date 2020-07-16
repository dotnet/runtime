// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    public static partial class RuntimeHelpers
    {
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void InitializeArray(Array array, RuntimeFieldHandle fldHandle);

        // GetObjectValue is intended to allow value classes to be manipulated as 'Object'
        // but have aliasing behavior of a value class.  The intent is that you would use
        // this function just before an assignment to a variable of type 'Object'.  If the
        // value being assigned is a mutable value class, then a shallow copy is returned
        // (because value classes have copy semantics), but otherwise the object itself
        // is returned.
        //
        // Note: VB calls this method when they're about to assign to an Object
        // or pass it as a parameter.  The goal is to make sure that boxed
        // value types work identical to unboxed value types - ie, they get
        // cloned when you pass them around, and are always passed by value.
        // Of course, reference types are not cloned.
        //
        [MethodImpl(MethodImplOptions.InternalCall)]
        [return: NotNullIfNotNull("obj")]
        public static extern object? GetObjectValue(object? obj);

        // RunClassConstructor causes the class constructor for the given type to be triggered
        // in the current domain.  After this call returns, the class constructor is guaranteed to
        // have at least been started by some thread.  In the absence of class constructor
        // deadlock conditions, the call is further guaranteed to have completed.
        //
        // This call will generate an exception if the specified class constructor threw an
        // exception when it ran.

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _RunClassConstructor(RuntimeType type);

        public static void RunClassConstructor(RuntimeTypeHandle type)
        {
            _RunClassConstructor(type.GetRuntimeType());
        }

        // RunModuleConstructor causes the module constructor for the given type to be triggered
        // in the current domain.  After this call returns, the module constructor is guaranteed to
        // have at least been started by some thread.  In the absence of module constructor
        // deadlock conditions, the call is further guaranteed to have completed.
        //
        // This call will generate an exception if the specified module constructor threw an
        // exception when it ran.

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _RunModuleConstructor(System.Reflection.RuntimeModule module);

        public static void RunModuleConstructor(ModuleHandle module)
        {
            _RunModuleConstructor(module.GetRuntimeModule());
        }


        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern void _CompileMethod(RuntimeMethodHandleInternal method);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void _PrepareMethod(IRuntimeMethodInfo method, IntPtr* pInstantiation, int cInstantiation);

        public static void PrepareMethod(RuntimeMethodHandle method)
        {
            unsafe
            {
                _PrepareMethod(method.GetMethodInfo(), null, 0);
            }
        }

        public static void PrepareMethod(RuntimeMethodHandle method, RuntimeTypeHandle[]? instantiation)
        {
            unsafe
            {
                IntPtr[]? instantiationHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(instantiation, out int length);
                fixed (IntPtr* pInstantiation = instantiationHandles)
                {
                    _PrepareMethod(method.GetMethodInfo(), pInstantiation, length);
                    GC.KeepAlive(instantiation);
                }
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void PrepareDelegate(Delegate d);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int GetHashCode(object? o);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern new bool Equals(object? o1, object? o2);

        public static int OffsetToStringData
        {
            // This offset is baked in by string indexer intrinsic, so there is no harm
            // in getting it baked in here as well.
            [System.Runtime.Versioning.NonVersionable]
            get =>
                // Number of bytes from the address pointed to by a reference to
                // a String to the first 16-bit character in the String.  Skip
                // over the MethodTable pointer, & String
                // length.  Of course, the String reference points to the memory
                // after the sync block, so don't count that.
                // This property allows C#'s fixed statement to work on Strings.
                // On 64 bit platforms, this should be 12 (8+4) and on 32 bit 8 (4+4).
#if TARGET_64BIT
                12;
#else // 32
                8;
#endif // TARGET_64BIT

        }

        // This method ensures that there is sufficient stack to execute the average Framework function.
        // If there is not enough stack, then it throws System.InsufficientExecutionStackException.
        // Note: this method is not part of the CER support, and is not to be confused with ProbeForSufficientStack
        // below.
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void EnsureSufficientExecutionStack();

        // This method ensures that there is sufficient stack to execute the average Framework function.
        // If there is not enough stack, then it return false.
        // Note: this method is not part of the CER support, and is not to be confused with ProbeForSufficientStack
        // below.
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern bool TryEnsureSufficientExecutionStack();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object GetUninitializedObjectInternal(
            // This API doesn't call any constructors, but the type needs to be seen as constructed.
            // A type is seen as constructed if a constructor is kept.
            // This obviously won't cover a type with no constructor. Reference types with no
            // constructor are an academic problem. Valuetypes with no constructors are a problem,
            // but IL Linker currently treats them as always implicitly boxed.
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object AllocateUninitializedClone(object obj);

        /// <returns>true if given type is reference type or value type that contains references</returns>
        [Intrinsic]
        public static bool IsReferenceOrContainsReferences<T>()
        {
            // The body of this function will be replaced by the EE with unsafe code!!!
            // See getILIntrinsicImplementationForRuntimeHelpers for how this happens.
            throw new InvalidOperationException();
        }

        /// <returns>true if given type is bitwise equatable (memcmp can be used for equality checking)</returns>
        /// <remarks>
        /// Only use the result of this for Equals() comparison, not for CompareTo() comparison.
        /// </remarks>
        [Intrinsic]
        internal static bool IsBitwiseEquatable<T>()
        {
            // The body of this function will be replaced by the EE with unsafe code!!!
            // See getILIntrinsicImplementationForRuntimeHelpers for how this happens.
            throw new InvalidOperationException();
        }

        [Intrinsic]
        internal static bool EnumEquals<T>(T x, T y) where T : struct, Enum
        {
            // The body of this function will be replaced by the EE with unsafe code
            // See getILIntrinsicImplementation for how this happens.
            return x.Equals(y);
        }

        [Intrinsic]
        internal static int EnumCompareTo<T>(T x, T y) where T : struct, Enum
        {
            // The body of this function will be replaced by the EE with unsafe code
            // See getILIntrinsicImplementation for how this happens.
            return x.CompareTo(y);
        }

        internal static ref byte GetRawData(this object obj) =>
            ref Unsafe.As<RawData>(obj).Data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe nuint GetRawObjectDataSize(object obj)
        {
            MethodTable* pMT = GetMethodTable(obj);

            // See comment on RawArrayData for details
            nuint rawSize = pMT->BaseSize - (nuint)(2 * sizeof(IntPtr));
            if (pMT->HasComponentSize)
                rawSize += (uint)Unsafe.As<RawArrayData>(obj).Length * (nuint)pMT->ComponentSize;

            GC.KeepAlive(obj); // Keep MethodTable alive

            return rawSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe ref byte GetRawArrayData(this Array array) =>
            // See comment on RawArrayData for details
            ref Unsafe.AddByteOffset(ref Unsafe.As<RawData>(array).Data, (nuint)GetMethodTable(array)->BaseSize - (nuint)(2 * sizeof(IntPtr)));

        internal static unsafe ushort GetElementSize(this Array array)
        {
            Debug.Assert(ObjectHasComponentSize(array));
            return GetMethodTable(array)->ComponentSize;
        }

        // Returns pointer to the multi-dimensional array bounds.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe ref int GetMultiDimensionalArrayBounds(Array array)
        {
            Debug.Assert(GetMultiDimensionalArrayRank(array) > 0);
            // See comment on RawArrayData for details
            return ref Unsafe.As<byte, int>(ref Unsafe.As<RawArrayData>(array).Data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe int GetMultiDimensionalArrayRank(Array array)
        {
            int rank = GetMethodTable(array)->MultiDimensionalArrayRank;
            GC.KeepAlive(array); // Keep MethodTable alive
            return rank;
        }

        // Returns true iff the object has a component size;
        // i.e., is variable length like System.String or Array.
        // Callers are required to keep obj alive
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool ObjectHasComponentSize(object obj)
        {
            return GetMethodTable(obj)->HasComponentSize;
        }

        // Given an object reference, returns its MethodTable*.
        //
        // WARNING: The caller has to ensure that MethodTable* does not get unloaded. The most robust way
        // to achieve this is by using GC.KeepAlive on the object that the MethodTable* was fetched from, e.g.:
        //
        // MethodTable* pMT = GetMethodTable(o);
        //
        // ... work with pMT ...
        //
        // GC.KeepAlive(o);
        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Intrinsic]
        internal static unsafe MethodTable* GetMethodTable(object obj)
        {
            // The body of this function will be replaced by the EE with unsafe code
            // See getILIntrinsicImplementationForRuntimeHelpers for how this happens.

            return (MethodTable *)Unsafe.Add(ref Unsafe.As<byte, IntPtr>(ref obj.GetRawData()), -1);
        }

        /// <summary>
        /// Allocate memory that is associated with the <paramref name="type"/> and
        /// will be freed if and when the <see cref="System.Type"/> is unloaded.
        /// </summary>
        /// <param name="type">Type associated with the allocated memory.</param>
        /// <param name="size">Amount of memory in bytes to allocate.</param>
        /// <returns>The allocated memory</returns>
        public static IntPtr AllocateTypeAssociatedMemory(Type type, int size)
        {
            RuntimeType? rt = type as RuntimeType;
            if (rt == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(type));

            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size));

            return AllocateTypeAssociatedMemoryInternal(new QCallTypeHandle(ref rt), (uint)size);
        }

        [DllImport(RuntimeHelpers.QCall)]
        private static extern IntPtr AllocateTypeAssociatedMemoryInternal(QCallTypeHandle type, uint size);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr AllocTailCallArgBuffer(int size, IntPtr gcDesc);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void FreeTailCallArgBuffer();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static unsafe extern TailCallTls* GetTailCallInfo(IntPtr retAddrSlot, IntPtr* retAddr);

        private static unsafe void DispatchTailCalls(
            IntPtr callersRetAddrSlot,
            delegate*<IntPtr, IntPtr, IntPtr*, void> callTarget,
            IntPtr retVal)
        {
            IntPtr callersRetAddr;
            TailCallTls* tls = GetTailCallInfo(callersRetAddrSlot, &callersRetAddr);
            PortableTailCallFrame* prevFrame = tls->Frame;
            if (callersRetAddr == prevFrame->TailCallAwareReturnAddress)
            {
                prevFrame->NextCall = callTarget;
                return;
            }

            PortableTailCallFrame newFrame;
            newFrame.Prev = prevFrame;

            try
            {
                tls->Frame = &newFrame;

                do
                {
                    newFrame.NextCall = null;
                    callTarget(tls->ArgBuffer, retVal, &newFrame.TailCallAwareReturnAddress);
                    callTarget = newFrame.NextCall;
                } while (callTarget != null);
            }
            finally
            {
                tls->Frame = prevFrame;
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern long GetILBytesJitted();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetMethodsJittedCount();
    }
    // Helper class to assist with unsafe pinning of arbitrary objects.
    // It's used by VM code.
    internal class RawData
    {
        public byte Data;
    }

    // CLR arrays are laid out in memory as follows (multidimensional array bounds are optional):
    // [ sync block || pMethodTable || num components || MD array bounds || array data .. ]
    //                 ^               ^                 ^                  ^ returned reference
    //                 |               |                 \-- ref Unsafe.As<RawArrayData>(array).Data
    //                 \-- array       \-- ref Unsafe.As<RawData>(array).Data
    // The BaseSize of an array includes all the fields before the array data,
    // including the sync block and method table. The reference to RawData.Data
    // points at the number of components, skipping over these two pointer-sized fields.
    internal class RawArrayData
    {
        public uint Length; // Array._numComponents padded to IntPtr
#if TARGET_64BIT
        public uint Padding;
#endif
        public byte Data;
    }

    // Subset of src\vm\methodtable.h
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct MethodTable
    {
        [FieldOffset(0)]
        public ushort ComponentSize;
        [FieldOffset(0)]
        private uint Flags;
        [FieldOffset(4)]
        public uint BaseSize;
        [FieldOffset(0x0e)]
        public ushort InterfaceCount;
        [FieldOffset(ParentMethodTableOffset)]
        public MethodTable* ParentMethodTable;
        [FieldOffset(ElementTypeOffset)]
        public void* ElementType;
        [FieldOffset(InterfaceMapOffset)]
        public MethodTable** InterfaceMap;

        // WFLAGS_HIGH_ENUM
        private const uint enum_flag_ContainsPointers = 0x01000000;
        private const uint enum_flag_HasComponentSize = 0x80000000;
        private const uint enum_flag_HasTypeEquivalence = 0x00004000;
        // Types that require non-trivial interface cast have this bit set in the category
        private const uint enum_flag_NonTrivialInterfaceCast = 0x00080000 // enum_flag_Category_Array
                                                             | 0x40000000 // enum_flag_ComObject
                                                             | 0x00400000 // enum_flag_ICastable;
                                                             | 0x00200000;// enum_flag_IDynamicInterfaceCastable;

        private const int DebugClassNamePtr = // adjust for debug_m_szClassName
#if DEBUG
#if TARGET_64BIT
            8
#else
            4
#endif
#else
            0
#endif
            ;

        private const int ParentMethodTableOffset = 0x10 + DebugClassNamePtr;

#if TARGET_64BIT
        private const int ElementTypeOffset = 0x30 + DebugClassNamePtr;
#else
        private const int ElementTypeOffset = 0x20 + DebugClassNamePtr;
#endif

#if TARGET_64BIT
        private const int InterfaceMapOffset = 0x38 + DebugClassNamePtr;
#else
        private const int InterfaceMapOffset = 0x24 + DebugClassNamePtr;
#endif

        public bool HasComponentSize
        {
            get
            {
                return (Flags & enum_flag_HasComponentSize) != 0;
            }
        }

        public bool ContainsGCPointers
        {
            get
            {
                return (Flags & enum_flag_ContainsPointers) != 0;
            }
        }

        public bool NonTrivialInterfaceCast
        {
            get
            {
                return (Flags & enum_flag_NonTrivialInterfaceCast) != 0;
            }
        }

        public bool HasTypeEquivalence
        {
            get
            {
                return (Flags & enum_flag_HasTypeEquivalence) != 0;
            }
        }

        public bool IsMultiDimensionalArray
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Debug.Assert(HasComponentSize);
                // See comment on RawArrayData for details
                return BaseSize > (uint)(3 * sizeof(IntPtr));
            }
        }

        // Returns rank of multi-dimensional array rank, 0 for sz arrays
        public int MultiDimensionalArrayRank
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Debug.Assert(HasComponentSize);
                // See comment on RawArrayData for details
                return (int)((BaseSize - (uint)(3 * sizeof(IntPtr))) / (uint)(2 * sizeof(int)));
            }
        }
    }

    // Helper structs used for tail calls via helper.
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct PortableTailCallFrame
    {
        public PortableTailCallFrame* Prev;
        public IntPtr TailCallAwareReturnAddress;
        public delegate*<IntPtr, IntPtr, IntPtr*, void> NextCall;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct TailCallTls
    {
        public PortableTailCallFrame* Frame;
        public IntPtr ArgBuffer;
        private IntPtr _argBufferSize;
        private IntPtr _argBufferGCDesc;
        private fixed byte _argBufferInline[64];
    }

}
