//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
    CrstAppDomainCache = 0,
    CrstAppDomainHandleTable = 1,
    CrstArgBasedStubCache = 2,
    CrstAssemblyList = 3,
    CrstAssemblyLoader = 4,
    CrstAvailableClass = 5,
    CrstAvailableParamTypes = 6,
    CrstBaseDomain = 7,
    CrstCCompRC = 8,
    CrstClassFactInfoHash = 9,
    CrstClassInit = 10,
    CrstClrNotification = 11,
    CrstCodeFragmentHeap = 12,
    CrstCodeVersioning = 13,
    CrstCOMCallWrapper = 14,
    CrstCOMWrapperCache = 15,
    CrstDataTest1 = 16,
    CrstDataTest2 = 17,
    CrstDbgTransport = 18,
    CrstDeadlockDetection = 19,
    CrstDebuggerController = 20,
    CrstDebuggerFavorLock = 21,
    CrstDebuggerHeapExecMemLock = 22,
    CrstDebuggerHeapLock = 23,
    CrstDebuggerJitInfo = 24,
    CrstDebuggerMutex = 25,
    CrstDelegateToFPtrHash = 26,
    CrstDomainLocalBlock = 27,
    CrstDynamicIL = 28,
    CrstDynamicMT = 29,
    CrstEtwTypeLogHash = 30,
    CrstEventPipe = 31,
    CrstEventStore = 32,
    CrstException = 33,
    CrstExecutableAllocatorLock = 34,
    CrstExecuteManRangeLock = 35,
    CrstExternalObjectContextCache = 36,
    CrstFCall = 37,
    CrstFuncPtrStubs = 38,
    CrstFusionAppCtx = 39,
    CrstGCCover = 40,
    CrstGlobalStrLiteralMap = 41,
    CrstHandleTable = 42,
    CrstHostAssemblyMap = 43,
    CrstHostAssemblyMapAdd = 44,
    CrstIbcProfile = 45,
    CrstIJWFixupData = 46,
    CrstIJWHash = 47,
    CrstILStubGen = 48,
    CrstInlineTrackingMap = 49,
    CrstInstMethodHashTable = 50,
    CrstInterop = 51,
    CrstInteropData = 52,
    CrstIsJMCMethod = 53,
    CrstISymUnmanagedReader = 54,
    CrstJit = 55,
    CrstJitGenericHandleCache = 56,
    CrstJitInlineTrackingMap = 57,
    CrstJitPatchpoint = 58,
    CrstJitPerf = 59,
    CrstJumpStubCache = 60,
    CrstLeafLock = 61,
    CrstListLock = 62,
    CrstLoaderAllocator = 63,
    CrstLoaderAllocatorReferences = 64,
    CrstLoaderHeap = 65,
    CrstManagedObjectWrapperMap = 66,
    CrstMethodDescBackpatchInfoTracker = 67,
    CrstModule = 68,
    CrstModuleFixup = 69,
    CrstModuleLookupTable = 70,
    CrstMulticoreJitHash = 71,
    CrstMulticoreJitManager = 72,
    CrstNativeImageEagerFixups = 73,
    CrstNativeImageLoad = 74,
    CrstNls = 75,
    CrstNotifyGdb = 76,
    CrstObjectList = 77,
    CrstPEImage = 78,
    CrstPendingTypeLoadEntry = 79,
    CrstPgoData = 80,
    CrstPinnedByrefValidation = 81,
    CrstProfilerGCRefDataFreeList = 82,
    CrstProfilingAPIStatus = 83,
    CrstRCWCache = 84,
    CrstRCWCleanupList = 85,
    CrstReadyToRunEntryPointToMethodDescMap = 86,
    CrstReflection = 87,
    CrstReJITGlobalRequest = 88,
    CrstRetThunkCache = 89,
    CrstSavedExceptionInfo = 90,
    CrstSaveModuleProfileData = 91,
    CrstSecurityStackwalkCache = 92,
    CrstSigConvert = 93,
    CrstSingleUseLock = 94,
    CrstSpecialStatics = 95,
    CrstStackSampler = 96,
    CrstStressLog = 97,
    CrstStubCache = 98,
    CrstStubDispatchCache = 99,
    CrstStubUnwindInfoHeapSegments = 100,
    CrstSyncBlockCache = 101,
    CrstSyncHashLock = 102,
    CrstSystemBaseDomain = 103,
    CrstSystemDomain = 104,
    CrstSystemDomainDelayedUnloadList = 105,
    CrstThreadIdDispenser = 106,
    CrstThreadpoolTimerQueue = 107,
    CrstThreadpoolWaitThreads = 108,
    CrstThreadpoolWorker = 109,
    CrstThreadStore = 110,
    CrstTieredCompilation = 111,
    CrstTypeEquivalenceMap = 112,
    CrstTypeIDMap = 113,
    CrstUMEntryThunkCache = 114,
    CrstUMEntryThunkFreeListLock = 115,
    CrstUniqueStack = 116,
    CrstUnresolvedClassLock = 117,
    CrstUnwindInfoTableLock = 118,
    CrstVSDIndirectionCellLock = 119,
    CrstWrapperTemplate = 120,
    kNumberOfCrstTypes = 121
};

