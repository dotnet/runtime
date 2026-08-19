// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;
using ContractModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader.DumpCollect;

internal static class DumpCreator
{
    private const int MaxThreads = 1_000_000;

    public static void EnumerateMemoryRegions(
        Target target,
        bool includeHeap,
        MemoryRegionEmitter emitter)
    {
        HashSet<TargetPointer> loaderAllocators = [];
        TryEnumerate("modules", () => EnumerateModules(target, emitter, loaderAllocators));
        TryEnumerate("threads", () => EnumerateThreads(target, emitter));

        if (includeHeap)
        {
            TryEnumerate("GC", () => EnumerateGC(target, emitter));
            TryEnumerate("code and loader heaps", () => EnumerateCodeAndLoaderHeaps(target, emitter, loaderAllocators));
            TryEnumerate("sync blocks", () => EnumerateSyncBlocks(target));
            TryEnumerate("stress log", () => EnumerateStressLog(target, emitter));
        }
    }

    private static void TryEnumerate(string phase, Action enumerate)
    {
        DumpCollectLogger.Log($"Starting {phase} enumeration.");
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
    }

    private static void EnumerateModules(
        Target target,
        MemoryRegionEmitter emitter,
        HashSet<TargetPointer> loaderAllocators)
    {
        ILoader loader = target.Contracts.Loader;
        IEcmaMetadata ecmaMetadata = target.Contracts.EcmaMetadata;

        TargetPointer appDomain = loader.GetAppDomain();
        IEnumerable<ContractModuleHandle> modules = loader.GetModuleHandles(
            appDomain,
            AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution);

        foreach (ContractModuleHandle module in modules)
        {
            loader.GetModule(module);
            TargetPointer peAssembly = loader.GetPEAssembly(module);
            ModuleFlags flags = loader.GetFlags(module);

            if (loader.TryGetLoadedImageContents(module, out _, out _, out _))
            {
                TargetSpan metadata = flags.HasFlag(ModuleFlags.ReflectionEmit)
                    ? ecmaMetadata.GetReadWriteSavedMetadataAddress(module)
                    : ecmaMetadata.GetReadOnlyMetadataAddress(module);
                emitter.Add(metadata.Address.Value, metadata.Size);
            }

            if (loader.TryGetSymbolStream(module, out TargetPointer symbolBuffer, out uint symbolSize))
                emitter.Add(symbolBuffer.Value, symbolSize);

            if (peAssembly != TargetPointer.Null)
                ecmaMetadata.HasReadWriteMetadata(peAssembly);

            TargetPointer loaderAllocator = loader.GetLoaderAllocator(module);
            if (loaderAllocator != TargetPointer.Null)
                loaderAllocators.Add(loaderAllocator);
        }

        TargetPointer globalLoaderAllocator = loader.GetGlobalLoaderAllocator();
        if (globalLoaderAllocator != TargetPointer.Null)
            loaderAllocators.Add(globalLoaderAllocator);
    }

    private static void EnumerateThreads(Target target, MemoryRegionEmitter emitter)
    {
        IThread thread = target.Contracts.Thread;
        IStackWalk stackWalk = target.Contracts.StackWalk;
        IExecutionManager executionManager = target.Contracts.ExecutionManager;
        ThreadStoreData threadStore = thread.GetThreadStoreData();
        HashSet<TargetPointer> visited = [];
        TargetPointer threadAddress = threadStore.FirstThread;

        for (int i = 0; threadAddress != TargetPointer.Null && i < MaxThreads; i++)
        {
            if (!visited.Add(threadAddress))
                break;

            ThreadData threadData = thread.GetThreadData(threadAddress);
            thread.GetThreadAllocContext(threadAddress, out _, out _);
            thread.GetStackLimitData(threadAddress, out TargetPointer stackBase, out TargetPointer stackLimit, out _);

            if (stackLimit != TargetPointer.Null && stackBase > stackLimit)
                emitter.Add(stackLimit.Value, stackBase.Value - stackLimit.Value);

            foreach (StackReferenceData _ in stackWalk.WalkStackReferences(threadData, resolveInteriorPointers: true))
            {
            }

            foreach (IStackDataFrameHandle frame in stackWalk.CreateStackWalk(threadData))
            {
                TargetCodePointer instructionPointer = stackWalk.GetInstructionPointer(frame);
                stackWalk.GetMethodDescPtr(frame);

                CodeBlockHandle? codeBlock = executionManager.GetCodeBlockHandle(instructionPointer);
                if (codeBlock is null)
                    continue;

                TargetPointer codeStart = executionManager.GetStartAddress(codeBlock.Value);
                executionManager.GetMethodRegionInfo(
                    codeBlock.Value,
                    out uint hotSize,
                    out TargetPointer coldStart,
                    out uint coldSize);
                emitter.Add(codeStart.Value, hotSize);
                emitter.Add(coldStart.Value, coldSize);
            }

            threadAddress = threadData.NextThread;
        }
    }

