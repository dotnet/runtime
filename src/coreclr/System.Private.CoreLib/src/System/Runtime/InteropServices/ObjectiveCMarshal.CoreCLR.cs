// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.ObjectiveC
{
    public static partial class ObjectiveCMarshal
    {
        private static bool s_initialized;
        private static ConditionalWeakTable<object, ObjcTrackingInformation> s_objects = new();

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

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ObjCMarshal_AllocateReferenceTrackingHandle")]
        private static partial IntPtr AllocateReferenceTrackingHandle(ObjectHandleOnStack obj);

        private static IntPtr CreateReferenceTrackingHandleInternal(
            object obj,
            out int memInSizeT,
            out IntPtr mem)
        {
            // Rely on GetOrCreateReferenceTrackingMemoryInternal for state checking.
            GetOrCreateReferenceTrackingMemoryInternal(obj, out memInSizeT, out mem);
            return AllocateReferenceTrackingHandle(ObjectHandleOnStack.Create(ref obj));
        }

        private static unsafe void GetOrCreateReferenceTrackingMemoryInternal(
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

            ObjcTrackingInformation trackerInfo = s_objects.GetOrAdd(obj, static _ => new ObjcTrackingInformation());
            trackerInfo.EnsureInitialized(obj);
            trackerInfo.GetTaggedMemory(out memInSizeT, out mem);
        }

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

        internal sealed class ObjcTrackingInformation
        {
            // Keep in sync with NativeAOT.
            private const int TAGGED_MEMORY_SIZE_IN_POINTERS = 2;

            internal IntPtr _memory;
            private GCHandle _longWeakHandle;

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
                if (_longWeakHandle.IsAllocated)
                {
                    return;
                }

                GCHandle newHandle = GCHandle.Alloc(o, GCHandleType.WeakTrackResurrection);
                lock (this)
                {
                    if (_longWeakHandle.IsAllocated)
                    {
                        newHandle.Free();
                    }
                    else
                    {
                        _longWeakHandle = newHandle;
                    }
                }
            }

            ~ObjcTrackingInformation()
            {
                if (_longWeakHandle.IsAllocated && _longWeakHandle.Target != null)
                {
                    GC.ReRegisterForFinalize(this);
                    return;
                }

                if (_memory != IntPtr.Zero)
                {
                    NativeMemory.Free((void*)_memory);
                    _memory = IntPtr.Zero;
                }

                if (_longWeakHandle.IsAllocated)
                {
                    _longWeakHandle.Free();
                }
            }
        }
    }
}
