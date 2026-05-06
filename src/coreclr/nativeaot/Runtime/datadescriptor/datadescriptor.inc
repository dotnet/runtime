// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// No include guards. This file is included multiple times.
//
// NativeAOT data descriptor declarations for the cDAC contract system.
// This file defines the types, fields, and globals that diagnostic tools
// need to inspect NativeAOT runtime state.
//
// When modifying this file (adding/removing types, fields, or globals), you must also:
//   1. Update the corresponding contract doc in docs/design/datacontracts/<contract>.md
//   2. Update the managed data class in src/native/managed/cdac/.../Data/<Type>.cs
//   3. Update the contract implementation in src/native/managed/cdac/.../Contracts/<Contract>.cs
//   4. Update the mock descriptors and tests in src/native/managed/cdac/tests/.

CDAC_BASELINE("empty")
CDAC_TYPES_BEGIN()

// ========================
// Thread and ThreadStore
// ========================

CDAC_TYPE_BEGIN(Thread)
CDAC_TYPE_INDETERMINATE(Thread)
CDAC_TYPE_FIELD(Thread, T_UINT64, OSId, offsetof(RuntimeThreadLocals, m_threadId))
CDAC_TYPE_FIELD(Thread, T_UINT32, State, offsetof(RuntimeThreadLocals, m_ThreadStateFlags))
CDAC_TYPE_FIELD(Thread, T_POINTER, LinkNext, offsetof(RuntimeThreadLocals, m_pNext))
CDAC_TYPE_FIELD(Thread, T_POINTER, ExceptionTracker, offsetof(RuntimeThreadLocals, m_pExInfoStackHead))
CDAC_TYPE_FIELD(Thread, T_POINTER, CachedStackBase, offsetof(RuntimeThreadLocals, m_pStackHigh))
CDAC_TYPE_FIELD(Thread, T_POINTER, CachedStackLimit, offsetof(RuntimeThreadLocals, m_pStackLow))
// Thread inherits from RuntimeThreadLocals at offset 0, so EEAllocContext is directly on Thread
CDAC_TYPE_FIELD(Thread, TYPE(EEAllocContext), AllocContext, offsetof(RuntimeThreadLocals, m_eeAllocContext))
CDAC_TYPE_FIELD(Thread, T_POINTER, TransitionFrame, offsetof(RuntimeThreadLocals, m_pTransitionFrame))
CDAC_TYPE_END(Thread)

CDAC_TYPE_BEGIN(ThreadStore)
CDAC_TYPE_INDETERMINATE(ThreadStore)
CDAC_TYPE_FIELD(ThreadStore, T_POINTER, FirstThreadLink, cdac_data<ThreadStore>::FirstThreadLink)
CDAC_TYPE_END(ThreadStore)

// ========================
// Allocation Context
// ========================

CDAC_TYPE_BEGIN(EEAllocContext)
CDAC_TYPE_INDETERMINATE(EEAllocContext)
CDAC_TYPE_FIELD(EEAllocContext, TYPE(GCAllocContext), GCAllocationContext, offsetof(ee_alloc_context, m_rgbAllocContextBuffer))
CDAC_TYPE_END(EEAllocContext)

CDAC_TYPE_BEGIN(GCAllocContext)
CDAC_TYPE_INDETERMINATE(GCAllocContext)
CDAC_TYPE_FIELD(GCAllocContext, T_POINTER, Pointer, offsetof(gc_alloc_context, alloc_ptr))
CDAC_TYPE_FIELD(GCAllocContext, T_POINTER, Limit, offsetof(gc_alloc_context, alloc_limit))
CDAC_TYPE_FIELD(GCAllocContext, T_INT64, AllocBytes, offsetof(gc_alloc_context, alloc_bytes))
CDAC_TYPE_FIELD(GCAllocContext, T_INT64, AllocBytesLoh, offsetof(gc_alloc_context, alloc_bytes_uoh))
CDAC_TYPE_END(GCAllocContext)

// ========================
// MethodTable (EEType)
// ========================

