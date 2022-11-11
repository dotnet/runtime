// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace System.Runtime.InteropServices.ObjectiveC
{
    public static unsafe partial class ObjectiveCMarshal
    {
        private static readonly IntPtr[] s_ObjcMessageSendFunctions = new IntPtr[(int)MessageSendFunction.MsgSendSuperStret + 1];

        [ThreadStatic]
        private static Exception? t_pendingExceptionObject;

        /// <summary>
        /// Sets a pending exception to be thrown the next time the runtime is entered from an Objective-C msgSend P/Invoke.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <remarks>
        /// If <c>null</c> is supplied any pending exception is discarded.
        /// </remarks>
        public static void SetMessageSendPendingException(Exception? exception)
        {
            t_pendingExceptionObject = exception;
        }

        private static bool TrySetGlobalMessageSendCallback(
            MessageSendFunction msgSendFunction,
            IntPtr func)
        {
            return Interlocked.CompareExchange(ref s_ObjcMessageSendFunctions[(int)msgSendFunction], func, IntPtr.Zero) == IntPtr.Zero;
        }

        internal static bool TryGetGlobalMessageSendCallback(int msgSendFunction, out IntPtr func)
        {
            func = s_ObjcMessageSendFunctions[msgSendFunction];
            return func != IntPtr.Zero;
        }

        [StackTraceHidden]
        internal static void ThrowPendingExceptionObject()
        {
            Exception? ex = t_pendingExceptionObject;
            if (ex != null)
            {
                t_pendingExceptionObject = null;
                ExceptionDispatchInfo.Throw(ex);
            }
        }

        private static bool TryInitializeReferenceTracker(
            delegate* unmanaged<void> beginEndCallback,
            delegate* unmanaged<IntPtr, int> isReferencedCallback,
            delegate* unmanaged<IntPtr, void> trackedObjectEnteredFinalization)
        {
            throw new NotImplementedException();
        }

        private static IntPtr CreateReferenceTrackingHandleInternal(
            object obj,
            out int memInSizeT,
            out IntPtr mem)
        {
            throw new NotImplementedException();
        }
    }
}
