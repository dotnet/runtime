// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.ObjectiveC
{
    [SupportedOSPlatform("macos")]
    [CLSCompliant(false)]
    public static class Bridge
    {
        /// <summary>
        /// Initialize reference tracking for the Objective-C bridge API.
        /// </summary>
        /// <param name="beginEndCallback">Called when tracking begins and ends.</param>
        /// <param name="isReferencedCallback">Called to determine if a managed object instance is reference.</param>
        /// <param name="trackedObjectEnteredFinalization">Called when a tracked object enters the finalization queue.</param>
        /// <exception cref="InvalidOperationException">Thrown if this API has already been called.</exception>
        /// <remarks>
        /// All callbacks must be written in native code since they will be called by the GC and
        /// managed code is not able to run at that time.
        ///
        /// The <paramref name="beginEndCallback"/> will be called when reference tracking begins and ends.
        /// The associated begin/end pair will never be nested.
        ///
        /// The <paramref name="isReferencedCallback"/> should return 0 for not reference or 1 for
        /// referenced. Any other value has undefined behavior.
        /// </remarks>
        public static unsafe void InitializeReferenceTracking(
            delegate* unmanaged<void> beginEndCallback,
            delegate* unmanaged<IntPtr, int> isReferencedCallback,
            delegate* unmanaged<IntPtr, void> trackedObjectEnteredFinalization)
            => throw new PlatformNotSupportedException();

        /// <summary>
        /// Request native reference tracking for the supplied object.
        /// </summary>
        /// <param name="obj">The object to track.</param>
        /// <param name="scratchMemory">A pointer to scratch memory.</param>
        /// <returns>Reference tracking GC handle.</returns>
        /// <remarks>
        /// Reference tracking in the <see cref="Bridge"/> must be initialized prior to calling
        /// this function.
        ///
        /// The <paramref name="obj"/> must have a type in its hierarchy marked with
        /// <see cref="TrackedNativeReferenceAttribute"/>.
        ///
        /// The "Is Referenced" callback passed to InitializeReferenceTracking
        /// will be passed the <paramref name="scratchMemory"/> returned from this function.
        /// The memory it points at is 2 pointers' worth (for example, 16 bytes on a 64-bit platform) and
        /// will be zeroed out and available until <paramref name="obj"/> is collected by the GC.
        /// The memory pointed to by <paramref name="scratchMemory"/> can be used for any purpose by the
        /// caller of this function and usable during the "Is Referenced" callback.
        ///
        /// Calling this function multiple times with the same <paramref name="obj"/> will
        /// return a new handle each time but the same scratch memory. The
        /// scratch memory is only guaranteed to be zero initialized on the first call.
        ///
        /// The caller is responsible for freeing the returned <see cref="GCHandle"/>.
        /// </remarks>
        public static GCHandle CreateReferenceTrackingHandle(
            object obj,
            out IntPtr scratchMemory)
            => throw new PlatformNotSupportedException();

        /// <summary>
        /// Objective-C msgSend function override options.
        /// </summary>
        /// <see href="https://developer.apple.com/documentation/objectivec/1456712-objc_msgsend"/>
        public enum MsgSendFunction
        {
            ObjCMsgSend,
            ObjCMsgSendFpret,
            ObjCMsgSendStret,
            ObjCMsgSendSuper,
            ObjCMsgSendSuperStret,
        }

        /// <summary>
        /// Set function pointer override for an Objective-C runtime message passing export.
        /// </summary>
        /// <param name="msgSendFunction">The export to override.</param>
        /// <param name="func">The function override.</param>
        /// <exception cref="InvalidOperationException">Thrown if the msgSend function has already been overridden.</exception>
        /// <remarks>
        /// Providing an override can enable support for Objective-C
        /// exception propagation and variadic argument support.
        /// </remarks>
        public static void SetMessageSendCallback(MsgSendFunction msgSendFunction, IntPtr func)
            => throw new PlatformNotSupportedException();

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
            => throw new PlatformNotSupportedException();

        /// <summary>
        /// Handler for unhandled Exceptions crossing the managed -> native boundary (that is, Reverse P/Invoke).
        /// </summary>
        /// <param name="exception">Unhandled exception.</param>
        /// <param name="lastMethod">Last managed method.</param>
        /// <param name="context">Context provided to the returned function pointer.</param>
        /// <returns>Exception propagation callback.</returns>
        /// <remarks>
        /// If the handler is able to propagate the managed Exception properly to the native environment an
        /// unmanaged callback can be returned, otherwise <c>null</c>. The RuntimeMethodHandle is to the
        /// last managed method that was executed prior to leaving the runtime. Along with the returned callback
        /// the handler can return a context that will be passed to the callback during dispatch.
        ///
        /// The returned handler will be passed the context when called and is the responsibility of the callback
        /// to managed. The handler must not return and is expected to propagate the exception into the native
        /// environment or fail fast.
        /// </remarks>
        public unsafe delegate delegate* unmanaged<IntPtr, void> UnhandledExceptionPropagationHandler(
            Exception exception,
            RuntimeMethodHandle lastMethod,
            out IntPtr context);

        /// <summary>
        /// Event for handling the propagation of unhandled Exceptions across a managed -> native boundary (that is, Reverse P/Invoke).
        /// </summary>
        /// <remarks>
        /// The first handler to return a non-null callback will be used to handle the Exception and no other
        /// handlers will be asked if they can propagate the exception.
        /// </remarks>
        public static event UnhandledExceptionPropagationHandler? UnhandledExceptionPropagation
        {
            add => throw new PlatformNotSupportedException();
            remove => throw new PlatformNotSupportedException();
        }
    }
}
