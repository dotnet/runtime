// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

using Internal.Runtime;

namespace System.Runtime
{
    // The browser NativeAOT runtime is not implemented yet. Keep the managed exports and
    // shared Runtime.Base type surface available so System.Private.CoreLib can be built.
    internal static unsafe partial class EH
    {
        internal struct MethodRegionInfo
        {
        }

        internal struct ExInfo
        {
        }

        internal struct PAL_LIMITED_CONTEXT
        {
        }

        [RuntimeExport("RhpThrowEx")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RhpThrowEx(object exception)
        {
            FallbackFailFast(RhFailFastReason.InternalError, exception);
        }

        [RuntimeExport("RhpRethrow")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RhpRethrow(object exception)
        {
            FallbackFailFast(RhFailFastReason.InternalError, exception);
        }

        [RuntimeExport("RhpHandleExceptionWasmCatch")]
        private static object RhpHandleExceptionWasmCatch(nuint catchUnwindIndex)
        {
            _ = catchUnwindIndex;
            FallbackFailFast(RhFailFastReason.InternalError, null);
            return null!;
        }

        [RuntimeExport("RhpPopUnwoundSparseVirtualFrames")]
        private static void RhpPopUnwoundSparseVirtualFrames()
        {
        }

        [RuntimeExport("RhpHandleUnhandledException")]
        private static void RhpHandleUnhandledException(object exception)
        {
            FallbackFailFast(RhFailFastReason.UnhandledException, exception);
        }

        [RuntimeExport("RhGetCurrentThreadStackTrace")]
        private static int RhGetCurrentThreadStackTrace(IntPtr[] outputBuffer)
        {
            _ = outputBuffer;
            return 0;
        }

        [StackTraceHidden]
        [DebuggerHidden]
        [RuntimeExport("RhExceptionHandling_FailedAllocation")]
        public static void FailedAllocation(MethodTable* pEEType, bool fIsOverflow)
        {
            _ = pEEType;
            _ = fIsOverflow;
            FallbackFailFast(RhFailFastReason.InternalError, null);
        }

        internal static void FallbackFailFast(RhFailFastReason reason, object? unhandledException)
        {
            _ = reason;
            _ = unhandledException;
            InternalCalls.RhpFallbackFailFast();
        }
    }
}
