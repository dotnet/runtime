// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.ObjectiveC
{
    public static partial class ObjectiveCMarshal
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
            System.StubHelpers.StubHelpers.SetPendingExceptionObject(exception);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ObjCMarshal_TrySetGlobalMessageSendCallback")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool TrySetGlobalMessageSendCallback(
            MessageSendFunction msgSendFunction,
            IntPtr func);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ObjCMarshal_TryInitializeReferenceTracker")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool TryInitializeReferenceTracker(
            delegate* unmanaged<void> beginEndCallback,
            delegate* unmanaged<IntPtr, int> isReferencedCallback,
            delegate* unmanaged<IntPtr, void> trackedObjectEnteredFinalization,
            ObjectHandleOnStack objectTrackingInfoTable);

        private static bool TryInitializeReferenceTracker(
            delegate* unmanaged<void> beginEndCallback,
            delegate* unmanaged<IntPtr, int> isReferencedCallback,
            delegate* unmanaged<IntPtr, void> trackedObjectEnteredFinalization)
             => TryInitializeReferenceTracker(
                beginEndCallback,
                isReferencedCallback,
                trackedObjectEnteredFinalization,
                ObjectHandleOnStack.Create(ref s_objects));

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ObjCMarshal_AllocateReferenceTrackingHandle")]
        private static partial IntPtr AllocateReferenceTrackingHandle(ObjectHandleOnStack obj);

        private static IntPtr AllocateReferenceTrackingHandle(object obj)
            => AllocateReferenceTrackingHandle(ObjectHandleOnStack.Create(ref obj));

        [UnmanagedCallersOnly]
        internal static unsafe void* InvokeUnhandledExceptionPropagation(Exception* pExceptionArg, IntPtr methodDesc, IntPtr* pContext, Exception* pException)
        {
            try
            {
                *pContext = IntPtr.Zero;
                if (s_unhandledExceptionPropagationHandler is null)
                    return null;

                RuntimeMethodHandle runtimeHandle = RuntimeMethodHandle.FromIntPtr(methodDesc);
                return s_unhandledExceptionPropagationHandler(*pExceptionArg, runtimeHandle, out *pContext);
            }
            catch (Exception ex)
            {
                *pException = ex;
                return null;
            }
        }
    }
}
