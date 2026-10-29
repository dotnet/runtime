// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using ContractModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader.Legacy.EnumMemory;

internal enum DumpType
{
    Mini,
    Heap,
    Triage,
}

internal sealed class MemoryEnumerator
{
    private const int MaxSyncBlocks = 1_000_000;
    private const int MaxThreads = 1_000_000;

    private readonly CollectorTarget _collectorTarget;
    private readonly HashSet<TargetPointer> _loaderAllocators = [];
    private readonly MethodCollector _methods;
    private readonly ObjectCollector _objects;
    private readonly PEImageCollector _peImageCollector;

    private MemoryEnumerator(CollectorTarget target)
    {
        _collectorTarget = target;
        _methods = new(target);
        _objects = new(target, _methods);
        _peImageCollector = new(target);
    }

    public static int Enumerate(Target target, MemoryRegionEmitter emitter, DumpType dumpType)
    {
        if (target is not ContractDescriptorTarget descriptorTarget)
            return HResults.E_NOTIMPL;

        target.Flush(FlushScope.All);
        using IDisposable readScope = descriptorTarget.RegisterReadCallback((address, size) =>
        {
            if (emitter.ShouldEmitTargetRead(address, size))
                emitter.Add(address, size);
        });
        foreach (TargetSpan range in descriptorTarget.EnumerateDescriptorMemory())
            emitter.Add(range.Address.Value, range.Size);

        new MemoryEnumerator(new(target, dumpType, emitter)).EnumerateMemoryRegions();
        return emitter.Result;
    }

    private void EnumerateMemoryRegions()
    {
        TryEnumerate(EnumerateNativeRuntimeModule);
        TryEnumerate(EnumerateStatics);
        TryEnumerate(EnumerateDebugger);
        TryEnumerate(EnumerateManagedModules);
        TryEnumerate(EnumerateThreadExceptions);
        TryEnumerate(EnumerateThreads);

        if (_collectorTarget.DumpType == DumpType.Heap)
        {
            TryEnumerate(EnumerateGC);
            TryEnumerate(EnumerateCodeAndLoaderHeaps);
            TryEnumerate(EnumerateSyncBlocks);
            TryEnumerate(EnumerateAuxiliarySymbols);
            TryEnumerate(EnumerateStressLog);
        }

        if (_collectorTarget.DumpType is DumpType.Mini or DumpType.Triage)
            TryEnumerate(_collectorTarget.MiniMetadata.Write);
    }

    private void EnumerateNativeRuntimeModule()
    {
        if (_collectorTarget.Target.TryGetRuntimeImageBase(out TargetPointer imageBase))
            _peImageCollector.EnumerateMemoryRegions(imageBase, uint.MaxValue, isMapped: true, includeExportsAndResources: true);
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
        IRuntimeTypeSystem rts = _collectorTarget.Target.Contracts.RuntimeTypeSystem;
        rts.GetWellKnownMethodTable(WellKnownMethodTable.Array);
        rts.GetWellKnownMethodTable(WellKnownMethodTable.Canon);
        rts.GetWellKnownMethodTable(WellKnownMethodTable.Exception);
        rts.GetWellKnownMethodTable(WellKnownMethodTable.Free);
        rts.GetWellKnownMethodTable(WellKnownMethodTable.Object);
        rts.GetWellKnownMethodTable(WellKnownMethodTable.String);
    }

    private void EnumerateDebugger()
    {
        IDebugger debugger = _collectorTarget.Target.Contracts.Debugger;
        debugger.TryGetDebuggerData(out _);
        debugger.GetAttachStateFlags();
        _collectorTarget.Target.Contracts.ReJIT.IsEnabled();
    }

