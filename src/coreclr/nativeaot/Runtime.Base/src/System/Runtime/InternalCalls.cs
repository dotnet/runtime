// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This is where we group together all the internal calls.
//

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.Runtime;

#pragma warning disable SYSLIB1054 // Use DllImport here instead of LibraryImport because this file is used by Test.CoreLib

namespace System.Runtime
{
    internal enum DispatchCellType
    {
        InterfaceAndSlot = 0x0,
        MetadataToken = 0x1,
        VTableOffset = 0x2,
    }

    internal unsafe struct DispatchCellInfo
    {
        public DispatchCellType CellType;
        public MethodTable* InterfaceType;
        public ushort InterfaceSlot;
        public byte HasCache;
        public uint MetadataToken;
        public uint VTableOffset;
    }

    // Constants used with RhpGetClasslibFunction, to indicate which classlib function
    // we are interested in.
    // Note: make sure you change the def in ICodeManager.h if you change this!
    internal enum ClassLibFunctionId
    {
        GetRuntimeException = 0,
        FailFast = 1,
        // UnhandledExceptionHandler = 2, // unused
        AppendExceptionStackFrame = 3,
        // unused = 4,
        GetSystemArrayEEType = 5,
        OnFirstChance = 6,
        OnUnhandledException = 7,
        IDynamicCastableIsInterfaceImplemented = 8,
        IDynamicCastableGetInterfaceImplementation = 9,
        ObjectiveCMarshalTryGetTaggedMemory = 10,
        ObjectiveCMarshalGetIsTrackedReferenceCallback = 11,
        ObjectiveCMarshalGetOnEnteredFinalizerQueueCallback = 12,
        ObjectiveCMarshalGetUnhandledExceptionPropagationHandler = 13,
    }

    internal static class InternalCalls
    {
        //
        // internalcalls for System.GC.
        //

        private const string RuntimeLibrary = "*";

        // Force a garbage collection.
        [RuntimeExport("RhCollect")]
        internal static void RhCollect(int generation, InternalGCCollectionMode mode, bool lowMemoryP = false)
        {
            RhpCollect(generation, mode, lowMemoryP ? Interop.BOOL.TRUE : Interop.BOOL.FALSE);
        }

        [DllImport(RuntimeLibrary)]
        private static extern void RhpCollect(int generation, InternalGCCollectionMode mode, Interop.BOOL lowMemoryP);

        [RuntimeExport("RhGetGcTotalMemory")]
        internal static long RhGetGcTotalMemory()
        {
            return RhpGetGcTotalMemory();
        }

        [DllImport(RuntimeLibrary)]
        private static extern long RhpGetGcTotalMemory();

        [RuntimeExport("RhStartNoGCRegion")]
        internal static int RhStartNoGCRegion(long totalSize, bool hasLohSize, long lohSize, bool disallowFullBlockingGC)
        {
            return RhpStartNoGCRegion(totalSize, hasLohSize ? Interop.BOOL.TRUE : Interop.BOOL.FALSE, lohSize, disallowFullBlockingGC ? Interop.BOOL.TRUE : Interop.BOOL.FALSE);
        }

        [RuntimeExport("RhEndNoGCRegion")]
        internal static int RhEndNoGCRegion()
        {
            return RhpEndNoGCRegion();
        }

        //
        // internalcalls for System.Runtime.__Finalizer.
        //

