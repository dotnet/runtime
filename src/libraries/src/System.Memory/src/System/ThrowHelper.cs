// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System
{
    //
    // This pattern of easily inlinable "void Throw" routines that stack on top of NoInlining factory methods
    // is a compromise between older JITs and newer JITs (RyuJIT in Core CLR 1.1.0+ and desktop CLR in 4.6.3+).
    // This package is explictly targeted at older JITs as newer runtimes expect to implement Span intrinsically for
    // best performance.
    //
    // The aim of this pattern is three-fold
    // 1. Extracting the throw makes the method preforming the throw in a conditional branch smaller and more inlinable
    // 2. Extracting the throw from generic method to non-generic method reduces the repeated codegen size for value types
    // 3a. Newer JITs will not inline the methods that only throw and also recognise them, move the call to cold section
    //     and not add stack prep and unwind before calling https://github.com/dotnet/coreclr/pull/6103
    // 3b. Older JITs will inline the throw itself and move to cold section; but not inline the non-inlinable exception
    //     factory methods - still maintaining advantages 1 & 2
    //

    internal static class ThrowHelper
    {
        internal static void ThrowArgumentNullException(ExceptionArgument argument) { throw CreateArgumentNullException(argument); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateArgumentNullException(ExceptionArgument argument) { return new ArgumentNullException(argument.ToString()); }

        internal static void ThrowArrayTypeMismatchException_ArrayTypeMustBeExactMatch(Type type) { throw CreateArrayTypeMismatchException_ArrayTypeMustBeExactMatch(type); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateArrayTypeMismatchException_ArrayTypeMustBeExactMatch(Type type) { return new ArrayTypeMismatchException(SR.Format(SR.ArrayTypeMustBeExactMatch, type)); }

        internal static void ThrowArgumentException_InvalidTypeWithPointersNotSupported(Type type) { throw CreateArgumentException_InvalidTypeWithPointersNotSupported(type); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateArgumentException_InvalidTypeWithPointersNotSupported(Type type) { return new ArgumentException(SR.Format(SR.Argument_InvalidTypeWithPointersNotSupported, type)); }

        internal static void ThrowArgumentException_DestinationTooShort() { throw CreateArgumentException_DestinationTooShort(); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateArgumentException_DestinationTooShort() { return new ArgumentException(SR.Argument_DestinationTooShort); }

        internal static void ThrowIndexOutOfRangeException() { throw CreateIndexOutOfRangeException(); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateIndexOutOfRangeException() { return new IndexOutOfRangeException(); }

        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument) { throw CreateArgumentOutOfRangeException(argument); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateArgumentOutOfRangeException(ExceptionArgument argument) { return new ArgumentOutOfRangeException(argument.ToString()); }
    }

    internal enum ExceptionArgument
    {
        array,
        length,
        start,
        text,
        obj,
    }
}
