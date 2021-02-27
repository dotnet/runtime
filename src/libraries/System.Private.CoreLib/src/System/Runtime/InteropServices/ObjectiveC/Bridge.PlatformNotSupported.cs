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
        /// The <paramref name="beginEndCallback"/> will be called when reference tracking begins and ends.
        /// The begin call will be passed a positive non-zero number and the end call
        /// will be passed the same non-zero number but negative (for example, begin: 2, end: -2).
        /// The associated begin/end pair will never be nested and so the callback can
        /// assume that if a value of "X" was passed in that same value will not be passed
        /// until a value of "-X" is observed. This callback cannot be written in managed code since this will
        /// be called by the GC and managed code is not able to run at that time.
        ///
        /// The <paramref name="isReferencedCallback"/> should return 0 for not reference or 1 for
        /// referenced. Any other value has undefined behavior.
        /// </remarks>
        public static unsafe void InitializeReferenceTracking(
            delegate* unmanaged<int, void> beginEndCallback,
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
    }
}
