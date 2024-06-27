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
    CrstArgBasedStubCache = 1,
    CrstAssemblyList = 2,
    CrstAssemblyLoader = 3,
    CrstAvailableClass = 4,
    CrstAvailableParamTypes = 5,
    CrstBaseDomain = 6,
    CrstCCompRC = 7,
    CrstClassFactInfoHash = 8,
    CrstClassInit = 9,
    CrstClrNotification = 10,
    CrstCodeFragmentHeap = 11,
    CrstCodeVersioning = 12,
    CrstCOMCallWrapper = 13,
    CrstCOMWrapperCache = 14,
    CrstDataTest1 = 15,
    CrstDataTest2 = 16,
    CrstDbgTransport = 17,
    CrstDeadlockDetection = 18,
    CrstDebuggerController = 19,
    CrstDebuggerFavorLock = 20,
    CrstDebuggerHeapExecMemLock = 21,
    CrstDebuggerHeapLock = 22,
    CrstDebuggerJitInfo = 23,
    CrstDebuggerMutex = 24,
    CrstDelegateToFPtrHash = 25,
    CrstDynamicIL = 26,
    CrstDynamicMT = 27,
    CrstEtwTypeLogHash = 28,
    CrstEventPipe = 29,
    CrstEventStore = 30,
    CrstException = 31,
    CrstExecutableAllocatorLock = 32,
    CrstExecuteManRangeLock = 33,
    CrstFCall = 34,
    CrstFrozenObjectHeap = 35,
    CrstFuncPtrStubs = 36,
    CrstFusionAppCtx = 37,
    CrstGCCover = 38,
    CrstGenericDictionaryExpansion = 39,
    CrstGlobalStrLiteralMap = 40,
    CrstHandleTable = 41,
    CrstIbcProfile = 42,
    CrstIJWFixupData = 43,
    CrstIJWHash = 44,
    CrstILStubGen = 45,
    CrstInlineTrackingMap = 46,
    CrstInstMethodHashTable = 47,
    CrstInterop = 48,
    CrstInteropData = 49,
    CrstIsJMCMethod = 50,
    CrstISymUnmanagedReader = 51,
    CrstJit = 52,
    CrstJitGenericHandleCache = 53,
    CrstJitInlineTrackingMap = 54,
    CrstJitPatchpoint = 55,
    CrstJitPerf = 56,
    CrstJumpStubCache = 57,
    CrstLeafLock = 58,
    CrstListLock = 59,
    CrstLoaderAllocator = 60,
    CrstLoaderAllocatorReferences = 61,
    CrstLoaderHeap = 62,
    CrstManagedObjectWrapperMap = 63,
    CrstMethodDescBackpatchInfoTracker = 64,
    CrstMethodTableExposedObject = 65,
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
    CrstPerfMap = 78,
    CrstPgoData = 79,
    CrstPinnedByrefValidation = 80,
    CrstPinnedHeapHandleTable = 81,
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
    CrstSigConvert = 92,
    CrstSingleUseLock = 93,
    CrstSpecialStatics = 94,
    CrstStackSampler = 95,
    CrstStressLog = 96,
    CrstStubCache = 97,
    CrstStubDispatchCache = 98,
    CrstStubUnwindInfoHeapSegments = 99,
    CrstSyncBlockCache = 100,
    CrstSyncHashLock = 101,
    CrstSystemBaseDomain = 102,
    CrstSystemDomain = 103,
    CrstSystemDomainDelayedUnloadList = 104,
    CrstThreadIdDispenser = 105,
    CrstThreadLocalStorageLock = 106,
    CrstThreadStore = 107,
    CrstTieredCompilation = 108,
    CrstTypeEquivalenceMap = 109,
    CrstTypeIDMap = 110,
    CrstUMEntryThunkCache = 111,
    CrstUMEntryThunkFreeListLock = 112,
    CrstUniqueStack = 113,
    CrstUnresolvedClassLock = 114,
    CrstUnwindInfoTableLock = 115,
    CrstVSDIndirectionCellLock = 116,
    CrstWrapperTemplate = 117,
    kNumberOfCrstTypes = 118
};

#endif // __CRST_TYPES_INCLUDED

// Define some debug data in one module only -- vm\crst.cpp.
#if defined(__IN_CRST_CPP) && defined(_DEBUG)

