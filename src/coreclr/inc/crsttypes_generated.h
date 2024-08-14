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
    CrstPEImage = 75,
    CrstPendingTypeLoadEntry = 76,
    CrstPerfMap = 77,
    CrstPgoData = 78,
    CrstPinnedByrefValidation = 79,
    CrstPinnedHeapHandleTable = 80,
    CrstProfilerGCRefDataFreeList = 81,
    CrstProfilingAPIStatus = 82,
    CrstRCWCache = 83,
    CrstRCWCleanupList = 84,
    CrstReadyToRunEntryPointToMethodDescMap = 85,
    CrstReflection = 86,
    CrstReJITGlobalRequest = 87,
    CrstRetThunkCache = 88,
    CrstSavedExceptionInfo = 89,
    CrstSaveModuleProfileData = 90,
    CrstSigConvert = 91,
    CrstSingleUseLock = 92,
    CrstSpecialStatics = 93,
    CrstStackSampler = 94,
    CrstStressLog = 95,
    CrstStubCache = 96,
    CrstStubDispatchCache = 97,
    CrstStubUnwindInfoHeapSegments = 98,
    CrstSyncBlockCache = 99,
    CrstSyncHashLock = 100,
    CrstSystemBaseDomain = 101,
    CrstSystemDomain = 102,
    CrstSystemDomainDelayedUnloadList = 103,
    CrstThreadIdDispenser = 104,
    CrstThreadLocalStorageLock = 105,
    CrstThreadStore = 106,
    CrstTieredCompilation = 107,
    CrstTypeEquivalenceMap = 108,
    CrstTypeIDMap = 109,
    CrstUMEntryThunkCache = 110,
    CrstUMEntryThunkFreeListLock = 111,
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
    9,          // CrstAppDomainCache
    2,          // CrstArgBasedStubCache
    2,          // CrstAssemblyList
    13,         // CrstAssemblyLoader
    3,          // CrstAvailableClass
    4,          // CrstAvailableParamTypes
    6,          // CrstBaseDomain
    -1,         // CrstCCompRC
    14,         // CrstClassFactInfoHash
    10,         // CrstClassInit
    -1,         // CrstClrNotification
    5,          // CrstCodeFragmentHeap
    8,          // CrstCodeVersioning
    2,          // CrstCOMCallWrapper
    9,          // CrstCOMWrapperCache
    2,          // CrstDataTest1
    0,          // CrstDataTest2
    0,          // CrstDbgTransport
    0,          // CrstDeadlockDetection
    -1,         // CrstDebuggerController
    2,          // CrstDebuggerFavorLock
    0,          // CrstDebuggerHeapExecMemLock
    0,          // CrstDebuggerHeapLock
    3,          // CrstDebuggerJitInfo
    12,         // CrstDebuggerMutex
    0,          // CrstDelegateToFPtrHash
    0,          // CrstDynamicIL
    9,          // CrstDynamicMT
    0,          // CrstEtwTypeLogHash
    19,         // CrstEventPipe
    0,          // CrstEventStore
    0,          // CrstException
    0,          // CrstExecutableAllocatorLock
    0,          // CrstExecuteManRangeLock
    3,          // CrstFCall
    -1,         // CrstFrozenObjectHeap
    6,          // CrstFuncPtrStubs
    9,          // CrstFusionAppCtx
    9,          // CrstGCCover
    17,         // CrstGenericDictionaryExpansion
    16,         // CrstGlobalStrLiteralMap
    1,          // CrstHandleTable
    0,          // CrstIbcProfile
    7,          // CrstIJWFixupData
    0,          // CrstIJWHash
    6,          // CrstILStubGen
    2,          // CrstInlineTrackingMap
    18,         // CrstInstMethodHashTable
    21,         // CrstInterop
    9,          // CrstInteropData
    0,          // CrstIsJMCMethod
    6,          // CrstISymUnmanagedReader
    10,         // CrstJit
    0,          // CrstJitGenericHandleCache
    11,         // CrstJitInlineTrackingMap
    3,          // CrstJitPatchpoint
    -1,         // CrstJitPerf
    5,          // CrstJumpStubCache
    0,          // CrstLeafLock
    -1,         // CrstListLock
    16,         // CrstLoaderAllocator
    17,         // CrstLoaderAllocatorReferences
    2,          // CrstLoaderHeap
    2,          // CrstManagedObjectWrapperMap
    9,          // CrstMethodDescBackpatchInfoTracker
    -1,         // CrstMethodTableExposedObject
    4,          // CrstModule
    17,         // CrstModuleFixup
    3,          // CrstModuleLookupTable
    0,          // CrstMulticoreJitHash
    14,         // CrstMulticoreJitManager
    2,          // CrstNativeImageEagerFixups
    0,          // CrstNativeImageLoad
    0,          // CrstNls
    0,          // CrstNotifyGdb
    4,          // CrstPEImage
    20,         // CrstPendingTypeLoadEntry
    0,          // CrstPerfMap
    3,          // CrstPgoData
    0,          // CrstPinnedByrefValidation
    15,         // CrstPinnedHeapHandleTable
    0,          // CrstProfilerGCRefDataFreeList
    14,         // CrstProfilingAPIStatus
    3,          // CrstRCWCache
    0,          // CrstRCWCleanupList
    9,          // CrstReadyToRunEntryPointToMethodDescMap
    7,          // CrstReflection
    15,         // CrstReJITGlobalRequest
    3,          // CrstRetThunkCache
    2,          // CrstSavedExceptionInfo
    0,          // CrstSaveModuleProfileData
    3,          // CrstSigConvert
    4,          // CrstSingleUseLock
    0,          // CrstSpecialStatics
    0,          // CrstStackSampler
    -1,         // CrstStressLog
    4,          // CrstStubCache
    0,          // CrstStubDispatchCache
    3,          // CrstStubUnwindInfoHeapSegments
    2,          // CrstSyncBlockCache
    0,          // CrstSyncHashLock
    4,          // CrstSystemBaseDomain
    14,         // CrstSystemDomain
    0,          // CrstSystemDomainDelayedUnloadList
    0,          // CrstThreadIdDispenser
    4,          // CrstThreadLocalStorageLock
    13,         // CrstThreadStore
    7,          // CrstTieredCompilation
    3,          // CrstTypeEquivalenceMap
    9,          // CrstTypeIDMap
    3,          // CrstUMEntryThunkCache
    2,          // CrstUMEntryThunkFreeListLock
    3,          // CrstUniqueStack
    6,          // CrstUnresolvedClassLock
    2,          // CrstUnwindInfoTableLock
    3,          // CrstVSDIndirectionCellLock
    2,          // CrstWrapperTemplate
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
