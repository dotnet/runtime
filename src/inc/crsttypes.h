//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#ifndef __CRST_TYPES_INCLUDED
#define __CRST_TYPES_INCLUDED

// **** THIS IS AN AUTOMATICALLY GENERATED HEADER FILE -- DO NOT EDIT!!! ****

// This file describes the range of Crst types available and their mapping to a numeric level (used by the
// runtime in debug mode to validate we're deadlock free). To modify these settings edit the
// file:CrstTypes.def file and run the clr\bin\CrstTypeTool utility to generate a new version of this file.

// Each Crst type is declared as a value in the following CrstType enum.
enum CrstType
{
    CrstAllowedFiles = 0,
    CrstAppDomainCache = 1,
    CrstAppDomainHandleTable = 2,
    CrstArgBasedStubCache = 3,
    CrstAssemblyDependencyGraph = 4,
    CrstAssemblyIdentityCache = 5,
    CrstAssemblyList = 6,
    CrstAssemblyLoader = 7,
    CrstAvailableClass = 8,
    CrstAvailableParamTypes = 9,
    CrstBaseDomain = 10,
    CrstCCompRC = 11,
    CrstCer = 12,
    CrstClassFactInfoHash = 13,
    CrstClassInit = 14,
    CrstClrNotification = 15,
    CrstCLRPrivBinderMaps = 16,
    CrstCLRPrivBinderMapsAdd = 17,
    CrstCodeFragmentHeap = 18,
    CrstCOMCallWrapper = 19,
    CrstCOMWrapperCache = 20,
    CrstConnectionNameTable = 21,
    CrstContexts = 22,
    CrstCoreCLRBinderLog = 23,
    CrstCrstCLRPrivBinderLocalWinMDPath = 24,
    CrstCSPCache = 25,
    CrstDataTest1 = 26,
    CrstDataTest2 = 27,
    CrstDbgTransport = 28,
    CrstDeadlockDetection = 29,
    CrstDebuggerController = 30,
    CrstDebuggerFavorLock = 31,
    CrstDebuggerHeapExecMemLock = 32,
    CrstDebuggerHeapLock = 33,
    CrstDebuggerJitInfo = 34,
    CrstDebuggerMutex = 35,
    CrstDelegateToFPtrHash = 36,
    CrstDomainLocalBlock = 37,
    CrstDynamicIL = 38,
    CrstDynamicMT = 39,
    CrstDynLinkZapItems = 40,
    CrstEtwTypeLogHash = 41,
    CrstEventPipe = 42,
    CrstEventStore = 43,
    CrstException = 44,
    CrstExecuteManLock = 45,
    CrstExecuteManRangeLock = 46,
    CrstFCall = 47,
    CrstFriendAccessCache = 48,
    CrstFuncPtrStubs = 49,
    CrstFusionAppCtx = 50,
    CrstGCCover = 51,
    CrstGCMemoryPressure = 52,
    CrstGlobalStrLiteralMap = 53,
    CrstHandleTable = 54,
    CrstHostAssemblyMap = 55,
    CrstHostAssemblyMapAdd = 56,
    CrstIbcProfile = 57,
    CrstIJWFixupData = 58,
    CrstIJWHash = 59,
    CrstILStubGen = 60,
    CrstInlineTrackingMap = 61,
    CrstInstMethodHashTable = 62,
    CrstInterfaceVTableMap = 63,
    CrstInterop = 64,
    CrstInteropData = 65,
    CrstIOThreadpoolWorker = 66,
    CrstIsJMCMethod = 67,
    CrstISymUnmanagedReader = 68,
    CrstJit = 69,
    CrstJitGenericHandleCache = 70,
    CrstJitPerf = 71,
    CrstJumpStubCache = 72,
    CrstLeafLock = 73,
    CrstListLock = 74,
    CrstLoaderAllocator = 75,
    CrstLoaderAllocatorReferences = 76,
    CrstLoaderHeap = 77,
    CrstMda = 78,
    CrstMetadataTracker = 79,
    CrstModIntPairList = 80,
    CrstModule = 81,
    CrstModuleFixup = 82,
    CrstModuleLookupTable = 83,
    CrstMulticoreJitHash = 84,
    CrstMulticoreJitManager = 85,
    CrstMUThunkHash = 86,
    CrstNativeBinderInit = 87,
    CrstNativeImageCache = 88,
    CrstNls = 89,
    CrstNotifyGdb = 90,
    CrstObjectList = 91,
    CrstOnEventManager = 92,
    CrstPatchEntryPoint = 93,
    CrstPEImage = 94,
    CrstPEImagePDBStream = 95,
    CrstPendingTypeLoadEntry = 96,
    CrstPinHandle = 97,
    CrstPinnedByrefValidation = 98,
    CrstProfilerGCRefDataFreeList = 99,
    CrstProfilingAPIStatus = 100,
    CrstPublisherCertificate = 101,
    CrstRCWCache = 102,
    CrstRCWCleanupList = 103,
    CrstRCWRefCache = 104,
    CrstReadyToRunEntryPointToMethodDescMap = 105,
    CrstReDacl = 106,
    CrstReflection = 107,
    CrstReJITDomainTable = 108,
    CrstReJITGlobalRequest = 109,
    CrstRemoting = 110,
    CrstRetThunkCache = 111,
    CrstRWLock = 112,
    CrstSavedExceptionInfo = 113,
    CrstSaveModuleProfileData = 114,
    CrstSecurityStackwalkCache = 115,
    CrstSharedAssemblyCreate = 116,
    CrstSigConvert = 117,
    CrstSingleUseLock = 118,
    CrstSpecialStatics = 119,
    CrstSqmManager = 120,
    CrstStackSampler = 121,
    CrstStressLog = 122,
    CrstStrongName = 123,
    CrstStubCache = 124,
    CrstStubDispatchCache = 125,
    CrstStubUnwindInfoHeapSegments = 126,
    CrstSyncBlockCache = 127,
    CrstSyncHashLock = 128,
    CrstSystemBaseDomain = 129,
    CrstSystemDomain = 130,
    CrstSystemDomainDelayedUnloadList = 131,
    CrstThreadIdDispenser = 132,
    CrstThreadpoolEventCache = 133,
    CrstThreadpoolTimerQueue = 134,
    CrstThreadpoolWaitThreads = 135,
    CrstThreadpoolWorker = 136,
    CrstThreadStaticDataHashTable = 137,
    CrstThreadStore = 138,
    CrstTieredCompilation = 139,
    CrstTPMethodTable = 140,
    CrstTypeEquivalenceMap = 141,
    CrstTypeIDMap = 142,
    CrstUMEntryThunkCache = 143,
    CrstUMThunkHash = 144,
    CrstUniqueStack = 145,
    CrstUnresolvedClassLock = 146,
    CrstUnwindInfoTableLock = 147,
    CrstVSDIndirectionCellLock = 148,
    CrstWinRTFactoryCache = 149,
    CrstWrapperTemplate = 150,
    kNumberOfCrstTypes = 151
};

