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
//     cdac_data<T> and provides a specialization of cdac_data<T> with a public constexpr
//     size_t member that holds the offset:
//
//     class MyClass {
//       private:
//         void* m_myField;
//         friend template<typename T> cdac_data<T>;
//     };
//     template<> struct cdac_data<MyClass> {
//       static constexpr size_t MyField = offsetof(MyClass, m_myField);
//     };
//
//     then the field layout can be specified as
//     CDAC_TYPE_FIELD(MyClassLayout, pointer, MyField, cdac_data<MyClass>::MyField)
//  There can be zero or more CDAC_TYPE_FIELD entries per type layout
//  For types mapping to managed objects, use exact managed type field names in the descriptor, as
//  field names often can't change due to binary serialization or implicit diagnostic contracts
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
CDAC_TYPE_FIELD(Thread, /*uint32*/, Id, cdac_data<Thread>::Id)
CDAC_TYPE_FIELD(Thread, /*nuint*/, OSId, cdac_data<Thread>::OSId)
CDAC_TYPE_FIELD(Thread, /*uint32*/, State, cdac_data<Thread>::State)
CDAC_TYPE_FIELD(Thread, /*uint32*/, PreemptiveGCDisabled, cdac_data<Thread>::PreemptiveGCDisabled)
CDAC_TYPE_FIELD(Thread, /*pointer*/, RuntimeThreadLocals, cdac_data<Thread>::RuntimeThreadLocals)
CDAC_TYPE_FIELD(Thread, /*pointer*/, Frame, cdac_data<Thread>::Frame)
CDAC_TYPE_FIELD(Thread, /*pointer*/, ExceptionTracker, cdac_data<Thread>::ExceptionTracker)
CDAC_TYPE_FIELD(Thread, GCHandle, GCHandle, cdac_data<Thread>::ExposedObject)
CDAC_TYPE_FIELD(Thread, GCHandle, LastThrownObject, cdac_data<Thread>::LastThrownObject)
CDAC_TYPE_FIELD(Thread, pointer, LinkNext, cdac_data<Thread>::Link)
#ifndef TARGET_UNIX
CDAC_TYPE_FIELD(Thread, /*pointer*/, TEB, cdac_data<Thread>::TEB)
#endif
CDAC_TYPE_END(Thread)

CDAC_TYPE_BEGIN(ThreadStore)
CDAC_TYPE_INDETERMINATE(ThreadStore)
CDAC_TYPE_FIELD(ThreadStore, /*SLink*/, FirstThreadLink, cdac_data<ThreadStore>::FirstThreadLink)
CDAC_TYPE_FIELD(ThreadStore, /*int32*/, ThreadCount, cdac_data<ThreadStore>::ThreadCount)
CDAC_TYPE_FIELD(ThreadStore, /*int32*/, UnstartedCount, cdac_data<ThreadStore>::UnstartedCount)
CDAC_TYPE_FIELD(ThreadStore, /*int32*/, BackgroundCount, cdac_data<ThreadStore>::BackgroundCount)
CDAC_TYPE_FIELD(ThreadStore, /*int32*/, PendingCount, cdac_data<ThreadStore>::PendingCount)
CDAC_TYPE_FIELD(ThreadStore, /*int32*/, DeadCount, cdac_data<ThreadStore>::DeadCount)
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

// Exception

// Use exact managed type field names for the descriptor as field names often can't change due to binary serialization or implicit diagnostic contracts
CDAC_TYPE_BEGIN(Exception)
CDAC_TYPE_INDETERMINATE(Exception)
CDAC_TYPE_FIELD(Exception, /*pointer*/, _message, cdac_data<ExceptionObject>::_message)
CDAC_TYPE_FIELD(Exception, /*pointer*/, _innerException, cdac_data<ExceptionObject>::_innerException)
CDAC_TYPE_FIELD(Exception, /*pointer*/, _stackTrace, cdac_data<ExceptionObject>::_stackTrace)
CDAC_TYPE_FIELD(Exception, /*pointer*/, _watsonBuckets, cdac_data<ExceptionObject>::_watsonBuckets)
CDAC_TYPE_FIELD(Exception, /*pointer*/, _stackTraceString, cdac_data<ExceptionObject>::_stackTraceString)
CDAC_TYPE_FIELD(Exception, /*pointer*/, _remoteStackTraceString, cdac_data<ExceptionObject>::_remoteStackTraceString)
CDAC_TYPE_FIELD(Exception, /*int32*/, _HResult, cdac_data<ExceptionObject>::_HResult)
CDAC_TYPE_FIELD(Exception, /*int32*/, _xcode, cdac_data<ExceptionObject>::_xcode)
CDAC_TYPE_END(Exception)

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

