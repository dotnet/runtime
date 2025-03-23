//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef __CRST_TYPES_INCLUDED
#define __CRST_TYPES_INCLUDED

// **** THIS IS AN AUTOMATICALLY GENERATED HEADER FILE -- DO NOT EDIT!!! ****

// This file describes the range of Crst types available and their mapping to a numeric level (used by the
// runtime in debug mode to validate we're deadlock free). To modify these settings edit the
// file:CrstTypes.def file and run the .\CrstTypeTool utility to generate a new version of this file.

// Each Crst type is declared as a value in the following CrstType enum.
enum CrstType
{
    CrstAppDomainCache = 0,
    CrstAssemblyList = 1,
    CrstAssemblyLoader = 2,
    CrstAvailableClass = 3,
    CrstAvailableParamTypes = 4,
    CrstCCompRC = 5,
    CrstClassFactInfoHash = 6,
    CrstClassInit = 7,
    CrstClrNotification = 8,
    CrstCodeFragmentHeap = 9,
    CrstCodeVersioning = 10,
    CrstCOMCallWrapper = 11,
    CrstCOMWrapperCache = 12,
    CrstDataTest1 = 13,
    CrstDataTest2 = 14,
    CrstDbgTransport = 15,
    CrstDeadlockDetection = 16,
    CrstDebuggerController = 17,
    CrstDebuggerFavorLock = 18,
    CrstDebuggerHeapExecMemLock = 19,
    CrstDebuggerHeapLock = 20,
    CrstDebuggerJitInfo = 21,
    CrstDebuggerMutex = 22,
    CrstDynamicIL = 23,
    CrstDynamicMT = 24,
    CrstEtwTypeLogHash = 25,
    CrstEventPipe = 26,
    CrstEventStore = 27,
    CrstException = 28,
    CrstExecutableAllocatorLock = 29,
    CrstFCall = 30,
    CrstFrozenObjectHeap = 31,
    CrstFuncPtrStubs = 32,
    CrstFusionAppCtx = 33,
    CrstGCCover = 34,
    CrstGenericDictionaryExpansion = 35,
    CrstGlobalStrLiteralMap = 36,
    CrstHandleTable = 37,
    CrstIbcProfile = 38,
    CrstIJWFixupData = 39,
    CrstIJWHash = 40,
    CrstILStubGen = 41,
    CrstInlineTrackingMap = 42,
    CrstInstMethodHashTable = 43,
    CrstInterfaceDispatchGlobalLists = 44,
    CrstInterop = 45,
    CrstInteropData = 46,
    CrstIsJMCMethod = 47,
    CrstISymUnmanagedReader = 48,
    CrstJit = 49,
    CrstJitInlineTrackingMap = 50,
    CrstJitPatchpoint = 51,
    CrstJumpStubCache = 52,
    CrstLeafLock = 53,
    CrstListLock = 54,
    CrstLoaderAllocator = 55,
    CrstLoaderAllocatorReferences = 56,
    CrstLoaderHeap = 57,
    CrstManagedObjectWrapperMap = 58,
    CrstMethodDescBackpatchInfoTracker = 59,
    CrstMethodTableExposedObject = 60,
    CrstModule = 61,
    CrstModuleLookupTable = 62,
    CrstMulticoreJitHash = 63,
    CrstMulticoreJitManager = 64,
    CrstNativeImageEagerFixups = 65,
    CrstNativeImageLoad = 66,
    CrstNotifyGdb = 67,
    CrstPEImage = 68,
    CrstPendingTypeLoadEntry = 69,
    CrstPerfMap = 70,
    CrstPgoData = 71,
    CrstPinnedByrefValidation = 72,
    CrstPinnedHeapHandleTable = 73,
    CrstProfilerGCRefDataFreeList = 74,
    CrstProfilingAPIStatus = 75,
    CrstRCWCache = 76,
    CrstRCWCleanupList = 77,
    CrstReadyToRunEntryPointToMethodDescMap = 78,
    CrstReflection = 79,
    CrstReJITGlobalRequest = 80,
    CrstRetThunkCache = 81,
    CrstSigConvert = 82,
    CrstSingleUseLock = 83,
    CrstStressLog = 84,
    CrstStubCache = 85,
    CrstStubDispatchCache = 86,
    CrstSyncBlockCache = 87,
    CrstSyncHashLock = 88,
    CrstSystemDomain = 89,
    CrstSystemDomainDelayedUnloadList = 90,
    CrstThreadIdDispenser = 91,
    CrstThreadLocalStorageLock = 92,
    CrstThreadStore = 93,
    CrstTieredCompilation = 94,
    CrstTypeEquivalenceMap = 95,
    CrstTypeIDMap = 96,
    CrstUMEntryThunkCache = 97,
    CrstUMEntryThunkFreeListLock = 98,
    CrstUniqueStack = 99,
    CrstUnresolvedClassLock = 100,
    CrstUnwindInfoTableLock = 101,
    CrstVSDIndirectionCellLock = 102,
    CrstWrapperTemplate = 103,
    kNumberOfCrstTypes = 104
};

