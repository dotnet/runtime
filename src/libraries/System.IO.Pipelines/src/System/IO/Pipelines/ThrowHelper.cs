// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.IO.Pipelines
{
    internal class ThrowHelper
    {
        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument) { throw CreateArgumentOutOfRangeException(argument); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateArgumentOutOfRangeException(ExceptionArgument argument) { return new ArgumentOutOfRangeException(argument.ToString()); }

        public static void ThrowInvalidOperationException_NotWritingNoAlloc()
        {
            throw CreateInvalidOperationException_NotWritingNoAlloc();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_NotWritingNoAlloc()
        {
            return new InvalidOperationException("No writing operation. Make sure GetMemory() was called.");
        }

        public static void ThrowInvalidOperationException_AlreadyReading()
        {
            throw CreateInvalidOperationException_AlreadyReading();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_AlreadyReading()
        {
            return new InvalidOperationException("Already reading.");
        }

        public static void ThrowInvalidOperationException_NoReadToComplete()
        {
            throw CreateInvalidOperationException_NoReadToComplete();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_NoReadToComplete()
        {
            return new InvalidOperationException("No reading operation to complete.");
        }

        public static void ThrowInvalidOperationException_NoConcurrentOperation()
        {
            throw CreateInvalidOperationException_NoConcurrentOperation();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_NoConcurrentOperation()
        {
            return new InvalidOperationException("Concurrent reads or writes are not supported.");
        }

        public static void ThrowInvalidOperationException_GetResultNotCompleted()
        {
            throw CreateInvalidOperationException_GetResultNotCompleted();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_GetResultNotCompleted()
        {
            return new InvalidOperationException("Can't GetResult unless completed");
        }

        public static void ThrowInvalidOperationException_NoWritingAllowed()
        {
            throw CreateInvalidOperationException_NoWritingAllowed();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_NoWritingAllowed()
        {
            return new InvalidOperationException("Writing is not allowed after writer was completed");
        }

        public static void ThrowInvalidOperationException_NoReadingAllowed()
        {
            throw CreateInvalidOperationException_NoReadingAllowed();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]

        public static Exception CreateInvalidOperationException_NoReadingAllowed()
        {
            return new InvalidOperationException("Reading is not allowed after reader was completed");
        }

        public static void ThrowInvalidOperationException_CompleteWriterActiveWriter()
        {
            throw CreateInvalidOperationException_CompleteWriterActiveWriter();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_CompleteWriterActiveWriter()
        {
            return new InvalidOperationException("Can't complete writer while writing.");
        }

        public static void ThrowInvalidOperationException_CompleteReaderActiveReader()
        {
            throw CreateInvalidOperationException_CompleteReaderActiveReader();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_CompleteReaderActiveReader()
        {
            return new InvalidOperationException("Can't complete reader while reading.");
        }

        public static void ThrowInvalidOperationException_AdvancingPastBufferSize()
        {
            throw CreateInvalidOperationException_AdvancingPastBufferSize();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_AdvancingPastBufferSize()
        {
            return new InvalidOperationException("Can't advance past buffer size");
        }

        public static void ThrowInvalidOperationException_BackpressureDeadlock()
        {
            throw CreateInvalidOperationException_BackpressureDeadlock();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_BackpressureDeadlock()
        {
            return new InvalidOperationException("Advancing examined to the end would cause pipe to deadlock because FlushAsync is waiting");
        }

        public static void ThrowInvalidOperationException_AdvanceToInvalidCursor()
        {
            throw CreateInvalidOperationException_AdvanceToInvalidCursor();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_AdvanceToInvalidCursor()
        {
            return new InvalidOperationException("Pipe is already advanced past provided cursor");
        }
    }

    internal enum ExceptionArgument
    {
        minimumSize,
        bytesWritten
    }
}
