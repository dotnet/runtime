// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.ObjectiveC
{
    /// <summary>
    /// API to enable Objective-C marshalling.
    /// </summary>
    [SupportedOSPlatform("macos")]
    [CLSCompliant(false)]
    public static class ObjectiveCMarshal
    {
        /// <summary>
        /// Handler for unhandled Exceptions crossing the managed -> native boundary (that is, Reverse P/Invoke).
        /// </summary>
        /// <param name="exception">Unhandled exception.</param>
        /// <param name="lastMethod">Last managed method.</param>
        /// <param name="context">Context provided to the returned function pointer.</param>
        /// <returns>Exception propagation callback.</returns>
        /// <remarks>
        /// If the handler is able to propagate the managed Exception properly to the native environment an
        /// unmanaged callback can be returned, otherwise <c>null</c>. The <see cref="RuntimeMethodHandle"/> is to the
        /// last managed method that was executed prior to leaving the runtime. Along with the returned callback
        /// the handler can return a context that will be passed to the callback during dispatch.
        ///
        /// The returned handler will be passed the context when called and it is the responsibility of the callback
        /// to manage. The handler must not return and is expected to propagate the exception (for example, throw a native exception)
        /// into the native environment or fail fast.
        /// </remarks>
        public unsafe delegate delegate* unmanaged<IntPtr, void> UnhandledExceptionPropagationHandler(
            Exception exception,
            RuntimeMethodHandle lastMethod,
            out IntPtr context);

        /// <summary>
        /// Initialize the Objective-C marshalling API.
        /// </summary>
        /// <param name="beginEndCallback">Called when tracking begins and ends.</param>
        /// <param name="isReferencedCallback">Called to determine if a managed object instance is referenced elsewhere, and must not be collected by the GC.</param>
        /// <param name="trackedObjectEnteredFinalization">Called when a tracked object enters the finalization queue.</param>
        /// <param name="unhandledExceptionPropagationHandler">Handler for the propagation of unhandled Exceptions across a managed -> native boundary (that is, Reverse P/Invoke).</param>
        /// <exception cref="InvalidOperationException">Thrown if this API has already been called.</exception>
        /// <remarks>
        /// All unmanaged function pointers must be written in native code since they will be called by the GC and
        /// managed code is not able to run at that time.
        ///
        /// The <paramref name="beginEndCallback"/> will be called when reference tracking begins and ends.
        /// The associated begin/end pair will never be nested. When using Workstation GC, the begin/end pair
        /// will be called on the same thread. When using Server GC, the begin/end pair is not guaranteed to
        /// be called on the same thread.
        ///
        /// The <paramref name="isReferencedCallback"/> should return 0 for not reference or 1 for
        /// referenced. Any other value has undefined behavior.
        /// </remarks>
        public static unsafe void Initialize(
            delegate* unmanaged<void> beginEndCallback,
            delegate* unmanaged<IntPtr, int> isReferencedCallback,
            delegate* unmanaged<IntPtr, void> trackedObjectEnteredFinalization,
            UnhandledExceptionPropagationHandler unhandledExceptionPropagationHandler)
            => throw new PlatformNotSupportedException();

        /// <summary>
        /// Request native reference tracking for the supplied object.
        /// </summary>
        /// <param name="obj">The object to track.</param>
        /// <param name="taggedMemory">A pointer to memory tagged to the object.</param>
        /// <returns>Reference tracking GC handle.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the ObjectiveCMarshal API has not been initialized.</exception>
        /// <remarks>
        /// The Initialize() must be called prior to calling this function.
        ///
        /// The <paramref name="obj"/> must have a type in its hierarchy marked with
        /// <see cref="ObjectiveCTrackedTypeAttribute"/>.
        ///
        /// The "Is Referenced" callback passed to Initialize()
        /// will be passed the <paramref name="taggedMemory"/> returned from this function.
        /// The memory it points at is defined by the length in the <see cref="Span{IntPtr}"/> and
        /// will be zeroed out. It will be available until <paramref name="obj"/> is collected by the GC.
        /// The memory pointed to by <paramref name="taggedMemory"/> can be used for any purpose by the
        /// caller of this function and usable during the "Is Referenced" callback.
        ///
        /// Calling this function multiple times with the same <paramref name="obj"/> will
        /// return a new handle each time but the same tagged memory will be returned. The
        /// tagged memory is only guaranteed to be zero initialized on the first call.
        ///
        /// The caller is responsible for freeing the returned <see cref="GCHandle"/>.
        /// </remarks>
        public static GCHandle CreateReferenceTrackingHandle(
            object obj,
            out Span<IntPtr> taggedMemory)
            => throw new PlatformNotSupportedException();

        /// <summary>
        /// Objective-C msgSend function override options.
        /// </summary>
        public enum MessageSendFunction
        {
            /// <summary>
            /// Overrides the Objective-C runtime's <see href="https://developer.apple.com/documentation/objectivec/1456712-objc_msgsend">msgSend()</see>.
            /// </summary>
            MsgSend,
            /// <summary>
            /// Overrides the Objective-C runtime's <see href="https://developer.apple.com/documentation/objectivec/1456697-objc_msgsend_fpret">objc_msgSend_fpret()</see>.
            /// </summary>
            MsgSendFpret,
            /// <summary>
            /// Overrides the Objective-C runtime's <see href="https://developer.apple.com/documentation/objectivec/1456730-objc_msgsend_stret">objc_msgSend_stret()</see>.
            /// </summary>
            MsgSendStret,
            /// <summary>
            /// Overrides the Objective-C runtime's <see href="https://developer.apple.com/documentation/objectivec/1456716-objc_msgsendsuper">objc_msgSendSuper()</see>.
            /// </summary>
            MsgSendSuper,
            /// <summary>
            /// Overrides the Objective-C runtime's <see href="https://developer.apple.com/documentation/objectivec/1456722-objc_msgsendsuper_stret">objc_msgSendSuper_stret()</see>.
            /// </summary>
            MsgSendSuperStret,
        }

        /// <summary>
        /// Set a function pointer override for an Objective-C runtime message passing export.
        /// </summary>
        /// <param name="msgSendFunction">The export to override.</param>
        /// <param name="func">The function override.</param>
        /// <exception cref="InvalidOperationException">Thrown if the msgSend function has already been overridden.</exception>
        /// <remarks>
        /// Providing an override can enable support for Objective-C variadic argument support.
        /// </remarks>
        public static void SetMessageSendCallback(MessageSendFunction msgSendFunction, IntPtr func)
            => throw new PlatformNotSupportedException();

        /// <summary>
        /// Sets a pending exception to be thrown the next time the runtime is entered from an Objective-C msgSend P/Invoke.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <remarks>
        /// If <c>null</c> is supplied any pending exception is discarded.
        /// </remarks>
        public static void SetMessageSendPendingException(Exception? exception)
            => throw new PlatformNotSupportedException();
    }
}