#endif // __CRST_TYPES_INCLUDED

// Define some debug data in one module only -- vm\crst.cpp.
#if defined(__IN_CRST_CPP) && defined(_DEBUG)

// An array mapping CrstType to level.
int g_rgCrstLevelMap[] =
{
    9,          // CrstAllowedFiles
    9,          // CrstAppDomainCache
    12,         // CrstAppDomainHandleTable
    0,          // CrstArgBasedStubCache
    0,          // CrstAssemblyDependencyGraph
    0,          // CrstAssemblyIdentityCache
    0,          // CrstAssemblyList
    7,          // CrstAssemblyLoader
    3,          // CrstAvailableClass
    3,          // CrstAvailableParamTypes
    7,          // CrstBaseDomain
    -1,         // CrstCCompRC
    9,          // CrstCer
    11,         // CrstClassFactInfoHash
    8,          // CrstClassInit
    -1,         // CrstClrNotification
    0,          // CrstCLRPrivBinderMaps
    3,          // CrstCLRPrivBinderMapsAdd
    6,          // CrstCodeFragmentHeap
    0,          // CrstCOMCallWrapper
    4,          // CrstCOMWrapperCache
    0,          // CrstConnectionNameTable
    15,         // CrstContexts
    -1,         // CrstCoreCLRBinderLog
    0,          // CrstCrstCLRPrivBinderLocalWinMDPath
    7,          // CrstCSPCache
    3,          // CrstDataTest1
    0,          // CrstDataTest2
    0,          // CrstDbgTransport
    0,          // CrstDeadlockDetection
    -1,         // CrstDebuggerController
    3,          // CrstDebuggerFavorLock
    0,          // CrstDebuggerHeapExecMemLock
    0,          // CrstDebuggerHeapLock
    4,          // CrstDebuggerJitInfo
    9,          // CrstDebuggerMutex
    0,          // CrstDelegateToFPtrHash
    14,         // CrstDomainLocalBlock
    0,          // CrstDynamicIL
    3,          // CrstDynamicMT
    3,          // CrstDynLinkZapItems
    7,          // CrstEtwTypeLogHash
    17,         // CrstEventPipe
    0,          // CrstEventStore
    0,          // CrstException
    7,          // CrstExecuteManLock
    0,          // CrstExecuteManRangeLock
    3,          // CrstFCall
    7,          // CrstFriendAccessCache
    7,          // CrstFuncPtrStubs
    5,          // CrstFusionAppCtx
    9,          // CrstGCCover
    0,          // CrstGCMemoryPressure
    11,         // CrstGlobalStrLiteralMap
    1,          // CrstHandleTable
    0,          // CrstHostAssemblyMap
    3,          // CrstHostAssemblyMapAdd
    0,          // CrstIbcProfile
    9,          // CrstIJWFixupData
    0,          // CrstIJWHash
    7,          // CrstILStubGen
    3,          // CrstInlineTrackingMap
    15,         // CrstInstMethodHashTable
    0,          // CrstInterfaceVTableMap
    17,         // CrstInterop
    4,          // CrstInteropData
    11,         // CrstIOThreadpoolWorker
    0,          // CrstIsJMCMethod
    7,          // CrstISymUnmanagedReader
    8,          // CrstJit
    0,          // CrstJitGenericHandleCache
    -1,         // CrstJitPerf
    6,          // CrstJumpStubCache
    0,          // CrstLeafLock
    -1,         // CrstListLock
    13,         // CrstLoaderAllocator
    14,         // CrstLoaderAllocatorReferences
    0,          // CrstLoaderHeap
    0,          // CrstMda
    -1,         // CrstMetadataTracker
    0,          // CrstModIntPairList
    4,          // CrstModule
    13,         // CrstModuleFixup
    3,          // CrstModuleLookupTable
    0,          // CrstMulticoreJitHash
    11,         // CrstMulticoreJitManager
    0,          // CrstMUThunkHash
    -1,         // CrstNativeBinderInit
    -1,         // CrstNativeImageCache
    0,          // CrstNls
    0,          // CrstNotifyGdb
    2,          // CrstObjectList
    0,          // CrstOnEventManager
    0,          // CrstPatchEntryPoint
    4,          // CrstPEImage
    0,          // CrstPEImagePDBStream
    16,         // CrstPendingTypeLoadEntry
    0,          // CrstPinHandle
    0,          // CrstPinnedByrefValidation
    0,          // CrstProfilerGCRefDataFreeList
    0,          // CrstProfilingAPIStatus
    0,          // CrstPublisherCertificate
    3,          // CrstRCWCache
    0,          // CrstRCWCleanupList
    3,          // CrstRCWRefCache
    4,          // CrstReadyToRunEntryPointToMethodDescMap
    0,          // CrstReDacl
    9,          // CrstReflection
    7,          // CrstReJITDomainTable
    12,         // CrstReJITGlobalRequest
    17,         // CrstRemoting
    3,          // CrstRetThunkCache
    0,          // CrstRWLock
    3,          // CrstSavedExceptionInfo
    0,          // CrstSaveModuleProfileData
    0,          // CrstSecurityStackwalkCache
    4,          // CrstSharedAssemblyCreate
    3,          // CrstSigConvert
    5,          // CrstSingleUseLock
    0,          // CrstSpecialStatics
    0,          // CrstSqmManager
    0,          // CrstStackSampler
    -1,         // CrstStressLog
    0,          // CrstStrongName
    5,          // CrstStubCache
    0,          // CrstStubDispatchCache
    4,          // CrstStubUnwindInfoHeapSegments
    3,          // CrstSyncBlockCache
    0,          // CrstSyncHashLock
    4,          // CrstSystemBaseDomain
    11,         // CrstSystemDomain
    0,          // CrstSystemDomainDelayedUnloadList
    0,          // CrstThreadIdDispenser
    0,          // CrstThreadpoolEventCache
    7,          // CrstThreadpoolTimerQueue
    7,          // CrstThreadpoolWaitThreads
    11,         // CrstThreadpoolWorker
    4,          // CrstThreadStaticDataHashTable
    10,         // CrstThreadStore
    9,          // CrstTieredCompilation
    9,          // CrstTPMethodTable
    3,          // CrstTypeEquivalenceMap
    7,          // CrstTypeIDMap
    3,          // CrstUMEntryThunkCache
    0,          // CrstUMThunkHash
    3,          // CrstUniqueStack
    7,          // CrstUnresolvedClassLock
    3,          // CrstUnwindInfoTableLock
    3,          // CrstVSDIndirectionCellLock
    3,          // CrstWinRTFactoryCache
    3,          // CrstWrapperTemplate
};

