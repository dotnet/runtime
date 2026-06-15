// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Runtime.InteropServices.ObjectiveC
{
    /// <summary>
    /// API to enable Objective-C marshalling.
    /// </summary>
    [SupportedOSPlatform("macos")]
    [CLSCompliant(false)]
    public static partial class ObjectiveCMarshal
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

        private static UnhandledExceptionPropagationHandler? s_unhandledExceptionPropagationHandler;
        private static bool s_initialized;
        private static readonly ConditionalWeakTable<object, ObjcTrackingInformation> s_objects = new();

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
        {
            ArgumentNullException.ThrowIfNull(beginEndCallback);
            ArgumentNullException.ThrowIfNull(isReferencedCallback);
            ArgumentNullException.ThrowIfNull(trackedObjectEnteredFinalization);
            ArgumentNullException.ThrowIfNull(unhandledExceptionPropagationHandler);

            if (s_unhandledExceptionPropagationHandler != null
                || !TryInitializeReferenceTracker(
                    beginEndCallback,
                    isReferencedCallback,
                    trackedObjectEnteredFinalization))
            {
                throw new InvalidOperationException(SR.InvalidOperation_ReinitializeObjectiveCMarshal);
            }
            s_initialized = true;
            s_unhandledExceptionPropagationHandler = unhandledExceptionPropagationHandler;
        }

        /// <summary>
        /// Request native reference tracking for the supplied object.
        /// </summary>
        /// <param name="obj">The object to track.</param>
        /// <param name="taggedMemory">A pointer to memory tagged to the object.</param>
        /// <returns>Reference tracking GC handle.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the ObjectiveCMarshal API has not been initialized.</exception>
        /// <remarks>
        /// The <see cref="Initialize" /> function must be called prior to calling this function.
        ///
        /// The <paramref name="obj"/> parameter must have a type in its hierarchy marked with
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
        /// The tagged memory returned is the same as the memory returned from <see cref="GetOrCreateReferenceTrackingMemory" />.
        ///
        /// The caller is responsible for freeing the returned <see cref="GCHandle"/>.
        /// </remarks>
        public static GCHandle CreateReferenceTrackingHandle(
            object obj,
            out Span<IntPtr> taggedMemory)
        {
            // Defer to GetOrCreateReferenceTrackingMemory for argument/state validation.
            taggedMemory = GetOrCreateReferenceTrackingMemory(obj);
            return GCHandle.FromIntPtr(AllocateReferenceTrackingHandle(obj));
        }

        /// <summary>
        /// Gets reference tracking memory for the supplied object.
        /// </summary>
        /// <param name="obj">The object whose tracking memory to return.</param>
        /// <returns>A span of tracking memory associated with <paramref name="obj"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the ObjectiveCMarshal API has not been initialized.</exception>
        /// <remarks>
        /// The Initialize() must be called prior to calling this function.
        ///
        /// The <paramref name="obj"/> must have a type in its hierarchy marked with
        /// <see cref="ObjectiveCTrackedTypeAttribute"/>.
        ///
        /// The "Is Referenced" callback passed to <see cref="Initialize" />
        /// will be passed the memory returned from this function.
        /// The memory it points at is defined by the length in the <see cref="Span{IntPtr}"/> and
        /// will be zeroed out. It will be available until <paramref name="obj"/> is collected by the GC.
        /// The returned memory can be used for any purpose by the caller of this function and usable
        /// during the "Is Referenced" callback.
        ///
        /// Calling this function multiple times with the same <paramref name="obj"/> will
        /// return the same tracking memory. It is only guaranteed to be zero initialized on
        /// the first call of this or <see cref="CreateReferenceTrackingHandle" />.
        ///
        /// The return value is the same as the tracking memory returned from <see cref="CreateReferenceTrackingHandle" />.
        /// </remarks>
        public static Span<IntPtr> GetOrCreateReferenceTrackingMemory(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            if (!s_initialized)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ObjectiveCMarshalNotInitialized);
            }

            if (!IsTrackedReferenceWithFinalizer(obj))
            {
                throw new InvalidOperationException(SR.InvalidOperation_ObjectiveCTypeNoFinalizer);
            }

            ObjcTrackingInformation trackerInfo = s_objects.GetOrAdd(obj, static _ => new ObjcTrackingInformation());
            trackerInfo.EnsureInitialized(obj);
            trackerInfo.GetTaggedMemory(out int memInSizeT, out IntPtr mem);

            unsafe
            {
                return new Span<IntPtr>(mem.ToPointer(), memInSizeT);
            }
        }

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
        {
            ArgumentNullException.ThrowIfNull(func);

            if (msgSendFunction < MessageSendFunction.MsgSend || msgSendFunction > MessageSendFunction.MsgSendSuperStret)
                throw new ArgumentOutOfRangeException(nameof(msgSendFunction));

            if (!TrySetGlobalMessageSendCallback(msgSendFunction, func))
                throw new InvalidOperationException(SR.InvalidOperation_ResetGlobalObjectiveCMsgSend);
        }

        internal sealed class ObjcTrackingInformation
        {
            private const int TAGGED_MEMORY_SIZE_IN_POINTERS = 2;

            internal IntPtr _memory;
            private IntPtr _longWeakHandle;

            public ObjcTrackingInformation()
            {
                _memory = (IntPtr)NativeMemory.AllocZeroed(TAGGED_MEMORY_SIZE_IN_POINTERS * (nuint)IntPtr.Size);
            }

            public void GetTaggedMemory(out int memInSizeT, out IntPtr mem)
            {
                memInSizeT = TAGGED_MEMORY_SIZE_IN_POINTERS;
                mem = _memory;
            }

            public void EnsureInitialized(object o)
            {
                if (_longWeakHandle != IntPtr.Zero)
                {
                    return;
                }

#if NATIVEAOT
                IntPtr newHandle = RuntimeImports.RhHandleAlloc(o, GCHandleType.WeakTrackResurrection);
#else
                IntPtr newHandle = GCHandle.ToIntPtr(GCHandle.Alloc(o, GCHandleType.WeakTrackResurrection));
#endif
                if (Interlocked.CompareExchange(ref _longWeakHandle, newHandle, IntPtr.Zero) != IntPtr.Zero)
                {
#if NATIVEAOT
                    RuntimeImports.RhHandleFree(newHandle);
#else
                    GCHandle.FromIntPtr(newHandle).Free();
#endif
                }
            }

            ~ObjcTrackingInformation()
            {
                IntPtr longWeakHandle = Volatile.Read(ref _longWeakHandle);
#if NATIVEAOT
                if (longWeakHandle != IntPtr.Zero && RuntimeImports.RhHandleGet(longWeakHandle) != null)
#else
                if (longWeakHandle != IntPtr.Zero && GCHandle.FromIntPtr(longWeakHandle).Target != null)
#endif
                {
                    GC.ReRegisterForFinalize(this);
                    return;
                }

                IntPtr memory = Interlocked.Exchange(ref _memory, IntPtr.Zero);
                if (memory != IntPtr.Zero)
                {
                    NativeMemory.Free((void*)memory);
                }

                longWeakHandle = Interlocked.Exchange(ref _longWeakHandle, IntPtr.Zero);
                if (longWeakHandle != IntPtr.Zero)
                {
#if NATIVEAOT
                    RuntimeImports.RhHandleFree(longWeakHandle);
#else
                    GCHandle.FromIntPtr(longWeakHandle).Free();
#endif
                }
            }
        }
    }
}
