// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This is where we group together all the internal calls.
//

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Internal.Runtime;

namespace System.Runtime
{
    internal enum DispatchCellType
    {
        InterfaceAndSlot = 0x0,
        MetadataToken = 0x1,
        VTableOffset = 0x2,
    }

    internal struct DispatchCellInfo
    {
        public DispatchCellType CellType;
        public EETypePtr InterfaceType;
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
    }

    internal static class InternalCalls
    {
        //
        // internalcalls for System.GC.
        //

        // Force a garbage collection.
        [RuntimeExport("RhCollect")]
        internal static void RhCollect(int generation, InternalGCCollectionMode mode)
        {
            RhpCollect(generation, mode);
        }

        [DllImport(Redhawk.BaseName)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
        private static extern void RhpCollect(int generation, InternalGCCollectionMode mode);

        [RuntimeExport("RhGetGcTotalMemory")]
        internal static long RhGetGcTotalMemory()
        {
            return RhpGetGcTotalMemory();
        }

        [DllImport(Redhawk.BaseName)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
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
        [RuntimeImport(Redhawk.BaseName, "RhpGetNextFinalizableObject")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object RhpGetNextFinalizableObject();

        //
        // internalcalls for System.Runtime.InteropServices.GCHandle.
        //

        // Allocate handle.
        [RuntimeImport(Redhawk.BaseName, "RhpHandleAlloc")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr RhpHandleAlloc(object value, GCHandleType type);

        [RuntimeImport(Redhawk.BaseName, "RhHandleGet")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object RhHandleGet(IntPtr handle);

        [RuntimeImport(Redhawk.BaseName, "RhHandleSet")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr RhHandleSet(IntPtr handle, object value);

        //
        // internal calls for allocation
        //
        [RuntimeImport(Redhawk.BaseName, "RhpNewFast")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object RhpNewFast(MethodTable* pEEType);  // BEWARE: not for finalizable objects!

        [RuntimeImport(Redhawk.BaseName, "RhpNewFinalizable")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object RhpNewFinalizable(MethodTable* pEEType);

        [RuntimeImport(Redhawk.BaseName, "RhpNewArray")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object RhpNewArray(MethodTable* pEEType, int length);

#if FEATURE_64BIT_ALIGNMENT
        [RuntimeImport(Redhawk.BaseName, "RhpNewFastAlign8")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object RhpNewFastAlign8(MethodTable * pEEType);  // BEWARE: not for finalizable objects!

        [RuntimeImport(Redhawk.BaseName, "RhpNewFinalizableAlign8")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object RhpNewFinalizableAlign8(MethodTable* pEEType);

        [RuntimeImport(Redhawk.BaseName, "RhpNewArrayAlign8")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object RhpNewArrayAlign8(MethodTable* pEEType, int length);

        [RuntimeImport(Redhawk.BaseName, "RhpNewFastMisalign")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object RhpNewFastMisalign(MethodTable * pEEType);
#endif // FEATURE_64BIT_ALIGNMENT

        [RuntimeImport(Redhawk.BaseName, "RhpCopyObjectContents")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void RhpCopyObjectContents(object objDest, object objSrc);

        [RuntimeImport(Redhawk.BaseName, "RhpAssignRef")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void RhpAssignRef(ref object address, object obj);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(Redhawk.BaseName, "RhpInitMultibyte")]
        internal static extern unsafe ref byte RhpInitMultibyte(ref byte dmem, int c, nuint size);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(Redhawk.BaseName, "memmove")]
        internal static extern unsafe void* memmove(byte* dmem, byte* smem, nuint size);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(Redhawk.BaseName, "RhBulkMoveWithWriteBarrier")]
        internal static extern unsafe void RhBulkMoveWithWriteBarrier(ref byte dmem, ref byte smem, nuint size);

#if FEATURE_GC_STRESS
        //
        // internal calls for GC stress
        //
        [RuntimeImport(Redhawk.BaseName, "RhpInitializeGcStress")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void RhpInitializeGcStress();
#endif // FEATURE_GC_STRESS

        [RuntimeImport(Redhawk.BaseName, "RhpEHEnumInitFromStackFrameIterator")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool RhpEHEnumInitFromStackFrameIterator(ref StackFrameIterator pFrameIter, byte** pMethodStartAddress, void* pEHEnum);

        [RuntimeImport(Redhawk.BaseName, "RhpEHEnumNext")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool RhpEHEnumNext(void* pEHEnum, void* pEHClause);

        [RuntimeImport(Redhawk.BaseName, "RhpGetDispatchCellInfo")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void RhpGetDispatchCellInfo(IntPtr pCell, out DispatchCellInfo newCellInfo);

        [RuntimeImport(Redhawk.BaseName, "RhpSearchDispatchCellCache")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe IntPtr RhpSearchDispatchCellCache(IntPtr pCell, MethodTable* pInstanceType);

        [RuntimeImport(Redhawk.BaseName, "RhpUpdateDispatchCellCache")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe IntPtr RhpUpdateDispatchCellCache(IntPtr pCell, IntPtr pTargetCode, MethodTable* pInstanceType, ref DispatchCellInfo newCellInfo);

        [RuntimeImport(Redhawk.BaseName, "RhpGetClasslibFunctionFromCodeAddress")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void* RhpGetClasslibFunctionFromCodeAddress(IntPtr address, ClassLibFunctionId id);

        [RuntimeImport(Redhawk.BaseName, "RhpGetClasslibFunctionFromEEType")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void* RhpGetClasslibFunctionFromEEType(MethodTable* pEEType, ClassLibFunctionId id);

        //
        // StackFrameIterator
        //

        [RuntimeImport(Redhawk.BaseName, "RhpSfiInit")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool RhpSfiInit(ref StackFrameIterator pThis, void* pStackwalkCtx, bool instructionFault);

        [RuntimeImport(Redhawk.BaseName, "RhpSfiNext")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool RhpSfiNext(ref StackFrameIterator pThis, uint* uExCollideClauseIdx, bool* fUnwoundReversePInvoke);

        //
        // Miscellaneous helpers.
        //

        [RuntimeImport(Redhawk.BaseName, "RhpCallCatchFunclet")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe IntPtr RhpCallCatchFunclet(
            object exceptionObj, byte* pHandlerIP, void* pvRegDisplay, ref EH.ExInfo exInfo);

        [RuntimeImport(Redhawk.BaseName, "RhpCallFinallyFunclet")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void RhpCallFinallyFunclet(byte* pHandlerIP, void* pvRegDisplay);

        [RuntimeImport(Redhawk.BaseName, "RhpCallFilterFunclet")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool RhpCallFilterFunclet(
            object exceptionObj, byte* pFilterIP, void* pvRegDisplay);

        [RuntimeImport(Redhawk.BaseName, "RhpFallbackFailFast")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void RhpFallbackFailFast();

        [RuntimeImport(Redhawk.BaseName, "RhpSetThreadDoNotTriggerGC")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void RhpSetThreadDoNotTriggerGC();

        [System.Diagnostics.Conditional("DEBUG")]
        [RuntimeImport(Redhawk.BaseName, "RhpValidateExInfoStack")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void RhpValidateExInfoStack();

        [RuntimeImport(Redhawk.BaseName, "RhpCopyContextFromExInfo")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void RhpCopyContextFromExInfo(void* pOSContext, int cbOSContext, EH.PAL_LIMITED_CONTEXT* pPalContext);

        [RuntimeImport(Redhawk.BaseName, "RhpGetNumThunkBlocksPerMapping")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int RhpGetNumThunkBlocksPerMapping();

        [RuntimeImport(Redhawk.BaseName, "RhpGetNumThunksPerBlock")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int RhpGetNumThunksPerBlock();

        [RuntimeImport(Redhawk.BaseName, "RhpGetThunkSize")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int RhpGetThunkSize();

        [RuntimeImport(Redhawk.BaseName, "RhpGetThunkDataBlockAddress")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr RhpGetThunkDataBlockAddress(IntPtr thunkStubAddress);

        [RuntimeImport(Redhawk.BaseName, "RhpGetThunkStubsBlockAddress")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr RhpGetThunkStubsBlockAddress(IntPtr thunkDataAddress);

        [RuntimeImport(Redhawk.BaseName, "RhpGetThunkBlockSize")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int RhpGetThunkBlockSize();

        [RuntimeImport(Redhawk.BaseName, "RhpGetThreadAbortException")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern Exception RhpGetThreadAbortException();

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
        [DllImport(Redhawk.BaseName)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
        internal static extern uint RhpWaitForFinalizerRequest();

        // Indicate that the current round of finalizations is complete.
        [DllImport(Redhawk.BaseName)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
        internal static extern void RhpSignalFinalizationComplete();

        [DllImport(Redhawk.BaseName)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
        internal static extern void RhpAcquireCastCacheLock();

        [DllImport(Redhawk.BaseName)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
        internal static extern void RhpReleaseCastCacheLock();

        [DllImport(Redhawk.BaseName)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
        internal static extern ulong RhpGetTickCount64();

        [DllImport(Redhawk.BaseName)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
        internal static extern void RhpAcquireThunkPoolLock();

        [DllImport(Redhawk.BaseName)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
        internal static extern void RhpReleaseThunkPoolLock();

        [DllImport(Redhawk.BaseName)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
        internal static extern IntPtr RhAllocateThunksMapping();

        // Enters a no GC region, possibly doing a blocking GC if there is not enough
        // memory available to satisfy the caller's request.
        [DllImport(Redhawk.BaseName)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
        internal static extern int RhpStartNoGCRegion(long totalSize, Interop.BOOL hasLohSize, long lohSize, Interop.BOOL disallowFullBlockingGC);

        // Exits a no GC region, possibly doing a GC to clean up the garbage that
        // the caller allocated.
        [DllImport(Redhawk.BaseName)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
        internal static extern int RhpEndNoGCRegion();
    }
}
