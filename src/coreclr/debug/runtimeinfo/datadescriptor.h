// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// No include guards. This file is included multiple times.

// The format is:
// CDAC_BASELINE("string") baseline data contract that the runtime should follow. "empty" is reasonable
// CDAC_TYPES_BEGIN()
// ... <types> ...
// CDAC_TYPES_END()
// CDAC_GLOBALS_BEGIN()
// ... <globals> ...
// CDAC_GLOBALS_END()
//
// In <types> the format is:
// CDAC_TYPE_BEGIN(cdacTypeIdentifier) // defined a new data descriptor named cdacIdentifier
//
// CDAC_TYPE_SIZE(k) -or- CDAC_TYPE_INDETERMINATE(cdacTypeIdentifier) specifies that the type has
//   size k (bytes - usually sizeof(SomeNativeType)) or specify that the type's size is not provided
//   It is important that CDAC_TYPE_SIZE or CDAC_TYPE_INDETERMINATE immediately follows
//   CDAC_TYPE_BEGIN
//
// CDAC_TYPE_FIELD(cdacTypeIdentifier, cdacFieldTypeIdentifier, cdacFieldName, k) specifies the
//   field of "cdacTypeIdentifier" that has name cdacFieldName and has the type
//   "cdacFieldtypeIdentifier" located at offset k in the type layout.  k is usually
//   offsetof(SomeClass, m_FieldName) if the field is public
//
//     if the field is private, the convention is that SomeClass declares a friend struct
//     cdac_offsets<T> and provides a specialization of cdac_offsets<T> with a public constexpr
//     size_t member that holds the offset:
//
//     class MyClass {
//       private:
//         void* m_myField;
//         friend template<typename T> cdac_offsets<T>;
//     };
//     template<> struct cdac_offsets<MyClass> {
//       static constexpr size_t MyField = offsetof(MyClass, m_myField);
//     };
//
//     then the field layout can be specified as
//     CDAC_TYPE_FIELD(MyClassLayout, pointer, MyField, cdac_offsets<MyClass>::MyField)
//  There can be zero or more CDAC_TYPE_FIELD entries per type layout
//
// CDAC_TYPE_END(cdacTypeIdentifier)  specifies the end of the type layout for cdacTypeIdentifier
//
// In <globals> the format is:
//
// CDAC_GLOBAL(cdacGlobalName, cdacTypeIdentifier, value)
// or
// CDAC_GLOBAL_POINTER(cdacGlobalName, cdacTypeIdentifier, address)
//
// Zero or more globals can be defined
//
// if a global is given with CDAC_GLOBAL(), `value` should be a constexpr uint64_t (or convertible
// to uint64_t) for example, it can be a literal constant or a preprocessor definition
//
// if a global is a CDAC_GLOBAL_POINTER(), address should be a constexpr pointer or a constexpr
// uintptr_t
//
//
//
// This file is compiled using the target architecture.  Preprocessor defines for the target
// platform will be available.  It is ok to use `#ifdef`.

#ifndef CDAC_BASELINE
#define CDAC_BASELINE(identifier)
#endif
#ifndef CDAC_TYPES_BEGIN
#define CDAC_TYPES_BEGIN()
#endif
#ifndef CDAC_TYPE_BEGIN
#define CDAC_TYPE_BEGIN(tyname)
#endif
#ifndef CDAC_TYPE_SIZE
#define CDAC_TYPE_SIZE(k)
#endif
#ifndef CDAC_TYPE_INDETERMINATE
#define CDAC_TYPE_INDETERMINATE(tyname)
#endif
#ifndef CDAC_TYPE_FIELD
#define CDAC_TYPE_FIELD(tyname,fieldtyname,fieldname,off)
#endif
#ifndef CDAC_TYPE_END
#define CDAC_TYPE_END(tyname)
#endif
#ifndef CDAC_TYPES_END
#define CDAC_TYPES_END()
#endif
#ifndef CDAC_GLOBALS_BEGIN
#define CDAC_GLOBALS_BEGIN()
#endif
#ifndef CDAC_GLOBAL
#define CDAC_GLOBAL(globalname,tyname,val)
#endif
#ifndef CDAC_GLOBAL_POINTER
#define CDAC_GLOBAL_POINTER(globalname,addr)
#endif
#ifndef CDAC_GLOBALS_END
#define CDAC_GLOBALS_END()
#endif

