// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.IO.Pipelines
{
    internal static class ThrowHelper
    {
        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument) => throw CreateArgumentOutOfRangeException(argument);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateArgumentOutOfRangeException(ExceptionArgument argument) => new ArgumentOutOfRangeException(argument.ToString());

        internal static void ThrowArgumentNullException(ExceptionArgument argument) => throw CreateArgumentNullException(argument);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateArgumentNullException(ExceptionArgument argument) => new ArgumentNullException(argument.ToString());

        public static void ThrowInvalidOperationException_NotWritingNoAlloc() { throw CreateInvalidOperationException_NotWritingNoAlloc(); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_NotWritingNoAlloc() => new InvalidOperationException(SR.NoWritingOperation);

        public static void ThrowInvalidOperationException_AlreadyReading() => throw CreateInvalidOperationException_AlreadyReading();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_AlreadyReading() => new InvalidOperationException(SR.ReadingIsInProgress);

        public static void ThrowInvalidOperationException_NoReadToComplete() => throw CreateInvalidOperationException_NoReadToComplete();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_NoReadToComplete() => new InvalidOperationException(SR.NoReadingOperationToComplete);

        public static void ThrowInvalidOperationException_NoConcurrentOperation() => throw CreateInvalidOperationException_NoConcurrentOperation();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_NoConcurrentOperation() => new InvalidOperationException(SR.ConcurrentOperationsNotSupported);

        public static void ThrowInvalidOperationException_GetResultNotCompleted() => throw CreateInvalidOperationException_GetResultNotCompleted();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_GetResultNotCompleted() => new InvalidOperationException(SR.GetResultBeforeCompleted);

        public static void ThrowInvalidOperationException_NoWritingAllowed() => throw CreateInvalidOperationException_NoWritingAllowed();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_NoWritingAllowed() => new InvalidOperationException(SR.WritingAfterCompleted);

        public static void ThrowInvalidOperationException_NoReadingAllowed() => throw CreateInvalidOperationException_NoReadingAllowed();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_NoReadingAllowed() => new InvalidOperationException(SR.ReadingAfterCompleted);

        public static void ThrowInvalidOperationException_AdvancingPastBufferSize() => throw CreateInvalidOperationException_AdvancingPastBufferSize();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_AdvancingPastBufferSize() => new InvalidOperationException(SR.CannotAdvancePastCurrentBufferSize);

        public static void ThrowInvalidOperationException_BackpressureDeadlock() => throw CreateInvalidOperationException_BackpressureDeadlock();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_BackpressureDeadlock() => new InvalidOperationException(SR.BackpressureDeadlock);

        public static void ThrowInvalidOperationException_AdvanceToInvalidCursor() => throw CreateInvalidOperationException_AdvanceToInvalidCursor();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_AdvanceToInvalidCursor() => new InvalidOperationException(SR.AdvanceToInvalidCursor);

        public static void ThrowInvalidOperationException_ResetIncompleteReaderWriter() => throw CreateInvalidOperationException_ResetIncompleteReaderWriter();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception CreateInvalidOperationException_ResetIncompleteReaderWriter() => new InvalidOperationException(SR.ReaderAndWriterHasToBeCompleted);
    }

    internal enum ExceptionArgument
    {
        minimumSize,
        bytesWritten,
        callback,
        options,
        pauseWriterThreshold,
        resumeWriterThreshold
    }
}
