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
    CrstEventStore = 41,
    CrstException = 42,
    CrstExecuteManLock = 43,
    CrstExecuteManRangeLock = 44,
    CrstFCall = 45,
    CrstFriendAccessCache = 46,
    CrstFuncPtrStubs = 47,
    CrstFusionAppCtx = 48,
    CrstFusionAssemblyDownload = 49,
    CrstFusionBindContext = 50,
    CrstFusionBindResult = 51,
    CrstFusionClb = 52,
    CrstFusionClosure = 53,
    CrstFusionClosureGraph = 54,
    CrstFusionConfigSettings = 55,
    CrstFusionDownload = 56,
    CrstFusionIsoLibInit = 57,
    CrstFusionLoadContext = 58,
    CrstFusionLog = 59,
    CrstFusionNgenIndex = 60,
    CrstFusionNgenIndexPool = 61,
    CrstFusionPcyCache = 62,
    CrstFusionPolicyConfigPool = 63,
    CrstFusionSingleUse = 64,
    CrstFusionWarningLog = 65,
    CrstGCMemoryPressure = 66,
    CrstGlobalStrLiteralMap = 67,
    CrstHandleTable = 68,
    CrstHostAssemblyMap = 69,
    CrstHostAssemblyMapAdd = 70,
    CrstIbcProfile = 71,
    CrstIJWFixupData = 72,
    CrstIJWHash = 73,
    CrstILFingerprintCache = 74,
    CrstILStubGen = 75,
    CrstInlineTrackingMap = 76,
    CrstInstMethodHashTable = 77,
    CrstInterfaceVTableMap = 78,
    CrstInterop = 79,
    CrstInteropData = 80,
    CrstIOThreadpoolWorker = 81,
    CrstIsJMCMethod = 82,
    CrstISymUnmanagedReader = 83,
    CrstJit = 84,
    CrstJitGenericHandleCache = 85,
    CrstJitPerf = 86,
    CrstJumpStubCache = 87,
    CrstLeafLock = 88,
    CrstListLock = 89,
    CrstLoaderAllocator = 90,
    CrstLoaderAllocatorReferences = 91,
    CrstLoaderHeap = 92,
    CrstMda = 93,
    CrstMetadataTracker = 94,
    CrstModIntPairList = 95,
    CrstModule = 96,
    CrstModuleFixup = 97,
    CrstModuleLookupTable = 98,
    CrstMulticoreJitHash = 99,
    CrstMulticoreJitManager = 100,
    CrstMUThunkHash = 101,
    CrstNativeBinderInit = 102,
    CrstNativeImageCache = 103,
    CrstNls = 104,
    CrstObjectList = 105,
    CrstOnEventManager = 106,
    CrstPatchEntryPoint = 107,
    CrstPEFileSecurityManager = 108,
    CrstPEImage = 109,
    CrstPEImagePDBStream = 110,
    CrstPendingTypeLoadEntry = 111,
    CrstPinHandle = 112,
    CrstPinnedByrefValidation = 113,
    CrstProfilerGCRefDataFreeList = 114,
    CrstProfilingAPIStatus = 115,
    CrstPublisherCertificate = 116,
    CrstRCWCache = 117,
    CrstRCWCleanupList = 118,
    CrstRCWRefCache = 119,
    CrstReDacl = 120,
    CrstReflection = 121,
    CrstReJITDomainTable = 122,
    CrstReJITGlobalRequest = 123,
    CrstReJITSharedDomainTable = 124,
    CrstRemoting = 125,
    CrstRetThunkCache = 126,
    CrstRWLock = 127,
    CrstSavedExceptionInfo = 128,
    CrstSaveModuleProfileData = 129,
    CrstSecurityPolicyCache = 130,
    CrstSecurityPolicyInit = 131,
    CrstSecurityStackwalkCache = 132,
    CrstSharedAssemblyCreate = 133,
    CrstSharedBaseDomain = 134,
    CrstSigConvert = 135,
    CrstSingleUseLock = 136,
    CrstSpecialStatics = 137,
    CrstSqmManager = 138,
    CrstStackSampler = 139,
    CrstStressLog = 140,
    CrstStrongName = 141,
    CrstStubCache = 142,
    CrstStubDispatchCache = 143,
    CrstStubUnwindInfoHeapSegments = 144,
    CrstSyncBlockCache = 145,
    CrstSyncHashLock = 146,
    CrstSystemBaseDomain = 147,
    CrstSystemDomain = 148,
    CrstSystemDomainDelayedUnloadList = 149,
    CrstThreadIdDispenser = 150,
    CrstThreadpoolEventCache = 151,
    CrstThreadpoolTimerQueue = 152,
    CrstThreadpoolWaitThreads = 153,
    CrstThreadpoolWorker = 154,
    CrstThreadStaticDataHashTable = 155,
    CrstThreadStore = 156,
    CrstTPMethodTable = 157,
    CrstTypeEquivalenceMap = 158,
    CrstTypeIDMap = 159,
    CrstUMEntryThunkCache = 160,
    CrstUMThunkHash = 161,
    CrstUniqueStack = 162,
    CrstUnresolvedClassLock = 163,
    CrstUnwindInfoTableLock = 164,
    CrstVSDIndirectionCellLock = 165,
    CrstWinRTFactoryCache = 166,
    CrstWrapperTemplate = 167,
    kNumberOfCrstTypes = 168
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
    0,			// CrstDebuggerHeapExecMemLock
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
