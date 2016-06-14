// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// This file contains the globals and statics that are visible to DAC.
// It is used for the following:
// 1. in daccess.h to build the table of DAC globals
// 2. in enummem.cpp to dump out the related memory of static and globals 
//    in a mini dump or heap dump
// 3. in DacUpdateDll and toolbox\DacTablenGen\main.cs
//
// To use this functionality for other tools or purposes, define the 
// DEFINE_DACVAR macro & include dacvars.h like so (see enummem.cpp and/or 
// daccess.h for examples):
//
// #define DEFINE_DACVAR(type, size, id, var)  type id;     //this defn. discards
//                                                          //the size
// #include "dacvars.h"
//
// @dbgtodo: 
// Ideally we may be able to build a tool that generates this automatically.  
// At the least, we should automatically verify that the contents of this file 
// are consistent with the uses of all the macros like SVAL_DECL and GARY_DECL.
//
//=================================================
// INSTRUCTIONS FOR ADDING VARIABLES TO THIS FILE
//=================================================
// You need to add a global or static declared with DAC macros, such as SPTR_*
// GPTR_*, SVAL_*, GVAL_*, or GARY_*, only if the global or static is actually used
// in a DACized code path. If you have declared a static or global that way just 
// because you were pattern-matching or because you anticipate that the variable 
// may eventually be used in a DACized code path, you don't need to add it here,
// although in that case, you should not really use the DAC macro when you declare
// the global or static. 
//					*				*				*
// The FIRST ARGUMENT should always be specified as ULONG. This is the type of
// the offsets for the corresponding id in the _DacGlobals table. 
// @dbgtodo: 
// We should get rid of the ULONG argument since it's always the same. We would
// also need to modify DacTablenGen\main.cs.
//					*				*				*
// The SECOND ARGUMENT, "true_type," is used to calculate the true size of the 
// static/global variable. It is currently used only in enummem.cpp to write out   
// theproper size of memory for dumps.
//					*				*				*
// The THIRD ARGUMENT should be a qualified name. If the variable is a static data
// member, the name should be <class_name>__<member_name>. If the variable is a 
// global, the name should be <dac>__<global_name>.
//					*				*				*
// The FOURTH ARGUMENT should be the actual name of the static/global variable. If 
// static data the should be [<namespace>::]<class_name>::<member_name>. If global,
// it should look like <global_name>.
//					*				*				*
// If you need to add an entry to this file, your type may not be visible when  
// this file is compiled. In that case, you need to do one of two things:
// - If the type is a pointer type, you can simply use UNKNOWN_POINTER_TYPE as the 
//	 "true type." It may be useful to specify the non-visible type in a comment. 
// - If the type is a composite/user-defined type, you must #include the header 
//   file that defines the type in enummem.cpp. Do NOT #include it in daccess.h
// Array types may be dumped via an explicit call to enumMem, so they should 
// be declared with DEFINE_DACVAR_NO_DUMP. The size in this case is immaterial, since 
// nothing will be dumped. 

#ifndef DEFINE_DACVAR
#define DEFINE_DACVAR(type, true_type, id, var)
#endif

// Use this macro for any server (namespace SVR) variables
#ifndef DEFINE_DACVAR_SVR
#define DEFINE_DACVAR_SVR(type, true_type, id, var)
#endif

// Use this macro to define a static var that is known to DAC, but not captured in a dump.                         
#ifndef DEFINE_DACVAR_NO_DUMP
#define DEFINE_DACVAR_NO_DUMP(type, true_type, id, var)
#endif

#define UNKNOWN_POINTER_TYPE SIZE_T

DEFINE_DACVAR(ULONG, PTR_RangeSection, ExecutionManager__m_CodeRangeList, ExecutionManager::m_CodeRangeList)
DEFINE_DACVAR(ULONG, PTR_EECodeManager, ExecutionManager__m_pDefaultCodeMan, ExecutionManager::m_pDefaultCodeMan)
DEFINE_DACVAR(ULONG, LONG, ExecutionManager__m_dwReaderCount, ExecutionManager::m_dwReaderCount)
DEFINE_DACVAR(ULONG, LONG, ExecutionManager__m_dwWriterLock, ExecutionManager::m_dwWriterLock)