#endif // __CRST_TYPES_INCLUDED

// Define some debug data in one module only -- vm\crst.cpp.
#if defined(__IN_CRST_CPP) && defined(_DEBUG)

// An array mapping CrstType to level.
int g_rgCrstLevelMap[] =
{
    10,         // CrstAppDomainCache
    14,         // CrstAppDomainHandleTable
    3,          // CrstArgBasedStubCache
    0,          // CrstAssemblyList
    12,         // CrstAssemblyLoader
    4,          // CrstAvailableClass
    5,          // CrstAvailableParamTypes
    7,          // CrstBaseDomain
    -1,         // CrstCCompRC
    13,         // CrstClassFactInfoHash
    11,         // CrstClassInit
    -1,         // CrstClrNotification
    6,          // CrstCodeFragmentHeap
    9,          // CrstCodeVersioning
    0,          // CrstCOMCallWrapper
    5,          // CrstCOMWrapperCache
    3,          // CrstDataTest1
    0,          // CrstDataTest2
    0,          // CrstDbgTransport
    0,          // CrstDeadlockDetection
    -1,         // CrstDebuggerController
    3,          // CrstDebuggerFavorLock
    0,          // CrstDebuggerHeapExecMemLock
    0,          // CrstDebuggerHeapLock
    4,          // CrstDebuggerJitInfo
    10,         // CrstDebuggerMutex
    0,          // CrstDelegateToFPtrHash
    16,         // CrstDomainLocalBlock
    0,          // CrstDynamicIL
    3,          // CrstDynamicMT
    0,          // CrstEtwTypeLogHash
    18,         // CrstEventPipe
    0,          // CrstEventStore
    0,          // CrstException
    0,          // CrstExecutableAllocatorLock
    0,          // CrstExecuteManRangeLock
    0,          // CrstExternalObjectContextCache
    4,          // CrstFCall
    7,          // CrstFuncPtrStubs
    10,         // CrstFusionAppCtx
    10,         // CrstGCCover
    13,         // CrstGlobalStrLiteralMap
    1,          // CrstHandleTable
    0,          // CrstHostAssemblyMap
    3,          // CrstHostAssemblyMapAdd
    0,          // CrstIbcProfile
    8,          // CrstIJWFixupData
    0,          // CrstIJWHash
    7,          // CrstILStubGen
    3,          // CrstInlineTrackingMap
    17,         // CrstInstMethodHashTable
    20,         // CrstInterop
    5,          // CrstInteropData
    0,          // CrstIsJMCMethod
    7,          // CrstISymUnmanagedReader
    11,         // CrstJit
    0,          // CrstJitGenericHandleCache
    16,         // CrstJitInlineTrackingMap
    4,          // CrstJitPatchpoint
    -1,         // CrstJitPerf
    6,          // CrstJumpStubCache
    0,          // CrstLeafLock
    -1,         // CrstListLock
    15,         // CrstLoaderAllocator
    16,         // CrstLoaderAllocatorReferences
    3,          // CrstLoaderHeap
    3,          // CrstManagedObjectWrapperMap
    14,         // CrstMethodDescBackpatchInfoTracker
    5,          // CrstModule
    15,         // CrstModuleFixup
    4,          // CrstModuleLookupTable
    0,          // CrstMulticoreJitHash
    13,         // CrstMulticoreJitManager
    0,          // CrstNativeImageEagerFixups
    0,          // CrstNativeImageLoad
    0,          // CrstNls
    0,          // CrstNotifyGdb
    2,          // CrstObjectList
    5,          // CrstPEImage
    19,         // CrstPendingTypeLoadEntry
    4,          // CrstPgoData
    0,          // CrstPinnedByrefValidation
    0,          // CrstProfilerGCRefDataFreeList
    0,          // CrstProfilingAPIStatus
    4,          // CrstRCWCache
    0,          // CrstRCWCleanupList
    10,         // CrstReadyToRunEntryPointToMethodDescMap
    8,          // CrstReflection
    17,         // CrstReJITGlobalRequest
    4,          // CrstRetThunkCache
    3,          // CrstSavedExceptionInfo
    0,          // CrstSaveModuleProfileData
    0,          // CrstSecurityStackwalkCache
    4,          // CrstSigConvert
    5,          // CrstSingleUseLock
    0,          // CrstSpecialStatics
    0,          // CrstStackSampler
    -1,         // CrstStressLog
    5,          // CrstStubCache
    0,          // CrstStubDispatchCache
    4,          // CrstStubUnwindInfoHeapSegments
    3,          // CrstSyncBlockCache
    0,          // CrstSyncHashLock
    5,          // CrstSystemBaseDomain
    13,         // CrstSystemDomain
    0,          // CrstSystemDomainDelayedUnloadList
    0,          // CrstThreadIdDispenser
    7,          // CrstThreadpoolTimerQueue
    7,          // CrstThreadpoolWaitThreads
    13,         // CrstThreadpoolWorker
    12,         // CrstThreadStore
    8,          // CrstTieredCompilation
    4,          // CrstTypeEquivalenceMap
    10,         // CrstTypeIDMap
    4,          // CrstUMEntryThunkCache
    3,          // CrstUMEntryThunkFreeListLock
    4,          // CrstUniqueStack
    7,          // CrstUnresolvedClassLock
    3,          // CrstUnwindInfoTableLock
    4,          // CrstVSDIndirectionCellLock
    3,          // CrstWrapperTemplate
};

