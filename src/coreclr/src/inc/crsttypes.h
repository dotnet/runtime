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
// file:CrstTypes.def file and run the clr\artifacts\CrstTypeTool utility to generate a new version of this file.

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
    CrstCrstCLRPrivBinderLocalWinMDPath = 23,
    CrstCSPCache = 24,
    CrstDataTest1 = 25,
    CrstDataTest2 = 26,
    CrstDbgTransport = 27,
    CrstDeadlockDetection = 28,
    CrstDebuggerController = 29,
    CrstDebuggerFavorLock = 30,
    CrstDebuggerHeapExecMemLock = 31,
    CrstDebuggerHeapLock = 32,
    CrstDebuggerJitInfo = 33,
    CrstDebuggerMutex = 34,
    CrstDelegateToFPtrHash = 35,
    CrstDomainLocalBlock = 36,
    CrstDynamicIL = 37,
    CrstDynamicMT = 38,
    CrstDynLinkZapItems = 39,
    CrstEtwTypeLogHash = 40,
    CrstEventPipe = 41,
    CrstEventStore = 42,
    CrstException = 43,
    CrstExecuteManLock = 44,
    CrstExecuteManRangeLock = 45,
    CrstFCall = 46,
    CrstFriendAccessCache = 47,
    CrstFuncPtrStubs = 48,
    CrstFusionAppCtx = 49,
    CrstGCCover = 50,
    CrstGCMemoryPressure = 51,
    CrstGlobalStrLiteralMap = 52,
    CrstHandleTable = 53,
    CrstHostAssemblyMap = 54,
    CrstHostAssemblyMapAdd = 55,
    CrstIbcProfile = 56,
    CrstIJWFixupData = 57,
    CrstIJWHash = 58,
    CrstILStubGen = 59,
    CrstInlineTrackingMap = 60,
    CrstInstMethodHashTable = 61,
    CrstInterfaceVTableMap = 62,
    CrstInterop = 63,
    CrstInteropData = 64,
    CrstIOThreadpoolWorker = 65,
    CrstIsJMCMethod = 66,
    CrstISymUnmanagedReader = 67,
    CrstJit = 68,
    CrstJitGenericHandleCache = 69,
    CrstJitInlineTrackingMap = 70,
    CrstJitPerf = 71,
    CrstJumpStubCache = 72,
    CrstLeafLock = 73,
    CrstListLock = 74,
    CrstLoaderAllocator = 75,
    CrstLoaderAllocatorReferences = 76,
    CrstLoaderHeap = 77,
    CrstMda = 78,
    CrstMetadataTracker = 79,
    CrstMethodDescBackpatchInfoTracker = 80,
    CrstModIntPairList = 81,
    CrstModule = 82,
    CrstModuleFixup = 83,
    CrstModuleLookupTable = 84,
    CrstMulticoreJitHash = 85,
    CrstMulticoreJitManager = 86,
    CrstMUThunkHash = 87,
    CrstNativeBinderInit = 88,
    CrstNativeImageCache = 89,
    CrstNls = 90,
    CrstNotifyGdb = 91,
    CrstObjectList = 92,
    CrstOnEventManager = 93,
    CrstPatchEntryPoint = 94,
    CrstPEImage = 95,
    CrstPEImagePDBStream = 96,
    CrstPendingTypeLoadEntry = 97,
    CrstPinHandle = 98,
    CrstPinnedByrefValidation = 99,
    CrstProfilerGCRefDataFreeList = 100,
    CrstProfilingAPIStatus = 101,
    CrstPublisherCertificate = 102,
    CrstRCWCache = 103,
    CrstRCWCleanupList = 104,
    CrstRCWRefCache = 105,
    CrstReadyToRunEntryPointToMethodDescMap = 106,
    CrstReDacl = 107,
    CrstReflection = 108,
    CrstReJITDomainTable = 109,
    CrstReJITGlobalRequest = 110,
    CrstRemoting = 111,
    CrstRetThunkCache = 112,
    CrstRWLock = 113,
    CrstSavedExceptionInfo = 114,
    CrstSaveModuleProfileData = 115,
    CrstSecurityStackwalkCache = 116,
    CrstSharedAssemblyCreate = 117,
    CrstSigConvert = 118,
    CrstSingleUseLock = 119,
    CrstSpecialStatics = 120,
    CrstSqmManager = 121,
    CrstStackSampler = 122,
    CrstStressLog = 123,
    CrstStrongName = 124,
    CrstStubCache = 125,
    CrstStubDispatchCache = 126,
    CrstStubUnwindInfoHeapSegments = 127,
    CrstSyncBlockCache = 128,
    CrstSyncHashLock = 129,
    CrstSystemBaseDomain = 130,
    CrstSystemDomain = 131,
    CrstSystemDomainDelayedUnloadList = 132,
    CrstThreadIdDispenser = 133,
    CrstThreadpoolEventCache = 134,
    CrstThreadpoolTimerQueue = 135,
    CrstThreadpoolWaitThreads = 136,
    CrstThreadpoolWorker = 137,
    CrstThreadStaticDataHashTable = 138,
    CrstThreadStore = 139,
    CrstTieredCompilation = 140,
    CrstTPMethodTable = 141,
    CrstTypeEquivalenceMap = 142,
    CrstTypeIDMap = 143,
    CrstUMEntryThunkCache = 144,
    CrstUMThunkHash = 145,
    CrstUniqueStack = 146,
    CrstUnresolvedClassLock = 147,
    CrstUnwindInfoTableLock = 148,
    CrstVSDIndirectionCellLock = 149,
    CrstWinRTFactoryCache = 150,
    CrstWrapperTemplate = 151,
    kNumberOfCrstTypes = 152
};