DEFINE_DACVAR(ULONG, PTR_EEJitManager, ExecutionManager__m_pEEJitManager, ExecutionManager::m_pEEJitManager)
#ifdef FEATURE_PREJIT
DEFINE_DACVAR(ULONG, PTR_NativeImageJitManager, ExecutionManager__m_pNativeImageJitManager, ExecutionManager::m_pNativeImageJitManager)
#endif
#ifdef FEATURE_READYTORUN
DEFINE_DACVAR(ULONG, PTR_ReadyToRunJitManager, ExecutionManager__m_pReadyToRunJitManager, ExecutionManager::m_pReadyToRunJitManager)
#endif

DEFINE_DACVAR_NO_DUMP(ULONG, VMHELPDEF *, dac__hlpFuncTable, ::hlpFuncTable)
DEFINE_DACVAR(ULONG, VMHELPDEF *, dac__hlpDynamicFuncTable, ::hlpDynamicFuncTable)

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
DEFINE_DACVAR(ULONG, PTR_ConnectionNameTable, CCLRDebugManager__m_pConnectionNameHash, CCLRDebugManager::m_pConnectionNameHash)
#endif // FEATURE_INCLUDE_ALL_INTERFACES
DEFINE_DACVAR(ULONG, PTR_StubManager, StubManager__g_pFirstManager, StubManager::g_pFirstManager)
DEFINE_DACVAR(ULONG, PTR_PrecodeStubManager, PrecodeStubManager__g_pManager, PrecodeStubManager::g_pManager)
DEFINE_DACVAR(ULONG, PTR_StubLinkStubManager, StubLinkStubManager__g_pManager, StubLinkStubManager::g_pManager)
DEFINE_DACVAR(ULONG, PTR_ThunkHeapStubManager, ThunkHeapStubManager__g_pManager, ThunkHeapStubManager::g_pManager)
DEFINE_DACVAR(ULONG, PTR_JumpStubStubManager, JumpStubStubManager__g_pManager, JumpStubStubManager::g_pManager)
DEFINE_DACVAR(ULONG, PTR_RangeSectionStubManager, RangeSectionStubManager__g_pManager, RangeSectionStubManager::g_pManager)
DEFINE_DACVAR(ULONG, PTR_DelegateInvokeStubManager, DelegateInvokeStubManager__g_pManager, DelegateInvokeStubManager::g_pManager)
DEFINE_DACVAR(ULONG, PTR_VirtualCallStubManagerManager, VirtualCallStubManagerManager__g_pManager, VirtualCallStubManagerManager::g_pManager)

DEFINE_DACVAR(ULONG, PTR_ThreadStore, ThreadStore__s_pThreadStore, ThreadStore::s_pThreadStore)

DEFINE_DACVAR(ULONG, int, ThreadpoolMgr__cpuUtilization, ThreadpoolMgr::cpuUtilization)
DEFINE_DACVAR(ULONG, ThreadpoolMgr::ThreadCounter, ThreadpoolMgr__WorkerCounter, ThreadpoolMgr::WorkerCounter)
DEFINE_DACVAR(ULONG, int, ThreadpoolMgr__MinLimitTotalWorkerThreads, ThreadpoolMgr::MinLimitTotalWorkerThreads)
DEFINE_DACVAR(ULONG, DWORD, ThreadpoolMgr__MaxLimitTotalWorkerThreads, ThreadpoolMgr::MaxLimitTotalWorkerThreads)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE /*PTR_WorkRequest*/, ThreadpoolMgr__WorkRequestHead, ThreadpoolMgr::WorkRequestHead)  // PTR_WorkRequest is not defined. So use a pointer type
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE  /*PTR_WorkRequest*/, ThreadpoolMgr__WorkRequestTail, ThreadpoolMgr::WorkRequestTail) // 
DEFINE_DACVAR(ULONG, ThreadpoolMgr::ThreadCounter, ThreadpoolMgr__CPThreadCounter, ThreadpoolMgr::CPThreadCounter)
DEFINE_DACVAR(ULONG, LONG, ThreadpoolMgr__MaxFreeCPThreads, ThreadpoolMgr::MaxFreeCPThreads)
DEFINE_DACVAR(ULONG, LONG, ThreadpoolMgr__MaxLimitTotalCPThreads, ThreadpoolMgr::MaxLimitTotalCPThreads)
DEFINE_DACVAR(ULONG, LONG, ThreadpoolMgr__MinLimitTotalCPThreads, ThreadpoolMgr::MinLimitTotalCPThreads)
DEFINE_DACVAR(ULONG, LIST_ENTRY, ThreadpoolMgr__TimerQueue, ThreadpoolMgr::TimerQueue)
DEFINE_DACVAR_NO_DUMP(ULONG, SIZE_T, dac__HillClimbingLog, ::HillClimbingLog)
DEFINE_DACVAR(ULONG, int, dac__HillClimbingLogFirstIndex, ::HillClimbingLogFirstIndex)
DEFINE_DACVAR(ULONG, int, dac__HillClimbingLogSize, ::HillClimbingLogSize)

