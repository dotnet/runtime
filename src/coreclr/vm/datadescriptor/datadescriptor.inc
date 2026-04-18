// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// No include guards. This file is included multiple times.
//
// This file is compiled using the target architecture.  Preprocessor defines for the target
// platform will be available.  It is ok to use `#ifdef`.
//
// When modifying this file (adding/removing types, fields, or globals), you must also:
//   1. Update the corresponding contract doc in docs/design/datacontracts/<contract>.md
//      to reflect the new data descriptors and any changes to the contract algorithm.
//   2. Update the managed data class in src/native/managed/cdac/.../Data/<Type>.cs
//      to read the new field(s).
//   3. Update the contract implementation in src/native/managed/cdac/.../Contracts/<Contract>.cs
//      to use the new data.
//   4. Update the mock descriptors and tests in src/native/managed/cdac/tests/.

// Increment this when making a change where we'd like to recommend users update their diagnostic tools.
// Tools can use this to display an advisory notice to users encouraging them to update. Changing
// this value on its own is not considered a breaking change. See the RuntimeInfo.md data contract for details.
// When incrementing this value, also implement the new functionality in the cDAC reader and update the
// GetCurrentReaderVersion() method on the IRuntimeInfo contract in
// src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/RuntimeInfo_1.cs
// to match.
#ifndef CDAC_RECOMMENDED_READER_VERSION
#define CDAC_RECOMMENDED_READER_VERSION 1
#endif

// When changes will break existing diagnostic tools (changes that are incompatible with the existing data contracts)
// you should also update the contract versions at the bottom of this file for any contract that is being broken.


CDAC_BASELINE("empty")
CDAC_TYPES_BEGIN()

CDAC_TYPE_BEGIN(Thread)
CDAC_TYPE_INDETERMINATE(Thread)
CDAC_TYPE_FIELD(Thread, T_UINT32, Id, cdac_data<Thread>::Id)
CDAC_TYPE_FIELD(Thread, T_NUINT, OSId, cdac_data<Thread>::OSId)
CDAC_TYPE_FIELD(Thread, T_UINT32, State, cdac_data<Thread>::State)
CDAC_TYPE_FIELD(Thread, T_UINT32, PreemptiveGCDisabled, cdac_data<Thread>::PreemptiveGCDisabled)
CDAC_TYPE_FIELD(Thread, T_POINTER, RuntimeThreadLocals, cdac_data<Thread>::RuntimeThreadLocals)
CDAC_TYPE_FIELD(Thread, T_POINTER, Frame, cdac_data<Thread>::Frame)
CDAC_TYPE_FIELD(Thread, T_POINTER, CachedStackBase, cdac_data<Thread>::CachedStackBase)
CDAC_TYPE_FIELD(Thread, T_POINTER, CachedStackLimit, cdac_data<Thread>::CachedStackLimit)
CDAC_TYPE_FIELD(Thread, T_POINTER, ExceptionTracker, cdac_data<Thread>::ExceptionTracker)
CDAC_TYPE_FIELD(Thread, T_POINTER, DebuggerFilterContext, cdac_data<Thread>::DebuggerFilterContext)
#ifdef PROFILING_SUPPORTED
CDAC_TYPE_FIELD(Thread, T_POINTER, ProfilerFilterContext, cdac_data<Thread>::ProfilerFilterContext)
#endif // PROFILING_SUPPORTED
CDAC_TYPE_FIELD(Thread, TYPE(GCHandle), GCHandle, cdac_data<Thread>::ExposedObject)
CDAC_TYPE_FIELD(Thread, T_POINTER, LastThrownObject, cdac_data<Thread>::LastThrownObject)
CDAC_TYPE_FIELD(Thread, T_POINTER, LinkNext, cdac_data<Thread>::Link)
CDAC_TYPE_FIELD(Thread, T_POINTER, ThreadLocalDataPtr, cdac_data<Thread>::ThreadLocalDataPtr)
#ifndef TARGET_UNIX
CDAC_TYPE_FIELD(Thread, T_POINTER, UEWatsonBucketTrackerBuckets, cdac_data<Thread>::UEWatsonBucketTrackerBuckets)
#endif
CDAC_TYPE_END(Thread)

CDAC_TYPE_BEGIN(ThreadStore)
CDAC_TYPE_INDETERMINATE(ThreadStore)
CDAC_TYPE_FIELD(ThreadStore, T_POINTER, FirstThreadLink, cdac_data<ThreadStore>::FirstThreadLink)
CDAC_TYPE_FIELD(ThreadStore, T_INT32, ThreadCount, cdac_data<ThreadStore>::ThreadCount)
CDAC_TYPE_FIELD(ThreadStore, T_INT32, UnstartedCount, cdac_data<ThreadStore>::UnstartedCount)
CDAC_TYPE_FIELD(ThreadStore, T_INT32, BackgroundCount, cdac_data<ThreadStore>::BackgroundCount)
CDAC_TYPE_FIELD(ThreadStore, T_INT32, PendingCount, cdac_data<ThreadStore>::PendingCount)
CDAC_TYPE_FIELD(ThreadStore, T_INT32, DeadCount, cdac_data<ThreadStore>::DeadCount)
CDAC_TYPE_END(ThreadStore)

CDAC_TYPE_BEGIN(RuntimeThreadLocals)
CDAC_TYPE_INDETERMINATE(RuntimeThreadLocals)
CDAC_TYPE_FIELD(RuntimeThreadLocals, TYPE(EEAllocContext), AllocContext, offsetof(RuntimeThreadLocals, alloc_context))
CDAC_TYPE_END(RuntimeThreadLocals)

CDAC_TYPE_BEGIN(IdDispenser)
CDAC_TYPE_INDETERMINATE(IdDispenser)
CDAC_TYPE_FIELD(IdDispenser, T_POINTER, IdToThread, cdac_data<IdDispenser>::IdToThread)
CDAC_TYPE_FIELD(IdDispenser, T_UINT32, HighestId, cdac_data<IdDispenser>::HighestId)
CDAC_TYPE_END(IdDispenser)

CDAC_TYPE_BEGIN(ThreadStaticsInfo)
CDAC_TYPE_SIZE(sizeof(ThreadStaticsInfo))
CDAC_TYPE_FIELD(ThreadStaticsInfo, TYPE(TLSIndex), GCTlsIndex, offsetof(ThreadStaticsInfo, GCTlsIndex))
CDAC_TYPE_FIELD(ThreadStaticsInfo, TYPE(TLSIndex), NonGCTlsIndex, offsetof(ThreadStaticsInfo, NonGCTlsIndex))
CDAC_TYPE_END(ThreadStaticsInfo)

CDAC_TYPE_BEGIN(ThreadLocalData)
CDAC_TYPE_INDETERMINATE(ThreadLocalData)
CDAC_TYPE_FIELD(ThreadLocalData, T_POINTER, CollectibleTlsArrayData, offsetof(ThreadLocalData, pCollectibleTlsArrayData))
CDAC_TYPE_FIELD(ThreadLocalData, T_POINTER, NonCollectibleTlsArrayData, offsetof(ThreadLocalData, pNonCollectibleTlsArrayData))
CDAC_TYPE_FIELD(ThreadLocalData, T_INT32, CollectibleTlsDataCount, offsetof(ThreadLocalData, cCollectibleTlsData))
CDAC_TYPE_FIELD(ThreadLocalData, T_INT32, NonCollectibleTlsDataCount, offsetof(ThreadLocalData, cNonCollectibleTlsData))
CDAC_TYPE_FIELD(ThreadLocalData, T_POINTER, InFlightData, offsetof(ThreadLocalData, pInFlightData))
CDAC_TYPE_END(ThreadLocalData)

CDAC_TYPE_BEGIN(InFlightTLSData)
CDAC_TYPE_INDETERMINATE(InFlightTLSData)
CDAC_TYPE_FIELD(InFlightTLSData, T_POINTER, Next, offsetof(InFlightTLSData, pNext))
CDAC_TYPE_FIELD(InFlightTLSData, TYPE(TLSIndex), TlsIndex, offsetof(InFlightTLSData, tlsIndex))
CDAC_TYPE_FIELD(InFlightTLSData, T_POINTER, TLSData, offsetof(InFlightTLSData, hTLSData))
CDAC_TYPE_END(InFlightTLSData)

CDAC_TYPE_BEGIN(TLSIndex)
CDAC_TYPE_INDETERMINATE(TLSIndex)
CDAC_TYPE_FIELD(TLSIndex, T_UINT32, TLSIndexRawIndex, offsetof(TLSIndex, TLSIndexRawIndex))
CDAC_TYPE_END(TLSIndex)

CDAC_TYPE_BEGIN(EEAllocContext)
CDAC_TYPE_INDETERMINATE(EEAllocContext)
CDAC_TYPE_FIELD(EEAllocContext, TYPE(GCAllocContext), GCAllocationContext, offsetof(ee_alloc_context, m_GCAllocContext))
CDAC_TYPE_END(EEAllocContext)

CDAC_TYPE_BEGIN(GCAllocContext)
CDAC_TYPE_INDETERMINATE(GCAllocContext)
CDAC_TYPE_FIELD(GCAllocContext, T_POINTER, Pointer, offsetof(gc_alloc_context, alloc_ptr))
CDAC_TYPE_FIELD(GCAllocContext, T_POINTER, Limit, offsetof(gc_alloc_context, alloc_limit))
CDAC_TYPE_FIELD(GCAllocContext, T_INT64, AllocBytes, offsetof(gc_alloc_context, alloc_bytes))
CDAC_TYPE_FIELD(GCAllocContext, T_INT64, AllocBytesLoh, offsetof(gc_alloc_context, alloc_bytes_uoh))
CDAC_TYPE_END(GCAllocContext)

// Exception

// Use exact managed type field names for the descriptor as field names often can't change due to binary serialization or implicit diagnostic contracts
CDAC_TYPE_BEGIN(Exception)
CDAC_TYPE_INDETERMINATE(Exception)
CDAC_TYPE_FIELD(Exception, T_POINTER, _message, cdac_data<ExceptionObject>::_message)
CDAC_TYPE_FIELD(Exception, T_POINTER, _innerException, cdac_data<ExceptionObject>::_innerException)
CDAC_TYPE_FIELD(Exception, T_POINTER, _stackTrace, cdac_data<ExceptionObject>::_stackTrace)
CDAC_TYPE_FIELD(Exception, T_POINTER, _watsonBuckets, cdac_data<ExceptionObject>::_watsonBuckets)
CDAC_TYPE_FIELD(Exception, T_POINTER, _stackTraceString, cdac_data<ExceptionObject>::_stackTraceString)
CDAC_TYPE_FIELD(Exception, T_POINTER, _remoteStackTraceString, cdac_data<ExceptionObject>::_remoteStackTraceString)
CDAC_TYPE_FIELD(Exception, T_INT32, _HResult, cdac_data<ExceptionObject>::_HResult)
CDAC_TYPE_FIELD(Exception, T_INT32, _xcode, cdac_data<ExceptionObject>::_xcode)
CDAC_TYPE_END(Exception)

CDAC_TYPE_BEGIN(ExceptionInfo)
CDAC_TYPE_INDETERMINATE(ExceptionInfo)
CDAC_TYPE_FIELD(ExceptionInfo, T_POINTER, PreviousNestedInfo, offsetof(ExInfo, m_pPrevNestedInfo))
CDAC_TYPE_FIELD(ExceptionInfo, T_POINTER, ThrownObjectHandle, offsetof(ExInfo, m_hThrowable))
CDAC_TYPE_FIELD(ExceptionInfo, T_UINT32, ExceptionFlags, cdac_data<ExInfo>::ExceptionFlagsValue)
CDAC_TYPE_FIELD(ExceptionInfo, T_POINTER, StackLowBound, cdac_data<ExInfo>::StackLowBound)
CDAC_TYPE_FIELD(ExceptionInfo, T_POINTER, StackHighBound, cdac_data<ExInfo>::StackHighBound)
#ifndef TARGET_UNIX
CDAC_TYPE_FIELD(ExceptionInfo, T_POINTER, ExceptionWatsonBucketTrackerBuckets, cdac_data<ExInfo>::ExceptionWatsonBucketTrackerBuckets)
#endif // TARGET_UNIX
CDAC_TYPE_FIELD(ExceptionInfo, T_UINT8, PassNumber, offsetof(ExInfo, m_passNumber))
CDAC_TYPE_FIELD(ExceptionInfo, T_POINTER, CSFEHClause, offsetof(ExInfo, m_csfEHClause))
CDAC_TYPE_FIELD(ExceptionInfo, T_POINTER, CSFEnclosingClause, offsetof(ExInfo, m_csfEnclosingClause))
CDAC_TYPE_FIELD(ExceptionInfo, T_POINTER, CallerOfActualHandlerFrame, offsetof(ExInfo, m_sfCallerOfActualHandlerFrame))
CDAC_TYPE_END(ExceptionInfo)

CDAC_TYPE_BEGIN(GCHandle)
CDAC_TYPE_SIZE(sizeof(OBJECTHANDLE))
CDAC_TYPE_END(GCHandle)

// Object

CDAC_TYPE_BEGIN(Object)
CDAC_TYPE_SIZE(sizeof(Object))
CDAC_TYPE_FIELD(Object, T_POINTER, m_pMethTab, cdac_data<Object>::m_pMethTab)
CDAC_TYPE_END(Object)

CDAC_TYPE_BEGIN(ObjectHeader)
CDAC_TYPE_SIZE(OBJHEADER_SIZE)
CDAC_TYPE_FIELD(ObjectHeader, T_UINT32, SyncBlockValue, cdac_data<ObjHeader>::SyncBlockValue)
CDAC_TYPE_END(ObjectHeader)

CDAC_TYPE_BEGIN(String)
CDAC_TYPE_INDETERMINATE(String)
CDAC_TYPE_FIELD(String, T_POINTER, m_FirstChar, cdac_data<StringObject>::m_FirstChar)
CDAC_TYPE_FIELD(String, T_UINT32, m_StringLength, cdac_data<StringObject>::m_StringLength)
CDAC_TYPE_END(String)

CDAC_TYPE_BEGIN(Array)
CDAC_TYPE_SIZE(sizeof(ArrayBase))
CDAC_TYPE_FIELD(Array, T_UINT32, m_NumComponents, cdac_data<ArrayBase>::m_NumComponents)
CDAC_TYPE_END(Array)

CDAC_TYPE_BEGIN(InteropSyncBlockInfo)
CDAC_TYPE_INDETERMINATE(InteropSyncBlockInfo)
#ifdef FEATURE_COMINTEROP
CDAC_TYPE_FIELD(InteropSyncBlockInfo, T_POINTER, CCW, cdac_data<InteropSyncBlockInfo>::CCW)
CDAC_TYPE_FIELD(InteropSyncBlockInfo, T_POINTER, RCW, cdac_data<InteropSyncBlockInfo>::RCW)
CDAC_TYPE_FIELD(InteropSyncBlockInfo, T_POINTER, CCF, cdac_data<InteropSyncBlockInfo>::CCF)
#endif // FEATURE_COMINTEROP
CDAC_TYPE_END(InteropSyncBlockInfo)

