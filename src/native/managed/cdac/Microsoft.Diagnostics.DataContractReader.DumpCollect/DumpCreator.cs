// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;
using ContractModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader.DumpCollect;

internal sealed class DumpCreator(
    Target target,
    bool includeHeap,
    MemoryRegionEmitter emitter)
{
    private const int MaxThreads = 1_000_000;

    private readonly Target _target = target;
    private readonly bool _includeHeap = includeHeap;
    private readonly MemoryRegionEmitter _emitter = emitter;
    private readonly HashSet<TargetPointer> _loaderAllocators = [];
    private readonly Dictionary<TargetPointer, string> _miniMetadataNames = [];
    private readonly HashSet<TargetPointer> _visitedObjects = [];

    public void EnumerateMemoryRegions()
    {
        TryEnumerate("modules", EnumerateModules);
        TryEnumerate("threads", EnumerateThreads);

        if (_includeHeap)
        {
            TryEnumerate("GC", EnumerateGC);
            TryEnumerate("code and loader heaps", EnumerateCodeAndLoaderHeaps);
            TryEnumerate("sync blocks", EnumerateSyncBlocks);
            TryEnumerate("stress log", EnumerateStressLog);
        }

        TryEnumerate("mini metadata", () => MiniMetadataWriter.Write(_target, _emitter, _miniMetadataNames));
    }

    private void TryEnumerate(string phase, Action enumerate)
    {
        DumpCollectLogger.Log($"Starting {phase} enumeration.");
        _emitter.BeginPhase(phase);
        try
        {
            enumerate();
            DumpCollectLogger.Log($"Completed {phase} enumeration.");
        }
        catch (System.Exception ex) when (ex.HResult != HResults.COR_E_OPERATIONCANCELED)
        {
            DumpCollectLogger.LogException(phase, ex);
            // Dump creation is best-effort because the target may be partially unreadable or corrupt.
        }
        finally
        {
            _emitter.EndPhase();
        }
    }

    private void EnumerateModules()
    {
        ILoader loader = _target.Contracts.Loader;
        IEcmaMetadata ecmaMetadata = _target.Contracts.EcmaMetadata;

        TargetPointer appDomain = loader.GetAppDomain();
        IEnumerable<ContractModuleHandle> modules = loader.GetModuleHandles(
            appDomain,
            AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution);

        foreach (ContractModuleHandle module in modules)
        {
            loader.GetModule(module);
            TargetPointer peAssembly = loader.GetPEAssembly(module);
            loader.GetFlags(module);
            loader.GetSimpleName(module);
            loader.GetPath(module);
            loader.GetFileName(module);
            loader.TryGetLoadedImageContents(module, out _, out _, out _);

            if (loader.TryGetSymbolStream(module, out TargetPointer symbolBuffer, out uint symbolSize))
                _emitter.Add(symbolBuffer.Value, symbolSize);

            if (peAssembly != TargetPointer.Null)
                ecmaMetadata.HasReadWriteMetadata(peAssembly);

            TargetPointer loaderAllocator = loader.GetLoaderAllocator(module);
            if (loaderAllocator != TargetPointer.Null)
                _loaderAllocators.Add(loaderAllocator);
        }

        TargetPointer globalLoaderAllocator = loader.GetGlobalLoaderAllocator();
        if (globalLoaderAllocator != TargetPointer.Null)
            _loaderAllocators.Add(globalLoaderAllocator);
    }

    private void EnumerateThreads()
    {
        IThread thread = _target.Contracts.Thread;
        ThreadStoreData threadStore = thread.GetThreadStoreData();
        ThreadStoreCounts threadCounts = thread.GetThreadCounts();
        DumpCollectLogger.Log(
            $"Thread store: count={threadStore.ThreadCount}, first=0x{threadStore.FirstThread.Value:x}, finalizer=0x{threadStore.FinalizerThread.Value:x}, gc=0x{threadStore.GCThread.Value:x}.");
        DumpCollectLogger.Log(
            $"Thread counts: unstarted={threadCounts.UnstartedThreadCount}, background={threadCounts.BackgroundThreadCount}, pending={threadCounts.PendingThreadCount}, dead={threadCounts.DeadThreadCount}.");
        HashSet<TargetPointer> visited = [];
        TargetPointer threadAddress = threadStore.FirstThread;
        int enumeratedThreadCount = 0;

        for (int i = 0; threadAddress != TargetPointer.Null && i < MaxThreads; i++)
        {
            if (!visited.Add(threadAddress))
                break;

            ThreadData threadData = thread.GetThreadData(threadAddress);
            thread.GetThreadAllocContext(threadAddress, out _, out _);
            DumpCollectLogger.Log(
                $"Thread: address=0x{threadAddress.Value:x}, id={threadData.Id}, osId=0x{threadData.OSId.Value:x}, next=0x{threadData.NextThread.Value:x}.");
            EnumerateStack(threadData);
            EnumerateThreadException(threadData);

            enumeratedThreadCount++;
            threadAddress = threadData.NextThread;
        }

        DumpCollectLogger.Log($"Completed thread list enumeration: threads={enumeratedThreadCount}.");
    }

    private void EnumerateStack(ThreadData threadData)
    {
        IStackWalk stackWalk = _target.Contracts.StackWalk;
        int frameCount = 0;
        foreach (IStackDataFrameHandle frame in stackWalk.CreateStackWalk(threadData))
        {
            frameCount++;
            TargetCodePointer instructionPointer = stackWalk.GetInstructionPointer(frame);
            TargetPointer methodDesc = stackWalk.GetMethodDescPtr(frame);
            CaptureMethod(methodDesc);
            DumpCollectLogger.Log(
                $"Thread {threadData.Id} frame {frameCount}: ip=0x{instructionPointer.Value:x}, methodDesc=0x{methodDesc.Value:x}.");
        }

        DumpCollectLogger.Log(
            $"Thread {threadData.Id} stack enumeration completed: frames={frameCount}.");
    }

    private void EnumerateThreadException(ThreadData threadData)
    {
        if (threadData.LastThrownObjectHandle == TargetPointer.Null)
            return;

        try
        {
            TargetPointer exceptionObject = _target.ReadPointer(threadData.LastThrownObjectHandle.Value);
            if (exceptionObject == TargetPointer.Null)
                return;

            DumpCollectLogger.Log(
                $"Thread {threadData.Id} exception: handle=0x{threadData.LastThrownObjectHandle.Value:x}, object=0x{exceptionObject.Value:x}.");
            EnumerateExceptionObject(exceptionObject);
        }
        catch (System.Exception ex)
        {
            DumpCollectLogger.LogException($"thread {threadData.Id} exception enumeration", ex);
        }
    }

    private void EnumerateObject(TargetPointer objectAddress)
    {
        if (objectAddress == TargetPointer.Null || _visitedObjects.Contains(objectAddress))
            return;

        try
        {
            IObject objectContract = _target.Contracts.Object;
            ulong size = objectContract.GetSize(objectAddress);
            if (size == 0 || size > 64 * 1024 * 1024)
                return;

            _emitter.Add(objectAddress.Value, size);

            TargetPointer methodTable = objectContract.GetMethodTableAddress(objectAddress);
            EnumerateObjectDataDependencies(objectContract, objectAddress, methodTable);
            TargetPointer exceptionMethodTable =
                _target.Contracts.RuntimeTypeSystem.GetWellKnownMethodTable(WellKnownMethodTable.Exception);
            ITypeHandle typeHandle = _target.Contracts.RuntimeTypeSystem.GetTypeHandle(methodTable);
            while (typeHandle.Address != TargetPointer.Null)
            {
                if (typeHandle.Address == exceptionMethodTable)
                {
                    EnumerateExceptionObject(objectAddress);
                    return;
                }

                TargetPointer parentMethodTable =
                    _target.Contracts.RuntimeTypeSystem.GetParentMethodTable(typeHandle);
                if (parentMethodTable == TargetPointer.Null)
                    break;

                typeHandle = _target.Contracts.RuntimeTypeSystem.GetTypeHandle(parentMethodTable);
            }

            _visitedObjects.Add(objectAddress);
        }
        catch (System.Exception ex)
        {
            DumpCollectLogger.LogException($"object 0x{objectAddress.Value:x} enumeration", ex);
        }
    }

    private void EnumerateExceptionObject(TargetPointer exceptionObject)
    {
        if (!_visitedObjects.Add(exceptionObject))
            return;

        IObject objectContract = _target.Contracts.Object;
        ulong objectSize = objectContract.GetSize(exceptionObject);
        _emitter.Add(exceptionObject.Value, objectSize);
        IRuntimeTypeSystem runtimeTypeSystem = _target.Contracts.RuntimeTypeSystem;
        TargetPointer methodTable = objectContract.GetMethodTableAddress(exceptionObject);
        EnumerateObjectDataDependencies(objectContract, exceptionObject, methodTable);
        ITypeHandle typeHandle = runtimeTypeSystem.GetTypeHandle(methodTable);
        runtimeTypeSystem.GetBaseSize(typeHandle);
        runtimeTypeSystem.GetComponentSize(typeHandle);
        runtimeTypeSystem.IsFreeObjectMethodTable(typeHandle);
        runtimeTypeSystem.GetWellKnownMethodTable(WellKnownMethodTable.String);
        runtimeTypeSystem.GetWellKnownMethodTable(WellKnownMethodTable.Object);
        runtimeTypeSystem.IsArray(typeHandle, out _);

        ITypeHandle currentType = typeHandle;
        while (currentType.Address != TargetPointer.Null)
        {
            EnumerateMethodTableDataDependencies(runtimeTypeSystem, currentType);
            TargetPointer parentMethodTable = runtimeTypeSystem.GetParentMethodTable(currentType);
            if (parentMethodTable == TargetPointer.Null)
                break;

            currentType = runtimeTypeSystem.GetTypeHandle(parentMethodTable);
        }

        if (_target.Contracts.FeatureFlags.IsEnabled(RuntimeFeature.COMInterop))
            objectContract.GetBuiltInComData(exceptionObject, out _, out _, out _);

        IException exceptionContract = _target.Contracts.Exception;
        ExceptionData exceptionData = exceptionContract.GetExceptionData(exceptionObject);
        EnumerateObject(exceptionData.Message);
        EnumerateObject(exceptionData.StackTrace);
        EnumerateObject(exceptionData.WatsonBuckets);
        EnumerateObject(exceptionData.StackTraceString);
        EnumerateObject(exceptionData.RemoteStackTraceString);

        foreach (ExceptionStackFrameInfo frame in exceptionContract.GetExceptionStackFrames(exceptionObject))
            CaptureMethod(frame.MethodDesc);

        if (exceptionData.InnerException != TargetPointer.Null)
            EnumerateExceptionObject(exceptionData.InnerException);
    }

    private static void EnumerateMethodTableDataDependencies(
        IRuntimeTypeSystem runtimeTypeSystem,
        ITypeHandle typeHandle)
    {
        runtimeTypeSystem.GetBaseSize(typeHandle);
        runtimeTypeSystem.GetComponentSize(typeHandle);
        if (runtimeTypeSystem.IsFreeObjectMethodTable(typeHandle))
            return;

        runtimeTypeSystem.GetModule(typeHandle);
        runtimeTypeSystem.GetCanonicalMethodTable(typeHandle);
        runtimeTypeSystem.GetParentMethodTable(typeHandle);
        runtimeTypeSystem.GetNumInterfaces(typeHandle);
        runtimeTypeSystem.GetNumMethods(typeHandle);
        runtimeTypeSystem.GetTypeDefToken(typeHandle);
        runtimeTypeSystem.GetTypeDefTypeAttributes(typeHandle);
        runtimeTypeSystem.ContainsGCPointers(typeHandle);
        runtimeTypeSystem.IsDynamicStatics(typeHandle);
    }

    private void EnumerateObjectDataDependencies(
        IObject objectContract,
        TargetPointer objectAddress,
        TargetPointer methodTable)
    {
        objectContract.GetSyncBlockAddress(objectAddress);

        IRuntimeTypeSystem runtimeTypeSystem = _target.Contracts.RuntimeTypeSystem;
        ITypeHandle typeHandle = runtimeTypeSystem.GetTypeHandle(methodTable);
        runtimeTypeSystem.GetBaseSize(typeHandle);
        runtimeTypeSystem.GetComponentSize(typeHandle);

        if (runtimeTypeSystem.IsFreeObjectMethodTable(typeHandle))
            return;

        if (methodTable == runtimeTypeSystem.GetWellKnownMethodTable(WellKnownMethodTable.String))
        {
            objectContract.GetStringValue(objectAddress);
            return;
        }

        runtimeTypeSystem.GetWellKnownMethodTable(WellKnownMethodTable.Object);
        if (runtimeTypeSystem.IsArray(typeHandle, out _))
        {
            objectContract.GetArrayData(objectAddress, out _, out _, out _, out _, out _);
            ITypeHandle elementType = runtimeTypeSystem.GetTypeParam(typeHandle);
            runtimeTypeSystem.GetSignatureCorElementType(elementType);
            while (runtimeTypeSystem.IsArray(elementType, out _))
                elementType = runtimeTypeSystem.GetTypeParam(elementType);
        }

        if (_target.Contracts.FeatureFlags.IsEnabled(RuntimeFeature.COMInterop))
            objectContract.GetBuiltInComData(objectAddress, out _, out _, out _);
    }

    private void CaptureMethod(TargetPointer methodDesc)
    {
        EnumerateMethodDependencies(methodDesc);
        CacheMethodName(methodDesc);
    }

    private void EnumerateMethodDependencies(TargetPointer methodDesc)
    {
        if (methodDesc == TargetPointer.Null)
            return;

        IRuntimeTypeSystem runtimeTypeSystem = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle methodDescHandle = runtimeTypeSystem.GetMethodDescHandle(methodDesc);
        if (runtimeTypeSystem.IsNoMetadataMethod(methodDescHandle, out _))
            return;

        runtimeTypeSystem.GetMethodToken(methodDescHandle);
        TargetPointer methodTable = runtimeTypeSystem.GetMethodTable(methodDescHandle);
        TargetPointer module = runtimeTypeSystem.GetModule(runtimeTypeSystem.GetTypeHandle(methodTable));
        ContractModuleHandle moduleHandle = _target.Contracts.Loader.GetModuleHandleFromModulePtr(module);
        _target.Contracts.Loader.GetPath(moduleHandle);
    }

    private void CacheMethodName(TargetPointer methodDesc)
    {
        if (methodDesc == TargetPointer.Null || _miniMetadataNames.ContainsKey(methodDesc))
            return;

        IRuntimeTypeSystem runtimeTypeSystem = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle methodDescHandle = runtimeTypeSystem.GetMethodDescHandle(methodDesc);
        if (runtimeTypeSystem.IsNoMetadataMethod(methodDescHandle, out _)
            && !runtimeTypeSystem.IsILStub(methodDescHandle))
        {
            return;
        }

        try
        {
            using (_emitter.SuppressTargetReadEmission())
            {
                string? name = ResolveMethodName(methodDescHandle);
                if (name is not null)
                    _miniMetadataNames.Add(methodDesc, name);
            }
        }
        catch (System.Exception ex)
        {
            DumpCollectLogger.LogException(
                $"method name 0x{methodDesc.Value:x} collection",
                ex);
        }
    }

    private string? ResolveMethodName(MethodDescHandle methodDesc)
    {
        IRuntimeTypeSystem runtimeTypeSystem = _target.Contracts.RuntimeTypeSystem;
        uint token = runtimeTypeSystem.GetMethodToken(methodDesc);
        TargetPointer methodTable = runtimeTypeSystem.GetMethodTable(methodDesc);
        TargetPointer module = runtimeTypeSystem.GetModule(runtimeTypeSystem.GetTypeHandle(methodTable));
        ContractModuleHandle moduleHandle = _target.Contracts.Loader.GetModuleHandleFromModulePtr(module);
        MetadataReader? metadataReader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle);
        if (metadataReader is null)
            return null;

        MethodDefinitionHandle methodDefinitionHandle =
            MetadataTokens.MethodDefinitionHandle((int)(token & 0x00FFFFFF));
        MethodDefinition methodDefinition = metadataReader.GetMethodDefinition(methodDefinitionHandle);
        TypeDefinitionHandle typeDefinitionHandle = methodDefinition.GetDeclaringType();
        Stack<string> declaringTypes = [];
        string typeNamespace = string.Empty;

        while (!typeDefinitionHandle.IsNil)
        {
            TypeDefinition typeDefinition = metadataReader.GetTypeDefinition(typeDefinitionHandle);
            declaringTypes.Push(metadataReader.GetString(typeDefinition.Name));
            if (typeDefinition.GetDeclaringType().IsNil)
                typeNamespace = metadataReader.GetString(typeDefinition.Namespace);

            typeDefinitionHandle = typeDefinition.GetDeclaringType();
        }

        StringBuilder name = new();
        if (!string.IsNullOrEmpty(typeNamespace))
        {
            name.Append(typeNamespace);
            name.Append('.');
        }

        name.AppendJoin('+', declaringTypes);
        name.Append('.');
        name.Append(metadataReader.GetString(methodDefinition.Name));
        name.Append("()");
        return name.ToString();
    }

    private void EnumerateGC()
    {
        IGC gc = _target.Contracts.GC;
        if (!gc.GetGCStructuresValid())
            return;

        foreach ((GCHeapSegmentInfo segment, _) in gc.EnumerateAllSegments())
        {
            if (segment.End > segment.Start)
                _emitter.Add(segment.Start.Value, segment.End.Value - segment.Start.Value);
        }

        AddGCMemoryRegions(gc.GetHandleTableMemoryRegions());
        AddGCMemoryRegions(gc.GetGCBookkeepingMemoryRegions());
        AddGCMemoryRegions(gc.GetGCFreeRegions());
    }

    private void AddGCMemoryRegions(IReadOnlyList<GCMemoryRegionData> regions)
    {
        foreach (GCMemoryRegionData region in regions)
            _emitter.Add(region.Start.Value, region.Size);
    }

    private void EnumerateCodeAndLoaderHeaps()
    {
        IExecutionManager executionManager = _target.Contracts.ExecutionManager;
        foreach (ICodeHeapInfo codeHeap in executionManager.GetCodeHeapInfos())
        {
            switch (codeHeap)
            {
                case HostCodeHeapInfo host when host.CurrentAddress > host.BaseAddress:
                    _emitter.Add(host.BaseAddress.Value, host.CurrentAddress.Value - host.BaseAddress.Value);
                    break;
                case LoaderCodeHeapInfo loader:
                    AddLoaderHeapBlocks(loader.LoaderHeapAddress);
                    break;
            }
        }

        ILoader loaderContract = _target.Contracts.Loader;
        foreach (TargetPointer loaderAllocator in _loaderAllocators)
        {
            IReadOnlyDictionary<LoaderAllocatorHeapType, TargetPointer> heaps =
                loaderContract.GetLoaderAllocatorHeaps(loaderAllocator);
            foreach (TargetPointer loaderHeap in heaps.Values)
                AddLoaderHeapBlocks(loaderHeap);
        }
    }

    private void AddLoaderHeapBlocks(TargetPointer loaderHeap)
    {
        if (loaderHeap == TargetPointer.Null)
            return;

        ILoader loader = _target.Contracts.Loader;
        foreach (LoaderHeapBlock block in loader.EnumerateLoaderHeapBlocks(loaderHeap))
            _emitter.Add(block.Address.Value, block.Size.Value);
    }

    private void EnumerateSyncBlocks()
    {
        ISyncBlock syncBlock = _target.Contracts.SyncBlock;
        uint count = syncBlock.GetSyncBlockCount();
        for (uint i = 1; i <= count; i++)
        {
            TargetPointer address = syncBlock.GetSyncBlock(i);
            if (address == TargetPointer.Null || syncBlock.IsSyncBlockFree(i))
                continue;

            syncBlock.GetSyncBlockObject(i);
            syncBlock.TryGetLockInfo(address, out _, out _);
            syncBlock.GetAdditionalThreadCount(address);
            syncBlock.GetBuiltInComData(address, out _, out _, out _);
        }

        TargetPointer cleanup = syncBlock.GetSyncBlockFromCleanupList();
        while (cleanup != TargetPointer.Null)
            cleanup = syncBlock.GetNextSyncBlock(cleanup);
    }

    private void EnumerateStressLog()
    {
        IStressLog stressLog = _target.Contracts.StressLog;
        if (!stressLog.HasStressLog())
            return;

        StressLogData data = stressLog.GetStressLogData();
        foreach (StressLogMemoryRange range in stressLog.GetStressLogMemoryRanges(data))
            _emitter.Add(range.Start.Value, range.Size);
    }
}