CDAC_TYPE_BEGIN(MethodTable)
CDAC_TYPE_INDETERMINATE(MethodTable)
CDAC_TYPE_FIELD(MethodTable, T_UINT32, Flags, cdac_data<MethodTable>::Flags)
CDAC_TYPE_FIELD(MethodTable, T_UINT32, BaseSize, cdac_data<MethodTable>::BaseSize)
CDAC_TYPE_FIELD(MethodTable, T_POINTER, RelatedType, cdac_data<MethodTable>::RelatedType)
CDAC_TYPE_FIELD(MethodTable, T_UINT16, NumVtableSlots, cdac_data<MethodTable>::NumVtableSlots)
CDAC_TYPE_FIELD(MethodTable, T_UINT16, NumInterfaces, cdac_data<MethodTable>::NumInterfaces)
CDAC_TYPE_FIELD(MethodTable, T_UINT32, HashCode, cdac_data<MethodTable>::HashCode)
CDAC_TYPE_FIELD(MethodTable, T_POINTER, VTable, cdac_data<MethodTable>::VTable)
CDAC_TYPE_END(MethodTable)

// ========================
// Exception Info
// ========================

CDAC_TYPE_BEGIN(ExInfo)
CDAC_TYPE_INDETERMINATE(ExInfo)
CDAC_TYPE_FIELD(ExInfo, T_POINTER, PreviousNestedInfo, offsetof(ExInfo, m_pPrevExInfo))
CDAC_TYPE_FIELD(ExInfo, T_POINTER, ThrownObject, offsetof(ExInfo, m_exception))
CDAC_TYPE_END(ExInfo)

// ========================
// StressLog
// ========================

CDAC_TYPE_BEGIN(StressLog)
CDAC_TYPE_SIZE(sizeof(StressLog))
CDAC_TYPE_FIELD(StressLog, T_UINT32, LoggedFacilities, offsetof(StressLog, facilitiesToLog))
CDAC_TYPE_FIELD(StressLog, T_UINT32, Level, offsetof(StressLog, levelToLog))
CDAC_TYPE_FIELD(StressLog, T_UINT32, MaxSizePerThread, offsetof(StressLog, MaxSizePerThread))
CDAC_TYPE_FIELD(StressLog, T_UINT32, MaxSizeTotal, offsetof(StressLog, MaxSizeTotal))
CDAC_TYPE_FIELD(StressLog, T_INT32, TotalChunks, offsetof(StressLog, totalChunk))
CDAC_TYPE_FIELD(StressLog, T_POINTER, Logs, offsetof(StressLog, logs))
CDAC_TYPE_FIELD(StressLog, T_UINT64, TickFrequency, offsetof(StressLog, tickFrequency))
CDAC_TYPE_FIELD(StressLog, T_UINT64, StartTimestamp, offsetof(StressLog, startTimeStamp))
CDAC_TYPE_END(StressLog)

CDAC_TYPE_BEGIN(ThreadStressLog)
CDAC_TYPE_INDETERMINATE(ThreadStressLog)
CDAC_TYPE_FIELD(ThreadStressLog, T_POINTER, Next, cdac_data<ThreadStressLog>::Next)
CDAC_TYPE_FIELD(ThreadStressLog, T_UINT64, ThreadId, cdac_data<ThreadStressLog>::ThreadId)
CDAC_TYPE_FIELD(ThreadStressLog, T_UINT8, WriteHasWrapped, cdac_data<ThreadStressLog>::WriteHasWrapped)
CDAC_TYPE_FIELD(ThreadStressLog, T_POINTER, CurrentPtr, cdac_data<ThreadStressLog>::CurrentPtr)
CDAC_TYPE_FIELD(ThreadStressLog, T_POINTER, ChunkListHead, cdac_data<ThreadStressLog>::ChunkListHead)
CDAC_TYPE_FIELD(ThreadStressLog, T_POINTER, ChunkListTail, cdac_data<ThreadStressLog>::ChunkListTail)
CDAC_TYPE_FIELD(ThreadStressLog, T_POINTER, CurrentWriteChunk, cdac_data<ThreadStressLog>::CurrentWriteChunk)
CDAC_TYPE_END(ThreadStressLog)

