// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal partial class Interop
{
#pragma warning disable 649
    internal unsafe struct EXCEPTION_RECORD
    {
        internal uint ExceptionCode;
        internal uint ExceptionFlags;
        internal IntPtr ExceptionRecord;
        internal IntPtr ExceptionAddress;
        internal uint NumberParameters;
#if TARGET_64BIT
        internal fixed ulong ExceptionInformation[15];
#else
        internal fixed uint ExceptionInformation[15];
#endif
    }
#pragma warning restore 649

    internal partial class Kernel32
    {
        internal const uint EXCEPTION_NONCONTINUABLE = 0x1;

        internal const uint FAIL_FAST_GENERATE_EXCEPTION_ADDRESS = 0x1;

        //
        // Wrapper for calling RaiseFailFastException
        //
        [DoesNotReturn]
        internal static unsafe void RaiseFailFastException(uint faultCode, IntPtr pExAddress, IntPtr pExContext)
        {
            EXCEPTION_RECORD exceptionRecord;
            exceptionRecord.ExceptionCode = faultCode;
            exceptionRecord.ExceptionFlags = EXCEPTION_NONCONTINUABLE;
            exceptionRecord.ExceptionRecord = IntPtr.Zero;
            exceptionRecord.ExceptionAddress = pExAddress;
            exceptionRecord.NumberParameters = 0;
            // don't care about exceptionRecord.ExceptionInformation as we set exceptionRecord.NumberParameters to zero

            RaiseFailFastException(
                &exceptionRecord,
                pExContext,
                pExAddress == IntPtr.Zero ? FAIL_FAST_GENERATE_EXCEPTION_ADDRESS : 0);
        }

        [LibraryImport(Libraries.Kernel32, EntryPoint = "RaiseFailFastException")]
        [DoesNotReturn]
        private static unsafe partial void RaiseFailFastException(
            EXCEPTION_RECORD* pExceptionRecord,
            IntPtr pContextRecord,
            uint dwFlags);
    }
}
