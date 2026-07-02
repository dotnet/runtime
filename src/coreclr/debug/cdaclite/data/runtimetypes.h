// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// runtimetypes.h
//
// Data types: typed readers for descriptor-defined runtime structures, mirroring
// the managed cDAC Data.<T> classes. Each type is a small self-contained block:
// a constructor naming its descriptor type, then one line per field. Field
// offsets are read from the target's in-memory descriptor at runtime (never
// written here). Access is ergonomic, e.g. `segment.Mem`, `thread.LinkNext`.
//
// See datatype.h for the Struct/field infrastructure and the CDAC_* macros.
//*****************************************************************************

#ifndef CDACLITE_RUNTIMETYPES_H
#define CDACLITE_RUNTIMETYPES_H

#include "datatype.h"

namespace cdac
{
namespace data
{
    // --- GC ---------------------------------------------------------------

    struct Generation : Struct
    {
        Generation() : Struct("Generation") {}
        CDAC_PTR(StartSegment)
    };

    struct HeapSegment : Struct
    {
        HeapSegment() : Struct("HeapSegment") {}
        CDAC_PTR(Mem)       // start of the segment's used range
        CDAC_PTR(Allocated) // end of the used range
        CDAC_PTR(Next)      // next segment in the generation list
    };

    struct GCHeap : Struct
    {
        GCHeap() : Struct("GCHeap") {}
        CDAC_ADDR(GenerationTable)      // inline array (field address)
        CDAC_PTR(EphemeralHeapSegment)
        CDAC_PTR(AllocAllocated)
        CDAC_PTR(FinalizeQueue)
        CDAC_OPT_PTR(FreeRegions)
        CDAC_OPT_PTR(FreeableSohSegment)
        CDAC_OPT_PTR(FreeableUohSegment)
    };

    // Finalize queue. FillPointers is an inline array (field address); its length is
    // the global CFinalizeFillPointersLength. The cDAC re-reads this array in GetFillPointers.
    struct CFinalize : Struct
    {
        CFinalize() : Struct("CFinalize") {}
        CDAC_ADDR(FillPointers)
    };

    // GC bookkeeping card-table info node (linked via NextCardTable).
    struct CardTableInfo : Struct
    {
        CardTableInfo() : Struct("CardTableInfo") {}
        CDAC_PTR(NextCardTable)
    };

    // A region free list head. The cDAC walks HeadFreeRegion as a HeapSegment.Next list.
    struct RegionFreeList : Struct
    {
        RegionFreeList() : Struct("RegionFreeList") {}
        CDAC_PTR(HeadFreeRegion)
    };

    // --- Threads ----------------------------------------------------------

    struct ThreadStore : Struct
    {
        ThreadStore() : Struct("ThreadStore") {}
        CDAC_PTR(FirstThreadLink) // head Thread*
        CDAC_U32(ThreadCount)
    };

    struct Thread : Struct
    {
        Thread() : Struct("Thread") {}
        CDAC_PTR(LinkNext)         // next Thread*
        CDAC_PTR(CachedStackBase)  // high address (stack grows down)
        CDAC_PTR(CachedStackLimit) // low address
        CDAC_PTR(Frame)
    };

    // --- Loader / modules -------------------------------------------------

    struct AppDomain : Struct
    {
        AppDomain() : Struct("AppDomain") {}
        CDAC_PTR(RootAssembly)
        CDAC_ADDR(AssemblyList) // embedded ArrayListBase
    };

    // Intrusive block-list container (arraylist.h). AssemblyList is one of these.
    struct ArrayListBase : Struct
    {
        ArrayListBase() : Struct("ArrayListBase") {}
        CDAC_U32(Count)       // total element count across all blocks
        CDAC_ADDR(FirstBlock) // embedded first block
    };

    struct ArrayListBlock : Struct
    {
        ArrayListBlock() : Struct("ArrayListBlock") {}
        CDAC_PTR(Next)        // next block, or null
        CDAC_U32(Size)        // element capacity of this block
        CDAC_ADDR(ArrayStart) // embedded element array (pointer-sized elements)
    };

    struct Assembly : Struct
    {
        Assembly() : Struct("Assembly") {}
        CDAC_PTR(Module)
    };