CDAC_TYPE_BEGIN(SyncBlock)
CDAC_TYPE_INDETERMINATE(SyncBlock)
CDAC_TYPE_FIELD(SyncBlock, T_POINTER, InteropInfo, cdac_data<SyncBlock>::InteropInfo)
CDAC_TYPE_FIELD(SyncBlock, T_POINTER, Lock, cdac_data<SyncBlock>::Lock)
CDAC_TYPE_FIELD(SyncBlock, T_UINT32, ThinLock, cdac_data<SyncBlock>::ThinLock)
CDAC_TYPE_FIELD(SyncBlock, T_POINTER, LinkNext, cdac_data<SyncBlock>::LinkNext)
CDAC_TYPE_FIELD(SyncBlock, T_UINT32, HashCode, cdac_data<SyncBlock>::HashCode)
CDAC_TYPE_END(SyncBlock)

CDAC_TYPE_BEGIN(SLink)
CDAC_TYPE_INDETERMINATE(SLink)
CDAC_TYPE_FIELD(SLink, T_POINTER, Next, offsetof(SLink, m_pNext))
CDAC_TYPE_END(SLink)

#ifdef FEATURE_COMWRAPPERS
CDAC_TYPE_BEGIN(NativeObjectWrapperObject)
CDAC_TYPE_INDETERMINATE(NativeObjectWrapperObject)
CDAC_TYPE_FIELD(NativeObjectWrapperObject, T_POINTER, ExternalComObject, cdac_data<NativeObjectWrapperObject>::ExternalComObject)
CDAC_TYPE_END(NativeObjectWrapperObject)

CDAC_TYPE_BEGIN(ManagedObjectWrapperHolderObject)
CDAC_TYPE_INDETERMINATE(ManagedObjectWrapperHolderObject)
CDAC_TYPE_FIELD(ManagedObjectWrapperHolderObject, T_POINTER, WrappedObject, cdac_data<ManagedObjectWrapperHolderObject>::WrappedObject)
CDAC_TYPE_FIELD(ManagedObjectWrapperHolderObject, T_POINTER, Wrapper, cdac_data<ManagedObjectWrapperHolderObject>::Wrapper)
CDAC_TYPE_END(ManagedObjectWrapperHolderObject)

CDAC_TYPE_BEGIN(ManagedObjectWrapperLayout)
CDAC_TYPE_INDETERMINATE(ManagedObjectWrapperLayout)
CDAC_TYPE_FIELD(ManagedObjectWrapperLayout, T_INT64, RefCount, cdac_data<InteropLib::ABI::ManagedObjectWrapperLayout>::RefCount)
CDAC_TYPE_FIELD(ManagedObjectWrapperLayout, T_INT32, Flags, cdac_data<InteropLib::ABI::ManagedObjectWrapperLayout>::Flags)
CDAC_TYPE_FIELD(ManagedObjectWrapperLayout, T_INT32, UserDefinedCount, cdac_data<InteropLib::ABI::ManagedObjectWrapperLayout>::UserDefinedCount)
CDAC_TYPE_FIELD(ManagedObjectWrapperLayout, T_POINTER, UserDefined, cdac_data<InteropLib::ABI::ManagedObjectWrapperLayout>::UserDefined)
CDAC_TYPE_FIELD(ManagedObjectWrapperLayout, T_POINTER, Dispatches, cdac_data<InteropLib::ABI::ManagedObjectWrapperLayout>::Dispatches)
CDAC_TYPE_END(ManagedObjectWrapperLayout)

CDAC_TYPE_BEGIN(ComInterfaceEntry)
CDAC_TYPE_SIZE(sizeof(InteropLib::ABI::ComInterfaceEntry))
CDAC_TYPE_FIELD(ComInterfaceEntry, T_NUINT, IID, cdac_data<InteropLib::ABI::ComInterfaceEntry>::IID)
CDAC_TYPE_END(ComInterfaceEntry)

CDAC_TYPE_BEGIN(InternalComInterfaceDispatch)
CDAC_TYPE_SIZE(sizeof(InteropLib::ABI::InternalComInterfaceDispatch))
CDAC_TYPE_FIELD(InternalComInterfaceDispatch, T_POINTER, Entries, cdac_data<InteropLib::ABI::InternalComInterfaceDispatch>::Entries)
CDAC_TYPE_END(InternalComInterfaceDispatch)
#endif // FEATURE_COMWRAPPERS

CDAC_TYPE_BEGIN(SyncTableEntry)
CDAC_TYPE_SIZE(sizeof(SyncTableEntry))
CDAC_TYPE_FIELD(SyncTableEntry, T_POINTER, SyncBlock, offsetof(SyncTableEntry, m_SyncBlock))
CDAC_TYPE_FIELD(SyncTableEntry, T_POINTER, Object, offsetof(SyncTableEntry, m_Object))
CDAC_TYPE_END(SyncTableEntry)

// Loader

CDAC_TYPE_BEGIN(Module)
CDAC_TYPE_INDETERMINATE(Module)
CDAC_TYPE_FIELD(Module, T_POINTER, Assembly, cdac_data<Module>::Assembly)
CDAC_TYPE_FIELD(Module, T_POINTER, PEAssembly, cdac_data<Module>::PEAssembly)
CDAC_TYPE_FIELD(Module, T_POINTER, Base, cdac_data<Module>::Base)
CDAC_TYPE_FIELD(Module, T_UINT32, Flags, cdac_data<Module>::Flags)
CDAC_TYPE_FIELD(Module, T_POINTER, LoaderAllocator, cdac_data<Module>::LoaderAllocator)
CDAC_TYPE_FIELD(Module, T_POINTER, DynamicMetadata, cdac_data<Module>::DynamicMetadata)
CDAC_TYPE_FIELD(Module, T_POINTER, SimpleName, cdac_data<Module>::SimpleName)
CDAC_TYPE_FIELD(Module, T_POINTER, Path, cdac_data<Module>::Path)
CDAC_TYPE_FIELD(Module, T_POINTER, FileName, cdac_data<Module>::FileName)
CDAC_TYPE_FIELD(Module, T_POINTER, ReadyToRunInfo, cdac_data<Module>::ReadyToRunInfo)
CDAC_TYPE_FIELD(Module, T_POINTER, GrowableSymbolStream, cdac_data<Module>::GrowableSymbolStream)
CDAC_TYPE_FIELD(Module, T_POINTER, AvailableTypeParams, offsetof(Module, m_pAvailableParamTypes))
CDAC_TYPE_FIELD(Module, T_POINTER, InstMethodHashTable, offsetof(Module, m_pInstMethodHashTable))

CDAC_TYPE_FIELD(Module, T_POINTER, FieldDefToDescMap, cdac_data<Module>::FieldDefToDescMap)
CDAC_TYPE_FIELD(Module, T_POINTER, ManifestModuleReferencesMap, cdac_data<Module>::ManifestModuleReferencesMap)
CDAC_TYPE_FIELD(Module, T_POINTER, MemberRefToDescMap, cdac_data<Module>::MemberRefToDescMap)
CDAC_TYPE_FIELD(Module, T_POINTER, MethodDefToDescMap, cdac_data<Module>::MethodDefToDescMap)
CDAC_TYPE_FIELD(Module, T_POINTER, TypeDefToMethodTableMap, cdac_data<Module>::TypeDefToMethodTableMap)
CDAC_TYPE_FIELD(Module, T_POINTER, TypeRefToMethodTableMap, cdac_data<Module>::TypeRefToMethodTableMap)
#ifdef FEATURE_CODE_VERSIONING
CDAC_TYPE_FIELD(Module, T_POINTER, MethodDefToILCodeVersioningStateMap, cdac_data<Module>::MethodDefToILCodeVersioningStateMap)
#endif // FEATURE_CODE_VERSIONING
CDAC_TYPE_FIELD(Module, T_POINTER, DynamicILBlobTable, cdac_data<Module>::DynamicILBlobTable)
CDAC_TYPE_END(Module)

CDAC_TYPE_BEGIN(ModuleLookupMap)
CDAC_TYPE_FIELD(ModuleLookupMap, T_POINTER, TableData, offsetof(LookupMapBase, pTable))
CDAC_TYPE_FIELD(ModuleLookupMap, T_POINTER, Next, offsetof(LookupMapBase, pNext))
CDAC_TYPE_FIELD(ModuleLookupMap, T_UINT32, Count, offsetof(LookupMapBase, dwCount))
CDAC_TYPE_FIELD(ModuleLookupMap, T_NUINT, SupportedFlagsMask, offsetof(LookupMapBase, supportedFlags))
CDAC_TYPE_END(ModuleLookupMap)

CDAC_TYPE_BEGIN(Assembly)
CDAC_TYPE_INDETERMINATE(Assembly)
#ifdef FEATURE_COLLECTIBLE_TYPES
CDAC_TYPE_FIELD(Assembly, T_UINT8, IsCollectible, cdac_data<Assembly>::IsCollectible)
#endif
CDAC_TYPE_FIELD(Assembly, T_BOOL, IsDynamic, cdac_data<Assembly>::IsDynamic)
CDAC_TYPE_FIELD(Assembly, T_POINTER, Module, cdac_data<Assembly>::Module)
CDAC_TYPE_FIELD(Assembly, T_POINTER, Error, cdac_data<Assembly>::Error)
CDAC_TYPE_FIELD(Assembly, T_UINT32, NotifyFlags, cdac_data<Assembly>::NotifyFlags)
CDAC_TYPE_FIELD(Assembly, T_BOOL, IsLoaded, cdac_data<Assembly>::IsLoaded)
CDAC_TYPE_END(Assembly)

CDAC_TYPE_BEGIN(LoaderAllocator)
CDAC_TYPE_INDETERMINATE(LoaderAllocator)
CDAC_TYPE_FIELD(LoaderAllocator, T_UINT32, ReferenceCount, cdac_data<LoaderAllocator>::ReferenceCount)
CDAC_TYPE_FIELD(LoaderAllocator, T_POINTER, HighFrequencyHeap, cdac_data<LoaderAllocator>::HighFrequencyHeap)
CDAC_TYPE_FIELD(LoaderAllocator, T_POINTER, LowFrequencyHeap, cdac_data<LoaderAllocator>::LowFrequencyHeap)
CDAC_TYPE_FIELD(LoaderAllocator, T_POINTER, StaticsHeap, cdac_data<LoaderAllocator>::StaticsHeap)
CDAC_TYPE_FIELD(LoaderAllocator, T_POINTER, StubHeap, cdac_data<LoaderAllocator>::StubHeap)
CDAC_TYPE_FIELD(LoaderAllocator, T_POINTER, ExecutableHeap, cdac_data<LoaderAllocator>::ExecutableHeap)
#ifdef HAS_FIXUP_PRECODE
CDAC_TYPE_FIELD(LoaderAllocator, T_POINTER, FixupPrecodeHeap, cdac_data<LoaderAllocator>::FixupPrecodeHeap)
#endif // HAS_FIXUP_PRECODE
#ifndef FEATURE_PORTABLE_ENTRYPOINTS
CDAC_TYPE_FIELD(LoaderAllocator, T_POINTER, NewStubPrecodeHeap, cdac_data<LoaderAllocator>::NewStubPrecodeHeap)
#endif // !FEATURE_PORTABLE_ENTRYPOINTS
#if defined(FEATURE_READYTORUN) && defined(FEATURE_STUBPRECODE_DYNAMIC_HELPERS)
CDAC_TYPE_FIELD(LoaderAllocator, T_POINTER, DynamicHelpersStubHeap, cdac_data<LoaderAllocator>::DynamicHelpersStubHeap)
#endif // defined(FEATURE_READYTORUN) && defined(FEATURE_STUBPRECODE_DYNAMIC_HELPERS)
CDAC_TYPE_FIELD(LoaderAllocator, T_POINTER, VirtualCallStubManager, cdac_data<LoaderAllocator>::VirtualCallStubManager)
CDAC_TYPE_FIELD(LoaderAllocator, T_POINTER, ObjectHandle, cdac_data<LoaderAllocator>::ObjectHandle)
CDAC_TYPE_END(LoaderAllocator)

CDAC_TYPE_BEGIN(VirtualCallStubManager)
CDAC_TYPE_INDETERMINATE(VirtualCallStubManager)
CDAC_TYPE_FIELD(VirtualCallStubManager, T_POINTER, IndcellHeap, cdac_data<VirtualCallStubManager>::IndcellHeap)
#ifdef FEATURE_VIRTUAL_STUB_DISPATCH
CDAC_TYPE_FIELD(VirtualCallStubManager, T_POINTER, CacheEntryHeap, cdac_data<VirtualCallStubManager>::CacheEntryHeap)
#endif // FEATURE_VIRTUAL_STUB_DISPATCH
CDAC_TYPE_END(VirtualCallStubManager)

CDAC_TYPE_BEGIN(PEAssembly)
CDAC_TYPE_INDETERMINATE(PEAssembly)
CDAC_TYPE_FIELD(PEAssembly, T_POINTER, PEImage, cdac_data<PEAssembly>::PEImage)
CDAC_TYPE_FIELD(PEAssembly, T_POINTER, AssemblyBinder, cdac_data<PEAssembly>::AssemblyBinder)
CDAC_TYPE_END(PEAssembly)

CDAC_TYPE_BEGIN(AssemblyBinder)
CDAC_TYPE_INDETERMINATE(AssemblyBinder)
CDAC_TYPE_FIELD(AssemblyBinder, T_POINTER, AssemblyLoadContext, cdac_data<AssemblyBinder>::AssemblyLoadContext)
CDAC_TYPE_END(AssemblyBinder)

CDAC_TYPE_BEGIN(PEImage)
CDAC_TYPE_INDETERMINATE(PEImage)
CDAC_TYPE_FIELD(PEImage, T_POINTER, LoadedImageLayout, cdac_data<PEImage>::LoadedImageLayout)
CDAC_TYPE_FIELD(PEImage, TYPE(ProbeExtensionResult), ProbeExtensionResult, cdac_data<PEImage>::ProbeExtensionResult)
CDAC_TYPE_END(PEImage)

CDAC_TYPE_BEGIN(PEImageLayout)
CDAC_TYPE_FIELD(PEImageLayout, T_POINTER, Base, cdac_data<PEImageLayout>::Base)
CDAC_TYPE_FIELD(PEImageLayout, T_UINT32, Size, cdac_data<PEImageLayout>::Size)
CDAC_TYPE_FIELD(PEImageLayout, T_UINT32, Flags, cdac_data<PEImageLayout>::Flags)
CDAC_TYPE_FIELD(PEImageLayout, T_UINT32, Format, cdac_data<PEImageLayout>::Format)
CDAC_TYPE_END(PEImageLayout)

CDAC_TYPE_BEGIN(CGrowableSymbolStream)
CDAC_TYPE_INDETERMINATE(CGrowableSymbolStream)
CDAC_TYPE_FIELD(CGrowableSymbolStream, T_POINTER, Buffer, cdac_data<CGrowableStream>::Buffer)
CDAC_TYPE_FIELD(CGrowableSymbolStream, T_UINT32, Size, cdac_data<CGrowableStream>::Size)
CDAC_TYPE_END(CGrowableSymbolStream)

CDAC_TYPE_BEGIN(ProbeExtensionResult)
CDAC_TYPE_INDETERMINATE(ProbeExtensionResult)
CDAC_TYPE_FIELD(ProbeExtensionResult, T_INT32, Type, offsetof(ProbeExtensionResult, Type))
CDAC_TYPE_END(ProbeExtensionResult)

CDAC_TYPE_BEGIN(AppDomain)
CDAC_TYPE_INDETERMINATE(AppDomain)
CDAC_TYPE_FIELD(AppDomain, T_POINTER, RootAssembly, cdac_data<AppDomain>::RootAssembly)
CDAC_TYPE_FIELD(AppDomain, TYPE(ArrayListBase), AssemblyList, cdac_data<AppDomain>::AssemblyList)
CDAC_TYPE_FIELD(AppDomain, T_POINTER, FriendlyName, cdac_data<AppDomain>::FriendlyName)
CDAC_TYPE_END(AppDomain)