// Object

CDAC_TYPE_BEGIN(Object)
CDAC_TYPE_INDETERMINATE(Object)
CDAC_TYPE_FIELD(Object, /*pointer*/, m_pMethTab, cdac_data<Object>::m_pMethTab)
CDAC_TYPE_END(Object)

CDAC_TYPE_BEGIN(String)
CDAC_TYPE_INDETERMINATE(String)
CDAC_TYPE_FIELD(String, /*pointer*/, m_FirstChar, cdac_data<StringObject>::m_FirstChar)
CDAC_TYPE_FIELD(String, /*uint32*/, m_StringLength, cdac_data<StringObject>::m_StringLength)
CDAC_TYPE_END(String)

CDAC_TYPE_BEGIN(Array)
CDAC_TYPE_SIZE(sizeof(ArrayBase))
CDAC_TYPE_FIELD(Array, /*pointer*/, m_NumComponents, cdac_data<ArrayBase>::m_NumComponents)
CDAC_TYPE_END(Array)

CDAC_TYPE_BEGIN(InteropSyncBlockInfo)
CDAC_TYPE_INDETERMINATE(InteropSyncBlockInfo)
#ifdef FEATURE_COMINTEROP
CDAC_TYPE_FIELD(InteropSyncBlockInfo, /*pointer*/, CCW, cdac_data<InteropSyncBlockInfo>::CCW)
CDAC_TYPE_FIELD(InteropSyncBlockInfo, /*pointer*/, RCW, cdac_data<InteropSyncBlockInfo>::RCW)
#endif // FEATURE_COMINTEROP
CDAC_TYPE_END(InteropSyncBlockInfo)

CDAC_TYPE_BEGIN(SyncBlock)
CDAC_TYPE_INDETERMINATE(SyncBlock)
CDAC_TYPE_FIELD(SyncBlock, /*pointer*/, InteropInfo, cdac_data<SyncBlock>::InteropInfo)
CDAC_TYPE_END(SyncBlock)

CDAC_TYPE_BEGIN(SyncTableEntry)
CDAC_TYPE_SIZE(sizeof(SyncTableEntry))
CDAC_TYPE_FIELD(SyncTableEntry, /*pointer*/, SyncBlock, offsetof(SyncTableEntry, m_SyncBlock))
CDAC_TYPE_END(SyncTableEntry)

// Loader

CDAC_TYPE_BEGIN(Module)
CDAC_TYPE_INDETERMINATE(Module)
CDAC_TYPE_FIELD(Module, /*pointer*/, Assembly, cdac_data<Module>::Assembly)
CDAC_TYPE_FIELD(Module, /*pointer*/, Base, cdac_data<Module>::Base)
CDAC_TYPE_FIELD(Module, /*pointer*/, Flags, cdac_data<Module>::Flags)
CDAC_TYPE_FIELD(Module, /*pointer*/, LoaderAllocator, cdac_data<Module>::LoaderAllocator)
CDAC_TYPE_FIELD(Module, /*pointer*/, ThunkHeap, cdac_data<Module>::ThunkHeap)
CDAC_TYPE_FIELD(Module, /*pointer*/, DynamicMetadata, cdac_data<Module>::DynamicMetadata)

CDAC_TYPE_FIELD(Module, /*pointer*/, FieldDefToDescMap, cdac_data<Module>::FieldDefToDescMap)
CDAC_TYPE_FIELD(Module, /*pointer*/, ManifestModuleReferencesMap, cdac_data<Module>::ManifestModuleReferencesMap)
CDAC_TYPE_FIELD(Module, /*pointer*/, MemberRefToDescMap, cdac_data<Module>::MemberRefToDescMap)
CDAC_TYPE_FIELD(Module, /*pointer*/, MethodDefToDescMap, cdac_data<Module>::MethodDefToDescMap)
CDAC_TYPE_FIELD(Module, /*pointer*/, TypeDefToMethodTableMap, cdac_data<Module>::TypeDefToMethodTableMap)
CDAC_TYPE_FIELD(Module, /*pointer*/, TypeRefToMethodTableMap, cdac_data<Module>::TypeRefToMethodTableMap)
CDAC_TYPE_END(Module)

// RuntimeTypeSystem