// An array mapping CrstType to level.
int g_rgCrstLevelMap[] =
{
    10,         // CrstAppDomainCache
    3,          // CrstArgBasedStubCache
    3,          // CrstAssemblyList
    14,         // CrstAssemblyLoader
    4,          // CrstAvailableClass
    5,          // CrstAvailableParamTypes
    7,          // CrstBaseDomain
    -1,         // CrstCCompRC
    15,         // CrstClassFactInfoHash
    11,         // CrstClassInit
    -1,         // CrstClrNotification
    6,          // CrstCodeFragmentHeap
    9,          // CrstCodeVersioning
    3,          // CrstCOMCallWrapper
    10,         // CrstCOMWrapperCache
    3,          // CrstDataTest1
    0,          // CrstDataTest2
    0,          // CrstDbgTransport
    0,          // CrstDeadlockDetection
    -1,         // CrstDebuggerController
    3,          // CrstDebuggerFavorLock
    0,          // CrstDebuggerHeapExecMemLock
    0,          // CrstDebuggerHeapLock
    4,          // CrstDebuggerJitInfo
    13,         // CrstDebuggerMutex
    0,          // CrstDelegateToFPtrHash
    0,          // CrstDynamicIL
    10,         // CrstDynamicMT
    0,          // CrstEtwTypeLogHash
    20,         // CrstEventPipe
    0,          // CrstEventStore
    0,          // CrstException
    0,          // CrstExecutableAllocatorLock
    0,          // CrstExecuteManRangeLock
    4,          // CrstFCall
    -1,         // CrstFrozenObjectHeap
    7,          // CrstFuncPtrStubs
    10,         // CrstFusionAppCtx
    10,         // CrstGCCover
    18,         // CrstGenericDictionaryExpansion
    17,         // CrstGlobalStrLiteralMap
    1,          // CrstHandleTable
    0,          // CrstIbcProfile
    8,          // CrstIJWFixupData
    0,          // CrstIJWHash
    7,          // CrstILStubGen
    3,          // CrstInlineTrackingMap
    19,         // CrstInstMethodHashTable
    22,         // CrstInterop
    10,         // CrstInteropData
    0,          // CrstIsJMCMethod
    7,          // CrstISymUnmanagedReader
    11,         // CrstJit
    0,          // CrstJitGenericHandleCache
    12,         // CrstJitInlineTrackingMap
    4,          // CrstJitPatchpoint
    -1,         // CrstJitPerf
    6,          // CrstJumpStubCache
    0,          // CrstLeafLock
    -1,         // CrstListLock
    17,         // CrstLoaderAllocator
    18,         // CrstLoaderAllocatorReferences
    3,          // CrstLoaderHeap
    3,          // CrstManagedObjectWrapperMap
    10,         // CrstMethodDescBackpatchInfoTracker
    -1,         // CrstMethodTableExposedObject
    5,          // CrstModule
    18,         // CrstModuleFixup
    4,          // CrstModuleLookupTable
    0,          // CrstMulticoreJitHash
    15,         // CrstMulticoreJitManager
    3,          // CrstNativeImageEagerFixups
    0,          // CrstNativeImageLoad
    0,          // CrstNls
    0,          // CrstNotifyGdb
    2,          // CrstObjectList
    5,          // CrstPEImage
    21,         // CrstPendingTypeLoadEntry
    0,          // CrstPerfMap
    4,          // CrstPgoData
    0,          // CrstPinnedByrefValidation
    16,         // CrstPinnedHeapHandleTable
    0,          // CrstProfilerGCRefDataFreeList
    15,         // CrstProfilingAPIStatus
    4,          // CrstRCWCache
    0,          // CrstRCWCleanupList
    10,         // CrstReadyToRunEntryPointToMethodDescMap
    8,          // CrstReflection
    16,         // CrstReJITGlobalRequest
    4,          // CrstRetThunkCache
    3,          // CrstSavedExceptionInfo
    0,          // CrstSaveModuleProfileData
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
    15,         // CrstSystemDomain
    0,          // CrstSystemDomainDelayedUnloadList
    0,          // CrstThreadIdDispenser
    5,          // CrstThreadLocalStorageLock
    14,         // CrstThreadStore
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
    "CrstDynamicIL",
    "CrstDynamicMT",
    "CrstEtwTypeLogHash",
    "CrstEventPipe",
    "CrstEventStore",
    "CrstException",
    "CrstExecutableAllocatorLock",
    "CrstExecuteManRangeLock",
    "CrstFCall",
    "CrstFrozenObjectHeap",
    "CrstFuncPtrStubs",
    "CrstFusionAppCtx",
    "CrstGCCover",
    "CrstGenericDictionaryExpansion",
    "CrstGlobalStrLiteralMap",
    "CrstHandleTable",
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
    "CrstMethodTableExposedObject",
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
    "CrstPerfMap",
    "CrstPgoData",
    "CrstPinnedByrefValidation",
    "CrstPinnedHeapHandleTable",
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
    "CrstThreadLocalStorageLock",
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
