// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices.ObjectiveC
{
    public static unsafe partial class ObjectiveCMarshal
    {
        /// <summary>
        /// Sets a pending exception to be thrown the next time the runtime is entered from an Objective-C msgSend P/Invoke.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <remarks>
        /// If <c>null</c> is supplied any pending exception is discarded.
        /// </remarks>
        public static void SetMessageSendPendingException(Exception? exception)
        {
            Internal.Runtime.CompilerHelpers.InteropHelpers.SetPendingExceptionObject(exception);
        }

        private static bool TrySetGlobalMessageSendCallback(
            MessageSendFunction msgSendFunction,
            IntPtr func)
        {
            return Internal.Runtime.CompilerHelpers.InteropHelpers.TrySetGlobalMessageSendCallback(msgSendFunction, func);
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
