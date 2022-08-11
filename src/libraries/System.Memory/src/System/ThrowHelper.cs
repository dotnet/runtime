// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System
{
    //
    // This pattern of easily inlinable "void Throw" routines that stack on top of NoInlining factory methods
    // is a compromise between older JITs and newer JITs (RyuJIT in .NET Core 1.1.0+ and .NET Framework in 4.6.3+).
    // This package is explicitly targeted at older JITs as newer runtimes expect to implement Span intrinsically for
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
        [DoesNotReturn]
        internal static void ThrowArgumentNullException(ExceptionArgument argument) { throw CreateArgumentNullException(argument); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateArgumentNullException(ExceptionArgument argument) { return new ArgumentNullException(argument.ToString()); }

        [DoesNotReturn]
        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument) { throw CreateArgumentOutOfRangeException(argument); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateArgumentOutOfRangeException(ExceptionArgument argument) { return new ArgumentOutOfRangeException(argument.ToString()); }

        [DoesNotReturn]
        internal static void ThrowInvalidOperationException() { throw CreateInvalidOperationException(); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateInvalidOperationException() { return new InvalidOperationException(); }

        [DoesNotReturn]
        internal static void ThrowInvalidOperationException_EndPositionNotReached() { throw CreateInvalidOperationException_EndPositionNotReached(); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateInvalidOperationException_EndPositionNotReached() { return new InvalidOperationException(SR.EndPositionNotReached); }

        [DoesNotReturn]
        internal static void ThrowArgumentOutOfRangeException_PositionOutOfRange() { throw CreateArgumentOutOfRangeException_PositionOutOfRange(); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateArgumentOutOfRangeException_PositionOutOfRange() { return new ArgumentOutOfRangeException("position"); }

        [DoesNotReturn]
        internal static void ThrowArgumentOutOfRangeException_OffsetOutOfRange() { throw CreateArgumentOutOfRangeException_OffsetOutOfRange(); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateArgumentOutOfRangeException_OffsetOutOfRange() { return new ArgumentOutOfRangeException(nameof(ExceptionArgument.offset)); }

        //
        // ReadOnlySequence .ctor validation Throws coalesced to enable inlining of the .ctor
        //
        [DoesNotReturn]
        public static void ThrowArgumentValidationException<T>(ReadOnlySequenceSegment<T>? startSegment, int startIndex, ReadOnlySequenceSegment<T>? endSegment)
            => throw CreateArgumentValidationException(startSegment, startIndex, endSegment);

        private static Exception CreateArgumentValidationException<T>(ReadOnlySequenceSegment<T>? startSegment, int startIndex, ReadOnlySequenceSegment<T>? endSegment)
        {
            if (startSegment == null)
                return CreateArgumentNullException(ExceptionArgument.startSegment);
            else if (endSegment == null)
                return CreateArgumentNullException(ExceptionArgument.endSegment);
            else if (startSegment != endSegment && startSegment.RunningIndex > endSegment.RunningIndex)
                return CreateArgumentOutOfRangeException(ExceptionArgument.endSegment);
            else if ((uint)startSegment.Memory.Length < (uint)startIndex)
                return CreateArgumentOutOfRangeException(ExceptionArgument.startIndex);
            else
                return CreateArgumentOutOfRangeException(ExceptionArgument.endIndex);
        }

        [DoesNotReturn]
        public static void ThrowArgumentValidationException(Array? array, int start)
            => throw CreateArgumentValidationException(array, start);

        private static Exception CreateArgumentValidationException(Array? array, int start)
        {
            if (array == null)
                return CreateArgumentNullException(ExceptionArgument.array);
            else if ((uint)start > (uint)array.Length)
                return CreateArgumentOutOfRangeException(ExceptionArgument.start);
            else
                return CreateArgumentOutOfRangeException(ExceptionArgument.length);
        }

        //
        // ReadOnlySequence Slice validation Throws coalesced to enable inlining of the Slice
        //
        [DoesNotReturn]
        public static void ThrowStartOrEndArgumentValidationException(long start)
            => throw CreateStartOrEndArgumentValidationException(start);

        private static Exception CreateStartOrEndArgumentValidationException(long start)
        {
            if (start < 0)
                return CreateArgumentOutOfRangeException(ExceptionArgument.start);
            return CreateArgumentOutOfRangeException(ExceptionArgument.length);
        }

    }

    //
    // The convention for this enum is using the argument name as the enum name
    //
    internal enum ExceptionArgument
    {
        length,
        start,
        minimumBufferSize,
        elementIndex,
        comparable,
        comparer,
        destination,
        offset,
        startSegment,
        endSegment,
        startIndex,
        endIndex,
        array,
        culture,
        manager,
        count,
        writer,
    }
}