CDAC_TYPE_BEGIN(StressLogChunk)
CDAC_TYPE_SIZE(sizeof(StressLogChunk))
CDAC_TYPE_FIELD(StressLogChunk, T_POINTER, Prev, offsetof(StressLogChunk, prev))
CDAC_TYPE_FIELD(StressLogChunk, T_POINTER, Next, offsetof(StressLogChunk, next))
CDAC_TYPE_FIELD(StressLogChunk, T_ARRAY(T_UINT8), Buf, offsetof(StressLogChunk, buf))
CDAC_TYPE_FIELD(StressLogChunk, T_UINT32, Sig1, offsetof(StressLogChunk, dwSig1))
CDAC_TYPE_FIELD(StressLogChunk, T_UINT32, Sig2, offsetof(StressLogChunk, dwSig2))
CDAC_TYPE_END(StressLogChunk)

CDAC_TYPE_BEGIN(StressMsgHeader)
CDAC_TYPE_SIZE(sizeof(StressMsg))
CDAC_TYPE_END(StressMsgHeader)

CDAC_TYPE_BEGIN(StressMsg)
CDAC_TYPE_INDETERMINATE(StressMsg)
CDAC_TYPE_FIELD(StressMsg, TYPE(StressMsgHeader), Header, 0)
CDAC_TYPE_FIELD(StressMsg, T_POINTER, Args, offsetof(StressMsg, args))
CDAC_TYPE_END(StressMsg)

CDAC_TYPES_END()

// ========================
// Globals
// ========================

CDAC_GLOBALS_BEGIN()

CDAC_GLOBAL_POINTER(ThreadStore, &ThreadStore::s_pThreadStore)

CDAC_GLOBAL_POINTER(FreeObjectMethodTable, &g_pFreeObjectEEType)

CDAC_GLOBAL_POINTER(GCLowestAddress, &g_lowest_address)
CDAC_GLOBAL_POINTER(GCHighestAddress, &g_highest_address)

// Thread state flag constants
CDAC_GLOBAL(ThreadStateFlagAttached, T_UINT32, 0x00000001)
CDAC_GLOBAL(ThreadStateFlagDetached, T_UINT32, 0x00000002)

// Object contract globals
#ifdef TARGET_64BIT
CDAC_GLOBAL(ObjectToMethodTableUnmask, T_UINT8, 7)
#else
CDAC_GLOBAL(ObjectToMethodTableUnmask, T_UINT8, 3)
#endif

// StressLog globals (no module table in NativeAOT)
CDAC_GLOBAL(StressLogEnabled, T_UINT8, 1)
CDAC_GLOBAL_POINTER(StressLog, &g_pStressLog)
CDAC_GLOBAL(StressLogHasModuleTable, T_UINT8, 0)
CDAC_GLOBAL(StressLogChunkSize, T_UINT32, STRESSLOG_CHUNK_SIZE)
CDAC_GLOBAL(StressLogValidChunkSig, T_UINT32, 0xCFCFCFCF)
CDAC_GLOBAL(StressLogMaxMessageSize, T_UINT64, (uint64_t)StressMsg::maxMsgSize)

// Contracts: declare which contracts this runtime supports
CDAC_GLOBAL_CONTRACT(Thread, n1)
CDAC_GLOBAL_CONTRACT(Exception, c1)
CDAC_GLOBAL_CONTRACT(RuntimeTypeSystem, n1)
CDAC_GLOBAL_CONTRACT(StressLog, c2)

// Managed type sub-descriptor: ILC emits a ContractDescriptor with managed type layouts
// that the cDAC reader merges as a sub-descriptor. This provides field offsets for managed
// types (e.g., ConditionalWeakTable internals, IdDispenser) that are not exposed through
// native C++ data descriptors.
CDAC_GLOBAL_SUB_DESCRIPTOR(ManagedTypes, &g_pManagedContractDescriptor)

// GC sub-descriptor: the GC populates gc_descriptor during GC_Initialize.
// It is important for subdescriptor pointers to be the last pointers.
CDAC_GLOBAL_SUB_DESCRIPTOR(GC, &(g_gc_dac_vars.gc_descriptor))

CDAC_GLOBALS_END()