CDAC_TYPE_BEGIN(SystemDomain)
CDAC_TYPE_INDETERMINATE(SystemDomain)
CDAC_TYPE_FIELD(SystemDomain, TYPE(LoaderAllocator), GlobalLoaderAllocator, cdac_data<SystemDomain>::GlobalLoaderAllocator)
CDAC_TYPE_FIELD(SystemDomain, T_POINTER, SystemAssembly, cdac_data<SystemDomain>::SystemAssembly)
CDAC_TYPE_END(SystemDomain)

#if defined(DEBUGGING_SUPPORTED) && !defined(TARGET_WASM)
CDAC_TYPE_BEGIN(Debugger)
CDAC_TYPE_INDETERMINATE(Debugger)
CDAC_TYPE_FIELD(Debugger, T_INT32, LeftSideInitialized, offsetof(Debugger, m_fLeftSideInitialized))
CDAC_TYPE_FIELD(Debugger, T_UINT32, Defines, offsetof(Debugger, m_defines))
CDAC_TYPE_FIELD(Debugger, T_UINT32, MDStructuresVersion, offsetof(Debugger, m_mdDataStructureVersion))
CDAC_TYPE_END(Debugger)
#endif // DEBUGGING_SUPPORTED && !TARGET_WASM

CDAC_TYPE_BEGIN(ArrayListBase)
CDAC_TYPE_INDETERMINATE(ArrayListBase)
CDAC_TYPE_FIELD(ArrayListBase, T_UINT32, Count, cdac_data<ArrayListBase>::Count)
CDAC_TYPE_FIELD(ArrayListBase, T_POINTER, FirstBlock, cdac_data<ArrayListBase>::FirstBlock)
CDAC_TYPE_END(ArrayListBase)

CDAC_TYPE_BEGIN(ArrayListBlock)
CDAC_TYPE_INDETERMINATE(ArrayListBlock)
CDAC_TYPE_FIELD(ArrayListBlock, T_POINTER, Next, cdac_data<ArrayListBase>::Next)
CDAC_TYPE_FIELD(ArrayListBlock, T_UINT32, Size, cdac_data<ArrayListBase>::Size)
CDAC_TYPE_FIELD(ArrayListBlock, T_POINTER, ArrayStart, cdac_data<ArrayListBase>::ArrayStart)
CDAC_TYPE_END(ArrayListBlock)

// RuntimeTypeSystem

CDAC_TYPE_BEGIN(MethodTable)
CDAC_TYPE_SIZE(sizeof(MethodTable))
CDAC_TYPE_FIELD(MethodTable, T_UINT32, MTFlags, cdac_data<MethodTable>::MTFlags)
CDAC_TYPE_FIELD(MethodTable, T_UINT32, BaseSize, cdac_data<MethodTable>::BaseSize)
CDAC_TYPE_FIELD(MethodTable, T_UINT32, MTFlags2, cdac_data<MethodTable>::MTFlags2)
CDAC_TYPE_FIELD(MethodTable, T_POINTER, EEClassOrCanonMT, cdac_data<MethodTable>::EEClassOrCanonMT)
CDAC_TYPE_FIELD(MethodTable, T_POINTER, Module, cdac_data<MethodTable>::Module)
CDAC_TYPE_FIELD(MethodTable, T_POINTER, ParentMethodTable, cdac_data<MethodTable>::ParentMethodTable)
CDAC_TYPE_FIELD(MethodTable, T_UINT16, NumInterfaces, cdac_data<MethodTable>::NumInterfaces)
CDAC_TYPE_FIELD(MethodTable, T_UINT16, NumVirtuals, cdac_data<MethodTable>::NumVirtuals)
CDAC_TYPE_FIELD(MethodTable, T_POINTER, PerInstInfo, cdac_data<MethodTable>::PerInstInfo)
CDAC_TYPE_FIELD(MethodTable, T_POINTER, AuxiliaryData, cdac_data<MethodTable>::AuxiliaryData)
CDAC_TYPE_END(MethodTable)

CDAC_TYPE_BEGIN(DynamicStaticsInfo)
CDAC_TYPE_SIZE(sizeof(DynamicStaticsInfo))
CDAC_TYPE_FIELD(DynamicStaticsInfo, T_POINTER, GCStatics, offsetof(DynamicStaticsInfo, m_pGCStatics))
CDAC_TYPE_FIELD(DynamicStaticsInfo, T_POINTER, NonGCStatics, offsetof(DynamicStaticsInfo, m_pNonGCStatics))
CDAC_TYPE_END(DynamicStaticsInfo)

CDAC_TYPE_BEGIN(MethodTableAuxiliaryData)
CDAC_TYPE_INDETERMINATE(MethodTableAuxiliaryData)
CDAC_TYPE_FIELD(MethodTableAuxiliaryData, T_POINTER, LoaderModule, offsetof(MethodTableAuxiliaryData, m_pLoaderModule))
CDAC_TYPE_FIELD(MethodTableAuxiliaryData, T_INT16, OffsetToNonVirtualSlots, offsetof(MethodTableAuxiliaryData, m_offsetToNonVirtualSlots))
CDAC_TYPE_FIELD(MethodTableAuxiliaryData, T_UINT32, Flags, offsetof(MethodTableAuxiliaryData, m_dwFlags))
CDAC_TYPE_END(MethodTableAuxiliaryData)

CDAC_TYPE_BEGIN(EEClass)
CDAC_TYPE_INDETERMINATE(EEClass)
CDAC_TYPE_FIELD(EEClass, T_POINTER, MethodTable, cdac_data<EEClass>::MethodTable)
CDAC_TYPE_FIELD(EEClass, T_POINTER, MethodDescChunk, cdac_data<EEClass>::MethodDescChunk)
CDAC_TYPE_FIELD(EEClass, T_UINT16, NumMethods, cdac_data<EEClass>::NumMethods)
CDAC_TYPE_FIELD(EEClass, T_POINTER, FieldDescList, cdac_data<EEClass>::FieldDescList)
CDAC_TYPE_FIELD(EEClass, T_UINT32, CorTypeAttr, cdac_data<EEClass>::CorTypeAttr)
CDAC_TYPE_FIELD(EEClass, T_UINT8, InternalCorElementType, cdac_data<EEClass>::InternalCorElementType)
CDAC_TYPE_FIELD(EEClass, T_UINT16, NumInstanceFields, cdac_data<EEClass>::NumInstanceFields)
CDAC_TYPE_FIELD(EEClass, T_UINT16, NumStaticFields, cdac_data<EEClass>::NumStaticFields)
CDAC_TYPE_FIELD(EEClass, T_UINT16, NumThreadStaticFields, cdac_data<EEClass>::NumThreadStaticFields)
CDAC_TYPE_FIELD(EEClass, T_UINT16, NumNonVirtualSlots, cdac_data<EEClass>::NumNonVirtualSlots)
CDAC_TYPE_END(EEClass)

CDAC_TYPE_BEGIN(GenericsDictInfo)
CDAC_TYPE_INDETERMINATE(GenericsDictInfo)
CDAC_TYPE_FIELD(GenericsDictInfo, T_UINT16, NumDicts, offsetof(GenericsDictInfo, m_wNumDicts))
CDAC_TYPE_FIELD(GenericsDictInfo, T_UINT16, NumTypeArgs, offsetof(GenericsDictInfo, m_wNumTyPars))
CDAC_TYPE_END(GenericsDictInfo)

CDAC_TYPE_BEGIN(TypeDesc)
CDAC_TYPE_INDETERMINATE(TypeDesc)
CDAC_TYPE_FIELD(TypeDesc, T_UINT32, TypeAndFlags, cdac_data<TypeDesc>::TypeAndFlags)
CDAC_TYPE_END(TypeDesc)

CDAC_TYPE_BEGIN(FieldDesc)
CDAC_TYPE_SIZE(sizeof(FieldDesc))
CDAC_TYPE_FIELD(FieldDesc, T_UINT32, DWord1, cdac_data<FieldDesc>::DWord1)
CDAC_TYPE_FIELD(FieldDesc, T_UINT32, DWord2, cdac_data<FieldDesc>::DWord2)
CDAC_TYPE_FIELD(FieldDesc, T_POINTER, MTOfEnclosingClass, cdac_data<FieldDesc>::MTOfEnclosingClass)
CDAC_TYPE_END(FieldDesc)

CDAC_TYPE_BEGIN(ParamTypeDesc)
CDAC_TYPE_INDETERMINATE(ParamTypeDesc)
CDAC_TYPE_FIELD(ParamTypeDesc, T_POINTER, TypeArg, cdac_data<ParamTypeDesc>::TypeArg)
CDAC_TYPE_END(ParamTypeDesc)

CDAC_TYPE_BEGIN(TypeVarTypeDesc)
CDAC_TYPE_INDETERMINATE(TypeVarTypeDesc)
CDAC_TYPE_FIELD(TypeVarTypeDesc, T_POINTER, Module, cdac_data<TypeVarTypeDesc>::Module)
CDAC_TYPE_FIELD(TypeVarTypeDesc, T_UINT32, Token, cdac_data<TypeVarTypeDesc>::Token)
CDAC_TYPE_END(TypeVarTypeDesc)

CDAC_TYPE_BEGIN(FnPtrTypeDesc)
CDAC_TYPE_INDETERMINATE(FnPtrTypeDesc)
CDAC_TYPE_FIELD(FnPtrTypeDesc, T_UINT32, NumArgs, cdac_data<FnPtrTypeDesc>::NumArgs)
CDAC_TYPE_FIELD(FnPtrTypeDesc, T_UINT32, CallConv, cdac_data<FnPtrTypeDesc>::CallConv)
CDAC_TYPE_FIELD(FnPtrTypeDesc, T_UINT32, RetAndArgTypes, cdac_data<FnPtrTypeDesc>::RetAndArgTypes)
CDAC_TYPE_FIELD(FnPtrTypeDesc, T_POINTER, LoaderModule, cdac_data<FnPtrTypeDesc>::LoaderModule)
CDAC_TYPE_END(FnPtrTypeDesc)

CDAC_TYPE_BEGIN(DynamicMetadata)
CDAC_TYPE_FIELD(DynamicMetadata, T_UINT32, Size, cdac_data<DynamicMetadata>::Size)
CDAC_TYPE_FIELD(DynamicMetadata, T_ARRAY(T_UINT8), Data, cdac_data<DynamicMetadata>::Data)
CDAC_TYPE_END(DynamicMetadata)

#ifdef STRESS_LOG
CDAC_TYPE_BEGIN(StressLog)
CDAC_TYPE_SIZE(sizeof(StressLog))
CDAC_TYPE_FIELD(StressLog, T_UINT32, LoggedFacilities, cdac_offsets<StressLog>::facilitiesToLog)
CDAC_TYPE_FIELD(StressLog, T_UINT32, Level, cdac_offsets<StressLog>::levelToLog)
CDAC_TYPE_FIELD(StressLog, T_UINT32, MaxSizePerThread, cdac_offsets<StressLog>::MaxSizePerThread)
CDAC_TYPE_FIELD(StressLog, T_UINT32, MaxSizeTotal, cdac_offsets<StressLog>::MaxSizeTotal)
CDAC_TYPE_FIELD(StressLog, T_UINT32, TotalChunks, cdac_offsets<StressLog>::totalChunk)
CDAC_TYPE_FIELD(StressLog, T_POINTER, Logs, cdac_offsets<StressLog>::logs)
CDAC_TYPE_FIELD(StressLog, T_UINT64, TickFrequency, cdac_offsets<StressLog>::tickFrequency)
CDAC_TYPE_FIELD(StressLog, T_UINT64, StartTimestamp, cdac_offsets<StressLog>::startTimeStamp)
CDAC_TYPE_FIELD(StressLog, T_NUINT, ModuleOffset, cdac_offsets<StressLog>::moduleOffset)
CDAC_TYPE_FIELD(StressLog, T_ARRAY(TYPE(StressLogModuleDesc)), Modules, cdac_offsets<StressLog>::modules)
CDAC_TYPE_END(StressLog)

CDAC_TYPE_BEGIN(StressLogModuleDesc)
CDAC_TYPE_SIZE(cdac_offsets<StressLog>::ModuleDesc::type_size)
CDAC_TYPE_FIELD(StressLogModuleDesc, T_POINTER, BaseAddress, cdac_offsets<StressLog>::ModuleDesc::baseAddress)
CDAC_TYPE_FIELD(StressLogModuleDesc, T_NUINT, Size, cdac_offsets<StressLog>::ModuleDesc::size)
CDAC_TYPE_END(StressLogModuleDesc)

CDAC_TYPE_BEGIN(ThreadStressLog)
CDAC_TYPE_INDETERMINATE(ThreadStressLog)
CDAC_TYPE_FIELD(ThreadStressLog, T_POINTER, Next, cdac_offsets<ThreadStressLog>::next)
CDAC_TYPE_FIELD(ThreadStressLog, T_UINT64, ThreadId, cdac_offsets<ThreadStressLog>::threadId)
CDAC_TYPE_FIELD(ThreadStressLog, T_UINT8, WriteHasWrapped, cdac_offsets<ThreadStressLog>::writeHasWrapped)
CDAC_TYPE_FIELD(ThreadStressLog, T_POINTER, CurrentPtr, cdac_offsets<ThreadStressLog>::curPtr)
CDAC_TYPE_FIELD(ThreadStressLog, T_POINTER, ChunkListHead, cdac_offsets<ThreadStressLog>::chunkListHead)
CDAC_TYPE_FIELD(ThreadStressLog, T_POINTER, ChunkListTail, cdac_offsets<ThreadStressLog>::chunkListTail)
CDAC_TYPE_FIELD(ThreadStressLog, T_POINTER, CurrentWriteChunk, cdac_offsets<ThreadStressLog>::curWriteChunk)
CDAC_TYPE_END(ThreadStressLog)

CDAC_TYPE_BEGIN(StressLogChunk)
CDAC_TYPE_SIZE(sizeof(StressLogChunk))
CDAC_TYPE_FIELD(StressLogChunk, T_POINTER, Prev, offsetof(StressLogChunk, prev))
CDAC_TYPE_FIELD(StressLogChunk, T_POINTER, Next, offsetof(StressLogChunk, next))
CDAC_TYPE_FIELD(StressLogChunk, T_ARRAY(T_UINT8), Buf, offsetof(StressLogChunk, buf))
CDAC_TYPE_FIELD(StressLogChunk, T_UINT32, Sig1, offsetof(StressLogChunk, dwSig1))
CDAC_TYPE_FIELD(StressLogChunk, T_UINT32, Sig2, offsetof(StressLogChunk, dwSig2))
CDAC_TYPE_END(StressLogChunk)

// The StressMsg Header is the fixed size portion of the StressMsg
CDAC_TYPE_BEGIN(StressMsgHeader)
CDAC_TYPE_SIZE(sizeof(StressMsg))
CDAC_TYPE_END(StressMsgHeader)

CDAC_TYPE_BEGIN(StressMsg)
CDAC_TYPE_INDETERMINATE(StressMsg)
CDAC_TYPE_FIELD(StressMsg, TYPE(StressMsgHeader), Header, 0)
CDAC_TYPE_FIELD(StressMsg, T_POINTER, Args, offsetof(StressMsg, args))
CDAC_TYPE_END(StressMsg)
#endif