#endif // __CRST_TYPES_INCLUDED

// Define some debug data in one module only -- vm\crst.cpp.
#if defined(__IN_CRST_CPP) && defined(_DEBUG)

// An array mapping CrstType to level.
int g_rgCrstLevelMap[] =
{
    8,          // CrstAllowedFiles
    10,         // CrstAppDomainCache
    14,         // CrstAppDomainHandleTable
    0,          // CrstArgBasedStubCache
    0,          // CrstAssemblyDependencyGraph
    0,          // CrstAssemblyIdentityCache
    0,          // CrstAssemblyList
    8,          // CrstAssemblyLoader
    3,          // CrstAvailableClass
    4,          // CrstAvailableParamTypes
    8,          // CrstBaseDomain
    -1,         // CrstCCompRC
    8,          // CrstCer
    13,         // CrstClassFactInfoHash
    9,          // CrstClassInit
    -1,         // CrstClrNotification
    0,          // CrstCLRPrivBinderMaps
    3,          // CrstCLRPrivBinderMapsAdd
    7,          // CrstCodeFragmentHeap
    0,          // CrstCOMCallWrapper
    4,          // CrstCOMWrapperCache
    0,          // CrstConnectionNameTable
    17,         // CrstContexts
    0,          // CrstCrstCLRPrivBinderLocalWinMDPath
    8,          // CrstCSPCache
    3,          // CrstDataTest1
    0,          // CrstDataTest2
    0,          // CrstDbgTransport
    0,          // CrstDeadlockDetection
    -1,         // CrstDebuggerController
    3,          // CrstDebuggerFavorLock
    0,          // CrstDebuggerHeapExecMemLock
    0,          // CrstDebuggerHeapLock
    4,          // CrstDebuggerJitInfo
    11,         // CrstDebuggerMutex
    0,          // CrstDelegateToFPtrHash
    16,         // CrstDomainLocalBlock
    0,          // CrstDynamicIL
    3,          // CrstDynamicMT
    3,          // CrstDynLinkZapItems
    5,          // CrstEtwTypeLogHash
    18,         // CrstEventPipe
    0,          // CrstEventStore
    0,          // CrstException
    8,          // CrstExecuteManLock
    0,          // CrstExecuteManRangeLock
    3,          // CrstFCall
    8,          // CrstFriendAccessCache
    8,          // CrstFuncPtrStubs
    5,          // CrstFusionAppCtx
    11,         // CrstGCCover
    0,          // CrstGCMemoryPressure
    13,         // CrstGlobalStrLiteralMap
    1,          // CrstHandleTable
    0,          // CrstHostAssemblyMap
    3,          // CrstHostAssemblyMapAdd
    0,          // CrstIbcProfile
    10,         // CrstIJWFixupData
    0,          // CrstIJWHash
    8,          // CrstILStubGen
    3,          // CrstInlineTrackingMap
    17,         // CrstInstMethodHashTable
    0,          // CrstInterfaceVTableMap
    18,         // CrstInterop
    4,          // CrstInteropData
    13,         // CrstIOThreadpoolWorker
    0,          // CrstIsJMCMethod
    8,          // CrstISymUnmanagedReader
    9,          // CrstJit
    0,          // CrstJitGenericHandleCache
    16,         // CrstJitInlineTrackingMap
    -1,         // CrstJitPerf
    7,          // CrstJumpStubCache
    0,          // CrstLeafLock
    -1,         // CrstListLock
    15,         // CrstLoaderAllocator
    16,         // CrstLoaderAllocatorReferences
    0,          // CrstLoaderHeap
    0,          // CrstMda
    -1,         // CrstMetadataTracker
    14,         // CrstMethodDescBackpatchInfoTracker
    0,          // CrstModIntPairList
    4,          // CrstModule
    15,         // CrstModuleFixup
    3,          // CrstModuleLookupTable
    0,          // CrstMulticoreJitHash
    13,         // CrstMulticoreJitManager
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
    19,         // CrstPendingTypeLoadEntry
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
    10,         // CrstReflection
    10,         // CrstReJITDomainTable
    17,         // CrstReJITGlobalRequest
    20,         // CrstRemoting
    3,          // CrstRetThunkCache
    0,          // CrstRWLock
    3,          // CrstSavedExceptionInfo
    0,          // CrstSaveModuleProfileData
    0,          // CrstSecurityStackwalkCache
    4,          // CrstSharedAssemblyCreate
    3,          // CrstSigConvert
    6,          // CrstSingleUseLock
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
    13,         // CrstSystemDomain
    0,          // CrstSystemDomainDelayedUnloadList
    0,          // CrstThreadIdDispenser
    0,          // CrstThreadpoolEventCache
    8,          // CrstThreadpoolTimerQueue
    8,          // CrstThreadpoolWaitThreads
    13,         // CrstThreadpoolWorker
    4,          // CrstThreadStaticDataHashTable
    12,         // CrstThreadStore
    10,         // CrstTieredCompilation
    8,          // CrstTPMethodTable
    3,          // CrstTypeEquivalenceMap
    8,          // CrstTypeIDMap
    3,          // CrstUMEntryThunkCache
    0,          // CrstUMThunkHash
    3,          // CrstUniqueStack
    8,          // CrstUnresolvedClassLock
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
    "CrstJitInlineTrackingMap",
    "CrstJitPerf",
    "CrstJumpStubCache",
    "CrstLeafLock",
    "CrstListLock",
    "CrstLoaderAllocator",
    "CrstLoaderAllocatorReferences",
    "CrstLoaderHeap",
    "CrstMda",
    "CrstMetadataTracker",
    "CrstMethodDescBackpatchInfoTracker",
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