    struct Module : Struct
    {
        Module() : Struct("Module") {}
        CDAC_PTR(Assembly)
        CDAC_PTR(PEAssembly)
        CDAC_PTR(LoaderAllocator)
        CDAC_OPT_PTR(GrowableSymbolStream) // in-memory symbol (PDB) stream, if any
    };

    // In-memory symbol stream (module's growable PDB buffer).
    struct CGrowableSymbolStream : Struct
    {
        CGrowableSymbolStream() : Struct("CGrowableSymbolStream") {}
        CDAC_PTR(Buffer)
        CDAC_U32(Size)
    };

    struct PEAssembly : Struct
    {
        PEAssembly() : Struct("PEAssembly") {}
        CDAC_PTR(PEImage)
    };

    struct PEImage : Struct
    {
        PEImage() : Struct("PEImage") {}
        CDAC_PTR(LoadedImageLayout)
    };

    struct PEImageLayout : Struct
    {
        PEImageLayout() : Struct("PEImageLayout") {}
        CDAC_PTR(Base) // loaded image base (code + metadata; file-backed)
        CDAC_U32(Size)
        CDAC_U32(Flags)
    };

    // --- GC handles (roots) -----------------------------------------------

    struct HandleTableMap : Struct
    {
        HandleTableMap() : Struct("HandleTableMap") {}
        CDAC_PTR(BucketsPtr) // array of HandleTableBucket* (InitialHandleTableArraySize entries)
        CDAC_PTR(Next)       // next map, or null
    };

    struct HandleTableBucket : Struct
    {
        HandleTableBucket() : Struct("HandleTableBucket") {}
        CDAC_PTR(Table) // array of HandleTable* (1 for WKS, TotalCpuCount for SVR)
    };

    struct HandleTable : Struct
    {
        HandleTable() : Struct("HandleTable") {}
        CDAC_PTR(SegmentList) // first TableSegment
    };

    struct TableSegment : Struct
    {
        TableSegment() : Struct("TableSegment") {}
        CDAC_PTR(NextSegment)
    };

    // --- JIT code heaps ---------------------------------------------------

    struct EEJitManager : Struct
    {
        EEJitManager() : Struct("EEJitManager") {}
        CDAC_PTR(AllCodeHeaps) // head of the CodeHeapListNode list
    };

    struct CodeHeapListNode : Struct
    {
        CodeHeapListNode() : Struct("CodeHeapListNode") {}
        CDAC_PTR(Next)
        CDAC_PTR(StartAddress) // start of the JIT code range
        CDAC_PTR(EndAddress)   // end of the JIT code range
        CDAC_PTR(Heap)         // the CodeHeap (Loader/Host) backing this node
        CDAC_PTR(MapBase)      // base address the NibbleMap is relative to
        CDAC_PTR(HeaderMap)    // the NibbleMap (IP -> code header) storage
    };

    // --- ExecutionManager / code lookup (for the Normal-tier stack scan) ----

    // Top of the multi-level range-section radix map (ExecutionManagerCodeRangeMapAddress).
    struct RangeSectionMap : Struct
    {
        RangeSectionMap() : Struct("RangeSectionMap") {}
        CDAC_PTR(TopLevelData)
    };

    struct RangeSectionFragment : Struct
    {
        RangeSectionFragment() : Struct("RangeSectionFragment") {}
        CDAC_PTR(RangeBegin)
        CDAC_PTR(RangeEndOpen)
        CDAC_PTR(RangeSection)
        CDAC_PTR(Next)
    };

    struct RangeSection : Struct
    {
        RangeSection() : Struct("RangeSection") {}
        CDAC_PTR(RangeBegin)
        CDAC_PTR(RangeEndOpen)
        CDAC_PTR(JitManager)
        CDAC_U32(Flags)
        CDAC_OPT_PTR(HeapList)  // EEJitManager: CodeHeapListNode for this range
        CDAC_OPT_PTR(R2RModule) // ReadyToRun: the module (image on disk)
    };

    // The code header immediately preceding a JIT method body; MethodDesc identifies the method.
    struct RealCodeHeader : Struct
    {
        RealCodeHeader() : Struct("RealCodeHeader") {}
        CDAC_PTR(MethodDesc)
    };

