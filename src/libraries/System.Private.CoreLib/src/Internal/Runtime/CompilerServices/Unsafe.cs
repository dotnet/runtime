// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable IDE0060 // implementations provided by the JIT
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

//
// The implementations of most the methods in this file are provided as intrinsics.
// In CoreCLR, the body of the functions are replaced by the EE with unsafe code. See see getILIntrinsicImplementationForUnsafe for details.
// In CoreRT, see Internal.IL.Stubs.UnsafeIntrinsics for details.
//

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Contains generic, low-level functionality for manipulating pointers.
    /// </summary>
    [CLSCompliant(false)]
    public static unsafe partial class Unsafe
    {
        /// <summary>
        /// Returns a pointer to the given by-ref parameter.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__AS_POINTER
        // CG2:AsPointer
        // Mono:AsPointer
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AsPointer<T>(ref T value)
        {
            throw new PlatformNotSupportedException();

            // ldarg.0
            // conv.u
            // ret
        }

        /// <summary>
        /// Returns the size of an object of the given type parameter.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__SIZEOF
        // CG2:SizeOf
        // Mono:SizeOf
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>()
        {
#if CORECLR
            typeof(T).ToString(); // Type token used by the actual method body
#endif
            throw new PlatformNotSupportedException();

            // sizeof !!0
            // ret
        }

        /// <summary>
        /// Casts the given object to the specified type, performs no dynamic type checking.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__OBJECT_AS
        // CG2:As
        // Mono:As
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNullIfNotNull("value")]
        public static T As<T>(object? value) where T : class?
        {
            throw new PlatformNotSupportedException();

            // ldarg.0
            // ret
        }

        /// <summary>
        /// Reinterprets the given reference as a reference to a value of type <typeparamref name="TTo"/>.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__BYREF_AS
        // CG2:As
        // Mono:As
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TTo As<TFrom, TTo>(ref TFrom source)
        {
            throw new PlatformNotSupportedException();

            // ldarg.0
            // ret
        }

        /// <summary>
        /// Adds an element offset to the given reference.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__BYREF_ADD
        // CG2:Add
        // Mono:Add
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Add<T>(ref T source, int elementOffset)
        {
#if CORECLR
            typeof(T).ToString(); // Type token used by the actual method body
            throw new PlatformNotSupportedException();
#else
            return ref AddByteOffset(ref source, (IntPtr)(elementOffset * (nint)SizeOf<T>()));
#endif

            // ldarg .0
            // ldarg .1
            // sizeof T
            // conv.i
            // mul
            // add
            // ret
        }

        /// <summary>
        /// Adds an element offset to the given reference.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__BYREF_INTPTR_ADD
        // CG2:Add
        // Mono:Add
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Add<T>(ref T source, IntPtr elementOffset)
        {
#if CORECLR
            typeof(T).ToString(); // Type token used by the actual method body
            throw new PlatformNotSupportedException();
#else
            return ref AddByteOffset(ref source, (IntPtr)((nint)elementOffset * (nint)SizeOf<T>()));
#endif

            // ldarg .0
            // ldarg .1
            // sizeof T
            // mul
            // add
            // ret
        }

        /// <summary>
        /// Adds an element offset to the given pointer.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__PTR_ADD
        // CG2:Add
        // Mono:Add
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* Add<T>(void* source, int elementOffset)
        {
#if CORECLR
            typeof(T).ToString(); // Type token used by the actual method body
            throw new PlatformNotSupportedException();
#else
            return (byte*)source + (elementOffset * (nint)SizeOf<T>());
#endif

            // ldarg .0
            // ldarg .1
            // sizeof T
            // conv.i
            // mul
            // add
            // ret
        }

        /// <summary>
        /// Adds an element offset to the given reference.
        /// </summary>
        [Intrinsic]
        // CoreCLR:
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Add<T>(ref T source, UIntPtr elementOffset)
        {
#if CORECLR
            typeof(T).ToString();
            throw new PlatformNotSupportedException();
#else
            return ref AddByteOffset(ref source, (nuint)(elementOffset * (nuint)SizeOf<T>()));
#endif

            // ldarg .0
            // ldarg .1
            // sizeof T
            // mul
            // add
            // ret
        }

        /// <summary>
        /// Adds an byte offset to the given reference.
        /// </summary>
        [Intrinsic]
        // CG2:AddByteOffset
        // Mono:AddByteOffset
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ref T AddByteOffset<T>(ref T source, nuint byteOffset)
        {
            return ref AddByteOffset(ref source, (IntPtr)(void*)byteOffset);

            // ldarg .0
            // ldarg .1
            // add
            // ret
        }

        /// <summary>
        /// Determines whether the specified references point to the same location.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__BYREF_ARE_SAME
        // CG2:AreSame
        // Mono:AreSame
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreSame<T>([AllowNull] ref T left, [AllowNull] ref T right)
        {
            throw new PlatformNotSupportedException();

            // ldarg.0
            // ldarg.1
            // ceq
            // ret
        }

        /// <summary>
        /// Copies a value of type T to the given location.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__PTR_BYREF_COPY
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(void* destination, ref T source)
        {
            throw new PlatformNotSupportedException();

            // ldarg .0
            // ldarg .1
            // ldobj !!T
            // stobj !!T
            // ret
        }

        /// <summary>
        /// Copies a value of type T to the given location.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__BYREF_PTR_COPY
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy<T>(ref T destination, void* source)
        {
            throw new PlatformNotSupportedException();

            // ldarg .0
            // ldarg .1
            // ldobj !!T
            // stobj !!T
            // ret
        }

        /// <summary>
        /// Copies bytes from the source address to the destination address.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__PTR_COPY_BLOCK
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyBlock(void* destination, void* source, uint byteCount)
        {
            throw new PlatformNotSupportedException();

            // ldarg .0
            // ldarg .1
            // ldarg .2
            // cpblk
            // ret
        }

        /// <summary>
        /// Copies bytes from the source address to the destination address.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__BYREF_COPY_BLOCK
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyBlock(ref byte destination, ref byte source, uint byteCount)
        {
            throw new PlatformNotSupportedException();

            // ldarg .0
            // ldarg .1
            // ldarg .2
            // cpblk
            // ret
        }

        /// <summary>
        /// Copies bytes from the source address to the destination address without assuming architecture dependent alignment of the addresses.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__PTR_COPY_BLOCK_UNALIGNED
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyBlockUnaligned(void* destination, void* source, uint byteCount)
        {
            throw new PlatformNotSupportedException();

            // ldarg .0
            // ldarg .1
            // ldarg .2
            // unaligned. 0x1
            // cpblk
            // ret
        }

        /// <summary>
        /// Copies bytes from the source address to the destination address without assuming architecture dependent alignment of the addresses.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__BYREF_COPY_BLOCK_UNALIGNED
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyBlockUnaligned(ref byte destination, ref byte source, uint byteCount)
        {
            throw new PlatformNotSupportedException();

            // ldarg .0
            // ldarg .1
            // ldarg .2
            // unaligned. 0x1
            // cpblk
            // ret
        }

        /// <summary>
        /// Determines whether the memory address referenced by <paramref name="left"/> is greater than
        /// the memory address referenced by <paramref name="right"/>.
        /// </summary>
        /// <remarks>
        /// This check is conceptually similar to "(void*)(&amp;left) &gt; (void*)(&amp;right)".
        /// </remarks>
        [Intrinsic]
        // CoreCLR:CoreCLR:METHOD__UNSAFE__BYREF_IS_ADDRESS_GREATER_THAN
        // CG2:IsAddressGreaterThan
        // Mono:IsAddressGreaterThan
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAddressGreaterThan<T>([AllowNull] ref T left, [AllowNull] ref T right)
        {
            throw new PlatformNotSupportedException();

            // ldarg.0
            // ldarg.1
            // cgt.un
            // ret
        }

        /// <summary>
        /// Determines whether the memory address referenced by <paramref name="left"/> is less than
        /// the memory address referenced by <paramref name="right"/>.
        /// </summary>
        /// <remarks>
        /// This check is conceptually similar to "(void*)(&amp;left) &lt; (void*)(&amp;right)".
        /// </remarks>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__BYREF_IS_ADDRESS_LESS_THAN
        // CG2:IsAddressLessThan
        // Mono:IsAddressLessThan
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAddressLessThan<T>([AllowNull] ref T left, [AllowNull] ref T right)
        {
            throw new PlatformNotSupportedException();

            // ldarg.0
            // ldarg.1
            // clt.un
            // ret
        }

        /// <summary>
        /// Initializes a block of memory at the given location with a given initial value.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__PTR_INIT_BLOCK
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitBlock(void* startAddress, byte value, uint byteCount)
        {
            throw new PlatformNotSupportedException();

            // ldarg .0
            // ldarg .1
            // ldarg .2
            // initblk
            // ret
        }

        /// <summary>
        /// Initializes a block of memory at the given location with a given initial value.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__BYREF_INIT_BLOCK
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitBlock(ref byte startAddress, byte value, uint byteCount)
        {
            throw new PlatformNotSupportedException();

            // ldarg .0
            // ldarg .1
            // ldarg .2
            // initblk
            // ret
        }

        /// <summary>
        /// Initializes a block of memory at the given location with a given initial value
        /// without assuming architecture dependent alignment of the address.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__PTR_INIT_BLOCK_UNALIGNED
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitBlockUnaligned(void* startAddress, byte value, uint byteCount)
        {
            throw new PlatformNotSupportedException();

            // ldarg .0
            // ldarg .1
            // ldarg .2
            // unaligned. 0x1
            // initblk
            // ret
        }

        /// <summary>
        /// Initializes a block of memory at the given location with a given initial value
        /// without assuming architecture dependent alignment of the address.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__BYREF_INIT_BLOCK_UNALIGNED
        // CG2:InitBlockUnaligned
        // Mono:InitBlockUnaligned
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitBlockUnaligned(ref byte startAddress, byte value, uint byteCount)
        {
            for (uint i = 0; i < byteCount; i++)
            {
                AddByteOffset(ref startAddress, i) = value;
            }

            // ldarg .0
            // ldarg .1
            // ldarg .2
            // unaligned. 0x1
            // initblk
            // ret
        }

        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> from the given location.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__PTR_READ_UNALIGNED
        // CG2:ReadUnaligned
        // Mono:ReadUnaligned
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadUnaligned<T>(void* source)
        {
#if CORECLR
            typeof(T).ToString(); // Type token used by the actual method body
            throw new PlatformNotSupportedException();
#else
            return Unsafe.As<byte, T>(ref *(byte*)source);
#endif

            // ldarg.0
            // unaligned. 0x1
            // ldobj T
            // ret
        }

        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> from the given location.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__BYREF_READ_UNALIGNED
        // CG2:ReadUnaligned
        // Mono:ReadUnaligned
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadUnaligned<T>(ref byte source)
        {
#if CORECLR
            typeof(T).ToString(); // Type token used by the actual method body
            throw new PlatformNotSupportedException();
#else
            return Unsafe.As<byte, T>(ref source);
#endif

            // ldarg.0
            // unaligned. 0x1
            // ldobj!!T
            // ret
        }

        /// <summary>
        /// Writes a value of type <typeparamref name="T"/> to the given location.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__PTR_WRITE_UNALIGNED
        // CG2:WriteUnaligned
        // Mono:WriteUnaligned
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUnaligned<T>(void* destination, T value)
        {
#if CORECLR
            typeof(T).ToString(); // Type token used by the actual method body
            throw new PlatformNotSupportedException();
#else
            Unsafe.As<byte, T>(ref *(byte*)destination) = value;
#endif

            // ldarg .0
            // ldarg .1
            // unaligned. 0x01
            // stobjT
            // ret
        }

        /// <summary>
        /// Writes a value of type <typeparamref name="T"/> to the given location.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__BYREF_WRITE_UNALIGNED
        // CG2:WriteUnaligned
        // Mono:WriteUnaligned
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUnaligned<T>(ref byte destination, T value)
        {
#if CORECLR
            typeof(T).ToString(); // Type token used by the actual method body
            throw new PlatformNotSupportedException();
#else
            Unsafe.As<byte, T>(ref destination) = value;
#endif

            // ldarg .0
            // ldarg .1
            // unaligned. 0x01
            // stobjT
            // ret
        }

        /// <summary>
        /// Adds an byte offset to the given reference.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__BYREF_ADD_BYTE_OFFSET
        // CG2:AddByteOffset
        // Mono:AddByteOffset
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddByteOffset<T>(ref T source, IntPtr byteOffset)
        {
            // This method is implemented by the toolchain
            throw new PlatformNotSupportedException();

            // ldarg.0
            // ldarg.1
            // add
            // ret
        }

        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> from the given location.
        /// </summary>
        //[Intrinsic]
        // CG2:Read
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(void* source)
        {
            return Unsafe.As<byte, T>(ref *(byte*)source);

            // ldarg.0
            // ldobj T
            // ret
        }

        /// <summary>
        /// Writes a value of type <typeparamref name="T"/> to the given location.
        /// </summary>
        //[Intrinsic]
        // CG2:Write
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(void* destination, T value)
        {
            Unsafe.As<byte, T>(ref *(byte*)destination) = value;

            // ldarg .0
            // ldarg .1
            // stobjT
            // ret
        }

        /// <summary>
        /// Reinterprets the given location as a reference to a value of type <typeparamref name="T"/>.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__AS_REF_POINTER
        // CG2:AsRef
        // Mono:AsRef
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsRef<T>(void* source)
        {
            return ref Unsafe.As<byte, T>(ref *(byte*)source);

            // ldarg .0
            // ret
        }

        /// <summary>
        /// Reinterprets the given location as a reference to a value of type <typeparamref name="T"/>.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__AS_REF_IN
        // CG2:AsRef
        // Mono:AsRef
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsRef<T>(in T source)
        {
            throw new PlatformNotSupportedException();

            //ldarg .0
            //ret
        }

        /// <summary>
        /// Determines the byte offset from origin to target from the given references.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__BYREF_BYTE_OFFSET
        // CG2:ByteOffset
        // Mono:ByteOffset
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr ByteOffset<T>([AllowNull] ref T origin, [AllowNull] ref T target)
        {
            throw new PlatformNotSupportedException();

            // ldarg .1
            // ldarg .0
            // sub
            // ret
        }

        /// <summary>
        /// Returns a by-ref to type <typeparamref name="T"/> that is a null reference.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__BYREF_NULLREF
        // CG2:NullRef
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T NullRef<T>()
        {
            return ref Unsafe.AsRef<T>(null);

            // ldc.i4.0
            // conv.u
            // ret
        }

        /// <summary>
        /// Returns if a given by-ref to type <typeparamref name="T"/> is a null reference.
        /// </summary>
        /// <remarks>
        /// This check is conceptually similar to "(void*)(&amp;source) == nullptr".
        /// </remarks>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__BYREF_IS_NULL
        // CG2: IsNullRef
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullRef<T>(ref T source)
        {
            return Unsafe.AsPointer(ref source) == null;

            // ldarg.0
            // ldc.i4.0
            // conv.u
            // ceq
            // ret
        }

        /// <summary>
        /// Bypasses definite assignment rules by taking advantage of <c>out</c> semantics.
        /// </summary>
        [Intrinsic]
        // CoreCLR:METHOD__UNSAFE__SKIPINIT
        // CG2:SkipInit
        // Mono:SkipInit
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SkipInit<T>(out T value)
        {
            throw new PlatformNotSupportedException();

            // ret
        }

        /// <summary>
        /// Returns a mutable ref to a boxed value
        /// </summary>
        //[Intrinsic]
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Unbox<T>(object box)
            where T : struct
        {
            throw new PlatformNotSupportedException();

            // ldarg .0
            // unboxT
            // ret
        }
    }
}