DEFINE_DACVAR(ULONG, PTR_Thread, dac__g_pFinalizerThread, ::g_pFinalizerThread)
DEFINE_DACVAR(ULONG, PTR_Thread, dac__g_pSuspensionThread, ::g_pSuspensionThread)

#ifdef FEATURE_SVR_GC
DEFINE_DACVAR(ULONG, DWORD, GCHeap__gcHeapType, GCHeap::gcHeapType)
#endif // FEATURE_SVR_GC

DEFINE_DACVAR(ULONG, PTR_BYTE, WKS__gc_heap__alloc_allocated, WKS::gc_heap::alloc_allocated)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE /*PTR_heap_segment*/, WKS__gc_heap__ephemeral_heap_segment, WKS::gc_heap::ephemeral_heap_segment)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE /*PTR_CFinalize*/, WKS__gc_heap__finalize_queue, WKS::gc_heap::finalize_queue)

// Can not use MULTIPLE_HEAPS here because desktop build contains it is not defined for workstation GC
// but we include workstation GC in mscorwks.dll.
#ifdef FEATURE_SVR_GC
DEFINE_DACVAR_SVR(ULONG, int, SVR__gc_heap__n_heaps, SVR::gc_heap::n_heaps)
DEFINE_DACVAR_SVR(ULONG, UNKNOWN_POINTER_TYPE /*(PTR_gc_heap*)*/, SVR__gc_heap__g_heaps, SVR::gc_heap::g_heaps)
#endif // FEATURE_SVR_GC
DEFINE_DACVAR(ULONG, oom_history, WKS__gc_heap__oom_info, WKS::gc_heap::oom_info)

DEFINE_DACVAR(ULONG, PTR_SystemDomain, SystemDomain__m_pSystemDomain, SystemDomain::m_pSystemDomain)
DEFINE_DACVAR(ULONG, ArrayListStatic, SystemDomain__m_appDomainIndexList, SystemDomain::m_appDomainIndexList)
DEFINE_DACVAR(ULONG, BOOL, SystemDomain__s_fForceDebug, SystemDomain::s_fForceDebug)
DEFINE_DACVAR(ULONG, BOOL, SystemDomain__s_fForceProfiling, SystemDomain::s_fForceProfiling)
DEFINE_DACVAR(ULONG, BOOL, SystemDomain__s_fForceInstrument, SystemDomain::s_fForceInstrument)
DEFINE_DACVAR(ULONG, PTR_SharedDomain, SharedDomain__m_pSharedDomain, SharedDomain::m_pSharedDomain)


DEFINE_DACVAR(ULONG, DWORD, CExecutionEngine__TlsIndex, CExecutionEngine::TlsIndex)

DEFINE_DACVAR(ULONG, LONG, GCScan__m_GcStructuresInvalidCnt, GCScan::m_GcStructuresInvalidCnt)

#if defined(FEATURE_INCLUDE_ALL_INTERFACES) || defined(FEATURE_WINDOWSPHONE)
DEFINE_DACVAR(ULONG, int, CCLRErrorReportingManager__g_ECustomDumpFlavor, CCLRErrorReportingManager::g_ECustomDumpFlavor)
#endif

DEFINE_DACVAR(ULONG, PTR_SString, SString__s_Empty, SString::s_Empty)

#ifdef FEATURE_APPX
#if defined(FEATURE_CORECLR)
DEFINE_DACVAR(ULONG, BOOL, dac__g_fAppX, ::g_fAppX)
#else
DEFINE_DACVAR(ULONG, PTR_AppXRTInfo, dac__g_pAppXRTInfo, ::g_pAppXRTInfo)
#endif
#endif // FEATURE_APPX