CDAC_TYPE_BEGIN(MethodDesc)
CDAC_TYPE_SIZE(sizeof(MethodDesc))
CDAC_TYPE_FIELD(MethodDesc, T_UINT8, ChunkIndex, cdac_data<MethodDesc>::ChunkIndex)
CDAC_TYPE_FIELD(MethodDesc, T_UINT16, Slot, cdac_data<MethodDesc>::Slot)
CDAC_TYPE_FIELD(MethodDesc, T_UINT16, Flags, cdac_data<MethodDesc>::Flags)
CDAC_TYPE_FIELD(MethodDesc, T_UINT16, Flags3AndTokenRemainder, cdac_data<MethodDesc>::Flags3AndTokenRemainder)
CDAC_TYPE_FIELD(MethodDesc, T_UINT8, EntryPointFlags, cdac_data<MethodDesc>::EntryPointFlags)
CDAC_TYPE_FIELD(MethodDesc, T_POINTER, CodeData, cdac_data<MethodDesc>::CodeData)
#ifdef HAVE_GCCOVER
CDAC_TYPE_FIELD(MethodDesc, T_POINTER, GCCoverageInfo, offsetof(MethodDesc, m_GcCover))
#endif // HAVE_GCCOVER
CDAC_TYPE_END(MethodDesc)

CDAC_TYPE_BEGIN(MethodDescChunk)
CDAC_TYPE_SIZE(sizeof(MethodDescChunk))
CDAC_TYPE_FIELD(MethodDescChunk, T_POINTER, MethodTable, cdac_data<MethodDescChunk>::MethodTable)
CDAC_TYPE_FIELD(MethodDescChunk, T_POINTER, Next, cdac_data<MethodDescChunk>::Next)
CDAC_TYPE_FIELD(MethodDescChunk, T_UINT8, Size, cdac_data<MethodDescChunk>::Size)
CDAC_TYPE_FIELD(MethodDescChunk, T_UINT8, Count, cdac_data<MethodDescChunk>::Count)
CDAC_TYPE_FIELD(MethodDescChunk, T_UINT16, FlagsAndTokenRange, cdac_data<MethodDescChunk>::FlagsAndTokenRange)
CDAC_TYPE_END(MethodDescChunk)

CDAC_TYPE_BEGIN(NonVtableSlot)
CDAC_TYPE_SIZE(sizeof(MethodDesc::NonVtableSlot))
CDAC_TYPE_END(NonVtableSlot)

CDAC_TYPE_BEGIN(MethodImpl)
CDAC_TYPE_SIZE(sizeof(MethodImpl))
CDAC_TYPE_END(MethodImpl)

CDAC_TYPE_BEGIN(NativeCodeSlot)
CDAC_TYPE_SIZE(sizeof(MethodDesc::NativeCodeSlot))
CDAC_TYPE_END(NativeCodeSlot)

CDAC_TYPE_BEGIN(AsyncMethodData)
CDAC_TYPE_SIZE(sizeof(AsyncMethodData))
CDAC_TYPE_END(AsyncMethodData)

CDAC_TYPE_BEGIN(InstantiatedMethodDesc)
CDAC_TYPE_SIZE(sizeof(InstantiatedMethodDesc))
CDAC_TYPE_FIELD(InstantiatedMethodDesc, T_POINTER, PerInstInfo, cdac_data<InstantiatedMethodDesc>::PerInstInfo)
CDAC_TYPE_FIELD(InstantiatedMethodDesc, T_UINT16, Flags2, cdac_data<InstantiatedMethodDesc>::Flags2)
CDAC_TYPE_FIELD(InstantiatedMethodDesc, T_UINT16, NumGenericArgs, cdac_data<InstantiatedMethodDesc>::NumGenericArgs)
CDAC_TYPE_END(InstantiatedMethodDesc)

CDAC_TYPE_BEGIN(StoredSigMethodDesc)
CDAC_TYPE_INDETERMINATE(StoredSigMethodDesc)
CDAC_TYPE_FIELD(StoredSigMethodDesc, T_POINTER, Sig, cdac_data<StoredSigMethodDesc>::Sig)
CDAC_TYPE_FIELD(StoredSigMethodDesc, T_UINT32, cSig, cdac_data<StoredSigMethodDesc>::cSig)
CDAC_TYPE_FIELD(StoredSigMethodDesc, T_UINT32, ExtendedFlags, cdac_data<StoredSigMethodDesc>::ExtendedFlags)
CDAC_TYPE_END(StoredSigMethodDesc)

CDAC_TYPE_BEGIN(DynamicMethodDesc)
CDAC_TYPE_SIZE(sizeof(DynamicMethodDesc))
CDAC_TYPE_FIELD(DynamicMethodDesc, T_POINTER, MethodName, cdac_data<DynamicMethodDesc>::MethodName)
CDAC_TYPE_END(DynamicMethodDesc)

CDAC_TYPE_BEGIN(ArrayMethodDesc)
CDAC_TYPE_SIZE(sizeof(ArrayMethodDesc))
CDAC_TYPE_END(ArrayMethodDesc)

CDAC_TYPE_BEGIN(FCallMethodDesc)
CDAC_TYPE_SIZE(sizeof(FCallMethodDesc))
CDAC_TYPE_END(FCallMethodDesc)

CDAC_TYPE_BEGIN(PInvokeMethodDesc)
CDAC_TYPE_SIZE(sizeof(PInvokeMethodDesc))
CDAC_TYPE_END(PInvokeMethodDesc)

CDAC_TYPE_BEGIN(EEImplMethodDesc)
CDAC_TYPE_SIZE(sizeof(EEImplMethodDesc))
CDAC_TYPE_END(EEImplMethodDesc)

#ifdef FEATURE_COMINTEROP
CDAC_TYPE_BEGIN(CLRToCOMCallMethodDesc)
CDAC_TYPE_SIZE(sizeof(CLRToCOMCallMethodDesc))
CDAC_TYPE_END(CLRToCOMCallMethodDesc)
#endif // FEATURE_COMINTEROP

CDAC_TYPE_BEGIN(CodePointer)
CDAC_TYPE_SIZE(sizeof(PCODE))
CDAC_TYPE_END(CodePointer)

#ifdef FEATURE_PORTABLE_ENTRYPOINTS
CDAC_TYPE_BEGIN(PortableEntryPoint)
CDAC_TYPE_INDETERMINATE(PortableEntryPoint)
CDAC_TYPE_FIELD(PortableEntryPoint, T_POINTER, MethodDesc, cdac_data<PortableEntryPoint>::MethodDesc)
CDAC_TYPE_END(PortableEntryPoint)
#endif

CDAC_TYPE_BEGIN(MethodDescCodeData)
CDAC_TYPE_INDETERMINATE(MethodDescCodeData)
CDAC_TYPE_FIELD(MethodDescCodeData, TYPE(CodePointer), TemporaryEntryPoint, offsetof(MethodDescCodeData,TemporaryEntryPoint))
#ifdef FEATURE_CODE_VERSIONING
CDAC_TYPE_FIELD(MethodDescCodeData, T_POINTER, VersioningState, offsetof(MethodDescCodeData,VersioningState))
CDAC_TYPE_FIELD(MethodDescCodeData, T_UINT32, OptimizationTier, offsetof(MethodDescCodeData,OptimizationTier))
#endif // FEATURE_CODE_VERSIONING
CDAC_TYPE_END(MethodDescCodeData)

#ifdef FEATURE_CODE_VERSIONING
CDAC_TYPE_BEGIN(MethodDescVersioningState)
CDAC_TYPE_INDETERMINATE(MethodDescVersioningState)
CDAC_TYPE_FIELD(MethodDescVersioningState, T_POINTER, NativeCodeVersionNode, cdac_data<MethodDescVersioningState>::NativeCodeVersionNode)
CDAC_TYPE_FIELD(MethodDescVersioningState, T_UINT8, Flags, cdac_data<MethodDescVersioningState>::Flags)
CDAC_TYPE_END(MethodDescVersioningState)
#endif // FEATURE_CODE_VERSIONING

#ifndef FEATURE_PORTABLE_ENTRYPOINTS
CDAC_TYPE_BEGIN(PrecodeMachineDescriptor)
CDAC_TYPE_INDETERMINATE(PrecodeMachineDescriptor)
CDAC_TYPE_FIELD(PrecodeMachineDescriptor, T_UINT8, InvalidPrecodeType, offsetof(PrecodeMachineDescriptor, InvalidPrecodeType))
#ifdef HAS_PINVOKE_IMPORT_PRECODE
CDAC_TYPE_FIELD(PrecodeMachineDescriptor, T_UINT8, PInvokeImportPrecodeType, offsetof(PrecodeMachineDescriptor, PInvokeImportPrecodeType))
#endif // HAS_PINVOKE_IMPORT_PRECODE

#ifdef HAS_FIXUP_PRECODE
CDAC_TYPE_FIELD(PrecodeMachineDescriptor, T_UINT8, FixupPrecodeType, offsetof(PrecodeMachineDescriptor, FixupPrecodeType))
CDAC_TYPE_FIELD(PrecodeMachineDescriptor, T_UINT8, FixupCodeOffset, offsetof(PrecodeMachineDescriptor, FixupCodeOffset))
CDAC_TYPE_FIELD(PrecodeMachineDescriptor, T_UINT8, FixupStubPrecodeSize, offsetof(PrecodeMachineDescriptor, FixupStubPrecodeSize))
CDAC_TYPE_FIELD(PrecodeMachineDescriptor, T_ARRAY(T_UINT8), FixupBytes, offsetof(PrecodeMachineDescriptor, FixupBytes))
CDAC_TYPE_FIELD(PrecodeMachineDescriptor, T_ARRAY(T_UINT8), FixupIgnoredBytes, offsetof(PrecodeMachineDescriptor, FixupIgnoredBytes))
#endif // HAS_FIXUP_PRECODE

CDAC_TYPE_FIELD(PrecodeMachineDescriptor, T_UINT8, StubPrecodeSize, offsetof(PrecodeMachineDescriptor, StubPrecodeSize))
CDAC_TYPE_FIELD(PrecodeMachineDescriptor, T_UINT8, StubPrecodeType, offsetof(PrecodeMachineDescriptor, StubPrecodeType))

CDAC_TYPE_FIELD(PrecodeMachineDescriptor, T_ARRAY(T_UINT8), StubBytes, offsetof(PrecodeMachineDescriptor, StubBytes))
CDAC_TYPE_FIELD(PrecodeMachineDescriptor, T_ARRAY(T_UINT8), StubIgnoredBytes, offsetof(PrecodeMachineDescriptor, StubIgnoredBytes))

#ifdef HAS_THISPTR_RETBUF_PRECODE
CDAC_TYPE_FIELD(PrecodeMachineDescriptor, T_UINT8, ThisPointerRetBufPrecodeType, offsetof(PrecodeMachineDescriptor, ThisPointerRetBufPrecodeType))
#endif
#ifdef FEATURE_INTERPRETER
CDAC_TYPE_FIELD(PrecodeMachineDescriptor, T_UINT8, InterpreterPrecodeType, offsetof(PrecodeMachineDescriptor, InterpreterPrecodeType))
#endif
#ifdef FEATURE_STUBPRECODE_DYNAMIC_HELPERS
CDAC_TYPE_FIELD(PrecodeMachineDescriptor, T_UINT8, DynamicHelperPrecodeType, offsetof(PrecodeMachineDescriptor, DynamicHelperPrecodeType))
#endif
CDAC_TYPE_FIELD(PrecodeMachineDescriptor, T_UINT8, UMEntryPrecodeType, offsetof(PrecodeMachineDescriptor, UMEntryPrecodeType))
CDAC_TYPE_FIELD(PrecodeMachineDescriptor, T_UINT32, StubCodePageSize, offsetof(PrecodeMachineDescriptor, StubCodePageSize))
CDAC_TYPE_END(PrecodeMachineDescriptor)
#endif // !FEATURE_PORTABLE_ENTRYPOINTS

CDAC_TYPE_BEGIN(PlatformMetadata)
CDAC_TYPE_INDETERMINATE(PlatformMetadata)
#ifndef FEATURE_PORTABLE_ENTRYPOINTS
CDAC_TYPE_FIELD(PlatformMetadata, TYPE(PrecodeMachineDescriptor), PrecodeMachineDescriptor, offsetof(CDacPlatformMetadata, precode))
#endif // !FEATURE_PORTABLE_ENTRYPOINTS
CDAC_TYPE_FIELD(PlatformMetadata, T_UINT8, CodePointerFlags, offsetof(CDacPlatformMetadata, codePointerFlags))
CDAC_TYPE_END(PlatformMetadata)

#ifndef FEATURE_PORTABLE_ENTRYPOINTS
CDAC_TYPE_BEGIN(StubPrecodeData)
CDAC_TYPE_INDETERMINATE(StubPrecodeData)
CDAC_TYPE_FIELD(StubPrecodeData, T_POINTER, SecretParam, offsetof(StubPrecodeData, SecretParam))
CDAC_TYPE_FIELD(StubPrecodeData, T_UINT8, Type, offsetof(StubPrecodeData, Type))
CDAC_TYPE_END(StubPrecodeData)

#ifdef HAS_THISPTR_RETBUF_PRECODE
CDAC_TYPE_BEGIN(ThisPtrRetBufPrecodeData)
CDAC_TYPE_INDETERMINATE(ThisPtrRetBufPrecodeData)
CDAC_TYPE_FIELD(ThisPtrRetBufPrecodeData, T_POINTER, MethodDesc, offsetof(ThisPtrRetBufPrecodeData, MethodDesc))
CDAC_TYPE_END(ThisPtrRetBufPrecodeData)
#endif

CDAC_TYPE_BEGIN(FixupPrecodeData)
CDAC_TYPE_INDETERMINATE(FixupPrecodeData)
CDAC_TYPE_FIELD(FixupPrecodeData, T_POINTER, MethodDesc, offsetof(FixupPrecodeData, MethodDesc))
CDAC_TYPE_END(FixupPrecodeData)
#endif // !FEATURE_PORTABLE_ENTRYPOINTS

CDAC_TYPE_BEGIN(ReadyToRunInfo)
CDAC_TYPE_INDETERMINATE(ReadyToRunInfo)
CDAC_TYPE_FIELD(ReadyToRunInfo, T_POINTER, ReadyToRunHeader, cdac_data<ReadyToRunInfo>::ReadyToRunHeader)
CDAC_TYPE_FIELD(ReadyToRunInfo, T_POINTER, CompositeInfo, cdac_data<ReadyToRunInfo>::CompositeInfo)
CDAC_TYPE_FIELD(ReadyToRunInfo, T_UINT32, NumRuntimeFunctions, cdac_data<ReadyToRunInfo>::NumRuntimeFunctions)
CDAC_TYPE_FIELD(ReadyToRunInfo, T_POINTER, RuntimeFunctions, cdac_data<ReadyToRunInfo>::RuntimeFunctions)
CDAC_TYPE_FIELD(ReadyToRunInfo, T_UINT32, NumHotColdMap, cdac_data<ReadyToRunInfo>::NumHotColdMap)
CDAC_TYPE_FIELD(ReadyToRunInfo, T_POINTER, HotColdMap, cdac_data<ReadyToRunInfo>::HotColdMap)
CDAC_TYPE_FIELD(ReadyToRunInfo, T_POINTER, DelayLoadMethodCallThunks, cdac_data<ReadyToRunInfo>::DelayLoadMethodCallThunks)
CDAC_TYPE_FIELD(ReadyToRunInfo, T_POINTER, DebugInfoSection, cdac_data<ReadyToRunInfo>::DebugInfoSection)
CDAC_TYPE_FIELD(ReadyToRunInfo, T_POINTER, ExceptionInfoSection, cdac_data<ReadyToRunInfo>::ExceptionInfoSection)
CDAC_TYPE_FIELD(ReadyToRunInfo, TYPE(HashMap), EntryPointToMethodDescMap, cdac_data<ReadyToRunInfo>::EntryPointToMethodDescMap)
CDAC_TYPE_FIELD(ReadyToRunInfo, T_POINTER, LoadedImageBase, cdac_data<ReadyToRunInfo>::LoadedImageBase)
CDAC_TYPE_FIELD(ReadyToRunInfo, T_POINTER, Composite, cdac_data<ReadyToRunInfo>::Composite)
CDAC_TYPE_END(ReadyToRunInfo)