CDAC_TYPE_BEGIN(MethodTable)
CDAC_TYPE_INDETERMINATE(MethodTable)
CDAC_TYPE_FIELD(MethodTable, /*uint32*/, MTFlags, cdac_data<MethodTable>::MTFlags)
CDAC_TYPE_FIELD(MethodTable, /*uint32*/, BaseSize, cdac_data<MethodTable>::BaseSize)
CDAC_TYPE_FIELD(MethodTable, /*uint32*/, MTFlags2, cdac_data<MethodTable>::MTFlags2)
CDAC_TYPE_FIELD(MethodTable, /*nuint*/, EEClassOrCanonMT, cdac_data<MethodTable>::EEClassOrCanonMT)
CDAC_TYPE_FIELD(MethodTable, /*pointer*/, Module, cdac_data<MethodTable>::Module)
CDAC_TYPE_FIELD(MethodTable, /*pointer*/, ParentMethodTable, cdac_data<MethodTable>::ParentMethodTable)
CDAC_TYPE_FIELD(MethodTable, /*uint16*/, NumInterfaces, cdac_data<MethodTable>::NumInterfaces)
CDAC_TYPE_FIELD(MethodTable, /*uint16*/, NumVirtuals, cdac_data<MethodTable>::NumVirtuals)
CDAC_TYPE_FIELD(MethodTable, /*pointer*/, PerInstInfo, cdac_data<MethodTable>::PerInstInfo)
CDAC_TYPE_END(MethodTable)

CDAC_TYPE_BEGIN(EEClass)
CDAC_TYPE_INDETERMINATE(EEClass)
CDAC_TYPE_FIELD(EEClass, /*pointer*/, MethodTable, cdac_data<EEClass>::MethodTable)
CDAC_TYPE_FIELD(EEClass, /*uint16*/, NumMethods, cdac_data<EEClass>::NumMethods)
CDAC_TYPE_FIELD(EEClass, /*uint32*/, CorTypeAttr, cdac_data<EEClass>::CorTypeAttr)
CDAC_TYPE_FIELD(EEClass, /*uint8*/, InternalCorElementType, cdac_data<EEClass>::InternalCorElementType)
CDAC_TYPE_FIELD(EEClass, /*uint16*/, NumNonVirtualSlots, cdac_data<EEClass>::NumNonVirtualSlots)
CDAC_TYPE_END(EEClass)

CDAC_TYPE_BEGIN(ArrayClass)
CDAC_TYPE_INDETERMINATE(ArrayClass)
CDAC_TYPE_FIELD(ArrayClass, /*uint8*/, Rank, cdac_data<ArrayClass>::Rank)
CDAC_TYPE_END(ArrayClass)

CDAC_TYPE_BEGIN(GenericsDictInfo)
CDAC_TYPE_INDETERMINATE(GenericsDictInfo)
CDAC_TYPE_FIELD(GenericsDictInfo, /*uint16*/, NumTypeArgs, cdac_data<GenericsDictInfo>::NumTypeArgs)
CDAC_TYPE_END(GenericsDictInfo)

CDAC_TYPE_BEGIN(TypeDesc)
CDAC_TYPE_INDETERMINATE(TypeDesc)
CDAC_TYPE_FIELD(TypeDesc, /*uint32*/, TypeAndFlags, cdac_data<TypeDesc>::TypeAndFlags)
CDAC_TYPE_END(TypeDesc)

CDAC_TYPE_BEGIN(ParamTypeDesc)
CDAC_TYPE_INDETERMINATE(ParamTypeDesc)
CDAC_TYPE_FIELD(ParamTypeDesc, /*pointer*/, TypeArg, cdac_data<ParamTypeDesc>::TypeArg)
CDAC_TYPE_END(ParamTypeDesc)

CDAC_TYPE_BEGIN(TypeVarTypeDesc)
CDAC_TYPE_INDETERMINATE(TypeVarTypeDesc)
CDAC_TYPE_FIELD(TypeVarTypeDesc, /*pointer*/, Module, cdac_data<TypeVarTypeDesc>::Module)
CDAC_TYPE_FIELD(TypeVarTypeDesc, /*uint32*/, Token, cdac_data<TypeVarTypeDesc>::Token)
CDAC_TYPE_END(TypeVarTypeDesc)

