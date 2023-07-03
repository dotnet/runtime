// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
        private const uint EXCEPTION_NONCONTINUABLE = 0x1;
        private const uint FAIL_FAST_GENERATE_EXCEPTION_ADDRESS = 0x1;
        private const uint STATUS_STACK_BUFFER_OVERRUN = 0xC0000409;
        private const uint FAST_FAIL_EXCEPTION_DOTNET_AOT = 0x48;

        //
        // NativeAOT wrapper for calling RaiseFailFastException
        //

        [DoesNotReturn]
        internal static unsafe void RaiseFailFastException(int errorCode, IntPtr pExAddress, IntPtr pExContext, IntPtr pTriageBuffer, int cbTriageBuffer)
        {
            EXCEPTION_RECORD exceptionRecord;
            // STATUS_STACK_BUFFER_OVERRUN is a "transport" exception code required by Watson to trigger the proper analyzer/provider for bucketing
            exceptionRecord.ExceptionCode = STATUS_STACK_BUFFER_OVERRUN;
            exceptionRecord.ExceptionFlags = EXCEPTION_NONCONTINUABLE;
            exceptionRecord.ExceptionRecord = IntPtr.Zero;
            exceptionRecord.ExceptionAddress = pExAddress;
            exceptionRecord.NumberParameters = 4;
            exceptionRecord.ExceptionInformation[0] = FAST_FAIL_EXCEPTION_DOTNET_AOT;
            exceptionRecord.ExceptionInformation[1] = (uint)errorCode;
#if TARGET_64BIT
            exceptionRecord.ExceptionInformation[2] = (ulong)pTriageBuffer;
#else
            exceptionRecord.ExceptionInformation[2] = (uint)pTriageBuffer;
#endif
            exceptionRecord.ExceptionInformation[3] = (uint)cbTriageBuffer;

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