CDAC_BASELINE("empty")
CDAC_TYPES_BEGIN()

CDAC_TYPE_BEGIN(Thread)
CDAC_TYPE_INDETERMINATE(Thread)
CDAC_TYPE_FIELD(Thread, /*uint32*/, Id, cdac_offsets<Thread>::Id)
CDAC_TYPE_FIELD(Thread, /*nuint*/, OSId, cdac_offsets<Thread>::OSId)
CDAC_TYPE_FIELD(Thread, /*uint32*/, State, cdac_offsets<Thread>::State)
CDAC_TYPE_FIELD(Thread, /*uint32*/, PreemptiveGCDisabled, cdac_offsets<Thread>::PreemptiveGCDisabled)
CDAC_TYPE_FIELD(Thread, /*pointer*/, RuntimeThreadLocals, cdac_offsets<Thread>::RuntimeThreadLocals)
CDAC_TYPE_FIELD(Thread, /*pointer*/, Frame, cdac_offsets<Thread>::Frame)
CDAC_TYPE_FIELD(Thread, /*pointer*/, ExceptionTracker, cdac_offsets<Thread>::ExceptionTracker)
CDAC_TYPE_FIELD(Thread, GCHandle, GCHandle, cdac_offsets<Thread>::ExposedObject)
CDAC_TYPE_FIELD(Thread, GCHandle, LastThrownObject, cdac_offsets<Thread>::LastThrownObject)
CDAC_TYPE_FIELD(Thread, pointer, LinkNext, cdac_offsets<Thread>::Link)
#ifndef TARGET_UNIX
CDAC_TYPE_FIELD(Thread, /*pointer*/, TEB, cdac_offsets<Thread>::TEB)
#endif
CDAC_TYPE_END(Thread)

CDAC_TYPE_BEGIN(ThreadStore)
CDAC_TYPE_INDETERMINATE(ThreadStore)
CDAC_TYPE_FIELD(ThreadStore, /*SLink*/, FirstThreadLink, cdac_offsets<ThreadStore>::FirstThreadLink)
CDAC_TYPE_FIELD(ThreadStore, /*int32*/, ThreadCount, cdac_offsets<ThreadStore>::ThreadCount)
CDAC_TYPE_FIELD(ThreadStore, /*int32*/, UnstartedCount, cdac_offsets<ThreadStore>::UnstartedCount)
CDAC_TYPE_FIELD(ThreadStore, /*int32*/, BackgroundCount, cdac_offsets<ThreadStore>::BackgroundCount)
CDAC_TYPE_FIELD(ThreadStore, /*int32*/, PendingCount, cdac_offsets<ThreadStore>::PendingCount)
CDAC_TYPE_FIELD(ThreadStore, /*int32*/, DeadCount, cdac_offsets<ThreadStore>::DeadCount)
CDAC_TYPE_END(ThreadStore)

CDAC_TYPE_BEGIN(RuntimeThreadLocals)
CDAC_TYPE_INDETERMINATE(RuntimeThreadLocals)
CDAC_TYPE_FIELD(RuntimeThreadLocals, AllocContext, AllocContext, offsetof(RuntimeThreadLocals, alloc_context))
CDAC_TYPE_END(RuntimeThreadLocals)

CDAC_TYPE_BEGIN(GCAllocContext)
CDAC_TYPE_INDETERMINATE(GCAllocContext)
CDAC_TYPE_FIELD(GCAllocContext, /*pointer*/, Pointer, offsetof(gc_alloc_context, alloc_ptr))
CDAC_TYPE_FIELD(GCAllocContext, /*pointer*/, Limit, offsetof(gc_alloc_context, alloc_limit))
CDAC_TYPE_END(GCAllocContext)