CDAC_TYPE_BEGIN(ReadyToRunHeader)
CDAC_TYPE_INDETERMINATE(ReadyToRunHeader)
CDAC_TYPE_FIELD(ReadyToRunHeader, T_UINT16, MajorVersion, offsetof(READYTORUN_HEADER, MajorVersion))
CDAC_TYPE_FIELD(ReadyToRunHeader, T_UINT16, MinorVersion, offsetof(READYTORUN_HEADER, MinorVersion))
CDAC_TYPE_END(ReadyToRunHeader)

CDAC_TYPE_BEGIN(ReadyToRunCoreHeader)
CDAC_TYPE_SIZE(sizeof(READYTORUN_CORE_HEADER))
CDAC_TYPE_FIELD(ReadyToRunCoreHeader, T_UINT32, Flags, offsetof(READYTORUN_CORE_HEADER, Flags))
CDAC_TYPE_FIELD(ReadyToRunCoreHeader, T_UINT32, NumberOfSections, offsetof(READYTORUN_CORE_HEADER, NumberOfSections))
CDAC_TYPE_END(ReadyToRunCoreHeader)

CDAC_TYPE_BEGIN(ReadyToRunCoreInfo)
CDAC_TYPE_INDETERMINATE(ReadyToRunCoreInfo)
CDAC_TYPE_FIELD(ReadyToRunCoreInfo, T_POINTER, Header, cdac_data<ReadyToRunCoreInfo>::Header)
CDAC_TYPE_END(ReadyToRunCoreInfo)

CDAC_TYPE_BEGIN(ReadyToRunSection)
CDAC_TYPE_SIZE(sizeof(READYTORUN_SECTION))
CDAC_TYPE_FIELD(ReadyToRunSection, T_UINT32, Type, offsetof(READYTORUN_SECTION, Type))
CDAC_TYPE_FIELD(ReadyToRunSection, TYPE(ImageDataDirectory), Section, offsetof(READYTORUN_SECTION, Section))
CDAC_TYPE_END(ReadyToRunSection)

CDAC_TYPE_BEGIN(ExceptionLookupTableEntry)
CDAC_TYPE_SIZE(sizeof(CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY))
CDAC_TYPE_FIELD(ExceptionLookupTableEntry, T_UINT32, MethodStartRVA, offsetof(CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY, MethodStartRVA))
CDAC_TYPE_FIELD(ExceptionLookupTableEntry, T_UINT32, ExceptionInfoRVA, offsetof(CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY, ExceptionInfoRVA))
CDAC_TYPE_END(ExceptionLookupTableEntry)

CDAC_TYPE_BEGIN(ImageDataDirectory)
CDAC_TYPE_SIZE(sizeof(IMAGE_DATA_DIRECTORY))
CDAC_TYPE_FIELD(ImageDataDirectory, T_UINT32, VirtualAddress, offsetof(IMAGE_DATA_DIRECTORY, VirtualAddress))
CDAC_TYPE_FIELD(ImageDataDirectory, T_UINT32, Size, offsetof(IMAGE_DATA_DIRECTORY, Size))
CDAC_TYPE_END(ImageDataDirectory)

CDAC_TYPE_BEGIN(RuntimeFunction)
CDAC_TYPE_SIZE(sizeof(RUNTIME_FUNCTION))
CDAC_TYPE_FIELD(RuntimeFunction, T_UINT32, BeginAddress, offsetof(RUNTIME_FUNCTION, BeginAddress))
#ifdef TARGET_AMD64
CDAC_TYPE_FIELD(RuntimeFunction, T_UINT32, EndAddress, offsetof(RUNTIME_FUNCTION, EndAddress))
#endif
CDAC_TYPE_FIELD(RuntimeFunction, T_UINT32, UnwindData, offsetof(RUNTIME_FUNCTION, UnwindData))
CDAC_TYPE_END(RuntimeFunction)

CDAC_TYPE_BEGIN(UnwindInfo)
CDAC_TYPE_INDETERMINATE(UnwindInfo)
#ifdef TARGET_X86
CDAC_TYPE_FIELD(UnwindInfo, T_UINT32, FunctionLength, offsetof(UNWIND_INFO, FunctionLength))
#endif
CDAC_TYPE_END(UnwindInfo)

CDAC_TYPE_BEGIN(HashMap)
CDAC_TYPE_INDETERMINATE(HashMap)
CDAC_TYPE_FIELD(HashMap, T_POINTER, Buckets, cdac_data<HashMap>::Buckets)
CDAC_TYPE_END(HashMap)

CDAC_TYPE_BEGIN(Bucket)
CDAC_TYPE_SIZE(sizeof(Bucket))
CDAC_TYPE_FIELD(Bucket, T_POINTER, Keys, offsetof(Bucket, m_rgKeys))
CDAC_TYPE_FIELD(Bucket, T_POINTER, Values, offsetof(Bucket, m_rgValues))
CDAC_TYPE_END(Bucket)

CDAC_TYPE_BEGIN(RangeSectionMap)
CDAC_TYPE_INDETERMINATE(RangeSectionMap)
CDAC_TYPE_FIELD(RangeSectionMap, T_POINTER, TopLevelData, cdac_data<RangeSectionMap>::TopLevelData)
CDAC_TYPE_END(RangeSectionMap)

CDAC_TYPE_BEGIN(RangeSectionFragment)
CDAC_TYPE_INDETERMINATE(RangeSectionFragment)
CDAC_TYPE_FIELD(RangeSectionFragment, T_POINTER, RangeBegin, cdac_data<RangeSectionMap>::RangeSectionFragment::RangeBegin)
CDAC_TYPE_FIELD(RangeSectionFragment, T_POINTER, RangeEndOpen, cdac_data<RangeSectionMap>::RangeSectionFragment::RangeEndOpen)
CDAC_TYPE_FIELD(RangeSectionFragment, T_POINTER, RangeSection, cdac_data<RangeSectionMap>::RangeSectionFragment::RangeSection)
CDAC_TYPE_FIELD(RangeSectionFragment, T_POINTER, Next, cdac_data<RangeSectionMap>::RangeSectionFragment::Next)
CDAC_TYPE_END(RangeSectionFragment)

CDAC_TYPE_BEGIN(RangeSection)
CDAC_TYPE_INDETERMINATE(RangeSection)
CDAC_TYPE_FIELD(RangeSection, T_POINTER, RangeBegin, cdac_data<RangeSection>::RangeBegin)
CDAC_TYPE_FIELD(RangeSection, T_POINTER, RangeEndOpen, cdac_data<RangeSection>::RangeEndOpen)
CDAC_TYPE_FIELD(RangeSection, T_POINTER, NextForDelete, cdac_data<RangeSection>::NextForDelete)
CDAC_TYPE_FIELD(RangeSection, T_POINTER, JitManager, cdac_data<RangeSection>::JitManager)
CDAC_TYPE_FIELD(RangeSection, T_INT32, Flags, cdac_data<RangeSection>::Flags)
CDAC_TYPE_FIELD(RangeSection, T_POINTER, HeapList, cdac_data<RangeSection>::HeapList)
CDAC_TYPE_FIELD(RangeSection, T_POINTER, R2RModule, cdac_data<RangeSection>::R2RModule)
CDAC_TYPE_END(RangeSection)

CDAC_TYPE_BEGIN(EEJitManager)
CDAC_TYPE_INDETERMINATE(EEJitManager)
CDAC_TYPE_FIELD(EEJitManager, T_BOOL, StoreRichDebugInfo, cdac_data<EEJitManager>::StoreRichDebugInfo)
CDAC_TYPE_FIELD(EEJitManager, T_POINTER, AllCodeHeaps, cdac_data<EEJitManager>::AllCodeHeaps)
CDAC_TYPE_END(EEJitManager)

CDAC_TYPE_BEGIN(RealCodeHeader)
CDAC_TYPE_INDETERMINATE(RealCodeHeader)
CDAC_TYPE_FIELD(RealCodeHeader, T_POINTER, MethodDesc, offsetof(RealCodeHeader, phdrMDesc))
CDAC_TYPE_FIELD(RealCodeHeader, T_POINTER, DebugInfo, offsetof(RealCodeHeader, phdrDebugInfo))
CDAC_TYPE_FIELD(RealCodeHeader, T_POINTER, EHInfo, offsetof(RealCodeHeader, phdrJitEHInfo))
CDAC_TYPE_FIELD(RealCodeHeader, T_POINTER, GCInfo, offsetof(RealCodeHeader, phdrJitGCInfo))
CDAC_TYPE_FIELD(RealCodeHeader, T_UINT32, NumUnwindInfos, offsetof(RealCodeHeader, nUnwindInfos))
CDAC_TYPE_FIELD(RealCodeHeader, TYPE(RuntimeFunction), UnwindInfos, offsetof(RealCodeHeader, unwindInfos))
CDAC_TYPE_END(RealCodeHeader)

CDAC_TYPE_BEGIN(EEExceptionClause)
CDAC_TYPE_SIZE(sizeof(EE_ILEXCEPTION_CLAUSE))
CDAC_TYPE_FIELD(EEExceptionClause, T_UINT32, Flags, offsetof(EE_ILEXCEPTION_CLAUSE, Flags))
CDAC_TYPE_FIELD(EEExceptionClause, T_UINT32, TryStartPC, offsetof(EE_ILEXCEPTION_CLAUSE, TryStartPC))
CDAC_TYPE_FIELD(EEExceptionClause, T_UINT32, TryEndPC, offsetof(EE_ILEXCEPTION_CLAUSE, TryEndPC))
CDAC_TYPE_FIELD(EEExceptionClause, T_UINT32, HandlerStartPC, offsetof(EE_ILEXCEPTION_CLAUSE, HandlerStartPC))
CDAC_TYPE_FIELD(EEExceptionClause, T_UINT32, HandlerEndPC, offsetof(EE_ILEXCEPTION_CLAUSE, HandlerEndPC))
CDAC_TYPE_FIELD(EEExceptionClause, T_NUINT, TypeHandle, offsetof(EE_ILEXCEPTION_CLAUSE, TypeHandle))
CDAC_TYPE_END(EEExceptionClause)

CDAC_TYPE_BEGIN(R2RExceptionClause)
CDAC_TYPE_SIZE(sizeof(CORCOMPILE_EXCEPTION_CLAUSE))
CDAC_TYPE_FIELD(R2RExceptionClause, T_UINT32, Flags, offsetof(CORCOMPILE_EXCEPTION_CLAUSE, Flags))
CDAC_TYPE_FIELD(R2RExceptionClause, T_UINT32, TryStartPC, offsetof(CORCOMPILE_EXCEPTION_CLAUSE, TryStartPC))
CDAC_TYPE_FIELD(R2RExceptionClause, T_UINT32, TryEndPC, offsetof(CORCOMPILE_EXCEPTION_CLAUSE, TryEndPC))
CDAC_TYPE_FIELD(R2RExceptionClause, T_UINT32, HandlerStartPC, offsetof(CORCOMPILE_EXCEPTION_CLAUSE, HandlerStartPC))
CDAC_TYPE_FIELD(R2RExceptionClause, T_UINT32, HandlerEndPC, offsetof(CORCOMPILE_EXCEPTION_CLAUSE, HandlerEndPC))
CDAC_TYPE_FIELD(R2RExceptionClause, T_UINT32, ClassToken, offsetof(CORCOMPILE_EXCEPTION_CLAUSE, ClassToken))
CDAC_TYPE_END(R2RExceptionClause)

CDAC_TYPE_BEGIN(EEILException)
CDAC_TYPE_INDETERMINATE(EEILException)
CDAC_TYPE_FIELD(EEILException, TYPE(EEExceptionClause), Clauses, offsetof(EE_ILEXCEPTION, Clauses))
CDAC_TYPE_END(EEILException)

CDAC_TYPE_BEGIN(PatchpointInfo)
CDAC_TYPE_SIZE(sizeof(PatchpointInfo))
CDAC_TYPE_FIELD(PatchpointInfo, T_UINT32, LocalCount, cdac_data<PatchpointInfo>::LocalCount)
CDAC_TYPE_END(PatchpointInfo)

CDAC_TYPE_BEGIN(CodeHeapListNode)
CDAC_TYPE_FIELD(CodeHeapListNode, T_POINTER, Next, offsetof(HeapList, hpNext))
CDAC_TYPE_FIELD(CodeHeapListNode, T_POINTER, StartAddress, offsetof(HeapList, startAddress))
CDAC_TYPE_FIELD(CodeHeapListNode, T_POINTER, EndAddress, offsetof(HeapList, endAddress))
CDAC_TYPE_FIELD(CodeHeapListNode, T_POINTER, MapBase, offsetof(HeapList, mapBase))
CDAC_TYPE_FIELD(CodeHeapListNode, T_POINTER, HeaderMap, offsetof(HeapList, pHdrMap))
CDAC_TYPE_FIELD(CodeHeapListNode, T_POINTER, Heap, offsetof(HeapList, pHeap))
CDAC_TYPE_END(CodeHeapListNode)

CDAC_TYPE_BEGIN(CodeHeap)
CDAC_TYPE_INDETERMINATE(CodeHeap)
CDAC_TYPE_FIELD(CodeHeap, T_UINT8, HeapType, cdac_data<CodeHeap>::HeapType)
CDAC_TYPE_END(CodeHeap)

CDAC_TYPE_BEGIN(LoaderCodeHeap)
CDAC_TYPE_INDETERMINATE(LoaderCodeHeap)
CDAC_TYPE_FIELD(LoaderCodeHeap, T_POINTER, LoaderHeap, cdac_data<LoaderCodeHeap>::LoaderHeap)
CDAC_TYPE_END(LoaderCodeHeap)

CDAC_TYPE_BEGIN(HostCodeHeap)
CDAC_TYPE_INDETERMINATE(HostCodeHeap)
CDAC_TYPE_FIELD(HostCodeHeap, T_POINTER, BaseAddress, cdac_data<HostCodeHeap>::BaseAddress)
CDAC_TYPE_FIELD(HostCodeHeap, T_POINTER, CurrentAddress, cdac_data<HostCodeHeap>::CurrentAddress)
CDAC_TYPE_END(HostCodeHeap)

