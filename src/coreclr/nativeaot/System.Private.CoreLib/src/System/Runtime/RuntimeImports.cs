// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

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
    [ReflectionBlocked]
    public static class RuntimeImports
    {
        private const string RuntimeLibrary = "*";

        [DllImport(RuntimeLibrary, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        [SuppressGCTransition]
        internal static extern ulong RhpGetTickCount64();

        [DllImport(RuntimeLibrary, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr RhpGetCurrentThread();

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
        internal static extern void RhCollect(int generation, InternalGCCollectionMode mode);

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
        [DllImport(RuntimeLibrary, ExactSpelling = true)]
        private static extern void RhWaitForPendingFinalizers(int allowReentrantWait);

        // Temporary workaround to unblock shareable assembly bring-up - without shared interop,
        // we must prevent RhWaitForPendingFinalizers from using marshaling because it would
        // rewrite System.Private.CoreLib to reference the non-shareable interop assembly. With shared interop,
        // we will be able to remove this helper method and change the DllImport above
        // to directly accept a boolean parameter.
        internal static void RhWaitForPendingFinalizers(bool allowReentrantWait)
        {
            RhWaitForPendingFinalizers(allowReentrantWait ? 1 : 0);
        }

        [DllImport(RuntimeLibrary, ExactSpelling = true)]
        internal static extern void RhInitializeFinalizerThread();

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

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetGcLatencyMode")]
        internal static extern GCLatencyMode RhGetGcLatencyMode();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhSetGcLatencyMode")]
        internal static extern int RhSetGcLatencyMode(GCLatencyMode newLatencyMode);

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

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpRegisterFrozenSegment")]
        internal static extern IntPtr RhpRegisterFrozenSegment(IntPtr pSegmentStart, IntPtr length);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpUnregisterFrozenSegment")]
        internal static extern void RhpUnregisterFrozenSegment(IntPtr pSegmentHandle);

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
        [RuntimeImport(RuntimeLibrary, "RhpShutdown")]
        internal static extern void RhpShutdown();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetGCSegmentSize")]
        internal static extern ulong RhGetGCSegmentSize();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetAllocatedBytesForCurrentThread")]
        internal static extern long RhGetAllocatedBytesForCurrentThread();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetTotalAllocatedBytes")]
        internal static extern long RhGetTotalAllocatedBytes();

        [DllImport(RuntimeLibrary, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern long RhGetTotalAllocatedBytesPrecise();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetMemoryInfo")]
        internal static extern void RhGetMemoryInfo(ref byte info, GCKind kind);

        [DllImport(RuntimeLibrary, ExactSpelling = true)]
        internal static unsafe extern void RhAllocateNewArray(IntPtr pArrayEEType, uint numElements, uint flags, void* pResult);

        [DllImport(RuntimeLibrary, ExactSpelling = true)]
        internal static unsafe extern void RhAllocateNewObject(IntPtr pEEType, uint flags, void* pResult);

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

        // Allocate handle for dependent handle case where a secondary can be set at the same time.
        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpHandleAllocDependent")]
        private static extern IntPtr RhpHandleAllocDependent(object primary, object secondary);

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
        private static extern object _RhHandleGet(IntPtr handle);

        internal static unsafe object RhHandleGet(IntPtr handle)
        {
#if DEBUG
            // The runtime performs additional checks in debug builds
            return _RhHandleGet(handle);
#else
            return Unsafe.As<IntPtr, object>(ref *(IntPtr*)(nint)handle);
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
        // calls to runtime for type equality checks
        //

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhTypeCast_AreTypesEquivalent")]
        private static unsafe extern bool AreTypesEquivalent(MethodTable* pType1, MethodTable* pType2);

        internal static unsafe bool AreTypesEquivalent(EETypePtr pType1, EETypePtr pType2)
            => AreTypesEquivalent(pType1.ToPointer(), pType2.ToPointer());

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhTypeCast_AreTypesAssignable")]
        private static unsafe extern bool AreTypesAssignable(MethodTable* pSourceType, MethodTable* pTargetType);

        internal static unsafe bool AreTypesAssignable(EETypePtr pSourceType, EETypePtr pTargetType)
            => AreTypesAssignable(pSourceType.ToPointer(), pTargetType.ToPointer());

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhTypeCast_CheckArrayStore")]
        internal static extern void RhCheckArrayStore(object array, object? obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhTypeCast_IsInstanceOf")]
        private static unsafe extern object IsInstanceOf(MethodTable* pTargetType, object obj);

        internal static unsafe object IsInstanceOf(EETypePtr pTargetType, object obj)
            => IsInstanceOf(pTargetType.ToPointer(), obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhTypeCast_IsInstanceOfClass")]
        private  static unsafe extern object IsInstanceOfClass(MethodTable* pTargetType, object obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhTypeCast_IsInstanceOfInterface")]
        internal static unsafe extern object IsInstanceOfInterface(MethodTable* pTargetType, object obj);

        internal static unsafe object IsInstanceOfInterface(EETypePtr pTargetType, object obj)
            => IsInstanceOfInterface(pTargetType.ToPointer(), obj);

        //
        // calls to runtime for allocation
        // These calls are needed in types which cannot use "new" to allocate and need to do it manually
        //
        // calls to runtime for allocation
        //
        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhBoxAny")]
        private static unsafe extern object RhBoxAny(ref byte pData, MethodTable* pEEType);

        internal static unsafe object RhBoxAny(ref byte pData, EETypePtr pEEType)
            => RhBoxAny(ref pData, pEEType.ToPointer());

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhNewObject")]
        private static unsafe extern object RhNewObject(MethodTable* pEEType);

        internal static unsafe object RhNewObject(EETypePtr pEEType)
            => RhNewObject(pEEType.ToPointer());

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhNewArray")]
        private static unsafe extern Array RhNewArray(MethodTable* pEEType, int length);

        internal static unsafe Array RhNewArray(EETypePtr pEEType, int length)
            => RhNewArray(pEEType.ToPointer(), length);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhNewString")]
        internal static unsafe extern string RhNewString(MethodTable* pEEType, int length);

        internal static unsafe string RhNewString(EETypePtr pEEType, int length)
            => RhNewString(pEEType.ToPointer(), length);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhBox")]
        private static extern unsafe object RhBox(MethodTable* pEEType, ref byte data);

        internal static unsafe object RhBox(EETypePtr pEEType, ref byte data)
            => RhBox(pEEType.ToPointer(), ref data);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhUnbox")]
        private static extern unsafe void RhUnbox(object? obj, ref byte data, MethodTable* pUnboxToEEType);

        internal static unsafe void RhUnbox(object? obj, ref byte data, EETypePtr pUnboxToEEType)
            => RhUnbox(obj, ref data, pUnboxToEEType.ToPointer());

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhMemberwiseClone")]
        internal static extern object RhMemberwiseClone(object obj);

        // Busy spin for the given number of iterations.
        [DllImport(RuntimeLibrary, EntryPoint = "RhSpinWait", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [SuppressGCTransition]
        internal static extern void RhSpinWait(int iterations);

        // Yield the cpu to another thread ready to process, if one is available.
        [DllImport(RuntimeLibrary, EntryPoint = "RhYield", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern int _RhYield();
        internal static bool RhYield() { return (_RhYield() != 0); }

        [DllImport(RuntimeLibrary, EntryPoint = "RhFlushProcessWriteBuffers", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern void RhFlushProcessWriteBuffers();

#if !TARGET_UNIX
        // Wait for any object to be signalled, in a way that's compatible with the CLR's behavior in an STA.
        // ExactSpelling = 'true' to force MCG to resolve it to default
        [DllImport(RuntimeLibrary, ExactSpelling = true)]
        private static extern unsafe int RhCompatibleReentrantWaitAny(int alertable, int timeout, int count, IntPtr* handles);

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
        internal static extern int RhGetGCDescSize(EETypePtr eeType);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhNewInterfaceDispatchCell")]
        internal static extern unsafe IntPtr RhNewInterfaceDispatchCell(EETypePtr pEEType, int slotNumber);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhResolveDispatch")]
        internal static extern IntPtr RhResolveDispatch(object pObject, EETypePtr pInterfaceType, ushort slot);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpResolveInterfaceMethod")]
        internal static extern IntPtr RhpResolveInterfaceMethod(object pObject, IntPtr pCell);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhCreateThunksHeap")]
        internal static extern object RhCreateThunksHeap(IntPtr commonStubAddress);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhAllocateThunk")]
        internal static extern IntPtr RhAllocateThunk(object thunksHeap);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhFreeThunk")]
        internal static extern void RhFreeThunk(object thunksHeap, IntPtr thunkAddress);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhSetThunkData")]
        internal static extern void RhSetThunkData(object thunksHeap, IntPtr thunkAddress, IntPtr context, IntPtr target);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhTryGetThunkData")]
        internal static extern bool RhTryGetThunkData(object thunksHeap, IntPtr thunkAddress, out IntPtr context, out IntPtr target);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetThunkSize")]
        internal static extern int RhGetThunkSize();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetThreadLocalStorageForDynamicType")]
        internal static extern IntPtr RhGetThreadLocalStorageForDynamicType(int index, int tlsStorageSize, int numTlsCells);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhResolveDispatchOnType")]
        internal static extern IntPtr RhResolveDispatchOnType(EETypePtr instanceType, EETypePtr interfaceType, ushort slot);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetRuntimeHelperForType")]
        internal static extern unsafe IntPtr RhGetRuntimeHelperForType(EETypePtr pEEType, RuntimeHelperKind kind);

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
        internal static extern bool RhRegisterRefCountedHandleCallback(IntPtr pCalloutMethod, EETypePtr pTypeFilter);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhUnregisterRefCountedHandleCallback")]
        internal static extern void RhUnregisterRefCountedHandleCallback(IntPtr pCalloutMethod, EETypePtr pTypeFilter);

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
        [RuntimeImport(RuntimeLibrary, "RhGetOSModuleFromPointer")]
        internal static extern IntPtr RhGetOSModuleFromPointer(IntPtr pointerVal);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetModuleFromEEType")]
        internal static extern TypeManagerHandle RhGetModuleFromEEType(IntPtr pEEType);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetOSModuleFromEEType")]
        internal static extern IntPtr RhGetOSModuleFromEEType(IntPtr pEEType);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetThreadStaticStorageForModule")]
        internal static unsafe extern object[] RhGetThreadStaticStorageForModule(int moduleIndex);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhSetThreadStaticStorageForModule")]
        internal static unsafe extern bool RhSetThreadStaticStorageForModule(object[] storage, int moduleIndex);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhCurrentNativeThreadId")]
        internal static unsafe extern IntPtr RhCurrentNativeThreadId();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhCurrentOSThreadId")]
        internal static unsafe extern ulong RhCurrentOSThreadId();

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

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetExceptionsForCurrentThread")]
        internal static extern unsafe bool RhGetExceptionsForCurrentThread(Exception[] outputArray, out int writtenCountOut);

        // returns the previous value.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhSetErrorInfoBuffer")]
        internal static extern unsafe void* RhSetErrorInfoBuffer(void* pNewBuffer);

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

        // Functions involved in thunks from managed to managed functions (Universal transition transitions
        // from an arbitrary method call into a defined function, and CallDescrWorker goes the other way.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhGetUniversalTransitionThunk")]
        internal static extern IntPtr RhGetUniversalTransitionThunk();

        // For Managed to Managed calls
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhCallDescrWorker")]
        internal static extern void RhCallDescrWorker(IntPtr callDescr);

        // For Managed to Native calls
        [DllImport(RuntimeLibrary, EntryPoint = "RhCallDescrWorker", ExactSpelling = true)]
        internal static extern void RhCallDescrWorkerNative(IntPtr callDescr);

        // Moves memory from smem to dmem. Size must be a positive value.
        // This copy uses an intrinsic to be safe for copying arbitrary bits of
        // heap memory
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhBulkMoveWithWriteBarrier")]
        internal static extern unsafe void RhBulkMoveWithWriteBarrier(ref byte dmem, ref byte smem, nuint size);

        // The GC conservative reporting descriptor is a special structure of data that the GC
        // parses to determine whether there are specific regions of memory that it should not
        // collect or move around.
        // This can only be used to report memory regions on the current stack and the structure must itself
        // be located on the stack.
        // This structure is contractually required to be 4 pointers in size. All details about
        // the contents are abstracted into the runtime
        // To use, place one of these structures on the stack, and then pass it by ref to a function
        // which will pin the byref to create a pinned interior pointer.
        // Then, call RhInitializeConservativeReportingRegion to mark the region as conservatively reported.
        // When done, call RhDisableConservativeReportingRegion to disable conservative reporting, or
        // simply let it be pulled off the stack.
        internal struct ConservativelyReportedRegionDesc
        {
            private IntPtr _ptr1;
            private IntPtr _ptr2;
            private IntPtr _ptr3;
            private IntPtr _ptr4;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhInitializeConservativeReportingRegion")]
        internal static extern unsafe void RhInitializeConservativeReportingRegion(ConservativelyReportedRegionDesc* regionDesc, void* bufferBegin, int cbBuffer);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhDisableConservativeReportingRegion")]
        internal static extern unsafe void RhDisableConservativeReportingRegion(ConservativelyReportedRegionDesc* regionDesc);

        //
        // ETW helpers.
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpEtwExceptionThrown")]
        internal static extern unsafe void RhpEtwExceptionThrown(char* exceptionTypeName, char* exceptionMessage, IntPtr faultingIP, long hresult);

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
        internal static extern object InterlockedExchange([NotNullIfNotNull("value")] ref object? location1, object? value);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "RhpMemoryBarrier")]
        internal static extern void MemoryBarrier();

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "fabs")]
        internal static extern double fabs(double x);

        [Intrinsic]
        internal static float fabsf(float x)
        {
            // fabsf is not a real export for some architectures
            return (float)fabs(x);
        }

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "acos")]
        internal static extern double acos(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "acosf")]
        internal static extern float acosf(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "acosh")]
        internal static extern double acosh(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "acoshf")]
        internal static extern float acoshf(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "asin")]
        internal static extern double asin(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "asinf")]
        internal static extern float asinf(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "asinh")]
        internal static extern double asinh(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "asinhf")]
        internal static extern float asinhf(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "atan")]
        internal static extern double atan(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "atanf")]
        internal static extern float atanf(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "atan2")]
        internal static extern double atan2(double y, double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "atan2f")]
        internal static extern float atan2f(float y, float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "atanh")]
        internal static extern double atanh(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "atanhf")]
        internal static extern float atanhf(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "cbrt")]
        internal static extern double cbrt(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "cbrtf")]
        internal static extern float cbrtf(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "ceil")]
        internal static extern double ceil(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "ceilf")]
        internal static extern float ceilf(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "cos")]
        internal static extern double cos(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "cosf")]
        internal static extern float cosf(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "cosh")]
        internal static extern double cosh(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "coshf")]
        internal static extern float coshf(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "exp")]
        internal static extern double exp(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "expf")]
        internal static extern float expf(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "floor")]
        internal static extern double floor(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "floorf")]
        internal static extern float floorf(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "log")]
        internal static extern double log(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "logf")]
        internal static extern float logf(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "log2")]
        internal static extern double log2(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "log2f")]
        internal static extern float log2f(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "log10")]
        internal static extern double log10(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "log10f")]
        internal static extern float log10f(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "pow")]
        internal static extern double pow(double x, double y);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "powf")]
        internal static extern float powf(float x, float y);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "sin")]
        internal static extern double sin(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "sinf")]
        internal static extern float sinf(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "sinh")]
        internal static extern double sinh(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "sinhf")]
        internal static extern float sinhf(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "sqrt")]
        internal static extern double sqrt(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "sqrtf")]
        internal static extern float sqrtf(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "tan")]
        internal static extern double tan(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "tanf")]
        internal static extern float tanf(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "tanh")]
        internal static extern double tanh(double x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "tanhf")]
        internal static extern float tanhf(float x);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "fmod")]
        internal static extern double fmod(double x, double y);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "fmodf")]
        internal static extern float fmodf(float x, float y);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "fma")]
        internal static extern double fma(double x, double y, double z);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "fmaf")]
        internal static extern float fmaf(float x, float y, float z);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "modf")]
        internal static extern unsafe double modf(double x, double* intptr);

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeLibrary, "modff")]
        internal static extern unsafe float modff(float x, float* intptr);

        [DllImport(RuntimeImports.RuntimeLibrary, ExactSpelling = true)]
        internal static extern unsafe void* memmove(byte* dmem, byte* smem, nuint size);

        [DllImport(RuntimeImports.RuntimeLibrary, ExactSpelling = true)]
        internal static extern unsafe void* memset(byte* mem, int value, nuint size);

