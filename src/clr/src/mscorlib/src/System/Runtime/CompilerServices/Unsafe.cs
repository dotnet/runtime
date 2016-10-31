// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Versioning;

namespace System.Runtime.CompilerServices
{
    //
    // Subsetted clone of System.Runtime.CompilerServices.Unsafe for internal runtime use.
    // Keep in sync with https://github.com/dotnet/corefx/tree/master/src/System.Runtime.CompilerServices.Unsafe.
    // 

    /// <summary>
    /// Contains generic, low-level functionality for manipulating pointers.
    /// </summary>
    internal static unsafe class Unsafe
    {
        /// <summary>
        /// Returns a pointer to the given by-ref parameter.
        /// </summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AsPointer<T>(ref T value)
        {
            // The body of this function will be replaced by the EE with unsafe code that just returns sizeof !!T
            // See getILIntrinsicImplementationForUnsafe for how this happens.  
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Returns the size of an object of the given type parameter.
        /// </summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>()
        {
            // The body of this function will be replaced by the EE with unsafe code that just returns sizeof !!T
            // See getILIntrinsicImplementationForUnsafe for how this happens.  
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Reinterprets the given reference as a reference to a value of type <typeparamref name="TTo"/>.
        /// </summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TTo As<TFrom, TTo>(ref TFrom source)
        {
            // The body of this function will be replaced by the EE with unsafe code that just returns sizeof !!T
            // See getILIntrinsicImplementationForUnsafe for how this happens.  
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Adds an element offset to the given reference.
        /// </summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Add<T>(ref T source, int elementOffset)
        {
            // The body of this function will be replaced by the EE with unsafe code!!!
            // See getILIntrinsicImplementationForUnsafe for how this happens.
            typeof(T).ToString(); // Type used by the actual method body
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Determines whether the specified references point to the same location.
        /// </summary>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreSame<T>(ref T left, ref T right)
        {
            // The body of this function will be replaced by the EE with unsafe code!!!
            // See getILIntrinsicImplementationForUnsafe for how this happens.  
            throw new InvalidOperationException();
        }
    }
}