#ifdef FEATURE_CODE_VERSIONING
CDAC_TYPE_BEGIN(ILCodeVersioningState)
CDAC_TYPE_INDETERMINATE(ILCodeVersioningState)
CDAC_TYPE_FIELD(ILCodeVersioningState, T_POINTER, FirstVersionNode, cdac_data<ILCodeVersioningState>::FirstVersionNode)
CDAC_TYPE_FIELD(ILCodeVersioningState, T_UINT32, ActiveVersionKind, cdac_data<ILCodeVersioningState>::ActiveVersionKind)
CDAC_TYPE_FIELD(ILCodeVersioningState, T_POINTER, ActiveVersionNode, cdac_data<ILCodeVersioningState>::ActiveVersionNode)
CDAC_TYPE_FIELD(ILCodeVersioningState, T_POINTER, ActiveVersionModule, cdac_data<ILCodeVersioningState>::ActiveVersionModule)
CDAC_TYPE_FIELD(ILCodeVersioningState, T_UINT32, ActiveVersionMethodDef, cdac_data<ILCodeVersioningState>::ActiveVersionMethodDef)
CDAC_TYPE_END(ILCodeVersioningState)

CDAC_TYPE_BEGIN(NativeCodeVersionNode)
CDAC_TYPE_INDETERMINATE(NativeCodeVersionNode)
CDAC_TYPE_FIELD(NativeCodeVersionNode, T_POINTER, Next, cdac_data<NativeCodeVersionNode>::Next)
CDAC_TYPE_FIELD(NativeCodeVersionNode, T_POINTER, MethodDesc, cdac_data<NativeCodeVersionNode>::MethodDesc)
CDAC_TYPE_FIELD(NativeCodeVersionNode, TYPE(CodePointer), NativeCode, cdac_data<NativeCodeVersionNode>::NativeCode)
CDAC_TYPE_FIELD(NativeCodeVersionNode, T_UINT32, Flags, cdac_data<NativeCodeVersionNode>::Flags)
CDAC_TYPE_FIELD(NativeCodeVersionNode, T_NUINT, ILVersionId, cdac_data<NativeCodeVersionNode>::ILVersionId)
#ifdef HAVE_GCCOVER
CDAC_TYPE_FIELD(NativeCodeVersionNode, T_POINTER, GCCoverageInfo, cdac_data<NativeCodeVersionNode>::GCCoverageInfo)
#endif // HAVE_GCCOVER
#ifdef FEATURE_TIERED_COMPILATION
CDAC_TYPE_FIELD(NativeCodeVersionNode, T_UINT32, OptimizationTier, cdac_data<NativeCodeVersionNode>::OptimizationTier)
#endif // FEATURE_TIERED_COMPILATION
CDAC_TYPE_END(NativeCodeVersionNode)

CDAC_TYPE_BEGIN(ILCodeVersionNode)
CDAC_TYPE_INDETERMINATE(ILCodeVersionNode)
CDAC_TYPE_FIELD(ILCodeVersionNode, T_NUINT, VersionId, cdac_data<ILCodeVersionNode>::VersionId)
CDAC_TYPE_FIELD(ILCodeVersionNode, T_POINTER, Next, cdac_data<ILCodeVersionNode>::Next)
CDAC_TYPE_FIELD(ILCodeVersionNode, T_UINT32, RejitState, cdac_data<ILCodeVersionNode>::RejitState)
CDAC_TYPE_FIELD(ILCodeVersionNode, T_POINTER, ILAddress, cdac_data<ILCodeVersionNode>::ILAddress)
CDAC_TYPE_END(ILCodeVersionNode)
#endif // FEATURE_CODE_VERSIONING

#ifdef PROFILING_SUPPORTED
CDAC_TYPE_BEGIN(ProfControlBlock)
CDAC_TYPE_FIELD(ProfControlBlock, T_UINT64, GlobalEventMask, offsetof(ProfControlBlock, globalEventMask))
CDAC_TYPE_FIELD(ProfControlBlock, T_BOOL, RejitOnAttachEnabled, offsetof(ProfControlBlock, fRejitOnAttachEnabled))
CDAC_TYPE_END(ProfControlBlock)
#endif // PROFILING_SUPPORTED

#ifdef HAVE_GCCOVER
CDAC_TYPE_BEGIN(GCCoverageInfo)
CDAC_TYPE_INDETERMINATE(GCCoverageInfo)
CDAC_TYPE_FIELD(GCCoverageInfo, T_POINTER, SavedCode, offsetof(GCCoverageInfo, savedCode))
CDAC_TYPE_END(GCCoverageInfo)
#endif // HAVE_GCCOVER

CDAC_TYPE_BEGIN(Frame)
CDAC_TYPE_INDETERMINATE(Frame)
CDAC_TYPE_FIELD(Frame, T_POINTER, Next, cdac_data<Frame>::Next)
CDAC_TYPE_END(Frame)

CDAC_TYPE_BEGIN(InlinedCallFrame)
CDAC_TYPE_SIZE(sizeof(InlinedCallFrame))
CDAC_TYPE_FIELD(InlinedCallFrame, T_POINTER, CallSiteSP, offsetof(InlinedCallFrame, m_pCallSiteSP))
CDAC_TYPE_FIELD(InlinedCallFrame, T_POINTER, CallerReturnAddress, offsetof(InlinedCallFrame, m_pCallerReturnAddress))
CDAC_TYPE_FIELD(InlinedCallFrame, T_POINTER, CalleeSavedFP, offsetof(InlinedCallFrame, m_pCalleeSavedFP))
CDAC_TYPE_FIELD(InlinedCallFrame, T_POINTER, Datum, offsetof(InlinedCallFrame, m_Datum))
#ifdef TARGET_ARM
CDAC_TYPE_FIELD(InlinedCallFrame, T_POINTER, SPAfterProlog, offsetof(InlinedCallFrame, m_pSPAfterProlog))
#endif // TARGET_ARM
CDAC_TYPE_END(InlinedCallFrame)

CDAC_TYPE_BEGIN(SoftwareExceptionFrame)
CDAC_TYPE_SIZE(sizeof(SoftwareExceptionFrame))
CDAC_TYPE_FIELD(SoftwareExceptionFrame, EXTERN_TYPE(Context), TargetContext, cdac_data<SoftwareExceptionFrame>::TargetContext)
CDAC_TYPE_FIELD(SoftwareExceptionFrame, T_POINTER, ReturnAddress, cdac_data<SoftwareExceptionFrame>::ReturnAddress)
CDAC_TYPE_END(SoftwareExceptionFrame)

CDAC_TYPE_BEGIN(FramedMethodFrame)
CDAC_TYPE_SIZE(sizeof(FramedMethodFrame))
CDAC_TYPE_FIELD(FramedMethodFrame, T_POINTER, TransitionBlockPtr, cdac_data<FramedMethodFrame>::TransitionBlockPtr)
CDAC_TYPE_FIELD(FramedMethodFrame, T_POINTER, MethodDescPtr, cdac_data<FramedMethodFrame>::MethodDescPtr)
CDAC_TYPE_END(FramedMethodFrame)

CDAC_TYPE_BEGIN(TransitionBlock)
CDAC_TYPE_SIZE(sizeof(TransitionBlock))
CDAC_TYPE_FIELD(TransitionBlock, T_POINTER, ReturnAddress, offsetof(TransitionBlock, m_ReturnAddress))
CDAC_TYPE_FIELD(TransitionBlock, TYPE(CalleeSavedRegisters), CalleeSavedRegisters, offsetof(TransitionBlock, m_calleeSavedRegisters))
#ifdef TARGET_ARM
CDAC_TYPE_FIELD(TransitionBlock, TYPE(ArgumentRegisters), ArgumentRegisters, offsetof(TransitionBlock, m_argumentRegisters))
#endif // TARGET_ARM
CDAC_TYPE_END(TransitionBlock)

#ifdef DEBUGGING_SUPPORTED
CDAC_TYPE_BEGIN(FuncEvalFrame)
CDAC_TYPE_SIZE(sizeof(FuncEvalFrame))
CDAC_TYPE_FIELD(FuncEvalFrame, T_POINTER, DebuggerEvalPtr, cdac_data<FuncEvalFrame>::DebuggerEvalPtr)
CDAC_TYPE_END(FuncEvalFrame)

CDAC_TYPE_BEGIN(DebuggerEval)
CDAC_TYPE_SIZE(sizeof(DebuggerEval))
CDAC_TYPE_FIELD(DebuggerEval, EXTERN_TYPE(Context), TargetContext, offsetof(DebuggerEval, m_context))
CDAC_TYPE_FIELD(DebuggerEval, T_BOOL, EvalDuringException, offsetof(DebuggerEval, m_evalDuringException))
CDAC_TYPE_END(DebuggerEval)
#endif // DEBUGGING_SUPPORTED

CDAC_TYPE_BEGIN(StubDispatchFrame)
CDAC_TYPE_SIZE(sizeof(StubDispatchFrame))
CDAC_TYPE_FIELD(StubDispatchFrame, T_POINTER, RepresentativeMTPtr, cdac_data<StubDispatchFrame>::RepresentativeMTPtr)
CDAC_TYPE_FIELD(StubDispatchFrame, T_POINTER, MethodDescPtr, cdac_data<FramedMethodFrame>::MethodDescPtr)
CDAC_TYPE_FIELD(StubDispatchFrame, T_UINT32, RepresentativeSlot, cdac_data<StubDispatchFrame>::RepresentativeSlot)
CDAC_TYPE_END(StubDispatchFrame)

#ifdef FEATURE_HIJACK
CDAC_TYPE_BEGIN(ResumableFrame)
CDAC_TYPE_SIZE(sizeof(ResumableFrame))
CDAC_TYPE_FIELD(ResumableFrame, T_POINTER, TargetContextPtr, cdac_data<ResumableFrame>::TargetContextPtr)
CDAC_TYPE_END(ResumableFrame)

CDAC_TYPE_BEGIN(HijackFrame)
CDAC_TYPE_SIZE(sizeof(HijackFrame))
CDAC_TYPE_FIELD(HijackFrame, T_POINTER, ReturnAddress, cdac_data<HijackFrame>::ReturnAddress)
CDAC_TYPE_FIELD(HijackFrame, T_POINTER, HijackArgsPtr, cdac_data<HijackFrame>::HijackArgsPtr)
CDAC_TYPE_END(HijackFrame)

// HijackArgs struct is different on each platform
CDAC_TYPE_BEGIN(HijackArgs)
CDAC_TYPE_SIZE(sizeof(HijackArgs))
#if defined(TARGET_AMD64)

CDAC_TYPE_FIELD(HijackArgs, TYPE(CalleeSavedRegisters), CalleeSavedRegisters, offsetof(HijackArgs, Regs))
#ifdef TARGET_WINDOWS
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, Rsp, offsetof(HijackArgs, Rsp))
#endif // TARGET_WINDOWS

#elif defined(TARGET_ARM64)

CDAC_TYPE_FIELD(HijackArgs, T_POINTER, X0, offsetof(HijackArgs, X0))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, X1, offsetof(HijackArgs, X1))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, X19, offsetof(HijackArgs, X19))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, X20, offsetof(HijackArgs, X20))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, X21, offsetof(HijackArgs, X21))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, X22, offsetof(HijackArgs, X22))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, X23, offsetof(HijackArgs, X23))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, X24, offsetof(HijackArgs, X24))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, X25, offsetof(HijackArgs, X25))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, X26, offsetof(HijackArgs, X26))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, X27, offsetof(HijackArgs, X27))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, X28, offsetof(HijackArgs, X28))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, Fp, offsetof(HijackArgs, X29))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, Lr, offsetof(HijackArgs, Lr))

#elif defined(TARGET_X86)

CDAC_TYPE_FIELD(HijackArgs, T_POINTER, Edi, offsetof(HijackArgs, Edi))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, Esi, offsetof(HijackArgs, Esi))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, Ebx, offsetof(HijackArgs, Ebx))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, Edx, offsetof(HijackArgs, Edx))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, Ecx, offsetof(HijackArgs, Ecx))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, Eax, offsetof(HijackArgs, Eax))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, Ebp, offsetof(HijackArgs, Ebp))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, Eip, offsetof(HijackArgs, Eip))

#elif defined(TARGET_ARM)

CDAC_TYPE_FIELD(HijackArgs, T_POINTER, R0, offsetof(HijackArgs, R0))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, R2, offsetof(HijackArgs, R2))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, R4, offsetof(HijackArgs, R4))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, R5, offsetof(HijackArgs, R5))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, R6, offsetof(HijackArgs, R6))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, R7, offsetof(HijackArgs, R7))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, R8, offsetof(HijackArgs, R8))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, R9, offsetof(HijackArgs, R9))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, R10, offsetof(HijackArgs, R10))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, R11, offsetof(HijackArgs, R11))

#elif defined(TARGET_RISCV64)

CDAC_TYPE_FIELD(HijackArgs, T_POINTER, Fp, offsetof(HijackArgs, Fp))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, Ra, offsetof(HijackArgs, Ra))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S1, offsetof(HijackArgs, S1))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S2, offsetof(HijackArgs, S2))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S3, offsetof(HijackArgs, S3))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S4, offsetof(HijackArgs, S4))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S5, offsetof(HijackArgs, S5))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S6, offsetof(HijackArgs, S6))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S7, offsetof(HijackArgs, S7))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S8, offsetof(HijackArgs, S8))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S9, offsetof(HijackArgs, S9))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S10, offsetof(HijackArgs, S10))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S11, offsetof(HijackArgs, S11))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, Gp, offsetof(HijackArgs, Gp))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, Tp, offsetof(HijackArgs, Tp))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, A0, offsetof(HijackArgs, A0))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, A1, offsetof(HijackArgs, A1))

#elif defined(TARGET_LOONGARCH64)

CDAC_TYPE_FIELD(HijackArgs, T_POINTER, Fp, offsetof(HijackArgs, Fp))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, Ra, offsetof(HijackArgs, Ra))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S0, offsetof(HijackArgs, S0))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S1, offsetof(HijackArgs, S1))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S2, offsetof(HijackArgs, S2))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S3, offsetof(HijackArgs, S3))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S4, offsetof(HijackArgs, S4))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S5, offsetof(HijackArgs, S5))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S6, offsetof(HijackArgs, S6))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S7, offsetof(HijackArgs, S7))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, S8, offsetof(HijackArgs, S8))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, A0, offsetof(HijackArgs, A0))
CDAC_TYPE_FIELD(HijackArgs, T_POINTER, A1, offsetof(HijackArgs, A1))

#endif // Platform switch
CDAC_TYPE_END(HijackArgs)
#endif // FEATURE_HIJACK

CDAC_TYPE_BEGIN(FaultingExceptionFrame)
CDAC_TYPE_SIZE(sizeof(FaultingExceptionFrame))
CDAC_TYPE_FIELD(FaultingExceptionFrame, EXTERN_TYPE(Context), TargetContext, cdac_data<FaultingExceptionFrame>::TargetContext)
CDAC_TYPE_END(FaultingExceptionFrame)

#if defined(TARGET_X86) && !defined(UNIX_X86_ABI)
CDAC_TYPE_BEGIN(TailCallFrame)
CDAC_TYPE_SIZE(sizeof(TailCallFrame))
CDAC_TYPE_FIELD(TailCallFrame, TYPE(CalleeSavedRegisters), CalleeSavedRegisters, cdac_data<TailCallFrame>::CalleeSavedRegisters)
CDAC_TYPE_FIELD(TailCallFrame, T_POINTER, ReturnAddress, cdac_data<TailCallFrame>::ReturnAddress)
CDAC_TYPE_END(TailCallFrame)
#endif // TARGET_X86 && !UNIX_X86_ABI

// ArgumentRegisters struct is different on each platform
CDAC_TYPE_BEGIN(ArgumentRegisters)
CDAC_TYPE_SIZE(sizeof(ArgumentRegisters))
#if defined(TARGET_ARM)