#if TARGET_X86 || TARGET_AMD64
        [DllImport(RuntimeLibrary, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void RhCpuIdEx(int* cpuInfo, int functionId, int subFunctionId);
#endif

        internal static RhCorElementTypeInfo GetRhCorElementTypeInfo(CorElementType elementType)
        {
            return RhCorElementTypeInfo.GetRhCorElementTypeInfo(elementType);
        }

        internal struct RhCorElementTypeInfo
        {
            public RhCorElementTypeInfo(ushort widenMask, bool isPrimitive = false)
            {
                RhCorElementTypeInfoFlags flags = RhCorElementTypeInfoFlags.IsValid;
                if (isPrimitive)
                    flags |= RhCorElementTypeInfoFlags.IsPrimitive;
                _flags = flags;
                _widenMask = widenMask;
            }

            public bool IsPrimitive
            {
                get
                {
                    return 0 != (_flags & RhCorElementTypeInfoFlags.IsPrimitive);
                }
            }

            public bool IsFloat
            {
                get
                {
                    return 0 != (_flags & RhCorElementTypeInfoFlags.IsFloat);
                }
            }

            //
            // This is a port of InvokeUtil::CanPrimitiveWiden() in the desktop runtime. This is used by various apis such as Array.SetValue()
            // and Delegate.DynamicInvoke() which allow value-preserving widenings from one primitive type to another.
            //
            public bool CanWidenTo(CorElementType targetElementType)
            {
                // Caller expected to ensure that both sides are primitive before calling us.
                Debug.Assert(this.IsPrimitive);
                Debug.Assert(GetRhCorElementTypeInfo(targetElementType).IsPrimitive);

                // Once we've asserted that the target is a primitive, we can also assert that it is >= ET_BOOLEAN.
                Debug.Assert(targetElementType >= CorElementType.ELEMENT_TYPE_BOOLEAN);
                byte targetElementTypeAsByte = (byte)targetElementType;
                ushort mask = (ushort)(1 << targetElementTypeAsByte);  // This is expected to overflow on larger ET_I and ET_U - this is ok and anticipated.
                if (0 != (_widenMask & mask))
                    return true;
                return false;
            }

            internal static RhCorElementTypeInfo GetRhCorElementTypeInfo(CorElementType elementType)
            {
                // The _lookupTable array only covers a subset of RhCorElementTypes, so we return a default
                // info when someone asks for an elementType which does not have an entry in the table.
                if ((int)elementType > s_lookupTable.Length)
                    return default(RhCorElementTypeInfo);

                return s_lookupTable[(int)elementType];
            }


            private RhCorElementTypeInfoFlags _flags;

            [Flags]
            private enum RhCorElementTypeInfoFlags : byte
            {
                IsValid = 0x01,       // Set for all valid CorElementTypeInfo's
                IsPrimitive = 0x02,   // Is it a primitive type (as defined by TypeInfo.IsPrimitive)
                IsFloat = 0x04,       // Is it a floating point type
            }

            private ushort _widenMask;

            private static RhCorElementTypeInfo[] s_lookupTable = new RhCorElementTypeInfo[]
            {
                // index = 0x0
                new RhCorElementTypeInfo { _widenMask = 0x0000, _flags = 0 },
                // index = 0x1
                new RhCorElementTypeInfo { _widenMask = 0x0000, _flags = 0 },
                // index = 0x2 = ELEMENT_TYPE_BOOLEAN   (W = BOOL)
                new RhCorElementTypeInfo { _widenMask = 0x0004, _flags = RhCorElementTypeInfoFlags.IsValid|RhCorElementTypeInfoFlags.IsPrimitive },
                // index = 0x3 = ELEMENT_TYPE_CHAR      (W = U2, CHAR, I4, U4, I8, U8, R4, R8) (U2 == Char)
                new RhCorElementTypeInfo { _widenMask = 0x3f88, _flags = RhCorElementTypeInfoFlags.IsValid|RhCorElementTypeInfoFlags.IsPrimitive },
                // index = 0x4 = ELEMENT_TYPE_I1        (W = I1, I2, I4, I8, R4, R8)
                new RhCorElementTypeInfo { _widenMask = 0x3550, _flags = RhCorElementTypeInfoFlags.IsValid|RhCorElementTypeInfoFlags.IsPrimitive },
                // index = 0x5 = ELEMENT_TYPE_U1        (W = CHAR, U1, I2, U2, I4, U4, I8, U8, R4, R8)
                new RhCorElementTypeInfo { _widenMask = 0x3FE8, _flags = RhCorElementTypeInfoFlags.IsValid|RhCorElementTypeInfoFlags.IsPrimitive },
                // index = 0x6 = ELEMENT_TYPE_I2        (W = I2, I4, I8, R4, R8)
                new RhCorElementTypeInfo { _widenMask = 0x3540, _flags = RhCorElementTypeInfoFlags.IsValid|RhCorElementTypeInfoFlags.IsPrimitive },
                // index = 0x7 = ELEMENT_TYPE_U2        (W = U2, CHAR, I4, U4, I8, U8, R4, R8)
                new RhCorElementTypeInfo { _widenMask = 0x3F88, _flags = RhCorElementTypeInfoFlags.IsValid|RhCorElementTypeInfoFlags.IsPrimitive },
                // index = 0x8 = ELEMENT_TYPE_I4        (W = I4, I8, R4, R8)
                new RhCorElementTypeInfo { _widenMask = 0x3500, _flags = RhCorElementTypeInfoFlags.IsValid|RhCorElementTypeInfoFlags.IsPrimitive },
                // index = 0x9 = ELEMENT_TYPE_U4        (W = U4, I8, R4, R8)
                new RhCorElementTypeInfo { _widenMask = 0x3E00, _flags = RhCorElementTypeInfoFlags.IsValid|RhCorElementTypeInfoFlags.IsPrimitive },
                // index = 0xa = ELEMENT_TYPE_I8        (W = I8, R4, R8)
                new RhCorElementTypeInfo { _widenMask = 0x3400, _flags = RhCorElementTypeInfoFlags.IsValid|RhCorElementTypeInfoFlags.IsPrimitive },
                // index = 0xb = ELEMENT_TYPE_U8        (W = U8, R4, R8)
                new RhCorElementTypeInfo { _widenMask = 0x3800, _flags = RhCorElementTypeInfoFlags.IsValid|RhCorElementTypeInfoFlags.IsPrimitive },
                // index = 0xc = ELEMENT_TYPE_R4        (W = R4, R8)
                new RhCorElementTypeInfo { _widenMask = 0x3000, _flags = RhCorElementTypeInfoFlags.IsValid|RhCorElementTypeInfoFlags.IsPrimitive|RhCorElementTypeInfoFlags.IsFloat },
                // index = 0xd = ELEMENT_TYPE_R8        (W = R8)
                new RhCorElementTypeInfo { _widenMask = 0x2000, _flags = RhCorElementTypeInfoFlags.IsValid|RhCorElementTypeInfoFlags.IsPrimitive|RhCorElementTypeInfoFlags.IsFloat },
                // index = 0xe
                new RhCorElementTypeInfo { _widenMask = 0x0000, _flags = 0 },
                // index = 0xf
                new RhCorElementTypeInfo { _widenMask = 0x0000, _flags = 0 },
                // index = 0x10
                new RhCorElementTypeInfo { _widenMask = 0x0000, _flags = 0 },
                // index = 0x11
                new RhCorElementTypeInfo { _widenMask = 0x0000, _flags = 0 },
                // index = 0x12
                new RhCorElementTypeInfo { _widenMask = 0x0000, _flags = 0 },
                // index = 0x13
                new RhCorElementTypeInfo { _widenMask = 0x0000, _flags = 0 },
                // index = 0x14
                new RhCorElementTypeInfo { _widenMask = 0x0000, _flags = 0 },
                // index = 0x15
                new RhCorElementTypeInfo { _widenMask = 0x0000, _flags = 0 },
                // index = 0x16
                new RhCorElementTypeInfo { _widenMask = 0x0000, _flags = 0 },
                // index = 0x17
                new RhCorElementTypeInfo { _widenMask = 0x0000, _flags = 0 },
                // index = 0x18 = ELEMENT_TYPE_I
                new RhCorElementTypeInfo { _widenMask = 0x0000, _flags = RhCorElementTypeInfoFlags.IsValid|RhCorElementTypeInfoFlags.IsPrimitive },
                // index = 0x19 = ELEMENT_TYPE_U
                new RhCorElementTypeInfo { _widenMask = 0x0000, _flags = RhCorElementTypeInfoFlags.IsValid|RhCorElementTypeInfoFlags.IsPrimitive },
            };
        }
    }
}
