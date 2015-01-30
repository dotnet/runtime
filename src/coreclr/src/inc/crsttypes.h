//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
    CrstAssemblyUsageLog = 8,
    CrstAvailableClass = 9,
    CrstAvailableParamTypes = 10,
    CrstBaseDomain = 11,
    CrstCCompRC = 12,
    CrstCer = 13,
    CrstClassFactInfoHash = 14,
    CrstClassInit = 15,
    CrstCLRPrivBinderMaps = 16,
    CrstCLRPrivBinderMapsAdd = 17,
    CrstCodeFragmentHeap = 18,
    CrstCOMWrapperCache = 19,
    CrstConnectionNameTable = 20,
    CrstContexts = 21,
    CrstCoreCLRBinderLog = 22,
    CrstCrstCLRPrivBinderLocalWinMDPath = 23,
    CrstCSPCache = 24,
    CrstDataTest1 = 25,
    CrstDataTest2 = 26,
    CrstDbgTransport = 27,
    CrstDeadlockDetection = 28,
    CrstDebuggerController = 29,
    CrstDebuggerFavorLock = 30,
    CrstDebuggerHeapLock = 31,
    CrstDebuggerJitInfo = 32,
    CrstDebuggerMutex = 33,
    CrstDelegateToFPtrHash = 34,
    CrstDomainLocalBlock = 35,
    CrstDynamicIL = 36,
    CrstDynamicMT = 37,
    CrstDynLinkZapItems = 38,
    CrstEtwTypeLogHash = 39,
    CrstEventStore = 40,
    CrstException = 41,
    CrstExecuteManLock = 42,
    CrstExecuteManRangeLock = 43,
    CrstFCall = 44,
    CrstFriendAccessCache = 45,
    CrstFuncPtrStubs = 46,
    CrstFusionAppCtx = 47,
    CrstFusionAssemblyDownload = 48,
    CrstFusionBindContext = 49,
    CrstFusionBindResult = 50,
    CrstFusionClb = 51,
    CrstFusionClosure = 52,
    CrstFusionClosureGraph = 53,
    CrstFusionConfigSettings = 54,
    CrstFusionDownload = 55,
    CrstFusionIsoLibInit = 56,
    CrstFusionLoadContext = 57,
    CrstFusionLog = 58,
    CrstFusionNgenIndex = 59,
    CrstFusionNgenIndexPool = 60,
    CrstFusionPcyCache = 61,
    CrstFusionPolicyConfigPool = 62,
    CrstFusionSingleUse = 63,
    CrstFusionWarningLog = 64,
    CrstGCMemoryPressure = 65,
    CrstGlobalStrLiteralMap = 66,
    CrstHandleTable = 67,
    CrstHostAssemblyMap = 68,
    CrstHostAssemblyMapAdd = 69,
    CrstIbcProfile = 70,
    CrstIJWFixupData = 71,
    CrstIJWHash = 72,
    CrstILFingerprintCache = 73,
    CrstILStubGen = 74,
    CrstInlineTrackingMap = 75,
    CrstInstMethodHashTable = 76,
    CrstInterfaceVTableMap = 77,
    CrstInterop = 78,
    CrstInteropData = 79,
    CrstIOThreadpoolWorker = 80,
    CrstIsJMCMethod = 81,
    CrstISymUnmanagedReader = 82,
    CrstJit = 83,
    CrstJitGenericHandleCache = 84,
    CrstJitPerf = 85,
    CrstJumpStubCache = 86,
    CrstLeafLock = 87,
    CrstListLock = 88,
    CrstLoaderAllocator = 89,
    CrstLoaderAllocatorReferences = 90,
    CrstLoaderHeap = 91,
    CrstMda = 92,
    CrstMetadataTracker = 93,
    CrstModIntPairList = 94,
    CrstModule = 95,
    CrstModuleFixup = 96,
    CrstModuleLookupTable = 97,
    CrstMulticoreJitHash = 98,
    CrstMulticoreJitManager = 99,
    CrstMUThunkHash = 100,
    CrstNativeBinderInit = 101,
    CrstNativeImageCache = 102,
    CrstNls = 103,
    CrstObjectList = 104,
    CrstOnEventManager = 105,
    CrstPatchEntryPoint = 106,
    CrstPEFileSecurityManager = 107,
    CrstPEImage = 108,
    CrstPEImagePDBStream = 109,
    CrstPendingTypeLoadEntry = 110,
    CrstPinHandle = 111,
    CrstPinnedByrefValidation = 112,
    CrstProfilerGCRefDataFreeList = 113,
    CrstProfilingAPIStatus = 114,
    CrstPublisherCertificate = 115,
    CrstRCWCache = 116,
    CrstRCWCleanupList = 117,
    CrstRCWRefCache = 118,
    CrstReDacl = 119,
    CrstReflection = 120,
    CrstReJITDomainTable = 121,
    CrstReJITGlobalRequest = 122,
    CrstReJITSharedDomainTable = 123,
    CrstRemoting = 124,
    CrstRetThunkCache = 125,
    CrstRWLock = 126,
    CrstSavedExceptionInfo = 127,
    CrstSaveModuleProfileData = 128,
    CrstSecurityPolicyCache = 129,
    CrstSecurityPolicyInit = 130,
    CrstSecurityStackwalkCache = 131,
    CrstSharedAssemblyCreate = 132,
    CrstSharedBaseDomain = 133,
    CrstSigConvert = 134,
    CrstSingleUseLock = 135,
    CrstSpecialStatics = 136,
    CrstSqmManager = 137,
    CrstStackSampler = 138,
    CrstStressLog = 139,
    CrstStrongName = 140,
    CrstStubCache = 141,
    CrstStubDispatchCache = 142,
    CrstStubUnwindInfoHeapSegments = 143,
    CrstSyncBlockCache = 144,
    CrstSyncHashLock = 145,
    CrstSystemBaseDomain = 146,
    CrstSystemDomain = 147,
    CrstSystemDomainDelayedUnloadList = 148,
    CrstThreadIdDispenser = 149,
    CrstThreadpoolEventCache = 150,
    CrstThreadpoolTimerQueue = 151,
    CrstThreadpoolWaitThreads = 152,
    CrstThreadpoolWorker = 153,
    CrstThreadStaticDataHashTable = 154,
    CrstThreadStore = 155,
    CrstTPMethodTable = 156,
    CrstTypeEquivalenceMap = 157,
    CrstTypeIDMap = 158,
    CrstUMEntryThunkCache = 159,
    CrstUMThunkHash = 160,
    CrstUniqueStack = 161,
    CrstUnresolvedClassLock = 162,
    CrstUnwindInfoTableLock = 163,
    CrstVSDIndirectionCellLock = 164,
    CrstWinRTFactoryCache = 165,
    CrstWrapperTemplate = 166,
    kNumberOfCrstTypes = 167
};

