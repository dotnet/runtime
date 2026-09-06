// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;
using ContractModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader.EnumMemory;

internal sealed class DumpCreator
{
    private const int MaxSyncBlocks = 1_000_000;
    private const int MaxThreads = 1_000_000;

    private readonly Target _target;
    private readonly RuntimeModuleInfo _runtimeModule;
    private readonly bool _includeHeap;
    private readonly MemoryRegionEmitter _emitter;
    private readonly HashSet<TargetPointer> _loaderAllocators = [];
    private readonly MethodCollector _methods;
    private readonly ObjectCollector _objects;

    public DumpCreator(
        Target target,
        RuntimeModuleInfo runtimeModule,
        bool includeHeap,
        MemoryRegionEmitter emitter)
    {
        _target = target;
        _runtimeModule = runtimeModule;
        _includeHeap = includeHeap;
        _emitter = emitter;
        _methods = new(target);
        _objects = new(target, emitter, _methods);
    }

    public void EnumerateMemoryRegions()
    {
        TryEnumerate(EnumerateRuntimeModule);
        TryEnumerate(EnumerateStatics);
        TryEnumerate(EnumerateModules);
        TryEnumerate(EnumerateThreads);

        if (_includeHeap)
        {
            TryEnumerate(EnumerateGC);
            TryEnumerate(EnumerateCodeAndLoaderHeaps);
            TryEnumerate(EnumerateSyncBlocks);
            TryEnumerate(EnumerateStressLog);
        }

        TryEnumerate(WriteMiniMetadata);
    }

    private void EnumerateRuntimeModule()
    {
        _runtimeModule.EnumerateMemoryRegions(_emitter);
    }

    private static void TryEnumerate(Action enumerate)
    {
        try
        {
            enumerate();
        }
        catch (System.Exception ex) when (ex.HResult != HResults.COR_E_OPERATIONCANCELED)
        {
            // Dump creation is best-effort because the target may be partially unreadable or corrupt.
        }
    }

    private void EnumerateStatics()
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        rts.GetWellKnownMethodTable(WellKnownMethodTable.Object);
        rts.GetWellKnownMethodTable(WellKnownMethodTable.String);
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
            ModuleFlags flags = loader.GetFlags(module);
            loader.GetSimpleName(module);
            loader.GetPath(module);
            loader.GetFileName(module);

            if (loader.TryGetLoadedImageContents(module, out _, out _, out _))
                _emitter.RegisterMetadataRange(ecmaMetadata.GetReadOnlyMetadataAddress(module));

            if (flags.HasFlag(ModuleFlags.ReflectionEmit))
                _emitter.RegisterMetadataRange(ecmaMetadata.GetReadWriteSavedMetadataAddress(module));

            // Smaller dumps do not include in-memory symbols unless they are otherwise referenced.
            if (loader.TryGetSymbolStream(module, out TargetPointer symbolBuffer, out uint symbolSize) && _includeHeap)
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
        _ = thread.GetThreadCounts();
        HashSet<TargetPointer> visited = [];
        TargetPointer threadAddress = threadStore.FirstThread;

        for (int i = 0; threadAddress != TargetPointer.Null && i < MaxThreads; i++)
        {
            if (!visited.Add(threadAddress))
                break;

            ThreadData threadData = thread.GetThreadData(threadAddress);
            thread.GetThreadAllocContext(threadAddress, out _, out _);
            EnumerateStack(threadData);
            if (threadData.LastThrownObjectHandle != TargetPointer.Null)
            {
                TargetPointer exceptionObject =
                    _target.ReadPointer(threadData.LastThrownObjectHandle.Value);
                _objects.EnumerateObject(exceptionObject);
            }

            threadAddress = threadData.NextThread;
        }
    }

    private void EnumerateStack(ThreadData threadData)
    {
        IStackWalk stackWalk = _target.Contracts.StackWalk;
        foreach (IStackDataFrameHandle frame in stackWalk.CreateStackWalk(threadData))
        {
            _ = stackWalk.GetInstructionPointer(frame);
            TargetPointer methodDesc = stackWalk.GetMethodDescPtr(frame);
            _methods.CaptureMethod(methodDesc);
        }
    }

    private void WriteMiniMetadata()
    {
        Dictionary<TargetPointer, string> names = new(_methods.Names);
        foreach ((TargetPointer address, string name) in _objects.Names)
            names.TryAdd(address, name);

        MiniMetadataWriter.Write(_target, _emitter, names);
    }

    private void EnumerateGC()
    {
        IGC gc = _target.Contracts.GC;
        if (!gc.GetGCStructuresValid())
            return;

        string[] identifiers = gc.GetGCIdentifiers();
        gc.GetGCHeapCount();
        gc.GetMaxGeneration();
        gc.GetGCBounds(out _, out _);
        gc.GetCurrentGCState();
        gc.TryGetGCDynamicAdaptationMode(out _);
        gc.GetGlobalMechanisms();
        gc.GetGlobalAllocationContext(out _, out _);

        if (Array.IndexOf(identifiers, GCIdentifiers.Workstation) >= 0)
        {
            gc.GetHeapData();
            gc.GetOomData();
        }
        else
        {
            foreach (TargetPointer heap in gc.GetGCHeaps())
            {
                gc.GetHeapData(heap);
                gc.GetOomData(heap);
            }
        }

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
        TraverseTargetLinkedList(cleanup, syncBlock.GetNextSyncBlock);
    }

    internal static void TraverseTargetLinkedList(
        TargetPointer current,
        Func<TargetPointer, TargetPointer> getNext)
    {
        HashSet<TargetPointer> visited = [];
        for (int i = 0;
             current != TargetPointer.Null && i < MaxSyncBlocks && visited.Add(current);
             i++)
        {
            current = getNext(current);
        }
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
