// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.Runtime;
using Internal.Runtime.CompilerServices;

using CorElementType = System.Reflection.CorElementType;

namespace System.Runtime
{
    // CONTRACT with Runtime
    // This class lists all the static methods that the redhawk runtime exports to a class library
    // These are not expected to change much but are needed by the class library to implement its functionality
    //
    //      The contents of this file can be modified if needed by the class library
    //      E.g., the class and methods are marked internal assuming that only the base class library needs them
    //            but if a class library wants to factor differently (such as putting the GCHandle methods in an
    //            optional library, those methods can be moved to a different file/namespace/dll
    internal static partial class RuntimeImports
    {
        private const string RuntimeLibrary = "*";

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetCrashInfoBuffer")]
        internal static extern unsafe byte* RhGetCrashInfoBuffer(out int cbMaxSize);

#if TARGET_UNIX
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhCreateCrashDumpIfEnabled")]
        internal static extern void RhCreateCrashDumpIfEnabled(IntPtr pExceptionRecord, IntPtr pContextRecord);
#endif

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetRuntimeVersion")]
        internal static extern unsafe byte* RhGetRuntimeVersion(out int cbLength);

        [LibraryImport(RuntimeLibrary)]
        [SuppressGCTransition]
        internal static partial ulong RhpGetTickCount64();

        [LibraryImport(RuntimeLibrary)]
        internal static partial IntPtr RhpGetCurrentThread();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpInitiateThreadAbort")]
        internal static extern void RhpInitiateThreadAbort(IntPtr thread, Exception exception, bool doRudeAbort);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpCancelThreadAbort")]
        internal static extern void RhpCancelThreadAbort(IntPtr thread);

        //
        // calls to GC
        // These methods are needed to implement System.GC like functionality (optional)
        //

        // Force a garbage collection.
        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhCollect")]
        internal static extern void RhCollect(int generation, InternalGCCollectionMode mode, bool lowMemoryP = false);

        // Mark an object instance as already finalized.
        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhSuppressFinalize")]
        internal static extern void RhSuppressFinalize(object obj);