        // Fetch next object which needs finalization or return null if we've reached the end of the list.
        [RuntimeImport(RuntimeLibrary, "RhpGetNextFinalizableObject")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object RhpGetNextFinalizableObject();

        //
        // internalcalls for System.Runtime.InteropServices.GCHandle.
        //

        // Allocate handle.
        [RuntimeImport(RuntimeLibrary, "RhpHandleAlloc")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr RhpHandleAlloc(object value, GCHandleType type);

        [RuntimeImport(RuntimeLibrary, "RhHandleGet")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object RhHandleGet(IntPtr handle);

        [RuntimeImport(RuntimeLibrary, "RhHandleSet")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr RhHandleSet(IntPtr handle, object value);

        //
        // internal calls for allocation
        //
        [RuntimeImport(RuntimeLibrary, "RhpNewFast")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object RhpNewFast(MethodTable* pEEType);  // BEWARE: not for finalizable objects!

        [RuntimeImport(RuntimeLibrary, "RhpNewFinalizable")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object RhpNewFinalizable(MethodTable* pEEType);

        [RuntimeImport(RuntimeLibrary, "RhpNewArrayFast")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object RhpNewArrayFast(MethodTable* pEEType, int length);

#if FEATURE_64BIT_ALIGNMENT
        [RuntimeImport(RuntimeLibrary, "RhpNewFastAlign8")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object RhpNewFastAlign8(MethodTable * pEEType);  // BEWARE: not for finalizable objects!

        [RuntimeImport(RuntimeLibrary, "RhpNewFinalizableAlign8")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object RhpNewFinalizableAlign8(MethodTable* pEEType);

        [RuntimeImport(RuntimeLibrary, "RhpNewArrayFastAlign8")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object RhpNewArrayFastAlign8(MethodTable* pEEType, int length);

        [RuntimeImport(RuntimeLibrary, "RhpNewFastMisalign")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object RhpNewFastMisalign(MethodTable * pEEType);
#endif // FEATURE_64BIT_ALIGNMENT

        [RuntimeImport(RuntimeLibrary, "RhpAssignRef")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void RhpAssignRef(ref object? address, object? obj);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpGcSafeZeroMemory")]
        internal static extern unsafe ref byte RhpGcSafeZeroMemory(ref byte dmem, nuint size);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhBulkMoveWithWriteBarrier")]
        internal static extern unsafe void RhBulkMoveWithWriteBarrier(ref byte dmem, ref byte smem, nuint size);

#if FEATURE_GC_STRESS
        //
        // internal calls for GC stress
        //
        [RuntimeImport(RuntimeLibrary, "RhpInitializeGcStress")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void RhpInitializeGcStress();
#endif // FEATURE_GC_STRESS

        [RuntimeImport(RuntimeLibrary, "RhpEHEnumInitFromStackFrameIterator")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool RhpEHEnumInitFromStackFrameIterator(ref StackFrameIterator pFrameIter, out EH.MethodRegionInfo pMethodRegionInfo, void* pEHEnum);

        [RuntimeImport(RuntimeLibrary, "RhpEHEnumNext")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool RhpEHEnumNext(void* pEHEnum, void* pEHClause);

        [RuntimeImport(RuntimeLibrary, "RhpGetDispatchCellInfo")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void RhpGetDispatchCellInfo(IntPtr pCell, out DispatchCellInfo newCellInfo);

        [RuntimeImport(RuntimeLibrary, "RhpSearchDispatchCellCache")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe IntPtr RhpSearchDispatchCellCache(IntPtr pCell, MethodTable* pInstanceType);

        [RuntimeImport(RuntimeLibrary, "RhpUpdateDispatchCellCache")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe IntPtr RhpUpdateDispatchCellCache(IntPtr pCell, IntPtr pTargetCode, MethodTable* pInstanceType, ref DispatchCellInfo newCellInfo);

        [RuntimeImport(RuntimeLibrary, "RhpGetClasslibFunctionFromCodeAddress")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void* RhpGetClasslibFunctionFromCodeAddress(IntPtr address, ClassLibFunctionId id);

        [RuntimeImport(RuntimeLibrary, "RhpGetClasslibFunctionFromEEType")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void* RhpGetClasslibFunctionFromEEType(MethodTable* pEEType, ClassLibFunctionId id);

        //
        // StackFrameIterator
        //

        [RuntimeImport(RuntimeLibrary, "RhpSfiInit")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool RhpSfiInit(ref StackFrameIterator pThis, void* pStackwalkCtx, bool instructionFault, bool* fIsExceptionIntercepted);

        [RuntimeImport(RuntimeLibrary, "RhpSfiNext")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool RhpSfiNext(ref StackFrameIterator pThis, uint* uExCollideClauseIdx, bool* fUnwoundReversePInvoke, bool* fIsExceptionIntercepted);

        //
        // Miscellaneous helpers.
        //

        [RuntimeImport(RuntimeLibrary, "RhpCallCatchFunclet")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe IntPtr RhpCallCatchFunclet(
            object exceptionObj, byte* pHandlerIP, void* pvRegDisplay, ref EH.ExInfo exInfo);

        [RuntimeImport(RuntimeLibrary, "RhpCallFinallyFunclet")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void RhpCallFinallyFunclet(byte* pHandlerIP, void* pvRegDisplay);

        [RuntimeImport(RuntimeLibrary, "RhpCallFilterFunclet")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool RhpCallFilterFunclet(
            object exceptionObj, byte* pFilterIP, void* pvRegDisplay);

#if FEATURE_OBJCMARSHAL
        [RuntimeImport(RuntimeLibrary, "RhpCallPropagateExceptionCallback")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe IntPtr RhpCallPropagateExceptionCallback(
            IntPtr callbackContext, IntPtr callback, void* pvRegDisplay, ref EH.ExInfo exInfo, IntPtr pPreviousTransitionFrame);
#endif // FEATURE_OBJCMARSHAL

        [RuntimeImport(RuntimeLibrary, "RhpFallbackFailFast")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void RhpFallbackFailFast();

        [RuntimeImport(RuntimeLibrary, "RhpSetThreadDoNotTriggerGC")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void RhpSetThreadDoNotTriggerGC();

        [System.Diagnostics.Conditional("DEBUG")]
        [RuntimeImport(RuntimeLibrary, "RhpValidateExInfoStack")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void RhpValidateExInfoStack();

#if TARGET_WINDOWS
        [RuntimeImport(RuntimeLibrary, "RhpFirstChanceExceptionNotification")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void RhpFirstChanceExceptionNotification();
#endif

#if TARGET_WINDOWS
        [RuntimeImport(RuntimeLibrary, "RhpCopyContextFromExInfo")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void RhpCopyContextFromExInfo(void* pOSContext, int cbOSContext, EH.PAL_LIMITED_CONTEXT* pPalContext);
#endif

        [RuntimeImport(RuntimeLibrary, "RhpGetThreadAbortException")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern Exception RhpGetThreadAbortException();

        [RuntimeImport(RuntimeLibrary, "RhCurrentNativeThreadId")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe IntPtr RhCurrentNativeThreadId();

        //------------------------------------------------------------------------------------------------------------
        // PInvoke-based internal calls
        //
        // These either do not need to be called in cooperative mode or, in some cases, MUST be called in preemptive
        // mode.  Note that they must use the Cdecl calling convention due to a limitation in our .obj file linking
        // support.
        // We use DllImport here instead of DllImport as we don't want to add a dependency on source-generated
        // interop support to Test.CoreLib.
        //------------------------------------------------------------------------------------------------------------

        // Block the current thread until at least one object needs to be finalized (returns true) or
        // memory is low (returns false and the finalizer thread should initiate a garbage collection).
        [DllImport(RuntimeLibrary)]
        internal static extern uint RhpWaitForFinalizerRequest();

        // Indicate that the current round of finalizations is complete.
        [DllImport(RuntimeLibrary)]
        internal static extern void RhpSignalFinalizationComplete(uint fCount, int observedFullGcCount);

        // Enters a no GC region, possibly doing a blocking GC if there is not enough
        // memory available to satisfy the caller's request.
        [DllImport(RuntimeLibrary)]
        internal static extern int RhpStartNoGCRegion(long totalSize, Interop.BOOL hasLohSize, long lohSize, Interop.BOOL disallowFullBlockingGC);

        // Exits a no GC region, possibly doing a GC to clean up the garbage that
        // the caller allocated.
        [DllImport(RuntimeLibrary)]
        internal static extern int RhpEndNoGCRegion();
    }
}
