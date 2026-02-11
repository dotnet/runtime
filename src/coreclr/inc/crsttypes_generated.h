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
    CrstCallStubCache = 5,
    CrstCCompRC = 6,
    CrstClassFactInfoHash = 7,
    CrstClassInit = 8,
    CrstClrNotification = 9,
    CrstCodeFragmentHeap = 10,
    CrstCodeVersioning = 11,
    CrstCOMCallWrapper = 12,
    CrstCOMWrapperCache = 13,
    CrstDataTest1 = 14,
    CrstDataTest2 = 15,
    CrstDbgTransport = 16,
    CrstDeadlockDetection = 17,
    CrstDebuggerController = 18,
    CrstDebuggerFavorLock = 19,
    CrstDebuggerHeapExecMemLock = 20,
    CrstDebuggerHeapLock = 21,
    CrstDebuggerJitInfo = 22,
    CrstDebuggerMutex = 23,
    CrstDynamicIL = 24,
    CrstDynamicMT = 25,
    CrstEbrPending = 26,
    CrstEbrThreadList = 27,
    CrstEtwTypeLogHash = 28,
    CrstEventPipe = 29,
    CrstEventStore = 30,
    CrstException = 31,
    CrstExecutableAllocatorLock = 32,
    CrstFCall = 33,
    CrstFrozenObjectHeap = 34,
    CrstFuncPtrStubs = 35,
    CrstFusionAppCtx = 36,
    CrstGCCover = 37,
    CrstGenericDictionaryExpansion = 38,
    CrstGlobalStrLiteralMap = 39,
    CrstHandleTable = 40,
    CrstIJWFixupData = 41,
    CrstIJWHash = 42,
    CrstILStubGen = 43,
    CrstInlineTrackingMap = 44,
    CrstInstMethodHashTable = 45,
    CrstInterfaceDispatchGlobalLists = 46,
    CrstInterop = 47,
    CrstInteropData = 48,
    CrstIsJMCMethod = 49,
    CrstISymUnmanagedReader = 50,
    CrstJit = 51,
    CrstJitInlineTrackingMap = 52,
    CrstJitPatchpoint = 53,
    CrstJumpStubCache = 54,
    CrstLeafLock = 55,
    CrstListLock = 56,
    CrstLoaderAllocator = 57,
    CrstLoaderAllocatorReferences = 58,
    CrstLoaderHeap = 59,
    CrstManagedObjectWrapperMap = 60,
    CrstMethodDescBackpatchInfoTracker = 61,
    CrstMethodTableExposedObject = 62,
    CrstModule = 63,
    CrstModuleLookupTable = 64,
    CrstMulticoreJitHash = 65,
    CrstMulticoreJitManager = 66,
    CrstNativeImageEagerFixups = 67,
    CrstNativeImageLoad = 68,
    CrstNotifyGdb = 69,
    CrstPEImage = 70,
    CrstPendingTypeLoadEntry = 71,
    CrstPerfMap = 72,
    CrstPgoData = 73,
    CrstPinnedByrefValidation = 74,
    CrstPinnedHeapHandleTable = 75,
    CrstProfilerGCRefDataFreeList = 76,
    CrstProfilingAPIStatus = 77,
    CrstRCWCache = 78,
    CrstRCWCleanupList = 79,
    CrstReadyToRunEntryPointToMethodDescMap = 80,
    CrstReflection = 81,
    CrstReJITGlobalRequest = 82,
    CrstSigConvert = 83,
    CrstSingleUseLock = 84,
    CrstStressLog = 85,
    CrstStubCache = 86,
    CrstStubDispatchCache = 87,
    CrstSyncBlockCache = 88,
    CrstSyncHashLock = 89,
    CrstSystemDomain = 90,
    CrstSystemDomainDelayedUnloadList = 91,
    CrstThreadIdDispenser = 92,
    CrstThreadLocalStorageLock = 93,
    CrstThreadStore = 94,
    CrstTieredCompilation = 95,
    CrstTypeEquivalenceMap = 96,
    CrstTypeIDMap = 97,
    CrstUMEntryThunkCache = 98,
    CrstUMEntryThunkFreeListLock = 99,
    CrstUniqueStack = 100,
    CrstUnresolvedClassLock = 101,
    CrstUnwindInfoTableLock = 102,
    CrstVSDIndirectionCellLock = 103,
    CrstWrapperTemplate = 104,
    kNumberOfCrstTypes = 105
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
    3,          // CrstCallStubCache
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
    0,          // CrstEbrPending
    0,          // CrstEbrThreadList
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
    7,          // CrstIJWFixupData
    0,          // CrstIJWHash
    6,          // CrstILStubGen
    0,          // CrstInlineTrackingMap
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
    "CrstCallStubCache",
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
    "CrstEbrPending",
    "CrstEbrThreadList",
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