DEFINE_DACVAR(ULONG, BOOL, SString__s_IsANSIMultibyte, SString::s_IsANSIMultibyte)

#ifdef FEATURE_REMOTING
DEFINE_DACVAR_NO_DUMP(ULONG, MethodTable, CTPMethodTable__s_pThunkTable, CTPMethodTable::s_pThunkTable)
#endif // FEATURE_REMOTING

DEFINE_DACVAR(ULONG, INT32, ArrayBase__s_arrayBoundsZero, ArrayBase::s_arrayBoundsZero)

DEFINE_DACVAR(ULONG, BOOL, StackwalkCache__s_Enabled, StackwalkCache::s_Enabled)

DEFINE_DACVAR(ULONG, PTR_JITNotification, dac__g_pNotificationTable, ::g_pNotificationTable)
DEFINE_DACVAR(ULONG, ULONG32, dac__g_dacNotificationFlags, ::g_dacNotificationFlags)
DEFINE_DACVAR(ULONG, PTR_GcNotification, dac__g_pGcNotificationTable, ::g_pGcNotificationTable)

#ifndef FEATURE_IMPLICIT_TLS
DEFINE_DACVAR(ULONG, DWORD, dac__gThreadTLSIndex, ::gThreadTLSIndex)
DEFINE_DACVAR(ULONG, DWORD, dac__gAppDomainTLSIndex, ::gAppDomainTLSIndex)
#endif

DEFINE_DACVAR(ULONG, PTR_EEConfig, dac__g_pConfig, ::g_pConfig)

DEFINE_DACVAR(ULONG, MscorlibBinder, dac__g_Mscorlib, ::g_Mscorlib)

#if defined(PROFILING_SUPPORTED) || defined(PROFILING_SUPPORTED_DATA)
DEFINE_DACVAR(ULONG, ProfControlBlock, dac__g_profControlBlock, ::g_profControlBlock)
#endif // defined(PROFILING_SUPPORTED) || defined(PROFILING_SUPPORTED_DATA)

DEFINE_DACVAR_NO_DUMP(ULONG, SIZE_T, dac__generation_table, WKS::generation_table)
DEFINE_DACVAR(ULONG, PTR_DWORD, dac__g_card_table, ::g_card_table)
DEFINE_DACVAR(ULONG, PTR_BYTE, dac__g_lowest_address, ::g_lowest_address)
DEFINE_DACVAR(ULONG, PTR_BYTE, dac__g_highest_address, ::g_highest_address)

DEFINE_DACVAR(ULONG, GCHeap, dac__g_pGCHeap, ::g_pGCHeap)

#ifdef GC_CONFIG_DRIVEN
DEFINE_DACVAR_NO_DUMP(ULONG, SIZE_T, dac__interesting_data_per_heap, WKS::interesting_data_per_heap)
DEFINE_DACVAR_NO_DUMP(ULONG, SIZE_T, dac__compact_reasons_per_heap, WKS::compact_reasons_per_heap)
DEFINE_DACVAR_NO_DUMP(ULONG, SIZE_T, dac__expand_mechanisms_per_heap, WKS::expand_mechanisms_per_heap)
DEFINE_DACVAR_NO_DUMP(ULONG, SIZE_T, dac__interesting_mechanism_bits_per_heap, WKS::interesting_mechanism_bits_per_heap)
DEFINE_DACVAR_NO_DUMP(ULONG, SIZE_T, dac__gc_global_mechanisms, ::gc_global_mechanisms)
#endif //GC_CONFIG_DRIVEN

DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pThinLockThreadIdDispenser, ::g_pThinLockThreadIdDispenser)    
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pModuleIndexDispenser, ::g_pModuleIndexDispenser)    
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pObjectClass, ::g_pObjectClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pRuntimeTypeClass, ::g_pRuntimeTypeClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pCanonMethodTableClass, ::g_pCanonMethodTableClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pStringClass, ::g_pStringClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pArrayClass, ::g_pArrayClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pSZArrayHelperClass, ::g_pSZArrayHelperClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pNullableClass, ::g_pNullableClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pExceptionClass, ::g_pExceptionClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pThreadAbortExceptionClass, ::g_pThreadAbortExceptionClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pOutOfMemoryExceptionClass, ::g_pOutOfMemoryExceptionClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pStackOverflowExceptionClass, ::g_pStackOverflowExceptionClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pExecutionEngineExceptionClass, ::g_pExecutionEngineExceptionClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pDelegateClass, ::g_pDelegateClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pMulticastDelegateClass, ::g_pMulticastDelegateClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pFreeObjectMethodTable, ::g_pFreeObjectMethodTable)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pOverlappedDataClass, ::g_pOverlappedDataClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pValueTypeClass, ::g_pValueTypeClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pEnumClass, ::g_pEnumClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pThreadClass, ::g_pThreadClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pCriticalFinalizerObjectClass, ::g_pCriticalFinalizerObjectClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pAsyncFileStream_AsyncResultClass, ::g_pAsyncFileStream_AsyncResultClass)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pPredefinedArrayTypes, ::g_pPredefinedArrayTypes)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_TypedReferenceMT, ::g_TypedReferenceMT)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pByteArrayMT, ::g_pByteArrayMT)

#ifdef FEATURE_COMINTEROP
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pBaseCOMObject, ::g_pBaseCOMObject)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pBaseRuntimeClass, ::g_pBaseRuntimeClass)
#endif

#ifdef FEATURE_ICASTABLE
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pICastableInterface, ::g_pICastableInterface)
#endif // FEATURE_ICASTABLE

DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pPrepareConstrainedRegionsMethod, ::g_pPrepareConstrainedRegionsMethod)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pExecuteBackoutCodeHelperMethod, ::g_pExecuteBackoutCodeHelperMethod)

DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pObjectCtorMD, ::g_pObjectCtorMD)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pObjectFinalizerMD, ::g_pObjectFinalizerMD)

DEFINE_DACVAR(ULONG, bool, dac__g_fProcessDetach, ::g_fProcessDetach)
DEFINE_DACVAR(ULONG, DWORD, dac__g_fEEShutDown, ::g_fEEShutDown)
DEFINE_DACVAR(ULONG, DWORD, dac__g_fHostConfig, ::g_fHostConfig)

DEFINE_DACVAR(ULONG, ULONG, dac__g_CORDebuggerControlFlags, ::g_CORDebuggerControlFlags)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pDebugger, ::g_pDebugger)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pDebugInterface, ::g_pDebugInterface)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pEEDbgInterfaceImpl, ::g_pEEDbgInterfaceImpl)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pEEInterface, ::g_pEEInterface)

DEFINE_DACVAR(ULONG, BOOL, Debugger__s_fCanChangeNgenFlags, Debugger::s_fCanChangeNgenFlags)

DEFINE_DACVAR(ULONG, PTR_DebuggerPatchTable, DebuggerController__g_patches, DebuggerController::g_patches)
DEFINE_DACVAR(ULONG, BOOL, DebuggerController__g_patchTableValid, DebuggerController::g_patchTableValid)


DEFINE_DACVAR(ULONG, SIZE_T, dac__gLowestFCall, ::gLowestFCall)
DEFINE_DACVAR(ULONG, SIZE_T, dac__gHighestFCall, ::gHighestFCall)
DEFINE_DACVAR(ULONG, SIZE_T, dac__gFCallMethods, ::gFCallMethods)

DEFINE_DACVAR(ULONG, PTR_SyncTableEntry, dac__g_pSyncTable, ::g_pSyncTable)
#ifdef FEATURE_COMINTEROP
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pRCWCleanupList, ::g_pRCWCleanupList)
DEFINE_DACVAR(ULONG, BOOL, RCWWalker__s_bIsGlobalPeggingOn, RCWWalker::s_bIsGlobalPeggingOn)
#endif // FEATURE_COMINTEROP

#ifndef FEATURE_PAL
DEFINE_DACVAR(ULONG, SIZE_T, dac__g_runtimeLoadedBaseAddress, ::g_runtimeLoadedBaseAddress)
DEFINE_DACVAR(ULONG, SIZE_T, dac__g_runtimeVirtualSize, ::g_runtimeVirtualSize)
#endif // !FEATURE_PAL

DEFINE_DACVAR(ULONG, SyncBlockCache *, SyncBlockCache__s_pSyncBlockCache, SyncBlockCache::s_pSyncBlockCache)