    private void EnumerateManagedModules()
    {
        ILoader loader = _collectorTarget.Target.Contracts.Loader;
        IEcmaMetadata ecmaMetadata = _collectorTarget.Target.Contracts.EcmaMetadata;

        TargetPointer appDomain = loader.GetAppDomain();
        loader.GetRootAssembly();
        IEnumerable<ContractModuleHandle> modules = loader.GetModuleHandles(
            appDomain,
            AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution);

        foreach (ContractModuleHandle module in modules)
        {
            loader.GetModule(module);
            loader.GetAssembly(module);
            TargetPointer peAssembly = loader.GetPEAssembly(module);
            ModuleFlags flags = loader.GetFlags(module);
            loader.GetSimpleName(module);
            if (_collectorTarget.DumpType != DumpType.Triage)
                loader.GetPath(module);
            loader.GetFileName(module);
            loader.GetILBase(module);
            loader.GetModuleLookupMapBase(module, ModuleLookupMapKind.FieldDefToDesc);
            loader.GetModuleLookupMapBase(module, ModuleLookupMapKind.ManifestModuleReferences);
            loader.GetModuleLookupMapBase(module, ModuleLookupMapKind.MemberRefToDesc);
            loader.GetModuleLookupMapBase(module, ModuleLookupMapKind.MethodDefToDesc);
            loader.GetModuleLookupMapBase(module, ModuleLookupMapKind.TypeDefToMethodTable);
            loader.GetModuleLookupMapBase(module, ModuleLookupMapKind.TypeRefToMethodTable);

            if (loader.TryGetLoadedImageContents(module, out TargetPointer imageBase, out uint imageSize, out uint imageFlags))
            {
                _collectorTarget.Emitter.RegisterMetadataRange(ecmaMetadata.GetReadOnlyMetadataAddress(module));
                TryEnumerate(() =>
                {
                    const uint ImageFlagMapped = 1;
                    _peImageCollector.EnumerateMemoryRegions(imageBase, imageSize, (imageFlags & ImageFlagMapped) != 0);
                });
            }

            if (flags.HasFlag(ModuleFlags.ReflectionEmit))
                _collectorTarget.Emitter.RegisterMetadataRange(ecmaMetadata.GetReadWriteSavedMetadataAddress(module));

            // Smaller dumps do not include in-memory symbols unless they are otherwise referenced.
            if (loader.TryGetSymbolStream(module, out TargetPointer symbolBuffer, out uint symbolSize) && _collectorTarget.DumpType == DumpType.Heap)
                _collectorTarget.Emitter.Add(symbolBuffer.Value, symbolSize);

            if (peAssembly != TargetPointer.Null)
                ecmaMetadata.HasReadWriteMetadata(peAssembly);

            TargetPointer loaderAllocator = loader.GetLoaderAllocator(module);
            if (loaderAllocator != TargetPointer.Null)
                _loaderAllocators.Add(loaderAllocator);

            loader.IsProbeExtensionResultValid(module);
        }

        TargetPointer globalLoaderAllocator = loader.GetGlobalLoaderAllocator();
        if (globalLoaderAllocator != TargetPointer.Null)
        {
            _loaderAllocators.Add(globalLoaderAllocator);
            loader.GetHighFrequencyHeap(globalLoaderAllocator);
            loader.GetLowFrequencyHeap(globalLoaderAllocator);
        }

        foreach (ContractModuleHandle module in loader.GetModuleHandles(
            appDomain,
            AssemblyIterationFlags.IncludeLoading | AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution))
        {
            loader.IsAssemblyLoaded(module);
        }

        foreach (ContractModuleHandle _ in loader.GetModuleHandles(appDomain, AssemblyIterationFlags.IncludeFailedToLoad))
        {
        }
    }

    private IEnumerable<ThreadData> EnumerateThreadData()
    {
        IThread thread = _collectorTarget.Target.Contracts.Thread;
        ThreadStoreData threadStore = thread.GetThreadStoreData();
        HashSet<TargetPointer> visited = [];
        TargetPointer threadAddress = threadStore.FirstThread;

        for (int i = 0; threadAddress != TargetPointer.Null && i < MaxThreads; i++)
        {
            if (!visited.Add(threadAddress))
                break;

            ThreadData threadData = thread.GetThreadData(threadAddress);
            yield return threadData;
            threadAddress = threadData.NextThread;
        }
    }

    private void EnumerateThreadExceptions()
    {
        // Collect exceptions across all threads before ordinary stack walks so their type and saved-frame
        // method names get priority in the bounded mini-metadata stream.
        foreach (ThreadData threadData in EnumerateThreadData())
        {
            if (threadData.LastThrownObjectHandle != TargetPointer.Null)
            {
                TargetPointer exceptionObject =
                    _collectorTarget.Target.ReadPointer(threadData.LastThrownObjectHandle.Value);
                _objects.EnumerateObject(exceptionObject);
            }
        }
    }

