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

    // Constants used with GetClasslibFunction, to indicate which classlib function
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
        [RuntimeExport("GCCollect")]
        internal static void GCCollect(int generation, InternalGCCollectionMode mode, bool lowMemoryP = false)
        {
            GC_Collect(generation, mode, lowMemoryP ? Interop.BOOL.TRUE : Interop.BOOL.FALSE);
        }

        [DllImport(RuntimeLibrary)]
        private static extern void GC_Collect(int generation, InternalGCCollectionMode mode, Interop.BOOL lowMemoryP);

        [RuntimeExport("RhGetGcTotalMemory")]
        internal static long RhGetGcTotalMemory()
        {
            return GetGcTotalMemory();
        }

        [DllImport(RuntimeLibrary)]
        private static extern long GetGcTotalMemory();

        [RuntimeExport("RhStartNoGCRegion")]
        internal static int RhStartNoGCRegion(long totalSize, bool hasLohSize, long lohSize, bool disallowFullBlockingGC)
        {
            return StartNoGCRegion(totalSize, hasLohSize ? Interop.BOOL.TRUE : Interop.BOOL.FALSE, lohSize, disallowFullBlockingGC ? Interop.BOOL.TRUE : Interop.BOOL.FALSE);
        }

        [RuntimeExport("RhEndNoGCRegion")]
        internal static int RhEndNoGCRegion()
        {
            return EndNoGCRegion();
        }

        //
        // internalcalls for System.Runtime.__Finalizer.
        //

        // Fetch next object which needs finalization or return null if we've reached the end of the list.
        [RuntimeImport(RuntimeLibrary, "GetNextFinalizableObject")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object GetNextFinalizableObject();

        //
        // internalcalls for System.Runtime.InteropServices.GCHandle.
        //

        // Allocate handle.
        [RuntimeImport(RuntimeLibrary, "HandleAlloc")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr HandleAlloc(object value, GCHandleType type);

        [RuntimeImport(RuntimeLibrary, "RhHandleGet")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object RhHandleGet(IntPtr handle);

        [RuntimeImport(RuntimeLibrary, "RhHandleSet")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr RhHandleSet(IntPtr handle, object value);

        //
        // internal calls for allocation
        //
        [RuntimeImport(RuntimeLibrary, "NewFast")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object NewFast(MethodTable* pEEType);  // BEWARE: not for finalizable objects!

        [RuntimeImport(RuntimeLibrary, "NewFinalizable")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object NewFinalizable(MethodTable* pEEType);

        [RuntimeImport(RuntimeLibrary, "NewArrayFast")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object NewArrayFast(MethodTable* pEEType, int length);

#if FEATURE_64BIT_ALIGNMENT
        [RuntimeImport(RuntimeLibrary, "NewFastAlign8")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object NewFastAlign8(MethodTable * pEEType);  // BEWARE: not for finalizable objects!

        [RuntimeImport(RuntimeLibrary, "NewFinalizableAlign8")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object NewFinalizableAlign8(MethodTable* pEEType);

        [RuntimeImport(RuntimeLibrary, "NewArrayFastAlign8")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object NewArrayFastAlign8(MethodTable* pEEType, int length);

        [RuntimeImport(RuntimeLibrary, "NewFastMisalign")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object NewFastMisalign(MethodTable * pEEType);
#endif // FEATURE_64BIT_ALIGNMENT

        [RuntimeImport(RuntimeLibrary, "AssignRef")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void AssignRef(ref object? address, object? obj);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "GcSafeZeroMemory")]
        internal static extern unsafe ref byte GcSafeZeroMemory(ref byte dmem, nuint size);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhBulkMoveWithWriteBarrier")]
        internal static extern unsafe void RhBulkMoveWithWriteBarrier(ref byte dmem, ref byte smem, nuint size);

#if FEATURE_GC_STRESS
        //
        // internal calls for GC stress
        //
        [RuntimeImport(RuntimeLibrary, "InitializeGcStress")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void InitializeGcStress();
#endif // FEATURE_GC_STRESS

        [RuntimeImport(RuntimeLibrary, "EHEnumInitFromStackFrameIterator")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool EHEnumInitFromStackFrameIterator(ref StackFrameIterator pFrameIter, out EH.MethodRegionInfo pMethodRegionInfo, void* pEHEnum);

        [RuntimeImport(RuntimeLibrary, "EHEnumNext")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool EHEnumNext(void* pEHEnum, void* pEHClause);

        [RuntimeImport(RuntimeLibrary, "GetDispatchCellInfo")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void GetDispatchCellInfo(IntPtr pCell, out DispatchCellInfo newCellInfo);

        [RuntimeImport(RuntimeLibrary, "SearchDispatchCellCache")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe IntPtr SearchDispatchCellCache(IntPtr pCell, MethodTable* pInstanceType);

        [RuntimeImport(RuntimeLibrary, "UpdateDispatchCellCache")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe IntPtr UpdateDispatchCellCache(IntPtr pCell, IntPtr pTargetCode, MethodTable* pInstanceType, ref DispatchCellInfo newCellInfo);

        [RuntimeImport(RuntimeLibrary, "GetClasslibFunctionFromCodeAddress")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void* GetClasslibFunctionFromCodeAddress(IntPtr address, ClassLibFunctionId id);

        [RuntimeImport(RuntimeLibrary, "GetClasslibFunctionFromEEType")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void* GetClasslibFunctionFromEEType(MethodTable* pEEType, ClassLibFunctionId id);

        //
        // StackFrameIterator
        //

        [RuntimeImport(RuntimeLibrary, "SfiInit")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool SfiInit(ref StackFrameIterator pThis, void* pStackwalkCtx, bool instructionFault, bool* fIsExceptionIntercepted);

        [RuntimeImport(RuntimeLibrary, "SfiNext")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool SfiNext(ref StackFrameIterator pThis, uint* uExCollideClauseIdx, bool* fUnwoundReversePInvoke, bool* fIsExceptionIntercepted);

        //
        // Miscellaneous helpers.
        //

        [RuntimeImport(RuntimeLibrary, "CallCatchFunclet")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe IntPtr CallCatchFunclet(
            object exceptionObj, byte* pHandlerIP, void* pvRegDisplay, ref EH.ExInfo exInfo);

        [RuntimeImport(RuntimeLibrary, "CallFinallyFunclet")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void CallFinallyFunclet(byte* pHandlerIP, void* pvRegDisplay);

        [RuntimeImport(RuntimeLibrary, "CallFilterFunclet")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool CallFilterFunclet(
            object exceptionObj, byte* pFilterIP, void* pvRegDisplay);

#if FEATURE_OBJCMARSHAL
        [RuntimeImport(RuntimeLibrary, "CallPropagateExceptionCallback")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe IntPtr CallPropagateExceptionCallback(
            IntPtr callbackContext, IntPtr callback, void* pvRegDisplay, ref EH.ExInfo exInfo, IntPtr pPreviousTransitionFrame);
#endif // FEATURE_OBJCMARSHAL

        [RuntimeImport(RuntimeLibrary, "FallbackFailFast")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void FallbackFailFast();

        [RuntimeImport(RuntimeLibrary, "SetThreadDoNotTriggerGC")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void SetThreadDoNotTriggerGC();

        [System.Diagnostics.Conditional("DEBUG")]
        [RuntimeImport(RuntimeLibrary, "ValidateExInfoStack")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void ValidateExInfoStack();

#if TARGET_WINDOWS
        [RuntimeImport(RuntimeLibrary, "FirstChanceExceptionNotification")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void FirstChanceExceptionNotification();
#endif

#if TARGET_WINDOWS
        [RuntimeImport(RuntimeLibrary, "CopyContextFromExInfo")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void CopyContextFromExInfo(void* pOSContext, int cbOSContext, EH.PAL_LIMITED_CONTEXT* pPalContext);
#endif

        [RuntimeImport(RuntimeLibrary, "GetThreadAbortException")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern Exception GetThreadAbortException();

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
        internal static extern uint WaitForFinalizerRequest();

        // Indicate that the current round of finalizations is complete.
        [DllImport(RuntimeLibrary)]
        internal static extern void SignalFinalizationComplete(uint fCount, int observedFullGcCount);

        // Enters a no GC region, possibly doing a blocking GC if there is not enough
        // memory available to satisfy the caller's request.
        [DllImport(RuntimeLibrary)]
        internal static extern int StartNoGCRegion(long totalSize, Interop.BOOL hasLohSize, long lohSize, Interop.BOOL disallowFullBlockingGC);

        // Exits a no GC region, possibly doing a GC to clean up the garbage that
        // the caller allocated.
        [DllImport(RuntimeLibrary)]
        internal static extern int EndNoGCRegion();
    }
}
