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
    CrstDomainLocalBlock = 26,
    CrstDynamicIL = 27,
    CrstDynamicMT = 28,
    CrstEtwTypeLogHash = 29,
    CrstEventPipe = 30,
    CrstEventStore = 31,
    CrstException = 32,
    CrstExecutableAllocatorLock = 33,
    CrstExecuteManRangeLock = 34,
    CrstExternalObjectContextCache = 35,
    CrstFCall = 36,
    CrstFrozenObjectHeap = 37,
    CrstFuncPtrStubs = 38,
    CrstFusionAppCtx = 39,
    CrstGCCover = 40,
    CrstGlobalStrLiteralMap = 41,
    CrstHandleTable = 42,
    CrstIbcProfile = 43,
    CrstIJWFixupData = 44,
    CrstIJWHash = 45,
    CrstILStubGen = 46,
    CrstInlineTrackingMap = 47,
    CrstInstMethodHashTable = 48,
    CrstInterop = 49,
    CrstInteropData = 50,
    CrstIsJMCMethod = 51,
    CrstISymUnmanagedReader = 52,
    CrstJit = 53,
    CrstJitGenericHandleCache = 54,
    CrstJitInlineTrackingMap = 55,
    CrstJitPatchpoint = 56,
    CrstJitPerf = 57,
    CrstJumpStubCache = 58,
    CrstLeafLock = 59,
    CrstListLock = 60,
    CrstLoaderAllocator = 61,
    CrstLoaderAllocatorReferences = 62,
    CrstLoaderHeap = 63,
    CrstManagedObjectWrapperMap = 64,
    CrstMethodDescBackpatchInfoTracker = 65,
    CrstMethodTableExposedObject = 66,
    CrstModule = 67,
    CrstModuleFixup = 68,
    CrstModuleLookupTable = 69,
    CrstMulticoreJitHash = 70,
    CrstMulticoreJitManager = 71,
    CrstNativeImageEagerFixups = 72,
    CrstNativeImageLoad = 73,
    CrstNls = 74,
    CrstNotifyGdb = 75,
    CrstObjectList = 76,
    CrstPEImage = 77,
    CrstPendingTypeLoadEntry = 78,
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
    -1,         // CrstFrozenObjectHeap
    7,          // CrstFuncPtrStubs
    10,         // CrstFusionAppCtx
    10,         // CrstGCCover
    15,         // CrstGlobalStrLiteralMap
    1,          // CrstHandleTable
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
    13,         // CrstJitInlineTrackingMap
    4,          // CrstJitPatchpoint
    -1,         // CrstJitPerf
    6,          // CrstJumpStubCache
    0,          // CrstLeafLock
    -1,         // CrstListLock
    15,         // CrstLoaderAllocator
    16,         // CrstLoaderAllocatorReferences
    3,          // CrstLoaderHeap
    3,          // CrstManagedObjectWrapperMap
    10,         // CrstMethodDescBackpatchInfoTracker
    -1,         // CrstMethodTableExposedObject
    5,          // CrstModule
    16,         // CrstModuleFixup
    4,          // CrstModuleLookupTable
    0,          // CrstMulticoreJitHash
    13,         // CrstMulticoreJitManager
    3,          // CrstNativeImageEagerFixups
    0,          // CrstNativeImageLoad
    0,          // CrstNls
    0,          // CrstNotifyGdb
    2,          // CrstObjectList
    5,          // CrstPEImage
    19,         // CrstPendingTypeLoadEntry
    4,          // CrstPgoData
    0,          // CrstPinnedByrefValidation
    14,         // CrstPinnedHeapHandleTable
    0,          // CrstProfilerGCRefDataFreeList
    13,         // CrstProfilingAPIStatus
    4,          // CrstRCWCache
    0,          // CrstRCWCleanupList
    10,         // CrstReadyToRunEntryPointToMethodDescMap
    8,          // CrstReflection
    14,         // CrstReJITGlobalRequest
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
    "CrstFrozenObjectHeap",
    "CrstFuncPtrStubs",
    "CrstFusionAppCtx",
    "CrstGCCover",
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
