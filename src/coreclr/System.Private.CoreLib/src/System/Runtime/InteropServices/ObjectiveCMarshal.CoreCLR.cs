// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.ObjectiveC
{
    public static partial class ObjectiveCMarshal
    {
        /// <summary>
        /// Sets a pending exception to be thrown the next time the runtime is entered from an overridden msgSend P/Invoke.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <remarks>
        /// If <c>null</c> is supplied any pending exception is discarded.
        /// </remarks>
        public static void SetMessageSendPendingException(Exception? exception)
        {
            System.StubHelpers.StubHelpers.SetPendingExceptionObject(exception);
        }

        [DllImport(RuntimeHelpers.QCall)]
        private static extern bool TrySetGlobalMessageSendCallback(
            MessageSendFunction msgSendFunction,
            IntPtr func);

        [DllImport(RuntimeHelpers.QCall)]
        private static unsafe extern bool TryInitializeReferenceTracker(
            delegate* unmanaged<void> beginEndCallback,
            delegate* unmanaged<IntPtr, int> isReferencedCallback,
            delegate* unmanaged<IntPtr, void> trackedObjectEnteredFinalization);

        [DllImport(RuntimeHelpers.QCall)]
        private static extern IntPtr CreateReferenceTrackingHandleInternal(
            ObjectHandleOnStack obj,
            out int memInSizeT,
            out IntPtr mem);

        internal static bool AvailableUnhandledExceptionPropagation()
        {
            return s_unhandledExceptionPropagationHandler != null;
        }

        internal static unsafe void* InvokeUnhandledExceptionPropagation(
            Exception exception,
            object methodInfoStub,
            out IntPtr context)
        {
            context = IntPtr.Zero;
            if (s_unhandledExceptionPropagationHandler == null)
                return null;

            Debug.Assert(methodInfoStub is RuntimeMethodInfoStub);
            var runtimeHandle = new RuntimeMethodHandle((RuntimeMethodInfoStub)methodInfoStub);
            var callback = s_unhandledExceptionPropagationHandler(exception, runtimeHandle, out context);
            if (callback != null)
                return callback;

            return null;
        }
    }
}