#endif // __CRST_TYPES_INCLUDED

// Define some debug data in one module only -- vm\crst.cpp.
#if defined(__IN_CRST_CPP) && defined(_DEBUG)

// An array mapping CrstType to level.
int g_rgCrstLevelMap[] =
{
    9,          // CrstAppDomainCache
    2,          // CrstAssemblyList
    13,         // CrstAssemblyLoader
    3,          // CrstAvailableClass
    4,          // CrstAvailableParamTypes
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
    0,          // CrstDynamicIL
    9,          // CrstDynamicMT
    0,          // CrstEtwTypeLogHash
    19,         // CrstEventPipe
    0,          // CrstEventStore
    0,          // CrstException
    0,          // CrstExecutableAllocatorLock
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
    0,          // CrstInterfaceDispatchGlobalLists
    21,         // CrstInterop
    9,          // CrstInteropData
    0,          // CrstIsJMCMethod
    6,          // CrstISymUnmanagedReader
    10,         // CrstJit
    11,         // CrstJitInlineTrackingMap
    3,          // CrstJitPatchpoint
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
    3,          // CrstModuleLookupTable
    0,          // CrstMulticoreJitHash
    14,         // CrstMulticoreJitManager
    7,          // CrstNativeImageEagerFixups
    0,          // CrstNativeImageLoad
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
    3,          // CrstSigConvert
    4,          // CrstSingleUseLock
    -1,         // CrstStressLog
    3,          // CrstStubCache
    0,          // CrstStubDispatchCache
    2,          // CrstSyncBlockCache
    0,          // CrstSyncHashLock
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
    "CrstAssemblyList",
    "CrstAssemblyLoader",
    "CrstAvailableClass",
    "CrstAvailableParamTypes",
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
    "CrstDynamicIL",
    "CrstDynamicMT",
    "CrstEtwTypeLogHash",
    "CrstEventPipe",
    "CrstEventStore",
    "CrstException",
    "CrstExecutableAllocatorLock",
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
    "CrstInterfaceDispatchGlobalLists",
    "CrstInterop",
    "CrstInteropData",
    "CrstIsJMCMethod",
    "CrstISymUnmanagedReader",
    "CrstJit",
    "CrstJitInlineTrackingMap",
    "CrstJitPatchpoint",
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
    "CrstModuleLookupTable",
    "CrstMulticoreJitHash",
    "CrstMulticoreJitManager",
    "CrstNativeImageEagerFixups",
    "CrstNativeImageLoad",
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
    "CrstSigConvert",
    "CrstSingleUseLock",
    "CrstStressLog",
    "CrstStubCache",
    "CrstStubDispatchCache",
    "CrstSyncBlockCache",
    "CrstSyncHashLock",
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
