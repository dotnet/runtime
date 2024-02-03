// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    //
    // Subsetted clone of System.Runtime.CompilerServices.Unsafe for internal runtime use.
    // Keep in sync with https://github.com/dotnet/corefx/tree/master/src/System.Runtime.CompilerServices.Unsafe.
    //

    /// <summary>
    /// Contains generic, low-level functionality for manipulating pointers.
    /// </summary>
    public static unsafe class Unsafe
    {
        /// <summary>
        /// Returns a pointer to the given by-ref parameter.
        /// </summary>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AsPointer<T>(ref T value)
        {
            throw new PlatformNotSupportedException();

            // ldarg.0
            // conv.u
            // ret
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>()
        {
            // This method is implemented by the toolchain
            throw new PlatformNotSupportedException();

            // sizeof !!0
            // ret
        }

        /// <summary>
        /// Casts the given object to the specified type, performs no dynamic type checking.
        /// </summary>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T As<T>(object value) where T : class
        {
            // This method is implemented by the toolchain
            throw new PlatformNotSupportedException();

            // ldarg.0
            // ret
        }

        /// <summary>
        /// Reinterprets the given reference as a reference to a value of type <typeparamref name="TTo"/>.
        /// </summary>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TTo As<TFrom, TTo>(ref TFrom source)
        {
            // This method is implemented by the toolchain
            throw new PlatformNotSupportedException();

            // ldarg.0
            // ret
        }

        [Intrinsic]
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
        /// Adds an element offset to the given reference.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Add<T>(ref T source, int elementOffset)
        {
            return ref AddByteOffset(ref source, (IntPtr)(elementOffset * (nint)SizeOf<T>()));
        }

        /// <summary>
        /// Adds an element offset to the given reference.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Add<T>(ref T source, IntPtr elementOffset)
        {
            return ref AddByteOffset(ref source, (IntPtr)((nint)elementOffset * (nint)SizeOf<T>()));
        }

        /// <summary>
        /// Reinterprets the given location as a reference to a value of type <typeparamref name="T"/>.
        /// </summary>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsRef<T>(in T source)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> from the given location.
        /// </summary>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadUnaligned<T>(void* source)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