CDAC_TYPE_FIELD(ArgumentRegisters, T_NUINT, R0, offsetof(ArgumentRegisters, r[0]))
CDAC_TYPE_FIELD(ArgumentRegisters, T_NUINT, R1, offsetof(ArgumentRegisters, r[1]))
CDAC_TYPE_FIELD(ArgumentRegisters, T_NUINT, R2, offsetof(ArgumentRegisters, r[2]))
CDAC_TYPE_FIELD(ArgumentRegisters, T_NUINT, R3, offsetof(ArgumentRegisters, r[3]))

#endif // TARGET_ARM
CDAC_TYPE_END(ArgumentRegisters)

// CalleeSavedRegisters struct is different on each platform
CDAC_TYPE_BEGIN(CalleeSavedRegisters)
CDAC_TYPE_SIZE(sizeof(CalleeSavedRegisters))
#if defined(TARGET_AMD64) || defined(TARGET_X86)

#define CALLEE_SAVED_REGISTER(regname) \
    CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, regname, offsetof(CalleeSavedRegisters, regname))
ENUM_CALLEE_SAVED_REGISTERS()
#undef CALLEE_SAVED_REGISTER

#elif defined(TARGET_ARM)

CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, R4, offsetof(CalleeSavedRegisters, r4))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, R5, offsetof(CalleeSavedRegisters, r5))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, R6, offsetof(CalleeSavedRegisters, r6))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, R7, offsetof(CalleeSavedRegisters, r7))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, R8, offsetof(CalleeSavedRegisters, r8))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, R9, offsetof(CalleeSavedRegisters, r9))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, R10, offsetof(CalleeSavedRegisters, r10))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, R11, offsetof(CalleeSavedRegisters, r11))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, Lr, offsetof(CalleeSavedRegisters, r14))

#elif defined(TARGET_ARM64)

CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, X19, offsetof(CalleeSavedRegisters, x19))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, X20, offsetof(CalleeSavedRegisters, x20))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, X21, offsetof(CalleeSavedRegisters, x21))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, X22, offsetof(CalleeSavedRegisters, x22))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, X23, offsetof(CalleeSavedRegisters, x23))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, X24, offsetof(CalleeSavedRegisters, x24))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, X25, offsetof(CalleeSavedRegisters, x25))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, X26, offsetof(CalleeSavedRegisters, x26))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, X27, offsetof(CalleeSavedRegisters, x27))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, X28, offsetof(CalleeSavedRegisters, x28))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, Fp, offsetof(CalleeSavedRegisters, x29))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, Lr, offsetof(CalleeSavedRegisters, x30))

#elif defined(TARGET_RISCV64)

CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, Fp, offsetof(CalleeSavedRegisters, fp))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, Ra, offsetof(CalleeSavedRegisters, ra))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S1, offsetof(CalleeSavedRegisters, s1))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S2, offsetof(CalleeSavedRegisters, s2))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S3, offsetof(CalleeSavedRegisters, s3))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S4, offsetof(CalleeSavedRegisters, s4))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S5, offsetof(CalleeSavedRegisters, s5))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S6, offsetof(CalleeSavedRegisters, s6))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S7, offsetof(CalleeSavedRegisters, s7))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S8, offsetof(CalleeSavedRegisters, s8))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S9, offsetof(CalleeSavedRegisters, s9))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S10, offsetof(CalleeSavedRegisters, s10))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S11, offsetof(CalleeSavedRegisters, s11))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, Tp, offsetof(CalleeSavedRegisters, tp))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, Gp, offsetof(CalleeSavedRegisters, gp))

#elif defined(TARGET_LOONGARCH64)

CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, Fp, offsetof(CalleeSavedRegisters, fp))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, Ra, offsetof(CalleeSavedRegisters, ra))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S0, offsetof(CalleeSavedRegisters, s0))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S1, offsetof(CalleeSavedRegisters, s1))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S2, offsetof(CalleeSavedRegisters, s2))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S3, offsetof(CalleeSavedRegisters, s3))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S4, offsetof(CalleeSavedRegisters, s4))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S5, offsetof(CalleeSavedRegisters, s5))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S6, offsetof(CalleeSavedRegisters, s6))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S7, offsetof(CalleeSavedRegisters, s7))
CDAC_TYPE_FIELD(CalleeSavedRegisters, T_NUINT, S8, offsetof(CalleeSavedRegisters, s8))

#endif // Platform switch
CDAC_TYPE_END(CalleeSavedRegisters)

CDAC_TYPE_BEGIN(EETypeHashTable)
CDAC_TYPE_INDETERMINATE(EETypeHashTable)
CDAC_TYPE_FIELD(EETypeHashTable, T_POINTER, Buckets, cdac_data<EETypeHashTable>::Buckets)
CDAC_TYPE_FIELD(EETypeHashTable, T_UINT32, Count, cdac_data<EETypeHashTable>::Count)
CDAC_TYPE_FIELD(EETypeHashTable, T_POINTER, VolatileEntryValue, cdac_data<EETypeHashTable>::VolatileEntryValue)
CDAC_TYPE_FIELD(EETypeHashTable, T_POINTER, VolatileEntryNextEntry, cdac_data<EETypeHashTable>::VolatileEntryNextEntry)
CDAC_TYPE_END(EETypeHashTable)

CDAC_TYPE_BEGIN(InstMethodHashTable)
CDAC_TYPE_INDETERMINATE(InstMethodHashTable)
CDAC_TYPE_FIELD(InstMethodHashTable, T_POINTER, Buckets, cdac_data<InstMethodHashTable>::Buckets)
CDAC_TYPE_FIELD(InstMethodHashTable, T_UINT32, Count, cdac_data<InstMethodHashTable>::Count)
CDAC_TYPE_FIELD(InstMethodHashTable, T_POINTER, VolatileEntryValue, cdac_data<InstMethodHashTable>::VolatileEntryValue)
CDAC_TYPE_FIELD(InstMethodHashTable, T_POINTER, VolatileEntryNextEntry, cdac_data<InstMethodHashTable>::VolatileEntryNextEntry)
CDAC_TYPE_END(InstMethodHashTable)

CDAC_TYPE_BEGIN(CoreLibBinder)
CDAC_TYPE_INDETERMINATE(CoreLibBinder)
CDAC_TYPE_FIELD(CoreLibBinder, T_POINTER, Classes, cdac_data<CoreLibBinder>::Classes)
CDAC_TYPE_END(CoreLibBinder)

// this is an SHash type
CDAC_TYPE_BEGIN(DynamicILBlobTable)
CDAC_TYPE_SIZE(cdac_data<DynamicILBlobTable>::EntrySize)
CDAC_TYPE_FIELD(DynamicILBlobTable, T_POINTER, Table, cdac_data<DynamicILBlobTable>::Table)
CDAC_TYPE_FIELD(DynamicILBlobTable, T_UINT32, TableSize, cdac_data<DynamicILBlobTable>::TableSize)
CDAC_TYPE_FIELD(DynamicILBlobTable, T_UINT32, EntryMethodToken, cdac_data<DynamicILBlobTable>::EntryMethodToken)
CDAC_TYPE_FIELD(DynamicILBlobTable, T_POINTER, EntryIL, cdac_data<DynamicILBlobTable>::EntryIL)
CDAC_TYPE_END(DynamicILBlobTable)

#ifdef FEATURE_COMINTEROP
CDAC_TYPE_BEGIN(ComCallWrapper)
CDAC_TYPE_INDETERMINATE(ComCallWrapper)
CDAC_TYPE_FIELD(ComCallWrapper, T_POINTER, Handle, cdac_data<ComCallWrapper>::Handle)
CDAC_TYPE_FIELD(ComCallWrapper, T_POINTER, SimpleWrapper, cdac_data<ComCallWrapper>::SimpleWrapper)
CDAC_TYPE_FIELD(ComCallWrapper, T_POINTER, IPtr, cdac_data<ComCallWrapper>::IPtr)
CDAC_TYPE_FIELD(ComCallWrapper, T_POINTER, Next, cdac_data<ComCallWrapper>::Next)
CDAC_TYPE_END(ComCallWrapper)

CDAC_TYPE_BEGIN(SimpleComCallWrapper)
CDAC_TYPE_INDETERMINATE(SimpleComCallWrapper)
CDAC_TYPE_FIELD(SimpleComCallWrapper, T_POINTER, OuterIUnknown, cdac_data<SimpleComCallWrapper>::OuterIUnknown)
CDAC_TYPE_FIELD(SimpleComCallWrapper, T_INT64, RefCount, cdac_data<SimpleComCallWrapper>::RefCount)
CDAC_TYPE_FIELD(SimpleComCallWrapper, T_UINT32, Flags, cdac_data<SimpleComCallWrapper>::Flags)
CDAC_TYPE_FIELD(SimpleComCallWrapper, T_POINTER, MainWrapper, cdac_data<SimpleComCallWrapper>::MainWrapper)
CDAC_TYPE_FIELD(SimpleComCallWrapper, T_POINTER, VTablePtr, cdac_data<SimpleComCallWrapper>::VTablePtr)
CDAC_TYPE_END(SimpleComCallWrapper)

CDAC_TYPE_BEGIN(ComMethodTable)
CDAC_TYPE_SIZE(sizeof(ComMethodTable))
CDAC_TYPE_FIELD(ComMethodTable, T_NUINT, Flags, cdac_data<ComMethodTable>::Flags)
CDAC_TYPE_FIELD(ComMethodTable, T_POINTER, MethodTable, cdac_data<ComMethodTable>::MethodTable)
CDAC_TYPE_END(ComMethodTable)
CDAC_TYPE_BEGIN(RCWCleanupList)
CDAC_TYPE_INDETERMINATE(RCWCleanupList)
CDAC_TYPE_FIELD(RCWCleanupList, T_POINTER, FirstBucket, cdac_data<RCWCleanupList>::FirstBucket)
CDAC_TYPE_END(RCWCleanupList)

CDAC_TYPE_BEGIN(RCW)
CDAC_TYPE_INDETERMINATE(RCW)
CDAC_TYPE_FIELD(RCW, T_POINTER, NextCleanupBucket, cdac_data<RCW>::NextCleanupBucket)
CDAC_TYPE_FIELD(RCW, T_POINTER, NextRCW, cdac_data<RCW>::NextRCW)
CDAC_TYPE_FIELD(RCW, T_UINT32, Flags, cdac_data<RCW>::Flags)
CDAC_TYPE_FIELD(RCW, T_POINTER, CtxCookie, cdac_data<RCW>::CtxCookie)
CDAC_TYPE_FIELD(RCW, T_POINTER, CtxEntry, cdac_data<RCW>::CtxEntry)
CDAC_TYPE_FIELD(RCW, T_ARRAY(TYPE(InterfaceEntry)), InterfaceEntries, cdac_data<RCW>::InterfaceEntries)
CDAC_TYPE_FIELD(RCW, T_POINTER, IdentityPointer, cdac_data<RCW>::IdentityPointer)
CDAC_TYPE_FIELD(RCW, T_UINT32, SyncBlockIndex, cdac_data<RCW>::SyncBlockIndex)
CDAC_TYPE_FIELD(RCW, T_POINTER, VTablePtr, cdac_data<RCW>::VTablePtr)
CDAC_TYPE_FIELD(RCW, T_POINTER, CreatorThread, cdac_data<RCW>::CreatorThread)
CDAC_TYPE_FIELD(RCW, T_UINT32, RefCount, cdac_data<RCW>::RefCount)
CDAC_TYPE_FIELD(RCW, T_POINTER, UnknownPointer, cdac_data<RCW>::UnknownPointer)
CDAC_TYPE_END(RCW)

CDAC_TYPE_BEGIN(CtxEntry)
CDAC_TYPE_INDETERMINATE(CtxEntry)
CDAC_TYPE_FIELD(CtxEntry, T_POINTER, STAThread, cdac_data<CtxEntry>::STAThread)
CDAC_TYPE_FIELD(CtxEntry, T_POINTER, CtxCookie, cdac_data<CtxEntry>::CtxCookie)
CDAC_TYPE_END(CtxEntry)

CDAC_TYPE_BEGIN(InterfaceEntry)
CDAC_TYPE_SIZE(sizeof(InterfaceEntry))
CDAC_TYPE_FIELD(InterfaceEntry, T_POINTER, MethodTable, cdac_data<InterfaceEntry>::MethodTable)
CDAC_TYPE_FIELD(InterfaceEntry, T_POINTER, Unknown, cdac_data<InterfaceEntry>::Unknown)
CDAC_TYPE_END(InterfaceEntry)
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMWRAPPERS
CDAC_TYPE_BEGIN(ComWrappersVtablePtrs)
CDAC_TYPE_SIZE(g_numKnownQueryInterfaceImplementations * sizeof(PCODE))
CDAC_TYPE_END(ComWrappersVtablePtrs)
#endif

CDAC_TYPE_BEGIN(SyncBlockCache)
CDAC_TYPE_INDETERMINATE(SyncBlockCache)
CDAC_TYPE_FIELD(SyncBlockCache, T_UINT32, FreeSyncTableIndex, cdac_data<SyncBlockCache>::FreeSyncTableIndex)
CDAC_TYPE_FIELD(SyncBlockCache, T_POINTER, CleanupBlockList, cdac_data<SyncBlockCache>::CleanupBlockList)
CDAC_TYPE_END(SyncBlockCache)

CDAC_TYPE_BEGIN(AuxiliarySymbolInfo)
CDAC_TYPE_SIZE(sizeof(VMAUXILIARYSYMBOLDEF))
CDAC_TYPE_FIELD(AuxiliarySymbolInfo, TYPE(CodePointer), Address, offsetof(VMAUXILIARYSYMBOLDEF, pfnAuxiliarySymbol))
CDAC_TYPE_FIELD(AuxiliarySymbolInfo, T_POINTER, Name, offsetof(VMAUXILIARYSYMBOLDEF, name))
CDAC_TYPE_END(AuxiliarySymbolInfo)

CDAC_TYPE_BEGIN(EEConfig)
CDAC_TYPE_INDETERMINATE(EEConfig)
CDAC_TYPE_FIELD(EEConfig, T_UINT32, ModifiableAssemblies, cdac_data<EEConfig>::ModifiableAssemblies)
CDAC_TYPE_END(EEConfig)

CDAC_TYPES_END()

CDAC_GLOBALS_BEGIN()

#if defined(TARGET_BROWSER)
#ifdef Browser
#error Handle 'Browser' define
#endif // Browser
CDAC_GLOBAL_STRING(OperatingSystem, Browser)
#elif defined(TARGET_UNIX)
#ifdef Unix
#error Handle 'Unix' define
#endif // Unix
CDAC_GLOBAL_STRING(OperatingSystem, Unix)
#elif defined(TARGET_WINDOWS)
#ifdef Windows
#error Handle 'Windows' define
#endif // Windows
CDAC_GLOBAL_STRING(OperatingSystem, Windows)
#else
#error TARGET_{OS} define is not recognized by the cDAC. Update this switch and the enum values in IRuntimeInfo.cs
#endif

#if defined(TARGET_X86)
CDAC_GLOBAL_STRING(Architecture, x86)
#elif defined(TARGET_AMD64)
CDAC_GLOBAL_STRING(Architecture, x64)
#elif defined(TARGET_ARM)
CDAC_GLOBAL_STRING(Architecture, arm)
#elif defined(TARGET_ARM64)
CDAC_GLOBAL_STRING(Architecture, arm64)
#elif defined(TARGET_LOONGARCH64)
CDAC_GLOBAL_STRING(Architecture, loongarch64)
#elif defined(TARGET_RISCV64)
CDAC_GLOBAL_STRING(Architecture, riscv64)
#elif defined(TARGET_WASM)
CDAC_GLOBAL_STRING(Architecture, wasm)
#else
#error TARGET_{ARCH} define is not recognized by the cDAC. Update this switch and the enum values in IRuntimeInfo.cs
#endif

