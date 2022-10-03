// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    internal sealed unsafe partial class _IOCompletionCallback
    {
        private readonly IOCompletionCallback _ioCompletionCallback;
        private readonly ExecutionContext _executionContext;
        private uint _errorCode; // Error code
        private uint _numBytes; // No. of bytes transferred
        private NativeOverlapped* _pNativeOverlapped;

        public _IOCompletionCallback(IOCompletionCallback ioCompletionCallback, ExecutionContext executionContext)
        {
            _ioCompletionCallback = ioCompletionCallback;
            _executionContext = executionContext;
        }

        // Context callback: same sig for SendOrPostCallback and ContextCallback
        private static readonly ContextCallback IOCompletionCallback_Context_Delegate = IOCompletionCallback_Context;
        private static void IOCompletionCallback_Context(object? state)
        {
            _IOCompletionCallback helper = (_IOCompletionCallback)state!;
            Debug.Assert(helper != null, "_IOCompletionCallback cannot be null");
            helper._ioCompletionCallback(helper._errorCode, helper._numBytes, helper._pNativeOverlapped);
        }

        public static void PerformSingleIOCompletionCallback(uint errorCode, uint numBytes, NativeOverlapped* pNativeOverlapped)
        {
            Debug.Assert(pNativeOverlapped != null);

            OverlappedData overlapped = OverlappedData.GetOverlappedFromNative(pNativeOverlapped);
            object? callback = overlapped._callback;
            if (callback is IOCompletionCallback iocb)
            {
                // We got here because of UnsafePack (or) Pack with EC flow suppressed
                iocb(errorCode, numBytes, pNativeOverlapped);
                return;
            }

            if (callback == null)
            {
                // A callback was not provided
                return;
            }

            // We got here because of Pack
            Debug.Assert(callback is _IOCompletionCallback);
            var helper = (_IOCompletionCallback)callback;
            helper._errorCode = errorCode;
            helper._numBytes = numBytes;
            helper._pNativeOverlapped = pNativeOverlapped;
            ExecutionContext.RunInternal(helper._executionContext, IOCompletionCallback_Context_Delegate, helper);
        }
    }
}
