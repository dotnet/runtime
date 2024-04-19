// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace System.Runtime.InteropServices.ObjectiveC
{
    public static unsafe partial class ObjectiveCMarshal
    {
        private static readonly IntPtr[] s_ObjcMessageSendFunctions = new IntPtr[(int)MessageSendFunction.MsgSendSuperStret + 1];
        private static bool s_initialized;
        private static readonly ConditionalWeakTable<object, ObjcTrackingInformation> s_objects = new();
        private static delegate* unmanaged[SuppressGCTransition]<void*, int> s_IsTrackedReferenceCallback;
        private static delegate* unmanaged[SuppressGCTransition]<void*, void> s_OnEnteredFinalizerQueueCallback;

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

        [RuntimeExport("ObjectiveCMarshalTryGetTaggedMemory")]
        private static bool TryGetTaggedMemory(IntPtr pObj, IntPtr* tagged)
        {
            // We are paused in the GC, so this is safe.
            object obj = Unsafe.AsRef<object>((void*)&pObj);

            if (!s_objects.TryGetValue(obj, out ObjcTrackingInformation? info))
            {
                return false;
            }

            *tagged = info._memory;
            return true;
        }

        [RuntimeExport("ObjectiveCMarshalGetIsTrackedReferenceCallback")]
        private static delegate* unmanaged[SuppressGCTransition]<void*, int> GetIsTrackedReferenceCallback()
        {
            return s_IsTrackedReferenceCallback;
        }

        [RuntimeExport("ObjectiveCMarshalGetOnEnteredFinalizerQueueCallback")]
        private static delegate* unmanaged[SuppressGCTransition]<void*, void> GetOnEnteredFinalizerQueueCallback()
        {
            return s_OnEnteredFinalizerQueueCallback;
        }

        [RuntimeExport("ObjectiveCMarshalGetUnhandledExceptionPropagationHandler")]
#pragma warning disable IDE0060
        private static IntPtr ObjectiveCMarshalGetUnhandledExceptionPropagationHandler(object exceptionObj, IntPtr ip, out IntPtr context)
#pragma warning restore IDE0060
        {
            if (s_unhandledExceptionPropagationHandler == null)
            {
                context = IntPtr.Zero;
                return IntPtr.Zero;
            }

            Exception? ex = exceptionObj as Exception;
            if (ex == null)
                Environment.FailFast("Exceptions must derive from the System.Exception class");

            // TODO: convert IP to RuntimeMethodHandle.
            // https://github.com/dotnet/runtime/issues/80985
            RuntimeMethodHandle lastMethod = default;

            return (IntPtr)s_unhandledExceptionPropagationHandler(ex, lastMethod, out context);
        }

        private static bool TryInitializeReferenceTracker(
            delegate* unmanaged<void> beginEndCallback,
            delegate* unmanaged<IntPtr, int> isReferencedCallback,
            delegate* unmanaged<IntPtr, void> trackedObjectEnteredFinalization)
        {
            if (!RuntimeImports.RhRegisterObjectiveCMarshalBeginEndCallback((IntPtr)beginEndCallback))
            {
                return false;
            }

            s_IsTrackedReferenceCallback = (delegate* unmanaged[SuppressGCTransition]<void*, int>)isReferencedCallback;
            s_OnEnteredFinalizerQueueCallback = (delegate* unmanaged[SuppressGCTransition]<void*, void>)trackedObjectEnteredFinalization;
            s_initialized = true;

            return true;
        }

        private static IntPtr CreateReferenceTrackingHandleInternal(
            object obj,
            out int memInSizeT,
            out IntPtr mem)
        {
            if (!s_initialized)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ObjectiveCMarshalNotInitialized);
            }

            if (!obj.GetMethodTable()->IsTrackedReferenceWithFinalizer)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ObjectiveCTypeNoFinalizer);
            }

            var trackerInfo = s_objects.GetValue(obj, static o => new ObjcTrackingInformation());
            trackerInfo.EnsureInitialized(obj);
            trackerInfo.GetTaggedMemory(out memInSizeT, out mem);
            return RuntimeImports.RhHandleAllocRefCounted(obj);
        }

        internal class ObjcTrackingInformation
        {
            // This matches the CoreCLR implementation. See
            // InteropSyncBlockInfo::m_taggedAlloc in syncblk.h .
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

                IntPtr newHandle = RuntimeImports.RhHandleAlloc(o, GCHandleType.WeakTrackResurrection);
                if (Interlocked.CompareExchange(ref _longWeakHandle, newHandle, IntPtr.Zero) != IntPtr.Zero)
                {
                    RuntimeImports.RhHandleFree(newHandle);
                }
            }

            ~ObjcTrackingInformation()
            {
                if (_longWeakHandle != IntPtr.Zero && RuntimeImports.RhHandleGet(_longWeakHandle) != null)
                {
                    GC.ReRegisterForFinalize(this);
                    return;
                }

                if (_memory != IntPtr.Zero)
                {
                    NativeMemory.Free((void*)_memory);
                    _memory = IntPtr.Zero;
                }
                if (_longWeakHandle != IntPtr.Zero)
                {
                    RuntimeImports.RhHandleFree(_longWeakHandle);
                    _longWeakHandle = IntPtr.Zero;
                }
            }
        }
    }
}