    private void EnumerateThreads()
    {
        IThread thread = _collectorTarget.Target.Contracts.Thread;
        _ = thread.GetThreadCounts();
        foreach (ThreadData threadData in EnumerateThreadData())
        {
            thread.GetThreadAllocContext(threadData.ThreadAddress, out _, out _);
            EnumerateStack(threadData);
        }
    }

    private void EnumerateStack(ThreadData threadData)
    {
        IStackWalk stackWalk = _collectorTarget.Target.Contracts.StackWalk;
        foreach (IStackDataFrameHandle frame in stackWalk.CreateStackWalk(threadData))
        {
            TargetCodePointer instructionPointer = stackWalk.GetInstructionPointer(frame);
            TargetPointer methodDesc = stackWalk.GetMethodDescPtr(frame);
            _methods.CaptureMethod(methodDesc, instructionPointer);
        }
    }

    private void EnumerateGC()
    {
        IGC gc = _collectorTarget.Target.Contracts.GC;
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
                _collectorTarget.Emitter.Add(segment.Start.Value, segment.End.Value - segment.Start.Value);
        }

        AddGCMemoryRegions(gc.GetHandleTableMemoryRegions());
        AddGCMemoryRegions(gc.GetGCBookkeepingMemoryRegions());
        AddGCMemoryRegions(gc.GetGCFreeRegions());
    }

    private void AddGCMemoryRegions(IReadOnlyList<GCMemoryRegionData> regions)
    {
        foreach (GCMemoryRegionData region in regions)
            _collectorTarget.Emitter.Add(region.Start.Value, region.Size);
    }

    private void EnumerateCodeAndLoaderHeaps()
    {
        IExecutionManager executionManager = _collectorTarget.Target.Contracts.ExecutionManager;
        foreach (JitManagerKind kind in new[] { JitManagerKind.EE, JitManagerKind.Interpreter })
        {
            foreach (ICodeHeapInfo codeHeap in executionManager.GetCodeHeapInfos(kind))
            {
                switch (codeHeap)
                {
                    case HostCodeHeapInfo host when host.CurrentAddress > host.BaseAddress:
                        _collectorTarget.Emitter.Add(host.BaseAddress.Value, host.CurrentAddress.Value - host.BaseAddress.Value);
                        break;
                    case LoaderCodeHeapInfo loader:
                        AddLoaderHeapBlocks(loader.LoaderHeapAddress);
                        break;
                }
            }
        }

        ILoader loaderContract = _collectorTarget.Target.Contracts.Loader;
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

        ILoader loader = _collectorTarget.Target.Contracts.Loader;
        foreach (LoaderHeapBlock block in loader.EnumerateLoaderHeapBlocks(loaderHeap))
            _collectorTarget.Emitter.Add(block.Address.Value, block.Size.Value);
    }

    private void EnumerateSyncBlocks()
    {
        ISyncBlock syncBlock = _collectorTarget.Target.Contracts.SyncBlock;
        IThread thread = _collectorTarget.Target.Contracts.Thread;
        uint count = syncBlock.GetSyncBlockCount();
        for (uint i = 1; i <= count; i++)
        {
            TargetPointer address = syncBlock.GetSyncBlock(i);
            if (address == TargetPointer.Null || syncBlock.IsSyncBlockFree(i))
                continue;

            syncBlock.GetSyncBlockObject(i);
            if (syncBlock.TryGetLockInfo(address, out uint owningThreadId, out _))
                thread.IdToThread(owningThreadId);
            syncBlock.GetAdditionalThreadCount(address);
            syncBlock.GetBuiltInComData(address, out _, out _, out _);
        }

        TargetPointer cleanup = syncBlock.GetSyncBlockFromCleanupList();
        TraverseTargetLinkedList(cleanup, syncBlock.GetNextSyncBlock);
    }

    private void EnumerateAuxiliarySymbols()
    {
        foreach ((TargetCodePointer _, string _) in _collectorTarget.Target.Contracts.AuxiliarySymbols.EnumerateAuxiliarySymbols())
        {
        }
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
        IStressLog stressLog = _collectorTarget.Target.Contracts.StressLog;
        if (!stressLog.HasStressLog())
            return;

        StressLogData data = stressLog.GetStressLogData();
        foreach (StressLogMemoryRange range in stressLog.GetStressLogMemoryRanges(data))
            _collectorTarget.Emitter.Add(range.Start.Value, range.Size);
    }
}
