// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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
            delegate* unmanaged<void> beginEndCallback,
            delegate* unmanaged<IntPtr, int> isReferencedCallback,
            delegate* unmanaged<IntPtr, void> trackedObjectEnteredFinalization);

        [DllImport(RuntimeHelpers.QCall)]
        private static extern IntPtr CreateReferenceTrackingHandleInternal(
            ObjectHandleOnStack obj,
            out IntPtr scratchMemory);

        internal static bool AvailableUnhandledExceptionPropagation()
        {
            return UnhandledExceptionPropagation != null;
        }

        internal static unsafe void* InvokeUnhandledExceptionPropagation(
            Exception exception,
            object methodInfoStub,
            out IntPtr context)
        {
            context = IntPtr.Zero;
            if (UnhandledExceptionPropagation == null)
                return null;

            Debug.Assert(methodInfoStub is RuntimeMethodInfoStub);
            var runtimeHandle = new RuntimeMethodHandle((RuntimeMethodInfoStub)methodInfoStub);
            foreach (UnhandledExceptionPropagationHandler handler in UnhandledExceptionPropagation.GetInvocationList())
            {
                var callback = handler(exception, runtimeHandle, out context);
                if (callback != null)
                    return callback;
            }

            return null;
        }
    }
}