    // Code heap base type; HeapType selects LoaderCodeHeap vs HostCodeHeap.
    struct CodeHeap : Struct
    {
        CodeHeap() : Struct("CodeHeap") {}
        CDAC_U32(HeapType)
    };

    struct LoaderCodeHeap : Struct
    {
        LoaderCodeHeap() : Struct("LoaderCodeHeap") {}
        CDAC_PTR(LoaderHeap)
    };

    struct HostCodeHeap : Struct
    {
        HostCodeHeap() : Struct("HostCodeHeap") {}
        CDAC_PTR(BaseAddress)
        CDAC_PTR(CurrentAddress)
    };

    // --- Loader heaps -----------------------------------------------------

    struct SystemDomain : Struct
    {
        SystemDomain() : Struct("SystemDomain") {}
        CDAC_ADDR(GlobalLoaderAllocator) // embedded LoaderAllocator
    };

    struct LoaderAllocator : Struct
    {
        LoaderAllocator() : Struct("LoaderAllocator") {}
        // Each heap is a LoaderHeap*. Some are configuration/architecture-specific,
        // so they are optional (absent -> 0, skipped).
        CDAC_OPT_PTR(HighFrequencyHeap)
        CDAC_OPT_PTR(LowFrequencyHeap)
        CDAC_OPT_PTR(StaticsHeap)
        CDAC_OPT_PTR(StubHeap)
        CDAC_OPT_PTR(ExecutableHeap)
        CDAC_OPT_PTR(FixupPrecodeHeap)
        CDAC_OPT_PTR(NewStubPrecodeHeap)
        CDAC_OPT_PTR(DynamicHelpersStubHeap)
    };

    struct LoaderHeap : Struct
    {
        LoaderHeap() : Struct("LoaderHeap") {}
        CDAC_PTR(FirstBlock)
    };

    struct LoaderHeapBlock : Struct
    {
        LoaderHeapBlock() : Struct("LoaderHeapBlock") {}
        CDAC_PTR(Next)
        CDAC_PTR(VirtualAddress)
        CDAC_PTR(VirtualSize) // nuint (pointer-sized)
    };

    // --- Sync blocks ------------------------------------------------------

    struct SyncTableEntry : Struct
    {
        SyncTableEntry() : Struct("SyncTableEntry") {}
        CDAC_PTR(SyncBlock)
        CDAC_PTR(Object) // low bit set => entry is free
    };

    struct SyncBlockCache : Struct
    {
        SyncBlockCache() : Struct("SyncBlockCache") {}
        CDAC_U32(FreeSyncTableIndex)
        CDAC_OPT_PTR(CleanupBlockList)
    };

    struct SyncBlock : Struct
    {
        SyncBlock() : Struct("SyncBlock") {}
        CDAC_PTR(Lock) // ObjectHandle to the managed Lock object
    };

    // --- Stress log -------------------------------------------------------

    struct StressLog : Struct
    {
        StressLog() : Struct("StressLog") {}
        CDAC_PTR(Logs) // head of the ThreadStressLog list
    };

    struct ThreadStressLog : Struct
    {
        ThreadStressLog() : Struct("ThreadStressLog") {}
        CDAC_PTR(Next)
        CDAC_PTR(ChunkListHead)
        CDAC_PTR(ChunkListTail)
        CDAC_PTR(CurrentWriteChunk)
    };

    struct StressLogChunk : Struct
    {
        StressLogChunk() : Struct("StressLogChunk") {}
        CDAC_PTR(Prev)
        CDAC_PTR(Next)
    };

    // --- COM interop ------------------------------------------------------

    struct RCWCleanupList : Struct
    {
        RCWCleanupList() : Struct("RCWCleanupList") {}
        CDAC_PTR(FirstBucket)
    };

    struct RCW : Struct
    {
        RCW() : Struct("RCW") {}
        CDAC_PTR(NextCleanupBucket)
        CDAC_PTR(NextRCW)
        CDAC_OPT_PTR(CtxEntry)
    };

    // Apartment (STA) context entry referenced by an RCW bucket (GetSTAThread).
    struct CtxEntry : Struct
    {
        CtxEntry() : Struct("CtxEntry") {}
        CDAC_PTR(STAThread)
    };
}
}

#endif // CDACLITE_RUNTIMETYPES_H