        internal static void RhReRegisterForFinalize(object obj)
        {
            if (!_RhReRegisterForFinalize(obj))
                throw new OutOfMemoryException();
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhReRegisterForFinalize")]
        private static extern bool _RhReRegisterForFinalize(object obj);

        // Wait for all pending finalizers. This must be a p/invoke to avoid starving the GC.
        [LibraryImport(RuntimeLibrary)]
        private static partial void RhWaitForPendingFinalizers(int allowReentrantWait);

        // Temporary workaround to unblock shareable assembly bring-up - without shared interop,
        // we must prevent RhWaitForPendingFinalizers from using marshaling because it would
        // rewrite System.Private.CoreLib to reference the non-shareable interop assembly. With shared interop,
        // we will be able to remove this helper method and change the DllImport above
        // to directly accept a boolean parameter.
        internal static void RhWaitForPendingFinalizers(bool allowReentrantWait)
        {
            RhWaitForPendingFinalizers(allowReentrantWait ? 1 : 0);
        }

        [LibraryImport(RuntimeLibrary)]
        internal static partial void RhInitializeFinalizerThread();

        // Get maximum GC generation number.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetMaxGcGeneration")]
        internal static extern int RhGetMaxGcGeneration();

        // Get count of collections so far.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetGcCollectionCount")]
        internal static extern int RhGetGcCollectionCount(int generation, bool getSpecialGCCount);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetGeneration")]
        internal static extern int RhGetGeneration(object obj);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetGenerationSize")]
        internal static extern int RhGetGenerationSize(int gen);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetLastGCPercentTimeInGC")]
        internal static extern int RhGetLastGCPercentTimeInGC();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetGcLatencyMode")]
        internal static extern GCLatencyMode RhGetGcLatencyMode();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhSetGcLatencyMode")]
        internal static extern int RhSetGcLatencyMode(GCLatencyMode newLatencyMode);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhIsPromoted")]
        internal static extern bool RhIsPromoted(object obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhIsServerGc")]
        internal static extern bool RhIsServerGc();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetGcTotalMemory")]
        internal static extern long RhGetGcTotalMemory();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetLohCompactionMode")]
        internal static extern int RhGetLohCompactionMode();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhSetLohCompactionMode")]
        internal static extern void RhSetLohCompactionMode(int newLohCompactionMode);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetCurrentObjSize")]
        internal static extern long RhGetCurrentObjSize();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetGCNow")]
        internal static extern long RhGetGCNow();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetLastGCStartTime")]
        internal static extern long RhGetLastGCStartTime(int generation);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetLastGCDuration")]
        internal static extern long RhGetLastGCDuration(int generation);

        [LibraryImport(RuntimeLibrary)]
        internal static unsafe partial IntPtr RhRegisterFrozenSegment(void* pSegmentStart, nuint allocSize, nuint commitSize, nuint reservedSize);

        [LibraryImport(RuntimeLibrary)]
        internal static unsafe partial void RhUpdateFrozenSegment(IntPtr seg, void* allocated, void* committed);

        [LibraryImport(RuntimeLibrary)]
        internal static partial void RhUnregisterFrozenSegment(IntPtr pSegmentHandle);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhRegisterForFullGCNotification")]
        internal static extern bool RhRegisterForFullGCNotification(int maxGenerationThreshold, int largeObjectHeapThreshold);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhWaitForFullGCApproach")]
        internal static extern int RhWaitForFullGCApproach(int millisecondsTimeout);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhWaitForFullGCComplete")]
        internal static extern int RhWaitForFullGCComplete(int millisecondsTimeout);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhCancelFullGCNotification")]
        internal static extern bool RhCancelFullGCNotification();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhStartNoGCRegion")]
        internal static extern int RhStartNoGCRegion(long totalSize, bool hasLohSize, long lohSize, bool disallowFullBlockingGC);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhEndNoGCRegion")]
        internal static extern int RhEndNoGCRegion();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetGCSegmentSize")]
        internal static extern ulong RhGetGCSegmentSize();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetAllocatedBytesForCurrentThread")]
        internal static extern long RhGetAllocatedBytesForCurrentThread();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetTotalAllocatedBytes")]
        internal static extern long RhGetTotalAllocatedBytes();

        internal enum GCConfigurationType
        {
            Int64,
            StringUtf8,
            Boolean
        }

        [LibraryImport(RuntimeLibrary)]
        internal static unsafe partial void RhEnumerateConfigurationValues(void* configurationContext, delegate* unmanaged<void*, void*, void*, GCConfigurationType, long, void> callback);

        internal struct GCHeapHardLimitInfo
        {
            internal ulong HeapHardLimit;
            internal ulong HeapHardLimitPercent;
            internal ulong HeapHardLimitSOH;
            internal ulong HeapHardLimitLOH;
            internal ulong HeapHardLimitPOH;
            internal ulong HeapHardLimitSOHPercent;
            internal ulong HeapHardLimitLOHPercent;
            internal ulong HeapHardLimitPOHPercent;
        }

        [LibraryImport(RuntimeLibrary)]
        internal static partial int RhRefreshMemoryLimit(GCHeapHardLimitInfo heapHardLimitInfo);

        [LibraryImport(RuntimeLibrary)]
        internal static unsafe partial int RhEnableNoGCRegionCallback(void* callback, long totalSize);

        [LibraryImport(RuntimeLibrary)]
        internal static partial long RhGetGenerationBudget(int generation);

        [LibraryImport(RuntimeLibrary)]
        internal static partial long RhGetTotalAllocatedBytesPrecise();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetMemoryInfo")]
        internal static extern void RhGetMemoryInfo(ref byte info, GCKind kind);

        [LibraryImport(RuntimeLibrary)]
        internal static unsafe partial void RhAllocateNewArray(MethodTable* pArrayEEType, uint numElements, uint flags, void* pResult);

        [LibraryImport(RuntimeLibrary)]
        internal static unsafe partial void RhAllocateNewObject(IntPtr pEEType, uint flags, void* pResult);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetTotalPauseDuration")]
        internal static extern long RhGetTotalPauseDuration();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhCompareObjectContentsAndPadding")]
        internal static extern bool RhCompareObjectContentsAndPadding(object obj1, object obj2);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetProcessCpuCount")]
        internal static extern int RhGetProcessCpuCount();

        //
        // calls for GCHandle.
        // These methods are needed to implement GCHandle class like functionality (optional)
        //

        // Allocate handle.
        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpHandleAlloc")]
        private static extern IntPtr RhpHandleAlloc(object value, GCHandleType type);

        internal static IntPtr RhHandleAlloc(object value, GCHandleType type)
        {
            IntPtr h = RhpHandleAlloc(value, type);
            if (h == IntPtr.Zero)
                throw new OutOfMemoryException();
            return h;
        }

        internal static IntPtr RhHandleAllocRefCounted(object value)
        {
            const int HNDTYPE_REFCOUNTED = 5;
            return RhHandleAlloc(value, (GCHandleType)HNDTYPE_REFCOUNTED);
        }

        // Allocate handle for dependent handle case where a secondary can be set at the same time.
        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpHandleAllocDependent")]
        internal static extern IntPtr RhpHandleAllocDependent(object primary, object secondary);

        internal static IntPtr RhHandleAllocDependent(object primary, object secondary)
        {
            IntPtr h = RhpHandleAllocDependent(primary, secondary);
            if (h == IntPtr.Zero)
                throw new OutOfMemoryException();
            return h;
        }

        // Free handle.
        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhHandleFree")]
        internal static extern void RhHandleFree(IntPtr handle);

        // Get object reference from handle.
        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhHandleGet")]
        private static extern object? _RhHandleGet(IntPtr handle);

        internal static unsafe object? RhHandleGet(IntPtr handle)
        {
#if DEBUG
            // The runtime performs additional checks in debug builds
            return _RhHandleGet(handle);
#else
            return Unsafe.As<IntPtr, object?>(ref *(IntPtr*)(nint)handle);
#endif
        }

        // Get primary and secondary object references from dependent handle.
        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhHandleGetDependent")]
        internal static extern object RhHandleGetDependent(IntPtr handle, out object secondary);

        // Set object reference into handle.
        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhHandleSet")]
        internal static extern void RhHandleSet(IntPtr handle, object? value);

        // Set the secondary object reference into a dependent handle.
        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhHandleSetDependentSecondary")]
        internal static extern void RhHandleSetDependentSecondary(IntPtr handle, object secondary);

        //
        // calls to runtime for thunk pool
        //

        [RuntimeImport(RuntimeLibrary, "RhpGetNumThunkBlocksPerMapping")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int RhpGetNumThunkBlocksPerMapping();

        [RuntimeImport(RuntimeLibrary, "RhpGetNumThunksPerBlock")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int RhpGetNumThunksPerBlock();

        [RuntimeImport(RuntimeLibrary, "RhpGetThunkSize")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int RhpGetThunkSize();

        [RuntimeImport(RuntimeLibrary, "RhpGetThunkDataBlockAddress")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr RhpGetThunkDataBlockAddress(IntPtr thunkStubAddress);

        [RuntimeImport(RuntimeLibrary, "RhpGetThunkStubsBlockAddress")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr RhpGetThunkStubsBlockAddress(IntPtr thunkDataAddress);

        [RuntimeImport(RuntimeLibrary, "RhpGetThunkBlockSize")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int RhpGetThunkBlockSize();

        [LibraryImport(RuntimeLibrary, EntryPoint = "RhAllocateThunksMapping")]
        internal static partial IntPtr RhAllocateThunksMapping();

        //
        // calls to runtime for type equality checks
        //

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhTypeCast_AreTypesAssignable")]
        internal static extern unsafe bool AreTypesAssignable(MethodTable* pSourceType, MethodTable* pTargetType);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhTypeCast_CheckArrayStore")]
        internal static extern void RhCheckArrayStore(object array, object? obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhTypeCast_IsInstanceOfAny")]
        internal static extern unsafe object IsInstanceOf(MethodTable* pTargetType, object obj);

        //
        // calls to runtime for allocation
        // These calls are needed in types which cannot use "new" to allocate and need to do it manually
        //
        // calls to runtime for allocation
        //
        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhBoxAny")]
        internal static extern unsafe object RhBoxAny(ref byte pData, MethodTable* pEEType);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhNewObject")]
        internal static extern unsafe object RhNewObject(MethodTable* pEEType);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhNewArray")]
        internal static extern unsafe Array RhNewArray(MethodTable* pEEType, int length);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhNewString")]
        internal static extern unsafe string RhNewString(MethodTable* pEEType, int length);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhBox")]
        internal static extern unsafe object RhBox(MethodTable* pEEType, ref byte data);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhUnbox")]
        internal static extern unsafe void RhUnbox(object? obj, ref byte data, MethodTable* pUnboxToEEType);

        // Busy spin for the given number of iterations.
        [LibraryImport(RuntimeLibrary, EntryPoint = "RhSpinWait")]
        [SuppressGCTransition]
        internal static partial void RhSpinWait(int iterations);

        // Call RhSpinWait with a GC transition
        [LibraryImport(RuntimeLibrary, EntryPoint = "RhSpinWait")]
        internal static partial void RhLongSpinWait(int iterations);

        // Yield the cpu to another thread ready to process, if one is available.
        [LibraryImport(RuntimeLibrary, EntryPoint = "RhYield")]
        private static partial int _RhYield();
        internal static bool RhYield() { return (_RhYield() != 0); }

        [LibraryImport(RuntimeLibrary, EntryPoint = "RhFlushProcessWriteBuffers")]
        internal static partial void RhFlushProcessWriteBuffers();

#if !TARGET_UNIX
        // Wait for any object to be signalled, in a way that's compatible with the CLR's behavior in an STA.
        // ExactSpelling = 'true' to force MCG to resolve it to default
        [LibraryImport(RuntimeLibrary)]
        private static unsafe partial int RhCompatibleReentrantWaitAny(int alertable, int timeout, int count, IntPtr* handles);

        // Temporary workaround to unblock shareable assembly bring-up - without shared interop,
        // we must prevent RhCompatibleReentrantWaitAny from using marshaling because it would
        // rewrite System.Private.CoreLib to reference the non-shareable interop assembly. With shared interop,
        // we will be able to remove this helper method and change the DllImport above
        // to directly accept a boolean parameter and use the SetLastError = true modifier.
        internal static unsafe int RhCompatibleReentrantWaitAny(bool alertable, int timeout, int count, IntPtr* handles)
        {
            return RhCompatibleReentrantWaitAny(alertable ? 1 : 0, timeout, count, handles);
        }
#endif

        //
        // MethodTable interrogation methods.
        //

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetGCDescSize")]
        internal static extern unsafe int RhGetGCDescSize(MethodTable* eeType);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhNewInterfaceDispatchCell")]
        internal static extern unsafe IntPtr RhNewInterfaceDispatchCell(MethodTable* pEEType, int slotNumber);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhResolveDispatch")]
        internal static extern unsafe IntPtr RhResolveDispatch(object pObject, MethodTable* pInterfaceType, ushort slot);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpResolveInterfaceMethod")]
        internal static extern IntPtr RhpResolveInterfaceMethod(object pObject, IntPtr pCell);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhResolveDispatchOnType")]
        internal static extern unsafe IntPtr RhResolveDispatchOnType(MethodTable* instanceType, MethodTable* interfaceType, ushort slot, MethodTable** pGenericContext);

        internal static unsafe IntPtr RhResolveDispatchOnType(MethodTable* instanceType, MethodTable* interfaceType, ushort slot)
        {
            return RhResolveDispatchOnType(instanceType, interfaceType, slot, null);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetRuntimeHelperForType")]
        internal static extern unsafe IntPtr RhGetRuntimeHelperForType(MethodTable* pEEType, RuntimeHelperKind kind);

        //
        // Support for GC and HandleTable callouts.
        //

        internal enum GcRestrictedCalloutKind
        {
            StartCollection = 0, // Collection is about to begin
            EndCollection = 1, // Collection has completed
            AfterMarkPhase = 2, // All live objects are marked (not including ready for finalization objects),
                                // no handles have been cleared
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhRegisterGcCallout")]
        internal static extern bool RhRegisterGcCallout(GcRestrictedCalloutKind eKind, IntPtr pCalloutMethod);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhUnregisterGcCallout")]
        internal static extern void RhUnregisterGcCallout(GcRestrictedCalloutKind eKind, IntPtr pCalloutMethod);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhRegisterRefCountedHandleCallback")]
        internal static extern unsafe bool RhRegisterRefCountedHandleCallback(IntPtr pCalloutMethod, MethodTable* pTypeFilter);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhUnregisterRefCountedHandleCallback")]
        internal static extern unsafe void RhUnregisterRefCountedHandleCallback(IntPtr pCalloutMethod, MethodTable* pTypeFilter);

#if FEATURE_OBJCMARSHAL
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhRegisterObjectiveCMarshalBeginEndCallback")]
        internal static extern bool RhRegisterObjectiveCMarshalBeginEndCallback(IntPtr pCalloutMethod);
#endif

        //
        // Blob support
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhFindBlob")]
        private static extern unsafe bool RhFindBlob(ref TypeManagerHandle typeManagerHandle, uint blobId, byte** ppbBlob, uint* pcbBlob);

        internal static unsafe bool RhFindBlob(TypeManagerHandle typeManagerHandle, uint blobId, byte** ppbBlob, uint* pcbBlob)
        {
            return RhFindBlob(ref typeManagerHandle, blobId, ppbBlob, pcbBlob);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpCreateTypeManager")]
        internal static extern unsafe TypeManagerHandle RhpCreateTypeManager(IntPtr osModule, IntPtr moduleHeader, IntPtr* pClasslibFunctions, int nClasslibFunctions);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpRegisterOsModule")]
        internal static extern unsafe IntPtr RhpRegisterOsModule(IntPtr osModule);

        [RuntimeImport(RuntimeLibrary, "RhpGetModuleSection")]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr RhGetModuleSection(ref TypeManagerHandle module, ReadyToRunSectionType section, out int length);

        internal static IntPtr RhGetModuleSection(TypeManagerHandle module, ReadyToRunSectionType section, out int length)
        {
            return RhGetModuleSection(ref module, section, out length);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetLoadedOSModules")]
        internal static extern uint RhGetLoadedOSModules(IntPtr[] resultArray);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetKnobValues")]
        internal static extern unsafe uint RhGetKnobValues(out byte** keyArray, out byte** valueArray);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetOSModuleFromPointer")]
        internal static extern IntPtr RhGetOSModuleFromPointer(IntPtr pointerVal);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetThreadStaticStorage")]
        internal static extern ref object[][] RhGetThreadStaticStorage();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhRegisterInlinedThreadStaticRoot")]
        internal static extern void RhRegisterInlinedThreadStaticRoot(ref object? root, TypeManagerHandle module);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhCurrentNativeThreadId")]
        internal static extern unsafe IntPtr RhCurrentNativeThreadId();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhCurrentOSThreadId")]
        internal static extern unsafe ulong RhCurrentOSThreadId();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport("*", "RhGetCurrentThunkContext")]
        internal static extern IntPtr GetCurrentInteropThunkContext();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport("*", "RhGetCommonStubAddress")]
        internal static extern IntPtr GetInteropCommonStubAddress();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetCodeTarget")]
        public static extern IntPtr RhGetCodeTarget(IntPtr pCode);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetTargetOfUnboxingAndInstantiatingStub")]
        public static extern IntPtr RhGetTargetOfUnboxingAndInstantiatingStub(IntPtr pCode);

        //
        // EH helpers
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetModuleFileName")]
#if TARGET_UNIX
        internal static extern unsafe int RhGetModuleFileName(IntPtr moduleHandle, out byte* moduleName);
#else
        internal static extern unsafe int RhGetModuleFileName(IntPtr moduleHandle, out char* moduleName);
#endif

        //
        // StackTrace helper
        //

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhFindMethodStartAddress")]
        internal static extern unsafe IntPtr RhFindMethodStartAddress(IntPtr codeAddr);

        // Fetch a (managed) stack trace.  Fills in the given array with "return address IPs" for the current
        // thread's (managed) stack (array index 0 will be the caller of this method).  The return value is
        // the number of frames in the stack or a negative number (representing the required array size) if
        // the passed-in buffer is too small.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetCurrentThreadStackTrace")]
        internal static extern int RhGetCurrentThreadStackTrace(IntPtr[] outputBuffer);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetCurrentThreadStackBounds")]
        internal static extern void RhGetCurrentThreadStackBounds(out IntPtr pStackLow, out IntPtr pStackHigh);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhSetThreadExitCallback")]
        internal static extern unsafe void RhSetThreadExitCallback(delegate* unmanaged<void> pCallback);

        // Moves memory from smem to dmem. Size must be a positive value.
        // This copy uses an intrinsic to be safe for copying arbitrary bits of
        // heap memory
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhBulkMoveWithWriteBarrier")]
        internal static extern unsafe void RhBulkMoveWithWriteBarrier(ref byte dmem, ref byte smem, nuint size);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhRegisterForGCReporting")]
        internal static extern unsafe void RhRegisterForGCReporting(GCFrameRegistration* pRegistration);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhUnregisterForGCReporting")]
        internal static extern unsafe void RhUnregisterForGCReporting(GCFrameRegistration* pRegistration);

#if FEATURE_PERFTRACING
        [LibraryImport(RuntimeLibrary)]
        internal static unsafe partial void NativeRuntimeEventSource_LogExceptionThrown(char* exceptionTypeName, char* exceptionMessage, IntPtr faultingIP, int hresult);
#endif // FEATURE_PERFTRACING

        //
        // Interlocked helpers
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpLockCmpXchg32")]
        internal static extern int InterlockedCompareExchange(ref int location1, int value, int comparand);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpLockCmpXchg64")]
        internal static extern long InterlockedCompareExchange(ref long location1, long value, long comparand);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpCheckedLockCmpXchg")]
        internal static extern object InterlockedCompareExchange(ref object? location1, object? value, object? comparand);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpCheckedXchg")]
        internal static extern object InterlockedExchange([NotNullIfNotNull(nameof(value))] ref object? location1, object? value);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "acos")]
        internal static extern double acos(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "acosf")]
        internal static extern float acosf(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "acosh")]
        internal static extern double acosh(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "acoshf")]
        internal static extern float acoshf(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "asin")]
        internal static extern double asin(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "asinf")]
        internal static extern float asinf(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "asinh")]
        internal static extern double asinh(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "asinhf")]
        internal static extern float asinhf(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "atan")]
        internal static extern double atan(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "atanf")]
        internal static extern float atanf(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "atan2")]
        internal static extern double atan2(double y, double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "atan2f")]
        internal static extern float atan2f(float y, float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "atanh")]
        internal static extern double atanh(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "atanhf")]
        internal static extern float atanhf(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "cbrt")]
        internal static extern double cbrt(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "cbrtf")]
        internal static extern float cbrtf(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "ceil")]
        internal static extern double ceil(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "ceilf")]
        internal static extern float ceilf(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "cos")]
        internal static extern double cos(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "cosf")]
        internal static extern float cosf(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "cosh")]
        internal static extern double cosh(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "coshf")]
        internal static extern float coshf(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "exp")]
        internal static extern double exp(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "expf")]
        internal static extern float expf(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "floor")]
        internal static extern double floor(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "floorf")]
        internal static extern float floorf(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "log")]
        internal static extern double log(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "logf")]
        internal static extern float logf(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "log2")]
        internal static extern double log2(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "log2f")]
        internal static extern float log2f(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "log10")]
        internal static extern double log10(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "log10f")]
        internal static extern float log10f(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "pow")]
        internal static extern double pow(double x, double y);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "powf")]
        internal static extern float powf(float x, float y);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "sin")]
        internal static extern double sin(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "sinf")]
        internal static extern float sinf(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "sinh")]
        internal static extern double sinh(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "sinhf")]
        internal static extern float sinhf(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "sqrt")]
        internal static extern double sqrt(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "sqrtf")]
        internal static extern float sqrtf(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "tan")]
        internal static extern double tan(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "tanf")]
        internal static extern float tanf(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "tanh")]
        internal static extern double tanh(double x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "tanhf")]
        internal static extern float tanhf(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "fmod")]
        internal static extern double fmod(double x, double y);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "fmodf")]
        internal static extern float fmodf(float x, float y);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "fma")]
        internal static extern double fma(double x, double y, double z);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "fmaf")]
        internal static extern float fmaf(float x, float y, float z);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "modf")]
        internal static extern unsafe double modf(double x, double* intptr);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "modff")]
        internal static extern unsafe float modff(float x, float* intptr);

        [LibraryImport(RuntimeImports.RuntimeLibrary)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void* memmove(byte* dmem, byte* smem, nuint size);

        [LibraryImport(RuntimeImports.RuntimeLibrary)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void* memset(byte* mem, int value, nuint size);

#if TARGET_X86 || TARGET_AMD64
        [LibraryImport(RuntimeLibrary)]
        internal static unsafe partial void RhCpuIdEx(int* cpuInfo, int functionId, int subFunctionId);
#endif
    }
}
