// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.ObjectiveC
{
    public static partial class Bridge
    {
        /// <summary>
        /// Sets a pending exception for this thread to be thrown
        /// the next time the runtime is entered from an overridden
        /// msgSend P/Invoke.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <remarks>
        /// If <c>null</c> is supplied any pending exception is discarded.
        /// </remarks>
        public static void SetMessageSendPendingExceptionForThread(Exception? exception)
        {
            System.StubHelpers.StubHelpers.SetPendingExceptionObject(exception);
        }

        [DllImport(RuntimeHelpers.QCall)]
        private static extern bool TrySetGlobalMessageSendCallback(
            MsgSendFunction msgSendFunction,
            IntPtr func);

        [DllImport(RuntimeHelpers.QCall)]
        private static unsafe extern bool TryInitializeReferenceTracker(
            delegate* unmanaged<int, void> beginEndCallback,
            delegate* unmanaged<IntPtr, int> isReferencedCallback,
            delegate* unmanaged<IntPtr, void> trackedObjectEnteredFinalization);

        [DllImport(RuntimeHelpers.QCall)]
        private static extern IntPtr CreateReferenceTrackingHandleInternal(
            ObjectHandleOnStack obj,
            out IntPtr scratchMemory);
    }
}