#endif // __CRST_TYPES_INCLUDED

// Define some debug data in one module only -- vm\crst.cpp.
#if defined(__IN_CRST_CPP) && defined(_DEBUG)

// An array mapping CrstType to level.
int g_rgCrstLevelMap[] =
{
    9,			// CrstAllowedFiles
    9,			// CrstAppDomainCache
    13,			// CrstAppDomainHandleTable
    0,			// CrstArgBasedStubCache
    0,			// CrstAssemblyDependencyGraph
    0,			// CrstAssemblyIdentityCache
    0,			// CrstAssemblyList
    7,			// CrstAssemblyLoader
    0,			// CrstAssemblyUsageLog
    3,			// CrstAvailableClass
    6,			// CrstAvailableParamTypes
    7,			// CrstBaseDomain
    -1,			// CrstCCompRC
    9,			// CrstCer
    11,			// CrstClassFactInfoHash
    8,			// CrstClassInit
    0,			// CrstCLRPrivBinderMaps
    3,			// CrstCLRPrivBinderMapsAdd
    6,			// CrstCodeFragmentHeap
    4,			// CrstCOMWrapperCache
    0,			// CrstConnectionNameTable
    17,			// CrstContexts
    -1,			// CrstCoreCLRBinderLog
    0,			// CrstCrstCLRPrivBinderLocalWinMDPath
    7,			// CrstCSPCache
    3,			// CrstDataTest1
    0,			// CrstDataTest2
    0,			// CrstDbgTransport
    0,			// CrstDeadlockDetection
    -1,			// CrstDebuggerController
    3,			// CrstDebuggerFavorLock
    0,			// CrstDebuggerHeapLock
    4,			// CrstDebuggerJitInfo
    11,			// CrstDebuggerMutex
    0,			// CrstDelegateToFPtrHash
    15,			// CrstDomainLocalBlock
    0,			// CrstDynamicIL
    3,			// CrstDynamicMT
    3,			// CrstDynLinkZapItems
    7,			// CrstEtwTypeLogHash
    0,			// CrstEventStore
    0,			// CrstException
    7,			// CrstExecuteManLock
    0,			// CrstExecuteManRangeLock
    3,			// CrstFCall
    7,			// CrstFriendAccessCache
    7,			// CrstFuncPtrStubs
    9,			// CrstFusionAppCtx
    7,			// CrstFusionAssemblyDownload
    5,			// CrstFusionBindContext
    0,			// CrstFusionBindResult
    0,			// CrstFusionClb
    16,			// CrstFusionClosure
    10,			// CrstFusionClosureGraph
    0,			// CrstFusionConfigSettings
    0,			// CrstFusionDownload
    0,			// CrstFusionIsoLibInit
    5,			// CrstFusionLoadContext
    4,			// CrstFusionLog
    7,			// CrstFusionNgenIndex
    7,			// CrstFusionNgenIndexPool
    0,			// CrstFusionPcyCache
    4,			// CrstFusionPolicyConfigPool
    5,			// CrstFusionSingleUse
    6,			// CrstFusionWarningLog
    0,			// CrstGCMemoryPressure
    11,			// CrstGlobalStrLiteralMap
    1,			// CrstHandleTable
    0,			// CrstHostAssemblyMap
    3,			// CrstHostAssemblyMapAdd
    0,			// CrstIbcProfile
    9,			// CrstIJWFixupData
    0,			// CrstIJWHash
    5,			// CrstILFingerprintCache
    7,			// CrstILStubGen
    3,			// CrstInlineTrackingMap
    16,			// CrstInstMethodHashTable
    0,			// CrstInterfaceVTableMap
    17,			// CrstInterop
    4,			// CrstInteropData
    11,			// CrstIOThreadpoolWorker
    0,			// CrstIsJMCMethod
    7,			// CrstISymUnmanagedReader
    8,			// CrstJit
    0,			// CrstJitGenericHandleCache
    -1,			// CrstJitPerf
    6,			// CrstJumpStubCache
    0,			// CrstLeafLock
    -1,			// CrstListLock
    14,			// CrstLoaderAllocator
    15,			// CrstLoaderAllocatorReferences
    0,			// CrstLoaderHeap
    0,			// CrstMda
    -1,			// CrstMetadataTracker
    0,			// CrstModIntPairList
    4,			// CrstModule
    14,			// CrstModuleFixup
    3,			// CrstModuleLookupTable
    0,			// CrstMulticoreJitHash
    11,			// CrstMulticoreJitManager
    0,			// CrstMUThunkHash
    -1,			// CrstNativeBinderInit
    -1,			// CrstNativeImageCache
    0,			// CrstNls
    2,			// CrstObjectList
    0,			// CrstOnEventManager
    0,			// CrstPatchEntryPoint
    0,			// CrstPEFileSecurityManager
    4,			// CrstPEImage
    0,			// CrstPEImagePDBStream
    18,			// CrstPendingTypeLoadEntry
    0,			// CrstPinHandle
    0,			// CrstPinnedByrefValidation
    0,			// CrstProfilerGCRefDataFreeList
    0,			// CrstProfilingAPIStatus
    0,			// CrstPublisherCertificate
    3,			// CrstRCWCache
    0,			// CrstRCWCleanupList
    3,			// CrstRCWRefCache
    0,			// CrstReDacl
    9,			// CrstReflection
    7,			// CrstReJITDomainTable
    13,			// CrstReJITGlobalRequest
    9,			// CrstReJITSharedDomainTable
    19,			// CrstRemoting
    3,			// CrstRetThunkCache
    0,			// CrstRWLock
    3,			// CrstSavedExceptionInfo
    0,			// CrstSaveModuleProfileData
    0,			// CrstSecurityPolicyCache
    3,			// CrstSecurityPolicyInit
    0,			// CrstSecurityStackwalkCache
    4,			// CrstSharedAssemblyCreate
    7,			// CrstSharedBaseDomain
    3,			// CrstSigConvert
    5,			// CrstSingleUseLock
    0,			// CrstSpecialStatics
    0,			// CrstSqmManager
    0,			// CrstStackSampler
    -1,			// CrstStressLog
    0,			// CrstStrongName
    5,			// CrstStubCache
    0,			// CrstStubDispatchCache
    4,			// CrstStubUnwindInfoHeapSegments
    3,			// CrstSyncBlockCache
    0,			// CrstSyncHashLock
    0,			// CrstSystemBaseDomain
    12,			// CrstSystemDomain
    0,			// CrstSystemDomainDelayedUnloadList
    0,			// CrstThreadIdDispenser
    0,			// CrstThreadpoolEventCache
    7,			// CrstThreadpoolTimerQueue
    7,			// CrstThreadpoolWaitThreads
    11,			// CrstThreadpoolWorker
    4,			// CrstThreadStaticDataHashTable
    10,			// CrstThreadStore
    9,			// CrstTPMethodTable
    3,			// CrstTypeEquivalenceMap
    7,			// CrstTypeIDMap
    3,			// CrstUMEntryThunkCache
    0,			// CrstUMThunkHash
    3,			// CrstUniqueStack
    7,			// CrstUnresolvedClassLock
    3,			// CrstUnwindInfoTableLock
    3,			// CrstVSDIndirectionCellLock
    3,			// CrstWinRTFactoryCache
    3,			// CrstWrapperTemplate
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
    "CrstAssemblyUsageLog",
    "CrstAvailableClass",
    "CrstAvailableParamTypes",
    "CrstBaseDomain",
    "CrstCCompRC",
    "CrstCer",
    "CrstClassFactInfoHash",
    "CrstClassInit",
    "CrstCLRPrivBinderMaps",
    "CrstCLRPrivBinderMapsAdd",
    "CrstCodeFragmentHeap",
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
    "CrstDebuggerHeapLock",
    "CrstDebuggerJitInfo",
    "CrstDebuggerMutex",
    "CrstDelegateToFPtrHash",
    "CrstDomainLocalBlock",
    "CrstDynamicIL",
    "CrstDynamicMT",
    "CrstDynLinkZapItems",
    "CrstEtwTypeLogHash",
    "CrstEventStore",
    "CrstException",
    "CrstExecuteManLock",
    "CrstExecuteManRangeLock",
    "CrstFCall",
    "CrstFriendAccessCache",
    "CrstFuncPtrStubs",
    "CrstFusionAppCtx",
    "CrstFusionAssemblyDownload",
    "CrstFusionBindContext",
    "CrstFusionBindResult",
    "CrstFusionClb",
    "CrstFusionClosure",
    "CrstFusionClosureGraph",
    "CrstFusionConfigSettings",
    "CrstFusionDownload",
    "CrstFusionIsoLibInit",
    "CrstFusionLoadContext",
    "CrstFusionLog",
    "CrstFusionNgenIndex",
    "CrstFusionNgenIndexPool",
    "CrstFusionPcyCache",
    "CrstFusionPolicyConfigPool",
    "CrstFusionSingleUse",
    "CrstFusionWarningLog",
    "CrstGCMemoryPressure",
    "CrstGlobalStrLiteralMap",
    "CrstHandleTable",
    "CrstHostAssemblyMap",
    "CrstHostAssemblyMapAdd",
    "CrstIbcProfile",
    "CrstIJWFixupData",
    "CrstIJWHash",
    "CrstILFingerprintCache",
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
    "CrstObjectList",
    "CrstOnEventManager",
    "CrstPatchEntryPoint",
    "CrstPEFileSecurityManager",
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
    "CrstReDacl",
    "CrstReflection",
    "CrstReJITDomainTable",
    "CrstReJITGlobalRequest",
    "CrstReJITSharedDomainTable",
    "CrstRemoting",
    "CrstRetThunkCache",
    "CrstRWLock",
    "CrstSavedExceptionInfo",
    "CrstSaveModuleProfileData",
    "CrstSecurityPolicyCache",
    "CrstSecurityPolicyInit",
    "CrstSecurityStackwalkCache",
    "CrstSharedAssemblyCreate",
    "CrstSharedBaseDomain",
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
