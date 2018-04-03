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
    CrstEventPipe = 41,
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
    CrstNotifyGdb = 106,
    CrstObjectList = 107,
    CrstOnEventManager = 108,
    CrstPatchEntryPoint = 109,
    CrstPEFileSecurityManager = 110,
    CrstPEImage = 111,
    CrstPEImagePDBStream = 112,
    CrstPendingTypeLoadEntry = 113,
    CrstPinHandle = 114,
    CrstPinnedByrefValidation = 115,
    CrstProfilerGCRefDataFreeList = 116,
    CrstProfilingAPIStatus = 117,
    CrstPublisherCertificate = 118,
    CrstRCWCache = 119,
    CrstRCWCleanupList = 120,
    CrstRCWRefCache = 121,
    CrstReadyToRunEntryPointToMethodDescMap = 122,
    CrstReDacl = 123,
    CrstReflection = 124,
    CrstReJITDomainTable = 125,
    CrstReJITGlobalRequest = 126,
    CrstReJITSharedDomainTable = 127,
    CrstRemoting = 128,
    CrstRetThunkCache = 129,
    CrstRWLock = 130,
    CrstSavedExceptionInfo = 131,
    CrstSaveModuleProfileData = 132,
    CrstSecurityPolicyCache = 133,
    CrstSecurityPolicyInit = 134,
    CrstSecurityStackwalkCache = 135,
    CrstSharedAssemblyCreate = 136,
    CrstSharedBaseDomain = 137,
    CrstSigConvert = 138,
    CrstSingleUseLock = 139,
    CrstSpecialStatics = 140,
    CrstSqmManager = 141,
    CrstStackSampler = 142,
    CrstStressLog = 143,
    CrstStrongName = 144,
    CrstStubCache = 145,
    CrstStubDispatchCache = 146,
    CrstStubUnwindInfoHeapSegments = 147,
    CrstSyncBlockCache = 148,
    CrstSyncHashLock = 149,
    CrstSystemBaseDomain = 150,
    CrstSystemDomain = 151,
    CrstSystemDomainDelayedUnloadList = 152,
    CrstThreadIdDispenser = 153,
    CrstThreadpoolEventCache = 154,
    CrstThreadpoolTimerQueue = 155,
    CrstThreadpoolWaitThreads = 156,
    CrstThreadpoolWorker = 157,
    CrstThreadStaticDataHashTable = 158,
    CrstThreadStore = 159,
    CrstTPMethodTable = 160,
    CrstTypeEquivalenceMap = 161,
    CrstTypeIDMap = 162,
    CrstUMEntryThunkCache = 163,
    CrstUMThunkHash = 164,
    CrstUniqueStack = 165,
    CrstUnresolvedClassLock = 166,
    CrstUnwindInfoTableLock = 167,
    CrstVSDIndirectionCellLock = 168,
    CrstWinRTFactoryCache = 169,
    CrstWrapperTemplate = 170,
    kNumberOfCrstTypes = 171
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
    17,			// CrstEventPipe
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
    0,			// CrstNotifyGdb
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
    3,			// CrstReadyToRunEntryPointToMethodDescMap
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
    "CrstEventPipe",
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
    "CrstNotifyGdb",
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
    "CrstReadyToRunEntryPointToMethodDescMap",
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