    private static void EnumerateGC(Target target, MemoryRegionEmitter emitter)
    {
        IGC gc = target.Contracts.GC;
        if (!gc.GetGCStructuresValid())
            return;

        foreach ((GCHeapSegmentInfo segment, _) in gc.EnumerateAllSegments())
        {
            if (segment.End > segment.Start)
                emitter.Add(segment.Start.Value, segment.End.Value - segment.Start.Value);
        }

        AddGCMemoryRegions(gc.GetHandleTableMemoryRegions(), emitter);
        AddGCMemoryRegions(gc.GetGCBookkeepingMemoryRegions(), emitter);
        AddGCMemoryRegions(gc.GetGCFreeRegions(), emitter);
    }

    private static void AddGCMemoryRegions(
        IReadOnlyList<GCMemoryRegionData> regions,
        MemoryRegionEmitter emitter)
    {
        foreach (GCMemoryRegionData region in regions)
            emitter.Add(region.Start.Value, region.Size);
    }

    private static void EnumerateCodeAndLoaderHeaps(
        Target target,
        MemoryRegionEmitter emitter,
        HashSet<TargetPointer> loaderAllocators)
    {
        IExecutionManager executionManager = target.Contracts.ExecutionManager;
        foreach (ICodeHeapInfo codeHeap in executionManager.GetCodeHeapInfos())
        {
            switch (codeHeap)
            {
                case HostCodeHeapInfo host when host.CurrentAddress > host.BaseAddress:
                    emitter.Add(host.BaseAddress.Value, host.CurrentAddress.Value - host.BaseAddress.Value);
                    break;
                case LoaderCodeHeapInfo loader:
                    AddLoaderHeapBlocks(target, loader.LoaderHeapAddress, emitter);
                    break;
            }
        }

        ILoader loaderContract = target.Contracts.Loader;
        foreach (TargetPointer loaderAllocator in loaderAllocators)
        {
            IReadOnlyDictionary<LoaderAllocatorHeapType, TargetPointer> heaps =
                loaderContract.GetLoaderAllocatorHeaps(loaderAllocator);
            foreach (TargetPointer loaderHeap in heaps.Values)
                AddLoaderHeapBlocks(target, loaderHeap, emitter);
        }
    }

    private static void AddLoaderHeapBlocks(
        Target target,
        TargetPointer loaderHeap,
        MemoryRegionEmitter emitter)
    {
        if (loaderHeap == TargetPointer.Null)
            return;

        ILoader loader = target.Contracts.Loader;
        foreach (LoaderHeapBlock block in loader.EnumerateLoaderHeapBlocks(loaderHeap))
            emitter.Add(block.Address.Value, block.Size.Value);
    }

    private static void EnumerateSyncBlocks(Target target)
    {
        ISyncBlock syncBlock = target.Contracts.SyncBlock;
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

    private static void EnumerateStressLog(Target target, MemoryRegionEmitter emitter)
    {
        IStressLog stressLog = target.Contracts.StressLog;
        if (!stressLog.HasStressLog())
            return;

        StressLogData data = stressLog.GetStressLogData();
        foreach (StressLogMemoryRange range in stressLog.GetStressLogMemoryRanges(data))
            emitter.Add(range.Start.Value, range.Size);
    }
}
