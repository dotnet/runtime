// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#define FireEtwGCStart(Count, Reason) 0
#define FireEtwGCStart_V1(Count, Depth, Reason, Type, ClrInstanceID) 0
#define FireEtwGCStart_V2(Count, Depth, Reason, Type, ClrInstanceID, ClientSequenceNumber) 0
#define FireEtwGCEnd(Count, Depth) 0
#define FireEtwGCEnd_V1(Count, Depth, ClrInstanceID) 0
#define FireEtwGCRestartEEEnd() 0
#define FireEtwGCRestartEEEnd_V1(ClrInstanceID) 0
#define FireEtwGCHeapStats(GenerationSize0, TotalPromotedSize0, GenerationSize1, TotalPromotedSize1, GenerationSize2, TotalPromotedSize2, GenerationSize3, TotalPromotedSize3, FinalizationPromotedSize, FinalizationPromotedCount, PinnedObjectCount, SinkBlockCount, GCHandleCount) 0
#define FireEtwGCHeapStats_V1(GenerationSize0, TotalPromotedSize0, GenerationSize1, TotalPromotedSize1, GenerationSize2, TotalPromotedSize2, GenerationSize3, TotalPromotedSize3, FinalizationPromotedSize, FinalizationPromotedCount, PinnedObjectCount, SinkBlockCount, GCHandleCount, ClrInstanceID) 0
#define FireEtwGCCreateSegment(Address, Size, Type) 0
#define FireEtwGCCreateSegment_V1(Address, Size, Type, ClrInstanceID) 0
#define FireEtwGCFreeSegment(Address) 0
#define FireEtwGCFreeSegment_V1(Address, ClrInstanceID) 0
#define FireEtwGCRestartEEBegin() 0
#define FireEtwGCRestartEEBegin_V1(ClrInstanceID) 0
#define FireEtwGCSuspendEEEnd() 0
#define FireEtwGCSuspendEEEnd_V1(ClrInstanceID) 0
#define FireEtwGCSuspendEEBegin(Reason) 0
#define FireEtwGCSuspendEEBegin_V1(Reason, Count, ClrInstanceID) 0
#define FireEtwGCAllocationTick(AllocationAmount, AllocationKind) 0
#define FireEtwGCAllocationTick_V1(AllocationAmount, AllocationKind, ClrInstanceID) 0
#define FireEtwGCAllocationTick_V2(AllocationAmount, AllocationKind, ClrInstanceID, AllocationAmount64, TypeID, TypeName, HeapIndex) 0
#define FireEtwGCAllocationTick_V3(AllocationAmount, AllocationKind, ClrInstanceID, AllocationAmount64, TypeID, TypeName, HeapIndex, Address) 0
#define FireEtwGCCreateConcurrentThread() 0
#define FireEtwGCCreateConcurrentThread_V1(ClrInstanceID) 0
#define FireEtwGCTerminateConcurrentThread() 0
#define FireEtwGCTerminateConcurrentThread_V1(ClrInstanceID) 0
#define FireEtwGCFinalizersEnd(Count) 0
#define FireEtwGCFinalizersEnd_V1(Count, ClrInstanceID) 0
#define FireEtwGCFinalizersBegin() 0
#define FireEtwGCFinalizersBegin_V1(ClrInstanceID) 0
#define FireEtwBulkType(Count, ClrInstanceID, Values_Len_, Values) 0
#define FireEtwGCBulkRootEdge(Index, Count, ClrInstanceID, Values_Len_, Values) 0
#define FireEtwGCBulkRootConditionalWeakTableElementEdge(Index, Count, ClrInstanceID, Values_Len_, Values) 0
#define FireEtwGCBulkNode(Index, Count, ClrInstanceID, Values_Len_, Values) 0
#define FireEtwGCBulkEdge(Index, Count, ClrInstanceID, Values_Len_, Values) 0
#define FireEtwGCSampledObjectAllocationHigh(Address, TypeID, ObjectCountForTypeSample, TotalSizeForTypeSample, ClrInstanceID) 0
#define FireEtwGCBulkSurvivingObjectRanges(Index, Count, ClrInstanceID, Values_Len_, Values) 0
#define FireEtwGCBulkMovedObjectRanges(Index, Count, ClrInstanceID, Values_Len_, Values) 0
#define FireEtwGCGenerationRange(Generation, RangeStart, RangeUsedLength, RangeReservedLength, ClrInstanceID) 0
#define FireEtwGCMarkStackRoots(HeapNum, ClrInstanceID) 0
#define FireEtwGCMarkFinalizeQueueRoots(HeapNum, ClrInstanceID) 0
#define FireEtwGCMarkHandles(HeapNum, ClrInstanceID) 0
#define FireEtwGCMarkOlderGenerationRoots(HeapNum, ClrInstanceID) 0
#define FireEtwFinalizeObject(TypeID, ObjectID, ClrInstanceID) 0
#define FireEtwSetGCHandle(HandleID, ObjectID, Kind, Generation, ClrInstanceID) 0
#define FireEtwDestroyGCHandle(HandleID, ClrInstanceID) 0
#define FireEtwGCSampledObjectAllocationLow(Address, TypeID, ObjectCountForTypeSample, TotalSizeForTypeSample, ClrInstanceID) 0
#define FireEtwPinObjectAtGCTime(HandleID, ObjectID, ObjectSize, TypeName, ClrInstanceID) 0
#define FireEtwGCTriggered(Reason, ClrInstanceID) 0
#define FireEtwGCBulkRootCCW(Count, ClrInstanceID, Values_Len_, Values) 0
#define FireEtwGCBulkRCW(Count, ClrInstanceID, Values_Len_, Values) 0
#define FireEtwGCBulkRootStaticVar(Count, AppDomainID, ClrInstanceID, Values_Len_, Values) 0
#define FireEtwWorkerThreadCreate(WorkerThreadCount, RetiredWorkerThreads) 0
#define FireEtwWorkerThreadTerminate(WorkerThreadCount, RetiredWorkerThreads) 0
#define FireEtwWorkerThreadRetire(WorkerThreadCount, RetiredWorkerThreads) 0
#define FireEtwWorkerThreadUnretire(WorkerThreadCount, RetiredWorkerThreads) 0
#define FireEtwIOThreadCreate(IOThreadCount, RetiredIOThreads) 0
#define FireEtwIOThreadCreate_V1(IOThreadCount, RetiredIOThreads, ClrInstanceID) 0
#define FireEtwIOThreadTerminate(IOThreadCount, RetiredIOThreads) 0
#define FireEtwIOThreadTerminate_V1(IOThreadCount, RetiredIOThreads, ClrInstanceID) 0
#define FireEtwIOThreadRetire(IOThreadCount, RetiredIOThreads) 0
#define FireEtwIOThreadRetire_V1(IOThreadCount, RetiredIOThreads, ClrInstanceID) 0
#define FireEtwIOThreadUnretire(IOThreadCount, RetiredIOThreads) 0
#define FireEtwIOThreadUnretire_V1(IOThreadCount, RetiredIOThreads, ClrInstanceID) 0
#define FireEtwThreadpoolSuspensionSuspendThread(ClrThreadID, CpuUtilization) 0
#define FireEtwThreadpoolSuspensionResumeThread(ClrThreadID, CpuUtilization) 0
#define FireEtwThreadPoolWorkerThreadStart(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID) 0
#define FireEtwThreadPoolWorkerThreadStop(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID) 0
#define FireEtwThreadPoolWorkerThreadRetirementStart(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID) 0
#define FireEtwThreadPoolWorkerThreadRetirementStop(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID) 0
#define FireEtwThreadPoolWorkerThreadAdjustmentSample(Throughput, ClrInstanceID) 0
#define FireEtwThreadPoolWorkerThreadAdjustmentAdjustment(AverageThroughput, NewWorkerThreadCount, Reason, ClrInstanceID) 0
#define FireEtwThreadPoolWorkerThreadAdjustmentStats(Duration, Throughput, ThreadWave, ThroughputWave, ThroughputErrorEstimate, AverageThroughputErrorEstimate, ThroughputRatio, Confidence, NewControlSetting, NewThreadWaveMagnitude, ClrInstanceID) 0
#define FireEtwThreadPoolWorkerThreadWait(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID) 0
#define FireEtwThreadPoolWorkingThreadCount(Count, ClrInstanceID) 0
#define FireEtwThreadPoolEnqueue(WorkID, ClrInstanceID) 0
#define FireEtwThreadPoolDequeue(WorkID, ClrInstanceID) 0
#define FireEtwThreadPoolIOEnqueue(NativeOverlapped, Overlapped, MultiDequeues, ClrInstanceID) 0
#define FireEtwThreadPoolIODequeue(NativeOverlapped, Overlapped, ClrInstanceID) 0
#define FireEtwThreadPoolIOPack(NativeOverlapped, Overlapped, ClrInstanceID) 0
#define FireEtwThreadCreating(ID, ClrInstanceID) 0
#define FireEtwThreadRunning(ID, ClrInstanceID) 0
#define FireEtwExceptionThrown() 0
#define FireEtwExceptionThrown_V1(ExceptionType, ExceptionMessage, ExceptionEIP, ExceptionHRESULT, ExceptionFlags, ClrInstanceID) 0
#define FireEtwExceptionCatchStart(EntryEIP, MethodID, MethodName, ClrInstanceID) 0
#define FireEtwExceptionCatchStop() 0
#define FireEtwExceptionFinallyStart(EntryEIP, MethodID, MethodName, ClrInstanceID) 0
#define FireEtwExceptionFinallyStop() 0
#define FireEtwExceptionFilterStart(EntryEIP, MethodID, MethodName, ClrInstanceID) 0
#define FireEtwExceptionFilterStop() 0
#define FireEtwExceptionThrownStop() 0
#define FireEtwContention() 0
#define FireEtwContentionStart_V1(ContentionFlags, ClrInstanceID) 0
#define FireEtwContentionStop(ContentionFlags, ClrInstanceID) 0
#define FireEtwCLRStackWalk(ClrInstanceID, Reserved1, Reserved2, FrameCount, Stack) 0
#define FireEtwAppDomainMemAllocated(AppDomainID, Allocated, ClrInstanceID) 0
#define FireEtwAppDomainMemSurvived(AppDomainID, Survived, ProcessSurvived, ClrInstanceID) 0
#define FireEtwThreadCreated(ManagedThreadID, AppDomainID, Flags, ManagedThreadIndex, OSThreadID, ClrInstanceID) 0
#define FireEtwThreadTerminated(ManagedThreadID, AppDomainID, ClrInstanceID) 0
#define FireEtwThreadDomainEnter(ManagedThreadID, AppDomainID, ClrInstanceID) 0
#define FireEtwILStubGenerated(ClrInstanceID, ModuleID, StubMethodID, StubFlags, ManagedInteropMethodToken, ManagedInteropMethodNamespace, ManagedInteropMethodName, ManagedInteropMethodSignature, NativeMethodSignature, StubMethodSignature, StubMethodILCode) 0
#define FireEtwILStubCacheHit(ClrInstanceID, ModuleID, StubMethodID, ManagedInteropMethodToken, ManagedInteropMethodNamespace, ManagedInteropMethodName, ManagedInteropMethodSignature) 0
#define FireEtwDCStartCompleteV2() 0
#define FireEtwDCEndCompleteV2() 0
#define FireEtwMethodDCStartV2(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags) 0
#define FireEtwMethodDCEndV2(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags) 0
#define FireEtwMethodDCStartVerboseV2(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, MethodNamespace, MethodName, MethodSignature) 0
#define FireEtwMethodDCEndVerboseV2(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, MethodNamespace, MethodName, MethodSignature) 0
#define FireEtwMethodLoad(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags) 0
#define FireEtwMethodLoad_V1(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, ClrInstanceID) 0
#define FireEtwMethodLoad_V2(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, ClrInstanceID, ReJITID) 0
#define FireEtwMethodUnload(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags) 0
#define FireEtwMethodUnload_V1(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, ClrInstanceID) 0
#define FireEtwMethodUnload_V2(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, ClrInstanceID, ReJITID) 0
#define FireEtwMethodLoadVerbose(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, MethodNamespace, MethodName, MethodSignature) 0
#define FireEtwMethodLoadVerbose_V1(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, MethodNamespace, MethodName, MethodSignature, ClrInstanceID) 0
#define FireEtwMethodLoadVerbose_V2(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, MethodNamespace, MethodName, MethodSignature, ClrInstanceID, ReJITID) 0
#define FireEtwMethodUnloadVerbose(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, MethodNamespace, MethodName, MethodSignature) 0
#define FireEtwMethodUnloadVerbose_V1(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, MethodNamespace, MethodName, MethodSignature, ClrInstanceID) 0
#define FireEtwMethodUnloadVerbose_V2(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, MethodNamespace, MethodName, MethodSignature, ClrInstanceID, ReJITID) 0
#define FireEtwMethodJittingStarted(MethodID, ModuleID, MethodToken, MethodILSize, MethodNamespace, MethodName, MethodSignature) 0
#define FireEtwMethodJittingStarted_V1(MethodID, ModuleID, MethodToken, MethodILSize, MethodNamespace, MethodName, MethodSignature, ClrInstanceID) 0
#define FireEtwMethodJitInliningSucceeded(MethodBeingCompiledNamespace, MethodBeingCompiledName, MethodBeingCompiledNameSignature, InlinerNamespace, InlinerName, InlinerNameSignature, InlineeNamespace, InlineeName, InlineeNameSignature, ClrInstanceID) 0
#define FireEtwMethodJitInliningFailed(MethodBeingCompiledNamespace, MethodBeingCompiledName, MethodBeingCompiledNameSignature, InlinerNamespace, InlinerName, InlinerNameSignature, InlineeNamespace, InlineeName, InlineeNameSignature, FailAlways, FailReason, ClrInstanceID) 0
#define FireEtwMethodJitTailCallSucceeded(MethodBeingCompiledNamespace, MethodBeingCompiledName, MethodBeingCompiledNameSignature, CallerNamespace, CallerName, CallerNameSignature, CalleeNamespace, CalleeName, CalleeNameSignature, TailPrefix, TailCallType, ClrInstanceID) 0
#define FireEtwMethodJitTailCallFailed(MethodBeingCompiledNamespace, MethodBeingCompiledName, MethodBeingCompiledNameSignature, CallerNamespace, CallerName, CallerNameSignature, CalleeNamespace, CalleeName, CalleeNameSignature, TailPrefix, FailReason, ClrInstanceID) 0
#define FireEtwMethodILToNativeMap(MethodID, ReJITID, MethodExtent, CountOfMapEntries, ILOffsets, NativeOffsets, ClrInstanceID) 0
#define FireEtwModuleDCStartV2(ModuleID, AssemblyID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath) 0
#define FireEtwModuleDCEndV2(ModuleID, AssemblyID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath) 0
#define FireEtwDomainModuleLoad(ModuleID, AssemblyID, AppDomainID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath) 0
#define FireEtwDomainModuleLoad_V1(ModuleID, AssemblyID, AppDomainID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath, ClrInstanceID) 0
#define FireEtwModuleLoad(ModuleID, AssemblyID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath) 0
#define FireEtwModuleLoad_V1(ModuleID, AssemblyID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath, ClrInstanceID) 0
#define FireEtwModuleLoad_V2(ModuleID, AssemblyID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath, ClrInstanceID, ManagedPdbSignature, ManagedPdbAge, ManagedPdbBuildPath, NativePdbSignature, NativePdbAge, NativePdbBuildPath) 0
#define FireEtwModuleUnload(ModuleID, AssemblyID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath) 0
#define FireEtwModuleUnload_V1(ModuleID, AssemblyID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath, ClrInstanceID) 0
#define FireEtwModuleUnload_V2(ModuleID, AssemblyID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath, ClrInstanceID, ManagedPdbSignature, ManagedPdbAge, ManagedPdbBuildPath, NativePdbSignature, NativePdbAge, NativePdbBuildPath) 0
#define FireEtwAssemblyLoad(AssemblyID, AppDomainID, AssemblyFlags, FullyQualifiedAssemblyName) 0
#define FireEtwAssemblyLoad_V1(AssemblyID, AppDomainID, BindingID, AssemblyFlags, FullyQualifiedAssemblyName, ClrInstanceID) 0
#define FireEtwAssemblyUnload(AssemblyID, AppDomainID, AssemblyFlags, FullyQualifiedAssemblyName) 0
#define FireEtwAssemblyUnload_V1(AssemblyID, AppDomainID, BindingID, AssemblyFlags, FullyQualifiedAssemblyName, ClrInstanceID) 0
#define FireEtwAppDomainLoad(AppDomainID, AppDomainFlags, AppDomainName) 0
#define FireEtwAppDomainLoad_V1(AppDomainID, AppDomainFlags, AppDomainName, AppDomainIndex, ClrInstanceID) 0
#define FireEtwAppDomainUnload(AppDomainID, AppDomainFlags, AppDomainName) 0
#define FireEtwAppDomainUnload_V1(AppDomainID, AppDomainFlags, AppDomainName, AppDomainIndex, ClrInstanceID) 0
#define FireEtwModuleRangeLoad(ClrInstanceID, ModuleID, RangeBegin, RangeSize, RangeType) 0
#define FireEtwStrongNameVerificationStart(VerificationFlags, ErrorCode, FullyQualifiedAssemblyName) 0
#define FireEtwStrongNameVerificationStart_V1(VerificationFlags, ErrorCode, FullyQualifiedAssemblyName, ClrInstanceID) 0
#define FireEtwStrongNameVerificationStop(VerificationFlags, ErrorCode, FullyQualifiedAssemblyName) 0
#define FireEtwStrongNameVerificationStop_V1(VerificationFlags, ErrorCode, FullyQualifiedAssemblyName, ClrInstanceID) 0
#define FireEtwAuthenticodeVerificationStart(VerificationFlags, ErrorCode, ModulePath) 0
#define FireEtwAuthenticodeVerificationStart_V1(VerificationFlags, ErrorCode, ModulePath, ClrInstanceID) 0
#define FireEtwAuthenticodeVerificationStop(VerificationFlags, ErrorCode, ModulePath) 0
#define FireEtwAuthenticodeVerificationStop_V1(VerificationFlags, ErrorCode, ModulePath, ClrInstanceID) 0
#define FireEtwRuntimeInformationStart(ClrInstanceID, Sku, BclMajorVersion, BclMinorVersion, BclBuildNumber, BclQfeNumber, VMMajorVersion, VMMinorVersion, VMBuildNumber, VMQfeNumber, StartupFlags, StartupMode, CommandLine, ComObjectGuid, RuntimeDllPath) 0
#define FireEtwIncreaseMemoryPressure(BytesAllocated, ClrInstanceID) 0
#define FireEtwDecreaseMemoryPressure(BytesFreed, ClrInstanceID) 0
#define FireEtwGCMarkWithType(HeapNum, ClrInstanceID, Type, Bytes) 0
#define FireEtwGCJoin_V2(Heap, JoinTime, JoinType, ClrInstanceID, JoinID) 0
#define FireEtwGCPerHeapHistory_V3(ClrInstanceID, FreeListAllocated, FreeListRejected, EndOfSegAllocated, CondemnedAllocated, PinnedAllocated, PinnedAllocatedAdvance, RunningFreeListEfficiency, CondemnReasons0, CondemnReasons1, CompactMechanisms, ExpandMechanisms, HeapIndex, ExtraGen0Commit, Count, Values_Len_, Values) 0
#define FireEtwGCGlobalHeapHistory_V2(FinalYoungestDesired, NumHeaps, CondemnedGeneration, Gen0ReductionCount, Reason, GlobalMechanisms, ClrInstanceID, PauseMode, MemoryPressure) 0
#define FireEtwDebugIPCEventStart() 0
#define FireEtwDebugIPCEventEnd() 0
#define FireEtwDebugExceptionProcessingStart() 0
#define FireEtwDebugExceptionProcessingEnd() 0
#define FireEtwCodeSymbols(ModuleId, TotalChunks, ChunkNumber, ChunkLength, Chunk, ClrInstanceID) 0
#define FireEtwCLRStackWalkDCStart(ClrInstanceID, Reserved1, Reserved2, FrameCount, Stack) 0
#define FireEtwMethodDCStart(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags) 0
#define FireEtwMethodDCStart_V1(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, ClrInstanceID) 0
#define FireEtwMethodDCStart_V2(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, ClrInstanceID, ReJITID) 0
#define FireEtwMethodDCEnd(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags) 0
#define FireEtwMethodDCEnd_V1(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, ClrInstanceID) 0
#define FireEtwMethodDCEnd_V2(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, ClrInstanceID, ReJITID) 0
#define FireEtwMethodDCStartVerbose(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, MethodNamespace, MethodName, MethodSignature) 0
#define FireEtwMethodDCStartVerbose_V1(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, MethodNamespace, MethodName, MethodSignature, ClrInstanceID) 0
#define FireEtwMethodDCStartVerbose_V2(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, MethodNamespace, MethodName, MethodSignature, ClrInstanceID, ReJITID) 0
#define FireEtwMethodDCEndVerbose(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, MethodNamespace, MethodName, MethodSignature) 0
#define FireEtwMethodDCEndVerbose_V1(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, MethodNamespace, MethodName, MethodSignature, ClrInstanceID) 0
#define FireEtwMethodDCEndVerbose_V2(MethodID, ModuleID, MethodStartAddress, MethodSize, MethodToken, MethodFlags, MethodNamespace, MethodName, MethodSignature, ClrInstanceID, ReJITID) 0
#define FireEtwDCStartComplete() 0
#define FireEtwDCStartComplete_V1(ClrInstanceID) 0
#define FireEtwDCEndComplete() 0
#define FireEtwDCEndComplete_V1(ClrInstanceID) 0
#define FireEtwDCStartInit() 0
#define FireEtwDCStartInit_V1(ClrInstanceID) 0
#define FireEtwDCEndInit() 0
#define FireEtwDCEndInit_V1(ClrInstanceID) 0
#define FireEtwMethodDCStartILToNativeMap(MethodID, ReJITID, MethodExtent, CountOfMapEntries, ILOffsets, NativeOffsets, ClrInstanceID) 0
#define FireEtwMethodDCEndILToNativeMap(MethodID, ReJITID, MethodExtent, CountOfMapEntries, ILOffsets, NativeOffsets, ClrInstanceID) 0
#define FireEtwDomainModuleDCStart(ModuleID, AssemblyID, AppDomainID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath) 0
#define FireEtwDomainModuleDCStart_V1(ModuleID, AssemblyID, AppDomainID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath, ClrInstanceID) 0
#define FireEtwDomainModuleDCEnd(ModuleID, AssemblyID, AppDomainID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath) 0
#define FireEtwDomainModuleDCEnd_V1(ModuleID, AssemblyID, AppDomainID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath, ClrInstanceID) 0
#define FireEtwModuleDCStart(ModuleID, AssemblyID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath) 0
#define FireEtwModuleDCStart_V1(ModuleID, AssemblyID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath, ClrInstanceID) 0
#define FireEtwModuleDCStart_V2(ModuleID, AssemblyID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath, ClrInstanceID, ManagedPdbSignature, ManagedPdbAge, ManagedPdbBuildPath, NativePdbSignature, NativePdbAge, NativePdbBuildPath) 0
#define FireEtwModuleDCEnd(ModuleID, AssemblyID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath) 0
#define FireEtwModuleDCEnd_V1(ModuleID, AssemblyID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath, ClrInstanceID) 0
#define FireEtwModuleDCEnd_V2(ModuleID, AssemblyID, ModuleFlags, Reserved1, ModuleILPath, ModuleNativePath, ClrInstanceID, ManagedPdbSignature, ManagedPdbAge, ManagedPdbBuildPath, NativePdbSignature, NativePdbAge, NativePdbBuildPath) 0
#define FireEtwAssemblyDCStart(AssemblyID, AppDomainID, AssemblyFlags, FullyQualifiedAssemblyName) 0
#define FireEtwAssemblyDCStart_V1(AssemblyID, AppDomainID, BindingID, AssemblyFlags, FullyQualifiedAssemblyName, ClrInstanceID) 0
#define FireEtwAssemblyDCEnd(AssemblyID, AppDomainID, AssemblyFlags, FullyQualifiedAssemblyName) 0
#define FireEtwAssemblyDCEnd_V1(AssemblyID, AppDomainID, BindingID, AssemblyFlags, FullyQualifiedAssemblyName, ClrInstanceID) 0
#define FireEtwAppDomainDCStart(AppDomainID, AppDomainFlags, AppDomainName) 0
#define FireEtwAppDomainDCStart_V1(AppDomainID, AppDomainFlags, AppDomainName, AppDomainIndex, ClrInstanceID) 0
#define FireEtwAppDomainDCEnd(AppDomainID, AppDomainFlags, AppDomainName) 0
#define FireEtwAppDomainDCEnd_V1(AppDomainID, AppDomainFlags, AppDomainName, AppDomainIndex, ClrInstanceID) 0
#define FireEtwThreadDC(ManagedThreadID, AppDomainID, Flags, ManagedThreadIndex, OSThreadID, ClrInstanceID) 0
#define FireEtwModuleRangeDCStart(ClrInstanceID, ModuleID, RangeBegin, RangeSize, RangeType) 0
#define FireEtwModuleRangeDCEnd(ClrInstanceID, ModuleID, RangeBegin, RangeSize, RangeType) 0
#define FireEtwRuntimeInformationDCStart(ClrInstanceID, Sku, BclMajorVersion, BclMinorVersion, BclBuildNumber, BclQfeNumber, VMMajorVersion, VMMinorVersion, VMBuildNumber, VMQfeNumber, StartupFlags, StartupMode, CommandLine, ComObjectGuid, RuntimeDllPath) 0
#define FireEtwStressLogEvent(Facility, LogLevel, Message) 0
#define FireEtwStressLogEvent_V1(Facility, LogLevel, Message, ClrInstanceID) 0
#define FireEtwCLRStackWalkStress(ClrInstanceID, Reserved1, Reserved2, FrameCount, Stack) 0
#define FireEtwGCDecision(DoCompact) 0
#define FireEtwGCDecision_V1(DoCompact, ClrInstanceID) 0
#define FireEtwGCSettings(SegmentSize, LargeObjectSegmentSize, ServerGC) 0
#define FireEtwGCSettings_V1(SegmentSize, LargeObjectSegmentSize, ServerGC, ClrInstanceID) 0
#define FireEtwGCOptimized(DesiredAllocation, NewAllocation, GenerationNumber) 0
#define FireEtwGCOptimized_V1(DesiredAllocation, NewAllocation, GenerationNumber, ClrInstanceID) 0
#define FireEtwGCPerHeapHistory() 0
#define FireEtwGCPerHeapHistory_V1(ClrInstanceID) 0
#define FireEtwGCGlobalHeapHistory(FinalYoungestDesired, NumHeaps, CondemnedGeneration, Gen0ReductionCount, Reason, GlobalMechanisms) 0
#define FireEtwGCGlobalHeapHistory_V1(FinalYoungestDesired, NumHeaps, CondemnedGeneration, Gen0ReductionCount, Reason, GlobalMechanisms, ClrInstanceID) 0
#define FireEtwGCJoin(Heap, JoinTime, JoinType) 0
#define FireEtwGCJoin_V1(Heap, JoinTime, JoinType, ClrInstanceID) 0
#define FireEtwPrvGCMarkStackRoots(HeapNum) 0
#define FireEtwPrvGCMarkStackRoots_V1(HeapNum, ClrInstanceID) 0
#define FireEtwPrvGCMarkFinalizeQueueRoots(HeapNum) 0
#define FireEtwPrvGCMarkFinalizeQueueRoots_V1(HeapNum, ClrInstanceID) 0
#define FireEtwPrvGCMarkHandles(HeapNum) 0
#define FireEtwPrvGCMarkHandles_V1(HeapNum, ClrInstanceID) 0
#define FireEtwPrvGCMarkCards(HeapNum) 0
#define FireEtwPrvGCMarkCards_V1(HeapNum, ClrInstanceID) 0
#define FireEtwBGCBegin(ClrInstanceID) 0
#define FireEtwBGC1stNonConEnd(ClrInstanceID) 0
#define FireEtwBGC1stConEnd(ClrInstanceID) 0
#define FireEtwBGC2ndNonConBegin(ClrInstanceID) 0
#define FireEtwBGC2ndNonConEnd(ClrInstanceID) 0
#define FireEtwBGC2ndConBegin(ClrInstanceID) 0
#define FireEtwBGC2ndConEnd(ClrInstanceID) 0
#define FireEtwBGCPlanEnd(ClrInstanceID) 0
#define FireEtwBGCSweepEnd(ClrInstanceID) 0
#define FireEtwBGCDrainMark(Objects, ClrInstanceID) 0
#define FireEtwBGCRevisit(Pages, Objects, IsLarge, ClrInstanceID) 0
#define FireEtwBGCOverflow(Min, Max, Objects, IsLarge, ClrInstanceID) 0
#define FireEtwBGCAllocWaitBegin(Reason, ClrInstanceID) 0
#define FireEtwBGCAllocWaitEnd(Reason, ClrInstanceID) 0
#define FireEtwGCFullNotify(GenNumber, IsAlloc) 0
#define FireEtwGCFullNotify_V1(GenNumber, IsAlloc, ClrInstanceID) 0
#define FireEtwEEStartupStart() 0
#define FireEtwEEStartupStart_V1(ClrInstanceID) 0
#define FireEtwEEStartupEnd() 0
#define FireEtwEEStartupEnd_V1(ClrInstanceID) 0
#define FireEtwEEConfigSetup() 0
#define FireEtwEEConfigSetup_V1(ClrInstanceID) 0
#define FireEtwEEConfigSetupEnd() 0
#define FireEtwEEConfigSetupEnd_V1(ClrInstanceID) 0
#define FireEtwLdSysBases() 0
#define FireEtwLdSysBases_V1(ClrInstanceID) 0
#define FireEtwLdSysBasesEnd() 0
#define FireEtwLdSysBasesEnd_V1(ClrInstanceID) 0
#define FireEtwExecExe() 0
#define FireEtwExecExe_V1(ClrInstanceID) 0
#define FireEtwExecExeEnd() 0
#define FireEtwExecExeEnd_V1(ClrInstanceID) 0
#define FireEtwMain() 0
#define FireEtwMain_V1(ClrInstanceID) 0
#define FireEtwMainEnd() 0
#define FireEtwMainEnd_V1(ClrInstanceID) 0
#define FireEtwApplyPolicyStart() 0
#define FireEtwApplyPolicyStart_V1(ClrInstanceID) 0
#define FireEtwApplyPolicyEnd() 0
#define FireEtwApplyPolicyEnd_V1(ClrInstanceID) 0
#define FireEtwLdLibShFolder() 0
#define FireEtwLdLibShFolder_V1(ClrInstanceID) 0
#define FireEtwLdLibShFolderEnd() 0
#define FireEtwLdLibShFolderEnd_V1(ClrInstanceID) 0
#define FireEtwPrestubWorker() 0
#define FireEtwPrestubWorker_V1(ClrInstanceID) 0
#define FireEtwPrestubWorkerEnd() 0
#define FireEtwPrestubWorkerEnd_V1(ClrInstanceID) 0
#define FireEtwGetInstallationStart() 0
#define FireEtwGetInstallationStart_V1(ClrInstanceID) 0
#define FireEtwGetInstallationEnd() 0
#define FireEtwGetInstallationEnd_V1(ClrInstanceID) 0
#define FireEtwOpenHModule() 0
#define FireEtwOpenHModule_V1(ClrInstanceID) 0
#define FireEtwOpenHModuleEnd() 0
#define FireEtwOpenHModuleEnd_V1(ClrInstanceID) 0
#define FireEtwExplicitBindStart() 0
#define FireEtwExplicitBindStart_V1(ClrInstanceID) 0
#define FireEtwExplicitBindEnd() 0
#define FireEtwExplicitBindEnd_V1(ClrInstanceID) 0
#define FireEtwParseXml() 0
#define FireEtwParseXml_V1(ClrInstanceID) 0
#define FireEtwParseXmlEnd() 0
#define FireEtwParseXmlEnd_V1(ClrInstanceID) 0
#define FireEtwInitDefaultDomain() 0
#define FireEtwInitDefaultDomain_V1(ClrInstanceID) 0
#define FireEtwInitDefaultDomainEnd() 0
#define FireEtwInitDefaultDomainEnd_V1(ClrInstanceID) 0
#define FireEtwInitSecurity() 0
#define FireEtwInitSecurity_V1(ClrInstanceID) 0
#define FireEtwInitSecurityEnd() 0
#define FireEtwInitSecurityEnd_V1(ClrInstanceID) 0
#define FireEtwAllowBindingRedirs() 0
#define FireEtwAllowBindingRedirs_V1(ClrInstanceID) 0
#define FireEtwAllowBindingRedirsEnd() 0
#define FireEtwAllowBindingRedirsEnd_V1(ClrInstanceID) 0
#define FireEtwEEConfigSync() 0
#define FireEtwEEConfigSync_V1(ClrInstanceID) 0
#define FireEtwEEConfigSyncEnd() 0
#define FireEtwEEConfigSyncEnd_V1(ClrInstanceID) 0
#define FireEtwFusionBinding() 0
#define FireEtwFusionBinding_V1(ClrInstanceID) 0
#define FireEtwFusionBindingEnd() 0
#define FireEtwFusionBindingEnd_V1(ClrInstanceID) 0
#define FireEtwLoaderCatchCall() 0
#define FireEtwLoaderCatchCall_V1(ClrInstanceID) 0
#define FireEtwLoaderCatchCallEnd() 0
#define FireEtwLoaderCatchCallEnd_V1(ClrInstanceID) 0
#define FireEtwFusionInit() 0
#define FireEtwFusionInit_V1(ClrInstanceID) 0
#define FireEtwFusionInitEnd() 0
#define FireEtwFusionInitEnd_V1(ClrInstanceID) 0
#define FireEtwFusionAppCtx() 0
#define FireEtwFusionAppCtx_V1(ClrInstanceID) 0
#define FireEtwFusionAppCtxEnd() 0
#define FireEtwFusionAppCtxEnd_V1(ClrInstanceID) 0
#define FireEtwFusion2EE() 0
#define FireEtwFusion2EE_V1(ClrInstanceID) 0
#define FireEtwFusion2EEEnd() 0
#define FireEtwFusion2EEEnd_V1(ClrInstanceID) 0
#define FireEtwSecurityCatchCall() 0
#define FireEtwSecurityCatchCall_V1(ClrInstanceID) 0
#define FireEtwSecurityCatchCallEnd() 0
#define FireEtwSecurityCatchCallEnd_V1(ClrInstanceID) 0
#define FireEtwCLRStackWalkPrivate(ClrInstanceID, Reserved1, Reserved2, FrameCount, Stack) 0
#define FireEtwModuleRangeLoadPrivate(ClrInstanceID, ModuleID, RangeBegin, RangeSize, RangeType, IBCType, SectionType) 0
#define FireEtwBindingPolicyPhaseStart(AppDomainID, LoadContextID, FromLoaderCache, DynamicLoad, AssemblyCodebase, AssemblyName, ClrInstanceID) 0
#define FireEtwBindingPolicyPhaseEnd(AppDomainID, LoadContextID, FromLoaderCache, DynamicLoad, AssemblyCodebase, AssemblyName, ClrInstanceID) 0
#define FireEtwBindingNgenPhaseStart(AppDomainID, LoadContextID, FromLoaderCache, DynamicLoad, AssemblyCodebase, AssemblyName, ClrInstanceID) 0
#define FireEtwBindingNgenPhaseEnd(AppDomainID, LoadContextID, FromLoaderCache, DynamicLoad, AssemblyCodebase, AssemblyName, ClrInstanceID) 0
#define FireEtwBindingLookupAndProbingPhaseStart(AppDomainID, LoadContextID, FromLoaderCache, DynamicLoad, AssemblyCodebase, AssemblyName, ClrInstanceID) 0
#define FireEtwBindingLookupAndProbingPhaseEnd(AppDomainID, LoadContextID, FromLoaderCache, DynamicLoad, AssemblyCodebase, AssemblyName, ClrInstanceID) 0
#define FireEtwLoaderPhaseStart(AppDomainID, LoadContextID, FromLoaderCache, DynamicLoad, AssemblyCodebase, AssemblyName, ClrInstanceID) 0
#define FireEtwLoaderPhaseEnd(AppDomainID, LoadContextID, FromLoaderCache, DynamicLoad, AssemblyCodebase, AssemblyName, ClrInstanceID) 0
#define FireEtwBindingPhaseStart(AppDomainID, LoadContextID, FromLoaderCache, DynamicLoad, AssemblyCodebase, AssemblyName, ClrInstanceID) 0
#define FireEtwBindingPhaseEnd(AppDomainID, LoadContextID, FromLoaderCache, DynamicLoad, AssemblyCodebase, AssemblyName, ClrInstanceID) 0
#define FireEtwBindingDownloadPhaseStart(AppDomainID, LoadContextID, FromLoaderCache, DynamicLoad, AssemblyCodebase, AssemblyName, ClrInstanceID) 0
#define FireEtwBindingDownloadPhaseEnd(AppDomainID, LoadContextID, FromLoaderCache, DynamicLoad, AssemblyCodebase, AssemblyName, ClrInstanceID) 0
#define FireEtwLoaderAssemblyInitPhaseStart(AppDomainID, LoadContextID, FromLoaderCache, DynamicLoad, AssemblyCodebase, AssemblyName, ClrInstanceID) 0
#define FireEtwLoaderAssemblyInitPhaseEnd(AppDomainID, LoadContextID, FromLoaderCache, DynamicLoad, AssemblyCodebase, AssemblyName, ClrInstanceID) 0
#define FireEtwLoaderMappingPhaseStart(AppDomainID, LoadContextID, FromLoaderCache, DynamicLoad, AssemblyCodebase, AssemblyName, ClrInstanceID) 0
#define FireEtwLoaderMappingPhaseEnd(AppDomainID, LoadContextID, FromLoaderCache, DynamicLoad, AssemblyCodebase, AssemblyName, ClrInstanceID) 0
#define FireEtwLoaderDeliverEventsPhaseStart(AppDomainID, LoadContextID, FromLoaderCache, DynamicLoad, AssemblyCodebase, AssemblyName, ClrInstanceID) 0
#define FireEtwLoaderDeliverEventsPhaseEnd(AppDomainID, LoadContextID, FromLoaderCache, DynamicLoad, AssemblyCodebase, AssemblyName, ClrInstanceID) 0
#define FireEtwEvidenceGenerated(Type, AppDomain, ILImage, ClrInstanceID) 0
#define FireEtwModuleTransparencyComputationStart(Module, AppDomainID, ClrInstanceID) 0
#define FireEtwModuleTransparencyComputationEnd(Module, AppDomainID, IsAllCritical, IsAllTransparent, IsTreatAsSafe, IsOpportunisticallyCritical, SecurityRuleSet, ClrInstanceID) 0
#define FireEtwTypeTransparencyComputationStart(Type, Module, AppDomainID, ClrInstanceID) 0
#define FireEtwTypeTransparencyComputationEnd(Type, Module, AppDomainID, IsAllCritical, IsAllTransparent, IsCritical, IsTreatAsSafe, ClrInstanceID) 0
#define FireEtwMethodTransparencyComputationStart(Method, Module, AppDomainID, ClrInstanceID) 0
#define FireEtwMethodTransparencyComputationEnd(Method, Module, AppDomainID, IsCritical, IsTreatAsSafe, ClrInstanceID) 0
#define FireEtwFieldTransparencyComputationStart(Field, Module, AppDomainID, ClrInstanceID) 0
#define FireEtwFieldTransparencyComputationEnd(Field, Module, AppDomainID, IsCritical, IsTreatAsSafe, ClrInstanceID) 0
#define FireEtwTokenTransparencyComputationStart(Token, Module, AppDomainID, ClrInstanceID) 0
#define FireEtwTokenTransparencyComputationEnd(Token, Module, AppDomainID, IsCritical, IsTreatAsSafe, ClrInstanceID) 0
#define FireEtwNgenBindEvent(ClrInstanceID, BindingID, ReasonCode, AssemblyName) 0
#define FireEtwFailFast(FailFastUserMessage, FailedEIP, OSExitCode, ClrExitCode, ClrInstanceID) 0
#define FireEtwPrvFinalizeObject(TypeID, ObjectID, ClrInstanceID, TypeName) 0
#define FireEtwCCWRefCountChange(HandleID, ObjectID, COMInterfacePointer, NewRefCount, AppDomainID, ClassName, NameSpace, Operation, ClrInstanceID) 0
#define FireEtwPrvSetGCHandle(HandleID, ObjectID, Kind, Generation, ClrInstanceID) 0
#define FireEtwPrvDestroyGCHandle(HandleID, ClrInstanceID) 0
#define FireEtwFusionMessageEvent(ClrInstanceID, Prepend, Message) 0
#define FireEtwFusionErrorCodeEvent(ClrInstanceID, Category, ErrorCode) 0
#define FireEtwPinPlugAtGCTime(PlugStart, PlugEnd, GapBeforeSize, ClrInstanceID) 0
#define FireEtwAllocRequest(LoaderHeapPtr, MemoryAddress, RequestSize, Unused1, Unused2, ClrInstanceID) 0
#define FireEtwMulticoreJit(ClrInstanceID, String1, String2, Int1, Int2, Int3) 0
#define FireEtwMulticoreJitMethodCodeReturned(ClrInstanceID, ModuleID, MethodID) 0
#define FireEtwIInspectableRuntimeClassName(TypeName, ClrInstanceID) 0
#define FireEtwWinRTUnbox(TypeName, SecondTypeName, ClrInstanceID) 0
#define FireEtwCreateRCW(TypeName, ClrInstanceID) 0
#define FireEtwRCWVariance(TypeName, InterfaceTypeName, VariantInterfaceTypeName, ClrInstanceID) 0
#define FireEtwRCWIEnumerableCasting(TypeName, SecondTypeName, ClrInstanceID) 0
#define FireEtwCreateCCW(TypeName, ClrInstanceID) 0
#define FireEtwCCWVariance(TypeName, InterfaceTypeName, VariantInterfaceTypeName, ClrInstanceID) 0
#define FireEtwObjectVariantMarshallingToNative(TypeName, Int1, ClrInstanceID) 0
#define FireEtwGetTypeFromGUID(TypeName, SecondTypeName, ClrInstanceID) 0
#define FireEtwGetTypeFromProgID(TypeName, SecondTypeName, ClrInstanceID) 0
#define FireEtwConvertToCallbackEtw(TypeName, SecondTypeName, ClrInstanceID) 0
#define FireEtwBeginCreateManagedReference(ClrInstanceID) 0
#define FireEtwEndCreateManagedReference(ClrInstanceID) 0
#define FireEtwObjectVariantMarshallingToManaged(TypeName, Int1, ClrInstanceID) 0
