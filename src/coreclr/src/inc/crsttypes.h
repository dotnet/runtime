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
    CrstExecuteManRangeLock = 34,
    CrstExternalObjectContextCache = 35,
    CrstFCall = 36,
    CrstFuncPtrStubs = 37,
    CrstFusionAppCtx = 38,
    CrstGCCover = 39,
    CrstGlobalStrLiteralMap = 40,
    CrstHandleTable = 41,
    CrstHostAssemblyMap = 42,
    CrstHostAssemblyMapAdd = 43,
    CrstIbcProfile = 44,
    CrstIJWFixupData = 45,
    CrstIJWHash = 46,
    CrstILStubGen = 47,
    CrstInlineTrackingMap = 48,
    CrstInstMethodHashTable = 49,
    CrstInterop = 50,
    CrstInteropData = 51,
    CrstIsJMCMethod = 52,
    CrstISymUnmanagedReader = 53,
    CrstJit = 54,
    CrstJitGenericHandleCache = 55,
    CrstJitInlineTrackingMap = 56,
    CrstJitPatchpoint = 57,
    CrstJitPerf = 58,
    CrstJumpStubCache = 59,
    CrstLeafLock = 60,
    CrstListLock = 61,
    CrstLoaderAllocator = 62,
    CrstLoaderAllocatorReferences = 63,
    CrstLoaderHeap = 64,
    CrstMethodDescBackpatchInfoTracker = 65,
    CrstModule = 66,
    CrstModuleFixup = 67,
    CrstModuleLookupTable = 68,
    CrstMulticoreJitHash = 69,
    CrstMulticoreJitManager = 70,
    CrstNativeImageEagerFixups = 71,
    CrstNativeImageLoad = 72,
    CrstNls = 73,
    CrstNotifyGdb = 74,
    CrstObjectList = 75,
    CrstPEImage = 76,
    CrstPendingTypeLoadEntry = 77,
    CrstPinnedByrefValidation = 78,
    CrstProfilerGCRefDataFreeList = 79,
    CrstProfilingAPIStatus = 80,
    CrstRCWCache = 81,
    CrstRCWCleanupList = 82,
    CrstReadyToRunEntryPointToMethodDescMap = 83,
    CrstReflection = 84,
    CrstReJITGlobalRequest = 85,
    CrstRetThunkCache = 86,
    CrstSavedExceptionInfo = 87,
    CrstSaveModuleProfileData = 88,
    CrstSecurityStackwalkCache = 89,
    CrstSigConvert = 90,
    CrstSingleUseLock = 91,
    CrstSpecialStatics = 92,
    CrstStackSampler = 93,
    CrstStressLog = 94,
    CrstStubCache = 95,
    CrstStubDispatchCache = 96,
    CrstStubUnwindInfoHeapSegments = 97,
    CrstSyncBlockCache = 98,
    CrstSyncHashLock = 99,
    CrstSystemBaseDomain = 100,
    CrstSystemDomain = 101,
    CrstSystemDomainDelayedUnloadList = 102,
    CrstThreadIdDispenser = 103,
    CrstThreadpoolTimerQueue = 104,
    CrstThreadpoolWaitThreads = 105,
    CrstThreadpoolWorker = 106,
    CrstThreadStore = 107,
    CrstTieredCompilation = 108,
    CrstTypeEquivalenceMap = 109,
    CrstTypeIDMap = 110,
    CrstUMEntryThunkCache = 111,
    CrstUniqueStack = 112,
    CrstUnresolvedClassLock = 113,
    CrstUnwindInfoTableLock = 114,
    CrstVSDIndirectionCellLock = 115,
    CrstWrapperTemplate = 116,
    kNumberOfCrstTypes = 117
};

#endif // __CRST_TYPES_INCLUDED

// Define some debug data in one module only -- vm\crst.cpp.
#if defined(__IN_CRST_CPP) && defined(_DEBUG)

// An array mapping CrstType to level.
int g_rgCrstLevelMap[] =
{
    10,         // CrstAppDomainCache
    14,         // CrstAppDomainHandleTable
    0,          // CrstArgBasedStubCache
    0,          // CrstAssemblyList
    7,          // CrstAssemblyLoader
    3,          // CrstAvailableClass
    4,          // CrstAvailableParamTypes
    7,          // CrstBaseDomain
    -1,         // CrstCCompRC
    13,         // CrstClassFactInfoHash
    11,         // CrstClassInit
    -1,         // CrstClrNotification
    6,          // CrstCodeFragmentHeap
    9,          // CrstCodeVersioning
    0,          // CrstCOMCallWrapper
    4,          // CrstCOMWrapperCache
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
    0,          // CrstExecuteManRangeLock
    0,          // CrstExternalObjectContextCache
    3,          // CrstFCall
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
    4,          // CrstInteropData
    0,          // CrstIsJMCMethod
    7,          // CrstISymUnmanagedReader
    11,         // CrstJit
    0,          // CrstJitGenericHandleCache
    16,         // CrstJitInlineTrackingMap
    3,          // CrstJitPatchpoint
    -1,         // CrstJitPerf
    6,          // CrstJumpStubCache
    0,          // CrstLeafLock
    -1,         // CrstListLock
    15,         // CrstLoaderAllocator
    16,         // CrstLoaderAllocatorReferences
    0,          // CrstLoaderHeap
    14,         // CrstMethodDescBackpatchInfoTracker
    4,          // CrstModule
    15,         // CrstModuleFixup
    3,          // CrstModuleLookupTable
    0,          // CrstMulticoreJitHash
    13,         // CrstMulticoreJitManager
    0,          // CrstNativeImageEagerFixups
    0,          // CrstNativeImageLoad
    0,          // CrstNls
    0,          // CrstNotifyGdb
    2,          // CrstObjectList
    4,          // CrstPEImage
    19,         // CrstPendingTypeLoadEntry
    0,          // CrstPinnedByrefValidation
    0,          // CrstProfilerGCRefDataFreeList
    0,          // CrstProfilingAPIStatus
    3,          // CrstRCWCache
    0,          // CrstRCWCleanupList
    10,         // CrstReadyToRunEntryPointToMethodDescMap
    8,          // CrstReflection
    17,         // CrstReJITGlobalRequest
    3,          // CrstRetThunkCache
    3,          // CrstSavedExceptionInfo
    0,          // CrstSaveModuleProfileData
    0,          // CrstSecurityStackwalkCache
    3,          // CrstSigConvert
    5,          // CrstSingleUseLock
    0,          // CrstSpecialStatics
    0,          // CrstStackSampler
    -1,         // CrstStressLog
    5,          // CrstStubCache
    0,          // CrstStubDispatchCache
    4,          // CrstStubUnwindInfoHeapSegments
    3,          // CrstSyncBlockCache
    0,          // CrstSyncHashLock
    4,          // CrstSystemBaseDomain
    13,         // CrstSystemDomain
    0,          // CrstSystemDomainDelayedUnloadList
    0,          // CrstThreadIdDispenser
    7,          // CrstThreadpoolTimerQueue
    7,          // CrstThreadpoolWaitThreads
    13,         // CrstThreadpoolWorker
    12,         // CrstThreadStore
    8,          // CrstTieredCompilation
    3,          // CrstTypeEquivalenceMap
    10,         // CrstTypeIDMap
    3,          // CrstUMEntryThunkCache
    3,          // CrstUniqueStack
    7,          // CrstUnresolvedClassLock
    3,          // CrstUnwindInfoTableLock
    3,          // CrstVSDIndirectionCellLock
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