DEFINE_DACVAR(ULONG, HandleTableMap, dac__g_HandleTableMap, ::g_HandleTableMap)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pStressLog, ::g_pStressLog)

DEFINE_DACVAR(ULONG, SIZE_T, dac__s_gsCookie, ::s_gsCookie)

#ifdef FEATURE_IPCMAN
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE, dac__g_pIPCManagerInterface, ::g_pIPCManagerInterface)
#endif // FEATURE_IPCMAN

DEFINE_DACVAR_NO_DUMP(ULONG, SIZE_T, dac__g_FCDynamicallyAssignedImplementations, ::g_FCDynamicallyAssignedImplementations)

DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE /*BYTE**/, WKS__gc_heap__internal_root_array, WKS::gc_heap::internal_root_array)
DEFINE_DACVAR(ULONG, size_t, WKS__gc_heap__internal_root_array_index, WKS::gc_heap::internal_root_array_index)
DEFINE_DACVAR(ULONG, ULONG, WKS__gc_heap__heap_analyze_success, WKS::gc_heap::heap_analyze_success)

DEFINE_DACVAR(ULONG, SIZE_T, WKS__gc_heap__mark_array, WKS::gc_heap::mark_array)
DEFINE_DACVAR(ULONG, SIZE_T, WKS__gc_heap__current_c_gc_state, WKS::gc_heap::current_c_gc_state)
DEFINE_DACVAR(ULONG, PTR_BYTE, WKS__gc_heap__next_sweep_obj, WKS::gc_heap::next_sweep_obj)
DEFINE_DACVAR(ULONG, UNKNOWN_POINTER_TYPE /* PTR_heap_segment */, WKS__gc_heap__saved_sweep_ephemeral_seg, WKS::gc_heap::saved_sweep_ephemeral_seg)
DEFINE_DACVAR(ULONG, PTR_BYTE, WKS__gc_heap__saved_sweep_ephemeral_start, WKS::gc_heap::saved_sweep_ephemeral_start)
DEFINE_DACVAR(ULONG, PTR_BYTE, WKS__gc_heap__background_saved_lowest_address, WKS::gc_heap::background_saved_lowest_address)
DEFINE_DACVAR(ULONG, PTR_BYTE, WKS__gc_heap__background_saved_highest_address, WKS::gc_heap::background_saved_highest_address)

#ifdef FEATURE_CORECLR
#ifndef FEATURE_PAL
DEFINE_DACVAR(ULONG, HANDLE, dac__g_hContinueStartupEvent, ::g_hContinueStartupEvent)
#endif // !FEATURE_PAL
DEFINE_DACVAR(ULONG, DWORD, CorHost2__m_dwStartupFlags, CorHost2::m_dwStartupFlags)
#endif // FEATURE_CORECLR

DEFINE_DACVAR(ULONG, HRESULT, dac__g_hrFatalError, ::g_hrFatalError)

#if defined(DEBUGGING_SUPPORTED) && defined (FEATURE_PREJIT)
    DEFINE_DACVAR(ULONG, DWORD, PEFile__s_NGENDebugFlags, PEFile::s_NGENDebugFlags)
#endif //defined(DEBUGGING_SUPPORTED) && defined (FEATURE_PREJIT)

#ifndef FEATURE_CORECLR
DEFINE_DACVAR(ULONG, DWORD, AssemblyUsageLogManager__s_UsageLogFlags, AssemblyUsageLogManager::s_UsageLogFlags)
#endif // FEATURE_CORECLR

#if defined(FEATURE_APPX_BINDER)
DEFINE_DACVAR(ULONG, PTR_CLRPrivBinderAppX, CLRPrivBinderAppX__s_pSingleton, CLRPrivBinderAppX::s_pSingleton)
#endif //defined(FEATURE_APPX)

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
DEFINE_DACVAR(ULONG, DWORD, dac__g_MiniMetaDataBuffMaxSize, ::g_MiniMetaDataBuffMaxSize)
DEFINE_DACVAR(ULONG, TADDR, dac__g_MiniMetaDataBuffAddress, ::g_MiniMetaDataBuffAddress)
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

DEFINE_DACVAR(ULONG, SIZE_T, dac__g_clrNotificationArguments, ::g_clrNotificationArguments)

#undef DEFINE_DACVAR
#undef DEFINE_DACVAR_SVR
#undef DEFINE_DACVAR_NO_DUMP