// An array mapping CrstType to a stringized name.
LPCSTR g_rgCrstNameMap[] =
{
    "CrstAppDomainCache",
    "CrstAppDomainHandleTable",
    "CrstArgBasedStubCache",
    "CrstAssemblyList",
    "CrstAssemblyLoader",
    "CrstAvailableClass",
    "CrstAvailableParamTypes",
    "CrstBaseDomain",
    "CrstCCompRC",
    "CrstClassFactInfoHash",
    "CrstClassInit",
    "CrstClrNotification",
    "CrstCodeFragmentHeap",
    "CrstCodeVersioning",
    "CrstCOMCallWrapper",
    "CrstCOMWrapperCache",
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
    "CrstEtwTypeLogHash",
    "CrstEventPipe",
    "CrstEventStore",
    "CrstException",
    "CrstExecutableAllocatorLock",
    "CrstExecuteManRangeLock",
    "CrstExternalObjectContextCache",
    "CrstFCall",
    "CrstFuncPtrStubs",
    "CrstFusionAppCtx",
    "CrstGCCover",
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
    "CrstInterop",
    "CrstInteropData",
    "CrstIsJMCMethod",
    "CrstISymUnmanagedReader",
    "CrstJit",
    "CrstJitGenericHandleCache",
    "CrstJitInlineTrackingMap",
    "CrstJitPatchpoint",
    "CrstJitPerf",
    "CrstJumpStubCache",
    "CrstLeafLock",
    "CrstListLock",
    "CrstLoaderAllocator",
    "CrstLoaderAllocatorReferences",
    "CrstLoaderHeap",
    "CrstManagedObjectWrapperMap",
    "CrstMethodDescBackpatchInfoTracker",
    "CrstModule",
    "CrstModuleFixup",
    "CrstModuleLookupTable",
    "CrstMulticoreJitHash",
    "CrstMulticoreJitManager",
    "CrstNativeImageEagerFixups",
    "CrstNativeImageLoad",
    "CrstNls",
    "CrstNotifyGdb",
    "CrstObjectList",
    "CrstPEImage",
    "CrstPendingTypeLoadEntry",
    "CrstPgoData",
    "CrstPinnedByrefValidation",
    "CrstProfilerGCRefDataFreeList",
    "CrstProfilingAPIStatus",
    "CrstRCWCache",
    "CrstRCWCleanupList",
    "CrstReadyToRunEntryPointToMethodDescMap",
    "CrstReflection",
    "CrstReJITGlobalRequest",
    "CrstRetThunkCache",
    "CrstSavedExceptionInfo",
    "CrstSaveModuleProfileData",
    "CrstSecurityStackwalkCache",
    "CrstSigConvert",
    "CrstSingleUseLock",
    "CrstSpecialStatics",
    "CrstStackSampler",
    "CrstStressLog",
    "CrstStubCache",
    "CrstStubDispatchCache",
    "CrstStubUnwindInfoHeapSegments",
    "CrstSyncBlockCache",
    "CrstSyncHashLock",
    "CrstSystemBaseDomain",
    "CrstSystemDomain",
    "CrstSystemDomainDelayedUnloadList",
    "CrstThreadIdDispenser",
    "CrstThreadpoolTimerQueue",
    "CrstThreadpoolWaitThreads",
    "CrstThreadpoolWorker",
    "CrstThreadStore",
    "CrstTieredCompilation",
    "CrstTypeEquivalenceMap",
    "CrstTypeIDMap",
    "CrstUMEntryThunkCache",
    "CrstUMEntryThunkFreeListLock",
    "CrstUniqueStack",
    "CrstUnresolvedClassLock",
    "CrstUnwindInfoTableLock",
    "CrstVSDIndirectionCellLock",
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