CDAC_GLOBAL_STRING(RID, RID_STRING)

CDAC_GLOBAL(GCInfoVersion, T_UINT32, GCINFO_VERSION)

CDAC_GLOBAL_POINTER(AppDomain, &AppDomain::m_pTheAppDomain)
CDAC_GLOBAL_POINTER(SystemDomain, cdac_data<SystemDomain>::SystemDomainPtr)
CDAC_GLOBAL_POINTER(ThreadStore, &ThreadStore::s_pThreadStore)
CDAC_GLOBAL_POINTER(FinalizerThread, &::g_pFinalizerThread)
CDAC_GLOBAL_POINTER(GCThread, &::g_pSuspensionThread)
CDAC_GLOBAL_POINTER(EEConfig, &::g_pConfig)
#if defined(DEBUGGING_SUPPORTED) && !defined(TARGET_WASM)
CDAC_GLOBAL_POINTER(Debugger, &::g_pDebugger)
CDAC_GLOBAL_POINTER(CLRJitAttachState, &::CLRJitAttachState)
#endif // DEBUGGING_SUPPORTED && !TARGET_WASM
#ifdef FEATURE_METADATA_UPDATER
CDAC_GLOBAL_POINTER(MetadataUpdatesApplied, &::g_metadataUpdatesApplied)
#endif

// Add FrameIdentifier for all defined Frame types. Used to differentiate Frame objects.
#define FRAME_TYPE_NAME(frameType) \
    CDAC_GLOBAL(frameType##Identifier, T_NUINT, (uint64_t)FrameIdentifier::frameType)

    #include "frames.h"
#undef FRAME_TYPE_NAME

CDAC_GLOBAL(MethodDescTokenRemainderBitCount, T_UINT8, METHOD_TOKEN_REMAINDER_BIT_COUNT)
#if FEATURE_COMINTEROP
CDAC_GLOBAL(FeatureCOMInterop, T_UINT8, 1)
#else
CDAC_GLOBAL(FeatureCOMInterop, T_UINT8, 0)
#endif
#if FEATURE_COMWRAPPERS
CDAC_GLOBAL(FeatureComWrappers, T_UINT8, 1)
#else
CDAC_GLOBAL(FeatureComWrappers, T_UINT8, 0)
#endif
#if FEATURE_OBJCMARSHAL
CDAC_GLOBAL(FeatureObjCMarshal, T_UINT8, 1)
#else
CDAC_GLOBAL(FeatureObjCMarshal, T_UINT8, 0)
#endif
#if FEATURE_JAVAMARSHAL
CDAC_GLOBAL(FeatureJavaMarshal, T_UINT8, 1)
#else
CDAC_GLOBAL(FeatureJavaMarshal, T_UINT8, 0)
#endif
#ifdef FEATURE_ON_STACK_REPLACEMENT
CDAC_GLOBAL(FeatureOnStackReplacement, T_UINT8, 1)
#else
CDAC_GLOBAL(FeatureOnStackReplacement, T_UINT8, 0)
#endif // FEATURE_ON_STACK_REPLACEMENT
#ifdef FEATURE_WEBCIL
CDAC_GLOBAL(FeatureWebcil, T_UINT8, 1)
#else
CDAC_GLOBAL(FeatureWebcil, T_UINT8, 0)
#endif
// See Object::GetGCSafeMethodTable
#ifdef TARGET_64BIT
CDAC_GLOBAL(ObjectToMethodTableUnmask, T_UINT8, 1 | 1 << 1 | 1 << 2)
#else
CDAC_GLOBAL(ObjectToMethodTableUnmask, T_UINT8, 1 | 1 << 1)
#endif //TARGET_64BIT
#ifdef FEATURE_PORTABLE_ENTRYPOINTS
CDAC_GLOBAL(FeaturePortableEntrypoints, T_UINT8, 1)
#else
CDAC_GLOBAL(FeaturePortableEntrypoints, T_UINT8, 0)
#endif // FEATURE_PORTABLE_ENTRYPOINTS
CDAC_GLOBAL(SOSBreakingChangeVersion, T_UINT8, SOS_BREAKING_CHANGE_VERSION)
CDAC_GLOBAL(RecommendedReaderVersion, T_UINT32, CDAC_RECOMMENDED_READER_VERSION)
CDAC_GLOBAL(DirectorySeparator, T_UINT8, (uint8_t)DIRECTORY_SEPARATOR_CHAR_A)
CDAC_GLOBAL(HashMapSlotsPerBucket, T_UINT32, SLOTS_PER_BUCKET)
CDAC_GLOBAL(HashMapValueMask, T_UINT64, VALUE_MASK)
CDAC_GLOBAL(MethodDescAlignment, T_UINT64, MethodDesc::ALIGNMENT)
CDAC_GLOBAL(ArrayBaseSize, T_UINT32, ARRAYBASE_BASESIZE)
CDAC_GLOBAL(SyncBlockValueToObjectOffset, T_UINT16, OBJHEADER_SIZE - cdac_data<ObjHeader>::SyncBlockValue)
CDAC_GLOBAL(StubCodeBlockLast, T_UINT8, STUB_CODE_BLOCK_LAST)
CDAC_GLOBAL(DefaultADID, T_UINT32, DefaultADID)
#ifndef TARGET_UNIX
CDAC_GLOBAL(SizeOfGenericModeBlock, T_UINT32, sizeof(GenericModeBlock))
#endif
CDAC_GLOBAL(StaticsPointerMask, T_NUINT, DynamicStaticsInfo::STATICSPOINTERMASK)
CDAC_GLOBAL(PtrArrayOffsetToDataArray, T_NUINT, offsetof(PtrArray, m_Array))
CDAC_GLOBAL(NumberOfTlsOffsetsNotUsedInNoncollectibleArray, T_UINT8, NUMBER_OF_TLSOFFSETS_NOT_USED_IN_NONCOLLECTIBLE_ARRAY)
CDAC_GLOBAL(MaxClrNotificationArgs, T_UINT32, MAX_CLR_NOTIFICATION_ARGS)
CDAC_GLOBAL(FieldOffsetBigRVA, T_UINT32, FIELD_OFFSET_BIG_RVA)
CDAC_GLOBAL(FieldOffsetDynamicRVA, T_UINT32, FIELD_OFFSET_DYNAMIC_RVA)
CDAC_GLOBAL_POINTER(ClrNotificationArguments, &::g_clrNotificationArguments)
CDAC_GLOBAL_POINTER(ArrayBoundsZero, cdac_data<ArrayBase>::ArrayBoundsZero)
CDAC_GLOBAL_POINTER(ContinuationMethodTable, &::g_pContinuationClassIfSubTypeCreated)
CDAC_GLOBAL_POINTER(ExceptionMethodTable, &::g_pExceptionClass)
CDAC_GLOBAL_POINTER(FreeObjectMethodTable, &::g_pFreeObjectMethodTable)
CDAC_GLOBAL_POINTER(ObjectMethodTable, &::g_pObjectClass)
CDAC_GLOBAL_POINTER(ObjectArrayMethodTable, &::g_pPredefinedArrayTypes[ELEMENT_TYPE_OBJECT])
CDAC_GLOBAL_POINTER(StringMethodTable, &::g_pStringClass)
CDAC_GLOBAL_POINTER(SyncTableEntries, &::g_pSyncTable)
CDAC_GLOBAL_POINTER(MiniMetaDataBuffAddress, &::g_MiniMetaDataBuffAddress)
CDAC_GLOBAL_POINTER(MiniMetaDataBuffMaxSize, &::g_MiniMetaDataBuffMaxSize)
CDAC_GLOBAL_POINTER(DacNotificationFlags, &::g_dacNotificationFlags)
CDAC_GLOBAL_POINTER(OffsetOfCurrentThreadInfo, &::g_offsetOfCurrentThreadInfo)
CDAC_GLOBAL_POINTER(ThinlockThreadIdDispenser, &::g_pThinLockThreadIdDispenser)
#ifdef FEATURE_COMWRAPPERS
CDAC_GLOBAL(DispatchThisPtrMask, T_NUINT, InteropLib::ABI::DispatchThisPtrMask)
CDAC_GLOBAL_POINTER(ComWrappersVtablePtrs, InteropLib::ABI::g_knownQueryInterfaceImplementations)
#endif // FEATURE_COMWRAPPERS
CDAC_GLOBAL_POINTER(GcNotificationFlags, &::g_gcNotificationFlags)
CDAC_GLOBAL_POINTER(GlobalAllocContext, &::g_global_alloc_context)
CDAC_GLOBAL_POINTER(CoreLib, &::g_CoreLib)
#ifdef TARGET_WINDOWS
CDAC_GLOBAL_POINTER(TlsIndexBase, &::_tls_index)
#endif // TARGET_WINDOWS
#ifdef STRESS_LOG
CDAC_GLOBAL(StressLogEnabled, T_UINT8, 1)
CDAC_GLOBAL_POINTER(StressLog, &g_pStressLog)
CDAC_GLOBAL(StressLogHasModuleTable, T_UINT8, 1)
CDAC_GLOBAL(StressLogMaxModules, T_UINT64, cdac_offsets<StressLog>::MAX_MODULES)
CDAC_GLOBAL(StressLogChunkSize, T_UINT32, STRESSLOG_CHUNK_SIZE)
CDAC_GLOBAL(StressLogValidChunkSig, T_UINT32, StressLogChunk::ValidChunkSig)
CDAC_GLOBAL(StressLogMaxMessageSize, T_UINT64, (uint64_t)StressMsg::maxMsgSize)
#else
CDAC_GLOBAL(StressLogEnabled, T_UINT8, 0)
#endif
CDAC_GLOBAL_POINTER(ExecutionManagerCodeRangeMapAddress, cdac_data<ExecutionManager>::CodeRangeMapAddress)
CDAC_GLOBAL_POINTER(EEJitManagerAddress, cdac_data<ExecutionManager>::EEJitManagerAddress)
CDAC_GLOBAL_POINTER(PlatformMetadata, &::g_cdacPlatformMetadata)
#ifdef PROFILING_SUPPORTED
CDAC_GLOBAL_POINTER(ProfilerControlBlock, &::g_profControlBlock)
#endif // PROFILING_SUPPORTED
CDAC_GLOBAL_POINTER(SyncBlockCache, &SyncBlockCache::s_pSyncBlockCache)
CDAC_GLOBAL(SyncBlockMaskLockThreadId, T_UINT32, SBLK_MASK_LOCK_THREADID)
CDAC_GLOBAL(SyncBlockMaskLockRecursionLevel, T_UINT32, SBLK_MASK_LOCK_RECLEVEL)
CDAC_GLOBAL(SyncBlockRecursionLevelShift, T_UINT32, SBLK_RECLEVEL_SHIFT)
CDAC_GLOBAL(SyncBlockIsHashOrSyncBlockIndex, T_UINT32, BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX)
CDAC_GLOBAL(SyncBlockIsHashCode, T_UINT32, BIT_SBLK_IS_HASHCODE)
CDAC_GLOBAL(SyncBlockIndexMask, T_UINT32, MASK_SYNCBLOCKINDEX)
CDAC_GLOBAL(SyncBlockHashCodeMask, T_UINT32, MASK_HASHCODE)

CDAC_GLOBAL_POINTER(GCLowestAddress, &g_lowest_address)
CDAC_GLOBAL_POINTER(GCHighestAddress, &g_highest_address)
CDAC_GLOBAL_POINTER(AuxiliarySymbols, &::hlpAuxiliarySymbolTable)
CDAC_GLOBAL_POINTER(AuxiliarySymbolCount, &::g_auxiliarySymbolCount)
#if FEATURE_COMINTEROP
CDAC_GLOBAL(CCWNumInterfaces, T_UINT32, cdac_data<ComCallWrapper>::NumInterfaces)
CDAC_GLOBAL(CCWThisMask, T_NUINT, cdac_data<ComCallWrapper>::ThisMask)
CDAC_GLOBAL_POINTER(TearOffAddRef, &g_cdacTearOffAddRef)
CDAC_GLOBAL_POINTER(TearOffAddRefSimple, &g_cdacTearOffAddRefSimple)
CDAC_GLOBAL_POINTER(TearOffAddRefSimpleInner, &g_cdacTearOffAddRefSimpleInner)
CDAC_GLOBAL_POINTER(RCWCleanupList, &g_pRCWCleanupList)
CDAC_GLOBAL(RCWInterfaceCacheSize, T_UINT32, INTERFACE_ENTRY_CACHE_SIZE)
#endif // FEATURE_COMINTEROP

// It is important for the subdescriptor pointers to be the last pointers in the global structure.
// EnumMemDataDescriptors in enummem.cpp hardcodes the number of subdescriptors and utilizes
// this order to find and enumerate the subdescriptors.
//
// When adding a new subdescriptor, EnumMemDescriptors must be updated appropriately.
CDAC_GLOBAL_SUB_DESCRIPTOR(GC, &(g_gc_dac_vars.gc_descriptor))

CDAC_GLOBAL_CONTRACT(AuxiliarySymbols, 1)
#if FEATURE_COMINTEROP
CDAC_GLOBAL_CONTRACT(BuiltInCOM, 1)
#endif // FEATURE_COMINTEROP
CDAC_GLOBAL_CONTRACT(CodeVersions, 1)
#ifdef FEATURE_COMWRAPPERS
CDAC_GLOBAL_CONTRACT(ComWrappers, 1)
#endif // FEATURE_COMWRAPPERS
CDAC_GLOBAL_CONTRACT(ConditionalWeakTable, 1)
CDAC_GLOBAL_CONTRACT(DacStreams, 1)
#if defined(DEBUGGING_SUPPORTED) && !defined(TARGET_WASM)
CDAC_GLOBAL_CONTRACT(Debugger, 1)
#endif // DEBUGGING_SUPPORTED && !TARGET_WASM
CDAC_GLOBAL_CONTRACT(DebugInfo, 2)
CDAC_GLOBAL_CONTRACT(EcmaMetadata, 1)
CDAC_GLOBAL_CONTRACT(Exception, 1)
CDAC_GLOBAL_CONTRACT(ExecutionManager, 2)
CDAC_GLOBAL_CONTRACT(GCInfo, 1)
CDAC_GLOBAL_CONTRACT(Loader, 1)
CDAC_GLOBAL_CONTRACT(Notifications, 1)
CDAC_GLOBAL_CONTRACT(Object, 1)
CDAC_GLOBAL_CONTRACT(PlatformMetadata, 1)
CDAC_GLOBAL_CONTRACT(PrecodeStubs, 3)
#ifdef PROFILING_SUPPORTED
CDAC_GLOBAL_CONTRACT(ReJIT, 1)
#endif // PROFILING_SUPPORTED
CDAC_GLOBAL_CONTRACT(RuntimeInfo, 1)
CDAC_GLOBAL_CONTRACT(RuntimeTypeSystem, 1)
CDAC_GLOBAL_CONTRACT(SHash, 1)
CDAC_GLOBAL_CONTRACT(SignatureDecoder, 1)
CDAC_GLOBAL_CONTRACT(StackWalk, 1)
CDAC_GLOBAL_CONTRACT(StressLog, 2)
CDAC_GLOBAL_CONTRACT(SyncBlock, 1)
CDAC_GLOBAL_CONTRACT(Thread, 1)

CDAC_GLOBALS_END()