CDAC_TYPE_BEGIN(ExceptionInfo)
CDAC_TYPE_INDETERMINATE(ExceptionInfo)
#if FEATURE_EH_FUNCLETS
CDAC_TYPE_FIELD(ExceptionInfo, /*pointer*/, ThrownObject, offsetof(ExceptionTrackerBase, m_hThrowable))
CDAC_TYPE_FIELD(PreviousNestedInfo, /*pointer*/, PreviousNestedInfo, offsetof(ExceptionTrackerBase, m_pPrevNestedInfo))
#else
CDAC_TYPE_FIELD(ExceptionInfo, /*pointer*/, ThrownObject, offsetof(ExInfo, m_hThrowable))
CDAC_TYPE_FIELD(PreviousNestedInfo, /*pointer*/, PreviousNestedInfo, offsetof(ExInfo, m_pPrevNestedInfo))
#endif
CDAC_TYPE_END(ExceptionInfo)

CDAC_TYPE_BEGIN(GCHandle)
CDAC_TYPE_SIZE(sizeof(OBJECTHANDLE))
CDAC_TYPE_END(GCHandle)

#ifdef STRESS_LOG
CDAC_TYPE_BEGIN(StressLog)
CDAC_TYPE_SIZE(sizeof(StressLog))
CDAC_TYPE_FIELD(StressLog, /* uint32 */, LoggedFacilities, cdac_offsets<StressLog>::facilitiesToLog)
CDAC_TYPE_FIELD(StressLog, /* uint32 */, Level, cdac_offsets<StressLog>::levelToLog)
CDAC_TYPE_FIELD(StressLog, /* uint32 */, MaxSizePerThread, cdac_offsets<StressLog>::MaxSizePerThread)
CDAC_TYPE_FIELD(StressLog, /* uint32 */, MaxSizeTotal, cdac_offsets<StressLog>::MaxSizeTotal)
CDAC_TYPE_FIELD(StressLog, /* uint32 */, TotalChunks, cdac_offsets<StressLog>::totalChunk)
CDAC_TYPE_FIELD(StressLog, /* pointer */, Logs, cdac_offsets<StressLog>::logs)
CDAC_TYPE_FIELD(StressLog, /* uint64 */, TickFrequency, cdac_offsets<StressLog>::tickFrequency)
CDAC_TYPE_FIELD(StressLog, /* uint64 */, StartTimestamp, cdac_offsets<StressLog>::startTimeStamp)
CDAC_TYPE_FIELD(StressLog, /* nuint */, ModuleOffset, cdac_offsets<StressLog>::moduleOffset)
CDAC_TYPE_END(StressLog)

CDAC_TYPE_BEGIN(StressLogModuleDesc)
CDAC_TYPE_SIZE(cdac_offsets<StressLog>::ModuleDesc::type_size)
CDAC_TYPE_FIELD(StressLogModuleDesc, pointer, BaseAddress, cdac_offsets<StressLog>::ModuleDesc::baseAddress)
CDAC_TYPE_FIELD(StressLogModuleDesc, nuint, Size, cdac_offsets<StressLog>::ModuleDesc::size)
CDAC_TYPE_END(StressLogModuleDesc)

CDAC_TYPE_BEGIN(ThreadStressLog)
CDAC_TYPE_INDETERMINATE(ThreadStressLog)
CDAC_TYPE_FIELD(ThreadStressLog, /* pointer */, Next, cdac_offsets<ThreadStressLog>::next)
CDAC_TYPE_FIELD(ThreadStressLog, uint64, ThreadId, cdac_offsets<ThreadStressLog>::threadId)
CDAC_TYPE_FIELD(ThreadStressLog, uint8, WriteHasWrapped, cdac_offsets<ThreadStressLog>::writeHasWrapped)
CDAC_TYPE_FIELD(ThreadStressLog, pointer, CurrentPtr, cdac_offsets<ThreadStressLog>::curPtr)
CDAC_TYPE_FIELD(ThreadStressLog, /* pointer */, ChunkListHead, cdac_offsets<ThreadStressLog>::chunkListHead)
CDAC_TYPE_FIELD(ThreadStressLog, /* pointer */, ChunkListTail, cdac_offsets<ThreadStressLog>::chunkListTail)
CDAC_TYPE_FIELD(ThreadStressLog, /* pointer */, CurrentWriteChunk, cdac_offsets<ThreadStressLog>::curWriteChunk)
CDAC_TYPE_END(ThreadStressLog)

