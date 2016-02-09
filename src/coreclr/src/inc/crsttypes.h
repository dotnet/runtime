// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    CrstClrNotification = 16,
    CrstCLRPrivBinderMaps = 17,
    CrstCLRPrivBinderMapsAdd = 18,
    CrstCodeFragmentHeap = 19,
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
    CrstEventStore = 42,
    CrstException = 43,
    CrstExecuteManLock = 44,
    CrstExecuteManRangeLock = 45,
    CrstFCall = 46,
    CrstFriendAccessCache = 47,
    CrstFuncPtrStubs = 48,
    CrstFusionAppCtx = 49,
    CrstFusionAssemblyDownload = 50,
    CrstFusionBindContext = 51,
    CrstFusionBindResult = 52,
    CrstFusionClb = 53,
    CrstFusionClosure = 54,
    CrstFusionClosureGraph = 55,
    CrstFusionConfigSettings = 56,
    CrstFusionDownload = 57,
    CrstFusionIsoLibInit = 58,
    CrstFusionLoadContext = 59,
    CrstFusionLog = 60,
    CrstFusionNgenIndex = 61,
    CrstFusionNgenIndexPool = 62,
    CrstFusionPcyCache = 63,
    CrstFusionPolicyConfigPool = 64,
    CrstFusionSingleUse = 65,
    CrstFusionWarningLog = 66,
    CrstGCMemoryPressure = 67,
    CrstGlobalStrLiteralMap = 68,
    CrstHandleTable = 69,
    CrstHostAssemblyMap = 70,
    CrstHostAssemblyMapAdd = 71,
    CrstIbcProfile = 72,
    CrstIJWFixupData = 73,
    CrstIJWHash = 74,
    CrstILFingerprintCache = 75,
    CrstILStubGen = 76,
    CrstInlineTrackingMap = 77,
    CrstInstMethodHashTable = 78,
    CrstInterfaceVTableMap = 79,
    CrstInterop = 80,
    CrstInteropData = 81,
    CrstIOThreadpoolWorker = 82,
    CrstIsJMCMethod = 83,
    CrstISymUnmanagedReader = 84,
    CrstJit = 85,
    CrstJitGenericHandleCache = 86,
    CrstJitPerf = 87,
    CrstJumpStubCache = 88,
    CrstLeafLock = 89,
    CrstListLock = 90,
    CrstLoaderAllocator = 91,
    CrstLoaderAllocatorReferences = 92,
    CrstLoaderHeap = 93,
    CrstMda = 94,
    CrstMetadataTracker = 95,
    CrstModIntPairList = 96,
    CrstModule = 97,
    CrstModuleFixup = 98,
    CrstModuleLookupTable = 99,
    CrstMulticoreJitHash = 100,
    CrstMulticoreJitManager = 101,
    CrstMUThunkHash = 102,
    CrstNativeBinderInit = 103,
    CrstNativeImageCache = 104,
    CrstNls = 105,
    CrstObjectList = 106,
    CrstOnEventManager = 107,
    CrstPatchEntryPoint = 108,
    CrstPEFileSecurityManager = 109,
    CrstPEImage = 110,
    CrstPEImagePDBStream = 111,
    CrstPendingTypeLoadEntry = 112,
    CrstPinHandle = 113,
    CrstPinnedByrefValidation = 114,
    CrstProfilerGCRefDataFreeList = 115,
    CrstProfilingAPIStatus = 116,
    CrstPublisherCertificate = 117,
    CrstRCWCache = 118,
    CrstRCWCleanupList = 119,
    CrstRCWRefCache = 120,
    CrstReDacl = 121,
    CrstReflection = 122,
    CrstReJITDomainTable = 123,
    CrstReJITGlobalRequest = 124,
    CrstReJITSharedDomainTable = 125,
    CrstRemoting = 126,
    CrstRetThunkCache = 127,
    CrstRWLock = 128,
    CrstSavedExceptionInfo = 129,
    CrstSaveModuleProfileData = 130,
    CrstSecurityPolicyCache = 131,
    CrstSecurityPolicyInit = 132,
    CrstSecurityStackwalkCache = 133,
    CrstSharedAssemblyCreate = 134,
    CrstSharedBaseDomain = 135,
    CrstSigConvert = 136,
    CrstSingleUseLock = 137,
    CrstSpecialStatics = 138,
    CrstSqmManager = 139,
    CrstStackSampler = 140,
    CrstStressLog = 141,
    CrstStrongName = 142,
    CrstStubCache = 143,
    CrstStubDispatchCache = 144,
    CrstStubUnwindInfoHeapSegments = 145,
    CrstSyncBlockCache = 146,
    CrstSyncHashLock = 147,
    CrstSystemBaseDomain = 148,
    CrstSystemDomain = 149,
    CrstSystemDomainDelayedUnloadList = 150,
    CrstThreadIdDispenser = 151,
    CrstThreadpoolEventCache = 152,
    CrstThreadpoolTimerQueue = 153,
    CrstThreadpoolWaitThreads = 154,
    CrstThreadpoolWorker = 155,
    CrstThreadStaticDataHashTable = 156,
    CrstThreadStore = 157,
    CrstTPMethodTable = 158,
    CrstTypeEquivalenceMap = 159,
    CrstTypeIDMap = 160,
    CrstUMEntryThunkCache = 161,
    CrstUMThunkHash = 162,
    CrstUniqueStack = 163,
    CrstUnresolvedClassLock = 164,
    CrstUnwindInfoTableLock = 165,
    CrstVSDIndirectionCellLock = 166,
    CrstWinRTFactoryCache = 167,
    CrstWrapperTemplate = 168,
    kNumberOfCrstTypes = 169
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
    -1,			// CrstClrNotification
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
    "CrstClrNotification",
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