// An array mapping CrstType to a stringized name.
LPCSTR g_rgCrstNameMap[] =
{
    "CrstAllowedFiles",
    "CrstAppDomainCache",
    "CrstAppDomainHandleTable",
    "CrstArgBasedStubCache",
    "CrstAssemblyDependencyGraph",
    "CrstAssemblyIdentityCache",
    "CrstAssemblyList",
    "CrstAssemblyLoader",
    "CrstAvailableClass",
    "CrstAvailableParamTypes",
    "CrstBaseDomain",
    "CrstCCompRC",
    "CrstCer",
    "CrstClassFactInfoHash",
    "CrstClassInit",
    "CrstClrNotification",
    "CrstCLRPrivBinderMaps",
    "CrstCLRPrivBinderMapsAdd",
    "CrstCodeFragmentHeap",
    "CrstCOMCallWrapper",
    "CrstCOMWrapperCache",
    "CrstConnectionNameTable",
    "CrstContexts",
    "CrstCoreCLRBinderLog",
    "CrstCrstCLRPrivBinderLocalWinMDPath",
    "CrstCSPCache",
    "CrstDataTest1",
    "CrstDataTest2",
    "CrstDbgTransport",
    "CrstDeadlockDetection",
    "CrstDebuggerController",
    "CrstDebuggerFavorLock",
    "CrstDebuggerHeapExecMemLock",
    "CrstDebuggerHeapLock",
    "CrstDebuggerJitInfo",
    "CrstDebuggerMutex",
    "CrstDelegateToFPtrHash",
    "CrstDomainLocalBlock",
    "CrstDynamicIL",
    "CrstDynamicMT",
    "CrstDynLinkZapItems",
    "CrstEtwTypeLogHash",
    "CrstEventPipe",
    "CrstEventStore",
    "CrstException",
    "CrstExecuteManLock",
    "CrstExecuteManRangeLock",
    "CrstFCall",
    "CrstFriendAccessCache",
    "CrstFuncPtrStubs",
    "CrstFusionAppCtx",
    "CrstGCCover",
    "CrstGCMemoryPressure",
    "CrstGlobalStrLiteralMap",
    "CrstHandleTable",
    "CrstHostAssemblyMap",
    "CrstHostAssemblyMapAdd",
    "CrstIbcProfile",
    "CrstIJWFixupData",
    "CrstIJWHash",
    "CrstILStubGen",
    "CrstInlineTrackingMap",
    "CrstInstMethodHashTable",
    "CrstInterfaceVTableMap",
    "CrstInterop",
    "CrstInteropData",
    "CrstIOThreadpoolWorker",
    "CrstIsJMCMethod",
    "CrstISymUnmanagedReader",
    "CrstJit",
    "CrstJitGenericHandleCache",
    "CrstJitPerf",
    "CrstJumpStubCache",
    "CrstLeafLock",
    "CrstListLock",
    "CrstLoaderAllocator",
    "CrstLoaderAllocatorReferences",
    "CrstLoaderHeap",
    "CrstMda",
    "CrstMetadataTracker",
    "CrstModIntPairList",
    "CrstModule",
    "CrstModuleFixup",
    "CrstModuleLookupTable",
    "CrstMulticoreJitHash",
    "CrstMulticoreJitManager",
    "CrstMUThunkHash",
    "CrstNativeBinderInit",
    "CrstNativeImageCache",
    "CrstNls",
    "CrstNotifyGdb",
    "CrstObjectList",
    "CrstOnEventManager",
    "CrstPatchEntryPoint",
    "CrstPEImage",
    "CrstPEImagePDBStream",
    "CrstPendingTypeLoadEntry",
    "CrstPinHandle",
    "CrstPinnedByrefValidation",
    "CrstProfilerGCRefDataFreeList",
    "CrstProfilingAPIStatus",
    "CrstPublisherCertificate",
    "CrstRCWCache",
    "CrstRCWCleanupList",
    "CrstRCWRefCache",
    "CrstReadyToRunEntryPointToMethodDescMap",
    "CrstReDacl",
    "CrstReflection",
    "CrstReJITDomainTable",
    "CrstReJITGlobalRequest",
    "CrstRemoting",
    "CrstRetThunkCache",
    "CrstRWLock",
    "CrstSavedExceptionInfo",
    "CrstSaveModuleProfileData",
    "CrstSecurityStackwalkCache",
    "CrstSharedAssemblyCreate",
    "CrstSigConvert",
    "CrstSingleUseLock",
    "CrstSpecialStatics",
    "CrstSqmManager",
    "CrstStackSampler",
    "CrstStressLog",
    "CrstStrongName",
    "CrstStubCache",
    "CrstStubDispatchCache",
    "CrstStubUnwindInfoHeapSegments",
    "CrstSyncBlockCache",
    "CrstSyncHashLock",
    "CrstSystemBaseDomain",
    "CrstSystemDomain",
    "CrstSystemDomainDelayedUnloadList",
    "CrstThreadIdDispenser",
    "CrstThreadpoolEventCache",
    "CrstThreadpoolTimerQueue",
    "CrstThreadpoolWaitThreads",
    "CrstThreadpoolWorker",
    "CrstThreadStaticDataHashTable",
    "CrstThreadStore",
    "CrstTieredCompilation",
    "CrstTPMethodTable",
    "CrstTypeEquivalenceMap",
    "CrstTypeIDMap",
    "CrstUMEntryThunkCache",
    "CrstUMThunkHash",
    "CrstUniqueStack",
    "CrstUnresolvedClassLock",
    "CrstUnwindInfoTableLock",
    "CrstVSDIndirectionCellLock",
    "CrstWinRTFactoryCache",
    "CrstWrapperTemplate",
};

// Define a special level constant for unordered locks.
#define CRSTUNORDERED (-1)

// Define inline helpers to map Crst types to names and levels.
inline static int GetCrstLevel(CrstType crstType)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(crstType >= 0 && crstType < kNumberOfCrstTypes);
    return g_rgCrstLevelMap[crstType];
}
inline static LPCSTR GetCrstName(CrstType crstType)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(crstType >= 0 && crstType < kNumberOfCrstTypes);
    return g_rgCrstNameMap[crstType];
}

#endif // defined(__IN_CRST_CPP) && defined(_DEBUG)