CDAC_TYPE_BEGIN(FnPtrTypeDesc)
CDAC_TYPE_INDETERMINATE(FnPtrTypeDesc)
CDAC_TYPE_FIELD(FnPtrTypeDesc, /*uint32*/, NumArgs, cdac_data<FnPtrTypeDesc>::NumArgs)
CDAC_TYPE_FIELD(FnPtrTypeDesc, /*uint32*/, CallConv, cdac_data<FnPtrTypeDesc>::CallConv)
CDAC_TYPE_FIELD(FnPtrTypeDesc, /*uint32*/, RetAndArgTypes, cdac_data<FnPtrTypeDesc>::RetAndArgTypes)
CDAC_TYPE_END(FnPtrTypeDesc)

CDAC_TYPE_BEGIN(DynamicMetadata)
CDAC_TYPE_FIELD(DynamicMetadata, /*uint32*/, Size, cdac_data<DynamicMetadata>::Size)
CDAC_TYPE_FIELD(DynamicMetadata, /*inline byte array*/, Data, cdac_data<DynamicMetadata>::Data)
CDAC_TYPE_END(DynamicMetadata)

CDAC_TYPE_BEGIN(MethodDesc)
CDAC_TYPE_INDETERMINATE(MethodDesc)
CDAC_TYPE_FIELD(MethodDesc, /*uint8*/, ChunkIndex, cdac_data<MethodDesc>::ChunkIndex)
CDAC_TYPE_FIELD(MethodDesc, /*uint16*/, Slot, cdac_data<MethodDesc>::Slot)
CDAC_TYPE_FIELD(MethodDesc, /*uint16*/, Flags, cdac_data<MethodDesc>::Flags)
CDAC_TYPE_END(MethodDesc)

CDAC_TYPE_BEGIN(MethodDescChunk)
CDAC_TYPE_SIZE(sizeof(MethodDescChunk))
CDAC_TYPE_FIELD(MethodDescChunk, /*pointer*/, MethodTable, cdac_data<MethodDescChunk>::MethodTable)
CDAC_TYPE_FIELD(MethodDescChunk, /*pointer*/, Next, cdac_data<MethodDescChunk>::Next)
CDAC_TYPE_FIELD(MethodDescChunk, /*uint8*/, Size, cdac_data<MethodDescChunk>::Size)
CDAC_TYPE_FIELD(MethodDescChunk, /*uint8*/, Count, cdac_data<MethodDescChunk>::Count)
CDAC_TYPE_END(MethodDescChunk)

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
#if FEATURE_COMINTEROP
CDAC_GLOBAL(FeatureCOMInterop, uint8, 1)
#else
CDAC_GLOBAL(FeatureCOMInterop, uint8, 0)
#endif
// See Object::GetGCSafeMethodTable
#ifdef TARGET_64BIT
CDAC_GLOBAL(ObjectToMethodTableUnmask, uint8, 1 | 1 << 1 | 1 << 2)
#else
CDAC_GLOBAL(ObjectToMethodTableUnmask, uint8, 1 | 1 << 1)
#endif //TARGET_64BIT
CDAC_GLOBAL(SOSBreakingChangeVersion, uint8, SOS_BREAKING_CHANGE_VERSION)
CDAC_GLOBAL(MethodDescAlignment, uint64, MethodDesc::ALIGNMENT)
CDAC_GLOBAL(ObjectHeaderSize, uint64, OBJHEADER_SIZE)
CDAC_GLOBAL(SyncBlockValueToObjectOffset, uint16, OBJHEADER_SIZE - cdac_data<ObjHeader>::SyncBlockValue)
CDAC_GLOBAL_POINTER(ArrayBoundsZero, cdac_data<ArrayBase>::ArrayBoundsZero)
CDAC_GLOBAL_POINTER(ExceptionMethodTable, &::g_pExceptionClass)
CDAC_GLOBAL_POINTER(FreeObjectMethodTable, &::g_pFreeObjectMethodTable)
CDAC_GLOBAL_POINTER(ObjectMethodTable, &::g_pObjectClass)
CDAC_GLOBAL_POINTER(ObjectArrayMethodTable, &::g_pPredefinedArrayTypes[ELEMENT_TYPE_OBJECT])
CDAC_GLOBAL_POINTER(StringMethodTable, &::g_pStringClass)
CDAC_GLOBAL_POINTER(SyncTableEntries, &::g_pSyncTable)
CDAC_GLOBAL_POINTER(MiniMetaDataBuffAddress, &::g_MiniMetaDataBuffAddress)
CDAC_GLOBAL_POINTER(MiniMetaDataBuffMaxSize, &::g_MiniMetaDataBuffMaxSize)
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