CDAC_TYPE_BEGIN(StressLogChunk)
CDAC_TYPE_SIZE(sizeof(StressLogChunk))
CDAC_TYPE_FIELD(StressLogChunk, /* pointer */, Prev, offsetof(StressLogChunk, prev))
CDAC_TYPE_FIELD(StressLogChunk, /* pointer */, Next, offsetof(StressLogChunk, next))
CDAC_TYPE_FIELD(StressLogChunk, /* uint8[STRESSLOG_CHUNK_SIZE] */, Buf, offsetof(StressLogChunk, buf))
CDAC_TYPE_FIELD(StressLogChunk, /* uint32 */, Sig1, offsetof(StressLogChunk, dwSig1))
CDAC_TYPE_FIELD(StressLogChunk, /* uint32 */, Sig2, offsetof(StressLogChunk, dwSig2))
CDAC_TYPE_END(StressLogChunk)

// The StressMsg Header is the fixed size portion of the StressMsg
CDAC_TYPE_BEGIN(StressMsgHeader)
CDAC_TYPE_SIZE(sizeof(StressMsg))
CDAC_TYPE_END(StressMsgHeader)

CDAC_TYPE_BEGIN(StressMsg)
CDAC_TYPE_INDETERMINATE(StressMsg)
CDAC_TYPE_FIELD(StressMsg, StressMsgHeader, Header, 0)
CDAC_TYPE_FIELD(StressMsg, /* pointer */, Args, offsetof(StressMsg, args))
CDAC_TYPE_END(StressMsg)
#endif

CDAC_TYPES_END()

CDAC_GLOBALS_BEGIN()
CDAC_GLOBAL_POINTER(AppDomain, &AppDomain::m_pTheAppDomain)
CDAC_GLOBAL_POINTER(ThreadStore, &ThreadStore::s_pThreadStore)
CDAC_GLOBAL_POINTER(FinalizerThread, &::g_pFinalizerThread)
CDAC_GLOBAL_POINTER(GCThread, &::g_pSuspensionThread)
#if FEATURE_EH_FUNCLETS
CDAC_GLOBAL(FeatureEHFunclets, uint8, 1)
#else
CDAC_GLOBAL(FeatureEHFunclets, uint8, 0)
#endif
CDAC_GLOBAL(SOSBreakingChangeVersion, uint8, SOS_BREAKING_CHANGE_VERSION)
#ifdef STRESS_LOG
CDAC_GLOBAL(StressLogEnabled, uint8, 1)
CDAC_GLOBAL_POINTER(StressLog, &g_pStressLog)
CDAC_GLOBAL_POINTER(StressLogModuleTable, &g_pStressLog->modules)
CDAC_GLOBAL(StressLogMaxModules, uint64, cdac_offsets<StressLog>::MAX_MODULES)
CDAC_GLOBAL(StressLogChunkSize, uint32, STRESSLOG_CHUNK_SIZE)
CDAC_GLOBAL(StressLogMaxMessageSize, uint64, (uint64_t)StressMsg::maxMsgSize())
CDAC_GLOBAL(StressMsgHeaderSize, uint32, (uint32_t)sizeof(StressMsg))
#else
CDAC_GLOBAL(StressLogEnabled, uint8, 0)
#endif
CDAC_GLOBALS_END()

#undef CDAC_BASELINE
#undef CDAC_TYPES_BEGIN
#undef CDAC_TYPE_BEGIN
#undef CDAC_TYPE_INDETERMINATE
#undef CDAC_TYPE_SIZE
#undef CDAC_TYPE_FIELD
#undef CDAC_TYPE_END
#undef CDAC_TYPES_END
#undef CDAC_GLOBALS_BEGIN
#undef CDAC_GLOBAL
#undef CDAC_GLOBAL_POINTER
#undef CDAC_GLOBALS_END
